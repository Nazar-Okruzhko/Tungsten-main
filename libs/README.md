# libs/

This folder is where **external native libraries** live — things that are not
NuGet packages and not engine source, e.g.:

- `cudart64_*.dll` / `libcudart.so` (CUDA Runtime, if you add GPU compute)
- `PhysX`, `Bullet`, or `BepuPhysics` native binaries (if you upgrade past
  `SimplePhysicsWorld`)
- `openal32.dll` / `libopenal.so` (if you wire up real audio playback for the
  Sound tab instead of the current stub)
- Any vendor SDK `.dll` / `.so` / `.dylib` your project needs at runtime

## Why this folder is currently empty

This build environment has no internet access to vendor download sites (only
a small allow-list of package registries for building code, not fetching
binary SDKs), so no actual CUDA/PhysX/OpenAL binaries could be downloaded and
placed here for you. Nothing in the current engine code requires anything
from this folder yet — `SimplePhysicsWorld` and the renderer are pure
C#/OpenGL with zero native dependencies, so the project builds and runs
without anything being placed here.

## How to add something later

1. Download the vendor's redistributable runtime for your OS/architecture.
2. Drop the native binary file(s) directly in this folder.
3. In `studio/Studio.csproj`, the existing `<None Include="..\libs\**\*.*">`
   item group already copies everything here next to the built executable at
   compile time — you don't need to edit the `.csproj` for a new file, only
   for a new *reference* (e.g. a C# P/Invoke wrapper) if you're calling into
   the native library directly instead of through a NuGet wrapper.
