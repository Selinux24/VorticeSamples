using Direct3D12.Hlsl;
using PrimalLike.Graphics;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Vortice.Dxc;

namespace Direct3D12.ShaderCompiler
{
    static class Compiler
    {
        static DxcShaderModel DefaultShaderModel { get; set; } = DxcShaderModel.Model6_6;
        static string GetShaderProfile(ShaderTypes stage)
        {
            var dxcStage = stage switch
            {
                ShaderTypes.Vertex => DxcShaderStage.Vertex,
                ShaderTypes.Hull => DxcShaderStage.Hull,
                ShaderTypes.Domain => DxcShaderStage.Domain,
                ShaderTypes.Geometry => DxcShaderStage.Geometry,
                ShaderTypes.Pixel => DxcShaderStage.Pixel,
                ShaderTypes.Compute => DxcShaderStage.Compute,
                ShaderTypes.Amplification => DxcShaderStage.Amplification,
                ShaderTypes.Mesh => DxcShaderStage.Mesh,
                _ => DxcShaderStage.Vertex,
            };

            return DxcCompiler.GetShaderProfile(dxcStage, DefaultShaderModel);
        }

        public static bool CompileEngineShaders(string outputPath)
        {
            string engineShaderSourcePath = HlslHelper.EngineShadersDirectory;
            string engineShaderSourceIncludePath = HlslHelper.EngineIncludesDirectory;

            ShaderInfo[] engineShaderFiles =
            [
                new ((int)EngineShaders.FullScreenTriangleVs, new (Path.Combine(engineShaderSourcePath, "FullScreenTriangle.hlsl"), "FullScreenTriangleVS", ShaderTypes.Vertex)),
                new ((int)EngineShaders.PostProcessPs, new (Path.Combine(engineShaderSourcePath, "PostProcess.hlsl"), "PostProcessPS", ShaderTypes.Pixel)),
                new ((int)EngineShaders.GridFrustumsCs, new (Path.Combine(engineShaderSourcePath, "GridFrustums.hlsl"), "ComputeGridFrustumsCS", ShaderTypes.Compute), ["-D", "TILE_SIZE=32"]),
                new ((int)EngineShaders.LightCullingCs, new (Path.Combine(engineShaderSourcePath, "CullLights.hlsl"), "CullLightsCS", ShaderTypes.Compute), ["-D", "TILE_SIZE=32"]),
            ];

            return CompileShaders(engineShaderFiles, engineShaderSourceIncludePath, outputPath);
        }
        public static bool CompileShaders(ShaderInfo[] engineShaderFiles, string shadersIncludeDir, string shadersOutputPath, IEnumerable<string> extraArgs = null)
        {
            shadersIncludeDir = Path.GetFullPath(shadersIncludeDir);
            if (!Directory.Exists(shadersIncludeDir))
            {
                Debug.WriteLine($"Shader Compilation: Includes folder not exists: {shadersIncludeDir}");
                return false;
            }

            shadersOutputPath = Path.GetFullPath(shadersOutputPath);
            string shadersOutputDir = Path.GetDirectoryName(shadersOutputPath);
            if (!Directory.Exists(shadersOutputDir))
            {
                Debug.WriteLine($"Shader Compilation: Output folder not exists: {shadersOutputDir}");
                return false;
            }

            if (CompiledShadersAreUpToDate(engineShaderFiles, shadersIncludeDir, shadersOutputPath))
            {
                Debug.WriteLine("Shader Compilation: [ Up to Date ]");
#if !DEBUG
                return true;
#endif
            }

            var shaders = new List<CompiledShader>();

            foreach (var file in engineShaderFiles)
            {
                string[] fileArgs = [.. file.ExtraArguments ?? [], .. extraArgs ?? []];

                if (!CompileInternal(file.Info, shadersIncludeDir, fileArgs, out var compiledShader))
                {
                    return false;
                }

                shaders.Add(compiledShader);
            }

            bool saved = SaveCompiledShaders(shaders, shadersOutputPath);

            Debug.WriteLine(saved ? "Shader Compilation: Successfully" : "Shader Compilation: Failed");

            return saved;
        }
        static bool CompiledShadersAreUpToDate(ShaderInfo[] engineShaderFiles, string shadersIncludeDir, string shadersOutputPath)
        {
            var shadersCompilationTime = File.GetLastWriteTime(shadersOutputPath);

            foreach (var entry in engineShaderFiles)
            {
                if (File.GetLastWriteTime(entry.Info.FileName) > shadersCompilationTime)
                {
                    return false;
                }
            }

            foreach (var entry in Directory.GetFiles(shadersIncludeDir))
            {
                if (File.GetLastWriteTime(entry) > shadersCompilationTime)
                {
                    return false;
                }
            }

            return true;
        }

