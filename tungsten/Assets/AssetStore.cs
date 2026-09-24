// =============================================================================
// TUNGSTEN ENGINE — Assets/AssetStore.cs
//
// The universal Asset Store — every asset (mesh, texture, material, prefab,
// audio clip, script template) is registered here and can be:
//   • Browsed in the Studio's Asset Store panel
//   • Dragged onto the 3D viewport to place it in the scene (OnDrop event)
//   • Saved as a "universal" asset usable in any project
//
// Assets live in shared/ on disk.  The AssetStore manages:
//   1. Discovery  — scan shared/ sub-folders and build a manifest
//   2. Loading    — lazy-load on first use; keep a ref-counted cache
//   3. Drag-drop  — expose an event the studio wires to viewport drops
// =============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace Tungsten.Assets
{
    // ── Asset type enum ───────────────────────────────────────────────────────

    public enum AssetType { Mesh, Texture, Material, Prefab, Audio, Script, Unknown }

    // ── Asset descriptor (serialised in the manifest JSON) ───────────────────

    public class AssetDescriptor
    {
        public Guid      Id          { get; set; } = Guid.NewGuid();
        public string    Name        { get; set; } = "";
        public AssetType Type        { get; set; }
        public string    RelPath     { get; set; } = ""; // Relative to shared/ root
        public string    Tags        { get; set; } = ""; // Comma-separated for search
        public string    ThumbnailPath { get; set; } = "";

        [JsonIgnore]
        public bool IsLoaded { get; set; }

        [JsonIgnore]
        public object? LoadedData { get; set; } // The actual GPU/CPU object once loaded
    }

    // ── Drag-drop payload ─────────────────────────────────────────────────────

    public class AssetDropPayload
    {
        public AssetDescriptor Asset    { get; set; } = null!;
        public float           DropX    { get; set; } // Normalised viewport X [0,1]
        public float           DropY    { get; set; } // Normalised viewport Y [0,1]
    }

    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Singleton registry for all assets in the project.
    /// </summary>
    public class AssetStore
    {
        // ---------------------------------------------------------------------------
        // Singleton
        // ---------------------------------------------------------------------------

        public static AssetStore Instance { get; } = new AssetStore();
        private AssetStore() { }

        // ---------------------------------------------------------------------------
        // Asset registry
        // ---------------------------------------------------------------------------

        private readonly Dictionary<Guid, AssetDescriptor> _assets = new();

        // ---------------------------------------------------------------------------
        // Paths
        // ---------------------------------------------------------------------------

        /// <summary>Root of the shared/ folder (set by the engine at startup).</summary>
        public string SharedRoot { get; set; } = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "shared");

        // ---------------------------------------------------------------------------
        // Events
        // ---------------------------------------------------------------------------

        /// <summary>
        /// Fired when the user drops an asset onto the 3D viewport.
        /// Studio listens here to instantiate the asset as an Entity.
        /// </summary>
        public event Action<AssetDropPayload>? OnAssetDropped;

        // ---------------------------------------------------------------------------
        // Scanning — walks shared/ and builds the manifest
        // ---------------------------------------------------------------------------

        /// <summary>
        /// Scans the shared/ directory structure and registers all found assets.
        /// Called once at engine startup.
        /// </summary>
        public void ScanSharedFolder()
        {
            _assets.Clear();

            if (!Directory.Exists(SharedRoot))
            {
                Console.WriteLine($"[AssetStore] shared/ not found at: {SharedRoot}");
                return;
            }

            // Walk every file under shared/
            foreach (var file in Directory.EnumerateFiles(SharedRoot, "*.*", SearchOption.AllDirectories))
            {
                var ext  = Path.GetExtension(file).ToLowerInvariant();
                var type = ExtensionToType(ext);
                if (type == AssetType.Unknown) continue;

                var desc = new AssetDescriptor
                {
                    Name    = Path.GetFileNameWithoutExtension(file),
                    Type    = type,
                    RelPath = Path.GetRelativePath(SharedRoot, file),
                    Tags    = type.ToString().ToLower()
                };

                _assets[desc.Id] = desc;
            }

            Console.WriteLine($"[AssetStore] Discovered {_assets.Count} assets.");
        }

        // ---------------------------------------------------------------------------
        // Retrieval
        // ---------------------------------------------------------------------------

        public IEnumerable<AssetDescriptor> All           => _assets.Values;
        public AssetDescriptor?             GetById(Guid id) =>
            _assets.TryGetValue(id, out var d) ? d : null;

        /// <summary>Returns assets whose name or tags contain <paramref name="query"/>.</summary>
        public IEnumerable<AssetDescriptor> Search(string query)
        {
            query = query.ToLower();
            foreach (var a in _assets.Values)
                if (a.Name.ToLower().Contains(query) || a.Tags.ToLower().Contains(query))
                    yield return a;
        }

        /// <summary>Returns all assets of a given type.</summary>
        public IEnumerable<AssetDescriptor> ByType(AssetType type)
        {
            foreach (var a in _assets.Values)
                if (a.Type == type) yield return a;
        }

        // ---------------------------------------------------------------------------
        // Drag-drop integration
        // ---------------------------------------------------------------------------

        /// <summary>
        /// Called by the Studio when the user drops an item from the asset panel
        /// onto the viewport at normalised coordinates (nx, ny).
        /// </summary>
        public void NotifyDrop(AssetDescriptor asset, float nx, float ny)
            => OnAssetDropped?.Invoke(new AssetDropPayload { Asset = asset, DropX = nx, DropY = ny });

        // ---------------------------------------------------------------------------
        // Manifest persistence
        // ---------------------------------------------------------------------------

        public void SaveManifest(string path)
        {
            var json = JsonConvert.SerializeObject(_assets.Values, Formatting.Indented);
            File.WriteAllText(path, json);
        }

        public void LoadManifest(string path)
        {
            if (!File.Exists(path)) return;
            var list = JsonConvert.DeserializeObject<List<AssetDescriptor>>(File.ReadAllText(path));
            if (list == null) return;
            _assets.Clear();
            foreach (var d in list) _assets[d.Id] = d;
        }

        // ---------------------------------------------------------------------------
        // Helper
        // ---------------------------------------------------------------------------

        private static AssetType ExtensionToType(string ext) => ext switch
        {
            ".obj" or ".fbx" or ".gltf" or ".glb"              => AssetType.Mesh,
            ".png" or ".jpg" or ".jpeg" or ".bmp" or ".tga"    => AssetType.Texture,
            ".mat"                                              => AssetType.Material,
            ".prefab" or ".tprefab"                             => AssetType.Prefab,
            ".wav" or ".mp3" or ".ogg" or ".flac"              => AssetType.Audio,
            ".cs" or ".lua"                                     => AssetType.Script,
            _ => AssetType.Unknown
        };
    }
}
