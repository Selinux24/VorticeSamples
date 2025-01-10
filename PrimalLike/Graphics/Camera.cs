using PrimalLike.Common;
using System.Numerics;
using System.Runtime.InteropServices;

namespace PrimalLike.Graphics
{
    public class Camera
    {
        private readonly CameraId id = CameraId.MaxValue;

        public Camera()
        {

        }
        public Camera(CameraId id)
        {
            this.id = id;
        }

        public CameraId Id { get => id; }
        public bool IsValid { get => IdDetail.IsValid(id); }

        private static T GetValue<T>(CameraId id, CameraParameters parameter) where T : unmanaged
        {
            int size = Marshal.SizeOf<T>();
            nint ptr = Marshal.AllocHGlobal(size);
            Renderer.Gfx.GetParameter(id, parameter, ptr, size);
            return Marshal.PtrToStructure<T>(ptr);
        }
        private static void SetValue<T>(CameraId id, CameraParameters parameter, T value) where T : unmanaged
        {
            int size = Marshal.SizeOf<T>();
            nint ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(value, ptr, false);
            Renderer.Gfx.SetParameter(id, parameter, ptr, size);
        }

        public Vector3 Up
        {
            get
            {
                return GetValue<Vector3>(id, CameraParameters.UpVector);
            }
            set
            {
                SetValue(id, CameraParameters.UpVector, value);
            }
        }
        public float FieldOfView
        {
            get
            {
                return GetValue<float>(id, CameraParameters.FieldOfView);
            }
            set
            {
                SetValue(id, CameraParameters.FieldOfView, value);
            }
        }
        public float AspectRatio
        {
            get
            {
                return GetValue<float>(id, CameraParameters.AspectRatio);
            }
            set
            {
                SetValue(id, CameraParameters.AspectRatio, value);
            }
        }
        public float ViewWidth
        {
            get
            {
                return GetValue<float>(id, CameraParameters.ViewWidth);
            }
            set
            {
                SetValue(id, CameraParameters.ViewWidth, value);
            }
        }
        public float ViewHeight
        {
            get
            {
                return GetValue<float>(id, CameraParameters.ViewHeight);
            }
            set
            {
                SetValue(id, CameraParameters.ViewHeight, value);
            }
        }

        public Matrix4x4 View
        {
            get
            {
                return GetValue<Matrix4x4>(id, CameraParameters.View);
            }
        }
        public Matrix4x4 Projection
        {
            get
            {
                return GetValue<Matrix4x4>(id, CameraParameters.Projection);
            }
        }
        public Matrix4x4 InverseProjection
        {
            get
            {
                return GetValue<Matrix4x4>(id, CameraParameters.InverseProjection);
            }
        }
        public Matrix4x4 ViewProjection
        {
            get
            {
                return GetValue<Matrix4x4>(id, CameraParameters.ViewProjection);
            }
        }
        public Matrix4x4 InverseViewProjection
        {
            get
            {
                return GetValue<Matrix4x4>(id, CameraParameters.InverseViewProjection);
            }
        }
        public float NearZ
        {
            get
            {
                return GetValue<float>(id, CameraParameters.NearZ);
            }
            set
            {
                SetValue(id, CameraParameters.NearZ, value);
            }
        }
        public float FarZ
        {
            get
            {
                return GetValue<float>(id, CameraParameters.FarZ);
            }
            set
            {
                SetValue(id, CameraParameters.FarZ, value);
            }
        }
        public CameraProjectionTypes ProjectionType
        {
            get
            {
                return (CameraProjectionTypes)GetValue<uint>(id, CameraParameters.ProjectionType);
            }
        }
        public EntityId EntityId
        {
            get
            {
                return GetValue<EntityId>(id, CameraParameters.EntityId);
            }
        }
    }
}
