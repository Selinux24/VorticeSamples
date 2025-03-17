using DX12Windows.Scripts;
using DX12Windows.Shaders;
using PrimalLike;
using PrimalLike.Common;
using PrimalLike.Content;
using PrimalLike.Graphics;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Threading;

namespace DX12Windows.Content
{
    class ModelRenderItem : ITestRenderItem
    {
        private const string testModelFile = "./Content/Model.model";

        private uint modelId = IdDetail.InvalidId;
        private uint mtlId = IdDetail.InvalidId;
        private uint itemId = IdDetail.InvalidId;

        private readonly Dictionary<uint, uint> renderItemEntityMap = [];

        public Vector3 InitialCameraPosition { get; } = new(0, 1f, 3f);
        public Quaternion InitialCameraRotation { get; } = Quaternion.CreateFromYawPitchRoll(0, 3.14f, 0);

        public void Load(string assetsFolder, string outputsFolder)
        {
            CreateRenderItem(testModelFile);
        }

        private void LoadModel(string model)
        {
            string modelPath = Path.GetFullPath(model);
            using var file = new MemoryStream(File.ReadAllBytes(modelPath));

            modelId = ContentToEngine.CreateResource(file, AssetTypes.Mesh);
            Debug.Assert(IdDetail.IsValid(modelId));
        }
        private void CreateMaterial()
        {
            MaterialInitInfo info = new();
            info.ShaderIds[(uint)ShaderTypes.Vertex] = TestShaders.VsId;
            info.ShaderIds[(uint)ShaderTypes.Pixel] = TestShaders.PsId;
            info.Type = MaterialTypes.Opaque;
            mtlId = ContentToEngine.CreateResource(info, AssetTypes.Material);
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

        private uint CreateRenderItem(string model)
        {
            uint entityId = HelloWorldApp.CreateOneGameEntity<RotatorScript>(Vector3.Zero, Vector3.Zero).Id;

            // load a model, pretend it belongs to entity_id
            var _1 = new Thread(() => LoadModel(model));
            _1.Start();

            // load a material:
            // 1) load textures, oh nooooo we don't have any, but that's ok.
            // 2) load shaders for that material
            var _2 = new Thread(TestShaders.LoadShaders);
            _2.Start();

            _1.Join();
            _2.Join();

            // add a render item using the model and its materials.
            CreateMaterial();
            uint[] materials = [mtlId, mtlId, mtlId, mtlId, mtlId];

            // TODO: add add_render_item in renderer.
            itemId = ContentToEngine.AddRenderItem(entityId, modelId, materials);

            renderItemEntityMap[itemId] = entityId;
            return itemId;
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
        public uint[] GetRenderItems()
        {
            return [itemId];
        }
    }
}
