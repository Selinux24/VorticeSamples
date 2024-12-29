using ContentTools;
using System;
using System.IO;

namespace AssetsImporter
{
    internal class Program
    {
        private const string modelPath = "../../../../../Assets/ToyTank.fbx";
        private const string outputPath = "./Assets/ToyTank.asset";

        static void Main()
        {
            string path = Path.GetFullPath(modelPath);
            if (!File.Exists(path))
            {
                Console.WriteLine("Asset file not found");
                Console.ReadKey();
                return;
            }

            SceneData sceneData = new();
            AssimpImporter.Import(path, sceneData);

            string output = Path.GetFullPath(outputPath);
            if (File.Exists(output))
            {
                File.Delete(output);
            }
            string outputDir = Path.GetDirectoryName(output);
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }
            File.WriteAllBytes(output, sceneData.Buffer);

            Console.WriteLine(sceneData.BufferSize > 0 ? $"Asset imported successfully. {sceneData.BufferSize} bytes" : "Asset import failed");
            Console.ReadKey();
        }
    }
}
