using PrimalLike.Common;
using PrimalLike.Components;
using PrimalLike.EngineAPI;
using System.Diagnostics;
using System.Threading;

namespace PrimalLikeDLL
{
    public class EntityAPI
    {
        readonly Lock mutex = new();

        public uint CreateGameEntity(GameEntityDescriptor desc)
        {
            lock (mutex)
            {
                var transformInfo = desc.Transform.ToTransformInfo();
                var scriptInfo = desc.Script.ToScriptInfo();
                var geometryInfo = desc.Geometry.ToGeometryInfo();
                EntityInfo entityInfo = new()
                {
                    Transform = transformInfo,
                    Script = scriptInfo,
                    Geometry = IdDetail.IsValid(desc.Geometry.GeometryContentId) ? geometryInfo : null,
                };
                return GameEntity.Create(entityInfo).Id;
            }
        }
        public void RemoveGameEntity(uint id)
        {
            lock (mutex)
            {
                Debug.Assert(IdDetail.IsValid(id));
                GameEntity.Remove(id);
            }
        }
        public bool UpdateComponent(uint entityId, GameEntityDescriptor desc, ComponentTypes type)
        {
            lock (mutex)
            {
                Debug.Assert(IdDetail.IsValid(entityId) && type != ComponentTypes.Transform);
                var scriptInfo = desc.Script.ToScriptInfo();
                var geometryInfo = desc.Geometry.ToGeometryInfo();
                EntityInfo entityInfo = new()
                {
                    Script = scriptInfo,
                    Geometry = IdDetail.IsValid(desc.Geometry.GeometryContentId) ? geometryInfo : null,
                };

                return GameEntity.UpdateComponent(entityId, entityInfo, type);
            }
        }
        public uint GetComponentId(uint entityId, ComponentTypes type)
        {
            lock (mutex)
            {
                Debug.Assert(IdDetail.IsValid(entityId));
                var entity = new Entity(entityId);

                return type switch
                {
                    ComponentTypes.Transform => entity.Transform.Id,
                    ComponentTypes.Script => entity.Script.Id,
                    ComponentTypes.Geometry => entity.Geometry.Id,
                    _ => IdDetail.InvalidId,
                };
            }
        }
    }
}
