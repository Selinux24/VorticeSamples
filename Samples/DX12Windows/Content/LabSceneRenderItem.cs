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
    class LabSceneRenderItem : ITestRenderItem
    {
        private const string modelPrimalLab = "../../../../../Assets/LabScene.fbx";

        private const string fanModelName = "fanmodel.model";
        private const string intModelName = "labmodel.model";
        private const string labModelName = "intmodel.model";

        private uint fanModelId = uint.MaxValue;
        private uint intModelId = uint.MaxValue;
        private uint labModelId = uint.MaxValue;

        private uint fanItemId = uint.MaxValue;
        private uint intItemId = uint.MaxValue;
        private uint labItemId = uint.MaxValue;

        private uint mtlId = uint.MaxValue;

        private readonly Dictionary<uint, uint> renderItemEntityMap = [];

        public Vector3 InitialCameraPosition { get; } = new(13.76f, 3f, -1.1f);
        public Quaternion InitialCameraRotation { get; } = Quaternion.CreateFromYawPitchRoll(-1.70f, -0.137f, 0f);

        public void Load(string assetsFolder, string outputsFolder)
        {
            string[] modelNames =
            [
                Path.Combine(outputsFolder, fanModelName),
                Path.Combine(outputsFolder, intModelName),
                Path.Combine(outputsFolder, labModelName)
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
            // NOTE: you can get these models if you're a patreon supporter of Primal Engine.
            //       Use the editor to import the scene and put the 3 models in this location.
            //       You can replace them with any model that's available to you.
            var _1 = new Thread(() => { fanModelId = ITestRenderItem.LoadModel(Path.Combine(outputsFolder, fanModelName)); });
            var _2 = new Thread(() => { labModelId = ITestRenderItem.LoadModel(Path.Combine(outputsFolder, intModelName)); });
            var _3 = new Thread(() => { intModelId = ITestRenderItem.LoadModel(Path.Combine(outputsFolder, labModelName)); });
            var _4 = new Thread(TestShaders.LoadShaders);

            uint fanEntityId = HelloWorldApp.CreateOneGameEntity<FanScript>(new(-10.47f, 5.93f, -6.7f), Vector3.Zero).Id;
            uint labEntityId = HelloWorldApp.CreateOneGameEntity(Vector3.Zero, Vector3.Zero).Id;
            uint intEntityId = HelloWorldApp.CreateOneGameEntity<WibblyWobblyScript>(new(0f, 1.3f, -6.6f), Vector3.Zero).Id;

            _1.Start();
            _2.Start();
            _3.Start();
            _4.Start();

            _1.Join();
            _2.Join();
            _3.Join();
            _4.Join();

            // NOTE: we need shaders to be ready before creating materials
            CreateMaterial();
            uint[] materials = [mtlId];

            fanItemId = Direct3D12.Content.RenderItem.Add(fanEntityId, fanModelId, materials);
            labItemId = Direct3D12.Content.RenderItem.Add(labEntityId, labModelId, materials);
            intItemId = Direct3D12.Content.RenderItem.Add(intEntityId, intModelId, materials);

            renderItemEntityMap[fanItemId] = fanEntityId;
            renderItemEntityMap[labItemId] = labEntityId;
            renderItemEntityMap[intItemId] = intEntityId;
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
            RemoveItem(labItemId, labModelId);
            RemoveItem(fanItemId, fanModelId);
            RemoveItem(intItemId, intModelId);

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
                Direct3D12.Content.RenderItem.Remove(itemId);

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
            return [labItemId, fanItemId, intItemId];
        }
    }
}
