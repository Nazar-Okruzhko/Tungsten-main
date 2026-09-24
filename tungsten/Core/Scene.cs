// =============================================================================
// TUNGSTEN ENGINE — Core/Scene.cs
// The Scene is the root container for all game objects (Entities).
// Analogous to a Roblox "Workspace" — everything placed in the 3D world
// lives inside the active Scene. The Engine holds one Scene at a time.
// =============================================================================

using System.Collections.Generic;
using Tungsten.Core;

namespace Tungsten.Core
{
    /// <summary>
    /// A Scene holds all Entities (game objects) and manages their lifecycle.
    /// The studio can serialize/deserialize a Scene to/from JSON for save/load.
    /// </summary>
    public class Scene
    {
        // ---------------------------------------------------------------------------
        // Public state
        // ---------------------------------------------------------------------------

        /// <summary>Human-readable name shown in the Studio title bar.</summary>
        public string Name { get; set; } = "Untitled Scene";

        /// <summary>All top-level entities in this scene (children are nested inside them).</summary>
        public List<Entity> Entities { get; private set; } = new();

        // ---------------------------------------------------------------------------
        // Scene management
        // ---------------------------------------------------------------------------

        /// <summary>Adds an entity to the scene and returns it (fluent API).</summary>
        public Entity AddEntity(string name = "Entity")
        {
            var e = new Entity(name);
            Entities.Add(e);
            return e;
        }

        /// <summary>Removes an entity by reference.</summary>
        public void RemoveEntity(Entity entity) => Entities.Remove(entity);

        /// <summary>
        /// Finds the first entity whose name matches, searching recursively through children.
        /// Returns null if not found.
        /// </summary>
        public Entity? FindByName(string name)
        {
            foreach (var e in Entities)
            {
                var found = SearchRecursive(e, name);
                if (found != null) return found;
            }
            return null;
        }

        private Entity? SearchRecursive(Entity root, string name)
        {
            if (root.Name == name) return root;
            foreach (var child in root.Children)
            {
                var found = SearchRecursive(child, name);
                if (found != null) return found;
            }
            return null;
        }

        // ---------------------------------------------------------------------------
        // Per-frame update — called by the Engine tick loop
        // ---------------------------------------------------------------------------

        /// <summary>Updates all entities and their components (physics, scripts, etc.).</summary>
        public void Update(float deltaTime)
        {
            foreach (var e in Entities)
                e.Update(deltaTime);
        }
    }
}
