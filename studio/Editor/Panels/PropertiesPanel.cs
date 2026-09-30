using OpenTK.Mathematics;
using TungstenStudio.Editor.UI;

namespace TungstenStudio.Editor.Panels
{
    /// <summary>The Inspector: read the selected entity's transform and nudge it with +/- step buttons.</summary>
    public class PropertiesPanel
    {
        public void Draw(EditorContext ctx, UiRect rect)
        {
            ctx.Ui.Rect(rect, new Color4(0.13f, 0.13f, 0.15f, 1f));

            if (ctx.SelectedEntity == null)
            {
                ctx.Ui.Text("(nothing selected)", rect.X + 8, rect.Y + 30, System.Drawing.Color.Gray);
                return;
            }

            var e = ctx.SelectedEntity;
            float y = rect.Y + 30;
            ctx.Ui.Text($"Name: {e.Name}", rect.X + 8, y); y += 22;
            ctx.Ui.Text($"Static: {e.IsStatic}", rect.X + 8, y); y += 26;

            y = DrawVectorField(ctx, rect, y, "Position", ref e.Transform.Position, 0.25f);
            y = DrawVectorField(ctx, rect, y, "Scale", ref e.Transform.Scale, 0.1f);

            if (e.Material != null)
            {
                y += 6;
                ctx.Ui.Text("Albedo (drag a Shader-panel style tweak later)", rect.X + 8, y);
            }
        }

        private static float DrawVectorField(EditorContext ctx, UiRect rect, float y, string label, ref Vector3 v, float step)
        {
            ctx.Ui.Text(label, rect.X + 8, y);
            y += 18;
            string[] axisNames = { "X", "Y", "Z" };
            float[] axisVals = { v.X, v.Y, v.Z };
            for (int i = 0; i < 3; i++)
            {
                float x = rect.X + 8;
                ctx.Ui.Text($"{axisNames[i]}: {axisVals[i]:F2}", x, y);
                if (ctx.Ui.TextButton(new UiRect(x + 110, y - 2, 20, 18), "-"))
                    axisVals[i] -= step;
                if (ctx.Ui.TextButton(new UiRect(x + 134, y - 2, 20, 18), "+"))
                    axisVals[i] += step;
                y += 22;
            }
            v = new Vector3(axisVals[0], axisVals[1], axisVals[2]);
            return y + 6;
        }
    }
}
