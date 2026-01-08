using AssetsImporter;
using DX12Windows.Assets;
using DX12Windows.Lights;
using DX12Windows.Scripts;
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

namespace DX12Windows.Content
{
    class HumveeRenderItem : ITestRenderItem
    {
        const string assetFolder = "../../../../../../Assets/";

        const string modelHumvee = assetFolder + "humvee.obj";
        const string model1Name = "humvee_modelA.model";
        const string model2Name = "humvee_modelB.model";

        const string envMapTexture = assetFolder + "sunny_rose_garden_4k.hdr";
        const string iblBrdfLutTextureName = "ibl/brdf_lut.texture";
        const string iblDiffuseTextureName = "ibl/set3/diffuse.texture";
        const string iblSpecularTextureName = "ibl/set3/specular.texture";

        uint model1Id = uint.MaxValue;
        uint model2Id = uint.MaxValue;

        uint entity1Id = uint.MaxValue;
        uint entity2Id = uint.MaxValue;

        uint iblBrdfLutId = uint.MaxValue;
        uint iblDiffuseId = uint.MaxValue;
        uint iblSpecularId = uint.MaxValue;

        uint mtlId = uint.MaxValue;

        public Vector3 InitialCameraPosition { get; } = new(0f, 0.8f, -3f);
        public Quaternion InitialCameraRotation { get; } = Quaternion.CreateFromYawPitchRoll(3.14f, 3.14f, 0f);

        public void Load(string assetsFolder, string outputsFolder)
        {
            string[] modelNames =
            [
                Path.Combine(outputsFolder, model1Name),
                Path.Combine(outputsFolder, model2Name),
            ];

            if (modelNames.Any(f => !File.Exists(f)))
            {
                string[] assets = [.. AssimpImporter.Read(modelHumvee, new(), assetsFolder)];
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

                new(() => { iblBrdfLutId = ITestRenderItem.LoadTexture(Path.Combine(outputsFolder, iblBrdfLutTextureName)); }),
                new(() => { iblDiffuseId = ITestRenderItem.LoadTexture(Path.Combine(outputsFolder, iblDiffuseTextureName)); }),
                new(() => { iblSpecularId = ITestRenderItem.LoadTexture(Path.Combine(outputsFolder, iblSpecularTextureName)); }),

                new(TestShader.Load));

            LightGenerator.CreateIblLight(iblBrdfLutId, iblDiffuseId, iblSpecularId);

            // NOTE: we need shaders to be ready before creating materials
            CreateMaterial();

            {
                GeometryInfo geometryInfo = new()
                {
                    MaterialIds = [mtlId],
                    GeometryContentId = model1Id
                };
                entity1Id = HelloWorldApp.CreateOneGameEntity<RotatorScript>(Vector3.Zero, Quaternion.Identity, geometryInfo).Id;
            }
            {
                GeometryInfo geometryInfo = new()
                {
                    MaterialIds = [mtlId],
                    GeometryContentId = model2Id
                };
                entity2Id = HelloWorldApp.CreateOneGameEntity<RotatorScript>(Vector3.Zero, Quaternion.Identity, geometryInfo).Id;
            }
        }
        void CreateMaterial()
        {
            Debug.Assert(IdDetail.IsValid(TestShader.VsId) && IdDetail.IsValid(TestShader.PsId));

            MaterialInitInfo info = new();
            info.ShaderIds[(uint)ShaderTypes.Vertex] = TestShader.VsId;
            info.ShaderIds[(uint)ShaderTypes.Pixel] = TestShader.PsId;
            info.Type = MaterialTypes.Opaque;

            info.Surface.BaseColor = new(0.5f, 0.1f, 0.5f, 1f);
            info.Surface.Roughness = 0.2f;
            info.Surface.Metallic = 1.0f;

            mtlId = ContentToEngine.CreateResource(info, AssetTypes.Material);
        }

        public void DestroyRenderItems()
        {
            HelloWorldApp.RemoveGameEntity(entity1Id);
            HelloWorldApp.RemoveGameEntity(entity2Id);

            ITestRenderItem.RemoveModel(model1Id);
            ITestRenderItem.RemoveModel(model2Id);

            // remove material
            if (IdDetail.IsValid(mtlId))
            {
                ContentToEngine.DestroyResource(mtlId, AssetTypes.Material);
            }

            // remove textures
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
