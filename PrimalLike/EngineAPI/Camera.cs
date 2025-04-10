﻿global using CameraId = System.UInt32;
using PrimalLike.Common;
using PrimalLike.Graphics;
using System.Numerics;

namespace PrimalLike.EngineAPI
{
    public class Camera
    {
        private readonly CameraId id;

        public Camera()
        {
            id = CameraId.MaxValue;
        }
        public Camera(CameraId id)
        {
            this.id = id;
        }

        public CameraId Id { get => id; }
        public bool IsValid { get => IdDetail.IsValid(id); }

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

        private static T GetValue<T>(CameraId id, CameraParameters parameter) where T : unmanaged
        {
            Renderer.Gfx.GetParameter(id, parameter, out T value);
            return value;
        }
        private static void SetValue<T>(CameraId id, CameraParameters parameter, T value) where T : unmanaged
        {
            Renderer.Gfx.SetParameter(id, parameter, value);
        }
    }
}
