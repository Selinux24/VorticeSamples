using AssetsImporter;
using DX12Windows.Assets;
using DX12Windows.Lights;
using DX12Windows.Shaders;
using PrimalLike.Common;
using PrimalLike.Components;
using PrimalLike.Content;
using PrimalLike.Graphics;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using TexturesImporter;
using Vortice.Mathematics;

namespace DX12Windows.Content
{
    class ToyTankRenderItem : ITestRenderItem
    {
        const string assetFolder = "../../../../../../Assets/";

        const string modelToyTank = assetFolder + "ToyTank.fbx";
        const string envMapTexture = assetFolder + "belfast_sunset_puresky_4k.hdr";

        const string modelName = "toytank_model.model";

        const string iblBrdfLutTextureName = "ibl/brdf_lut.texture";
        const string iblDiffuseTextureName = "ibl/set2/diffuse.texture";
        const string iblSpecularTextureName = "ibl/set2/specular.texture";

        uint modelId = uint.MaxValue;
        uint entityId = uint.MaxValue;

        uint iblBrdfLutId = uint.MaxValue;
        uint iblDiffuseId = uint.MaxValue;
        uint iblSpecularId = uint.MaxValue;

        uint mtlId = uint.MaxValue;

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
                new(() => { modelId = ITestRenderItem.LoadModel(Path.Combine(outputsFolder, modelName)); }),

                new(() => { iblBrdfLutId = ITestRenderItem.LoadTexture(Path.Combine(outputsFolder, iblBrdfLutTextureName)); }),
                new(() => { iblDiffuseId = ITestRenderItem.LoadTexture(Path.Combine(outputsFolder, iblDiffuseTextureName)); }),
                new(() => { iblSpecularId = ITestRenderItem.LoadTexture(Path.Combine(outputsFolder, iblSpecularTextureName)); }),

                new(TestShader.Load));

            LightGenerator.CreateIblLight(iblBrdfLutId, iblDiffuseId, iblSpecularId);

            // NOTE: we need shaders to be ready before creating materials
            CreateMaterial();

            GeometryInfo geometryInfo = new()
            {
                GeometryContentId = modelId,
                MaterialIds = [mtlId]
            };
            entityId = HelloWorldApp.CreateOneGameEntity(Vector3.Zero, Quaternion.CreateFromYawPitchRoll(MathHelper.PiOver4, -MathHelper.PiOver2, 0f), 25f, geometryInfo).Id;
        }
        void CreateMaterial()
        {
            Debug.Assert(IdDetail.IsValid(TestShader.VsId) && IdDetail.IsValid(TestShader.PsId));

            MaterialInitInfo info = new();
            info.ShaderIds[(uint)ShaderTypes.Vertex] = TestShader.VsId;
            info.ShaderIds[(uint)ShaderTypes.Pixel] = TestShader.PsId;
            info.Type = MaterialTypes.Opaque;

            info.Surface.BaseColor = new(0.5f, 1f, 0.5f, 1f);
            info.Surface.Roughness = 0.2f;
            info.Surface.Metallic = 1.0f;

            mtlId = ContentToEngine.CreateResource(info, AssetTypes.Material);
        }

        public void DestroyRenderItems()
        {
            HelloWorldApp.RemoveGameEntity(entityId);

            ITestRenderItem.RemoveModel(modelId);

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
