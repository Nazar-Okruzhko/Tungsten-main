using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using Tungsten.World;

namespace Tungsten.Rendering
{
    /// <summary>
    /// A deliberately simple forward renderer: one draw call per entity, one
    /// directional light, no shadow mapping yet. This is the seam where you'd
    /// later add: shadow maps, an HDR framebuffer + bloom pass, SSAO, etc. to
    /// push toward "id Tech 5 tier" visuals - the MainLit shader already
    /// exposes exposure/roughness/metallic/fog so that work slots in without
    /// changing this class's public API.
    /// </summary>
    public class Renderer
    {
        public Shader LitShader { get; }
        public Vector3 SunDirection = new(-0.4f, -1f, -0.3f);
        public Vector3 SunColor = new(1.0f, 0.95f, 0.85f);
        public Vector3 FogColor = new(0.55f, 0.62f, 0.7f);

        public Renderer()
        {
            LitShader = new Shader(ShaderLibrary.MainLitVertex, ShaderLibrary.MainLitFragment);

            // These four lines are what populate the Studio's "Shader" tab
            // with slider bars - see Panels/ShaderPanel.cs.
            LitShader.ExposeFloat("uRoughness", 0.5f, 0.02f, 1f);
            LitShader.ExposeFloat("uMetallic", 0.0f, 0f, 1f);
            LitShader.ExposeFloat("uAmbient", 0.12f, 0f, 1f);
            LitShader.ExposeFloat("uExposure", 1.2f, 0.1f, 4f);
            LitShader.ExposeFloat("uFogDensity", 0.015f, 0f, 0.2f);

            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.CullFace);
            GL.ClearColor(0.10f, 0.11f, 0.13f, 1f);
        }

        public void RenderScene(Scene scene, Camera camera, int viewportWidth, int viewportHeight)
        {
            // Explicitly re-assert 3D state every frame rather than relying on
            // the constructor having set it once: the UI pass (ImmediateUi)
            // intentionally disables depth test / culling and enables
            // blending for its own 2D quads, and since both passes share one
            // GL context, whichever ran last frame otherwise leaks its state
            // into this one.
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.CullFace);
            GL.Disable(EnableCap.Blend);

            // NOTE: this method deliberately does NOT call GL.Viewport.
            // The caller (ViewportPanel, when hosting this inside a docked
            // editor panel) is responsible for setting the GL viewport/
            // scissor rectangle *in window pixel coordinates before calling
            // this*, because only the caller knows where the panel sits
            // inside the actual window - GL.Viewport(0,0,w,h) would always
            // draw into the window's bottom-left corner (GL's origin is
            // bottom-left, not top-left) regardless of the panel's real
            // on-screen position, which previously misplaced the whole 3D
            // view. viewportWidth/viewportHeight here are only used for the
            // camera's aspect ratio.
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            camera.AspectRatio = viewportHeight == 0 ? 1f : (float)viewportWidth / viewportHeight;

            LitShader.Use();
            LitShader.ApplyExposedFloats(); // <-- live slider values from the Shader panel
            LitShader.SetVector3("uCameraPos", camera.Position);
            LitShader.SetVector3("uSunDirection", SunDirection.Normalized());
            LitShader.SetVector3("uSunColor", SunColor);
            LitShader.SetVector3("uFogColor", FogColor);
            LitShader.SetMatrix4("uView", camera.GetViewMatrix());
            LitShader.SetMatrix4("uProjection", camera.GetProjectionMatrix());

            foreach (Entity e in scene.Entities)
            {
                if (e.Mesh == null || e.Material == null) continue;
                LitShader.SetMatrix4("uModel", e.Transform.GetMatrix());
                LitShader.SetVector3("uAlbedo", e.Material.Albedo);
                e.Mesh.Draw();
            }
        }
    }
}
