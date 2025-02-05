using AssetsImporter;
using DX12Windows.Scripts;
using DX12Windows.Shaders;
using PrimalLike;
using PrimalLike.Common;
using PrimalLike.Content;
using PrimalLike.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Threading;

namespace DX12Windows.Content
{
    class LabSceneRenderItem : ITestRenderItem
    {
        private const string modelPrimalLab = "../../../../../Assets/LabScene.fbx";

        private uint fanModelId = uint.MaxValue;
        private uint intModelId = uint.MaxValue;
        private uint labModelId = uint.MaxValue;

        private uint fanItemId = uint.MaxValue;
        private uint intItemId = uint.MaxValue;
        private uint labItemId = uint.MaxValue;

        private uint fanEntityId = uint.MaxValue;
        private uint intEntityId = uint.MaxValue;
        private uint labEntityId = uint.MaxValue;

        private uint mtlId = uint.MaxValue;

        private readonly Dictionary<uint, uint> renderItemEntityMap = [];

        public Vector3 InitialCameraPosition { get; } = new(13.76f, 3f, -1.1f);
        public Quaternion InitialCameraRotation { get; } = Quaternion.CreateFromYawPitchRoll(-2.1f, -0.117f, 0f);

        public void Load(string assetsFolder, string outputsFolder)
        {
            if (!Application.RegisterScript<FanScript>())
            {
                Console.WriteLine("Failed to register script");
            }
            if (!Application.RegisterScript<WibblyWobblyScript>())
            {
                Console.WriteLine("Failed to register script");
            }

            string[] modelNames =
            [
                Path.Combine(outputsFolder, "fan_model.model"),
                Path.Combine(outputsFolder, "lab_model.model"),
                Path.Combine(outputsFolder, "int_model.model")
            ];

            if (!File.Exists(modelNames[0]) || !File.Exists(modelNames[1]) || !File.Exists(modelNames[2]))
            {
                var assets = AssimpImporter.Read(modelPrimalLab, new(), assetsFolder);
                Debug.Assert(assets.Length == 3);
                for (int i = 0; i < assets.Length; i++)
                {
                    if (string.IsNullOrEmpty(assets[i]))
                    {
                        continue;
                    }

                    string output = modelNames[i];
                    if (File.Exists(output))
                    {
                        File.Delete(output);
                    }
                    AssimpImporter.Import(assets[i], output);
                }
            }

            CreateRenderItems(modelNames);
        }

        private uint LoadModel(string modelPath)
        {
            modelPath = Path.GetFullPath(modelPath);
            using var file = new MemoryStream(File.ReadAllBytes(modelPath));

            uint modelId = ContentToEngine.CreateResource(file, AssetTypes.Mesh);
            Debug.Assert(IdDetail.IsValid(modelId));

            return modelId;
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
        private void RemoveItem(uint entityId, uint itemId, uint modelId)
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

        private void CreateRenderItems(string[] models)
        {
            // NOTE: you can get these models if you're a patreon supporter of Primal Engine.
            //       Use the editor to import the scene and put the 3 models in this location.
            //       You can replace them with any model that's available to you.
            var _1 = new Thread(() => { fanModelId = LoadModel(models[0]); });
            var _2 = new Thread(() => { labModelId = LoadModel(models[1]); });
            var _3 = new Thread(() => { intModelId = LoadModel(models[2]); });
            var _4 = new Thread(TestShaders.LoadShaders);

            fanEntityId = HelloWorldApp.CreateOneGameEntity(new(-10.47f, 5.93f, -6.7f), Vector3.Zero, nameof(FanScript)).Id;
            labEntityId = HelloWorldApp.CreateOneGameEntity(Vector3.Zero, Vector3.Zero, null).Id;
            intEntityId = HelloWorldApp.CreateOneGameEntity(new(0f, 1.3f, -6.6f), Vector3.Zero, nameof(WibblyWobblyScript)).Id;

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
        public void DestroyRenderItems()
        {
            RemoveItem(labEntityId, labItemId, labModelId);
            RemoveItem(fanEntityId, fanItemId, fanModelId);
            RemoveItem(intEntityId, intItemId, intModelId);

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
            return [labItemId, fanItemId, intItemId];
        }
    }
}
