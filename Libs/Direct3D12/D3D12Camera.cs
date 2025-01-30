﻿using PrimalLike.Common;
using PrimalLike.EngineAPI;
using System;
using System.Diagnostics;
using System.Numerics;
using Utilities;

namespace Direct3D12
{
    public class D3D12Camera
    {
        private static readonly FreeList<D3D12Camera> cameras = new();

        delegate void SetFunction(D3D12Camera camera, object value);
        delegate object GetFunction(D3D12Camera camera);
        private static readonly SetFunction[] setFunctions =
        [
            (camera, value)=>SetUpVector(camera, (Vector3)value),
            (camera, value)=>SetFieldOfView(camera, (float)value),
            (camera, value)=>SetAspectRatio(camera, (float)value),
            (camera, value)=>SetViewWidth(camera, (float)value),
            (camera, value)=>SetViewHeight(camera, (float)value),
            (camera, value)=>SetNearZ(camera, (float)value),
            (camera, value)=>SetFarZ(camera, (float)value),
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
            (camera)=>GetUpVector(camera),
            (camera)=>GetFieldOfView(camera),
            (camera)=>GetAspectRatio(camera),
            (camera)=>GetViewWidth(camera),
            (camera)=>GetViewHeight(camera),
            (camera)=>GetNearZ(camera),
            (camera)=>GetFarZ(camera),
            (camera)=>GetView(camera),
            (camera)=>GetProjection(camera),
            (camera)=>GetInverseProjection(camera),
            (camera)=>GetViewProjection(camera),
            (camera)=>GetInverseViewProjection(camera),
            (camera)=>GetProjectionType(camera),
            (camera)=>GetEntityId(camera),
        ];

        public static Camera Create(CameraInitInfo info)
        {
            uint id = cameras.Add(new D3D12Camera(info));

            return new Camera(id);
        }
        public static void Remove(uint id)
        {
            Debug.Assert(IdDetail.IsValid(id));
            cameras.Remove(id);
        }
        public static void SetParameter<T>(uint id, CameraParameters parameter, T value) where T : unmanaged
        {
            Debug.Assert((uint)parameter < (uint)CameraParameters.Count);
            Debug.Assert(setFunctions[(uint)parameter] != DummySet);
            D3D12Camera camera = Get(id);
            setFunctions[(int)parameter](camera, value);
        }
        public static void GetParameter<T>(uint id, CameraParameters parameter, out T value) where T : unmanaged
        {
            Debug.Assert((uint)parameter < (uint)CameraParameters.Count);
            D3D12Camera camera = Get(id);
            value = (T)getFunctions[(int)parameter](camera);
        }
        public static D3D12Camera Get(uint id)
        {
            Debug.Assert(IdDetail.IsValid(id));
            return cameras[id];
        }

        private static void SetUpVector(D3D12Camera camera, Vector3 value)
        {
            camera.Up = value;
        }
        private static void SetFieldOfView(D3D12Camera camera, float value)
        {
            camera.FieldOfView = value;
        }
        private static void SetAspectRatio(D3D12Camera camera, float value)
        {
            camera.AspectRatio = value;
        }
        private static void SetViewWidth(D3D12Camera camera, float value)
        {
            camera.ViewWidth = value;
        }
        private static void SetViewHeight(D3D12Camera camera, float value)
        {
            camera.ViewHeight = value;
        }
        private static void SetNearZ(D3D12Camera camera, float value)
        {
            camera.NearZ = value;
        }
        private static void SetFarZ(D3D12Camera camera, float value)
        {
            camera.FarZ = value;
        }
        private static void DummySet(D3D12Camera camera, object value)
        {

        }

