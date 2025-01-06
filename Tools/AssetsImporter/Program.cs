using ContentTools;
using System;
using System.IO;

namespace AssetsImporter
{
    internal class Program
    {
        private const string sceneName = "Test Scene";
        private const string modelM24Path = "../../../../../Assets/M24.dae";
        private const string modelHumveePath = "../../../../../Assets/Humvee.obj";
        private const string modelToyTankPath = "../../../../../Assets/ToyTank.fbx";
        private const string outputPath = "./Assets/Scene.asset";

        static void Main()
        {
            SceneData sceneData = new(sceneName);

            AddModel(modelM24Path, sceneData);
            AddModel(modelHumveePath, sceneData);
            AddModel(modelToyTankPath, sceneData);
            ExportAssetsFile(outputPath, sceneData);
            Console.ReadKey();
        }

        private static void AddModel(string modelPath, SceneData sceneData)
        {
            string path = Path.GetFullPath(modelPath);
            if (!File.Exists(path))
            {
                Console.WriteLine("Asset file not found");
                Console.ReadKey();
                return;
            }

            AssimpImporter.Add(path, sceneData);
        }
        private static void ExportAssetsFile(string assetFilename, SceneData sceneData)
        {
            AssimpImporter.Import(sceneData);

            string output = Path.GetFullPath(assetFilename);
            if (File.Exists(output))
            {
                File.Delete(output);
            }
            string outputDir = Path.GetDirectoryName(output);
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }
            sceneData.SaveToFile(output);

            Console.WriteLine(sceneData.BufferSize > 0 ? $"Asset imported successfully. {sceneData.BufferSize} bytes" : "Asset import failed");
        }
    }
}
