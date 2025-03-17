using AssetsImporter;
using DX12Windows.Scripts;
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

namespace DX12Windows.Content
{
    class HumveeRenderItem : ITestRenderItem
    {
        private const string modelPrimalLab = "../../../../../Assets/humvee.obj";

        private const string model1Name = "humvee_modelA.model";
        private const string model2Name = "humvee_modelB.model";

        private uint model1Id = uint.MaxValue;
        private uint model2Id = uint.MaxValue;

        private uint item1Id = uint.MaxValue;
        private uint item2Id = uint.MaxValue;

        private uint mtlId = uint.MaxValue;

        private readonly Dictionary<uint, uint> renderItemEntityMap = [];

        public Vector3 InitialCameraPosition { get; } = new(0f, 0.8f, -3f);
        public Quaternion InitialCameraRotation { get; } = Quaternion.CreateFromYawPitchRoll(3.14f, 3.14f, 0f);

        public void Load(string assetsFolder, string outputsFolder)
        {
            string[] modelNames =
            [
                Path.Combine(outputsFolder, model1Name),
                Path.Combine(outputsFolder, model2Name),
            ];

            if (modelNames.Any(f => !File.Exists(f)))
            {
                var assets = AssimpImporter.Read(modelPrimalLab, new(), assetsFolder);
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
            var _3 = new Thread(TestShaders.LoadShaders);

            uint entity1Id = HelloWorldApp.CreateOneGameEntity<RotatorScript>(Vector3.Zero, Vector3.Zero).Id;
            uint entity2Id = HelloWorldApp.CreateOneGameEntity<RotatorScript>(Vector3.Zero, Vector3.Zero).Id;

            _1.Start();
            _2.Start();
            _3.Start();

            _1.Join();
            _2.Join();
            _3.Join();

            // NOTE: we need shaders to be ready before creating materials
            CreateMaterial();
            uint[] materials = [mtlId];

            item1Id = ContentToEngine.AddRenderItem(entity1Id, model1Id, materials);
            item2Id = ContentToEngine.AddRenderItem(entity2Id, model2Id, materials);

            renderItemEntityMap[item1Id] = entity1Id;
            renderItemEntityMap[item2Id] = entity2Id;
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
            return [item1Id, item2Id];
        }
    }
}
