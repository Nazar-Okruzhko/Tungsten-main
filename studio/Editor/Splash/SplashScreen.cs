using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using Tungsten.Rendering;
using TungstenStudio.Editor.UI;

namespace TungstenStudio.Editor.Splash
{
    /// <summary>
    /// Shows shared/Bitmap/Bitmap1076.bmp (your "id Studio / id Tech 5" banner)
    /// full-screen with a fade-in/hold/fade-out while heavier engine systems
    /// (Renderer, AssetDatabase, icon set) finish initializing on a
    /// background task - exactly like a real engine's boot splash.
    /// </summary>
    public class SplashScreen
    {
        private readonly Texture _banner;
        private float _timer;

        public const float FadeInTime = 0.5f;
        public const float HoldTime = 1.4f;
        public const float FadeOutTime = 0.6f;
        public float TotalDuration => FadeInTime + HoldTime + FadeOutTime;

        public bool Finished => _timer >= TotalDuration;

        public SplashScreen(string bimapPath)
        {
            _banner = new Texture(bimapPath);
        }

        public void Update(float deltaTime) => _timer += deltaTime;

        public void Render(ImmediateUi ui, int screenWidth, int screenHeight)
        {
            GL.ClearColor(0.02f, 0.02f, 0.03f, 1f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            float alpha = _timer < FadeInTime
                ? _timer / FadeInTime
                : _timer < FadeInTime + HoldTime
                    ? 1f
                    : 1f - (_timer - FadeInTime - HoldTime) / FadeOutTime;
            alpha = MathHelper.Clamp(alpha, 0f, 1f);

            // Fit the banner centered, preserving aspect ratio.
            float aspect = (float)_banner.Width / _banner.Height;
            float w = screenWidth * 0.55f;
            float h = w / aspect;
            if (h > screenHeight * 0.55f) { h = screenHeight * 0.55f; w = h * aspect; }
            float x = (screenWidth - w) / 2f;
            float y = (screenHeight - h) / 2f;

            ui.Image(new UiRect(x, y, w, h), _banner.Handle, new Color4(1f, 1f, 1f, alpha));
            ui.Text("Loading Tungsten engine...", x, y + h + 12, System.Drawing.Color.FromArgb((int)(alpha * 255), 220, 220, 225));
        }
    }
}
