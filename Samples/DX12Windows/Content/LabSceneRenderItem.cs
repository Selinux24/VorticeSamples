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
using System.Linq;
using System.Numerics;
using System.Threading;
using TexturesImporter;

namespace DX12Windows.Content
{
    class LabSceneRenderItem : ITestRenderItem
    {
        private const string modelPrimalLab = "../../../../../Assets/LabScene.fbx";
        private const string modelFembot = "../../../../../Assets/Fembot_v1.fbx";

        private const string ambientOcclusionTexture = "../../../../../Assets/AmbientOcclusion.png";
        private const string baseColorTexture = "../../../../../Assets/BaseColor.png";
        private const string emissiveTexture = "../../../../../Assets/Emissive.png";
        private const string metalRoughTexture = "../../../../../Assets/MetalRough.png";
        private const string normalTexture = "../../../../../Assets/Normal.png";

        private const string fanModelName = "fanmodel.model";
        private const string intModelName = "labmodel.model";
        private const string labModelName = "intmodel.model";
        private const string fembotModelName = "fembotmodel.model";

        private const string ambientOcclusionTextureName = "ambient_occlusion.texture";
        private const string baseColorTextureName = "base_color.texture";
        private const string emissiveTextureName = "emissive.texture";
        private const string metalRoughTextureName = "metal_rough.texture";
        private const string normalTextureName = "normal.texture";

        private uint fanModelId = uint.MaxValue;
        private uint intModelId = uint.MaxValue;
        private uint labModelId = uint.MaxValue;
        private uint fembotModelId = uint.MaxValue;

        private uint fanItemId = uint.MaxValue;
        private uint intItemId = uint.MaxValue;
        private uint labItemId = uint.MaxValue;
        private uint fembotItemId = uint.MaxValue;

        private enum TextureUsages : uint
        {
            AmbientOcclusion = 0,
            BaseColor,
            Emissive,
            MetalRough,
            Normal,

            Count
        }

        private readonly uint[] textureIds = new uint[(int)TextureUsages.Count];

        private uint defaultMtlId = uint.MaxValue;
        private uint fembotMtlId = uint.MaxValue;

        private readonly Dictionary<uint, uint> renderItemEntityMap = [];

        public Vector3 InitialCameraPosition { get; } = new(13.76f, 3f, -1.1f);
        public Quaternion InitialCameraRotation { get; } = Quaternion.CreateFromYawPitchRoll(-1.70f, -0.137f, 0f);

