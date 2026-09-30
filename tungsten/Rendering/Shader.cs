using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using Tungsten.Core;

namespace Tungsten.Rendering
{
    /// <summary>
    /// Compiles a vertex+fragment GLSL pair into a GL program and caches the
    /// location of every uniform it declares.
    ///
    /// Exposes <see cref="FloatUniforms"/> - the live list of float uniforms
    /// (exposure, roughness, fog density, bloom threshold, ...) that the
    /// Studio's "Shader" panel reads to auto-generate slider bars for. This is
    /// how "drag a slider -> see the shader change instantly" works: the panel
    /// doesn't know anything about lighting math, it just enumerates whatever
    /// floats the active shader chooses to publish here.
    /// </summary>
    public class Shader : IDisposable
    {
        public int Handle { get; }
        private readonly Dictionary<string, int> _uniformLocations = new();

        /// <summary>name -> (value, min, max) currently exposed to the editor UI.</summary>
        public readonly Dictionary<string, (float value, float min, float max)> FloatUniforms = new();

        public Shader(string vertexSource, string fragmentSource)
        {
            int vertex = Compile(ShaderType.VertexShader, vertexSource);
            int fragment = Compile(ShaderType.FragmentShader, fragmentSource);

            Handle = GL.CreateProgram();
            GL.AttachShader(Handle, vertex);
            GL.AttachShader(Handle, fragment);
            GL.LinkProgram(Handle);
            GL.GetProgram(Handle, GetProgramParameterName.LinkStatus, out int success);
            if (success == 0)
            {
                string log = GL.GetProgramInfoLog(Handle);
                Logger.Error($"Shader link failed: {log}");
            }

            GL.DetachShader(Handle, vertex);
            GL.DetachShader(Handle, fragment);
            GL.DeleteShader(vertex);
            GL.DeleteShader(fragment);

            CacheUniformLocations();
        }

        private static int Compile(ShaderType type, string src)
        {
            int handle = GL.CreateShader(type);
            GL.ShaderSource(handle, src);
            GL.CompileShader(handle);
            GL.GetShader(handle, ShaderParameter.CompileStatus, out int success);
            if (success == 0)
            {
                string log = GL.GetShaderInfoLog(handle);
                Logger.Error($"{type} compile failed: {log}");
            }
            return handle;
        }

        private void CacheUniformLocations()
        {
            GL.GetProgram(Handle, GetProgramParameterName.ActiveUniforms, out int count);
            for (int i = 0; i < count; i++)
            {
                string name = GL.GetActiveUniform(Handle, i, out _, out _);
                int location = GL.GetUniformLocation(Handle, name);
                _uniformLocations[name] = location;
            }
        }

        public void Use() => GL.UseProgram(Handle);

        /// <summary>Registers a tweakable float uniform so it shows up as a slider in the Studio UI.</summary>
        public void ExposeFloat(string name, float defaultValue, float min, float max)
        {
            FloatUniforms[name] = (defaultValue, min, max);
        }

        public void SetFloat(string name, float value)
        {
            if (_uniformLocations.TryGetValue(name, out int loc)) GL.Uniform1(loc, value);
            if (FloatUniforms.ContainsKey(name))
            {
                var (_, min, max) = FloatUniforms[name];
                FloatUniforms[name] = (value, min, max);
            }
        }

        public void SetInt(string name, int value)
        {
            if (_uniformLocations.TryGetValue(name, out int loc)) GL.Uniform1(loc, value);
        }

        public void SetVector3(string name, Vector3 value)
        {
            if (_uniformLocations.TryGetValue(name, out int loc)) GL.Uniform3(loc, value);
        }

        public void SetMatrix4(string name, Matrix4 value)
        {
            if (_uniformLocations.TryGetValue(name, out int loc)) GL.UniformMatrix4(loc, false, ref value);
        }

        /// <summary>Push every exposed float uniform's current value to the GPU. Call once per frame after Use().</summary>
        public void ApplyExposedFloats()
        {
            foreach (var kv in FloatUniforms)
            {
                if (_uniformLocations.TryGetValue(kv.Key, out int loc))
                    GL.Uniform1(loc, kv.Value.value);
            }
        }

        public void Dispose() => GL.DeleteProgram(Handle);
    }
}
