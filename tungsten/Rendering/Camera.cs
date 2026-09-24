// =============================================================================
// TUNGSTEN ENGINE — Rendering/Camera.cs
// Camera component: computes View + Projection matrices for the renderer.
// One Camera is designated as the "active" camera each frame.
// The Studio has a separate Editor Camera that is never in the scene graph.
// =============================================================================

using OpenTK.Mathematics;

namespace Tungsten.Rendering
{
    public class Camera : Core.Component
    {
        // ---------------------------------------------------------------------------
        // Projection settings (tweakable in the Properties panel)
        // ---------------------------------------------------------------------------

        public float FieldOfView  { get; set; } = 75.0f;   // degrees
        public float NearClip     { get; set; } = 0.1f;
        public float FarClip      { get; set; } = 1000.0f;
        public float AspectRatio  { get; set; } = 16.0f / 9.0f;

        // ---------------------------------------------------------------------------
        // Derived matrices (computed on demand)
        // ---------------------------------------------------------------------------

        /// <summary>
        /// Converts world space → camera space.
        /// Built from the owning Entity's world position and forward direction.
        /// </summary>
        public Matrix4 ViewMatrix =>
            Matrix4.LookAt(
                Position,
                Position + Transform.Forward,
                Transform.Up);

        /// <summary>
        /// Converts camera space → clip space (perspective divide).
        /// </summary>
        public Matrix4 ProjectionMatrix =>
            Matrix4.CreatePerspectiveFieldOfView(
                MathHelper.DegreesToRadians(FieldOfView),
                AspectRatio,
                NearClip,
                FarClip);

        // Convenience shortcut used by the lighting shader
        public Vector3 Position => Owner.Transform.Position;
    }

    // =============================================================================
    // MeshRenderer — draws a mesh with a material
    // =============================================================================

    public class MeshRenderer : Core.Component
    {
        // Material data (will be extended with texture handles in a full build)
        public Vector3 AlbedoColor { get; set; } = new(0.8f, 0.8f, 0.8f);
        public float   Roughness   { get; set; } = 0.5f;
        public float   Metallic    { get; set; } = 0.0f;

        // VAO/VBO handles created by MeshLoader
        internal int VAO  { get; set; }
        internal int VBO  { get; set; }
        internal int EBO  { get; set; }
        internal int IndexCount { get; set; }

        public void Draw()
        {
            if (VAO == 0 || IndexCount == 0) return;
            OpenTK.Graphics.OpenGL4.GL.BindVertexArray(VAO);
            OpenTK.Graphics.OpenGL4.GL.DrawElements(
                OpenTK.Graphics.OpenGL4.PrimitiveType.Triangles,
                IndexCount,
                OpenTK.Graphics.OpenGL4.DrawElementsType.UnsignedInt, 0);
            OpenTK.Graphics.OpenGL4.GL.BindVertexArray(0);
        }
    }
}
