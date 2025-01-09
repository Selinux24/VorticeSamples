using PrimalLike.Common;
using PrimalLike.EngineAPI;
using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using Utilities;

namespace Direct3D12
{
    public class D3D12Camera
    {
        private static readonly FreeList<D3D12Camera> cameras = new();

        delegate void SetFunction(D3D12Camera camera, IntPtr data, int size);
        delegate void GetFunction(D3D12Camera camera, IntPtr data, int size);
        private static readonly SetFunction[] setFunctions =
        [
            SetUpVector,
            SetFieldOfView,
            SetAspectRatio,
            SetViewWidth,
            SetViewHeight,
            SetNearZ,
            SetFarZ,
            DummySet,
            DummySet,
            DummySet,
            DummySet,
            DummySet,
            DummySet,
            DummySet,
        ];
        private static readonly GetFunction[] getFunctions =
        [
            GetUpVector,
            GetFieldOfView,
            GetAspectRatio,
            GetViewWidth,
            GetViewHeight,
            GetNearZ,
            GetFarZ,
            GetView,
            GetProjection,
            GetInverseProjection,
            GetViewProjection,
            GetInverseViewProjection,
            GetProjectionType,
            GetEntityId,
        ];

        public static Camera Create(CameraInitInfo info)
        {
            int id = cameras.Add(new D3D12Camera(info));

            return new Camera((uint)id);
        }
        public static void Remove(uint id)
        {
            Debug.Assert(IdDetail.IsValid(id));
            cameras.Remove((int)id);
        }
        public static void SetParameter(uint id, CameraParameters parameter, IntPtr data, int dataSize)
        {
            Debug.Assert(data != IntPtr.Zero && dataSize > 0);
            D3D12Camera camera = Get(id);
            setFunctions[(int)parameter](camera, data, dataSize);
        }
        public static void GetParameter(uint id, CameraParameters parameter, IntPtr data, int dataSize)
        {
            Debug.Assert(data != IntPtr.Zero && dataSize > 0);
            D3D12Camera camera = Get(id);
            getFunctions[(int)parameter](camera, data, dataSize);
        }
        public static D3D12Camera Get(uint id)
        {
            Debug.Assert(IdDetail.IsValid(id));
            return cameras[(int)id];
        }

        private static void SetUpVector(D3D12Camera camera, IntPtr data, int size)
        {
            //Read the up vector from the data pointer
            Vector3 upVector = Vector3.Zero;
            Marshal.PtrToStructure(data, upVector);
            Debug.Assert(Marshal.SizeOf(upVector) == size);
            camera.Up = upVector;
        }
        private static void SetFieldOfView(D3D12Camera camera, IntPtr data, int size)
        {
            Debug.Assert(camera.ProjectionType == CameraProjectionTypes.Perspective);
            float fov = 0f;
            Marshal.PtrToStructure(data, fov);
            Debug.Assert(Marshal.SizeOf(fov) == size);
            camera.FieldOfView = fov;
        }
        private static void SetAspectRatio(D3D12Camera camera, IntPtr data, int size)
        {
            Debug.Assert(camera.ProjectionType == CameraProjectionTypes.Perspective);
            float aspectRatio = 0f;
            Marshal.PtrToStructure(data, aspectRatio);
            Debug.Assert(Marshal.SizeOf(aspectRatio) == size);
            camera.AspectRatio = aspectRatio;
        }
        private static void SetViewWidth(D3D12Camera camera, IntPtr data, int size)
        {
            Debug.Assert(camera.ProjectionType == CameraProjectionTypes.Orthographic);
            float viewWidth = 0f;
            Marshal.PtrToStructure(data, viewWidth);
            Debug.Assert(Marshal.SizeOf(viewWidth) == size);
            camera.ViewWidth = viewWidth;
        }
        private static void SetViewHeight(D3D12Camera camera, IntPtr data, int size)
        {
            Debug.Assert(camera.ProjectionType == CameraProjectionTypes.Orthographic);
            float viewHeight = 0f;
            Marshal.PtrToStructure(data, viewHeight);
            Debug.Assert(Marshal.SizeOf(viewHeight) == size);
            camera.ViewHeight = viewHeight;
        }
        private static void SetNearZ(D3D12Camera camera, IntPtr data, int size)
        {
            float nearZ = 0f;
            Marshal.PtrToStructure(data, nearZ);
            Debug.Assert(Marshal.SizeOf(nearZ) == size);
            camera.NearZ = nearZ;
        }
        private static void SetFarZ(D3D12Camera camera, IntPtr data, int size)
        {
            float farZ = 0f;
            Marshal.PtrToStructure(data, farZ);
            Debug.Assert(Marshal.SizeOf(farZ) == size);
            camera.FarZ = farZ;
        }

