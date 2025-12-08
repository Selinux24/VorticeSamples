using AssetsImporter;
using ContentTools;
using DX12Windows.Scripts;
using DX12Windows.Shaders;
using PrimalLike.Common;
using PrimalLike.Components;
using PrimalLike.Content;
using PrimalLike.Graphics;
using System;
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
        private const string sphereModelName = "sphere.model";

        private const string ambientOcclusionTextureName = "AmbientOcclusion.texture";
        private const string baseColorTextureName = "BaseColor.texture";
        private const string emissiveTextureName = "Emissive.texture";
        private const string metalRoughTextureName = "MetalRough.texture";
        private const string normalTextureName = "Normal.texture";

        private uint fanModelId = uint.MaxValue;
        private uint intModelId = uint.MaxValue;
        private uint labModelId = uint.MaxValue;
        private uint fembotModelId = uint.MaxValue;
        private uint sphereModelId = uint.MaxValue;

        private uint fanEntityId = uint.MaxValue;
        private uint intEntityId = uint.MaxValue;
        private uint labEntityId = uint.MaxValue;
        private uint fembotEntityId = uint.MaxValue;
        private readonly uint[] sphereEntityIds = new uint[12];

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
        private readonly uint[] pbrMtlIds = new uint[12];

        public Vector3 InitialCameraPosition { get; } = new(-5.49f, 1.73f, 9.26f);
        public Quaternion InitialCameraRotation { get; } = Quaternion.CreateFromYawPitchRoll(5.61f, 0.19f, 0f);

        public void Load(string assetsFolder, string outputsFolder)
        {
            string[] modelNames =
            [
                Path.Combine(outputsFolder, fanModelName),
                Path.Combine(outputsFolder, intModelName),
                Path.Combine(outputsFolder, labModelName),
                Path.Combine(outputsFolder, fembotModelName),
                Path.Combine(outputsFolder, sphereModelName),
            ];

            if (modelNames.Any(f => !File.Exists(f)))
            {
                string[] assets =
                [
                    .. AssimpImporter.Read(modelPrimalLab, new(), assetsFolder),
                    .. AssimpImporter.Read(modelFembot, new(), assetsFolder),
                    .. PrimitiveMesh.CreatePrimitiveMesh(new(), new() { Type = PrimitiveMeshType.UvSphere, Segments = [48, 48], Size = new Vector3(0.5f) }, assetsFolder),
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

            string outputAmbientOcclusionTexture = Path.Combine(outputsFolder, ambientOcclusionTextureName);
            string outputBaseColorTexture = Path.Combine(outputsFolder, baseColorTextureName);
            string outputEmissiveTexture = Path.Combine(outputsFolder, emissiveTextureName);
            string outputMetalRoughTexture = Path.Combine(outputsFolder, metalRoughTextureName);
            string outputNormalTexture = Path.Combine(outputsFolder, normalTextureName);

            if (!File.Exists(outputAmbientOcclusionTexture))
            {
                TextureData ambientOcclusionData = new();
                ambientOcclusionData.ImportSettings.Compress = true;
                ambientOcclusionData.ImportSettings.PreferBc7 = true;
                ambientOcclusionData.ImportSettings.AlphaThreshold = 0.5f;
                ambientOcclusionData.ImportSettings.Sources = ambientOcclusionTexture;

                TextureImporter.Import(ref ambientOcclusionData);
                ambientOcclusionData.SaveTexture(outputAmbientOcclusionTexture);
            }

            if (!File.Exists(outputBaseColorTexture))
            {
                TextureData baseColorData = new();
                baseColorData.ImportSettings.Compress = true;
                baseColorData.ImportSettings.PreferBc7 = true;
                baseColorData.ImportSettings.AlphaThreshold = 0.5f;
                baseColorData.ImportSettings.Sources = baseColorTexture;

                TextureImporter.Import(ref baseColorData);
                baseColorData.SaveTexture(outputBaseColorTexture);
            }

            if (!File.Exists(outputEmissiveTexture))
            {
                TextureData emissiveData = new();
                emissiveData.ImportSettings.Compress = true;
                emissiveData.ImportSettings.PreferBc7 = true;
                emissiveData.ImportSettings.AlphaThreshold = 0.5f;
                emissiveData.ImportSettings.Sources = emissiveTexture;

                TextureImporter.Import(ref emissiveData);
                emissiveData.SaveTexture(outputEmissiveTexture);
            }

            if (!File.Exists(outputMetalRoughTexture))
            {
                TextureData metalRoughData = new();
                metalRoughData.ImportSettings.Compress = true;
                metalRoughData.ImportSettings.PreferBc7 = true;
                metalRoughData.ImportSettings.AlphaThreshold = 0.5f;
                metalRoughData.ImportSettings.OutputFormat = BCFormats.BC5DualChannelGray;
                metalRoughData.ImportSettings.Sources = metalRoughTexture;

                TextureImporter.Import(ref metalRoughData);
                metalRoughData.SaveTexture(outputMetalRoughTexture);
            }

            if (!File.Exists(outputNormalTexture))
            {
                TextureData normalData = new();
                normalData.ImportSettings.Compress = true;
                normalData.ImportSettings.PreferBc7 = true;
                normalData.ImportSettings.AlphaThreshold = 0.5f;
                normalData.ImportSettings.Sources = normalTexture;

                TextureImporter.Import(ref normalData);
                normalData.SaveTexture(outputNormalTexture);
            }

            TextureImporter.ShutDownTextureTools();

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
                new(() => { textureIds[(uint)TextureUsages.AmbientOcclusion] = ITestRenderItem.LoadTexture(Path.Combine(outputsFolder, ambientOcclusionTextureName)); }),
                new(() => { textureIds[(uint)TextureUsages.BaseColor] = ITestRenderItem.LoadTexture(Path.Combine(outputsFolder, baseColorTextureName)); }),
                new(() => { textureIds[(uint)TextureUsages.Emissive] = ITestRenderItem.LoadTexture(Path.Combine(outputsFolder, emissiveTextureName)); }),
                new(() => { textureIds[(uint)TextureUsages.MetalRough] = ITestRenderItem.LoadTexture(Path.Combine(outputsFolder, metalRoughTextureName)); }),
                new(() => { textureIds[(uint)TextureUsages.Normal] = ITestRenderItem.LoadTexture(Path.Combine(outputsFolder, normalTextureName)); }),

                new(() => { fanModelId = ITestRenderItem.LoadModel(Path.Combine(outputsFolder, fanModelName)); }),
                new(() => { labModelId = ITestRenderItem.LoadModel(Path.Combine(outputsFolder, intModelName)); }),
                new(() => { intModelId = ITestRenderItem.LoadModel(Path.Combine(outputsFolder, labModelName)); }),
                new(() => { fembotModelId = ITestRenderItem.LoadModel(Path.Combine(outputsFolder, fembotModelName)); }),
                new(() => { sphereModelId = ITestRenderItem.LoadModel(Path.Combine(outputsFolder, sphereModelName)); }),
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

            geometryInfo.MaterialIds = [defaultMtlId];

            geometryInfo.GeometryContentId = labModelId;
            labEntityId = HelloWorldApp.CreateOneGameEntity(Vector3.Zero, Quaternion.Identity, geometryInfo).Id;

            geometryInfo.GeometryContentId = fanModelId;
            fanEntityId = HelloWorldApp.CreateOneGameEntity(new(-10.47f, 5.93f, -6.7f), Quaternion.Identity, geometryInfo).Id;

            geometryInfo.GeometryContentId = intModelId;
            intEntityId = HelloWorldApp.CreateOneGameEntity(new(0f, 1.3f, -6.6f), Quaternion.Identity, geometryInfo).Id;

            geometryInfo.GeometryContentId = fembotModelId;
            geometryInfo.MaterialIds = [fembotMtlId, fembotMtlId];
            fembotEntityId = HelloWorldApp.CreateOneGameEntity<RotatorScript>(new(-6f, 0f, 10f), Quaternion.CreateFromYawPitchRoll(MathF.PI, 0f, 0f), geometryInfo).Id;

            Array.Fill(sphereEntityIds, uint.MaxValue);
            geometryInfo.GeometryContentId = sphereModelId;
            for (int i = 0; i < pbrMtlIds.Length; i++)
            {
                geometryInfo.MaterialIds = [pbrMtlIds[i]];
                float x = -6f + i % 6;
                float y = (i < 6) ? 7f : 5.5f;
                float z = x;
                sphereEntityIds[i] = HelloWorldApp.CreateOneGameEntity(new(x, y, z), Quaternion.Identity, geometryInfo).Id;
            }
        }
        private void CreateMaterial()
        {
            Debug.Assert(IdDetail.IsValid(TestShaders.VsId) && IdDetail.IsValid(TestShaders.PsId));

            {
                MaterialInitInfo info = new();
                info.ShaderIds[(uint)ShaderTypes.Vertex] = TestShaders.VsId;
                info.ShaderIds[(uint)ShaderTypes.Pixel] = TestShaders.PsId;
                info.Type = MaterialTypes.Opaque;

                defaultMtlId = ContentToEngine.CreateResource(info, AssetTypes.Material);
            }

            {
                Array.Fill(pbrMtlIds, uint.MaxValue);
                Vector2[] metalRough =
                [
                    new(0f, 0.0f),
                    new(0f, 0.2f),
                    new(0f, 0.4f),
                    new(0f, 0.6f),
                    new(0f, 0.8f),
                    new(0f, 1f),
                    new(1f, 0.0f),
                    new(1f, 0.2f),
                    new(1f, 0.4f),
                    new(1f, 0.6f),
                    new(1f, 0.8f),
                    new(1f, 1f),
                ];

                MaterialInitInfo info = new();
                info.ShaderIds[(uint)ShaderTypes.Vertex] = TestShaders.VsId;
                info.ShaderIds[(uint)ShaderTypes.Pixel] = TestShaders.PsId;
                info.Type = MaterialTypes.Opaque;

                ref var s = ref info.Surface;
                s.BaseColor = new(0.5f, 0.5f, 0.5f, 1f);

                for (int i = 0; i < pbrMtlIds.Length; i++)
                {
                    s.Metallic = metalRough[i].X;
                    s.Roughness = metalRough[i].Y;

                    pbrMtlIds[i] = ContentToEngine.CreateResource(info, AssetTypes.Material);
                }
            }

            {
                MaterialInitInfo info = new();
                info.ShaderIds[(uint)ShaderTypes.Vertex] = TestShaders.VsId;
                info.ShaderIds[(uint)ShaderTypes.Pixel] = TestShaders.PsId;
                info.ShaderIds[(uint)ShaderTypes.Pixel] = TestShaders.TexturedPsId;
                info.Type = MaterialTypes.Opaque;

                info.TextureCount = (int)TextureUsages.Count;
                info.TextureIds = textureIds;

                fembotMtlId = ContentToEngine.CreateResource(info, AssetTypes.Material);
            }
        }

        public void DestroyRenderItems()
        {
            HelloWorldApp.RemoveGameEntity(labEntityId);
            HelloWorldApp.RemoveGameEntity(fanEntityId);
            HelloWorldApp.RemoveGameEntity(intEntityId);
            HelloWorldApp.RemoveGameEntity(fembotEntityId);

            for (int i = 0; i < sphereEntityIds.Length; i++)
            {
                HelloWorldApp.RemoveGameEntity(sphereEntityIds[i]);
            }

            ITestRenderItem.RemoveModel(labModelId);
            ITestRenderItem.RemoveModel(fanModelId);
            ITestRenderItem.RemoveModel(intModelId);
            ITestRenderItem.RemoveModel(fembotModelId);
            ITestRenderItem.RemoveModel(sphereModelId);

            // remove material
            if (IdDetail.IsValid(defaultMtlId))
            {
                ContentToEngine.DestroyResource(defaultMtlId, AssetTypes.Material);
            }

            if (IdDetail.IsValid(fembotMtlId))
            {
                ContentToEngine.DestroyResource(fembotMtlId, AssetTypes.Material);
            }

            foreach (uint id in pbrMtlIds)
            {
                if (IdDetail.IsValid(id))
                {
                    ContentToEngine.DestroyResource(id, AssetTypes.Material);
                }
            }

            // remove textures
            foreach (uint id in textureIds)
            {
                if (IdDetail.IsValid(id))
                {
                    ContentToEngine.DestroyResource(id, AssetTypes.Texture);
                }
            }

            // remove shaders and textures
            TestShaders.RemoveShaders();
        }
    }
}
