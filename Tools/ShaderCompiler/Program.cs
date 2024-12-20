
using System;

namespace ShaderCompiler
{
    internal class Program
    {
        private const string ShadersSourcePath = "../../../../../Libs/Direct3D12/Shaders/";

        static void Main()
        {
            bool res = ShaderCompilation.CompileShaders(ShadersSourcePath);

            Console.WriteLine(res ? "Shaders compiled successfully" : "Shaders compilation failed");
            Console.ReadKey();
        }
    }
}
