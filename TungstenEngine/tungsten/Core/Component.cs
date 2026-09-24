// =============================================================================
// TUNGSTEN ENGINE — Core/Component.cs
// Base class for all Components that can be attached to an Entity.
// Components are the "behaviour units" of the engine — Mesh, Collider, Script,
// CharacterController, Gun, VehicleController, etc. all extend this class.
// =============================================================================

namespace Tungsten.Core
{
    /// <summary>
    /// Abstract base for every behaviour/data component.
    /// Extend this to add custom logic to Entities.
    /// </summary>
    public abstract class Component
    {
        // ---------------------------------------------------------------------------
        // Back-reference to the owning Entity (set by Entity.AddComponent)
        // ---------------------------------------------------------------------------

        /// <summary>The Entity this component is attached to.</summary>
        public Entity Owner { get; internal set; } = null!;

        // ---------------------------------------------------------------------------
        // Convenience shortcut — access the owner's Transform directly
        // ---------------------------------------------------------------------------

        protected Transform Transform => Owner.Transform;

        // ---------------------------------------------------------------------------
        // Lifecycle hooks — override in subclasses
        // ---------------------------------------------------------------------------

        /// <summary>
        /// Called once when the component is first attached and the scene starts.
        /// Override to initialise GPU resources, physics bodies, etc.
        /// </summary>
        public virtual void Start() { }

        /// <summary>
        /// Called every frame by Entity.Update().
        /// <paramref name="dt"/> = delta-time in seconds.
        /// </summary>
        public virtual void Update(float dt) { }

        /// <summary>
        /// Called when the component or its owner is removed from the scene.
        /// Override to release GPU buffers, physics handles, audio sources, etc.
        /// </summary>
        public virtual void Dispose() { }
    }
}
