using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Tungsten.Rendering;

namespace TungstenStudio.Editor.UI
{
    public readonly struct UiRect
    {
        public readonly float X, Y, W, H;
        public UiRect(float x, float y, float w, float h) { X = x; Y = y; W = w; H = h; }
        public bool Contains(Vector2 p) => p.X >= X && p.X <= X + W && p.Y >= Y && p.Y <= Y + H;
    }

    /// <summary>
    /// A deliberately small immediate-mode GUI: everything the Studio needs
    /// (docked panels, icon toolbar buttons, sliders, scrolling text lists)
    /// built from two GL draw calls (a colored quad shader and a textured
    /// quad shader) instead of pulling in Dear ImGui or a WPF/WinForms
    /// dependency - keeping the whole editor to a handful of files, per the
    /// brief. Call Begin() once per frame, then Panel/Button/Slider/Label as
    /// needed, then End() to flush the batched geometry.
    /// </summary>
    public class ImmediateUi : IDisposable
    {
        private readonly Shader _quadShader;
        private readonly int _vao, _vbo;
        private readonly List<float> _vertsColored = new();   // x,y,r,g,b,a
        private readonly List<float> _vertsTextured = new();  // x,y,u,v (+ bound texture drawn immediately)

        private readonly Dictionary<string, Texture> _textCache = new();
        private readonly Dictionary<string, int> _rawTextureCache = new();

        public int ScreenWidth, ScreenHeight;
        public Vector2 MousePos;
        public bool MouseDown, MousePressed, MouseReleased;

        private bool _mouseDownLastFrame;
        // A control "claims" a drag once the mouse goes down on it, identified by this key,
        // so dragging a slider doesn't also register as clicking whatever is behind the cursor.
        public string? ActiveControl;

        public ImmediateUi()
        {
            _quadShader = new Shader(ShaderLibrary.UnlitTexturedVertex, ShaderLibrary.UnlitTexturedFragment);
            _vao = GL.GenVertexArray();
            _vbo = GL.GenBuffer();
        }

        public void UpdateInput(MouseState mouse, int width, int height)
        {
            ScreenWidth = width;
            ScreenHeight = height;
            MousePos = new Vector2(mouse.X, mouse.Y);
            MouseDown = mouse.IsButtonDown(MouseButton.Left);
            MousePressed = MouseDown && !_mouseDownLastFrame;
            MouseReleased = !MouseDown && _mouseDownLastFrame;
            _mouseDownLastFrame = MouseDown;
            if (MouseReleased) ActiveControl = null;
        }

        public Matrix4 Projection => Matrix4.CreateOrthographicOffCenter(0, ScreenWidth, ScreenHeight, 0, -1, 1);

        // ---------------- primitive drawing ----------------

        public void Rect(UiRect r, Color4 color)
        {
            SetUiGlState();
            _quadShader.Use();
            _quadShader.SetMatrix4("uProjection", Projection);
            _quadShader.SetVector3("uTint", new Vector3(color.R, color.G, color.B));
            _quadShader.SetFloat("uAlpha", color.A);
            GL.BindTexture(TextureTarget.Texture2D, WhiteTexture());
            DrawQuad(r.X, r.Y, r.W, r.H, 0, 0, 1, 1);
        }

        public void Image(UiRect r, int glTextureHandle, Color4? tint = null)
        {
            SetUiGlState();
            _quadShader.Use();
            _quadShader.SetMatrix4("uProjection", Projection);
            var c = tint ?? Color4.White;
            _quadShader.SetVector3("uTint", new Vector3(c.R, c.G, c.B));
            _quadShader.SetFloat("uAlpha", c.A);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, glTextureHandle);
            DrawQuad(r.X, r.Y, r.W, r.H, 0, 0, 1, 1);
        }

        /// <summary>
        /// GL state for every 2D UI draw call. Crucially disables face
        /// culling: the orthographic projection used for UI (top-left
        /// origin, Y increasing downward) flips the effective winding order
        /// versus the 3D pass's convention, so leaving culling on (as the 3D
        /// Renderer needs it) silently discards every UI quad - which is
        /// exactly what caused the "engine renders but the whole editor UI
        /// is invisible" bug. Blending is enabled so icon/text PNG alpha
        /// channels composite correctly instead of drawing opaque squares.
        /// </summary>
        private static void SetUiGlState()
        {
            GL.Disable(EnableCap.DepthTest);
            GL.Disable(EnableCap.CullFace);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        }

        public void Text(string text, float x, float y, Color? color = null)
        {
            if (string.IsNullOrEmpty(text)) return;
            string key = text + "_" + (color ?? Color.WhiteSmoke).ToArgb();
            if (!_rawTextureCache.TryGetValue(key, out int handle))
            {
                handle = RasterizeText(text, color ?? Color.WhiteSmoke);
                _rawTextureCache[key] = handle;
            }
            GL.BindTexture(TextureTarget.Texture2D, handle);
            GL.GetTexLevelParameter(TextureTarget.Texture2D, 0, GetTextureParameter.TextureWidth, out int w);
            GL.GetTexLevelParameter(TextureTarget.Texture2D, 0, GetTextureParameter.TextureHeight, out int h);
            Image(new UiRect(x, y, w, h), handle);
        }

