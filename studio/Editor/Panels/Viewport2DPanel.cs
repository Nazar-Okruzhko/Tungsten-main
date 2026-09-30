using System.Collections.Generic;
using OpenTK.Mathematics;
using TungstenStudio.Editor.UI;

namespace TungstenStudio.Editor.Panels
{
    /// <summary>
    /// The "2D" tab: a flat canvas for UI/sprite work, kept deliberately
    /// separate from the 3D scene graph (the same split Unity draws between
    /// its Scene view and a Canvas/UI view). Click an icon in the strip along
    /// the bottom to stamp a sprite onto the canvas at the clicked position.
    /// </summary>
    public class Viewport2DPanel
    {
        private readonly List<(string icon, Vector2 pos)> _sprites = new();
        private static readonly string[] PaletteIcons = { "Sprite2D", "Sound", "Light" };

        public void Draw(EditorContext ctx, UiRect rect)
        {
            ctx.Ui.Rect(rect, new Color4(0.10f, 0.10f, 0.12f, 1f));
            ctx.Ui.Text("2D Canvas - click palette then click canvas to place", rect.X + 8, rect.Y + 6);

            var canvasRect = new UiRect(rect.X + 4, rect.Y + 26, rect.W - 8, rect.H - 60);
            ctx.Ui.Rect(canvasRect, new Color4(0.16f, 0.16f, 0.19f, 1f));

            foreach (var (icon, pos) in _sprites)
                ctx.Ui.Image(new UiRect(canvasRect.X + pos.X - 16, canvasRect.Y + pos.Y - 16, 32, 32), ctx.Icons.Get(icon));

            // Palette strip along the bottom of the panel.
            float px = rect.X + 8;
            float py = rect.Y + rect.H - 28;
            string? picked = null;
            foreach (var icon in PaletteIcons)
            {
                var tile = new UiRect(px, py, 24, 24);
                if (ctx.Ui.IconButton("2d_" + icon, tile, ctx.Icons.Get(icon)))
                    picked = icon;
                px += 30;
            }

            if (picked != null) _lastPicked = picked;
            if (_lastPicked != null && canvasRect.Contains(ctx.Ui.MousePos) && ctx.Ui.MousePressed)
            {
                var local = ctx.Ui.MousePos - new Vector2(canvasRect.X, canvasRect.Y);
                _sprites.Add((_lastPicked, local));
            }
        }

        private string? _lastPicked;
    }
}
