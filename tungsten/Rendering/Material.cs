using OpenTK.Mathematics;

namespace Tungsten.Rendering
{
    /// <summary>
    /// Every renderable Entity owns one of these. It's intentionally tiny:
    /// a shader reference plus the handful of per-object values the MainLit
    /// shader needs (base color). Global look-and-feel (roughness, exposure,
    /// fog, ...) lives on the Shader itself via ExposeFloat/FloatUniforms so
    /// the whole scene can be tuned from one place - the Shader panel.
    /// </summary>
    public class Material
    {
        public Shader Shader;
        public Vector3 Albedo = new(0.8f, 0.8f, 0.8f);

        public Material(Shader shader) => Shader = shader;
    }
}
