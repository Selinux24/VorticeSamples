global using TransformId = uint;
using PrimalLike.Common;
using PrimalLike.EngineAPI;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;

namespace PrimalLike.Components
{
    public static class Transform
    {
        public struct LocalFrame()
        {
            public Vector3 Right = Vector3.UnitX;
            public Vector3 Up = Vector3.UnitY;
            public Vector3 Front = Vector3.UnitZ;
            public readonly Matrix4x4 Frame => Matrix4x4.CreateWorld(Vector3.Zero, Front, Up);

            public static LocalFrame CalculateLocalFrame(Quaternion rotation)
            {
                var right = Vector3.Normalize(Vector3.Transform(Vector3.UnitX, rotation));
                var up = Vector3.Normalize(Vector3.Transform(Vector3.UnitY, rotation));
                var front = Vector3.Normalize(Vector3.Cross(right, up));

                var fn = Vector3.Normalize(Vector3.Transform(Vector3.UnitZ, rotation));

                return new()
                {
                    Right = right,
                    Up = up,
                    Front = front
                };
            }
        }

        static readonly List<Matrix4x4> toWorld = [];
        static readonly List<Matrix4x4> invWorld = [];
        static readonly List<ushort> hasTransforms = [];
        static readonly List<TransformFlags> changesFromPreviousFrame = [];
        static ushort readWriteFlag = 0;

        public static List<Quaternion> Rotations { get; } = [];
        public static List<LocalFrame> LocalFrames { get; } = [];
        public static List<Vector3> Positions { get; } = [];
        public static List<Vector3> Scales { get; } = [];

        static void SetRotation(TransformId id, Quaternion rotation)
        {
            int index = (int)IdDetail.Index(id);

            Rotations[index] = rotation;
            LocalFrames[index] = LocalFrame.CalculateLocalFrame(rotation);

            hasTransforms[index] = 0;
            changesFromPreviousFrame[index] |= TransformFlags.Rotation;
        }
        static void SetPosition(TransformId id, Vector3 position)
        {
            int index = (int)IdDetail.Index(id);

            Positions[index] = position;

            hasTransforms[index] = 0;
            changesFromPreviousFrame[index] |= TransformFlags.Position;
        }
        static void SetScale(TransformId id, Vector3 scale)
        {
            int index = (int)IdDetail.Index(id);

            Scales[index] = scale;

            hasTransforms[index] = 0;
            changesFromPreviousFrame[index] |= TransformFlags.Scale;
        }

        public static TransformComponent Create(TransformInfo info, Entity entity)
        {
            Debug.Assert(entity.IsValid);
            int entityIndex = (int)IdDetail.Index(entity.Id);

            if (Positions.Count > entityIndex)
            {
                Positions[entityIndex] = info.Position;
                Rotations[entityIndex] = info.Rotation;
                LocalFrames[entityIndex] = LocalFrame.CalculateLocalFrame(info.Rotation);
                Scales[entityIndex] = info.Scale;
                hasTransforms[entityIndex] = 0;
                changesFromPreviousFrame[entityIndex] = TransformFlags.All;
            }
            else
            {
                Debug.Assert(Positions.Count == entityIndex);
                toWorld.Add(default);
                invWorld.Add(default);
                Rotations.Add(info.Rotation);
                LocalFrames.Add(LocalFrame.CalculateLocalFrame(info.Rotation));
                Positions.Add(info.Position);
                Scales.Add(info.Scale);
                hasTransforms.Add(0);
                changesFromPreviousFrame.Add(TransformFlags.All);
            }

            return new(entity.Id);
        }
        public static void Remove(TransformComponent c)
        {
            Debug.Assert(c.IsValid());
        }

        public static void GetTransformMatrices(EntityId id, out Matrix4x4 world, out Matrix4x4 inverseWorld)
        {
            Debug.Assert(IdDetail.IsValid(id));
            int index = (int)IdDetail.Index(id);

            if (hasTransforms[index] == 0)
            {
                CalculateTransformMatrices(index);
            }

            world = toWorld[index];
            inverseWorld = invWorld[index];
        }
        static void CalculateTransformMatrices(int index)
        {
            Debug.Assert(Rotations.Count > index);
            Debug.Assert(Positions.Count > index);
            Debug.Assert(Scales.Count > index);

            var r = Matrix4x4.CreateFromQuaternion(Rotations[index]);
            var t = Matrix4x4.CreateTranslation(Positions[index]);
            var s = Matrix4x4.CreateScale(Scales[index]);
            var world = s * r * t;
            toWorld[index] = world;

            world.M41 = 0f;
            world.M42 = 0f;
            world.M43 = 0f;
            world.M44 = 1f;
            Matrix4x4.Invert(world, out Matrix4x4 inverseWorld);
            invWorld[index] = inverseWorld;

            hasTransforms[index] = 1;
        }

        public static TransformFlags[] GetUpdatedComponentsFlags(EntityId[] ids)
        {
            Debug.Assert(ids.Length > 0);
            readWriteFlag = 1;

            TransformFlags[] flags = new TransformFlags[ids.Length];

            for (int i = 0; i < ids.Length; i++)
            {
                Debug.Assert(IdDetail.IsValid(ids[i]));

                int index = (int)IdDetail.Index(ids[i]);
                flags[i] = changesFromPreviousFrame[index];
            }

            return flags;
        }

        public static void Update(TransformCache[] cache)
        {
            Debug.Assert(cache.Length > 0);

            if (readWriteFlag != 0)
            {
                for (int i = 0; i < changesFromPreviousFrame.Count; i++)
                {
                    changesFromPreviousFrame[i] = 0;
                }
                readWriteFlag = 0;
            }

            for (int i = 0; i < cache.Length; i++)
            {
                var c = cache[i];
                Debug.Assert(IdDetail.IsValid(c.Id));

                if (c.Flags.HasFlag(TransformFlags.Rotation))
                {
                    SetRotation(c.Id, c.Rotation);
                }

                if (c.Flags.HasFlag(TransformFlags.Position))
                {
                    SetPosition(c.Id, c.Position);
                }

                if (c.Flags.HasFlag(TransformFlags.Scale))
                {
                    SetScale(c.Id, c.Scale);
                }
            }
        }
    }
}
