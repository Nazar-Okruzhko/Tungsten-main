using System;
using System.IO;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Tungsten.Core;
using TungstenStudio.Editor.Panels;
using TungstenStudio.Editor.Splash;
using TungstenStudio.Editor.UI;

namespace TungstenStudio.Editor
{
    public enum CenterTab { ThreeD, TwoD, Sound }

    /// <summary>
    /// The Studio's main window. Owns the boot sequence (splash -> editor)
    /// and the docked panel layout. Deliberately mirrors Roblox Studio's own
    /// default arrangement rather than Unity's: Asset/Prefab Store stacked
    /// over the Shader panel on the LEFT (matching Roblox's Toolbox-over-
    /// Terrain-Editor column), Hierarchy stacked over Properties on the
    /// RIGHT (matching Roblox's Explorer-over-Properties column), the 3D/2D/
    /// Sound viewport centered, a ribbon-style icon toolbar on top, and the
    /// Output console spanning the bottom.
    /// </summary>
    public class EditorWindow : GameWindow
    {
        private readonly string _sharedRoot;
        private SplashScreen? _splash;
        private bool _bootComplete;

        private ImmediateUi _ui = null!;
        private IconRegistry _icons = null!;
        private Tungsten.Core.Engine _engine = null!;
        private EditorContext _ctx = null!;
        private readonly Input _input = new();

        private readonly Toolbar _toolbar = new();
        private readonly HierarchyPanel _hierarchy = new();
        private readonly PropertiesPanel _properties = new();
        private readonly AssetStorePanel _assetStore = new();
        private readonly ShaderPanel _shaderPanel = new();
        private readonly ViewportPanel _viewport3D = new();
        private readonly Viewport2DPanel _viewport2D = new();
        private readonly SoundPanel _soundPanel = new();
        private readonly OutputPanel _output = new();

        private CenterTab _centerTab = CenterTab.ThreeD;

        public EditorWindow(string sharedRoot)
            : base(GameWindowSettings.Default, new NativeWindowSettings
            {
                ClientSize = new Vector2i(1600, 900),
                Title = "Tungsten Studio - id Tech 5 tier",
                APIVersion = new Version(4, 1),
                Profile = ContextProfile.Core,
            })
        {
            _sharedRoot = sharedRoot;
        }

        protected override void OnLoad()
        {
            base.OnLoad();
            GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);

            // Boot the splash immediately so there is zero perceived delay,
            // then finish loading everything else on the very next frames.
            string bimapPath = Path.Combine(_sharedRoot, "Bitmap", "Bitmap1076.bmp");
            _splash = new SplashScreen(bimapPath);
            _ui = new ImmediateUi();
        }

        private void FinishBoot()
        {
            _icons = new IconRegistry();
            _icons.LoadAll(Path.Combine(_sharedRoot, "Icons"));

            _engine = new Tungsten.Core.Engine(_sharedRoot);
            _ctx = new EditorContext(_engine, _icons, _ui);
            _bootComplete = true;
            Logger.Info("Tungsten Studio ready.");
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);
            Time.Tick((float)args.Time);

            if (!_bootComplete)
            {
                _splash?.Update((float)args.Time);
                if (_splash!.Finished) FinishBoot();
                return;
            }

            _input.UpdateFrame(KeyboardState, MouseState);
            if (_suppressNextMouseDelta)
            {
                _input.ClearMouseDelta();
                _suppressNextMouseDelta = false;
            }

            // Mouse-look capture: only lock/hide the cursor while actually
            // playing (first-person) or while free-flying with RMB held in edit mode.
            bool wantCapture = _engine.IsPlaying || (MouseState.IsButtonDown(MouseButton.Right) && _ctx.ViewportHovered);
            if (wantCapture != _mouseWasCaptured)
            {
                // The cursor is about to lock/unlock this frame, which makes
                // GLFW's next reported mouse position jump - flag it so next
                // frame's delta gets zeroed instead of snapping the camera.
                _suppressNextMouseDelta = true;
                _mouseWasCaptured = wantCapture;
            }
            CursorState = wantCapture ? CursorState.Grabbed : CursorState.Normal;
            _input.MouseCaptured = wantCapture;

