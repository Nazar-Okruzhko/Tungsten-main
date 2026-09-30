using System;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Tungsten.Rendering
{
    /// <summary>
    /// GPU-resident geometry: a VAO wrapping a position/normal/uv interleaved
    /// VBO and an index buffer. Includes a small factory for the built-in
    /// primitive shapes (Cube, Plane, Sphere) used by the placeholder
    /// Asset/Prefab Store entries until real imported models are added.
    /// </summary>
    public class Mesh : IDisposable
    {
        private readonly int _vao, _vbo, _ebo;
        private readonly int _indexCount;

        // Layout per vertex: position(3) normal(3) uv(2) = 8 floats
        public Mesh(float[] vertices, uint[] indices)
        {
            _indexCount = indices.Length;

            _vao = GL.GenVertexArray();
            GL.BindVertexArray(_vao);

            _vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

            _ebo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);

            const int stride = 8 * sizeof(float);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, 0);
            GL.EnableVertexAttribArray(0); // position

            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride, 3 * sizeof(float));
            GL.EnableVertexAttribArray(1); // normal

            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, stride, 6 * sizeof(float));
            GL.EnableVertexAttribArray(2); // uv

            GL.BindVertexArray(0);
        }

        public void Draw()
        {
            GL.BindVertexArray(_vao);
            GL.DrawElements(PrimitiveType.Triangles, _indexCount, DrawElementsType.UnsignedInt, 0);
        }

        public void Dispose()
        {
            GL.DeleteBuffer(_vbo);
            GL.DeleteBuffer(_ebo);
            GL.DeleteVertexArray(_vao);
        }

        // ---------------------------------------------------------------
        // Primitive factory - used by the Asset Store's built-in prefabs.
        // ---------------------------------------------------------------

        public static Mesh CreateCube()
        {
            // 24 verts (4 per face) so each face gets correct flat normals.
            float[] v =
            {
                // +X
                 0.5f,-0.5f,-0.5f, 1,0,0, 0,0,   0.5f, 0.5f,-0.5f, 1,0,0, 1,0,   0.5f, 0.5f, 0.5f, 1,0,0, 1,1,   0.5f,-0.5f, 0.5f, 1,0,0, 0,1,
                // -X
                -0.5f,-0.5f, 0.5f,-1,0,0, 0,0,  -0.5f, 0.5f, 0.5f,-1,0,0, 1,0,  -0.5f, 0.5f,-0.5f,-1,0,0, 1,1,  -0.5f,-0.5f,-0.5f,-1,0,0, 0,1,
                // +Y
                -0.5f, 0.5f,-0.5f, 0,1,0, 0,0,  -0.5f, 0.5f, 0.5f, 0,1,0, 1,0,   0.5f, 0.5f, 0.5f, 0,1,0, 1,1,   0.5f, 0.5f,-0.5f, 0,1,0, 0,1,
                // -Y
                -0.5f,-0.5f, 0.5f, 0,-1,0,0,0,  -0.5f,-0.5f,-0.5f,0,-1,0,1,0,    0.5f,-0.5f,-0.5f,0,-1,0,1,1,    0.5f,-0.5f, 0.5f,0,-1,0,0,1,
                // +Z
                -0.5f,-0.5f, 0.5f, 0,0,1, 0,0,   0.5f,-0.5f, 0.5f, 0,0,1, 1,0,   0.5f, 0.5f, 0.5f, 0,0,1, 1,1,  -0.5f, 0.5f, 0.5f, 0,0,1, 0,1,
                // -Z
                 0.5f,-0.5f,-0.5f, 0,0,-1,0,0,  -0.5f,-0.5f,-0.5f,0,0,-1,1,0,   -0.5f, 0.5f,-0.5f,0,0,-1,1,1,    0.5f, 0.5f,-0.5f,0,0,-1,0,1,
            };
            uint[] idx = new uint[36];
            for (uint f = 0; f < 6; f++)
            {
                uint b = f * 4;
                uint[] face = { b, b + 1, b + 2, b, b + 2, b + 3 };
                Array.Copy(face, 0, idx, f * 6, 6);
            }
            return new Mesh(v, idx);
        }

        public static Mesh CreatePlane(float size = 10f)
        {
            float h = size * 0.5f;
            float[] v =
            {
                -h, 0,-h, 0,1,0, 0,0,
                 h, 0,-h, 0,1,0, size,0,
                 h, 0, h, 0,1,0, size,size,
                -h, 0, h, 0,1,0, 0,size,
            };
            uint[] idx = { 0, 1, 2, 0, 2, 3 };
            return new Mesh(v, idx);
        }

        public static Mesh CreateSphere(int stacks = 16, int slices = 24)
        {
            var verts = new System.Collections.Generic.List<float>();
            var idx = new System.Collections.Generic.List<uint>();
            for (int i = 0; i <= stacks; i++)
            {
                float phi = MathF.PI * i / stacks;
                for (int j = 0; j <= slices; j++)
                {
                    float theta = 2 * MathF.PI * j / slices;
                    float x = MathF.Sin(phi) * MathF.Cos(theta);
                    float y = MathF.Cos(phi);
                    float z = MathF.Sin(phi) * MathF.Sin(theta);
                    verts.AddRange(new[] { x * 0.5f, y * 0.5f, z * 0.5f, x, y, z, (float)j / slices, (float)i / stacks });
                }
            }
            for (int i = 0; i < stacks; i++)
            {
                for (int j = 0; j < slices; j++)
                {
                    uint a = (uint)(i * (slices + 1) + j);
                    uint b = (uint)(a + slices + 1);
                    idx.AddRange(new[] { a, b, a + 1, b, b + 1, a + 1 });
                }
            }
            return new Mesh(verts.ToArray(), idx.ToArray());
        }
    }
}
