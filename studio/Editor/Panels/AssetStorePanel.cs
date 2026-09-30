using Tungsten.World;
using TungstenStudio.Editor.UI;

namespace TungstenStudio.Editor.Panels
{
    /// <summary>
    /// The Asset/Prefab Store: every prop, gun, vehicle and light the engine
    /// knows about, shown as icon tiles you drag into the 3D viewport. "All
    /// assets are universal" means this same panel, same Prefab type, and
    /// same drag gesture works whether the asset is a built-in primitive or
    /// something scanned in from shared/ - there is no separate "import"
    /// pipeline the user has to think about.
    /// </summary>
    public class AssetStorePanel
    {
        public void Draw(EditorContext ctx, UiRect rect)
        {
            ctx.Ui.Rect(rect, new OpenTK.Mathematics.Color4(0.13f, 0.13f, 0.15f, 1f));

            const float tile = 72f, pad = 8f;
            float x = rect.X + pad, y = rect.Y + pad;
            int col = 0;
            const int columns = 2;

            foreach (Prefab prefab in ctx.Engine.Assets.Prefabs)
            {
                var tileRect = new UiRect(x, y, tile, tile);
                int icon = ctx.Icons.Get(prefab.IconName);
                bool clicked = ctx.Ui.IconButton("asset_" + prefab.Name, tileRect, icon, prefab.Name);

                // Begin a drag when the user presses down on a tile; the ViewportPanel
                // is the drop target and clears DraggingPrefab on mouse release.
                if (tileRect.Contains(ctx.Ui.MousePos) && ctx.Ui.MousePressed)
                    ctx.DraggingPrefab = prefab;

                // A plain click with no drag also works: drop it right in front of the camera.
                if (clicked && ctx.DraggingPrefab == null)
                {
                    var cam = ctx.Engine.IsPlaying ? ctx.Engine.Player.Camera : ctx.EditorCamera;
                    prefab.Instantiate(ctx.Engine.Scene, ctx.Engine.Renderer, cam.Position + cam.Front * 4f);
                }

                col++;
                if (col >= columns) { col = 0; x = rect.X + pad; y += tile + pad; }
                else x += tile + pad;
            }

            // Ghost icon following the cursor while a drag is in progress.
            if (ctx.DraggingPrefab != null)
            {
                var ghost = new UiRect(ctx.Ui.MousePos.X - 24, ctx.Ui.MousePos.Y - 24, 48, 48);
                ctx.Ui.Image(ghost, ctx.Icons.Get(ctx.DraggingPrefab.IconName), new OpenTK.Mathematics.Color4(1f, 1f, 1f, 0.85f));
            }
        }
    }
}
