Shader "BlueROV/Ocean"
{
    Properties
    {
        _Absorption ("Absorption RVB (1/m)", Vector) = (0.32, 0.09, 0.06, 0)
        _Diffusion ("Couleur profonde", Color) = (0.012, 0.11, 0.16, 1)
        _Ecume ("Ecume", Range(0,1)) = 0.5
        _Ondulation ("Petites rides", Range(0,1)) = 0.35
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Cull Off
            ZWrite On
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Absorption;
                float4 _Diffusion;
                float _Ecume;
                float _Ondulation;
            CBUFFER_END
            float4 _MerVagues[4];
            float4 _MerReglages; // temps, cambrure, niveau

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float2 repos : TEXCOORD1;
                float fogCoord : TEXCOORD2;
                float crete : TEXCOORD3;
            };

            float Portee(float2 p) { return saturate(1.0 - (length(p - _WorldSpaceCameraPos.xz) - 250.0) / 500.0); }

            Varyings vert(Attributes v)
            {
                Varyings o;
                float3 w = TransformObjectToWorld(v.positionOS.xyz);
                o.repos = w.xz;
                float f = Portee(w.xz);
                float3 d = 0;
                for (int i = 0; i < 4; i++)
                {
                    float4 g = _MerVagues[i];
                    float k = TWO_PI / g.w;
                    float ph = k * dot(g.xy, w.xz) - sqrt(9.81 * k) * _MerReglages.x;
                    float c = cos(ph) * g.z * _MerReglages.y;
                    d += float3(g.x * c, sin(ph) * g.z, g.y * c);
                }
                w += d * f;
                o.crete = d.y * f;
                o.positionWS = w;
                o.positionCS = TransformWorldToHClip(w);
                o.fogCoord = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            float Hash(float2 p) { p = frac(p * float2(123.34, 456.21)); p += dot(p, p + 45.32); return frac(p.x * p.y); }
            float Bruit(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                f = f * f * (3 - 2 * f);
                return lerp(lerp(Hash(i), Hash(i + float2(1,0)), f.x),
                            lerp(Hash(i + float2(0,1)), Hash(i + float2(1,1)), f.x), f.y);
            }
            float Rides(float2 p)
            {
                float t = _MerReglages.x;
                float h = Bruit(p * 0.35 + t * float2(0.35, 0.2)) * 1.6;
                h += Bruit(p.yx * 0.8 + t * float2(-0.5, 0.4)) * 0.8;
                h += Bruit(p * 1.9 + t * float2(0.8, -0.7)) * 0.33;
                h += Bruit(p.yx * 4.3 - t * float2(1.1, 0.9)) * 0.14;
                return h;
            }

            float3 Normale(float2 p, float dist)
            {
                float f = Portee(p);
                float2 pente = 0;
                for (int i = 0; i < 4; i++)
                {
                    float4 g = _MerVagues[i];
                    float k = TWO_PI / g.w;
                    float ph = k * dot(g.xy, p) - sqrt(9.81 * k) * _MerReglages.x;
                    pente += g.xy * (k * g.z * cos(ph)) * f;
                }
                float e = 0.15;
                float h0 = Rides(p);
                float2 r = float2(Rides(p + float2(e, 0)) - h0, Rides(p + float2(0, e)) - h0) / e;
                pente += r * _Ondulation * 0.35 / (1.0 + dist * 0.02);
                return normalize(float3(-pente.x, 1, -pente.y));
            }

            float3 Ciel(float3 d, float3 L, float3 lum)
            {
                float y = saturate(d.y);
                float3 c = lerp(float3(0.62, 0.73, 0.83), float3(0.13, 0.32, 0.62), pow(y, 0.55));
                return c + lum * pow(saturate(dot(d, L)), 900) * 8.0;
            }

            half4 frag(Varyings i, bool avant : SV_IsFrontFace) : SV_Target
            {
                Light soleil = GetMainLight();
                float3 versCam = _WorldSpaceCameraPos - i.positionWS;
                float dist = length(versCam);
                float3 V = versCam / dist;
                float3 N = Normale(i.repos, dist);
                float3 col;

                if (avant)
                {
                    float2 uv = GetNormalizedScreenSpaceUV(i.positionCS);
                    float zSurf = LinearEyeDepth(i.positionCS.z, _ZBufferParams);
                    float2 uvR = uv + N.xz * 0.04 * saturate(8.0 / dist);
                    float zFond = LinearEyeDepth(SampleSceneDepth(uvR), _ZBufferParams);
                    if (zFond < zSurf) { uvR = uv; zFond = LinearEyeDepth(SampleSceneDepth(uv), _ZBufferParams); }
                    float trajet = max(0, zFond - zSurf) * dist / zSurf;
                    float3 T = exp(-_Absorption.rgb * trajet);
                    float3 eau = SampleSceneColor(uvR) * T + _Diffusion.rgb * (1 - T);
                    // Lumiere traversant le haut des vagues.
                    eau += float3(0.01, 0.10, 0.09) * saturate(i.crete + 0.3) * 0.6;

                    float3 R = reflect(-V, N);
                    R.y = abs(R.y);
                    float fresnel = 0.02 + 0.98 * pow(1 - saturate(dot(N, V)), 5);
                    col = lerp(eau, Ciel(R, soleil.direction, soleil.color), fresnel);

                    float m = Bruit(i.repos * 1.3 + _MerReglages.x * 0.25) * Bruit(i.repos * 0.21 - _MerReglages.x * 0.05);
                    float ecume = saturate((i.crete - 0.42) * 1.6) * smoothstep(0.18, 0.5, m + (Rides(i.repos * 1.7) - 1.4) * 0.22) * 0.8;
                    ecume += smoothstep(0.5, 0.0, trajet) * 0.7;
                    col = lerp(col, float3(0.86, 0.91, 0.93), saturate(ecume * _Ecume));
                }
                else
                {
                    // Vu de dessous : fenetre de Snell, puis reflexion totale.
                    float3 n = -N;
                    float3 refr = refract(-V, n, 1.33);
                    float3 profond = unity_FogColor.rgb;
                    float3 reflechi = profond * 0.75;
                    if (dot(refr, refr) > 0.001)
                    {
                        float F = pow(1 - saturate(dot(N, refr)), 3);
                        float3 ciel = Ciel(refr, soleil.direction, soleil.color) * float3(0.55, 0.85, 0.92);
                        col = lerp(ciel, reflechi, saturate(F));
                    }
                    else col = reflechi;
                }
                col = MixFog(col, i.fogCoord);
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
