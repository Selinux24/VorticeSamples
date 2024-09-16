
namespace Engine.Graphics
{
    public abstract class Graphics
    {
        public abstract void Shutdown();

        public abstract ISurface CreateSurface(Window window);
        public abstract void RemoveSurface(int id);
        public abstract void ResizeSurface(int id, uint width, uint height);
        public abstract void RenderSurface(int id, IFrameInfo info);

        public abstract ICamera CreateCamera(ICameraInitInfo info);
        public abstract void RemoveCamera(int id);
        public abstract void SetCameraParameter<T>(int id, ICameraParameters parameter, T value);
        public abstract T GetCameraParameter<T>(int id, ICameraParameters parameter);

        public abstract void CreateLightSet(ulong lightSetKey);
        public abstract void RemoveLightSet(ulong lightSetKey);
        public abstract ILight CreateLight(ILightInitInfo info);
        public abstract void RemoveLight(int id, ulong lightSetKey);
        public abstract void SetLightParameter<T>(int id, ulong lightSetKey, ILightParameters parameter, T value);
        public abstract T GetLightParameter<T>(int id, ulong lightSetKey, ILightParameters parameter);

        public abstract int AddSubmesh(byte[] data);
        public abstract void RemoveSubmesh(int id);
        public abstract int AddTexture(byte[] data);
        public abstract void RemoveTexture(int id);
        public abstract int AddMaterial(IMaterialInfo info);
        public abstract void RemoveMaterial(int id);
        public abstract int AddRenderItem(int entityId, int geometryContentId, int materialCount, int[] materialIds);
        public abstract void RemoveRenderItem(int id);
    }
}
