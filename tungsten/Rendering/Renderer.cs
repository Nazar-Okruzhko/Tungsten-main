// =============================================================================
// TUNGSTEN ENGINE — Rendering/Renderer.cs
//
// Main OpenGL renderer — the "best graphics possible" tier.
// Pipeline overview:
//
//   1. Geometry Pass  — render all opaque meshes → G-Buffer (position/normal/albedo)
//   2. Lighting Pass  — deferred PBR lighting in screen space
//   3. SSAO Pass      — Screen Space Ambient Occlusion (optional, quality setting)
//   4. Bloom Pass     — extract bright areas, blur, composite
//   5. Tonemap + Gamma— HDR → LDR with ACES filmic curve
//   6. FXAA           — Fast Approximate Anti-Aliasing
//
// Shader parameters exposed as "Shader Slide Bars" in the studio's shader panel.
// =============================================================================

using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Tungsten.Rendering
{
    /// <summary>
    /// All adjustable shader quality knobs — edited via the Studio's
    /// Shader Configuration panel (slider bars).
    /// </summary>
    public class ShaderSettings
    {
        // ── Lighting ──────────────────────────────────────────────────────────
        public float AmbientStrength     { get; set; } = 0.08f;   // [0, 1]
        public float SunIntensity        { get; set; } = 3.0f;    // [0, 10]
        public Vector3 SunDirection      { get; set; } = new(-0.4f, -1.0f, -0.6f);
        public Vector3 SunColor         { get; set; } = new(1.0f, 0.96f, 0.88f);

        // ── PBR roughness/metallic overrides ──────────────────────────────────
        public float GlobalRoughness     { get; set; } = 0.5f;    // [0, 1]
        public float GlobalMetallic      { get; set; } = 0.0f;    // [0, 1]

        // ── Ambient Occlusion ─────────────────────────────────────────────────
        public bool  SSAOEnabled         { get; set; } = true;
        public float SSAORadius          { get; set; } = 0.5f;    // [0.1, 2.0]
        public float SSAOBias            { get; set; } = 0.025f;  // [0.001, 0.1]
        public int   SSAOSamples         { get; set; } = 32;      // [8, 128]
        public float SSAOPower           { get; set; } = 1.5f;    // [0.5, 4.0]

        // ── Bloom ─────────────────────────────────────────────────────────────
        public bool  BloomEnabled        { get; set; } = true;
        public float BloomThreshold      { get; set; } = 1.2f;    // HDR luma cutoff
        public float BloomStrength       { get; set; } = 0.04f;   // [0, 0.5]
        public float BloomRadius         { get; set; } = 0.005f;  // blur spread

        // ── Tone mapping / colour grading ─────────────────────────────────────
        public float Exposure            { get; set; } = 1.0f;    // [0.1, 5.0]
        public float Contrast            { get; set; } = 1.05f;   // [0.5, 2.0]
        public float Saturation          { get; set; } = 1.1f;    // [0, 2.0]
        public bool  ACESFilmic          { get; set; } = true;    // ACES vs Reinhard

        // ── Fog ───────────────────────────────────────────────────────────────
        public bool  FogEnabled          { get; set; } = true;
        public float FogStart            { get; set; } = 80.0f;
        public float FogEnd              { get; set; } = 400.0f;
        public Vector3 FogColor          { get; set; } = new(0.6f, 0.7f, 0.9f);

        // ── Shadow ────────────────────────────────────────────────────────────
        public bool  ShadowsEnabled      { get; set; } = true;
        public int   ShadowMapSize       { get; set; } = 2048;    // 512/1024/2048/4096
        public float ShadowBias          { get; set; } = 0.005f;
        public float ShadowSoftness      { get; set; } = 1.0f;   // PCF radius

        // ── Anti-aliasing ─────────────────────────────────────────────────────
        public bool  FXAAEnabled         { get; set; } = true;
    }

    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Manages the full deferred PBR rendering pipeline.
    /// One Renderer lives on the Engine's GL context thread.
    /// </summary>
    public class Renderer : IDisposable
    {
        // ---------------------------------------------------------------------------
        // Public settings (bound to studio's shader slider bars)
        // ---------------------------------------------------------------------------

        public ShaderSettings Settings { get; } = new();

        // ---------------------------------------------------------------------------
        // G-Buffer handles
        // ---------------------------------------------------------------------------

        private int _gBuffer;           // FBO
        private int _gPosition;         // Texture: world-space position + depth
        private int _gNormal;           // Texture: packed normals
        private int _gAlbedoSpec;       // Texture: albedo RGB + specular A

        // ---------------------------------------------------------------------------
        // Shadow map
        // ---------------------------------------------------------------------------

        private int _shadowFBO;
        private int _shadowDepthTex;

        // ---------------------------------------------------------------------------
        // Screen quad (for fullscreen passes)
        // ---------------------------------------------------------------------------

        private int _quadVAO;
        private int _quadVBO;

        // ---------------------------------------------------------------------------
        // Shader program handles (compiled at init)
        // ---------------------------------------------------------------------------

        private int _shaderGeometry;    // G-Buffer fill
        private int _shaderLighting;    // Deferred PBR lighting
        private int _shaderSSAO;        // SSAO
        private int _shaderBloom;       // Bloom extract + blur
        private int _shaderFinal;       // Tonemap + FXAA composite

        // ---------------------------------------------------------------------------
        // Viewport dimensions
        // ---------------------------------------------------------------------------

        private int _width, _height;

        // ---------------------------------------------------------------------------
        // Init
        // ---------------------------------------------------------------------------

        public void Init(int width, int height)
        {
            _width  = width;
            _height = height;

            CompileShaders();
            CreateGBuffer(width, height);
            CreateShadowMap();
            CreateScreenQuad();

            Console.WriteLine($"[Renderer] Initialised {width}×{height}  GL {GL.GetString(StringName.Version)}");
        }

        // ---------------------------------------------------------------------------
        // Main render call (called by Engine each frame)
        // ---------------------------------------------------------------------------

        public void RenderScene(Core.Scene scene, Camera camera)
        {
            // ── 0. Shadow pass ────────────────────────────────────────────────
            if (Settings.ShadowsEnabled)
                DoShadowPass(scene);

            // ── 1. Geometry pass → fill G-Buffer ──────────────────────────────
            DoGeometryPass(scene, camera);

            // ── 2. SSAO pass (reads G-Buffer) ─────────────────────────────────
            if (Settings.SSAOEnabled)
                DoSSAOPass(camera);

            // ── 3. Deferred PBR lighting pass ─────────────────────────────────
            DoLightingPass(camera);

            // ── 4. Bloom pass ─────────────────────────────────────────────────
            if (Settings.BloomEnabled)
                DoBloomPass();

            // ── 5. Tonemap / FXAA composite → default framebuffer ─────────────
            DoFinalPass();
        }

        // ---------------------------------------------------------------------------
        // Resize
        // ---------------------------------------------------------------------------

        public void Resize(int width, int height)
        {
            _width  = width;
            _height = height;
            // Recreate resolution-dependent FBOs
            GL.DeleteFramebuffer(_gBuffer);
            CreateGBuffer(width, height);
            Console.WriteLine($"[Renderer] Resized → {width}×{height}");
        }

        // ---------------------------------------------------------------------------
        // Cleanup
        // ---------------------------------------------------------------------------

        public void Dispose()
        {
            GL.DeleteFramebuffer(_gBuffer);
            GL.DeleteTexture(_gPosition);
            GL.DeleteTexture(_gNormal);
            GL.DeleteTexture(_gAlbedoSpec);
            GL.DeleteFramebuffer(_shadowFBO);
            GL.DeleteTexture(_shadowDepthTex);
            GL.DeleteVertexArray(_quadVAO);
            GL.DeleteBuffer(_quadVBO);
        }

        // =========================================================================
        // Private helpers — each rendering pass
        // =========================================================================

        private void DoShadowPass(Core.Scene scene)
        {
            // Render depth from sun's perspective into shadow map.
            GL.Viewport(0, 0, Settings.ShadowMapSize, Settings.ShadowMapSize);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _shadowFBO);
            GL.Clear(ClearBufferMask.DepthBufferBit);

            // TODO: bind _shaderDepth, render scene geometry
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.Viewport(0, 0, _width, _height);
        }

        private void DoGeometryPass(Core.Scene scene, Camera camera)
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _gBuffer);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            GL.UseProgram(_shaderGeometry);
            SetUniformMat4(_shaderGeometry, "uView",       camera.ViewMatrix);
            SetUniformMat4(_shaderGeometry, "uProjection", camera.ProjectionMatrix);

            // Render each entity's MeshRenderer
            foreach (var entity in scene.Entities)
            {
                var mesh = entity.GetComponent<MeshRenderer>();
                if (mesh == null || !entity.IsVisible) continue;

                SetUniformMat4(_shaderGeometry, "uModel", entity.Transform.ModelMatrix);
                SetUniformVec3(_shaderGeometry, "uAlbedo", mesh.AlbedoColor);
                SetUniformFloat(_shaderGeometry, "uRoughness",
                    mesh.Roughness * Settings.GlobalRoughness + Settings.GlobalRoughness * 0.0f);
                SetUniformFloat(_shaderGeometry, "uMetallic", mesh.Metallic);

                mesh.Draw();
            }

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        private void DoSSAOPass(Camera camera)
        {
            // Full-screen pass: sample G-Buffer normals + positions,
            // cast kernel of sample rays, compute occlusion factor.
            GL.UseProgram(_shaderSSAO);
            SetUniformFloat(_shaderSSAO, "uRadius",  Settings.SSAORadius);
            SetUniformFloat(_shaderSSAO, "uBias",    Settings.SSAOBias);
            SetUniformFloat(_shaderSSAO, "uPower",   Settings.SSAOPower);
            SetUniformInt  (_shaderSSAO, "uSamples", Settings.SSAOSamples);
            DrawQuad();
        }

        private void DoLightingPass(Camera camera)
        {
            GL.UseProgram(_shaderLighting);

            // Bind G-Buffer textures
            GL.ActiveTexture(TextureUnit.Texture0); GL.BindTexture(TextureTarget.Texture2D, _gPosition);
            GL.ActiveTexture(TextureUnit.Texture1); GL.BindTexture(TextureTarget.Texture2D, _gNormal);
            GL.ActiveTexture(TextureUnit.Texture2); GL.BindTexture(TextureTarget.Texture2D, _gAlbedoSpec);

            // Sun light
            SetUniformVec3 (_shaderLighting, "uSunDirection", Settings.SunDirection.Normalized());
            SetUniformVec3 (_shaderLighting, "uSunColor",     Settings.SunColor * Settings.SunIntensity);
            SetUniformFloat(_shaderLighting, "uAmbient",      Settings.AmbientStrength);

            // Camera position for specular
            SetUniformVec3(_shaderLighting, "uCamPos", camera.Position);

            // Fog
            SetUniformBool (_shaderLighting, "uFogEnabled", Settings.FogEnabled);
            SetUniformFloat(_shaderLighting, "uFogStart",   Settings.FogStart);
            SetUniformFloat(_shaderLighting, "uFogEnd",     Settings.FogEnd);
            SetUniformVec3 (_shaderLighting, "uFogColor",   Settings.FogColor);

            // Shadow
            SetUniformBool (_shaderLighting, "uShadowsEnabled", Settings.ShadowsEnabled);
            SetUniformFloat(_shaderLighting, "uShadowBias",     Settings.ShadowBias);

            DrawQuad();
        }

        private void DoBloomPass()
        {
            GL.UseProgram(_shaderBloom);
            SetUniformFloat(_shaderBloom, "uThreshold", Settings.BloomThreshold);
            SetUniformFloat(_shaderBloom, "uStrength",  Settings.BloomStrength);
            SetUniformFloat(_shaderBloom, "uRadius",    Settings.BloomRadius);
            DrawQuad();
        }

        private void DoFinalPass()
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.UseProgram(_shaderFinal);
            SetUniformFloat(_shaderFinal, "uExposure",    Settings.Exposure);
            SetUniformFloat(_shaderFinal, "uContrast",    Settings.Contrast);
            SetUniformFloat(_shaderFinal, "uSaturation",  Settings.Saturation);
            SetUniformBool (_shaderFinal, "uACES",         Settings.ACESFilmic);
            SetUniformBool (_shaderFinal, "uFXAA",         Settings.FXAAEnabled);
            DrawQuad();
        }

        // ---------------------------------------------------------------------------
        // Resource creation helpers
        // ---------------------------------------------------------------------------

        private void CreateGBuffer(int w, int h)
        {
            _gBuffer = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _gBuffer);

            // Position (RGB16F — needs precision for SSAO)
            _gPosition = CreateFBOTexture(w, h, PixelInternalFormat.Rgb16f, PixelFormat.Rgb, PixelType.Float);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer,
                FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, _gPosition, 0);

            // Normals (RGB16F)
            _gNormal = CreateFBOTexture(w, h, PixelInternalFormat.Rgb16f, PixelFormat.Rgb, PixelType.Float);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer,
                FramebufferAttachment.ColorAttachment1, TextureTarget.Texture2D, _gNormal, 0);

            // Albedo + specular (RGBA8)
            _gAlbedoSpec = CreateFBOTexture(w, h, PixelInternalFormat.Rgba, PixelFormat.Rgba, PixelType.UnsignedByte);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer,
                FramebufferAttachment.ColorAttachment2, TextureTarget.Texture2D, _gAlbedoSpec, 0);

            // Tell OpenGL we're rendering into all 3 colour attachments
            GL.DrawBuffers(3, new[]
            {
                DrawBuffersEnum.ColorAttachment0,
                DrawBuffersEnum.ColorAttachment1,
                DrawBuffersEnum.ColorAttachment2
            });

            // Depth renderbuffer
            int rbo = GL.GenRenderbuffer();
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, rbo);
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer,
                RenderbufferStorage.DepthComponent, w, h);
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer,
                FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, rbo);

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        private void CreateShadowMap()
        {
            _shadowFBO = GL.GenFramebuffer();
            _shadowDepthTex = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, _shadowDepthTex);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent,
                Settings.ShadowMapSize, Settings.ShadowMapSize, 0,
                PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _shadowFBO);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer,
                FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, _shadowDepthTex, 0);
            GL.DrawBuffer(DrawBufferMode.None);
            GL.ReadBuffer(ReadBufferMode.None);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        private void CreateScreenQuad()
        {
            // Two triangles covering the full NDC screen — used for all fullscreen passes
            float[] verts = {
                -1,-1,  0,0,
                 1,-1,  1,0,
                -1, 1,  0,1,
                 1, 1,  1,1,
                -1, 1,  0,1,
                 1,-1,  1,0
            };
            _quadVAO = GL.GenVertexArray();
            _quadVBO = GL.GenBuffer();
            GL.BindVertexArray(_quadVAO);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _quadVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, verts.Length * sizeof(float), verts, BufferUsageHint.StaticDraw);
            GL.EnableVertexAttribArray(0); GL.VertexAttribPointer(0,2,VertexAttribPointerType.Float,false,4*4,0);
            GL.EnableVertexAttribArray(1); GL.VertexAttribPointer(1,2,VertexAttribPointerType.Float,false,4*4,2*4);
            GL.BindVertexArray(0);
        }

        private void DrawQuad()
        {
            GL.BindVertexArray(_quadVAO);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
            GL.BindVertexArray(0);
        }

        private static int CreateFBOTexture(int w, int h,
            PixelInternalFormat internalFmt, PixelFormat fmt, PixelType type)
        {
            int tex = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, tex);
            GL.TexImage2D(TextureTarget.Texture2D, 0, internalFmt, w, h, 0, fmt, type, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            return tex;
        }

        // ---------------------------------------------------------------------------
        // Shader compilation (source strings in Shaders/ folder)
        // ---------------------------------------------------------------------------

        private void CompileShaders()
        {
            // In a full build these would load .glsl files from disk.
            // For the scaffold we create minimal passthrough shaders so the
            // project compiles and runs without shader files present.
            _shaderGeometry = CreateMinimalShader();
            _shaderLighting = CreateMinimalShader();
            _shaderSSAO     = CreateMinimalShader();
            _shaderBloom    = CreateMinimalShader();
            _shaderFinal    = CreateMinimalShader();
        }

        private static int CreateMinimalShader()
        {
            const string vert = @"#version 330 core
layout(location=0) in vec2 aPos;
layout(location=1) in vec2 aUV;
out vec2 vUV;
void main(){ vUV=aUV; gl_Position=vec4(aPos,0,1); }";

            const string frag = @"#version 330 core
in vec2 vUV; out vec4 fColor;
void main(){ fColor=vec4(vUV,0.5,1.0); }";

            int vs = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vs, vert); GL.CompileShader(vs);

            int fs = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fs, frag); GL.CompileShader(fs);

            int prog = GL.CreateProgram();
            GL.AttachShader(prog, vs); GL.AttachShader(prog, fs);
            GL.LinkProgram(prog);
            GL.DeleteShader(vs); GL.DeleteShader(fs);
            return prog;
        }

        // ---------------------------------------------------------------------------
        // Uniform setters — thin wrappers around GL.Uniform*
        // ---------------------------------------------------------------------------

        private static void SetUniformMat4 (int prog, string name, Matrix4 v)  { GL.UseProgram(prog); GL.UniformMatrix4(GL.GetUniformLocation(prog,name),false,ref v); }
        private static void SetUniformVec3 (int prog, string name, Vector3 v)  { GL.UseProgram(prog); GL.Uniform3(GL.GetUniformLocation(prog,name),v); }
        private static void SetUniformFloat(int prog, string name, float v)    { GL.UseProgram(prog); GL.Uniform1(GL.GetUniformLocation(prog,name),v); }
        private static void SetUniformInt  (int prog, string name, int v)      { GL.UseProgram(prog); GL.Uniform1(GL.GetUniformLocation(prog,name),v); }
        private static void SetUniformBool (int prog, string name, bool v)     { GL.UseProgram(prog); GL.Uniform1(GL.GetUniformLocation(prog,name),v?1:0); }
    }
}
