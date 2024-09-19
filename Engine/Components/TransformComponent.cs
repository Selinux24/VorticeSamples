global using TransformId = uint;
using Engine.Common;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;

namespace Engine.Components
{
    static class TransformComponent
    {
        public static readonly List<Matrix4x4> ToWorld = [];
        public static readonly List<Matrix4x4> InvWorld = [];
        public static readonly List<Quaternion> Rotations = [];
        public static readonly List<Vector3> Orientations = [];
        public static readonly List<Vector3> Positions = [];
        public static readonly List<Vector3> Scales = [];
        public static readonly List<ushort> HasTransforms = [];
        public static readonly List<TransformFlags> ChangesFromPreviousFrame = [];
        public static ushort ReadWriteFlag = 0;

        public static void CalculateTransformMatrices(IdType index)
        {
            Debug.Assert(Rotations.Count >= index);
            Debug.Assert(Positions.Count >= index);
            Debug.Assert(Scales.Count >= index);

            var r = Rotations[(int)index];
            var t = Positions[(int)index];
            var s = Scales[(int)index];

            Matrix4x4 world = Matrix4x4.CreateScale(s) * Matrix4x4.CreateFromQuaternion(r) * Matrix4x4.CreateTranslation(t);
            ToWorld[(int)index] = world;

            world.M41 = 0f;
            world.M42 = 0f;
            world.M43 = 0f;
            world.M44 = 1f;
            Matrix4x4.Invert(world, out Matrix4x4 invWorld);
            InvWorld[(int)index] = invWorld;

            HasTransforms[(int)index] = 1;
        }
        public static Vector3 CalculateOrientation(Quaternion rotation)
        {
            return Vector3.Transform(Vector3.UnitZ, rotation);
        }

        public static void SetRotation(TransformId id, Quaternion rotation)
        {
            IdType index = IdDetail.Index(id);
            Rotations[(int)index] = rotation;
            Orientations[(int)index] = CalculateOrientation(rotation);
            HasTransforms[(int)index] = 0;
            ChangesFromPreviousFrame[(int)index] |= TransformFlags.Rotation;
        }
        public static void SetPosition(TransformId id, Vector3 position)
        {
            IdType index = IdDetail.Index(id);
            Positions[(int)index] = position;
            HasTransforms[(int)index] = 0;
            ChangesFromPreviousFrame[(int)index] |= TransformFlags.Position;
        }
        public static void SetScale(TransformId id, Vector3 scale)
        {
            IdType index = IdDetail.Index(id);
            Scales[(int)index] = scale;
            HasTransforms[(int)index] = 0;
            ChangesFromPreviousFrame[(int)index] |= TransformFlags.Scale;
        }

        public static Transform Create(TransformInfo info, Entity entity)
        {
            Debug.Assert(entity.IsValid());
            IdType entityIndex = IdDetail.Index(entity.Id);

            if (Positions.Count > entityIndex)
            {
                Positions[(int)entityIndex] = info.Position;
                Rotations[(int)entityIndex] = info.Rotation;
                Scales[(int)entityIndex] = info.Scale;
            }
            else
            {
                Debug.Assert(Positions.Count == entityIndex);
                Positions.Add(info.Position);
                Rotations.Add(info.Rotation);
                Scales.Add(info.Scale);
            }

            return new(entity.Id);
        }
        public static void Remove(Transform c)
        {
            Debug.Assert(c.IsValid());
        }

        public static TransformFlags[] GetUpdatedComponentFlags(EntityId[] ids)
        {
            Debug.Assert(ids.Length > 0);
            ReadWriteFlag = 1;

            TransformFlags[] flags = new TransformFlags[ids.Length];

            for (int i = 0; i < ids.Length; i++)
            {
                Debug.Assert(IdDetail.IsValid(ids[i]));
                flags[i] = ChangesFromPreviousFrame[(int)IdDetail.Index(ids[i])];
            }

            return flags;
        }
    }
}
