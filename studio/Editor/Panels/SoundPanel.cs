using System.IO;
using OpenTK.Mathematics;
using TungstenStudio.Editor.UI;

namespace TungstenStudio.Editor.Panels
{
    /// <summary>The "Sound" tab: every audio clip found under shared/Audio at boot.</summary>
    public class SoundPanel
    {
        public void Draw(EditorContext ctx, UiRect rect)
        {
            ctx.Ui.Rect(rect, new Color4(0.13f, 0.13f, 0.15f, 1f));
            ctx.Ui.Text($"Sound ({ctx.Engine.Assets.AudioClips.Count} clips in shared/Audio)", rect.X + 8, rect.Y + 6);

            float y = rect.Y + 30;
            if (ctx.Engine.Assets.AudioClips.Count == 0)
            {
                ctx.Ui.Text("Drop .wav / .ogg / .mp3 files into shared/Audio", rect.X + 8, y, System.Drawing.Color.Gray);
                return;
            }
            foreach (var clip in ctx.Engine.Assets.AudioClips)
            {
                var row = new UiRect(rect.X + 4, y, rect.W - 8, 22);
                ctx.Ui.Rect(row, new Color4(0.17f, 0.17f, 0.19f, 1f));
                ctx.Ui.Text(Path.GetFileName(clip), row.X + 6, row.Y + 4);
                if (ctx.Ui.TextButton(new UiRect(row.X + row.W - 40, row.Y + 1, 36, 20), "▶"))
                {
                    // Hook point: wire an actual audio backend (OpenAL / NAudio) here.
                    Tungsten.Core.Logger.Info($"(stub) Playing {Path.GetFileName(clip)}");
                }
                y += 24;
            }
        }
    }
}
