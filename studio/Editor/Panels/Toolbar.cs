using OpenTK.Mathematics;
using Tungsten.Core;
using TungstenStudio.Editor.UI;

namespace TungstenStudio.Editor.Panels
{
    /// <summary>
    /// Top toolbar. Every button's texture is loaded from shared/Icons under
    /// the exact same name as the action (New.png, Open.png, Save.png,
    /// Select.png, Move.png, Rotate.png, Scale.png, Play.png, Stop.png) - per
    /// the brief's "buttons will use the exact same named icons as the button
    /// names" requirement.
    /// </summary>
    public class Toolbar
    {
        public void Draw(EditorContext ctx, UiRect rect)
        {
            ctx.Ui.Rect(rect, new Color4(0.16f, 0.16f, 0.18f, 1f));
            float x = rect.X + 6;
            const float size = 32f;

            x = IconSlot(ctx, "New", x, rect.Y, size, () => Logger.Info("New scene (stub - clears Entities list)."));
            x = IconSlot(ctx, "Open", x, rect.Y, size, () => Logger.Info("Open scene (stub - wire to a .json scene loader)."));
            x = IconSlot(ctx, "Save", x, rect.Y, size, () => Logger.Info("Save scene (stub - wire to a .json scene writer)."));

            x += 16;
            x = IconSlot(ctx, "Select", x, rect.Y, size, () => ctx.ActiveTool = EditTool.Select, ctx.ActiveTool == EditTool.Select);
            x = IconSlot(ctx, "Move", x, rect.Y, size, () => ctx.ActiveTool = EditTool.Move, ctx.ActiveTool == EditTool.Move);
            x = IconSlot(ctx, "Rotate", x, rect.Y, size, () => ctx.ActiveTool = EditTool.Rotate, ctx.ActiveTool == EditTool.Rotate);
            x = IconSlot(ctx, "Scale", x, rect.Y, size, () => ctx.ActiveTool = EditTool.Scale, ctx.ActiveTool == EditTool.Scale);

            x += 16;
            x = IconSlot(ctx, "Undo", x, rect.Y, size, () => Logger.Info("Undo (stub - wire to a command history stack)."));
            x = IconSlot(ctx, "Redo", x, rect.Y, size, () => Logger.Info("Redo (stub)."));

            // Play/Stop sit at the far right, mirroring Roblox Studio / Unity's toolbar layout.
            float playX = rect.X + rect.W - size * 2 - 20;
            bool playing = ctx.Engine.IsPlaying;
            if (ctx.Ui.IconButton("Play", new UiRect(playX, rect.Y + 4, size, size), ctx.Icons.Get("Play"), active: playing))
            {
                ctx.Engine.SetPlaying(true);
                Logger.Info("▶ Play mode started.");
            }
            if (ctx.Ui.IconButton("Stop", new UiRect(playX + size + 6, rect.Y + 4, size, size), ctx.Icons.Get("Stop"), active: !playing))
            {
                ctx.Engine.SetPlaying(false);
                Logger.Info("■ Stopped - back to Edit mode.");
            }
        }

        private static float IconSlot(EditorContext ctx, string name, float x, float y, float size, System.Action onClick, bool active = false)
        {
            if (ctx.Ui.IconButton(name, new UiRect(x, y + 4, size, size), ctx.Icons.Get(name), active: active))
                onClick();
            return x + size + 6;
        }
    }
}
