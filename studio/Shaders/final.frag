// =============================================================================
// FINAL COMPOSITE SHADER  — final.frag
// Last pass before presenting to the display.  Applies in order:
//   1. FXAA          — removes jaggies without MSAA cost
//   2. Bloom blend   — adds glow to bright areas
//   3. Exposure      — scales HDR luminance
//   4. ACES Filmic / Reinhard tone mapping — compresses HDR to [0,1]
//   5. Contrast      — S-curve contrast adjustment
//   6. Saturation    — colour saturation boost/cut
//   7. Gamma encode  — linear → sRGB (γ = 2.2)
// =============================================================================
#version 450 core

out vec4 FragColor;
in  vec2 vUV;

uniform sampler2D uHDRBuffer;   // Lighting pass output
uniform sampler2D uBloomBuffer; // Bloom blur pass output

// ── Shader Settings uniforms (bound from ShaderSettings C# object) ─────────
uniform float uExposure;
uniform float uContrast;
uniform float uSaturation;
uniform bool  uACES;
uniform bool  uFXAA;
uniform float uBloomStrength;

// =============================================================================
// FXAA  (Nvidia FXAA 3.11 simplified)
// Detects edges via luma gradient and blends neighbours to smooth them.
// =============================================================================
vec3 FXAA(sampler2D tex, vec2 uv)
{
    vec2 texelSize = 1.0 / vec2(textureSize(tex, 0));

    // Sample luma (perceived brightness) at 5 positions
    float lumC  = dot(texture(tex, uv).rgb,                       vec3(0.299,0.587,0.114));
    float lumN  = dot(texture(tex, uv + vec2( 0,-1)*texelSize).rgb, vec3(0.299,0.587,0.114));
    float lumS  = dot(texture(tex, uv + vec2( 0, 1)*texelSize).rgb, vec3(0.299,0.587,0.114));
    float lumE  = dot(texture(tex, uv + vec2( 1, 0)*texelSize).rgb, vec3(0.299,0.587,0.114));
    float lumW  = dot(texture(tex, uv + vec2(-1, 0)*texelSize).rgb, vec3(0.299,0.587,0.114));

    float lumMin  = min(lumC, min(min(lumN,lumS),min(lumE,lumW)));
    float lumMax  = max(lumC, max(max(lumN,lumS),max(lumE,lumW)));
    float lumRange= lumMax - lumMin;

    // Skip pixels that are not on an edge
    if (lumRange < max(0.0625, lumMax * 0.125))
        return texture(tex, uv).rgb;

    // Gradient direction
    vec2 dir = vec2(-(lumN - lumS), lumE - lumW);
    float dirReduce = max((lumN+lumS+lumE+lumW) * 0.25 * 0.125, 1.0/128.0);
    float rcpDirMin = 1.0 / (min(abs(dir.x), abs(dir.y)) + dirReduce);
    dir = clamp(dir * rcpDirMin, vec2(-8), vec2(8)) * texelSize;

    vec3 rgbA = 0.5 * (
        texture(tex, uv - dir * (1.0/6.0)).rgb +
        texture(tex, uv + dir * (1.0/6.0)).rgb);
    vec3 rgbB = rgbA * 0.5 + 0.25 * (
        texture(tex, uv - dir * 0.5).rgb +
        texture(tex, uv + dir * 0.5).rgb);

    float lumB = dot(rgbB, vec3(0.299,0.587,0.114));
    return (lumB < lumMin || lumB > lumMax) ? rgbA : rgbB;
}

// =============================================================================
// ACES Filmic Tone Mapping  (Academy Color Encoding System)
// Produces natural film-like response: richer shadows, controlled highlights.
// =============================================================================
vec3 ACESFilmic(vec3 x)
{
    const float a = 2.51, b = 0.03, c = 2.43, d = 0.59, e = 0.14;
    return clamp((x*(a*x+b))/(x*(c*x+d)+e), 0.0, 1.0);
}

// Reinhard tone mapping (simpler, softer rolloff)
vec3 Reinhard(vec3 x) { return x / (1.0 + x); }

// =============================================================================
// Saturation  (shift colour toward/away from grey)
// =============================================================================
vec3 AdjustSaturation(vec3 color, float saturation)
{
    float grey = dot(color, vec3(0.2126, 0.7152, 0.0722)); // Rec.709 luma
    return mix(vec3(grey), color, saturation);
}

// =============================================================================
// Contrast  (S-curve via gamma + scale)
// =============================================================================
vec3 AdjustContrast(vec3 color, float contrast)
{
    return pow(color, vec3(contrast)); // Simple power contrast
}

// =============================================================================
// MAIN
// =============================================================================
void main()
{
    // ── 1. FXAA ───────────────────────────────────────────────────────────
    vec3 hdr = uFXAA
        ? FXAA(uHDRBuffer, vUV)
        : texture(uHDRBuffer, vUV).rgb;

    // ── 2. Add bloom ──────────────────────────────────────────────────────
    vec3 bloom = texture(uBloomBuffer, vUV).rgb;
    hdr = hdr + bloom * uBloomStrength;

    // ── 3. Exposure ───────────────────────────────────────────────────────
    hdr = hdr * uExposure;

    // ── 4. Tone mapping ───────────────────────────────────────────────────
    vec3 ldr = uACES ? ACESFilmic(hdr) : Reinhard(hdr);

    // ── 5. Contrast ───────────────────────────────────────────────────────
    ldr = AdjustContrast(ldr, uContrast);

    // ── 6. Saturation ─────────────────────────────────────────────────────
    ldr = AdjustSaturation(ldr, uSaturation);

    // ── 7. Gamma encode: linear → sRGB ────────────────────────────────────
    ldr = pow(ldr, vec3(1.0 / 2.2));

    FragColor = vec4(ldr, 1.0);
}