        private static void GetView(D3D12Camera camera, IntPtr data, int size)
        {
            Marshal.StructureToPtr(camera.View, data, false);
            Debug.Assert(Marshal.SizeOf(typeof(Matrix4x4)) == size);
        }
        private static void GetProjection(D3D12Camera camera, IntPtr data, int size)
        {
            Marshal.StructureToPtr(camera.Projection, data, false);
            Debug.Assert(Marshal.SizeOf(typeof(Matrix4x4)) == size);
        }
        private static void GetInverseProjection(D3D12Camera camera, IntPtr data, int size)
        {
            Marshal.StructureToPtr(camera.InverseProjection, data, false);
            Debug.Assert(Marshal.SizeOf(typeof(Matrix4x4)) == size);
        }
        private static void GetViewProjection(D3D12Camera camera, IntPtr data, int size)
        {
            Marshal.StructureToPtr(camera.ViewProjection, data, false);
            Debug.Assert(Marshal.SizeOf(typeof(Matrix4x4)) == size);
        }
        private static void GetInverseViewProjection(D3D12Camera camera, IntPtr data, int size)
        {
            Marshal.StructureToPtr(camera.InverseViewProjection, data, false);
            Debug.Assert(Marshal.SizeOf(typeof(Matrix4x4)) == size);
        }
        private static void GetUpVector(D3D12Camera camera, IntPtr data, int size)
        {
            Marshal.StructureToPtr(camera.Up, data, false);
            Debug.Assert(Marshal.SizeOf(typeof(Vector3)) == size);
        }
        private static void GetFieldOfView(D3D12Camera camera, IntPtr data, int size)
        {
            Debug.Assert(camera.ProjectionType == CameraProjectionTypes.Perspective);
            Marshal.StructureToPtr(camera.FieldOfView, data, false);
            Debug.Assert(sizeof(float) == size);
        }
        private static void GetAspectRatio(D3D12Camera camera, IntPtr data, int size)
        {
            Debug.Assert(camera.ProjectionType == CameraProjectionTypes.Perspective);
            Marshal.StructureToPtr(camera.AspectRatio, data, false);
            Debug.Assert(sizeof(float) == size);
        }
        private static void GetViewWidth(D3D12Camera camera, IntPtr data, int size)
        {
            Debug.Assert(camera.ProjectionType == CameraProjectionTypes.Orthographic);
            Marshal.StructureToPtr(camera.ViewWidth, data, false);
            Debug.Assert(sizeof(float) == size);
        }
        private static void GetViewHeight(D3D12Camera camera, IntPtr data, int size)
        {
            Debug.Assert(camera.ProjectionType == CameraProjectionTypes.Orthographic);
            Marshal.StructureToPtr(camera.ViewHeight, data, false);
            Debug.Assert(sizeof(float) == size);
        }
        private static void GetNearZ(D3D12Camera camera, IntPtr data, int size)
        {
            Marshal.StructureToPtr(camera.NearZ, data, false);
            Debug.Assert(sizeof(float) == size);
        }
        private static void GetFarZ(D3D12Camera camera, IntPtr data, int size)
        {
            Marshal.StructureToPtr(camera.FarZ, data, false);
            Debug.Assert(sizeof(float) == size);
        }
        private static void GetProjectionType(D3D12Camera camera, IntPtr data, int size)
        {
            Marshal.StructureToPtr((uint)camera.ProjectionType, data, false);
            Debug.Assert(sizeof(CameraProjectionTypes) == size);
        }
        private static void GetEntityId(D3D12Camera camera, IntPtr data, int size)
        {
            Marshal.StructureToPtr(camera.EntityId, data, false);
            Debug.Assert(sizeof(uint) == size);
        }

