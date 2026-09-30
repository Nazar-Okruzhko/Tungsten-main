# Tungsten Studio

A Roblox-Studio-style editor shell (**studio**) built on top of a small
custom game engine/runtime (**tungsten**), written in **C# / .NET 6** using
**OpenTK** for windowing, OpenGL and math. No Visual Studio required — this
was written to be built and run with the `dotnet` CLI or JetBrains Rider.

## Folder layout (as requested)

```
TungstenStudio/
├─ studio/      the engine INTERFACE — the editor shell (the .exe)
├─ libs/        external native libraries (CUDA rt, physics/audio SDKs, etc.)
├─ shared/      icons, the boot banner, audio — everything panels load by name
│  ├─ Bitmap/  Bitmap1076.bmp  (your "id Studio" banner, shown on boot)
│  ├─ PNG/  JPG/  Icons/  Audio/
└─ tungsten/    the ENGINE ITSELF — acts as a client hosted inside the editor
```

`studio` never talks to files directly by a hardcoded path scattered through
the code — every icon, sound, and the splash banner is loaded once, by name,
from `shared/`, so the "buttons use the exact same named icons as the button
names" rule is centralized in `studio/Editor/UI/IconRegistry.cs`.

## What's actually implemented right now

- **First Person Character Controller** — WASD movement, mouse-look, jump,
  crouch, sprint, gravity, AABB collision (`tungsten/Player/FirstPersonController.cs`)
- **Gun System** — hitscan raycast, ammo/reload, recoil feeding into a
  procedural view-bob/recoil animator, with Pistol/Rifle/Shotgun presets
  (`tungsten/Player/GunSystem.cs`, `Weapon.cs`, `PlayerAnimator.cs`)
- **Driving System** — arcade-style 4-wheel vehicle controller: throttle,
  steer, brake/handbrake (`tungsten/Vehicles/VehicleController.cs`)
- **Universal Asset/Prefab Store** — one `Prefab` format for everything
  (props, guns, vehicles, lights); click or drag a tile from the Asset Store
  panel into the 3D viewport to spawn it (`tungsten/Assets/AssetDatabase.cs`,
  `studio/Editor/Panels/AssetStorePanel.cs`)
- **3D / 2D / Sound tabs** across the center of the editor, **Hierarchy /
  Properties / Shader** tabs on the right, an **Output** console at the
  bottom, and a **Toolbar** with New/Open/Save/Select/Move/Rotate/Scale/
  Undo/Redo/Play/Stop — the same broad layout as Roblox Studio / Unity
- **Live Shader Slider Bars** — the active shader's PBR parameters
  (Roughness, Metallic, Ambient, Exposure, Fog Density) are exposed as
  sliders in the Shader tab and applied every frame with zero recompilation
  (`tungsten/Rendering/Shader.cs` `ExposeFloat`, `studio/Editor/Panels/ShaderPanel.cs`)
- **Boot splash** — `shared/Bitmap/Bitmap1076.bmp` fades in/holds/fades out while
  the engine finishes loading (`studio/Editor/Splash/SplashScreen.cs`)
- **Block-scripting data model** — `ScriptBlock`/`BlockScript`/`BlockRuntime`
  are ready as a foundation for the Scratch-like visual scripting you
  mentioned wanting to design together later; only a minimal "Log" action is
  wired up today

Every file above is commented to explain *why*, not just *what*, so you can
extend it without archaeology.

## What is intentionally a stand-in, not a finished product

Being upfront about this, per your request for everything to be functional:

- **Physics** (`SimplePhysicsWorld.cs`) is a hand-rolled AABB system, not a
  full rigid-body engine — no rotational dynamics, no constraint solver. It's
  enough for a character to walk/jump on blocky geometry and for guns to
  raycast, but a ragdoll or realistic car physics would need a real physics
  library (Bullet/PhysX/BepuPhysics) wired in via `libs/`.
- **Rendering** is a single forward PBR-lite pass (Cook-Torrance + one sun
  light + fog + tonemap) — no shadow maps, no bloom/SSAO/GI yet. The shader
  already exposes the uniforms those techniques would tune, so adding them is
  additive, not a rewrite.
- **Audio** — the Sound tab lists files from `shared/Audio` and has a Play
  button, but actual playback is a logged stub (`SoundPanel.cs`) since wiring
  a real audio backend (OpenAL/NAudio) needs a native/NuGet dependency this
  sandbox couldn't fetch and verify.
- **Block-based visual scripting** is a data model + a 3-line interpreter,
  not a drag-and-drop node editor yet — you said you'd help design the actual
  block palette later, so this is the hook point for that, not a finished UI.
- **"Universal" assets** currently means one shared `Prefab` format and one
  drag/drop code path — there's no asset-import pipeline (e.g. loading
  external `.fbx`/`.gltf` models) yet.

None of this is hidden or faked — every stand-in above logs clearly to the
Output console what it's doing (e.g. "(stub) Playing gunshot.wav") so it's
obvious at a glance what's real gameplay logic vs. a hook for later.

## Build & run

This sandbox has no internet access to NuGet and no .NET SDK, so this was
written but not compiled here. On your own machine:

```bash
# from the TungstenStudio/ folder
dotnet restore
dotnet run --project studio
```

Requires the .NET 6 SDK. First run will download the OpenTK, StbImageSharp,
and System.Drawing.Common NuGet packages declared in `studio/Studio.csproj`
and `tungsten/Tungsten.csproj`.

Controls:
- **Edit mode**: hold Right Mouse to fly the camera (WASD + Q/E, Shift to go
  faster); Left Click with the Select tool picks an entity; drag a tile from
  the Asset Store onto the viewport to place it.
- **Play mode** (Play button, top-right of the toolbar): WASD to move, mouse
  to look, Space to jump, Ctrl to crouch, Shift to sprint, Left Mouse to
  fire the equipped gun, R to reload.

## Suggested next steps

1. Swap `SimplePhysicsWorld` for BepuPhysics (pure-managed, no native binary
   needed — the friendliest upgrade path given this project's constraints).
2. Add a real scene serialization format (`.json`) behind the toolbar's
   New/Open/Save buttons, which currently just log stubs.
3. Design the block palette together and extend `BlockRuntime`'s action
   dictionary + build a node-graph editor panel for it.
4. Add shadow mapping and an HDR + bloom post pass to push the renderer
   toward the "best graphics possible" goal.
