using PrimalLike.Common;
using PrimalLike.Components;
using PrimalLike.EngineAPI;
using System;
using System.Diagnostics;
using System.Numerics;
using System.Threading;

namespace PrimalLikeDLL
{
    public class EntityAPI
    {
        readonly Lock mutex = new();

        TransformCache[] transformCache = [];

        static Vector3 GetEulerAnglesFromLocalFrame(Matrix4x4 m)
        {
            float pitch;
            float yaw;
            float roll;

            // Assuming front is Z, up is Y and right is X
            if (m.M32 < 1f)
            {
                if (m.M32 > -1f)
                {
                    pitch = MathF.Asin(-m.M32);
                    yaw = MathF.Atan2(m.M31, m.M33);
                    roll = MathF.Atan2(m.M12, m.M22);
                }
                else
                {
                    // m._32 == -1
                    pitch = MathF.PI * 0.5f;
                    yaw = -MathF.Atan2(-m.M13, m.M11);
                    roll = 0f;
                }
            }
            else
            {
                // m._32 == +1
                pitch = -MathF.PI * 0.5f;
                yaw = MathF.Atan2(-m.M13, m.M11);
                roll = 0f;
            }

            return Vector3.RadiansToDegrees(new Vector3(pitch, yaw, roll));
        }
        static Vector3 GetLocalPos(Vector3 pos, uint id)
        {
            PrimalLike.EngineAPI.TransformComponent xform = new(id);
            return xform.CalculateLocalPosition(pos);
        }
        static Quaternion GetWorldQuat(Vector3 w, uint id)
        {
            w = Vector3.DegreesToRadians(w);
            PrimalLike.EngineAPI.TransformComponent xform = new(id);
            return xform.CalculateWorldRotation(w);
        }
        static Quaternion GetLocalQuat(Vector3 w, uint id)
        {
            w = Vector3.DegreesToRadians(w);
            PrimalLike.EngineAPI.TransformComponent xform = new(id);
            return xform.CalculateLocalRotation(w);
        }
        static Quaternion GetAbsoluteQuat(Vector3 w, uint id)
        {
            w = Vector3.DegreesToRadians(w);
            PrimalLike.EngineAPI.TransformComponent xform = new(id);
            return xform.CalculateAbsoluteRotation(w);
        }

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

        public void GetPosition(uint[] ids, float[] x, float[] y, float[] z, uint count)
        {
            Debug.Assert(ids != null && count > 0);
            lock (mutex)
            {
                for (uint i = 0; i < count; i++)
                {
                    Debug.Assert(ids[i] == new Entity(ids[i]).Transform.Id);
                    // NOTE: transform id is the same as entity id
                    uint id = ids[i];
                    PrimalLike.EngineAPI.TransformComponent t = new(id);
                    var pos = t.Position;
                    x[i] = pos.X;
                    y[i] = pos.Y;
                    z[i] = pos.Z;
                }
            }
        }
        public void GetRotation(uint[] ids, float[] x, float[] y, float[] z, uint count)
        {
            Debug.Assert(ids != null && count > 0);
            lock (mutex)
            {
                for (uint i = 0; i < count; i++)
                {
                    Debug.Assert(ids[i] == new Entity(ids[i]).Transform.Id);
                    // NOTE: transform id is the same as entity id
                    uint id = ids[i];
                    PrimalLike.EngineAPI.TransformComponent t = new(id);
                    var euler = GetEulerAnglesFromLocalFrame(t.LocalFrame);
                    x[i] = euler.X;
                    y[i] = euler.Y;
                    z[i] = euler.Z;
                }
            }
        }
        public void GetScale(uint[] ids, float[] x, float[] y, float[] z, uint count)
        {
            Debug.Assert(ids != null && count > 0);
            lock (mutex)
            {
                for (uint i = 0; i < count; i++)
                {
                    Debug.Assert(ids[i] == new Entity(ids[i]).Transform.Id);
                    // NOTE: transform id is the same as entity id
                    uint id = ids[i];
                    PrimalLike.EngineAPI.TransformComponent t = new(id);
                    var scale = t.Scale;
                    x[i] = scale.X;
                    y[i] = scale.Y;
                    z[i] = scale.Z;
                }
            }
        }

        public void SetPosition(uint[] ids, float[] x, float[] y, float[] z, uint count, bool isLocal)
        {
            Debug.Assert(ids != null && count > 0);
            lock (mutex)
            {
                Array.Resize(ref transformCache, (int)count);

                for (int i = 0; i < count; i++)
                {
                    Vector3 v = new(x[i], y[i], z[i]);

                    var trn = transformCache[i];
                    trn.Position = !isLocal ? v : GetLocalPos(v, ids[i]);
                    trn.Flags = TransformFlags.Position;
                    Debug.Assert(ids[i] == new Entity(ids[i]).Transform.Id);
                    // NOTE: transform id is the same as entity id
                    trn.Id = ids[i];
                    transformCache[i] = trn;
                }

                Transform.Update(transformCache);
            }
        }
        public void SetRotation(uint[] ids, float[] x, float[] y, float[] z, uint count, uint frame)
        {
            Debug.Assert(ids != null && count > 0);
            lock (mutex)
            {
                Array.Resize(ref transformCache, (int)count);

                var frameFn = GetAbsoluteQuat;
                if (frame == (uint)TransformSpace.Local)
                {
                    frameFn = GetLocalQuat;
                }
                else if (frame == (uint)TransformSpace.World)
                {
                    frameFn = GetWorldQuat;
                }

                for (int i = 0; i < count; i++)
                {
                    Vector3 v = new(x[i], y[i], z[i]);

                    var trn = transformCache[i];
                    trn.Rotation = frameFn(v, ids[i]);
                    trn.Flags = TransformFlags.Rotation;
                    Debug.Assert(ids[i] == new Entity(ids[i]).Transform.Id);
                    // NOTE: transform id is the same as entity id
                    trn.Id = ids[i];
                    transformCache[i] = trn;
                }

                Transform.Update(transformCache);
            }
        }
    }
}
