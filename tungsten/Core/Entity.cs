// =============================================================================
// TUNGSTEN ENGINE — Core/Entity.cs
// Entity is the fundamental unit of the scene graph — every object you place
// in the 3D world is an Entity.  It owns a Transform and a list of Components.
// This is analogous to a Roblox BasePart/Model or Unity's GameObject.
//
// Design: Entity-Component architecture keeps data and behaviour separate.
//   - Entity   = identity + hierarchy + Transform
//   - Component = reusable behaviour attached to an Entity (Mesh, Collider, …)
// =============================================================================

using System;
using System.Collections.Generic;
using OpenTK.Mathematics;

namespace Tungsten.Core
{
    /// <summary>
    /// A node in the Scene's entity hierarchy.
    /// Drag one into the world from the Asset Store → it becomes an Entity.
    /// </summary>
    public class Entity
    {
        // ---------------------------------------------------------------------------
        // Identity
        // ---------------------------------------------------------------------------

        /// <summary>Unique ID — used for serialisation and cross-reference.</summary>
        public Guid Id { get; } = Guid.NewGuid();

        /// <summary>Display name shown in the Hierarchy panel.</summary>
        public string Name { get; set; }

        /// <summary>Hidden entities are excluded from rendering and physics.</summary>
        public bool IsVisible { get; set; } = true;

        // ---------------------------------------------------------------------------
        // Hierarchy  (parent ↔ children)
        // ---------------------------------------------------------------------------

        public Entity? Parent { get; private set; }
        public List<Entity> Children { get; } = new();

        public void AddChild(Entity child)
        {
            child.Parent?.Children.Remove(child);
            child.Parent = this;
            Children.Add(child);
        }

        // ---------------------------------------------------------------------------
        // Transform — every Entity has exactly one Transform component
        // ---------------------------------------------------------------------------

        public Transform Transform { get; } = new Transform();

        // ---------------------------------------------------------------------------
        // Generic component list
        // ---------------------------------------------------------------------------

        private readonly List<Component> _components = new();

        /// <summary>Attaches a component and returns it.</summary>
        public T AddComponent<T>(T component) where T : Component
        {
            component.Owner = this;
            _components.Add(component);
            return component;
        }

        /// <summary>Finds the first component of type T, or null.</summary>
        public T? GetComponent<T>() where T : Component
        {
            foreach (var c in _components)
                if (c is T typed) return typed;
            return null;
        }

        /// <summary>Returns all attached components (read-only view).</summary>
        public IReadOnlyList<Component> Components => _components;

        // ---------------------------------------------------------------------------
        // Constructor
        // ---------------------------------------------------------------------------

        public Entity(string name = "Entity") => Name = name;

        // ---------------------------------------------------------------------------
        // Lifecycle
        // ---------------------------------------------------------------------------

        /// <summary>Called every frame by Scene.Update().</summary>
        public void Update(float dt)
        {
            // Update all attached components
            foreach (var c in _components) c.Update(dt);
            // Recurse into children
            foreach (var child in Children) child.Update(dt);
        }
    }
}
