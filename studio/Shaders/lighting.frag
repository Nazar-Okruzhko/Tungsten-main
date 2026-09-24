// =============================================================================
// DEFERRED PBR LIGHTING SHADER  — lighting.frag
// Reads the G-Buffer and computes physically based lighting (Cook-Torrance BRDF)
// for the directional sun light plus a hemisphere ambient term.
//
// Cook-Torrance BRDF components:
//   D  = GGX Normal Distribution Function (microfacet distribution)
//   G  = Smith masking-shadowing term
//   F  = Schlick Fresnel approximation (specular colour at grazing angles)
// =============================================================================
#version 450 core

out vec4 FragColor;

// ── Full-screen quad UVs ──────────────────────────────────────────────────
in vec2 vUV;

// ── G-Buffer samplers ─────────────────────────────────────────────────────
uniform sampler2D gPosition;
uniform sampler2D gNormal;
uniform sampler2D gAlbedoSpec;
uniform sampler2D gSSAO;          // SSAO occlusion factor [0,1]

// ── Lighting uniforms (mapped to ShaderSettings) ─────────────────────────
uniform vec3  uSunDirection;   // Normalised, world space
uniform vec3  uSunColor;       // Multiplied by intensity on CPU side
uniform float uAmbient;
uniform vec3  uCamPos;         // World-space camera position

// ── Shadow ────────────────────────────────────────────────────────────────
uniform bool      uShadowsEnabled;
uniform sampler2D uShadowMap;
uniform mat4      uLightSpaceMatrix;
uniform float     uShadowBias;

// ── Fog ───────────────────────────────────────────────────────────────────
uniform bool  uFogEnabled;
uniform float uFogStart;
uniform float uFogEnd;
uniform vec3  uFogColor;

// =============================================================================
// Cook-Torrance BRDF helpers
// =============================================================================

const float PI = 3.14159265359;

// GGX Normal Distribution Function — models the statistical distribution of
// microfacet orientations for a surface with roughness α.
float DistributionGGX(vec3 N, vec3 H, float roughness)
{
    float a  = roughness * roughness;
    float a2 = a * a;
    float NdotH  = max(dot(N, H), 0.0);
    float NdotH2 = NdotH * NdotH;
    float denom  = NdotH2 * (a2 - 1.0) + 1.0;
    return a2 / (PI * denom * denom);
}

// Smith geometry sub-function (Schlick-GGX)
float GeometrySchlickGGX(float NdotV, float roughness)
{
    float r = roughness + 1.0;
    float k = (r * r) / 8.0;       // Direct lighting k
    return NdotV / (NdotV * (1.0 - k) + k);
}

// Smith combined geometry term (accounts for both masking and shadowing)
float GeometrySmith(vec3 N, vec3 V, vec3 L, float roughness)
{
    float NdotV = max(dot(N, V), 0.0);
    float NdotL = max(dot(N, L), 0.0);
    return GeometrySchlickGGX(NdotV, roughness) *
           GeometrySchlickGGX(NdotL, roughness);
}

// Schlick Fresnel approximation — how reflective the surface becomes at
// grazing angles (always more reflective edge-on, even for rough dielectrics).
vec3 FresnelSchlick(float cosTheta, vec3 F0)
{
    return F0 + (1.0 - F0) * pow(clamp(1.0 - cosTheta, 0.0, 1.0), 5.0);
}

// =============================================================================
// Shadow PCF  (Percentage Closer Filtering — soft shadow edges)
// =============================================================================
float SampleShadow(vec4 fragPosLightSpace, float bias)
{
    if (!uShadowsEnabled) return 1.0;

    vec3 projCoords = fragPosLightSpace.xyz / fragPosLightSpace.w;
    projCoords = projCoords * 0.5 + 0.5;
    if (projCoords.z > 1.0) return 1.0;

    float shadow  = 0.0;
    vec2  texelSz = 1.0 / textureSize(uShadowMap, 0);

    // 3×3 PCF kernel
    for (int x = -1; x <= 1; x++)
    for (int y = -1; y <= 1; y++)
    {
        float pcfDepth = texture(uShadowMap, projCoords.xy + vec2(x, y) * texelSz).r;
        shadow += (projCoords.z - bias) > pcfDepth ? 0.0 : 1.0;
    }
    return shadow / 9.0;
}

// =============================================================================
// MAIN
// =============================================================================
void main()
{
    // ── Read G-Buffer ─────────────────────────────────────────────────────
    vec3  worldPos   = texture(gPosition,   vUV).rgb;
    vec3  normal     = normalize(texture(gNormal, vUV).rgb);
    vec4  albedoRgh  = texture(gAlbedoSpec, vUV);
    vec3  albedo     = albedoRgh.rgb;
    float roughness  = albedoRgh.a;
    float metallic   = 0.0; // TODO: pack into gAlbedoSpec.a alongside roughness
    float ao         = texture(gSSAO, vUV).r;

    vec3  V = normalize(uCamPos - worldPos);  // View direction
    vec3  L = normalize(-uSunDirection);       // Light direction (toward sun)
    vec3  H = normalize(V + L);               // Halfway vector

    // ── F0: base reflectivity ─────────────────────────────────────────────
    // Dielectrics ~ 0.04; conductors (metals) use albedo colour.
    vec3 F0 = mix(vec3(0.04), albedo, metallic);

    // ── Cook-Torrance specular BRDF ───────────────────────────────────────
    float NDF = DistributionGGX(normal, H, roughness);
    float G   = GeometrySmith(normal, V, L, roughness);
    vec3  F   = FresnelSchlick(max(dot(H, V), 0.0), F0);

    vec3  numerator   = NDF * G * F;
    float denominator = 4.0 * max(dot(normal, V), 0.0) * max(dot(normal, L), 0.0) + 0.0001;
    vec3  specular    = numerator / denominator;

    // ── Diffuse (energy conserving: metals have no diffuse) ───────────────
    vec3 kD = (vec3(1.0) - F) * (1.0 - metallic);
    vec3 diffuse = kD * albedo / PI;

    // ── Direct lighting ───────────────────────────────────────────────────
    float NdotL  = max(dot(normal, L), 0.0);
    float shadow = SampleShadow(uLightSpaceMatrix * vec4(worldPos, 1.0), uShadowBias);

    vec3 Lo = (diffuse + specular) * uSunColor * NdotL * shadow;

    // ── Ambient ───────────────────────────────────────────────────────────
    vec3 ambient = albedo * uAmbient * ao;

    vec3 color = ambient + Lo;

    // ── Fog ───────────────────────────────────────────────────────────────
    if (uFogEnabled)
    {
        float dist    = length(uCamPos - worldPos);
        float fogFact = clamp((dist - uFogStart) / (uFogEnd - uFogStart), 0.0, 1.0);
        color = mix(color, uFogColor, fogFact);
    }

    FragColor = vec4(color, 1.0);
}
