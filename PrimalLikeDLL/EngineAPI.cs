using PrimalLike.Common;
using PrimalLike.Components;
using PrimalLike.Content;
using PrimalLike.EngineAPI;
using PrimalLike.Graphics;
using PrimalLike.Platform;
using PrimalLikeDLL.Native;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;
using Utilities;

namespace PrimalLikeDLL
{
    public static class EngineAPI
    {
        readonly static Lock mutex = new();

        delegate IntPtr ScriptCreatorDelegate(string name);
        delegate string[] GetScriptNamesDelegate();

        static IntPtr gameCodeDll;
        static IPlatform platform;
        static IGraphicsPlatform gfx;
        static ScriptCreatorDelegate getScriptCreator;
        static GetScriptNamesDelegate getScriptNames;
        static readonly List<ViewportSurface> surfaces = [];

        static readonly Light[] lights = new Light[4];

        public static ErrorCode InitializeEngine(IPlatform platform, IGraphicsPlatform gfx)
        {
            if (!gfx.CompileShaders())
            {
                Console.WriteLine("Failed to compile engine shaders.");
                return ErrorCode.ShaderCompilation;
            }

            EngineAPI.platform = platform;
            EngineAPI.gfx = gfx;

            return gfx.Initialize() ? ErrorCode.Succeeded : ErrorCode.Graphics;
        }
        public static void ShutdownEngine()
        {
            // TEMPORARY //////////////////////////////////////////////////////////////////////////////////
            if (lights[0].IsValid) RemoveLights();
            // TEMPORARY //////////////////////////////////////////////////////////////////////////////////
            gfx.Shutdown();
        }

        public static bool LoadGameCodeDll(string dllPath)
        {
            if (gameCodeDll != IntPtr.Zero)
            {
                return false;
            }

            gameCodeDll = Kernel32.LoadLibrary(dllPath);
            if (gameCodeDll == IntPtr.Zero)
            {
                return false;
            }

            IntPtr scriptCreatorPtr = Kernel32.GetProcAddress(gameCodeDll, "get_script_creator");
            IntPtr scriptNamesPtr = Kernel32.GetProcAddress(gameCodeDll, "get_script_names");

            if (scriptCreatorPtr == IntPtr.Zero || scriptNamesPtr == IntPtr.Zero)
            {
                return false;
            }

            getScriptCreator = Marshal.GetDelegateForFunctionPointer<ScriptCreatorDelegate>(scriptCreatorPtr);
            getScriptNames = Marshal.GetDelegateForFunctionPointer<GetScriptNamesDelegate>(scriptNamesPtr);

            return true;
        }
        public static bool UnloadGameCodeDll()
        {
            if (gameCodeDll == IntPtr.Zero)
            {
                return false;
            }
            bool result = Kernel32.FreeLibrary(gameCodeDll);
            if (!result)
            {
                return false;
            }

            gameCodeDll = IntPtr.Zero;
            return true;
        }
        public static IntPtr GetScriptCreator(string name)
        {
            if (gameCodeDll == IntPtr.Zero || getScriptCreator == null)
            {
                return IntPtr.Zero;
            }

            return getScriptCreator(name);
        }
        public static string[] GetScriptNames()
        {
            if (gameCodeDll == IntPtr.Zero || getScriptNames == null)
            {
                return null;
            }

            return getScriptNames();
        }

        public static int CreateRenderSurface(IPlatformWindowInfo info)
        {
            lock (mutex)
            {
                // TEMPORARY //////////////////////////////////////////////////////////////////////////////////
                if (lights[0].IsValid) CreateLights();
                // TEMPORARY //////////////////////////////////////////////////////////////////////////////////

                var window = platform.CreateWindow(info);
                Debug.Assert(window.IsValid);

                var surface = new ViewportSurface()
                {
                    Window = window,
                    Surface = gfx.CreateSurface(window),
                    Camera = CreateCamera(),
                };

                surfaces.Add(surface);
                return surfaces.Count - 1;
            }
        }
        public static void RemoveRenderSurface(int id)
        {
            lock (mutex)
            {
                Debug.Assert(id < surfaces.Count);
                RemoveCamera(surfaces[id].Camera);
                gfx.RemoveSurface(surfaces[id].Surface.Id);
                platform.RemoveWindow(surfaces[id].Window.Id);
            }
        }
        public static void ResizeRenderSurface(int id)
        {
            lock (mutex)
            {
                Debug.Assert(id < surfaces.Count);
                var window = surfaces[id].Window;
                window.Resize(0, 0);
                surfaces[id].Surface.Resize(window.Width, window.Height);
                surfaces[id].Camera.AspectRatio = (float)window.Width / window.Height;
            }
        }
        public static IntPtr GetWindowHandle(int id)
        {
            lock (mutex)
            {
                Debug.Assert(id < surfaces.Count);
                return surfaces[id].Window.Handle;
            }
        }

