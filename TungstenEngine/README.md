# Tungsten Engine
### id Tech 5-tier · .NET 8 · OpenTK 4 · OpenGL 4.5

A **Roblox Studio-inspired** game engine with a deferred PBR rendering pipeline,
built-in First Person Character Controller, Gun System, Driving System,
universal drag-and-drop Asset Store, and a live Shader Configuration panel.

---

## Folder Structure

```
TungstenEngine/
├── studio/                  ← Editor UI (the application the designer opens)
│   ├── Program.cs           ← Entry point; creates the OpenTK window
│   ├── StudioWindow.cs      ← Main window; owns all panels + engine host
│   ├── Panels.cs            ← Hierarchy / Viewport / Properties / Asset /
│   │                           Console / Shader panels
│   └── Shaders/             ← GLSL source files loaded at runtime
│       ├── geometry.vert    ← Geometry pass — transforms vertices to G-Buffer
│       ├── geometry.frag    ← Fills G-Buffer (position / normal / albedo)
│       ├── lighting.frag    ← Deferred PBR Cook-Torrance + PCF shadows + fog
│       └── final.frag       ← FXAA + Bloom + ACES tonemap + colour grade
│
├── tungsten/                ← The engine runtime (referenced by studio)
│   ├── Core/
│   │   ├── Scene.cs         ← Root container for all Entities
│   │   ├── Entity.cs        ← Game object: name + hierarchy + Transform + Components
│   │   ├── Transform.cs     ← Position / Rotation (quaternion) / Scale → ModelMatrix
│   │   └── Component.cs     ← Abstract base all behaviours extend
│   ├── Rendering/
│   │   ├── Renderer.cs      ← Deferred pipeline: Geometry → SSAO → Light → Bloom → Final
│   │   │                       ShaderSettings exposes all sliders for the Shader Panel
│   │   └── Camera.cs        ← View + Projection matrices; MeshRenderer draw calls
│   ├── Gameplay/
│   │   ├── FirstPersonController.cs  ← WASD + mouse-look + jump + crouch + head-bob
│   │   ├── GunSystem.cs              ← Hitscan & projectile; magazine; recoil; spread
│   │   └── DrivingSystem.cs          ← Arcade 4-wheel vehicle; steering; drift
│   └── Assets/
│       └── AssetStore.cs    ← Scans shared/; drag-drop registry; search; manifest
│
├── shared/                  ← Universal project assets (platform-agnostic)
│   ├── BITMAP/              ← .bmp textures
│   ├── PNG/                 ← .png textures / sprites
│   ├── JPG/                 ← .jpg photo textures
│   ├── Icons/               ← UI icons (toolbar, panel headers, asset types)
│   └── Audio/               ← .wav / .ogg sound effects and music
│
└── libs/                    ← External native libraries
    └── README.md            ← (CUDA RT, PhysX, OpenAL native .so/.dll go here)
```

---

## Build & Run

```bash
# Restore NuGet packages
dotnet restore TungstenEngine.sln

# Build everything
dotnet build TungstenEngine.sln -c Release

# Run the Studio
dotnet run --project studio/studio.csproj
```

> **Requirements:** .NET 8 SDK · GPU with OpenGL 4.5 support · Linux / Windows / macOS

---

## Studio Layout (Roblox / Unity inspired)

