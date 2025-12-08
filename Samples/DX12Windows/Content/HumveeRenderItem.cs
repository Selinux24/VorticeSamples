using AssetsImporter;
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
using System.Threading;

namespace DX12Windows.Content
{
    class HumveeRenderItem : ITestRenderItem
    {
        private const string modelHumvee = "../../../../../Assets/humvee.obj";

        private const string model1Name = "humvee_modelA.model";
        private const string model2Name = "humvee_modelB.model";

        private uint model1Id = uint.MaxValue;
        private uint model2Id = uint.MaxValue;

        private uint entity1Id = uint.MaxValue;
        private uint entity2Id = uint.MaxValue;

        private uint mtlId = uint.MaxValue;

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
                string[] assets = [..AssimpImporter.Read(modelHumvee, new(), assetsFolder)];
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
            var _1 = new Thread(() => { model1Id = ITestRenderItem.LoadModel(Path.Combine(outputsFolder, model1Name)); });
            var _2 = new Thread(() => { model2Id = ITestRenderItem.LoadModel(Path.Combine(outputsFolder, model2Name)); });
            var _3 = new Thread(TestShaders.LoadShaders);

            _1.Start();
            _2.Start();
            _3.Start();

            _1.Join();
            _2.Join();
            _3.Join();

            GeometryInfo geometryInfo = new();

            // NOTE: we need shaders to be ready before creating materials
            CreateMaterial();

            geometryInfo.MaterialIds = [mtlId];

            geometryInfo.GeometryContentId = model1Id;
            entity1Id = HelloWorldApp.CreateOneGameEntity<RotatorScript>(Vector3.Zero, Quaternion.Identity, geometryInfo).Id;
           
            geometryInfo.GeometryContentId = model2Id;
            entity2Id = HelloWorldApp.CreateOneGameEntity<RotatorScript>(Vector3.Zero, Quaternion.Identity, geometryInfo).Id;
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
            HelloWorldApp.RemoveGameEntity(entity1Id);
            HelloWorldApp.RemoveGameEntity(entity2Id);

            ITestRenderItem.RemoveModel(model1Id);
            ITestRenderItem.RemoveModel(model2Id);

            // remove material
            if (IdDetail.IsValid(mtlId))
            {
                ContentToEngine.DestroyResource(mtlId, AssetTypes.Material);
            }

            // remove shaders and textures
            TestShaders.RemoveShaders();
        }
    }
}
