using AssetsImporter;
using DX12Windows.Assets;
using DX12Windows.Shaders;
using PrimalLike.Common;
using PrimalLike.Components;
using PrimalLike.Content;
using PrimalLike.Graphics;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;
using TexturesImporter;
using Vortice.Mathematics;

namespace DX12Windows.Content
{
    class M24RenderItem : ITestRenderItem
    {
        private const string modelM24 = "../../../../../Assets/m24.dae";

        private const string baseColorTexture = "../../../../../Assets/m24.dds";

        private const string model1Name = "m24_1_model.model";
        private const string model2Name = "m24_2_model.model";
        private const string model3Name = "m24_3_model.model";
        private const string model4Name = "m24_4_model.model";
        private const string model5Name = "m24_5_model.model";
        private const string model6Name = "m24_6_model.model";

        private const string baseColorTextureName = "m24.texture";

        private uint model1Id = uint.MaxValue;
        private uint model2Id = uint.MaxValue;
        private uint model3Id = uint.MaxValue;
        private uint model4Id = uint.MaxValue;
        private uint model5Id = uint.MaxValue;
        private uint model6Id = uint.MaxValue;

        private uint entity1Id = uint.MaxValue;
        private uint entity2Id = uint.MaxValue;
        private uint entity3Id = uint.MaxValue;
        private uint entity4Id = uint.MaxValue;
        private uint entity5Id = uint.MaxValue;
        private uint entity6Id = uint.MaxValue;

        private readonly uint[] textureIds = new uint[(int)TestShaders.TextureUsages.Count];

        private uint mtlId = uint.MaxValue;

        public Vector3 InitialCameraPosition { get; } = new(0, 0.2f * 30f, -3f * 30f);
        public Quaternion InitialCameraRotation { get; } = Quaternion.CreateFromYawPitchRoll(3.14f, 3.14f, 0);

        public void Load(string assetsFolder, string outputsFolder)
        {
            string[] modelNames =
            [
                Path.Combine(outputsFolder, model1Name),
                Path.Combine(outputsFolder, model2Name),
                Path.Combine(outputsFolder, model3Name),
                Path.Combine(outputsFolder, model4Name),
                Path.Combine(outputsFolder, model5Name),
                Path.Combine(outputsFolder, model6Name),
            ];

            if (modelNames.Any(f => !File.Exists(f)))
            {
                string[] assets = [.. AssimpImporter.Read(modelM24, new(), assetsFolder)];

                Debug.Assert(assets.Length == modelNames.Length);
                for (int i = 0; i < assets.Length; i++)
                {
                    if (string.IsNullOrEmpty(assets[i]))
                    {
                        continue;
                    }

                    AssimpImporter.PackForEngine(assets[i], modelNames[i]);
                }
            }

            Importer.ImportBaseColorTexture(baseColorTexture, outputsFolder, baseColorTextureName);

            TextureImporter.ShutDownTextureTools();

            CreateRenderItems(outputsFolder);
        }
        private void CreateRenderItems(string outputsFolder)
        {
            Thread[] tasks =
            [
                new(() => { textureIds[(uint)TestShaders.TextureUsages.BaseColor] = ITestRenderItem.LoadTexture(Path.Combine(outputsFolder, baseColorTextureName)); }),

                new(() => { model1Id = ITestRenderItem.LoadModel(Path.Combine(outputsFolder, model1Name)); }),
                new(() => { model2Id = ITestRenderItem.LoadModel(Path.Combine(outputsFolder, model2Name)); }),
                new(() => { model3Id = ITestRenderItem.LoadModel(Path.Combine(outputsFolder, model3Name)); }),
                new(() => { model4Id = ITestRenderItem.LoadModel(Path.Combine(outputsFolder, model4Name)); }),
                new(() => { model5Id = ITestRenderItem.LoadModel(Path.Combine(outputsFolder, model5Name)); }),
                new(() => { model6Id = ITestRenderItem.LoadModel(Path.Combine(outputsFolder, model6Name)); }),

                new(TestShaders.LoadShaders),
            ];

            foreach (var t in tasks)
            {
                t.Start();
            }

            foreach (var t in tasks)
            {
                t.Join();
            }

            GeometryInfo geometryInfo = new();

            // NOTE: we need shaders to be ready before creating materials
            CreateMaterial();

            geometryInfo.MaterialIds = [mtlId];
            var rotation = Quaternion.CreateFromYawPitchRoll(MathHelper.PiOver4 * 3, -MathHelper.PiOver2, 0f);

            geometryInfo.GeometryContentId = model1Id;
            entity1Id = HelloWorldApp.CreateOneGameEntity(Vector3.Zero, rotation, geometryInfo).Id;
            geometryInfo.GeometryContentId = model2Id;
            entity2Id = HelloWorldApp.CreateOneGameEntity(Vector3.Zero, rotation, geometryInfo).Id;
            geometryInfo.GeometryContentId = model3Id;
            entity3Id = HelloWorldApp.CreateOneGameEntity(Vector3.Zero, rotation, geometryInfo).Id;
            geometryInfo.GeometryContentId = model4Id;
            entity4Id = HelloWorldApp.CreateOneGameEntity(Vector3.Zero, rotation, geometryInfo).Id;
            geometryInfo.GeometryContentId = model5Id;
            entity5Id = HelloWorldApp.CreateOneGameEntity(Vector3.Zero, rotation, geometryInfo).Id;
            geometryInfo.GeometryContentId = model6Id;
            entity6Id = HelloWorldApp.CreateOneGameEntity(Vector3.Zero, rotation, geometryInfo).Id;
        }
        private void CreateMaterial()
        {
            Debug.Assert(IdDetail.IsValid(TestShaders.VsId) && IdDetail.IsValid(TestShaders.PsId));

            MaterialInitInfo info = new();
            info.ShaderIds[(uint)ShaderTypes.Vertex] = TestShaders.VsId;
            info.ShaderIds[(uint)ShaderTypes.Pixel] = TestShaders.TexturedPsId;
            info.Type = MaterialTypes.Opaque;
            info.TextureCount = (int)TestShaders.TextureUsages.Count;
            info.TextureIds = textureIds;

            mtlId = ContentToEngine.CreateResource(info, AssetTypes.Material);
        }

        public void DestroyRenderItems()
        {
            HelloWorldApp.RemoveGameEntity(entity1Id);
            HelloWorldApp.RemoveGameEntity(entity2Id);
            HelloWorldApp.RemoveGameEntity(entity3Id);
            HelloWorldApp.RemoveGameEntity(entity4Id);
            HelloWorldApp.RemoveGameEntity(entity5Id);
            HelloWorldApp.RemoveGameEntity(entity6Id);

            ITestRenderItem.RemoveModel(model1Id);
            ITestRenderItem.RemoveModel(model2Id);
            ITestRenderItem.RemoveModel(model3Id);
            ITestRenderItem.RemoveModel(model4Id);
            ITestRenderItem.RemoveModel(model5Id);
            ITestRenderItem.RemoveModel(model6Id);

            // remove material
            if (IdDetail.IsValid(mtlId))
            {
                ContentToEngine.DestroyResource(mtlId, AssetTypes.Material);
            }

            // remove shaders and textures
            TestShaders.RemoveShaders();
        }
    }
}
