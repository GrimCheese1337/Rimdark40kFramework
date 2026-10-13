#version 450

layout(location = 0) in vec4 vColor;
layout(location = 1) in vec2 vUv;

layout(location = 0) out vec4 outColor;

layout(set = 2, binding = 0) uniform MaterialUniforms
{
    vec4 _DrawColor;      // @color @default = vec4(1.0, 1.0, 1.0, 1.0)
    vec4 _DrawColorTwo;   // @color @default = vec4(1.0, 1.0, 1.0, 1.0)
    vec4 _DrawColorThree; // @color @default = vec4(1.0, 1.0, 1.0, 1.0)
    vec4 _MainTex_ST;     // @default = vec4(0.0, 0.0, 1.0, 1.0)
} uMaterial;

layout(set = 2, binding = 1) uniform sampler2D _MainTex;
layout(set = 2, binding = 2) uniform sampler2D _MaskTex; // @default = black

void main()
{
    vec2 uv = vUv * uMaterial._MainTex_ST.zw + uMaterial._MainTex_ST.xy;
    vec4 mask = texture(_MaskTex, uv);
    mask.rgb *= clamp(mask.a * 255.0, 0.0, 1.0);

    vec4 tint = mask.r * uMaterial._DrawColor
              + mask.g * uMaterial._DrawColorTwo
              + mask.b * uMaterial._DrawColorThree
              + (1.0 - mask.r - mask.g - mask.b);

    vec4 f = texture(_MainTex, uv);
    if (f.a * tint.a < 0.5)
        discard;
    outColor = f * tint;
}
