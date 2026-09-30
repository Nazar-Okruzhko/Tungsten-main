using OpenTK.Mathematics;
using TungstenStudio.Editor.UI;

namespace TungstenStudio.Editor.Panels
{
    /// <summary>The "Explorer"/hierarchy tree (flat, since Entities don't nest yet): click a row to select it.</summary>
    public class HierarchyPanel
    {
        public void Draw(EditorContext ctx, UiRect rect)
        {
            ctx.Ui.Rect(rect, new Color4(0.13f, 0.13f, 0.15f, 1f));

            float y = rect.Y + 8;
            foreach (var entity in ctx.Engine.Scene.Entities)
            {
                var rowRect = new UiRect(rect.X + 4, y, rect.W - 8, 22);
                bool selected = ctx.SelectedEntity == entity;
                ctx.Ui.Rect(rowRect, selected ? new Color4(0.20f, 0.42f, 0.75f, 1f) : new Color4(0.17f, 0.17f, 0.19f, 1f));
                ctx.Ui.Text(entity.Name, rowRect.X + 6, rowRect.Y + 4);
                if (rowRect.Contains(ctx.Ui.MousePos) && ctx.Ui.MousePressed)
                    ctx.SelectedEntity = entity;
                y += 24;
            }
        }
    }
}
