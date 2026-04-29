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

namespace DX12Windows.Content
{
    class TalkingOnPhoneRenderItem : ITestRenderItem
    {
        const string assetFolder = "../../../../../../Assets/";

        const string modelToyTank = assetFolder + "Talking On Phone.fbx";
        const string envMapTexture = assetFolder + "belfast_sunset_puresky_4k.hdr";

        const string modelName1 = "talking_on_phone_Cloth.model";
        const string modelName2 = "talking_on_phone_Eyelashes.model";
        const string modelName3 = "talking_on_phone_Body.model";
        const string modelName4 = "talking_on_phone_Sneakers.model";
        const string modelName5 = "talking_on_phone_Socks.model";
        const string modelName6 = "talking_on_phone_Hair.model";

        const string iblBrdfLutTextureName = "ibl/brdf_lut.texture";
        const string iblDiffuseTextureName = "ibl/set2/diffuse.texture";
        const string iblSpecularTextureName = "ibl/set2/specular.texture";

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

        public Vector3 InitialCameraPosition { get; } = new(0, 1.2f, -2.4f);
        public Quaternion InitialCameraRotation { get; } = Quaternion.CreateFromYawPitchRoll(0, 0.1f, 0);

        public void Load(string assetsFolder, string outputsFolder)
        {
            string[] modelNames =
            [
                Path.Combine(outputsFolder, modelName1),
                Path.Combine(outputsFolder, modelName2),
                Path.Combine(outputsFolder, modelName3),
                Path.Combine(outputsFolder, modelName4),
                Path.Combine(outputsFolder, modelName5),
                Path.Combine(outputsFolder, modelName6),
            ];

            //if (modelNames.Any(f => !File.Exists(f)))
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
                new(() => { model1Id = ITestRenderItem.LoadModel(Path.Combine(outputsFolder, modelName1)); }),
                new(() => { model2Id = ITestRenderItem.LoadModel(Path.Combine(outputsFolder, modelName2)); }),
                new(() => { model3Id = ITestRenderItem.LoadModel(Path.Combine(outputsFolder, modelName3)); }),
                new(() => { model4Id = ITestRenderItem.LoadModel(Path.Combine(outputsFolder, modelName4)); }),
                new(() => { model5Id = ITestRenderItem.LoadModel(Path.Combine(outputsFolder, modelName5)); }),
                new(() => { model6Id = ITestRenderItem.LoadModel(Path.Combine(outputsFolder, modelName6)); }),

                new(() => { iblBrdfLutId = ITestRenderItem.LoadTexture(Path.Combine(outputsFolder, iblBrdfLutTextureName)); }),
                new(() => { iblDiffuseId = ITestRenderItem.LoadTexture(Path.Combine(outputsFolder, iblDiffuseTextureName)); }),
                new(() => { iblSpecularId = ITestRenderItem.LoadTexture(Path.Combine(outputsFolder, iblSpecularTextureName)); }),

                new(TestShader.Load));

            LightGenerator.CreateIblLight(iblBrdfLutId, iblDiffuseId, iblSpecularId);

            // NOTE: we need shaders to be ready before creating materials
            CreateMaterial();

            var entityIds = new uint[6];
            CreateEntityGroup(
                [model1Id, model2Id, model3Id, model4Id, model5Id, model6Id],
                [mtlId],
                entityIds);

            //Set the entity ids from the list
            entity1Id = entityIds[0];
            entity2Id = entityIds[1];
            entity3Id = entityIds[2];
            entity4Id = entityIds[3];
            entity5Id = entityIds[4];
            entity6Id = entityIds[5];
        }
        void CreateMaterial()
        {
            Debug.Assert(IdDetail.IsValid(TestShader.VsId) && IdDetail.IsValid(TestShader.PsId));

            MaterialInitInfo info = new();
            info.ShaderIds[(uint)ShaderTypes.Vertex] = TestShader.VsId;
            info.ShaderIds[(uint)ShaderTypes.Pixel] = TestShader.PsId;
            info.Type = MaterialTypes.Opaque;

            info.Surface.BaseColor = new(250 / 255f, 219 / 255f, 172 / 255f, 1f);
            info.Surface.Roughness = (byte)(255 * 0.8f);
            info.Surface.Metallic = 0;

            mtlId = ContentToEngine.CreateResource(info, AssetTypes.Material);
        }
        static void CreateEntityGroup(uint[] models, uint[] materials, uint[] entities)
        {
            for (int i = 0; i < models.Length; i++)
            {
                GeometryInfo geometryInfo = new()
                {
                    GeometryContentId = models[i],
                    MaterialIds = materials
                };
                entities[i] = HelloWorldApp.CreateOneGameEntity(Vector3.Zero, Quaternion.CreateFromYawPitchRoll(MathF.PI, 0, 0), 1, geometryInfo).Id;
            }
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
