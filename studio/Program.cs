using System;
using System.IO;
using Tungsten.Core;
using TungstenStudio.Editor;

namespace TungstenStudio
{
    /// <summary>
    /// Entry point. Locates the /shared folder next to the executable (it is
    /// copied there automatically at build time - see Studio.csproj) and
    /// boots the EditorWindow, which itself shows the Bitmap1076.bmp splash
    /// before revealing the full Studio layout.
    /// </summary>
    public static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            string exeDir = AppContext.BaseDirectory;
            string sharedRoot = Path.Combine(exeDir, "shared");

            if (!Directory.Exists(sharedRoot))
            {
                Logger.Error($"'shared' folder not found next to the executable at: {sharedRoot}");
                Logger.Error("Make sure the solution's folder layout (studio/libs/shared/tungsten) is intact.");
            }

            using var window = new EditorWindow(sharedRoot);
            window.Run();
        }
    }
}
