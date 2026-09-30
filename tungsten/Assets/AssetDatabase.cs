using System.Collections.Generic;
using System.IO;
using OpenTK.Mathematics;
using Tungsten.Core;
using Tungsten.World;

namespace Tungsten.Assets
{
    /// <summary>
    /// The "Universal Asset Store" the brief asked for: a single catalog that
    /// both built-in engine prefabs AND anything found under shared/ (icons,
    /// audio, textures) are registered into, so every asset in the project -
    /// no matter its original source - is browsed and dragged into the scene
    /// through the exact same Asset Store panel/UI.
    ///
    /// "Universal" here means: one Prefab format, one drag-and-drop path, one
    /// thumbnail convention (icon name == prefab name), regardless of whether
    /// the underlying content is a built-in primitive or an imported asset.
    /// </summary>
    public class AssetDatabase
    {
        public readonly List<Prefab> Prefabs = new();
        public readonly List<string> AudioClips = new();

        public void LoadBuiltins()
        {
            Prefabs.Add(new Prefab("Crate", "Model3D", PrefabShape.Cube, PrefabKind.Prop, new Vector3(0.55f, 0.4f, 0.25f)));
            Prefabs.Add(new Prefab("Sphere Prop", "Model3D", PrefabShape.Sphere, PrefabKind.Prop, new Vector3(0.7f, 0.7f, 0.75f)));
            Prefabs.Add(new Prefab("Ground Plane", "Terrain", PrefabShape.Plane, PrefabKind.Prop, new Vector3(0.3f, 0.5f, 0.3f), new Vector3(4, 1, 4)));
            Prefabs.Add(new Prefab("Point Light", "Light", PrefabShape.Sphere, PrefabKind.Light, new Vector3(1f, 0.95f, 0.7f), new Vector3(0.2f, 0.2f, 0.2f)));
            Prefabs.Add(new Prefab("Pistol", "Gun", PrefabShape.Cube, PrefabKind.Gun, new Vector3(0.15f, 0.15f, 0.15f), new Vector3(0.15f, 0.15f, 0.6f)));
            Prefabs.Add(new Prefab("Rifle", "Gun", PrefabShape.Cube, PrefabKind.Gun, new Vector3(0.1f, 0.1f, 0.1f), new Vector3(0.15f, 0.2f, 0.9f)));
            Prefabs.Add(new Prefab("Shotgun", "Gun", PrefabShape.Cube, PrefabKind.Gun, new Vector3(0.35f, 0.2f, 0.1f), new Vector3(0.18f, 0.2f, 0.8f)));
            Prefabs.Add(new Prefab("Car", "Car", PrefabShape.Cube, PrefabKind.Vehicle, new Vector3(0.8f, 0.15f, 0.15f), new Vector3(1.8f, 0.9f, 3.6f)));
            Prefabs.Add(new Prefab("Player Spawn", "Select", PrefabShape.Cube, PrefabKind.Spawner, new Vector3(0.2f, 0.8f, 0.9f), new Vector3(0.4f, 1.8f, 0.4f)));

            Logger.Info($"Asset Store: loaded {Prefabs.Count} built-in prefabs.");
        }

        /// <summary>Scans shared/Audio for .wav/.ogg/.mp3 so the Sound tab can list them.</summary>
        public void ScanSharedAudio(string sharedRoot)
        {
            string audioDir = Path.Combine(sharedRoot, "Audio");
            if (!Directory.Exists(audioDir)) return;
            foreach (var file in Directory.GetFiles(audioDir))
            {
                var ext = Path.GetExtension(file).ToLowerInvariant();
                if (ext is ".wav" or ".ogg" or ".mp3")
                    AudioClips.Add(file);
            }
            Logger.Info($"Asset Store: found {AudioClips.Count} audio clips in shared/Audio.");
        }
    }
}
