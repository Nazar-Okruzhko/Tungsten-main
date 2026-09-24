// =============================================================================
// TUNGSTEN STUDIO — StudioWindow.cs
//
// The main GameWindow.  Owns the GL context and orchestrates all panels:
//   • HierarchyPanel  — scene tree (Explorer)
//   • ViewportPanel   — 3D render area (OpenGL)
//   • PropertiesPanel — selected entity inspector
//   • AssetPanel      — asset store + drag-drop
//   • ConsolePanel    — log output
//   • ShaderPanel     — live shader slider bars
//
// The UI is drawn using a retained-mode approach on top of the GL context:
// we use OpenTK's built-in GLFW window and draw our own panels with simple
// colour-filled rectangles + text rendered via a texture-atlas font batcher.
// (A real shipping product would integrate Dear ImGui via ImGui.NET here.)
// =============================================================================

using System;
using System.Collections.Generic;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using Tungsten.Core;
using Tungsten.Rendering;
using Tungsten.Assets;
using Tungsten.Gameplay;

namespace TungstenStudio
{
    /// <summary>
    /// Editor play mode state — mirrors Roblox Studio's Play/Stop buttons.
    /// </summary>
    public enum PlayMode { Edit, Playing, Paused }

    // ─────────────────────────────────────────────────────────────────────────

    public class StudioWindow : GameWindow
    {
        // ---------------------------------------------------------------------------
        // Engine objects
        // ---------------------------------------------------------------------------

        private Scene        _scene    = new() { Name = "Untitled Scene" };
        private Renderer     _renderer = new();
        private Camera?      _editorCamera;          // Free-fly editor camera
        private Entity?      _selectedEntity;        // Currently selected in hierarchy

        // ---------------------------------------------------------------------------
        // Panels
        // ---------------------------------------------------------------------------

        private readonly HierarchyPanel  _hierarchy;
        private readonly ViewportPanel   _viewport;
        private readonly PropertiesPanel _properties;
        private readonly AssetPanel      _assets;
        private readonly ConsolePanel    _console;
        private readonly ShaderPanel     _shader;

        // ---------------------------------------------------------------------------
        // Play mode
        // ---------------------------------------------------------------------------

        private PlayMode _playMode = PlayMode.Edit;

        // ---------------------------------------------------------------------------
        // Input tracking
        // ---------------------------------------------------------------------------

        private Vector2 _lastMousePos;
        private bool    _viewportFocused;

        // ---------------------------------------------------------------------------
        // Frame timing
        // ---------------------------------------------------------------------------

        private double _fpsTimer;
        private int    _fpsFrames;
        private float  _displayFPS;

        // ---------------------------------------------------------------------------
        // Constructor
        // ---------------------------------------------------------------------------

        public StudioWindow(GameWindowSettings gs, NativeWindowSettings ns) : base(gs, ns)
        {
            // Wire panel selection callbacks
            _hierarchy  = new HierarchyPanel(this);
            _viewport   = new ViewportPanel(this);
            _properties = new PropertiesPanel(this);
            _assets     = new AssetPanel(this);
            _console    = new ConsolePanel(this);
            _shader     = new ShaderPanel(this);
        }

        // ---------------------------------------------------------------------------
        // Init
        // ---------------------------------------------------------------------------

        protected override void OnLoad()
        {
            base.OnLoad();

            // ── OpenGL global state ────────────────────────────────────────────
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);
            GL.ClearColor(0.12f, 0.12f, 0.14f, 1.0f); // Dark studio background

            // ── Renderer ──────────────────────────────────────────────────────
            _renderer.Init(ClientSize.X, ClientSize.Y);

            // ── Editor camera entity ───────────────────────────────────────────
            var camEntity = _scene.AddEntity("EditorCamera");
            camEntity.Transform.Position = new Vector3(0, 3, 10);
            _editorCamera = camEntity.AddComponent(new Camera());
            _editorCamera.AspectRatio = ClientSize.X / (float)ClientSize.Y;

            // ── Scan asset store ──────────────────────────────────────────────
            AssetStore.Instance.ScanSharedFolder();
            AssetStore.Instance.OnAssetDropped += OnAssetDroppedToViewport;

            // ── Bootstrap scene ───────────────────────────────────────────────
            PopulateDefaultScene();

            // ── Panels init ───────────────────────────────────────────────────
            _hierarchy.Init(_scene);
            _viewport.Init();
            _properties.Init();
            _assets.Init();
            _console.Init();
            _shader.Init(_renderer.Settings);