        private static void DummySet(D3D12Camera camera, IntPtr data, int size)
        {

        }

        private Matrix4x4 view;
        private Matrix4x4 projection;
        private Matrix4x4 inverseProjection;
        private Matrix4x4 viewProjection;
        private Matrix4x4 inverseViewProjection;
        private Vector3 up;
        private float nearZ;
        private float farZ;
        /// <summary>
        /// The field of view for perspective camera or view width in pixels for orthographic camera
        /// </summary>
        private float fieldOfView;
        /// <summary>
        /// Width/height aspect ratio for perspective camera or view height in pixels for orthographic camera
        /// </summary>
        private float aspectRatio;
        private CameraProjectionTypes projectionType;
        private uint entityId;
        private bool isDirty;

        public Matrix4x4 View { get => view; }
        public Matrix4x4 Projection { get => projection; }
        public Matrix4x4 InverseProjection { get => inverseProjection; }
        public Matrix4x4 ViewProjection { get => viewProjection; }
        public Matrix4x4 InverseViewProjection { get => inverseViewProjection; }
        public Vector3 Up
        {
            get => up; set
            {
                up = value;
                isDirty = true;
            }
        }
        public float NearZ
        {
            get => nearZ; set
            {
                nearZ = value;
                isDirty = true;
            }
        }
        public float FarZ
        {
            get => farZ; set
            {
                farZ = value;
                isDirty = true;
            }
        }
        public float FieldOfView
        {
            get => fieldOfView; set
            {
                fieldOfView = value;
                isDirty = true;
            }
        }
        public float AspectRatio
        {
            get => aspectRatio; set
            {
                aspectRatio = value;
                isDirty = true;
            }
        }
        public float ViewWidth
        {
            get => fieldOfView; set
            {
                fieldOfView = value;
                isDirty = true;
            }
        }
        public float ViewHeight
        {
            get => aspectRatio; set
            {
                aspectRatio = value;
                isDirty = true;
            }
        }
        public CameraProjectionTypes ProjectionType
        {
            get => projectionType; set
            {
                projectionType = value;
                isDirty = true;
            }
        }
        public uint EntityId
        {
            get => entityId; set
            {
                entityId = value;
                isDirty = true;
            }
        }

        public D3D12Camera(CameraInitInfo info)
        {
            up = info.Up;
            nearZ = info.NearZ;
            farZ = info.FarZ;
            fieldOfView = info.FieldOfView;
            aspectRatio = info.AspectRatio;
            projectionType = info.ProjectionType;
            entityId = info.EntityId;
            isDirty = true;

            Debug.Assert(entityId != IdDetail.InvalidId);
            Update();
        }

        public void Update()
        {
            Entity entity = new(entityId);
            Vector3 position = entity.Transform.Position;
            Vector3 direction = entity.Transform.Orientation;
            view = Matrix4x4.CreateLookAt(position, direction, up);

            if (isDirty)
            {
                projection = (projectionType == CameraProjectionTypes.Perspective) ?
                    Matrix4x4.CreatePerspectiveFieldOfView(fieldOfView * MathF.PI, aspectRatio, nearZ, farZ) :
                    Matrix4x4.CreatePerspective(fieldOfView, aspectRatio, nearZ, farZ);
                Matrix4x4.Invert(projection, out inverseProjection);
                isDirty = false;
            }

            viewProjection = Matrix4x4.Multiply(view, projection);
            Matrix4x4.Invert(viewProjection, out inverseViewProjection);
        }
    }
}
