using OpenTK.Mathematics;
using Tungsten.Rendering;

namespace Tungsten.World
{
    public enum PrefabShape { Cube, Sphere, Plane }

    public enum PrefabKind { Prop, Gun, Vehicle, Light, Spawner }

    /// <summary>
    /// A lightweight, serializable "template" for something the Asset/Prefab
    /// Store can spawn. This is deliberately data-only (no live GL handles)
    /// so a prefab list can be built at editor start-up before any GL context
    /// work happens, and so it maps cleanly onto a future JSON-on-disk format
    /// for "Universal Assets" shared between projects.
    /// </summary>
    public class Prefab
    {
        public string Name;
        public string IconName;       // matches a file in shared/Icons, e.g. "Gun" -> Gun.png
        public PrefabShape Shape;
        public PrefabKind Kind;
        public Vector3 Color = new(0.8f, 0.8f, 0.8f);
        public Vector3 DefaultScale = Vector3.One;

        public Prefab(string name, string iconName, PrefabShape shape, PrefabKind kind, Vector3 color, Vector3? scale = null)
        {
            Name = name;
            IconName = iconName;
            Shape = shape;
            Kind = kind;
            Color = color;
            DefaultScale = scale ?? Vector3.One;
        }

        public Entity Instantiate(Scene scene, Renderer renderer, Vector3 position)
        {
            var mesh = Shape switch
            {
                PrefabShape.Sphere => Mesh.CreateSphere(),
                PrefabShape.Plane => Mesh.CreatePlane(),
                _ => Mesh.CreateCube(),
            };

            var entity = new Entity(Name)
            {
                Mesh = mesh,
                Material = new Material(renderer.LitShader) { Albedo = Color },
                IsStatic = Kind != PrefabKind.Vehicle,
            };
            entity.Transform.Position = position;
            entity.Transform.Scale = DefaultScale;
            return scene.Spawn(entity);
        }
    }
}
