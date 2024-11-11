using NUnit.Framework;
using System.IO;
using System.Text;

namespace EngineTests.Content
{
    public class ContentLoaderTests
    {
        const string path = "game.bin";

        const int numEntities = 10;
        const int numComponents = 2;

        // Test preparation
        [OneTimeSetUp]
        public void Setup()
        {
            // Create the file
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            using FileStream fileStream = new(path, FileMode.CreateNew, FileAccess.Write);
            using BinaryWriter writer = new(fileStream, Encoding.UTF8, false);

            writer.Write(numEntities);
            for (int i = 0; i < numEntities; i++)
            {
                writer.Write(0); // Reserved for future use
                writer.Write(numComponents); // Number of components

                writer.Write(0); // Component type transform
                int transformComponentSize = sizeof(float) * 9;
                writer.Write(transformComponentSize);
                writer.Write(0f); writer.Write(1f); writer.Write(2f);
                writer.Write(3f); writer.Write(4f); writer.Write(5f);
                writer.Write(6f); writer.Write(7f); writer.Write(8f);

                writer.Write(1); // Component type script
                byte[] stringData = Encoding.UTF8.GetBytes($"script{i}");
                int scriptComponentSize = stringData.Length;
                writer.Write(scriptComponentSize);
                writer.Write(stringData);
            }
        }

        [Test()]
        public void LoadContentTest()
        {
            bool result = Engine.Core.Engine.EngineInitialize(path, null);

            Assert.That(result);
        }
    }
}
