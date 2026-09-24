// =============================================================================
// TUNGSTEN STUDIO — Panels.cs
//
// All six editor panels live here.  In a shipping build each would be a
// separate file and use Dear ImGui for rendering.  For this scaffold they
// hold their layout logic and data; the actual GL draw calls would be
// dispatched through an ImGui or custom widget renderer.
//
// Panels:
//   HierarchyPanel  — left: scene tree (like Roblox Explorer)
//   ViewportPanel   — centre: 3D view with play/gizmo overlays
//   PropertiesPanel — right: inspector for selected entity
//   AssetPanel      — bottom centre: asset store + drag-drop
//   ConsolePanel    — bottom right: log output
//   ShaderPanel     — bottom left: live shader slider configuration
// =============================================================================

using System;
using System.Collections.Generic;
using System.Text;
using OpenTK.Mathematics;
using Tungsten.Core;
using Tungsten.Rendering;
using Tungsten.Assets;

namespace TungstenStudio
{
    // =========================================================================
    // HIERARCHY PANEL  (Explorer — scene tree)
    // =========================================================================

    /// <summary>
    /// Left panel: tree view of every Entity in the scene.
    /// Click to select; Shift+click to multi-select (planned).
    /// </summary>
    public class HierarchyPanel
    {
        private readonly StudioWindow _studio;
        private Scene?  _scene;

        // Flat list of (indent level, entity) for quick drawing
        private readonly List<(int depth, Entity entity)> _flatList = new();

        public HierarchyPanel(StudioWindow studio) => _studio = studio;

        public void Init(Scene scene)
        {
            _scene = scene;
            Refresh(scene);
        }

        /// <summary>Rebuilds the flat display list after a hierarchy change.</summary>
        public void Refresh(Scene scene)
        {
            _scene = scene;
            _flatList.Clear();
            foreach (var e in scene.Entities)
                FlattenRecursive(e, 0);
        }

        private void FlattenRecursive(Entity e, int depth)
        {
            _flatList.Add((depth, e));
            foreach (var child in e.Children) FlattenRecursive(child, depth + 1);
        }

        /// <summary>
        /// Draw call — outputs text representation of the hierarchy.
        /// Replace with ImGui.TreeNode() calls in a UI-complete build.
        /// </summary>
        public void Draw()
        {
            // In this scaffold we emit to console on change; real draw uses GL quads.
            // Each entry is:  "  [icon]  Name  [eye/lock icons]"
        }

        // Exposes the flat list for the GL text batcher
        public IReadOnlyList<(int depth, Entity entity)> FlatList => _flatList;
    }

    // =========================================================================
    // VIEWPORT PANEL  (3D view)
    // =========================================================================

    /// <summary>
    /// Centre panel: shows the 3D rendered scene.
    /// Also draws the play-bar overlay and gizmo controls.
    /// </summary>
    public class ViewportPanel
    {
        private readonly StudioWindow _studio;

        // Pixel region the viewport occupies in the window
        public Box2i Bounds { get; private set; }

        public ViewportPanel(StudioWindow studio) => _studio = studio;

        public void Init()
        {
            // Initial bounds — recalculated on resize
            UpdateBounds(_studio.ClientSize.X, _studio.ClientSize.Y);
        }

        public void UpdateBounds(int winW, int winH)
        {
            // Left panel 240 px, right panel 280 px, top bar 56 px, bottom 220 px
            int x0 = 240, y0 = 56;
            int x1 = winW - 280, y1 = winH - 220;
            Bounds = new Box2i(x0, y0, x1, y1);
        }

        /// <summary>Returns true if screen-space point <paramref name="p"/> is inside the 3D viewport.</summary>
        public bool Contains(Vector2 p) =>
            p.X >= Bounds.Min.X && p.X <= Bounds.Max.X &&
            p.Y >= Bounds.Min.Y && p.Y <= Bounds.Max.Y;

        /// <summary>
        /// Draws the viewport overlay:
        ///   • Play/Pause/Stop buttons (top-centre)
        ///   • Gizmo tool selector (Translate/Rotate/Scale)
        ///   • FPS counter (top-right)
        ///   • HUD info in play mode
        /// </summary>
        public void Draw(float fps, PlayMode mode)
        {
            // Overlay items would be drawn as textured quads + text via font atlas.
            // Emit important info to console title bar instead (handled in StudioWindow).
        }
    }