        private static Matrix4x4 GetView(D3D12Camera camera)
        {
            return camera.View;
        }
        private static Matrix4x4 GetProjection(D3D12Camera camera)
        {
            return camera.Projection;
        }
        private static Matrix4x4 GetInverseProjection(D3D12Camera camera)
        {
            return camera.InverseProjection;
        }
        private static Matrix4x4 GetViewProjection(D3D12Camera camera)
        {
            return camera.ViewProjection;
        }
        private static Matrix4x4 GetInverseViewProjection(D3D12Camera camera)
        {
            return camera.InverseViewProjection;
        }
        private static Vector3 GetUpVector(D3D12Camera camera)
        {
            return camera.Up;
        }
        private static float GetFieldOfView(D3D12Camera camera)
        {
            Debug.Assert(camera.ProjectionType == CameraProjectionTypes.Perspective);
            return camera.FieldOfView;
        }
        private static float GetAspectRatio(D3D12Camera camera)
        {
            Debug.Assert(camera.ProjectionType == CameraProjectionTypes.Perspective);
            return camera.AspectRatio;
        }
        private static float GetViewWidth(D3D12Camera camera)
        {
            Debug.Assert(camera.ProjectionType == CameraProjectionTypes.Orthographic);
            return camera.ViewWidth;
        }
        private static float GetViewHeight(D3D12Camera camera)
        {
            Debug.Assert(camera.ProjectionType == CameraProjectionTypes.Orthographic);
            return camera.ViewHeight;
        }
        private static float GetNearZ(D3D12Camera camera)
        {
            return camera.NearZ;
        }
        private static float GetFarZ(D3D12Camera camera)
        {
            return camera.FarZ;
        }
        private static CameraProjectionTypes GetProjectionType(D3D12Camera camera)
        {
            return camera.ProjectionType;
        }
        private static uint GetEntityId(D3D12Camera camera)
        {
            return camera.EntityId;
        }

        private Matrix4x4 view;
        private Matrix4x4 projection;
        private Matrix4x4 inverseProjection;
        private Matrix4x4 viewProjection;
        private Matrix4x4 inverseViewProjection;
        private Vector3 up;
        private float nearZ;
        private float farZ;
        private float fieldOfView; // The field of view for perspective camera or view width in pixels for orthographic camera
        private float aspectRatio; // Width/height aspect ratio for perspective camera or view height in pixels for orthographic camera
        private CameraProjectionTypes projectionType;
        private uint entityId;
        private bool isDirty;

        public Matrix4x4 View { get => view; }
        public Matrix4x4 Projection { get => projection; }
        public Matrix4x4 InverseProjection { get => inverseProjection; }
        public Matrix4x4 ViewProjection { get => viewProjection; }
        public Matrix4x4 InverseViewProjection { get => inverseViewProjection; }
        public Vector3 Position { get; private set; }
        public Vector3 Direction { get; private set; }
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
            Position = entity.Transform.Position;
            Direction = entity.Transform.Orientation;
            view = Matrix4x4.CreateLookTo(Position, Direction, up);

            if (isDirty)
            {
                // NOTE: _near_z and _far_z are swapped because we use inverse depth in d3d12 renderer.
                //projection = (projectionType == CameraProjectionTypes.Perspective) ?
                //    Matrix4x4.CreatePerspectiveFieldOfView(FieldOfView * MathF.PI, AspectRatio, farZ, nearZ) :
                //    Matrix4x4.CreateOrthographic(ViewWidth, ViewHeight, farZ, nearZ);
                projection = (projectionType == CameraProjectionTypes.Perspective) ?
                    XMMatrixPerspectiveFovRH(FieldOfView * MathF.PI, AspectRatio, farZ, nearZ) :
                    XMMatrixOrthographicRH(ViewWidth, ViewHeight, farZ, nearZ);
                Matrix4x4.Invert(projection, out inverseProjection);
                isDirty = false;
            }

            viewProjection = Matrix4x4.Multiply(view, projection);
            Matrix4x4.Invert(viewProjection, out inverseViewProjection);
        }

        private static Matrix4x4 XMMatrixPerspectiveFovRH(float fovY, float aspectRatio, float nearZ, float farZ)
        {
            float h = 1.0f / MathF.Tan(fovY / 2.0f);
            float w = h / aspectRatio;
            float q = farZ / (nearZ - farZ);

            return new Matrix4x4(
                w, 0, 0, 0,
                0, h, 0, 0,
                0, 0, q, -1,
                0, 0, q * nearZ, 0
            );
        }
        private static Matrix4x4 XMMatrixOrthographicRH(float viewWidth, float viewHeight, float nearZ, float farZ)
        {
            float w = 2.0f / viewWidth;
            float h = 2.0f / viewHeight;
            float q = 1.0f / (nearZ - farZ);

            return new Matrix4x4(
                w, 0, 0, 0,
                0, h, 0, 0,
                0, 0, q, 0,
                0, 0, q * nearZ, 1
            );
        }
    }
}