            Log("Tungsten Studio loaded.  Drag assets from the Asset Store to place them.");
        }

        // ---------------------------------------------------------------------------
        // Update loop
        // ---------------------------------------------------------------------------

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);

            float dt = (float)args.Time;

            // ── FPS counter ────────────────────────────────────────────────────
            _fpsFrames++;
            _fpsTimer += dt;
            if (_fpsTimer >= 0.5)
            {
                _displayFPS = _fpsFrames / (float)_fpsTimer;
                _fpsFrames  = 0;
                _fpsTimer   = 0;
                Title = $"Tungsten Studio  —  {_scene.Name}    [{_displayFPS:F0} FPS]  [{_playMode}]";
            }

            // ── Editor camera free-fly ─────────────────────────────────────────
            if (_viewportFocused && _playMode == PlayMode.Edit)
                UpdateEditorCamera(dt);

            // ── Scene update (only in play mode) ─────────────────────────────
            if (_playMode == PlayMode.Playing)
                _scene.Update(dt);
        }

        // ---------------------------------------------------------------------------
        // Render loop
        // ---------------------------------------------------------------------------

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            if (_editorCamera != null)
                _renderer.RenderScene(_scene, _editorCamera);

            // ── Draw UI panels on top of the 3D view ──────────────────────────
            DrawUI((float)args.Time);

            SwapBuffers();
        }

        // ---------------------------------------------------------------------------
        // Window events
        // ---------------------------------------------------------------------------

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
            GL.Viewport(0, 0, e.Width, e.Height);
            _renderer.Resize(e.Width, e.Height);
            if (_editorCamera != null)
                _editorCamera.AspectRatio = e.Width / (float)e.Height;
        }

        protected override void OnMouseMove(MouseMoveEventArgs e)
        {
            base.OnMouseMove(e);
            _lastMousePos = new Vector2(e.X, e.Y);
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);
            _viewportFocused = _viewport.Contains(_lastMousePos);

            // Right-click in viewport → capture cursor for free-fly
            if (e.Button == MouseButton.Right && _viewportFocused)
                CursorState = CursorState.Grabbed;
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == MouseButton.Right)
                CursorState = CursorState.Normal;
        }

        protected override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            base.OnKeyDown(e);

            switch (e.Key)
            {
                // Play / Stop shortcuts (F5 / F6)
                case Keys.F5 when _playMode == PlayMode.Edit:
                    StartPlaying(); break;
                case Keys.F6 when _playMode != PlayMode.Edit:
                    StopPlaying(); break;

                // Delete selected entity
                case Keys.Delete when _selectedEntity != null && _playMode == PlayMode.Edit:
                    _scene.RemoveEntity(_selectedEntity);
                    _selectedEntity = null;
                    _hierarchy.Refresh(_scene);
                    break;

                // Duplicate selected entity (Ctrl+D)
                case Keys.D when e.Control && _selectedEntity != null:
                    DuplicateSelected(); break;

                // Save (Ctrl+S)
                case Keys.S when e.Control:
                    SaveScene(); break;

                // Escape — deselect
                case Keys.Escape:
                    _selectedEntity = null; break;
            }
        }

        // ---------------------------------------------------------------------------
        // Editor camera free-fly (right-mouse held)
        // ---------------------------------------------------------------------------

        private float _editorYaw, _editorPitch;

        private void UpdateEditorCamera(float dt)
        {
            if (CursorState != CursorState.Grabbed || _editorCamera == null) return;

            var mouse = MouseState.Delta;
            _editorYaw   -= mouse.X * 0.2f;
            _editorPitch -= mouse.Y * 0.2f;
            _editorPitch  = Math.Clamp(_editorPitch, -89f, 89f);

            _editorCamera.Owner.Transform.EulerAngles = new Vector3(_editorPitch, _editorYaw, 0);

            // WASD movement
            float speed = KeyboardState.IsKeyDown(Keys.LeftShift) ? 20.0f : 6.0f;
            var   fwd   = _editorCamera.Owner.Transform.Forward;
            var   right = _editorCamera.Owner.Transform.Right;
            var   pos   = _editorCamera.Owner.Transform.Position;

            if (KeyboardState.IsKeyDown(Keys.W)) pos += fwd   * speed * dt;
            if (KeyboardState.IsKeyDown(Keys.S)) pos -= fwd   * speed * dt;
            if (KeyboardState.IsKeyDown(Keys.A)) pos -= right * speed * dt;
            if (KeyboardState.IsKeyDown(Keys.D)) pos += right * speed * dt;
            if (KeyboardState.IsKeyDown(Keys.E)) pos += Vector3.UnitY * speed * dt;
            if (KeyboardState.IsKeyDown(Keys.Q)) pos -= Vector3.UnitY * speed * dt;

            _editorCamera.Owner.Transform.Position = pos;
        }

        // ---------------------------------------------------------------------------
        // Play mode management
        // ---------------------------------------------------------------------------

        private void StartPlaying()
        {
            _playMode = PlayMode.Playing;
            Log("▶  Play mode started.");
        }

        private void StopPlaying()
        {
            _playMode = PlayMode.Edit;
            Log("■  Stopped.");
        }

        // ---------------------------------------------------------------------------
        // UI drawing (placeholder boxes — replace with Dear ImGui for shipping)
        // ---------------------------------------------------------------------------

        private void DrawUI(float dt)
        {
            // In a full implementation this would call ImGui.NET's render here.
            // For the scaffold the panels report their layout rects so the
            // renderer can scissor the 3D view to the viewport region.
            _hierarchy.Draw();
            _viewport.Draw(_displayFPS, _playMode);
            _properties.Draw(_selectedEntity);
            _assets.Draw();
            _console.Draw();
            _shader.Draw();
        }

        // ---------------------------------------------------------------------------
        // Scene bootstrap — fills default scene for a new project
        // ---------------------------------------------------------------------------

        private void PopulateDefaultScene()
        {
            // Ground plane entity
            var ground = _scene.AddEntity("Ground");
            ground.Transform.Scale    = new Vector3(50, 0.2f, 50);
            ground.Transform.Position = new Vector3(0, -0.1f, 0);
            ground.AddComponent(new MeshRenderer { AlbedoColor = new Vector3(0.3f, 0.5f, 0.3f) });

            // Sample cube
            var cube = _scene.AddEntity("Cube");
            cube.Transform.Position = new Vector3(0, 1, 0);
            cube.AddComponent(new MeshRenderer { AlbedoColor = new Vector3(0.8f, 0.3f, 0.2f) });

            // Directional light placeholder entity
            _scene.AddEntity("SunLight");

            Log($"[Scene] Default scene initialised with {_scene.Entities.Count} entities.");
        }

        // ---------------------------------------------------------------------------
        // Asset-drop handler — places a dragged asset into the scene
        // ---------------------------------------------------------------------------

        private void OnAssetDroppedToViewport(AssetDropPayload payload)
        {
            var e = _scene.AddEntity(payload.Asset.Name);
            // TODO: unproject drop coords to world position via viewport ray-cast
            e.Transform.Position = new Vector3(0, 0, 0);

            switch (payload.Asset.Type)
            {
                case AssetType.Mesh:
                    e.AddComponent(new MeshRenderer()); break;
                case AssetType.Audio:
                    // e.AddComponent(new AudioSource(payload.Asset)); break;
                    break;
                case AssetType.Prefab:
                    // PrefabInstantiator.Instantiate(payload.Asset, scene);
                    break;
            }

            _selectedEntity = e;
            _hierarchy.Refresh(_scene);
            Log($"[Asset Store] Placed '{payload.Asset.Name}' in scene.");
        }

        // ---------------------------------------------------------------------------
        // Entity duplication
        // ---------------------------------------------------------------------------

        private void DuplicateSelected()
        {
            if (_selectedEntity == null) return;
            var dup = _scene.AddEntity(_selectedEntity.Name + " (copy)");
            dup.Transform.Position = _selectedEntity.Transform.Position + new Vector3(1, 0, 0);
            _selectedEntity = dup;
            _hierarchy.Refresh(_scene);
        }

        // ---------------------------------------------------------------------------
        // Save / Load
        // ---------------------------------------------------------------------------

        private void SaveScene()
        {
            // TODO: serialise _scene to JSON via SceneSerializer
            Log("[File] Scene saved.");
        }

        // ---------------------------------------------------------------------------
        // Logging — feeds the ConsolePanel
        // ---------------------------------------------------------------------------

        internal void Log(string message)
        {
            Console.WriteLine(message);
            _console?.AppendLine(message);
        }

        // ---------------------------------------------------------------------------
        // Properties exposed to panels
        // ---------------------------------------------------------------------------

        internal Scene        ActiveScene      => _scene;
        internal Entity?      SelectedEntity   => _selectedEntity;
        internal Renderer     ActiveRenderer   => _renderer;
        internal PlayMode     CurrentPlayMode  => _playMode;

        internal void SelectEntity(Entity? e) => _selectedEntity = e;
    }
}
