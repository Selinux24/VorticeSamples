using AssetsImporter;
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
using Vortice.Mathematics;

namespace DX12Windows.Content
{
    class M24RenderItem : ITestRenderItem
    {
        private const string modelM24 = "../../../../../Assets/m24.dae";

        private const string model1Name = "m24_1_model.model";
        private const string model2Name = "m24_2_model.model";
        private const string model3Name = "m24_3_model.model";
        private const string model4Name = "m24_4_model.model";
        private const string model5Name = "m24_5_model.model";
        private const string model6Name = "m24_6_model.model";

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

            CreateRenderItems(outputsFolder);
        }
        private void CreateRenderItems(string outputsFolder)
        {
            var _1 = new Thread(() => { model1Id = ITestRenderItem.LoadModel(Path.Combine(outputsFolder, model1Name)); });
            var _2 = new Thread(() => { model2Id = ITestRenderItem.LoadModel(Path.Combine(outputsFolder, model2Name)); });
            var _3 = new Thread(() => { model3Id = ITestRenderItem.LoadModel(Path.Combine(outputsFolder, model3Name)); });
            var _4 = new Thread(() => { model4Id = ITestRenderItem.LoadModel(Path.Combine(outputsFolder, model4Name)); });
            var _5 = new Thread(() => { model5Id = ITestRenderItem.LoadModel(Path.Combine(outputsFolder, model5Name)); });
            var _6 = new Thread(() => { model6Id = ITestRenderItem.LoadModel(Path.Combine(outputsFolder, model6Name)); });
            var _7 = new Thread(TestShaders.LoadShaders);

            _1.Start();
            _2.Start();
            _3.Start();
            _4.Start();
            _5.Start();
            _6.Start();
            _7.Start();

            _1.Join();
            _2.Join();
            _3.Join();
            _4.Join();
            _5.Join();
            _6.Join();
            _7.Join();

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
            info.ShaderIds[(uint)ShaderTypes.Pixel] = TestShaders.PsId;
            info.Type = MaterialTypes.Opaque;
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
