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
    }
}
