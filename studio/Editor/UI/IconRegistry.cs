using System.Collections.Generic;
using System.IO;
using Tungsten.Core;
using Tungsten.Rendering;

namespace TungstenStudio.Editor.UI
{
    /// <summary>
    /// Loads shared/Icons/*.png once at startup. Every toolbar/asset-store
    /// button in the Studio looks its texture up by the exact same name as
    /// the action it performs (e.g. the "Play" button uses Icons/Play.png,
    /// the "Save" button uses Icons/Save.png) - this is what the brief meant
    /// by "buttons will use the exact same named icons as the button names".
    /// </summary>
    public class IconRegistry
    {
        private readonly Dictionary<string, Texture> _icons = new();

        public void LoadAll(string iconsDirectory)
        {
            if (!Directory.Exists(iconsDirectory))
            {
                Logger.Warn($"Icons directory not found: {iconsDirectory}");
                return;
            }
            foreach (var file in Directory.GetFiles(iconsDirectory, "*.png"))
            {
                string name = Path.GetFileNameWithoutExtension(file);
                _icons[name] = new Texture(file);
            }
            Logger.Info($"Loaded {_icons.Count} editor icons from {iconsDirectory}");
        }

        /// <summary>Returns the icon's GL texture handle by name (matches the .png filename, no extension).</summary>
        public int Get(string name) => _icons.TryGetValue(name, out var t) ? t.Handle : 0;
        public bool Has(string name) => _icons.ContainsKey(name);
    }
}
