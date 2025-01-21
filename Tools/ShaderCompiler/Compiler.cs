using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Vortice.Dxc;

namespace ShaderCompiler
{
    public static class Compiler
    {
        public static DxcShaderModel DefaultShaderModel { get; set; } = DxcShaderModel.Model6_6;

        public static bool CompileShaders(EngineShaderInfo[] engineShaderFiles, string shadersIncludeDir, string shadersOutputPath, IEnumerable<string> extraArgs = null)
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
                if (!Compile(file.Info, shadersIncludeDir, extraArgs, out var compiledShader))
                {
                    return false;
                }

                shaders.Add(compiledShader);
            }

            bool saved = SaveCompiledShaders(shaders, shadersOutputPath);

            Debug.WriteLine(saved ? "Shader Compilation: Successfully" : "Shader Compilation: Failed");

            return saved;
        }
        private static bool CompiledShadersAreUpToDate(EngineShaderInfo[] engineShaderFiles, string shadersIncludeDir, string shadersOutputPath)
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

        public static bool Compile(ShaderFileInfo info, string shadersIncludeDir, out CompiledShader compiledShader)
        {
            return Compile(info, shadersIncludeDir, [], out compiledShader);
        }
        public static bool Compile(ShaderFileInfo info, string shadersIncludeDir, IEnumerable<string> extraArgs, out CompiledShader compiledShader)
        {
            string shadersSourcePath = Path.GetFullPath(info.FileName);
            if (!File.Exists(shadersSourcePath))
            {
                Debug.WriteLine($"Shader Compilation: Source not found: {shadersSourcePath}");
                compiledShader = default;
                return false;
            }

            string shaderSource = File.ReadAllText(shadersSourcePath);
            var args = GetArgs(shadersIncludeDir, info, DefaultShaderModel, extraArgs);

            if (!DxcCompiler.Utils.CreateDefaultIncludeHandler(out var includeHandler).Success)
            {
                Debug.WriteLine("Shader Compilation: Failed to create DXC include handler instance");
                compiledShader = default;
                return false;
            }

            using (includeHandler)
            {
                Debug.WriteLine($"Shader Compilation: {info.Stage} {info.Profile} - {info.EntryPoint} {info.FileName}");

                using IDxcResult results = DxcCompiler.Compiler.Compile(shaderSource, args, includeHandler);
                if (results.GetStatus().Failure)
                {
                    Debug.WriteLine(results.GetErrors());
                    compiledShader = default;
                    return false;
                }

                byte[] disassembly = null;
                if (results.GetOutput(DxcOutKind.Disassembly, out IDxcBlob disassemblyBlob).Success)
                {
                    disassembly = disassemblyBlob.AsBytes();
                }
                else
                {
                    Debug.WriteLine("Shader Compilation: Failed to get disassembly");
                }

                if (results.GetOutput(DxcOutKind.ShaderHash, out IDxcBlob hashBlob).Failure)
                {
                    Debug.WriteLine("Shader Compilation: Failed to get shader hash");
                    compiledShader = default;
                    return false;
                }
                var shaderHash = Marshal.PtrToStructure<ShaderHash>(hashBlob.BufferPointer);
                Debug.WriteLine($"Shader Compilation: HashDigest [{shaderHash.GetHashDigestString()}]");

                compiledShader = new(results.GetObjectBytecodeMemory(), shaderHash, disassembly);
            }

            return true;
        }
        private static string[] GetArgs(string shadersSourcePath, ShaderFileInfo info, DxcShaderModel shaderModel, IEnumerable<string> extraArgs = null)
        {
            var args = new List<string>
            {
                info.FileName,
                "-E", info.EntryPoint,
                "-T", info.Profile,
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
                "-HV",
                "2021",
                "-Qstrip_reflect",
                "-Qstrip_debug",
            };

            args.AddRange(extraArgs ?? []);

            return [.. args];
        }
        private static bool SaveCompiledShaders(IEnumerable<CompiledShader> shaders, string shadersOutputPath)
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
