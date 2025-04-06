using AssetsImporter;
using DX12Windows.Shaders;
using PrimalLike;
using PrimalLike.Common;
using PrimalLike.Content;
using PrimalLike.Graphics;
using System.Collections.Generic;
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

        private uint item1Id = uint.MaxValue;
        private uint item2Id = uint.MaxValue;
        private uint item3Id = uint.MaxValue;
        private uint item4Id = uint.MaxValue;
        private uint item5Id = uint.MaxValue;
        private uint item6Id = uint.MaxValue;

        private uint mtlId = uint.MaxValue;

        private readonly Dictionary<uint, uint> renderItemEntityMap = [];

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

            uint entity1Id = HelloWorldApp.CreateOneGameEntity(Vector3.Zero, new(MathHelper.PiOver4, -MathHelper.PiOver2, 0f), 1f).Id;
            uint entity2Id = HelloWorldApp.CreateOneGameEntity(Vector3.Zero, new(MathHelper.PiOver4, -MathHelper.PiOver2, 0f), 1f).Id;
            uint entity3Id = HelloWorldApp.CreateOneGameEntity(Vector3.Zero, new(MathHelper.PiOver4, -MathHelper.PiOver2, 0f), 1f).Id;
            uint entity4Id = HelloWorldApp.CreateOneGameEntity(Vector3.Zero, new(MathHelper.PiOver4, -MathHelper.PiOver2, 0f), 1f).Id;
            uint entity5Id = HelloWorldApp.CreateOneGameEntity(Vector3.Zero, new(MathHelper.PiOver4, -MathHelper.PiOver2, 0f), 1f).Id;
            uint entity6Id = HelloWorldApp.CreateOneGameEntity(Vector3.Zero, new(MathHelper.PiOver4, -MathHelper.PiOver2, 0f), 1f).Id;

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

            // NOTE: we need shaders to be ready before creating materials
            CreateMaterial();
            uint[] materials = [mtlId];

            item1Id = ContentToEngine.AddRenderItem(entity1Id, model1Id, materials);
            item2Id = ContentToEngine.AddRenderItem(entity2Id, model2Id, materials);
            item3Id = ContentToEngine.AddRenderItem(entity3Id, model3Id, materials);
            item4Id = ContentToEngine.AddRenderItem(entity4Id, model4Id, materials);
            item5Id = ContentToEngine.AddRenderItem(entity5Id, model5Id, materials);
            item6Id = ContentToEngine.AddRenderItem(entity6Id, model6Id, materials);

            renderItemEntityMap[item1Id] = entity1Id;
            renderItemEntityMap[item2Id] = entity2Id;
            renderItemEntityMap[item3Id] = entity3Id;
            renderItemEntityMap[item4Id] = entity4Id;
            renderItemEntityMap[item5Id] = entity5Id;
            renderItemEntityMap[item6Id] = entity6Id;
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
            RemoveItem(item1Id, model1Id);
            RemoveItem(item2Id, model2Id);
            RemoveItem(item3Id, model3Id);
            RemoveItem(item4Id, model4Id);
            RemoveItem(item5Id, model5Id);
            RemoveItem(item6Id, model6Id);

            // remove material
            if (IdDetail.IsValid(mtlId))
            {
                ContentToEngine.DestroyResource(mtlId, AssetTypes.Material);
            }

            // remove shaders and textures
            TestShaders.RemoveShaders();
        }
        private void RemoveItem(uint itemId, uint modelId)
        {
            if (IdDetail.IsValid(itemId))
            {
                ContentToEngine.RemoveRenderItem(itemId);

                if (renderItemEntityMap.TryGetValue(itemId, out var value))
                {
                    Application.RemoveEntity(value);
                }

                if (IdDetail.IsValid(modelId))
                {
                    ContentToEngine.DestroyResource(modelId, AssetTypes.Mesh);
                }
            }
        }

        public uint[] GetRenderItems()
        {
            return
            [
                item1Id,
                item2Id,
                item3Id,
                item4Id,
                item5Id,
                item6Id,
            ];
        }
    }
}
