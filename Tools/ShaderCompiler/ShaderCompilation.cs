using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Vortice.Dxc;

namespace ShaderCompiler
{
    public static class ShaderCompilation
    {
        class ShaderCompiler
        {
            private readonly IDxcCompiler3 compiler;
            private readonly IDxcUtils utils;
            private readonly IDxcIncludeHandler includeHandler;

            public ShaderCompiler()
            {
                if (!Dxc.DxcCreateInstance(Dxc.CLSID_DxcCompiler, out compiler).Success)
                {
                    Console.WriteLine("Failed to create DXC compiler instance");
                    return;
                }
                if (!Dxc.DxcCreateInstance(Dxc.CLSID_DxcUtils, out utils).Success)
                {
                    Console.WriteLine("Failed to create DXC utils instance");
                    return;
                }
                if (!utils.CreateDefaultIncludeHandler(out includeHandler).Success)
                {
                    Console.WriteLine("Failed to create DXC include handler instance");
                    return;
                }
            }

            public DxcCompiledShader Compile(string shadersSourcePath, ShaderFileInfo info, string fullPath)
            {
                Debug.Assert(compiler != null && utils != null && includeHandler != null);

                Console.WriteLine($"[{info.Type}] -> {info.FileName} {info.Function}");

                string source = File.ReadAllText(fullPath);
                var args = GetArgs(shadersSourcePath, info, []);
                if (compiler.Compile(source, args, includeHandler, out IDxcResult results).Failure)
                {
                    var errors = results.GetErrors();
                    if (!string.IsNullOrEmpty(errors))
                    {
                        Console.WriteLine($"Shader compilation error: {errors}");
                    }

                    return null;
                }

                Console.WriteLine(" [ Succeeded ]");

                if (results.GetOutput(DxcOutKind.ShaderHash, out IDxcBlob hashBlob).Failure)
                {
                    return null;
                }

                var shaderHash = Marshal.PtrToStructure<DxcShaderHash>(hashBlob.BufferPointer);
                // different source code could result in the same byte code, so we only care about byte code hash.
                Debug.Assert((shaderHash.Flags & Dxc.DXC_HASHFLAG_INCLUDES_SOURCE) == 0);
                Console.WriteLine($"Shader hash: {shaderHash.GetHashDigestString()}");

                Console.WriteLine("Disassembling...");
                byte[] disassemblyBytes = null;
                if (compiler.Disassemble(source, out IDxcResult disasmResults).Success)
                {
                    if (disasmResults.GetOutput(DxcOutKind.Disassembly, out IDxcBlobUtf8 disassembly).Success)
                    {
                        disassemblyBytes = disassembly.AsBytes();
                    }
                }

                return new DxcCompiledShader(
                    results.GetObjectBytecodeArray(),
                    disassemblyBytes,
                    shaderHash);
            }
            private static string[] GetArgs(string shadersSourcePath, ShaderFileInfo info, IEnumerable<string> extraArgs)
            {
                var args = new List<string>
                {
                    info.FileName,
                    "-E", info.Function,
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
                    "-Qstrip_reflect",
                    "-Qstrip_debug"
                };

                args.AddRange(extraArgs ?? []);

                return [.. args];
            }
        }

        public static bool CompileShaders(string shadersSourcePath, EngineShaderInfo[] engineShaderFiles, string outputFileName)
        {
            if (CompiledShadersAreUpToDate(shadersSourcePath, outputFileName))
            {
                Console.WriteLine(" [ Up to Date ]");

                return true;
            }

            var shaders = new List<DxcCompiledShader>();

            foreach (var file in engineShaderFiles)
            {
                var fullPath = Path.Combine(shadersSourcePath, file.Info.FileName);
                fullPath = Path.GetFullPath(fullPath);
                if (!File.Exists(fullPath)) return false;

                var compiledShader = Compile(shadersSourcePath, file.Info, fullPath);
                if (compiledShader.ByteCode != null && compiledShader.ByteCode.Length > 0)
                {
                    shaders.Add(compiledShader);
                }
                else
                {
                    return false;
                }
            }

            return SaveCompiledShaders(shaders, outputFileName);
        }

        private static bool CompiledShadersAreUpToDate(string shadersSourcePath, string outputFileName)
        {
            var engineShadersPath = GetEngineShadersPath(outputFileName);
            if (!File.Exists(engineShadersPath))
            {
                return false;
            }

            var shadersCompilationTime = File.GetLastWriteTime(engineShadersPath);

            foreach (var entry in Directory.GetFiles(shadersSourcePath))
            {
                if (File.GetLastWriteTime(entry) > shadersCompilationTime)
                {
                    return false;
                }
            }

            return true;
        }
        private static string GetEngineShadersPath(string outputFileName)
        {
            return Path.GetFullPath(outputFileName);
        }
        private static DxcCompiledShader Compile(string shadersSourcePath, ShaderFileInfo info, string fullPath)
        {
            var compiler = new ShaderCompiler();

            return compiler.Compile(shadersSourcePath, info, fullPath);
        }
        private static bool SaveCompiledShaders(IEnumerable<DxcCompiledShader> shaders, string outputFileName)
        {
            var engineShadersPath = GetEngineShadersPath(outputFileName);
            var engineShadersDir = Path.GetDirectoryName(engineShadersPath);
            if (!Directory.Exists(engineShadersDir))
            {
                Directory.CreateDirectory(engineShadersDir);
            }

            using (var file = new BinaryWriter(File.Open(engineShadersPath, FileMode.Create)))
            {
                foreach (var shader in shaders)
                {
                    var byteCode = shader.ByteCode;
                    file.Write(byteCode.Length);
                    file.Write(shader.Hash.HashDigest);
                    file.Write(byteCode);
                }
            }

            return File.Exists(engineShadersPath);
        }
    }
}
