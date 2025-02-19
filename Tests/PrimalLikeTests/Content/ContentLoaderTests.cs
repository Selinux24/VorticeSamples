using NUnit.Framework;
using PrimalLike;
using PrimalLike.Common;
using PrimalLike.Content;
using PrimalLike.EngineAPI;
using System.IO;
using System.Text;

namespace PrimalLikeTests.Content
{
    public class ContentLoaderTests
    {
        class TestScript : EntityScript
        {
            public TestScript() : base()
            {
            }
            public TestScript(Entity entity) : base(entity)
            {
            }

            public override void Update(float deltaTime)
            {
            }
        }

        private static string CreateWithScripts()
        {
            const string path = "game.bin";

            // Create the file
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            const int numEntities = 10;
            const int numComponents = 2;

            string testScriptTag = IdDetail.StringHash<TestScript>();
            byte[] stringData = Encoding.UTF8.GetBytes(testScriptTag);

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
                int scriptComponentSize = stringData.Length;
                writer.Write(scriptComponentSize);
                writer.Write(stringData);
            }

            return path;
        }
        private static string CreateWithNoScripts()
        {
            const string path = "gameNoScripts.bin";

            // Create the file
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            const int numEntities = 10;
            const int numComponents = 1;

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
            }

            return path;
        }

        [Test()]
        public void LoadContentTest()
        {
            string path = CreateWithScripts();

            //Register the script creator
            Application.RegisterScript<TestScript>();

            bool result = ContentLoader.LoadGame(path);

            Assert.That(result);
        }
        [Test()]
        public void LoadContentNoScriptTest()
        {
            string path = CreateWithNoScripts();

            bool result = ContentLoader.LoadGame(path);

            Assert.That(result);
        }
    }
}
