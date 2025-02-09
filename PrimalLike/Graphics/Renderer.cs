using PrimalLike.Common;
using PrimalLike.EngineAPI;
using System;
using System.Diagnostics;

namespace PrimalLike.Graphics
{
    static class Renderer
    {
        private static IGraphicsPlatform gfx;

        private static void SetPlatformInterface(IGraphicsPlatformFactory graphicsFactory)
        {
            gfx = graphicsFactory.CreateGraphicsPlatform();
        }

        public static IGraphicsPlatform Gfx { get => gfx; }

        public static bool Initialize(IGraphicsPlatformFactory graphicsFactory)
        {
            SetPlatformInterface(graphicsFactory);

            return gfx.Initialize();
        }
        public static void Shutdown()
        {
            gfx.Shutdown();
        }

        public static string GetEngineShaderPath()
        {
            return gfx.GetEngineShaderPath();
        }

        public static Surface CreateSurface(Window window)
        {
            return gfx.CreateSurface(window);
        }
        public static void RemoveSurface(SurfaceId id)
        {
            Debug.Assert(IdDetail.IsValid(id));
            gfx.RemoveSurface(id);
        }

        public static Light CreateLight(LightInitInfo info)
        {
            return gfx.CreateLight(info);
        }
        public static void RemoveLight(LightId id, ulong lightSetKey)
        {
            gfx.RemoveLight(id, lightSetKey);
        }
        public static void SetParameter<T>(LightId id, ulong lightSetKey, LightParametersTypes parameter, T value) where T : unmanaged
        {
            gfx.SetParameter(id, lightSetKey, parameter, value);
        }
        public static void GetParameter<T>(LightId id, ulong lightSetKey, LightParametersTypes parameter, out T value) where T : unmanaged
        {
            gfx.GetParameter(id, lightSetKey, parameter, out value);
        }

        public static Camera CreateCamera(CameraInitInfo info)
        {
            return gfx.CreateCamera(info);
        }
        public static void RemoveCamera(CameraId id)
        {
            gfx.RemoveCamera(id);
        }
        public static void SetParameter<T>(CameraId id, CameraParameters parameter, T value) where T : unmanaged
        {
            gfx.SetParameter(id, parameter, value);
        }
        public static void GetParameter<T>(CameraId id, CameraParameters parameter, out T value) where T : unmanaged
        {
            gfx.GetParameter(id, parameter, out value);
        }

        public static IdType AddSubmesh(ref IntPtr data)
        {
            return gfx.AddSubmesh(ref data);
        }
        public static void RemoveSubmesh(IdType id)
        {
            gfx.RemoveSubmesh(id);
        }
        public static IdType AddMaterial(MaterialInitInfo info)
        {
            return gfx.AddMaterial(info);
        }
        public static void RemoveMaterial(IdType id)
        {
            gfx.RemoveMaterial(id);
        }
        public static IdType AddRenderItem(IdType entityId, IdType geometryContentId, IdType[] materialIds)
        {
            return gfx.AddRenderItem(entityId, geometryContentId, materialIds);
        }
        public static void RemoveRenderItem(IdType id)
        {
            gfx.RemoveRenderItem(id);
        }
    }
}
