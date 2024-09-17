
namespace Engine.Graphics
{
    public abstract class Graphics
    {
        public abstract void Shutdown();

        public abstract ISurface CreateSurface(Window window);
        public abstract void RemoveSurface(IdType id);
        public abstract void ResizeSurface(IdType id, uint width, uint height);
        public abstract void RenderSurface(IdType id, IFrameInfo info);

        public abstract ICamera CreateCamera(ICameraInitInfo info);
        public abstract void RemoveCamera(IdType id);
        public abstract void SetCameraParameter<T>(IdType id, ICameraParameters parameter, T value);
        public abstract T GetCameraParameter<T>(IdType id, ICameraParameters parameter);

        public abstract void CreateLightSet(ulong lightSetKey);
        public abstract void RemoveLightSet(ulong lightSetKey);
        public abstract ILight CreateLight(ILightInitInfo info);
        public abstract void RemoveLight(IdType id, ulong lightSetKey);
        public abstract void SetLightParameter<T>(IdType id, ulong lightSetKey, ILightParameters parameter, T value);
        public abstract T GetLightParameter<T>(IdType id, ulong lightSetKey, ILightParameters parameter);

        public abstract IdType AddSubmesh(byte[] data);
        public abstract void RemoveSubmesh(IdType id);
        public abstract IdType AddTexture(byte[] data);
        public abstract void RemoveTexture(IdType id);
        public abstract IdType AddMaterial(IMaterialInfo info);
        public abstract void RemoveMaterial(IdType id);
        public abstract IdType AddRenderItem(IdType entityId, IdType geometryContentId, IdType[] materialIds);
        public abstract void RemoveRenderItem(IdType id);
    }
}
