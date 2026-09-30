using OpenTK.Mathematics;
using Tungsten.Core;
using TungstenStudio.Editor.UI;

namespace TungstenStudio.Editor.Panels
{
    /// <summary>The bottom Output/console strip, mirroring Roblox Studio's / Unity's Console window.</summary>
    public class OutputPanel
    {
        public void Draw(EditorContext ctx, UiRect rect)
        {
            ctx.Ui.Rect(rect, new Color4(0.08f, 0.08f, 0.09f, 1f));
            var entries = Logger.Snapshot();

            int maxLines = System.Math.Max(1, (int)((rect.H - 8) / 16));
            int start = System.Math.Max(0, entries.Length - maxLines);
            float y = rect.Y + 4;
            for (int i = start; i < entries.Length; i++)
            {
                var e = entries[i];
                var color = e.Level switch
                {
                    LogLevel.Error => System.Drawing.Color.OrangeRed,
                    LogLevel.Warning => System.Drawing.Color.Khaki,
                    _ => System.Drawing.Color.Gainsboro,
                };
                ctx.Ui.Text($"[{e.Time:HH:mm:ss}] {e.Message}", rect.X + 6, y, color);
                y += 16;
            }
        }
    }
}
