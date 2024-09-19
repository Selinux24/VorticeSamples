global using TransformId = uint;
using Engine.Common;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;

namespace Engine.Components
{
    static class TransformComponent
    {
        public static readonly List<Vector3> Positions = [];
        public static readonly List<Quaternion> Rotations = [];
        public static readonly List<Vector3> Scales = [];

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
    }
}
