// =============================================================================
// TUNGSTEN ENGINE — Core/Transform.cs
// Transform stores and computes the spatial state of an Entity:
//   • Position  — XYZ in world space (or local space if parented)
//   • Rotation  — Quaternion (avoids gimbal lock); exposed as Euler for editor UI
//   • Scale     — per-axis scale factor
//
// The ModelMatrix combines all three into a single 4×4 matrix fed to the shader
// as the "model" matrix in the classic MVP (Model-View-Projection) pipeline.
// =============================================================================

using OpenTK.Mathematics;

namespace Tungsten.Core
{
    public class Transform
    {
        // ---------------------------------------------------------------------------
        // Raw state — stored as floats for compact serialisation
        // ---------------------------------------------------------------------------

        public Vector3 Position { get; set; } = Vector3.Zero;
        public Quaternion Rotation { get; set; } = Quaternion.Identity;
        public Vector3 Scale    { get; set; } = Vector3.One;

        // ---------------------------------------------------------------------------
        // Euler convenience  (degrees, XYZ order)
        // Used by the Properties panel sliders in the studio
        // ---------------------------------------------------------------------------

        public Vector3 EulerAngles
        {
            get
            {
                // Convert quaternion → Euler (radians → degrees)
                var (pitch, yaw, roll) = Rotation.ToEulerAngles();
                return new Vector3(
                    MathHelper.RadiansToDegrees(pitch),
                    MathHelper.RadiansToDegrees(yaw),
                    MathHelper.RadiansToDegrees(roll));
            }
            set
            {
                // Convert degree Euler → quaternion
                Rotation = Quaternion.FromEulerAngles(
                    MathHelper.DegreesToRadians(value.X),
                    MathHelper.DegreesToRadians(value.Y),
                    MathHelper.DegreesToRadians(value.Z));
            }
        }

        // ---------------------------------------------------------------------------
        // Direction vectors — derived from the rotation quaternion
        // ---------------------------------------------------------------------------

        /// <summary>The direction the entity is "looking" (forward = −Z in OpenGL).</summary>
        public Vector3 Forward => Rotation * (-Vector3.UnitZ);

        /// <summary>Up vector in the entity's local space.</summary>
        public Vector3 Up      => Rotation * Vector3.UnitY;

        /// <summary>Right vector (cross of Forward × Up won't flip).</summary>
        public Vector3 Right   => Rotation * Vector3.UnitX;

        // ---------------------------------------------------------------------------
        // Model matrix (TRS = Translate × Rotate × Scale)
        // Sent to the vertex shader as "uniform mat4 uModel"
        // ---------------------------------------------------------------------------

        public Matrix4 ModelMatrix =>
            Matrix4.CreateScale(Scale) *
            Matrix4.CreateFromQuaternion(Rotation) *
            Matrix4.CreateTranslation(Position);
    }
}