        public static uint CreateResource(IntPtr data, AssetTypes type)
        {
            if (type == AssetTypes.Material)
            {
                data = PatchMaterialData(data);
            }

            return ContentToEngine.CreateResource(data, type);
        }
        public static void DestroyResource(uint id, AssetTypes type)
        {
            Debug.Assert(IdDetail.IsValid(id));
            ContentToEngine.DestroyResource(id, type);
        }
        static IntPtr PatchMaterialData(IntPtr data)
        {
            BlobStreamReader blob = new(data);
            uint textureCount = blob.Read<uint>();
            if (textureCount > 0)
            {
                // Skip texture IDs
                blob.Read<uint>((int)textureCount);
            }

            return blob.Position;
        }

        public static uint AddShaderGroup(ShaderGroupData data)
        {
            Debug.Assert(data.Count > 0 && data.DataSize > 0 && data.Data != IntPtr.Zero);
            uint count = data.Count;

            // data->data =
            // {
            //    u32 keys[count];
            //    struct{
            //      u64 bytecode_length;
            //      u8  hash[hash_length];
            //      u8  bytecode[bytecode_length];
            //    } blocks[count];
            // }
            //
            BlobStreamReader blob = new(data.Data);

            uint keyCount = blob.Read<uint>();
            uint[] keys = blob.Read<uint>((int)keyCount);

            CompiledShader[] shaderPointers = new CompiledShader[count];
            for (uint i = 0; i < count; i++)
            {
                // NOTE: byteCodeLength is a 64-bit value!
                ulong byteCodeSize = blob.Read<ulong>();
                byte[] hash = blob.Read((int)CompiledShader.HashLength);
                ReadOnlyMemory<byte> byteCode = blob.Read((int)byteCodeSize);
                shaderPointers[i] = new CompiledShader(byteCode, hash);
            }

            Debug.Assert(blob.Position == data.Data + data.DataSize);

            return ContentToEngine.AddShaderGroup(shaderPointers, keys);
        }
        public static void RemoveShaderGroup(uint id)
        {
            ContentToEngine.RemoveShaderGroup(id);
        }

        public static void SetGeometryIds(uint surfaceId, uint[] geometryIds)
        {
            lock (mutex)
            {
                Debug.Assert(surfaceId < surfaces.Count);

                var surface = surfaces[(int)surfaceId];
                surface.GeometryIds.Clear();
                if (geometryIds.Length > 0)
                {
                    surface.GeometryIds.AddRange(geometryIds);
                }
            }
        }
        public static void RenderFrame(uint surfaceId, uint cameraId, ulong lightSet = 0 /* TEMPORARY */)
        {
            lock (mutex)
            {
                Debug.Assert(surfaceId < surfaces.Count);

                var surface = surfaces[(int)surfaceId];

                uint count = (uint)surface.GeometryIds.Count;

                uint[] itemIds = [];
                float[] thresholds = [];
                if (count > 0)
                {
                    itemIds = Geometry.GetRenderItemIds([.. surface.GeometryIds]);
                    thresholds = CalculateThresholds([.. surface.GeometryIds], surfaceId);
                }

                FrameInfo info = new()
                {
                    RenderItemIds = itemIds,
                    RenderItemCount = count,
                    Thresholds = thresholds,
                    CameraId = !IdDetail.IsValid(cameraId) ? surface.Camera.Id : cameraId,
                    LightSetKey = lightSet
                };

                surface.Surface.Render(info);
            }
        }
        static float[] CalculateThresholds(uint[] geometryIds, uint surfaceId)
        {
            uint[] entityIds = Geometry.GetEntityIds(geometryIds);
            float[] thresholds = new float[entityIds.Length];

            Entity camera = new(surfaces[(int)surfaceId].Camera.EntityId);

            var cameraPos = camera.Position;

            for (uint i = 0; i < entityIds.Length; i++)
            {
                Debug.Assert(IdDetail.IsValid(entityIds[i]));
                var entityPos = new Entity(entityIds[i]).Position;
                thresholds[i] = Vector3.Distance(cameraPos, entityPos);
            }

            return thresholds;
        }

