using ContentTools;
using System;
using System.Collections.Generic;
using System.IO;

namespace AssetsImporter
{
    internal class Program
    {
        private const string modelM24Path = "../../../../../Assets/M24.dae";
        private const string modelHumveePath = "../../../../../Assets/Humvee.obj";
        private const string modelToyTankPath = "../../../../../Assets/ToyTank.fbx";
        private const string modelLabScenePath = "../../../../../Assets/LabScene.fbx";
        private const string assetsFolder = "./Assets";
        private const string modelsFolder = "./Models";

        static void Main()
        {
            GeometryImportSettings settings = new();

            List<string> files = [];

            files.AddRange(ImportModel(modelM24Path, settings, assetsFolder));
            files.AddRange(ImportModel(modelHumveePath, settings, assetsFolder));
            files.AddRange(ImportModel(modelToyTankPath, settings, assetsFolder));
            files.AddRange(ImportModel(modelLabScenePath, settings, assetsFolder));

            foreach (string assetFilename in files)
            {
                if (string.IsNullOrEmpty(assetFilename))
                {
                    continue;
                }

                ExportAssetsFile(assetFilename);
            }
            Console.ReadKey();
        }

        private static string[] ImportModel(string modelPath, GeometryImportSettings settings, string assetsFolder)
        {
            string path = Path.GetFullPath(modelPath);
            if (!File.Exists(path))
            {
                Console.WriteLine("Asset file not found");
                Console.ReadKey();
                return null;
            }

            return AssimpImporter.Read(path, settings, assetsFolder);
        }
        private static void ExportAssetsFile(string assetFilename)
        {
            string contentFilename = Path.Combine(modelsFolder, Path.ChangeExtension(Path.GetFileName(assetFilename), ".model"));

            AssimpImporter.PackForEngine(assetFilename, contentFilename);
        }
    }
}