    // =========================================================================
    // PROPERTIES PANEL  (Inspector — right side)
    // =========================================================================

    /// <summary>
    /// Right panel: shows all components attached to the selected entity.
    /// Each component exposes its public properties as editable fields / sliders.
    /// </summary>
    public class PropertiesPanel
    {
        private readonly StudioWindow _studio;

        public PropertiesPanel(StudioWindow studio) => _studio = studio;

        public void Init() { }

        /// <summary>
        /// Draws the inspector for <paramref name="entity"/>.
        /// Sections:
        ///   • Transform (Position XYZ / Rotation XYZ / Scale XYZ)
        ///   • One collapsible section per Component
        ///   • [Add Component] button at the bottom
        /// </summary>
        public void Draw(Entity? entity)
        {
            if (entity == null) return;

            // ── Transform section ──────────────────────────────────────────────
            // In ImGui:
            //   ImGui.DragFloat3("Position", ref pos);
            //   ImGui.DragFloat3("Rotation", ref rot);
            //   ImGui.DragFloat3("Scale",    ref scale);

            // ── Component sections ────────────────────────────────────────────
            foreach (var component in entity.Components)
            {
                // Reflect public properties and draw them as widgets.
                // Each Component could implement an optional DrawInspector() method
                // for custom inspector layouts (like Unity's custom editors).
                DrawComponentInspector(component);
            }
        }

        private static void DrawComponentInspector(Component component)
        {
            // Use reflection to enumerate [Inspectable] properties.
            // For now, log the component type to the console overlay.
            var type = component.GetType().Name;
            // ImGui.CollapsingHeader(type) → draw properties via reflection
        }
    }

    // =========================================================================
    // ASSET PANEL  (Asset Store — bottom centre)
    // =========================================================================

    /// <summary>
    /// Bottom panel: lists all assets in the project.
    /// Tabs: All | Meshes | Textures | Materials | Prefabs | Audio | Scripts
    /// Search bar + thumbnail grid.
    /// Drag from here → release on viewport → OnAssetDropped fires.
    /// </summary>
    public class AssetPanel
    {
        private readonly StudioWindow _studio;

        // Current filter tab
        private AssetType? _filterType = null; // null = All
        private string      _searchQuery = "";

        // Drag state
        private AssetDescriptor? _dragging;

        public AssetPanel(StudioWindow studio) => _studio = studio;

        public void Init() { }

        public void Draw()
        {
            // ── Tab bar ───────────────────────────────────────────────────────
            // Tabs: [All] [Meshes] [Textures] [Materials] [Prefabs] [Audio] [Scripts]
            // Active tab sets _filterType.

            // ── Search bar ────────────────────────────────────────────────────
            // _searchQuery updated from text input.

            // ── Thumbnail grid ────────────────────────────────────────────────
            // For each asset matching filter + query:
            //   Draw 80×80 px thumbnail quad (from asset.ThumbnailPath or type icon)
            //   Draw asset name below thumbnail
            //   Mouse-down → start drag
            //   Mouse-up over viewport → AssetStore.Instance.NotifyDrop(...)

            var assets = _searchQuery.Length > 0
                ? AssetStore.Instance.Search(_searchQuery)
                : _filterType.HasValue
                    ? AssetStore.Instance.ByType(_filterType.Value)
                    : AssetStore.Instance.All;

            // Layout would position these in a wrapping grid.
        }

        // Called by the window when the user releases the mouse over the viewport
        public void CommitDrop(float normX, float normY)
        {
            if (_dragging == null) return;
            AssetStore.Instance.NotifyDrop(_dragging, normX, normY);
            _dragging = null;
        }
    }

    // =========================================================================
    // CONSOLE PANEL  (Output / Log)
    // =========================================================================

    /// <summary>
    /// Bottom-right panel: scrollable log of engine messages.
    /// Colour-coded by severity: Info (white) / Warning (yellow) / Error (red).
    /// </summary>
    public class ConsolePanel
    {
        private readonly StudioWindow         _studio;
        private readonly List<(string msg, ConsoleLevel level)> _lines = new();
        private bool _scrollToBottom;

