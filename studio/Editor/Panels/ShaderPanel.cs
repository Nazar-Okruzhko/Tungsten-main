using System.Collections.Generic;
using OpenTK.Mathematics;
using TungstenStudio.Editor.UI;

namespace TungstenStudio.Editor.Panels
{
    /// <summary>
    /// The "Shader Slide Bars" the brief asked for. This panel does not know
    /// anything about lighting math - it simply enumerates whatever the
    /// active Shader chose to expose via Shader.ExposeFloat(...) (see
    /// Renderer's constructor) and draws one slider per entry. Drag a slider
    /// and the very next rendered frame uses the new value - true live,
    /// experimental shader tuning.
    /// </summary>
    public class ShaderPanel
    {
        public void Draw(EditorContext ctx, UiRect rect)
        {
            ctx.Ui.Rect(rect, new Color4(0.13f, 0.13f, 0.15f, 1f));

            float y = rect.Y + 10;
            var shader = ctx.Engine.Renderer.LitShader;

            // Copy keys first since Slider() below mutates the dictionary values.
            var names = new List<string>(shader.FloatUniforms.Keys);
            foreach (string name in names)
            {
                var (value, min, max) = shader.FloatUniforms[name];
                ctx.Ui.Text(FriendlyName(name), rect.X + 8, y);
                float newValue = ctx.Ui.Slider(name, new UiRect(rect.X + 8, y + 16, rect.W - 70, 10), value, min, max);
                shader.SetFloat(name, newValue);
                y += 42;
            }

            y += 10;
            ctx.Ui.Text("Sun / Fog Colour", rect.X + 8, y);
        }

        private static string FriendlyName(string uniform) => uniform switch
        {
            "uRoughness" => "Roughness",
            "uMetallic" => "Metallic",
            "uAmbient" => "Ambient",
            "uExposure" => "Exposure",
            "uFogDensity" => "Fog Density",
            _ => uniform,
        };
    }
}
