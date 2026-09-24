// =============================================================================
// GEOMETRY PASS VERTEX SHADER  — geometry.vert
// Transforms each vertex into clip space and passes attributes to the
// fragment shader to fill the G-Buffer (position / normal / albedo).
// =============================================================================
#version 450 core

// ── Per-vertex attributes ──────────────────────────────────────────────────
layout(location = 0) in vec3 aPosition;   // Object-space position
layout(location = 1) in vec3 aNormal;     // Object-space normal
layout(location = 2) in vec2 aTexCoord;  // UV0

// ── Uniforms ───────────────────────────────────────────────────────────────
uniform mat4 uModel;         // Object → World
uniform mat4 uView;          // World  → Camera
uniform mat4 uProjection;    // Camera → Clip

// ── Outputs to fragment shader ────────────────────────────────────────────
out vec3 vWorldPos;     // World-space position (for lighting calculations)
out vec3 vNormal;       // World-space normal (after inverse-transpose)
out vec2 vTexCoord;     // UV coordinates

void main()
{
    vec4 worldPos = uModel * vec4(aPosition, 1.0);
    vWorldPos  = worldPos.xyz;

    // Normal must be transformed by the inverse-transpose of the model matrix
    // to handle non-uniform scale correctly.
    mat3 normalMatrix = transpose(inverse(mat3(uModel)));
    vNormal   = normalize(normalMatrix * aNormal);

    vTexCoord = aTexCoord;

    gl_Position = uProjection * uView * worldPos;
}
