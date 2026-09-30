using System;
using OpenTK.Mathematics;

namespace Tungsten.Rendering
{
    /// <summary>
    /// A standard yaw/pitch fly-camera. Used directly for the editor's free
    /// "Move" tool camera, and driven indirectly by FirstPersonController's
    /// head bone during Play mode so switching between edit/play feels seamless.
    /// </summary>
    public class Camera
    {
        public Vector3 Position;
        public float Yaw = -90f;   // degrees, -90 so default forward is -Z
        public float Pitch;
        public float Fov = 75f;
        public float AspectRatio = 16f / 9f;
        public float NearPlane = 0.05f;
        public float FarPlane = 1000f;

        public Vector3 Front { get; private set; } = -Vector3.UnitZ;
        public Vector3 Right { get; private set; } = Vector3.UnitX;
        public Vector3 Up { get; private set; } = Vector3.UnitY;

        public Camera(Vector3 position) => Position = position;

        public void AddLook(float yawDelta, float pitchDelta)
        {
            Yaw += yawDelta;
            Pitch = MathHelper.Clamp(Pitch + pitchDelta, -89f, 89f);
            RecalculateBasis();
        }

        private void RecalculateBasis()
        {
            float yawR = MathHelper.DegreesToRadians(Yaw);
            float pitchR = MathHelper.DegreesToRadians(Pitch);
            Vector3 front;
            front.X = MathF.Cos(pitchR) * MathF.Cos(yawR);
            front.Y = MathF.Sin(pitchR);
            front.Z = MathF.Cos(pitchR) * MathF.Sin(yawR);
            Front = Vector3.Normalize(front);
            Right = Vector3.Normalize(Vector3.Cross(Front, Vector3.UnitY));
            Up = Vector3.Normalize(Vector3.Cross(Right, Front));
        }

        public Matrix4 GetViewMatrix() => Matrix4.LookAt(Position, Position + Front, Up);

        public Matrix4 GetProjectionMatrix() =>
            Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(Fov), AspectRatio, NearPlane, FarPlane);
    }
}
