using PrimalLike.Common;
using PrimalLike.Components;
using System;
using System.Diagnostics;
using System.Numerics;
using Utilities;

namespace PrimalLike.EngineAPI
{
    public struct TransformComponent
    {
        public TransformId Id { get; private set; }
        public readonly Quaternion Rotation
        {
            get
            {
                Debug.Assert(IsValid());
                return Transform.Rotations[(int)IdDetail.Index(Id)];
            }
        }
        public readonly Vector3 Position
        {
            get
            {
                Debug.Assert(IsValid());
                return Transform.Positions[(int)IdDetail.Index(Id)];
            }
        }
        public readonly Vector3 Scale
        {
            get
            {
                Debug.Assert(IsValid());
                return Transform.Scales[(int)IdDetail.Index(Id)];
            }
        }
        public readonly Vector3 Right
        {
            get
            {
                Debug.Assert(IsValid());
                return Transform.LocalFrames[(int)IdDetail.Index(Id)].Right;
            }
        }
        public readonly Vector3 Up
        {
            get
            {
                Debug.Assert(IsValid());
                return Transform.LocalFrames[(int)IdDetail.Index(Id)].Up;
            }
        }
        public readonly Vector3 Front
        {
            get
            {
                Debug.Assert(IsValid());
                return Transform.LocalFrames[(int)IdDetail.Index(Id)].Front;
            }
        }
        public readonly Matrix4x4 LocalFrame
        {
            get
            {
                Debug.Assert(IsValid());
                return Transform.LocalFrames[(int)IdDetail.Index(Id)].Frame;
            }
        }

        public TransformComponent()
        {
            Id = TransformId.MaxValue;
        }
        public TransformComponent(TransformId id)
        {
            Id = id;
        }

        public readonly bool IsValid()
        {
            return IdDetail.IsValid(Id);
        }

        public readonly Vector3 CalculateLocalPosition(Vector3 delta)
        {
            Debug.Assert(IsValid());
            int index = (int)IdDetail.Index(Id);
            var frame = Transform.LocalFrames[index].Frame;
            var lPos = Vector3.Transform(delta, frame);
            var wPos = Transform.Positions[index];

            return wPos + lPos;
        }
        public readonly Quaternion CalculateAbsoluteRotation(Vector3 rotation)
        {
            Debug.Assert(IsValid());
            return Quaternion.CreateFromYawPitchRoll(rotation.Y, rotation.X, rotation.Z);
        }
        public readonly Quaternion CalculateLocalRotation(Vector3 delta)
        {
            Debug.Assert(IsValid());
            int index = (int)IdDetail.Index(Id);

            var d = Quaternion.CreateFromYawPitchRoll(delta.Y, delta.X, delta.Z);
            var q = Transform.Rotations[index];

            return Quaternion.Multiply(d, q);
        }
        public readonly Quaternion CalculateWorldRotation(Vector3 delta)
        {
            Debug.Assert(IsValid());

            var axis = Vector3.UnitX;
            float angle = delta.X;
            if (MathF.Abs(delta.X) < MathUtils.Epsilon)
            {
                if (MathF.Abs(delta.Z) < MathUtils.Epsilon)
                {
                    axis = Vector3.UnitY;
                    angle = delta.Y;
                }
                else if (MathF.Abs(delta.Y) < MathUtils.Epsilon)
                {
                    axis = Vector3.UnitZ;
                    angle = delta.Z;
                }
            }

            int index = (int)IdDetail.Index(Id);

            var d = Quaternion.CreateFromAxisAngle(axis, angle);
            var q = Transform.Rotations[index];

            return Quaternion.Multiply(q, d);
        }
    }
}
