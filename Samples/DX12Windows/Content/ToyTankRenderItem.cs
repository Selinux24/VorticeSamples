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
    class ToyTankRenderItem : ITestRenderItem
    {
        private const string modelToyTank = "../../../../../Assets/ToyTank.fbx";

        private const string modelName = "toytank_model.model";
        private uint modelId = uint.MaxValue;
        private uint itemId = uint.MaxValue;
        private uint mtlId = uint.MaxValue;

        private readonly Dictionary<uint, uint> renderItemEntityMap = [];

        public Vector3 InitialCameraPosition { get; } = new(0, 0.2f, -3f);
        public Quaternion InitialCameraRotation { get; } = Quaternion.CreateFromYawPitchRoll(3.14f, 3.14f, 0);

        public void Load(string assetsFolder, string outputsFolder)
        {
            string[] modelNames =
            [
                Path.Combine(outputsFolder, modelName),
            ];

            if (modelNames.Any(f => !File.Exists(f)))
            {
                string[] assets = [.. AssimpImporter.Read(modelToyTank, new(), assetsFolder)];
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
            var _1 = new Thread(() => { modelId = ITestRenderItem.LoadModel(Path.Combine(outputsFolder, modelName)); });
            var _2 = new Thread(TestShaders.LoadShaders);

            uint entityId = HelloWorldApp.CreateOneGameEntity(Vector3.Zero, new(-MathHelper.PiOver2, MathHelper.PiOver4, 0), 25f).Id;

            _1.Start();
            _2.Start();

            _1.Join();
            _2.Join();

            // NOTE: we need shaders to be ready before creating materials
            CreateMaterial();
            uint[] materials = [mtlId];

            itemId = ContentToEngine.AddRenderItem(entityId, modelId, materials);

            renderItemEntityMap[itemId] = entityId;
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
            RemoveItem(itemId, modelId);

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
            return [itemId];
        }
    }
}
