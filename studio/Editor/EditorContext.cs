using Tungsten.Core;
using Tungsten.Rendering;
using Tungsten.World;
using TungstenStudio.Editor.UI;

namespace TungstenStudio.Editor
{
    public enum EditTool { Select, Move, Rotate, Scale }

    /// <summary>
    /// Grab-bag of state every panel needs: a reference to the running
    /// Engine, the loaded icon set, the immediate-mode UI batcher, which
    /// entity is selected, which gizmo tool is active, and the drag-and-drop
    /// state used while dragging a prefab out of the Asset Store panel.
    /// Passed by reference into every Panel.Draw() call instead of each
    /// panel reaching for globals, so panels stay independently testable.
    /// </summary>
    public class EditorContext
    {
        public readonly Engine Engine;
        public readonly IconRegistry Icons;
        public readonly ImmediateUi Ui;

        public Entity? SelectedEntity;
        public EditTool ActiveTool = EditTool.Select;
        public Camera EditorCamera = new(new OpenTK.Mathematics.Vector3(6, 4, 8));

        // --- Asset Store drag state ---
        public Tungsten.World.Prefab? DraggingPrefab;

        public UiRect ViewportScreenRect;
        public bool ViewportHovered;

        public EditorContext(Engine engine, IconRegistry icons, ImmediateUi ui)
        {
            Engine = engine;
            Icons = icons;
            Ui = ui;
        }
    }
}
