using System;
using System.Collections.Generic;
using OpenTK.Mathematics;
using Tungsten.Rendering;

namespace Tungsten.World
{
    /// <summary>Position/rotation/scale. Nothing fancy - no scene graph parenting yet.</summary>
    public class Transform
    {
        public Vector3 Position;
        public Quaternion Rotation = Quaternion.Identity;
        public Vector3 Scale = Vector3.One;

        public Matrix4 GetMatrix() =>
            Matrix4.CreateScale(Scale) * Matrix4.CreateFromQuaternion(Rotation) * Matrix4.CreateTranslation(Position);
    }

    /// <summary>
    /// The universal runtime object. Everything dragged in from the Asset
    /// Store - a crate, a gun, a car, the player capsule - is an Entity.
    /// Kept as plain data + optional component references rather than a full
    /// ECS, since a small hand-rolled scene graph is far easier to read and
    /// extend than a generic ECS for a project this size.
    /// </summary>
    public class Entity
    {
        public string Name;
        public Guid Id { get; } = Guid.NewGuid();
        public Transform Transform { get; } = new();

        public Mesh? Mesh;
        public Material? Material;

        /// <summary>Axis-aligned half-extents in local space, used by SimplePhysicsWorld for collision.</summary>
        public Vector3 ColliderHalfExtents = new(0.5f, 0.5f, 0.5f);
        public bool IsStatic = true; // true = level geometry/props, false = physics-driven (player, vehicle, dropped prop)

        public Entity(string name) => Name = name;
    }

    /// <summary>Flat list of every entity currently placed in the level being edited/played.</summary>
    public class Scene
    {
        public readonly List<Entity> Entities = new();
        public string Name = "Untitled Scene";

        public Entity Spawn(Entity e)
        {
            Entities.Add(e);
            return e;
        }

        public void Destroy(Entity e) => Entities.Remove(e);
    }
}
