using System;

namespace TexturesImporter
{
    internal class Program
    {
        private const string textureM24Path = "../../../../../Assets/M24.dds";

        static void Main()
        {
            TextureData textureData = new();
            textureData.ImportSettings.Sources = textureM24Path;
            textureData.ImportSettings.Compress = true;

            TextureImporter.Import(ref textureData);
            TextureImporter.DecompressMipmaps(ref textureData);

            Console.ReadKey();
        }
    }
}
