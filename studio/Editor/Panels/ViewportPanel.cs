using System;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Tungsten.Core;
using Tungsten.Rendering;
using Tungsten.World;
using TungstenStudio.Editor.UI;

namespace TungstenStudio.Editor.Panels
{
    /// <summary>
    /// The "3D" tab: the actual rendered scene. In Edit mode you get a free
    /// Unity/Roblox-Studio-style fly camera (right-mouse + WASD); press Play
    /// (or F5) and control hands over to the built-in FirstPersonController.
    /// Also the drop target for prefabs dragged from the Asset Store panel.
    /// </summary>
    public class ViewportPanel
    {
        public void Draw(EditorContext ctx, Input input, UiRect rect, float deltaTime)
        {
            ctx.ViewportScreenRect = rect;
            ctx.ViewportHovered = rect.Contains(ctx.Ui.MousePos);

            // --- Edit-mode fly camera: hold right mouse to look + WASD to move ---
            if (!ctx.Engine.IsPlaying && ctx.ViewportHovered && input.IsMouseDown(MouseButton.Right))
            {
                ctx.EditorCamera.AddLook(input.MouseDelta.X * 0.15f, -input.MouseDelta.Y * 0.15f);
                float speed = (input.IsDown(Keys.LeftShift) ? 12f : 5f) * deltaTime;
                if (input.IsDown(Keys.W)) ctx.EditorCamera.Position += ctx.EditorCamera.Front * speed;
                if (input.IsDown(Keys.S)) ctx.EditorCamera.Position -= ctx.EditorCamera.Front * speed;
                if (input.IsDown(Keys.D)) ctx.EditorCamera.Position += ctx.EditorCamera.Right * speed;
                if (input.IsDown(Keys.A)) ctx.EditorCamera.Position -= ctx.EditorCamera.Right * speed;
                if (input.IsDown(Keys.E)) ctx.EditorCamera.Position += Vector3.UnitY * speed;
                if (input.IsDown(Keys.Q)) ctx.EditorCamera.Position -= Vector3.UnitY * speed;
            }

            Camera activeCamera = ctx.Engine.IsPlaying ? ctx.Engine.Player.Camera : ctx.EditorCamera;

            // Render the 3D scene into exactly this panel's rectangle. GL's
            // viewport/scissor origin is the window's BOTTOM-left corner,
            // while our UI rects use a top-left origin (matching mouse
            // coordinates), so the Y coordinate has to be flipped here -
            // skipping this flip is what previously rendered the 3D view
            // into the wrong corner of the window at the wrong size.
            int glX = (int)rect.X;
            int glY = (int)(ctx.Ui.ScreenHeight - rect.Y - rect.H);
            int glW = Math.Max(1, (int)rect.W);
            int glH = Math.Max(1, (int)rect.H);

            GL.Enable(EnableCap.ScissorTest);
            GL.Scissor(glX, glY, glW, glH);
            GL.Viewport(glX, glY, glW, glH);
            ctx.Engine.Render(activeCamera, glW, glH);
            GL.Disable(EnableCap.ScissorTest);

            // Restore the full-window viewport immediately. Every 2D UI draw
            // call after this one this frame - and the very first UI draw
            // call of next frame, before this panel runs again - assumes
            // GL.Viewport covers the whole window 1:1 with its orthographic
            // projection. Leaving the small 3D sub-viewport active would
            // otherwise squash/clip every panel that draws after this one
            // into that tiny rectangle, which is exactly the "engine renders
            // tiny in a corner and no UI shows up at all" bug this fixes.
            GL.Viewport(0, 0, ctx.Ui.ScreenWidth, ctx.Ui.ScreenHeight);

            // --- Select tool: left click picks the closest entity under the cursor ---
            if (!ctx.Engine.IsPlaying && ctx.ActiveTool == EditTool.Select && ctx.ViewportHovered && ctx.Ui.MousePressed)
                TrySelect(ctx, activeCamera, rect);

            // --- Drop target: releasing a dragged prefab over the viewport spawns it ---
            if (ctx.DraggingPrefab != null && ctx.Ui.MouseReleased && ctx.ViewportHovered)
            {
                Vector3 spawnPos = ScreenToGroundPlane(ctx, activeCamera, rect);
                ctx.SelectedEntity = ctx.DraggingPrefab.Instantiate(ctx.Engine.Scene, ctx.Engine.Renderer, spawnPos);
                Logger.Info($"Placed '{ctx.DraggingPrefab.Name}' at {spawnPos}.");
                ctx.DraggingPrefab = null;
            }

            // Panel border + label
            ctx.Ui.Text(ctx.Engine.IsPlaying ? "3D  (Playing)" : "3D  (Edit - RMB to fly)", rect.X + 6, rect.Y + 4);
        }

        private static void TrySelect(EditorContext ctx, Camera camera, UiRect rect)
        {
            Vector2 local = ctx.Ui.MousePos - new Vector2(rect.X, rect.Y);
            Vector3 dir = ScreenPointToRay(camera, local, rect.W, rect.H);
            if (ctx.Engine.Physics.Raycast(camera.Position, dir, 500f, out Entity? hit, out _))
                ctx.SelectedEntity = hit;
        }

        private static Vector3 ScreenToGroundPlane(EditorContext ctx, Camera camera, UiRect rect)
        {
            Vector2 local = ctx.Ui.MousePos - new Vector2(rect.X, rect.Y);
            Vector3 dir = ScreenPointToRay(camera, local, rect.W, rect.H);
            // Intersect with the y=0 ground plane.
            if (MathF.Abs(dir.Y) < 1e-4f) return camera.Position + camera.Front * 5f;
            float t = -camera.Position.Y / dir.Y;
            if (t < 0) t = 5f;
            return camera.Position + dir * t;
        }

        private static Vector3 ScreenPointToRay(Camera camera, Vector2 local, float w, float h)
        {
            float ndcX = (2f * local.X / w) - 1f;
            float ndcY = 1f - (2f * local.Y / h);
            Matrix4 invVp = Matrix4.Invert(camera.GetViewMatrix() * camera.GetProjectionMatrix());
            Vector4 near = Vector4.TransformRow(new Vector4(ndcX, ndcY, -1f, 1f), invVp);
            Vector4 far = Vector4.TransformRow(new Vector4(ndcX, ndcY, 1f, 1f), invVp);
            near /= near.W; far /= far.W;
            return (far.Xyz - near.Xyz).Normalized();
        }
    }
}
