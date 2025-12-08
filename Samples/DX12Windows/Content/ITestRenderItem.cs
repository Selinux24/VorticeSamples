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

        public static uint LoadModel(string modelPath)
        {
            return LoadResource(modelPath, AssetTypes.Mesh);
        }
        public static uint LoadTexture(string texturePath)
        {
            return LoadResource(texturePath, AssetTypes.Texture);
        }
        private static uint LoadResource(string path, AssetTypes assetType)
        {
            path = Path.GetFullPath(path);
            using var file = new MemoryStream(File.ReadAllBytes(path));

            uint id = ContentToEngine.CreateResource(file, assetType);
            Debug.Assert(IdDetail.IsValid(id));

            return id;
        }

        public static void RemoveModel(uint modelId)
        {
            if (IdDetail.IsValid(modelId))
            {
                ContentToEngine.DestroyResource(modelId, AssetTypes.Mesh);
            }
        }
    }
}