```
┌──────────────────────────────────────────────────────────────────────────┐
│  [File]  [Edit]  [View]  [Insert]  [Tools]  ▶Play  ■Stop  ●Record  [Help]│
├─────────────────────────────────────────────────────────────────────────-│
│  Toolbar: [Select▼] [Move] [Rotate] [Scale]  [Snap: 1u] [Local/World]   │
├────────────────┬──────────────────────────────────┬──────────────────────┤
│  HIERARCHY     │          3D VIEWPORT             │    PROPERTIES        │
│  (Explorer)    │     (OpenGL 4.5 deferred PBR)    │    (Inspector)       │
│                │                                  │                      │
│  ▸ Ground      │  ← Right-drag: free-fly camera   │  Transform           │
│  ▸ Cube        │  ← WASD+QE: pan/elevate          │   Position  [0,1,0]  │
│  ▸ SunLight    │  ← Ctrl+Z: undo                  │   Rotation  [0,0,0]  │
│  ▸ Player      │                                  │   Scale     [1,1,1]  │
│                │  [Play] [Stop] [Pause]            │                      │
│  [+] Add       │  FPS: 144                        │  MeshRenderer        │
│                │  Objects: 4                      │   Albedo   [■■■]     │
│                │                                  │   Roughness  0.5 ─── │
│                │                                  │   Metallic   0.0 ─── │
│                │                                  │                      │
│                │                                  │  [+ Add Component]   │
├────────────────┴──────────────────────────────────┴──────────────────────┤
│ SHADER CONFIG │             ASSET STORE                │     CONSOLE      │
│               │  [All] [Meshes] [Textures] [Audio] …  │                  │
│ Ambient  0.08 │  🔍 Search…                           │ ▸ Scene loaded   │
│ Sun Int  3.0  │  ┌──┐┌──┐┌──┐┌──┐┌──┐┌──┐┌──┐       │ ▸ Assets: 0      │
│ SSAO ✓   0.5  │  │  ││  ││  ││  ││  ││  ││  │       │ ▸ GL 4.5 ready   │
│ Bloom ✓  1.2  │  └──┘└──┘└──┘└──┘└──┘└──┘└──┘       │                  │
│ Exposure 1.0  │  Cube  Sphere Plane  Car  Gun  Tree   │ [Clear]          │
│ ACES ✓        │  ← Drag onto viewport to place →      │                  │
└───────────────┴────────────────────────────────────────┴─────────────────┘
```

---

## Built-in Systems

### First Person Character Controller
- WASD movement · Sprint (Shift) · Crouch (Ctrl) · Jump (Space)
- Mouse-look with configurable sensitivity and pitch clamp
- Head-bob animation (sine wave on camera Y, speed-linked)
- Footstep audio cue events

### Gun System
- **Hitscan** (instant ray-cast) or **Projectile** (ballistic arc) modes
- Magazine + reserve ammo with reload cycle
- Accuracy spread: grows on each shot, recovers over time
- Recoil pitch kick fed back to the character controller
- Events: `OnFire` · `OnHit` · `OnReload` · `OnEmpty`

### Driving System
- Bicycle kinematic model with Ackermann steering
- Throttle · Brake · Handbrake (drift)
- Per-frame lateral slip model
- Events: `OnEnterVehicle` · `OnExitVehicle` · `OnWheelSlip`

### Asset Store
- Scans `shared/` on startup — zero config
- Universal assets work in any scene
- Drag from Asset Panel → drop on viewport → entity spawned
- Full-text search by name and type tag

### Rendering Pipeline (best-tier graphics)
| Pass | Technology |
|------|-----------|
| Geometry | Deferred G-Buffer (position / normal / albedo+roughness) |
| Shadows | Cascaded shadow maps with PCF soft edges |
| SSAO | Screen-space ambient occlusion (configurable samples) |
| Lighting | Cook-Torrance PBR BRDF (GGX + Smith + Schlick) |
| Bloom | Threshold extract → dual Kawase blur → additive blend |
| Tone Map | ACES filmic or Reinhard, exposure, contrast, saturation |
| AA | FXAA 3.11 post-process |

### Shader Slide-Bar Panel
Every parameter in `ShaderSettings` maps 1:1 to a labelled slider in the
bottom-left Shader panel.  Changes apply on the next frame — no restart.

**Coming next:** Scratch-like block-based visual shader graph (you'll help design the block types!).

---

## Technology Stack

| Layer | Library | Version |
|-------|---------|---------|
| Windowing & GL | OpenTK | 4.8.2 |
| Math | OpenTK.Mathematics | 4.8.2 |
| Serialisation | Newtonsoft.Json | 13.0.3 |
| Runtime | .NET | 8.0 |
| Shader language | GLSL | 4.50 |

---

## Adding an Asset

1. Drop your `.obj` / `.png` / `.wav` / etc. into the appropriate `shared/` subfolder.
2. Re-run or call `AssetStore.Instance.ScanSharedFolder()` in the console.
3. The asset appears in the Asset Store panel immediately.
4. Drag it onto the viewport — done.
