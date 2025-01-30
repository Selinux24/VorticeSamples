using PrimalLike.Common;
using System.Diagnostics;
using System.Numerics;

namespace PrimalLike.EngineAPI
{
    public abstract record CameraInitInfo()
    {
        public EntityId EntityId = EntityId.MaxValue;
        public CameraProjectionTypes ProjectionType;
        public Vector3 Up;

        public float FieldOfView;
        public float ViewWidth;

        public float AspectRatio;
        public float ViewHeight;

        public float NearZ;
        public float FarZ;
    }

    public record PerspectiveCameraInitInfo : CameraInitInfo
    {
        public PerspectiveCameraInitInfo(EntityId id)
        {
            Debug.Assert(IdDetail.IsValid(id));
            EntityId = id;
            ProjectionType = CameraProjectionTypes.Perspective;
            Up = new Vector3(0f, 1f, 0f);
            FieldOfView = 0.25f;
            AspectRatio = 16f / 10f;
            NearZ = 0.01f;
            FarZ = 1000f;
        }
    }

    public record OrtographicCameraInitInfo : CameraInitInfo
    {
        public OrtographicCameraInitInfo(EntityId id)
        {
            Debug.Assert(IdDetail.IsValid(id));
            EntityId = id;
            ProjectionType = CameraProjectionTypes.Orthographic;
            Up = new Vector3(0f, 1f, 0f);
            ViewWidth = 1920;
            ViewHeight = 1080;
            NearZ = 0.01f;
            FarZ = 1000f;
        }
    }
}