        public void Load(string assetsFolder, string outputsFolder)
        {
            string[] modelNames =
            [
                Path.Combine(outputsFolder, fanModelName),
                Path.Combine(outputsFolder, intModelName),
                Path.Combine(outputsFolder, labModelName),
                Path.Combine(outputsFolder, fembotModelName),
            ];

            if (modelNames.Any(f => !File.Exists(f)))
            {
                string[] assets =
                [
                    .. AssimpImporter.Read(modelPrimalLab, new(), assetsFolder),
                    .. AssimpImporter.Read(modelFembot, new(), assetsFolder),
                ];

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

            TextureData data = new();
            data.ImportSettings.Sources = ambientOcclusionTexture;
            TextureImporter.Import(ref data);
            data.SaveTexture(Path.Combine(outputsFolder, ambientOcclusionTextureName));

            data.ImportSettings.Sources = baseColorTexture;
            TextureImporter.Import(ref data);
            data.SaveTexture(Path.Combine(outputsFolder, baseColorTextureName));

            data.ImportSettings.Sources = emissiveTexture;
            TextureImporter.Import(ref data);
            data.SaveTexture(Path.Combine(outputsFolder, emissiveTextureName));

            data.ImportSettings.Sources = metalRoughTexture;
            TextureImporter.Import(ref data);
            data.SaveTexture(Path.Combine(outputsFolder, metalRoughTextureName));

            data.ImportSettings.Sources = normalTexture;
            TextureImporter.Import(ref data);
            data.SaveTexture(Path.Combine(outputsFolder, normalTextureName));

            CreateRenderItems(outputsFolder);
        }
        private void CreateRenderItems(string outputsFolder)
        {
            // NOTE: you can get these models if you're a patreon or ko-fi supporter of Primal Engine.
            //       https://www.patreon.com/collection/270663
            //       https://ko-fi.com/gameengineseries/shop
            //       Use the editor to import the scene and put the 3 models in this location.
            //       You can replace them with any model that's available to you.
            Thread[] tasks =
            [
                new(() => { textureIds[(uint)TextureUsages.AmbientOcclusion] = ITestRenderItem.LoadTexture(ambientOcclusionTextureName); }),
                new(() => { textureIds[(uint)TextureUsages.BaseColor] = ITestRenderItem.LoadTexture(baseColorTextureName); }),
                new(() => { textureIds[(uint)TextureUsages.Emissive] = ITestRenderItem.LoadTexture(emissiveTextureName); }),
                new(() => { textureIds[(uint)TextureUsages.MetalRough] = ITestRenderItem.LoadTexture(metalRoughTextureName); }),
                new(() => { textureIds[(uint)TextureUsages.Normal] = ITestRenderItem.LoadTexture(normalTextureName); }),

                new(() => { fanModelId = ITestRenderItem.LoadModel(Path.Combine(outputsFolder, fanModelName)); }),
                new(() => { labModelId = ITestRenderItem.LoadModel(Path.Combine(outputsFolder, intModelName)); }),
                new(() => { intModelId = ITestRenderItem.LoadModel(Path.Combine(outputsFolder, labModelName)); }),
                new(() => { fembotModelId = ITestRenderItem.LoadModel(Path.Combine(outputsFolder, fembotModelName)); }),
                new(TestShaders.LoadShaders),
            ];

            uint fanEntityId = HelloWorldApp.CreateOneGameEntity<FanScript>(new Vector3(-10.47f, 5.93f, -6.7f), Vector3.Zero).Id;
            uint labEntityId = HelloWorldApp.CreateOneGameEntity(Vector3.Zero, Vector3.Zero).Id;
            uint intEntityId = HelloWorldApp.CreateOneGameEntity<WibblyWobblyScript>(new Vector3(0f, 1.3f, -6.6f), Vector3.Zero).Id;
            uint fembotEntityId = HelloWorldApp.CreateOneGameEntity<RotatorScript>(new Vector3(-6f, 0f, 10f), new Vector3(0f, MathF.PI, 0f)).Id;

            foreach (var t in tasks)
            {
                t.Start();
            }

            foreach (var t in tasks)
            {
                t.Join();
            }

            // NOTE: we need shaders to be ready before creating materials
            CreateMaterial();
            uint[] materials = [defaultMtlId, fembotMtlId];
            uint[] fembotMaterials = [fembotMtlId, fembotMtlId];

            fanItemId = ContentToEngine.AddRenderItem(fanEntityId, fanModelId, materials);
            labItemId = ContentToEngine.AddRenderItem(labEntityId, labModelId, materials);
            intItemId = ContentToEngine.AddRenderItem(intEntityId, intModelId, materials);
            fembotItemId = ContentToEngine.AddRenderItem(fembotEntityId, fembotModelId, fembotMaterials);

            renderItemEntityMap[fanItemId] = fanEntityId;
            renderItemEntityMap[labItemId] = labEntityId;
            renderItemEntityMap[intItemId] = intEntityId;
            renderItemEntityMap[fembotItemId] = fembotEntityId;
        }
        private void CreateMaterial()
        {
            Debug.Assert(IdDetail.IsValid(TestShaders.VsId) && IdDetail.IsValid(TestShaders.PsId));

            MaterialInitInfo info = new();
            info.ShaderIds[(uint)ShaderTypes.Vertex] = TestShaders.VsId;
            info.ShaderIds[(uint)ShaderTypes.Pixel] = TestShaders.PsId;
            info.Type = MaterialTypes.Opaque;
            defaultMtlId = ContentToEngine.CreateResource(info, AssetTypes.Material);

            info.ShaderIds[(uint)ShaderTypes.Pixel] = TestShaders.TexturedPsId;
            info.TextureCount = (int)TextureUsages.Count;
            info.TextureIds = textureIds;
            fembotMtlId = ContentToEngine.CreateResource(info, AssetTypes.Material);
        }

        public void DestroyRenderItems()
        {
            RemoveItem(labItemId, labModelId);
            RemoveItem(fanItemId, fanModelId);
            RemoveItem(intItemId, intModelId);
            RemoveItem(fembotItemId, fembotModelId);

            // remove material
            if (IdDetail.IsValid(defaultMtlId))
            {
                ContentToEngine.DestroyResource(defaultMtlId, AssetTypes.Material);
            }

            if (IdDetail.IsValid(fembotMtlId))
            {
                ContentToEngine.DestroyResource(fembotMtlId, AssetTypes.Material);
            }

            // remove shaders and textures
            TestShaders.RemoveShaders();
        }
        private void RemoveItem(uint itemId, uint modelId)
        {
            if (!IdDetail.IsValid(itemId))
            {
                return;
            }

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

        public uint[] GetRenderItems()
        {
            return [labItemId, fanItemId, intItemId, fembotItemId];
        }
    }
}