        // ---------------- controls ----------------

        /// <summary>Icon toolbar / asset-store button. Returns true the frame it is clicked.</summary>
        public bool IconButton(string id, UiRect r, int iconTexture, string? label = null, bool active = false)
        {
            bool hovered = r.Contains(MousePos);
            Color4 bg = active ? new Color4(0.20f, 0.42f, 0.75f, 1f)
                       : hovered ? new Color4(0.28f, 0.28f, 0.32f, 1f)
                       : new Color4(0.18f, 0.18f, 0.21f, 1f);
            Rect(r, bg);
            float pad = 4f;
            Image(new UiRect(r.X + pad, r.Y + pad, r.W - pad * 2, r.H - pad * 2 - (label != null ? 12 : 0)), iconTexture);
            if (label != null) Text(label, r.X + 2, r.Y + r.H - 12, Color.Gainsboro);
            return hovered && MousePressed;
        }

        public bool TextButton(UiRect r, string label, bool primary = false)
        {
            bool hovered = r.Contains(MousePos);
            Color4 bg = primary ? new Color4(0.16f, 0.45f, 0.28f, 1f) : hovered ? new Color4(0.30f, 0.30f, 0.34f, 1f) : new Color4(0.22f, 0.22f, 0.25f, 1f);
            Rect(r, bg);
            Text(label, r.X + 6, r.Y + r.H / 2 - 6, Color.WhiteSmoke);
            return hovered && MousePressed;
        }

        /// <summary>Horizontal slider bound directly to a (value,min,max) tuple - this is what the Shader panel uses.</summary>
        public float Slider(string id, UiRect r, float value, float min, float max)
        {
            Rect(r, new Color4(0.15f, 0.15f, 0.17f, 1f));
            float t = max > min ? (value - min) / (max - min) : 0f;
            t = MathHelper.Clamp(t, 0f, 1f);
            Rect(new UiRect(r.X, r.Y, r.W * t, r.H), new Color4(0.25f, 0.55f, 0.85f, 1f));

            bool hovered = r.Contains(MousePos);
            if (hovered && MousePressed) ActiveControl = id;
            if (ActiveControl == id && MouseDown)
            {
                float nt = MathHelper.Clamp((MousePos.X - r.X) / r.W, 0f, 1f);
                value = min + nt * (max - min);
            }
            Text($"{value:F2}", r.X + r.W + 8, r.Y - 2, Color.Gainsboro);
            return value;
        }

        public bool Checkbox(UiRect r, bool value)
        {
            Rect(r, value ? new Color4(0.25f, 0.55f, 0.3f, 1f) : new Color4(0.2f, 0.2f, 0.22f, 1f));
            if (r.Contains(MousePos) && MousePressed) return !value;
            return value;
        }

        // ---------------- internals ----------------

        private int _whiteTex = -1;
        private int WhiteTexture()
        {
            if (_whiteTex != -1) return _whiteTex;
            _whiteTex = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, _whiteTex);
            byte[] px = { 255, 255, 255, 255 };
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, 1, 1, 0, OpenTK.Graphics.OpenGL4.PixelFormat.Rgba, PixelType.UnsignedByte, px);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            return _whiteTex;
        }

        private void DrawQuad(float x, float y, float w, float h, float u0, float v0, float u1, float v1)
        {
            float[] verts =
            {
                x,     y,     u0, v0,
                x + w, y,     u1, v0,
                x + w, y + h, u1, v1,
                x,     y,     u0, v0,
                x + w, y + h, u1, v1,
                x,     y + h, u0, v1,
            };
            GL.BindVertexArray(_vao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, verts.Length * sizeof(float), verts, BufferUsageHint.StreamDraw);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));
            GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
        }

        /// <summary>Rasterizes a text string into a GL texture using System.Drawing (editor-only, never on the hot gameplay path).</summary>
        private int RasterizeText(string text, Color color)
        {
            using var font = new Font("Segoe UI", 10f, System.Drawing.FontStyle.Regular, GraphicsUnit.Pixel);
            using var measureBmp = new Bitmap(1, 1);
            SizeF size;
            using (var mg = Graphics.FromImage(measureBmp)) size = mg.MeasureString(text, font);

            int w = Math.Max(1, (int)MathF.Ceiling(size.Width));
            int h = Math.Max(1, (int)MathF.Ceiling(size.Height));
            using var bmp = new Bitmap(w, h);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                using var brush = new SolidBrush(color);
                g.DrawString(text, font, brush, 0, 0);
            }

            var data = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            int tex = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, tex);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, w, h, 0, OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            bmp.UnlockBits(data);
            return tex;
        }

        public void Dispose()
        {
            GL.DeleteBuffer(_vbo);
            GL.DeleteVertexArray(_vao);
        }
    }
}