            _engine.Update(_input);
        }

        private bool _mouseWasCaptured;
        private bool _suppressNextMouseDelta;

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);
            _ui.UpdateInput(MouseState, ClientSize.X, ClientSize.Y);

            if (!_bootComplete)
            {
                _splash?.Render(_ui, ClientSize.X, ClientSize.Y);
                SwapBuffers();
                return;
            }

            DrawEditorLayout();
            SwapBuffers();
        }

        private void DrawEditorLayout()
        {
            // Defensive reset: guarantees every UI draw call this frame maps
            // 1:1 to window pixels even if something upstream (a resize, the
            // very first frame, a future panel) left the GL viewport in a
            // non-full-window state.
            GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);

            GL.ClearColor(0.09f, 0.09f, 0.10f, 1f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            int w = ClientSize.X, h = ClientSize.Y;
            const float ribbonH = 26, toolbarH = 40, leftW = 260, rightW = 280, outputH = 140, tabH = 26, headerH = 22;

            DrawRibbonTabs(new UiRect(0, 0, w, ribbonH));
            _toolbar.Draw(_ctx, new UiRect(0, ribbonH, w, toolbarH));

            float bodyY = ribbonH + toolbarH;
            float bodyH = h - bodyY - outputH;

            // ---- Left column: Asset/Prefab Store (top) over Shader panel (bottom) -
            //      the same "Toolbox over Terrain Editor" stack Roblox Studio ships by default. ----
            float leftSplit = bodyY + bodyH * 0.62f;
            DrawPanelWithHeader("Asset / Prefab Store", new UiRect(0, bodyY, leftW, leftSplit - bodyY), headerH,
                content => _assetStore.Draw(_ctx, content));
            DrawPanelWithHeader("Shader", new UiRect(0, leftSplit, leftW, bodyY + bodyH - leftSplit), headerH,
                content => _shaderPanel.Draw(_ctx, content));

            // ---- Right column: Hierarchy/Explorer (top) over Properties (bottom) -
            //      matches Roblox Studio's Explorer-over-Properties stack. ----
            float rightSplit = bodyY + bodyH * 0.45f;
            DrawPanelWithHeader($"Explorer ({_engine.Scene.Entities.Count})", new UiRect(w - rightW, bodyY, rightW, rightSplit - bodyY), headerH,
                content => _hierarchy.Draw(_ctx, content));
            DrawPanelWithHeader("Properties", new UiRect(w - rightW, rightSplit, rightW, bodyY + bodyH - rightSplit), headerH,
                content => _properties.Draw(_ctx, content));

            // ---- Center: 3D / 2D / Sound tab strip + viewport ----
            var centerRect = new UiRect(leftW, bodyY, w - leftW - rightW, bodyH);
            DrawCenterTabs(centerRect);
            var centerContent = new UiRect(centerRect.X, centerRect.Y + tabH, centerRect.W, centerRect.H - tabH);
            switch (_centerTab)
            {
                case CenterTab.ThreeD: _viewport3D.Draw(_ctx, _input, centerContent, (float)UpdateTime); break;
                case CenterTab.TwoD: _viewport2D.Draw(_ctx, centerContent); break;
                case CenterTab.Sound: _soundPanel.Draw(_ctx, centerContent); break;
            }

            _output.Draw(_ctx, new UiRect(0, h - outputH, w, outputH));
        }

        /// <summary>
        /// Purely cosmetic ribbon tab row across the very top of the window,
        /// styled after Roblox Studio's Home/Avatar/UI/Script/Model/Plugins
        /// strip. Only "Home" does anything right now (it's the only ribbon
        /// page implemented, in Toolbar.Draw below it) - the rest are laid
        /// out as an obvious hook point for future ribbon pages.
        /// </summary>
        private static readonly string[] RibbonTabNames = { "Home", "Avatar", "UI", "Script", "Model", "Plugins" };
        private int _activeRibbonTab;

        private void DrawRibbonTabs(UiRect rect)
        {
            _ui.Rect(rect, new Color4(0.15f, 0.15f, 0.17f, 1f));
            float x = rect.X + 4;
            for (int i = 0; i < RibbonTabNames.Length; i++)
            {
                float tw = 64;
                var tabRect = new UiRect(x, rect.Y, tw, rect.H);
                bool active = i == _activeRibbonTab;
                if (active) _ui.Rect(tabRect, new Color4(0.20f, 0.20f, 0.24f, 1f));
                _ui.Text(RibbonTabNames[i], tabRect.X + 8, tabRect.Y + 5, active ? System.Drawing.Color.White : System.Drawing.Color.Gainsboro);
                if (tabRect.Contains(_ui.MousePos) && _ui.MousePressed) _activeRibbonTab = i;
                x += tw;
            }
        }

        private void DrawPanelWithHeader(string title, UiRect rect, float headerH, Action<UiRect> drawContent)
        {
            // Small titled header strip (like Roblox's "Toolbox" / "Explorer" panel captions) above a panel's content.
            _ui.Rect(new UiRect(rect.X, rect.Y, rect.W, headerH), new Color4(0.19f, 0.19f, 0.22f, 1f));
            _ui.Text(title, rect.X + 8, rect.Y + 5, System.Drawing.Color.Gainsboro);
            drawContent(new UiRect(rect.X, rect.Y + headerH, rect.W, rect.H - headerH));
        }

        private void DrawCenterTabs(UiRect rect)
        {
            string[] names = { "3D", "2D", "Sound" };
            CenterTab[] values = { CenterTab.ThreeD, CenterTab.TwoD, CenterTab.Sound };
            float x = rect.X;
            for (int i = 0; i < names.Length; i++)
            {
                var tabRect = new UiRect(x, rect.Y, 70, 26);
                bool active = _centerTab == values[i];
                _ui.Rect(tabRect, active ? new Color4(0.20f, 0.20f, 0.24f, 1f) : new Color4(0.14f, 0.14f, 0.16f, 1f));
                _ui.Text(names[i], tabRect.X + 10, tabRect.Y + 6);
                if (tabRect.Contains(_ui.MousePos) && _ui.MousePressed) _centerTab = values[i];
                x += 72;
            }
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
            GL.Viewport(0, 0, e.Width, e.Height);
        }
    }
}
