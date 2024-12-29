using ContentTools;
using System;
using System.IO;

namespace AssetsImporter
{
    internal class Program
    {
        private const string modelM24Path = "../../../../../Assets/M24.dae";
        private const string modelHumveePath = "../../../../../Assets/Humvee.obj";
        private const string modelToyTankPath = "../../../../../Assets/ToyTank.fbx";
        private const string outputPath = "./Assets/";

        static void Main()
        {
            ImportModel(modelM24Path);
            ImportModel(modelHumveePath);
            ImportModel(modelToyTankPath);
            Console.ReadKey();
        }

        private static void ImportModel(string modelPath)
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

            string output = Path.GetFullPath(Path.Combine(outputPath, Path.GetFileNameWithoutExtension(path) + ".asset"));
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
        }
    }
}
