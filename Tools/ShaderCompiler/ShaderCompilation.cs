using System;
using System.Collections.Generic;
using System.IO;
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

            public DxcCompiledShader Compile(string shadersSourcePath, ShaderFileInfo info, string source, string[] extraArgs)
            {
                Console.WriteLine($"[{info.Type}] -> {info.FileName} {info.Function}");

                var args = GetArgs(shadersSourcePath, info, extraArgs);
                var results = compiler.Compile(source, args, includeHandler);
                var errors = results.GetErrors();
                if (!string.IsNullOrEmpty(errors))
                {
                    Console.WriteLine($"Shader compilation error: {errors}");
                }
                else
                {
                    Console.WriteLine(" [ Succeeded ]");
                }

                var status = results.GetStatus();
                if (status != 0)
                {
                    return null;
                }

                var shaderBytes = results.GetObjectBytecode().ToArray();

                return new DxcCompiledShader(shaderBytes);
            }
            private static string[] GetArgs(string shadersSourcePath, ShaderFileInfo info, IEnumerable<string> extraArgs)
            {
                var args = new List<string>
                {
                    info.FileName,
                    "-E", info.Function,
                    "-T", ProfileStrings[(int)info.Type],
                    "-I", shadersSourcePath,
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

        private static readonly EngineShaderInfo[] EngineShaderFiles =
        [
            new (EngineShader.FullscreenTriangleVs, new ShaderFileInfo("FullScreenTriangle.hlsl", "FullScreenTriangleVS", ShaderType.Vertex)),
            new (EngineShader.FillColorPs, new ShaderFileInfo("FillColor.hlsl", "FillColorPS", ShaderType.Pixel)),
        ];

        private static readonly string[] ProfileStrings = ["vs_6_6", "hs_6_6", "ds_6_6", "gs_6_6", "ps_6_6", "cs_6_6", "as_6_6", "ms_6_6"];

        public static bool CompileShaders(string shadersSourcePath)
        {
            if (CompiledShadersAreUpToDate(shadersSourcePath))
            {
                Console.WriteLine(" [ Up to Date ]");

                return true;
            }

            var shaders = new List<DxcCompiledShader>();

            foreach (var file in EngineShaderFiles)
            {
                var fullPath = Path.Combine(shadersSourcePath, file.Info.FileName);
                fullPath = Path.GetFullPath(fullPath);
                if (!File.Exists(fullPath)) return false;
                string[] extraArgs = [];

                var compiledShader = Compile(shadersSourcePath, file.Info, fullPath, extraArgs);
                if (compiledShader.ByteCode != null && compiledShader.ByteCode.Length > 0)
                {
                    shaders.Add(compiledShader);
                }
                else
                {
                    return false;
                }
            }

            return SaveCompiledShaders(shaders);
        }

        private static bool CompiledShadersAreUpToDate(string shadersSourcePath)
        {
            var engineShadersPath = GetEngineShadersPath();
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
        private static string GetEngineShadersPath()
        {
            return Path.GetFullPath("./OutputShaders/Shaders.bin");
        }
        private static DxcCompiledShader Compile(string shadersSourcePath, ShaderFileInfo info, string fullPath, string[] extraArgs)
        {
            var compiler = new ShaderCompiler();

            return compiler.Compile(shadersSourcePath, info, File.ReadAllText(fullPath), extraArgs);
        }
        private static bool SaveCompiledShaders(IEnumerable<DxcCompiledShader> shaders)
        {
            var engineShadersPath = GetEngineShadersPath();
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
                    file.Write(byteCode);
                }
            }

            return File.Exists(engineShadersPath);
        }
    }
}
