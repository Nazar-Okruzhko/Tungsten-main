// =============================================================================
// TUNGSTEN STUDIO — Program.cs
// Entry point: creates the Studio window (OpenTK GameWindow with a custom
// panel layout) and starts the engine loop.
//
// Window layout (Roblox Studio / Unity inspiration):
// ┌─────────────────────────────────────────────────────────────┐
// │  Menu Bar  [File  Edit  View  Insert  Tools  Play  Help]   │
// │  Toolbar   [▶ Play  ■ Stop  ● Record  ··· gizmo controls]  │
// ├──────────┬──────────────────────────────────┬──────────────┤
// │ Explorer │       3D Viewport (GL)           │  Properties  │
// │(Hierarchy│  ← drag assets here to place →  │  Inspector   │
// │  panel)  │                                  │  panel       │
// ├──────────┴──────────────────────────────────┴──────────────┤
// │  Asset Store / Console / Shader Config  (bottom tabs)      │
// └─────────────────────────────────────────────────────────────┘
// =============================================================================

using System;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.Common;
using OpenTK.Mathematics;

namespace TungstenStudio
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Console.WriteLine("╔════════════════════════════════════════╗");
            Console.WriteLine("║   TUNGSTEN STUDIO  —  id Tech 5 tier   ║");
            Console.WriteLine("║   .NET 8  ·  OpenTK 4  ·  OpenGL 4.5   ║");
            Console.WriteLine("╚════════════════════════════════════════╝");

            var nativeSettings = new NativeWindowSettings
            {
                ClientSize      = new Vector2i(1600, 900),
                Title           = "Tungsten Studio  —  Untitled Scene",
                Profile         = ContextProfile.Core,
                APIVersion      = new Version(4, 5),
                Flags           = ContextFlags.ForwardCompatible | ContextFlags.Debug,
                NumberOfSamples = 0,      // No MSAA — using post-process FXAA
                WindowBorder    = WindowBorder.Resizable,
                IsEventDriven   = false   // Continuous render loop
            };

            var gameSettings = new GameWindowSettings { UpdateFrequency = 0 };

            using var studio = new StudioWindow(gameSettings, nativeSettings);
            studio.Run();
        }
    }
}