        public static bool CompileShader(ShaderFileInfo info, string shadersIncludeDir, IEnumerable<string> extraArgs, out CompiledShader compiledShader)
        {
            bool result = CompileInternal(info, shadersIncludeDir, extraArgs, out var dxcCompiledShader);
            compiledShader = result ? dxcCompiledShader : default;
            return result;
        }

        static bool CompileInternal(ShaderFileInfo info, string shadersIncludeDir, IEnumerable<string> extraArgs, out CompiledShader compiledShader)
        {
            string shadersSourcePath = Path.GetFullPath(info.FileName);
            if (!File.Exists(shadersSourcePath))
            {
                Debug.WriteLine($"Shader Compilation: ERROR - Source not found: {shadersSourcePath}");
                compiledShader = default;
                return false;
            }

            if (DxcCompiler.Utils.CreateDefaultIncludeHandler(out var includeHandler).Failure)
            {
                Debug.WriteLine("Shader Compilation: ERROR - Failed to create DXC include handler instance");
                compiledShader = default;
                return false;
            }

            using (includeHandler)
            {
                Debug.WriteLine($"Shader Compilation: {info.Profile} - {info.EntryPoint} {info.FileName}");

                string shaderSource = File.ReadAllText(shadersSourcePath);
                var args = GetArgs(shadersIncludeDir, info, extraArgs);

                using var results = DxcCompiler.Compile(shaderSource, args, includeHandler);
                if (results.GetStatus().Failure)
                {
                    Debug.WriteLine(results.GetErrors());
                    compiledShader = default;
                    return false;
                }

                var byteCode = results.GetObjectBytecodeArray();
                ShaderHash shaderHash;
                byte[] disassembly;

                if (results.HasOutput(DxcOutKind.ShaderHash))
                {
                    using var hashBlob = results.GetOutput(DxcOutKind.ShaderHash);
                    shaderHash = Marshal.PtrToStructure<ShaderHash>(hashBlob.BufferPointer);
                    Debug.WriteLine($"Shader Compilation: HashDigest [{shaderHash.GetHashDigestString()}]");
                }
                else
                {
                    Debug.WriteLine("Shader Compilation: ERROR - Shader hash not available in compilation results.");
                    compiledShader = default;
                    return false;
                }

                DxcBuffer buffer = new()
                {
                    Ptr = Marshal.UnsafeAddrOfPinnedArrayElement(byteCode, 0),
                    Size = (nuint)byteCode.Length,
                    Encoding = Dxc.DXC_CP_ACP
                };
                if (DxcCompiler.Compiler.Disassemble(buffer, out IDxcResult dres).Success)
                {
                    disassembly = dres.GetOutput(DxcOutKind.Disassembly).AsBytes();
                }
                else
                {
                    Debug.WriteLine("Shader Compilation: WARNING - Disassembly not available in compilation results.");
                    disassembly = [];
                }

                compiledShader = new(byteCode, shaderHash, disassembly);
            }

            return true;
        }
        static string[] GetArgs(string shadersSourcePath, ShaderFileInfo info, IEnumerable<string> extraArgs = null)
        {
            var args = new List<string>
            {
                info.FileName,
                "-E", info.EntryPoint,
                "-T", info.Profile ?? GetShaderProfile(info.Stage),
                "-I", shadersSourcePath,
                "-enable-16bit-types",
                Dxc.DXC_ARG_ALL_RESOURCES_BOUND,
#if DEBUG
                Dxc.DXC_ARG_DEBUG,
                Dxc.DXC_ARG_SKIP_OPTIMIZATIONS,
#else
                Dxc.DXC_ARG_OPTIMIZATION_LEVEL3,
#endif
                Dxc.DXC_ARG_WARNINGS_ARE_ERRORS,
                "-Qstrip_reflect",
                "-Qstrip_debug",
            };

            args.AddRange(extraArgs ?? []);

            return [.. args];
        }
        static bool SaveCompiledShaders(IEnumerable<CompiledShader> shaders, string shadersOutputPath)
        {
            var shadersOutputDir = Path.GetDirectoryName(shadersOutputPath);
            if (!Directory.Exists(shadersOutputDir))
            {
                Directory.CreateDirectory(shadersOutputDir);
            }

            using (var file = new BinaryWriter(File.Open(shadersOutputPath, FileMode.Create)))
            {
                foreach (var shader in shaders)
                {
                    var byteCode = shader.ByteCode;
                    file.Write(byteCode.Length);
                    file.Write(shader.Hash.HashDigest);
                    file.Write(byteCode.Span);
                }
            }

            return File.Exists(shadersOutputPath);
        }
    }
}