        static Entity CreateOneGameEntity(Vector3 position, Vector3 rotation, GeometryInfo? geometryInfo)
        {
            return CreateOneGameEntity(position, rotation, geometryInfo, Vector3.One);
        }
        static Entity CreateOneGameEntity(Vector3 position, Vector3 rotation, GeometryInfo? geometryInfo, Vector3 scale)
        {
            var rotQuat = Quaternion.CreateFromYawPitchRoll(rotation.Y, rotation.X, rotation.Z);

            EntityInfo entityInfo = new()
            {
                Geometry = geometryInfo,
                Transform = new()
                {
                    Rotation = rotQuat,
                    Position = position,
                    Scale = scale
                }
            };

            Entity ntt = GameEntity.Create(entityInfo);
            Debug.Assert(ntt.IsValid);
            return ntt;
        }
        static Entity CreateOneGameEntity<T>(Vector3 position, Vector3 rotation, GeometryInfo? geometryInfo) where T : EntityScript
        {
            return CreateOneGameEntity<T>(position, rotation, geometryInfo, Vector3.One);
        }
        static Entity CreateOneGameEntity<T>(Vector3 position, Vector3 rotation, GeometryInfo? geometryInfo, Vector3 scale) where T : EntityScript
        {
            if (!RegisterScript<T>())
            {
                Console.WriteLine($"Failed to register script {nameof(T)}");
            }

            var rotQuat = Quaternion.CreateFromYawPitchRoll(rotation.Y, rotation.X, rotation.Z);

            EntityInfo entityInfo = new()
            {
                Geometry = geometryInfo,
                Transform = new()
                {
                    Rotation = rotQuat,
                    Position = position,
                    Scale = scale
                },
                Script = new()
                {
                    ScriptCreator = Script.GetScriptCreator<T>(),
                }
            };

            Entity ntt = GameEntity.Create(entityInfo);
            Debug.Assert(ntt.IsValid);
            return ntt;
        }
        static bool RegisterScript<T>() where T : EntityScript
        {
            return Script.RegisterScript(
                IdDetail.StringHash<T>(),
                (entity) => (T)Activator.CreateInstance(typeof(T), [entity]));
        }

        static void CreateLights()
        {
            //Default lights setup
            gfx.CreateLightSet(0);

            LightInitInfo info = new()
            {
                LightType = LightTypes.Directional,
                LightSetKey = 0
            };

            {
                info.EntityId = CreateOneGameEntity(new(), new(0.23f, 5.28f, 0f), null).Id;
                info.Intensity = 0.5f;
                info.Color = RgbToColor(174, 174, 174);
                lights[0] = gfx.CreateLight(info);
            }
            {
                info.EntityId = CreateOneGameEntity(new(), new(0.23f, 5.28f - MathF.PI, 0f), null).Id;
                info.Intensity = 1f;
                lights[1] = gfx.CreateLight(info);
            }
            {
                info.EntityId = CreateOneGameEntity(new(), new(MathF.PI * 0.5f, 0, 0), null).Id;
                info.Intensity = 0.5f;
                info.Color = RgbToColor(17, 27, 48);
                lights[2] = gfx.CreateLight(info);
            }
            {
                info.EntityId = CreateOneGameEntity(new(), new(-MathF.PI * 0.5f, 0, 0), null).Id;
                info.Color = RgbToColor(63, 47, 30);
                lights[3] = gfx.CreateLight(info);
            }
        }
        static void RemoveLights()
        {
            for (uint i = 0; i < lights.Length; i++)
            {
                if (!lights[i].IsValid) continue;
                var id = lights[i].EntityId;
                gfx.RemoveLight(lights[i].Id, lights[i].LightSetKey);
                GameEntity.Remove(id);
            }

            gfx.RemoveLightSet(0);
        }
        static Vector3 RgbToColor(byte r, byte g, byte b)
        {
            return new(r / 255f, g / 255f, b / 255f);
        }

        static Camera CreateCamera()
        {
            Entity ntt = CreateOneGameEntity(new Vector3(0, 1, 10), new Vector3(0, -MathF.PI, 0), null);
            return gfx.CreateCamera(new PerspectiveCameraInitInfo(ntt.Id));
        }
        static void RemoveCamera(Camera camera)
        {
            uint id = camera.EntityId;
            gfx.RemoveCamera(camera.Id);
            GameEntity.Remove(id);
        }
    }
}
