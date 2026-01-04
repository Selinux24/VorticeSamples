using AssetsImporter;
using ContentTools;
using DX12Windows.Assets;
using DX12Windows.Lights;
using DX12Windows.Scripts;
using DX12Windows.Shaders;
using PrimalLike.Common;
using PrimalLike.Components;
using PrimalLike.Content;
using PrimalLike.Graphics;
using System;
using System.Diagnostics;
using System.IO;
using System.Numerics;
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
        private const string envMapTexture = "../../../../../Assets/qwantani_moon_noon_puresky_4k.hdr";

        private const string fanModelName = "fan_model.model";
        private const string labModelName = "lab_model.model";
        private const string intModelName = "int_model.model";
        private const string fembotModelName = "fembot_model.model";
        private const string sphereModelName = "sphere_model.model";

        private const string ambientOcclusionTextureName = "ambient_occlusion.texture";
        private const string baseColorTextureName = "base_color.texture";
        private const string emissiveTextureName = "emissive.texture";
        private const string metalRoughTextureName = "metal_rough.texture";
        private const string normalTextureName = "normal.texture";

        private const string iblBrdfLutTextureName = "ibl/brdf_lut.texture";
        private const string iblDiffuseTextureName = "ibl/set1/diffuse.texture";
        private const string iblSpecularTextureName = "ibl/set1/specular.texture";

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

        private readonly uint[] textureIds = new uint[(int)TestShaders.TextureUsages.Count];

        private uint iblBrdfLutId = uint.MaxValue;
        private uint iblDiffuseId = uint.MaxValue;
        private uint iblSpecularId = uint.MaxValue;

        private uint defaultMtlId = uint.MaxValue;
        private uint fembotMtlId = uint.MaxValue;
        private readonly uint[] pbrMtlIds = new uint[12];

        public Vector3 InitialCameraPosition { get; } = new(-5.49f, 1.73f, 9.26f);
        public Quaternion InitialCameraRotation { get; } = Quaternion.CreateFromYawPitchRoll(5.61f, 0.19f, 0f);

        public void Load(string assetsFolder, string outputsFolder)
        {
            Importer.ImportModels(
                () => AssimpImporter.Read(modelPrimalLab, new(), assetsFolder),
                [
                    Path.Combine(outputsFolder, fanModelName),
                    Path.Combine(outputsFolder, labModelName),
                    Path.Combine(outputsFolder, intModelName),
                ]);

            Importer.ImportModels(
                () => AssimpImporter.Read(modelFembot, new(), assetsFolder),
                Path.Combine(outputsFolder, fembotModelName));

            Importer.ImportModels(
                () => PrimitiveMesh.CreatePrimitiveMesh(new() { SmoothingAngle = 0f }, new() { Type = PrimitiveMeshType.UvSphere, Segments = [48, 48], Size = new Vector3(0.5f) }, assetsFolder),
                Path.Combine(outputsFolder, sphereModelName));

            using TextureImporter importer = new();

            Importer.ImportAmbientOcclusionTexture(importer, ambientOcclusionTexture, outputsFolder, ambientOcclusionTextureName);
            Importer.ImportBaseColorTexture(importer, baseColorTexture, outputsFolder, baseColorTextureName);
            Importer.ImportEmissiveTexture(importer, emissiveTexture, outputsFolder, emissiveTextureName);
            Importer.ImportMetalRoughTexture(importer, metalRoughTexture, outputsFolder, metalRoughTextureName);
            Importer.ImportNormalTexture(importer, normalTexture, outputsFolder, normalTextureName);

            string brdfLutPath = Path.Combine(outputsFolder, iblBrdfLutTextureName);
            string diffusePath = Path.Combine(outputsFolder, iblDiffuseTextureName);
            string specularPath = Path.Combine(outputsFolder, iblSpecularTextureName);
            Importer.ImportEnvironmentMapTexture(importer, envMapTexture, brdfLutPath, diffusePath, specularPath);

            CreateRenderItems(outputsFolder);
        }
        private void CreateRenderItems(string outputsFolder)
        {
            Array.Fill(textureIds, uint.MaxValue);

            // NOTE: you can get these models if you're a patreon or ko-fi supporter of Primal Engine.
            //       https://www.patreon.com/collection/270663
            //       https://ko-fi.com/gameengineseries/shop
            //       Use the editor to import the scene and put the 3 models in this location.
            //       You can replace them with any model that's available to you.
            Utils.Run(
                new(() => { textureIds[(uint)TestShaders.TextureUsages.AmbientOcclusion] = ITestRenderItem.LoadTexture(Path.Combine(outputsFolder, ambientOcclusionTextureName)); }),
                new(() => { textureIds[(uint)TestShaders.TextureUsages.BaseColor] = ITestRenderItem.LoadTexture(Path.Combine(outputsFolder, baseColorTextureName)); }),
                new(() => { textureIds[(uint)TestShaders.TextureUsages.Emissive] = ITestRenderItem.LoadTexture(Path.Combine(outputsFolder, emissiveTextureName)); }),
                new(() => { textureIds[(uint)TestShaders.TextureUsages.MetalRough] = ITestRenderItem.LoadTexture(Path.Combine(outputsFolder, metalRoughTextureName)); }),
                new(() => { textureIds[(uint)TestShaders.TextureUsages.Normal] = ITestRenderItem.LoadTexture(Path.Combine(outputsFolder, normalTextureName)); }),

                new(() => { iblBrdfLutId = ITestRenderItem.LoadTexture(Path.Combine(outputsFolder, iblBrdfLutTextureName)); }),
                new(() => { iblDiffuseId = ITestRenderItem.LoadTexture(Path.Combine(outputsFolder, iblDiffuseTextureName)); }),
                new(() => { iblSpecularId = ITestRenderItem.LoadTexture(Path.Combine(outputsFolder, iblSpecularTextureName)); }),

                new(() => { fanModelId = ITestRenderItem.LoadModel(Path.Combine(outputsFolder, fanModelName)); }),
                new(() => { labModelId = ITestRenderItem.LoadModel(Path.Combine(outputsFolder, labModelName)); }),
                new(() => { intModelId = ITestRenderItem.LoadModel(Path.Combine(outputsFolder, intModelName)); }),
                new(() => { fembotModelId = ITestRenderItem.LoadModel(Path.Combine(outputsFolder, fembotModelName)); }),
                new(() => { sphereModelId = ITestRenderItem.LoadModel(Path.Combine(outputsFolder, sphereModelName)); }),

                new(TestShaders.LoadShaders));

            LightGenerator.CreateIblLight(iblBrdfLutId, iblDiffuseId, iblSpecularId);

            // NOTE: we need shaders to be ready before creating materials
            CreateMaterial();

            {
                GeometryInfo geometryInfo = new()
                {
                    GeometryContentId = labModelId,
                    MaterialIds = [defaultMtlId]
                };
                labEntityId = HelloWorldApp.CreateOneGameEntity(Vector3.Zero, Quaternion.Identity, geometryInfo).Id;
            }
            {
                GeometryInfo geometryInfo = new()
                {
                    GeometryContentId = fanModelId,
                    MaterialIds = [defaultMtlId]
                };
                fanEntityId = HelloWorldApp.CreateOneGameEntity<FanScript>(new(-10.47f, 5.93f, -6.7f), Quaternion.Identity, geometryInfo).Id;
            }
            {
                GeometryInfo geometryInfo = new()
                {
                    GeometryContentId = intModelId,
                    MaterialIds = [defaultMtlId]
                };
                intEntityId = HelloWorldApp.CreateOneGameEntity<WibblyWobblyScript>(new(0f, 1.3f, 0f), Quaternion.Identity, geometryInfo).Id;
            }
            {
                GeometryInfo geometryInfo = new()
                {
                    GeometryContentId = fembotModelId,
                    MaterialIds = [fembotMtlId, fembotMtlId]
                };
                fembotEntityId = HelloWorldApp.CreateOneGameEntity(new(-6f, 0f, 10f), Quaternion.CreateFromYawPitchRoll(MathF.PI, 0f, 0f), geometryInfo).Id;
            }
            {
                Array.Fill(sphereEntityIds, uint.MaxValue);
                GeometryInfo geometryInfo = new()
                {
                    GeometryContentId = sphereModelId
                };
                for (int i = 0; i < pbrMtlIds.Length; i++)
                {
                    geometryInfo.MaterialIds = [pbrMtlIds[i]];
                    float x = -6f + i % 6;
                    float y = (i < 6) ? 7f : 5.5f;
                    float z = x;
                    sphereEntityIds[i] = HelloWorldApp.CreateOneGameEntity(new(x, y, z), Quaternion.Identity, geometryInfo).Id;
                }
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
                info.ShaderIds[(uint)ShaderTypes.Pixel] = TestShaders.TexturedPsId;
                info.Type = MaterialTypes.Opaque;

                info.TextureCount = (int)TestShaders.TextureUsages.Count;
                info.TextureIds = textureIds;

                fembotMtlId = ContentToEngine.CreateResource(info, AssetTypes.Material);
            }
        }

        public void DestroyRenderItems()
        {
            HelloWorldApp.RemoveGameEntity(fanEntityId);
            HelloWorldApp.RemoveGameEntity(labEntityId);
            HelloWorldApp.RemoveGameEntity(intEntityId);
            HelloWorldApp.RemoveGameEntity(fembotEntityId);

            for (int i = 0; i < sphereEntityIds.Length; i++)
            {
                HelloWorldApp.RemoveGameEntity(sphereEntityIds[i]);
            }

            ITestRenderItem.RemoveModel(fanModelId);
            ITestRenderItem.RemoveModel(labModelId);
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

            if (IdDetail.IsValid(iblBrdfLutId))
            {
                ContentToEngine.DestroyResource(iblBrdfLutId, AssetTypes.Texture);
            }

            if (IdDetail.IsValid(iblDiffuseId))
            {
                ContentToEngine.DestroyResource(iblDiffuseId, AssetTypes.Texture);
            }

            if (IdDetail.IsValid(iblSpecularId))
            {
                ContentToEngine.DestroyResource(iblSpecularId, AssetTypes.Texture);
            }

            // remove shaders and textures
            TestShaders.RemoveShaders();
        }
    }
}
