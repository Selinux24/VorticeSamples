using AssetsImporter;
using DX12Windows.Assets;
using DX12Windows.Lights;
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
using TexturesImporter;
using Vortice.Mathematics;

namespace DX12Windows.Content
{
    class M24RenderItem : ITestRenderItem
    {
        const string assetFolder = "../../../../../../Assets/";

        const string modelM24 = assetFolder + "m24.dae";
        const string envMapTexture = assetFolder + "kloofendal_48d_partly_cloudy_puresky_4k.hdr";

        const string model1Name = "m24_1_model.model";
        const string model2Name = "m24_2_model.model";
        const string model3Name = "m24_3_model.model";
        const string model4Name = "m24_4_model.model";
        const string model5Name = "m24_5_model.model";
        const string model6Name = "m24_6_model.model";

        const string iblBrdfLutTextureName = "ibl/brdf_lut.texture";
        const string iblDiffuseTextureName = "ibl/set4/diffuse.texture";
        const string iblSpecularTextureName = "ibl/set4/specular.texture";

        uint model1Id = uint.MaxValue;
        uint model2Id = uint.MaxValue;
        uint model3Id = uint.MaxValue;
        uint model4Id = uint.MaxValue;
        uint model5Id = uint.MaxValue;
        uint model6Id = uint.MaxValue;

        uint entity1Id = uint.MaxValue;
        uint entity2Id = uint.MaxValue;
        uint entity3Id = uint.MaxValue;
        uint entity4Id = uint.MaxValue;
        uint entity5Id = uint.MaxValue;
        uint entity6Id = uint.MaxValue;

        uint iblBrdfLutId = uint.MaxValue;
        uint iblDiffuseId = uint.MaxValue;
        uint iblSpecularId = uint.MaxValue;

        uint mtlId = uint.MaxValue;

        public Vector3 InitialCameraPosition { get; } = new(0, 10f, -45f);
        public Quaternion InitialCameraRotation { get; } = Quaternion.CreateFromYawPitchRoll(MathF.PI, MathF.PI, 0);

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

            using TextureImporter importer = new();
            string brdfLutPath = Path.Combine(outputsFolder, iblBrdfLutTextureName);
            string diffusePath = Path.Combine(outputsFolder, iblDiffuseTextureName);
            string specularPath = Path.Combine(outputsFolder, iblSpecularTextureName);
            Importer.ImportEnvironmentMapTexture(importer, envMapTexture, brdfLutPath, diffusePath, specularPath);

            CreateRenderItems(outputsFolder);
        }
        void CreateRenderItems(string outputsFolder)
        {
            Utils.Run(
                new(() => { model1Id = ITestRenderItem.LoadModel(Path.Combine(outputsFolder, model1Name)); }),
                new(() => { model2Id = ITestRenderItem.LoadModel(Path.Combine(outputsFolder, model2Name)); }),
                new(() => { model3Id = ITestRenderItem.LoadModel(Path.Combine(outputsFolder, model3Name)); }),
                new(() => { model4Id = ITestRenderItem.LoadModel(Path.Combine(outputsFolder, model4Name)); }),
                new(() => { model5Id = ITestRenderItem.LoadModel(Path.Combine(outputsFolder, model5Name)); }),
                new(() => { model6Id = ITestRenderItem.LoadModel(Path.Combine(outputsFolder, model6Name)); }),

                new(() => { iblBrdfLutId = ITestRenderItem.LoadTexture(Path.Combine(outputsFolder, iblBrdfLutTextureName)); }),
                new(() => { iblDiffuseId = ITestRenderItem.LoadTexture(Path.Combine(outputsFolder, iblDiffuseTextureName)); }),
                new(() => { iblSpecularId = ITestRenderItem.LoadTexture(Path.Combine(outputsFolder, iblSpecularTextureName)); }),

                new(TestShader.Load));

            LightGenerator.CreateIblLight(iblBrdfLutId, iblDiffuseId, iblSpecularId);

            // NOTE: we need shaders to be ready before creating materials
            CreateMaterial();

            var rotation = Quaternion.CreateFromYawPitchRoll(MathHelper.PiOver4 * 3, -MathHelper.PiOver2, 0f);

            {
                GeometryInfo geometryInfo = new()
                {
                    MaterialIds = [mtlId],
                    GeometryContentId = model1Id
                };
                entity1Id = HelloWorldApp.CreateOneGameEntity(Vector3.Zero, rotation, geometryInfo).Id;
            }
            {
                GeometryInfo geometryInfo = new()
                {
                    MaterialIds = [mtlId],
                    GeometryContentId = model2Id
                };
                entity2Id = HelloWorldApp.CreateOneGameEntity(Vector3.Zero, rotation, geometryInfo).Id;
            }
            {
                GeometryInfo geometryInfo = new()
                {
                    MaterialIds = [mtlId],
                    GeometryContentId = model3Id
                };
                entity3Id = HelloWorldApp.CreateOneGameEntity(Vector3.Zero, rotation, geometryInfo).Id;
            }
            {
                GeometryInfo geometryInfo = new()
                {
                    MaterialIds = [mtlId],
                    GeometryContentId = model4Id
                };
                entity4Id = HelloWorldApp.CreateOneGameEntity(Vector3.Zero, rotation, geometryInfo).Id;
            }
            {
                GeometryInfo geometryInfo = new()
                {
                    MaterialIds = [mtlId],
                    GeometryContentId = model5Id
                };
                entity5Id = HelloWorldApp.CreateOneGameEntity(Vector3.Zero, rotation, geometryInfo).Id;
            }
            {
                GeometryInfo geometryInfo = new()
                {
                    MaterialIds = [mtlId],
                    GeometryContentId = model6Id
                };
                entity6Id = HelloWorldApp.CreateOneGameEntity(Vector3.Zero, rotation, geometryInfo).Id;
            }
        }
        void CreateMaterial()
        {
            Debug.Assert(IdDetail.IsValid(TestShader.VsId) && IdDetail.IsValid(TestShader.PsId));

            MaterialInitInfo info = new();
            info.ShaderIds[(uint)ShaderTypes.Vertex] = TestShader.VsId;
            info.ShaderIds[(uint)ShaderTypes.Pixel] = TestShader.PsId;
            info.Type = MaterialTypes.Opaque;

            info.Surface.BaseColor = new(0.1f, 0.1f, 0.1f, 1f);
            info.Surface.Roughness = 0.2f;
            info.Surface.Metallic = 1.0f;

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
            TestShader.Remove();
        }
    }
}
