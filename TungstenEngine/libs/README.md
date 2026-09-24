# libs/ — External Native Libraries

Drop native binary dependencies here.  They are **not** NuGet packages —
these are compiled `.so` (Linux), `.dll` (Windows), or `.dylib` (macOS) files
that the engine P/Invokes at runtime.

## Recommended libs for production

| Library | Purpose | Where to get |
|---------|---------|-------------|
| `cudart.so` / `cudart.dll` | NVIDIA CUDA runtime (GPU compute for simulation, AI) | CUDA Toolkit |
| `PhysX_64.dll` | NVIDIA PhysX rigid body / cloth / vehicle physics | PhysX SDK |
| `OpenAL32.dll` / `libopenal.so` | 3D spatial audio | openal-soft |
| `freetype.so` | Font rendering (glyph rasterisation for the UI) | FreeType |
| `assimp.dll` | Asset Import Library — loads .fbx, .gltf, .obj, … | AssImp |

## Usage pattern

```csharp
// In your C# code, declare a P/Invoke binding:
[DllImport("libs/assimp", CallingConvention = CallingConvention.Cdecl)]
private static extern IntPtr aiImportFile(string file, uint flags);
```

Set the lib search path before first use:

```csharp
NativeLibrary.SetDllImportResolver(typeof(Program).Assembly, (name, _, _) =>
{
    var path = Path.Combine(AppContext.BaseDirectory, "libs", name);
    return NativeLibrary.Load(path);
});
```
