using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Tungsten.Core
{
    /// <summary>
    /// Thin wrapper around OpenTK's raw KeyboardState/MouseState.
    /// Every gameplay system (FirstPersonController, GunSystem, VehicleController)
    /// reads input through this single class instead of touching GLFW directly,
    /// so input can later be swapped for a rebindable action-map without
    /// touching gameplay code.
    /// </summary>
    public class Input
    {
        public KeyboardState Keyboard { get; private set; } = null!;
        public MouseState Mouse { get; private set; } = null!;

        public Vector2 MouseDelta { get; private set; }
        public bool MouseCaptured { get; set; }

        public void UpdateFrame(KeyboardState keyboard, MouseState mouse)
        {
            Keyboard = keyboard;
            Mouse = mouse;
            MouseDelta = mouse.Delta;
        }

        public bool IsDown(Keys key) => Keyboard != null && Keyboard.IsKeyDown(key);
        public bool WasPressed(Keys key) => Keyboard != null && Keyboard.IsKeyPressed(key);
        public bool IsMouseDown(MouseButton b) => Mouse != null && Mouse.IsButtonDown(b);
        public bool WasMousePressed(MouseButton b) => Mouse != null && Mouse.IsButtonPressed(b);

        /// <summary>
        /// Zeroes this frame's mouse delta. Call the frame right after
        /// toggling the OS cursor between visible/locked (e.g. entering Play
        /// mode or holding RMB to fly): GLFW/the OS cursor position jumps
        /// under the hood when that happens, which otherwise reads back as
        /// one huge spurious mouse-delta and snaps the camera violently.
        /// </summary>
        public void ClearMouseDelta() => MouseDelta = Vector2.Zero;
    }
}
