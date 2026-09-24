// =============================================================================
// GEOMETRY PASS FRAGMENT SHADER  — geometry.frag
// Fills the G-Buffer:
//   gPosition   (RGB16F) — world-space fragment position
//   gNormal     (RGB16F) — world-space normal (octahedral-encoded in final)
//   gAlbedoSpec (RGBA8)  — RGB albedo + A specular intensity
// =============================================================================
#version 450 core

// ── G-Buffer outputs ──────────────────────────────────────────────────────
layout(location = 0) out vec3 gPosition;
layout(location = 1) out vec3 gNormal;
layout(location = 2) out vec4 gAlbedoSpec;

// ── Inputs from vertex shader ─────────────────────────────────────────────
in vec3 vWorldPos;
in vec3 vNormal;
in vec2 vTexCoord;

// ── Material uniforms (set per draw call by MeshRenderer) ─────────────────
uniform vec3      uAlbedo;        // Base colour
uniform float     uRoughness;     // PBR roughness [0,1]
uniform float     uMetallic;      // PBR metallic  [0,1]
uniform sampler2D uAlbedoTex;     // Albedo texture (optional)
uniform bool      uHasAlbedoTex;  // Whether the above is bound

void main()
{
    // Store world-space position into gPosition
    gPosition = vWorldPos;

    // Store normalised normal into gNormal
    gNormal = normalize(vNormal);

    // Albedo: blend texture and uniform colour
    vec3 albedo = uHasAlbedoTex
        ? texture(uAlbedoTex, vTexCoord).rgb * uAlbedo
        : uAlbedo;

    // Pack roughness into alpha channel of gAlbedoSpec
    gAlbedoSpec = vec4(albedo, uRoughness);
}
