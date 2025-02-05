using PrimalLike;
using PrimalLike.Common;
using PrimalLike.Components;
using PrimalLike.EngineAPI;
using PrimalLike.Graphics;
using PrimalLike.Platform;
using System.Diagnostics;
using System.Numerics;

namespace DX12Windows
{
    class HelloWorldApp(IPlatformFactory platformFactory, IGraphicsPlatformFactory graphicsFactory)
        : Application("Content/Game.bin", platformFactory, graphicsFactory)
    {
        public static HelloWorldApp Start<TPlatform, TGraphics>()
            where TPlatform : IPlatformFactory, new()
            where TGraphics : IGraphicsPlatformFactory, new()
        {
            return new HelloWorldApp(new TPlatform(), new TGraphics());
        }

        public static Entity CreateOneGameEntity(Vector3 position, Vector3 rotation, string scriptName)
        {
            TransformInfo transform = new()
            {
                Position = position,
                Rotation = Quaternion.CreateFromYawPitchRoll(rotation.X, rotation.Y, rotation.Z)
            };

            EntityInfo entityInfo = new()
            {
                Transform = transform,
            };

            if (!string.IsNullOrEmpty(scriptName))
            {
                entityInfo.Script = new()
                {
                    ScriptCreator = Script.GetScriptCreator(IdDetail.StringHash(scriptName)),
                };
            }

            Entity ntt = CreateEntity(entityInfo);
            Debug.Assert(ntt.IsValid);
            return ntt;
        }
    }
}
