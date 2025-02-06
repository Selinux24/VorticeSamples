using PrimalLike.Common;
using PrimalLike.Content;
using System.Diagnostics;
using System.IO;
using System.Numerics;

namespace DX12Windows.Content
{
    interface ITestRenderItem
    {
        Vector3 InitialCameraPosition { get; }
        Quaternion InitialCameraRotation { get; }

        void Load(string assetsFolder, string outputsFolder);
        void DestroyRenderItems();
        uint[] GetRenderItems();

        public static uint LoadModel(string modelPath)
        {
            modelPath = Path.GetFullPath(modelPath);
            using var file = new MemoryStream(File.ReadAllBytes(modelPath));

            uint modelId = ContentToEngine.CreateResource(file, AssetTypes.Mesh);
            Debug.Assert(IdDetail.IsValid(modelId));

            return modelId;
        }
    }
}