        public ConsolePanel(StudioWindow studio) => _studio = studio;

        public void Init() { }

        public void AppendLine(string message, ConsoleLevel level = ConsoleLevel.Info)
        {
            _lines.Add((message, level));
            _scrollToBottom = true;
            if (_lines.Count > 2000) _lines.RemoveAt(0); // Prevent unbounded growth
        }

        public void Draw()
        {
            // Draw a dark scrollable text area.
            // Each line coloured by level:
            //   Info    → #DDDDDD
            //   Warning → #FFD060
            //   Error   → #FF6060
            // Clear button top-right.
        }

        public enum ConsoleLevel { Info, Warning, Error }
    }

    // =========================================================================
    // SHADER PANEL  (Shader Configuration — slider bars)
    // =========================================================================

    /// <summary>
    /// Bottom-left panel: exposes every ShaderSettings property as a labelled
    /// slider bar.  Changes take effect immediately on the next render frame
    /// because ShaderSettings is a reference type shared with the Renderer.
    ///
    /// Future: the "Scratch-like Block-Based Config" system will sit here —
    /// users can build shader graphs by connecting visual blocks.
    /// </summary>
    public class ShaderPanel
    {
        private readonly StudioWindow _studio;
        private ShaderSettings?       _settings;

        public ShaderPanel(StudioWindow studio) => _studio = studio;

        public void Init(ShaderSettings settings) => _settings = settings;

        public void Draw()
        {
            if (_settings == null) return;

            // ── Section: Lighting ──────────────────────────────────────────────
            // SliderFloat("Ambient",      ref _settings.AmbientStrength,  0, 1)
            // SliderFloat("Sun Intensity",ref _settings.SunIntensity,     0, 10)
            // ColourEdit3("Sun Color",    ref _settings.SunColor)

            // ── Section: SSAO ─────────────────────────────────────────────────
            // Checkbox("SSAO",          ref _settings.SSAOEnabled)
            // SliderFloat("Radius",     ref _settings.SSAORadius, 0.1, 2.0)
            // SliderFloat("Bias",       ref _settings.SSAOBias,   0.001, 0.1)
            // SliderInt  ("Samples",    ref _settings.SSAOSamples, 8, 128)
            // SliderFloat("Power",      ref _settings.SSAOPower,  0.5, 4.0)

            // ── Section: Bloom ────────────────────────────────────────────────
            // Checkbox("Bloom",         ref _settings.BloomEnabled)
            // SliderFloat("Threshold",  ref _settings.BloomThreshold, 0.5, 3.0)
            // SliderFloat("Strength",   ref _settings.BloomStrength,  0, 0.5)

            // ── Section: Tone Mapping ─────────────────────────────────────────
            // SliderFloat("Exposure",   ref _settings.Exposure,   0.1, 5.0)
            // SliderFloat("Contrast",   ref _settings.Contrast,   0.5, 2.0)
            // SliderFloat("Saturation", ref _settings.Saturation, 0, 2.0)
            // Checkbox("ACES Filmic",   ref _settings.ACESFilmic)

            // ── Section: Fog ──────────────────────────────────────────────────
            // Checkbox("Fog",           ref _settings.FogEnabled)
            // SliderFloat("Fog Start",  ref _settings.FogStart, 10, 500)
            // SliderFloat("Fog End",    ref _settings.FogEnd,   50, 2000)
            // ColourEdit3("Fog Color",  ref _settings.FogColor)

            // ── Section: Shadows ──────────────────────────────────────────────
            // Checkbox("Shadows",       ref _settings.ShadowsEnabled)
            // ComboBox("Shadow Map",    ["512","1024","2048","4096"])
            // SliderFloat("Bias",       ref _settings.ShadowBias, 0.001, 0.05)
            // SliderFloat("Softness",   ref _settings.ShadowSoftness, 0, 4)

            // ── Section: Anti-Aliasing ────────────────────────────────────────
            // Checkbox("FXAA",          ref _settings.FXAAEnabled)

            // ── Future: Block-Based Config  (Scratch-style visual shader graph)
            // This panel will host the drag-and-drop shader block workspace.
            // Blocks:  [Math] [Texture Sample] [Lerp] [Output] etc.
            // Connections drawn as bezier curves between node ports.
        }
    }
}
