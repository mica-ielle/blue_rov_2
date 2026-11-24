// Surface eclairee avec texture calculee (triplanaire) et caustiques sous l'eau.
Shader "BlueROV/Immerge"
{
    Properties
    {
        _Couleur ("Couleur", Color) = (0.7, 0.62, 0.46, 1)
        _Couleur2 ("Couleur secondaire", Color) = (0.45, 0.42, 0.33, 1)
        _Echelle ("Echelle du grain", Float) = 0.6
        _Rides ("Rides de sable", Range(0,1)) = 0
        _Caustiques ("Caustiques", Range(0,3)) = 1.2
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        CBUFFER_START(UnityPerMaterial)
            float4 _Couleur;
            float4 _Couleur2;
            float _Echelle;
            float _Rides;
            float _Caustiques;
        CBUFFER_END
        struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
        ENDHLSL

        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Cull Off
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            float4 _MerReglages;

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float fogCoord : TEXCOORD2;
            };
            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
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
            float Fbm(float2 p) { return Bruit(p) * 0.5 + Bruit(p * 2.1 + 7) * 0.25 + Bruit(p * 4.3 + 3) * 0.15 + Bruit(p * 9.1) * 0.1; }

            float Caustique(float2 p)
            {
                float t = _MerReglages.x * 0.6;
                p += float2(Bruit(p * 0.7 + t * 0.4), Bruit(p.yx * 0.7 - t * 0.35)) * 1.6;
                float a = abs(sin(p.x * 1.6 + t) + sin(p.y * 1.9 - t * 0.8)) * 0.5;
                float b = abs(sin((p.x + p.y) * 1.3 - t * 0.7) + sin((p.x - p.y) * 1.1 + t * 1.1)) * 0.5;
                return pow(1 - min(a, b), 7);
            }

            half4 frag(Varyings i, bool avant : SV_IsFrontFace) : SV_Target
            {
                float3 N = normalize(i.normalWS) * (avant ? 1 : -1);
                float3 w = pow(abs(N), 4); w /= w.x + w.y + w.z;
                float3 p = i.positionWS * _Echelle;
                float g = Fbm(p.zy) * w.x + Fbm(p.xz) * w.y + Fbm(p.xy) * w.z;
                float macro = Bruit(i.positionWS.xz * 0.06);
                float3 alb = lerp(_Couleur2.rgb, _Couleur.rgb, saturate(g * 1.1 + macro * 0.4 - 0.1));

                // Rides de sable, estompees au loin.
                float dist = distance(i.positionWS, _WorldSpaceCameraPos);
                float ph = i.positionWS.x * 9 + Bruit(i.positionWS.xz * 0.5) * 9 + i.positionWS.z * 3;
                N = normalize(N + float3(cos(ph), 0, cos(ph) * 0.33) * _Rides * 0.35 * saturate(1 - dist / 25));

                Light l = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                float diff = saturate(dot(N, l.direction)) * l.shadowAttenuation;
                float prof = max(0, _MerReglages.z - i.positionWS.y);
                float c = Caustique(i.positionWS.xz + i.positionWS.y * 0.35) * _Caustiques
                          * saturate(prof) * exp(-prof / 18) * saturate(N.y + 0.2) * saturate(1 - dist / 70);
                float3 lum = l.color * diff * (1 + c * 2);
                // Le rouge disparait en premier avec la profondeur et la distance.
                float eauVue = _WorldSpaceCameraPos.y < _MerReglages.z ? dist : 0;
                lum *= exp(-float3(0.045, 0.012, 0.008) * prof);
                float3 col = alb * (lum + SampleSH(N)) * exp(-float3(0.05, 0.012, 0.006) * min(prof * 4, eauVue));

                #if defined(_ADDITIONAL_LIGHTS)
                InputData inputData = (InputData)0;
                inputData.positionWS = i.positionWS;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);
                uint pixelLightCount = GetAdditionalLightsCount();
                LIGHT_LOOP_BEGIN(pixelLightCount)
                    Light a = GetAdditionalLight(lightIndex, i.positionWS);
                    col += alb * a.color * (a.distanceAttenuation * saturate(dot(N, a.direction)));
                LIGHT_LOOP_END
                #endif

                return half4(MixFog(col, i.fogCoord), 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ColorMask 0
            Cull Off
            HLSLPROGRAM
            #pragma vertex vertOmbre
            #pragma fragment fragOmbre
            float3 _LightDirection;
            float4 vertOmbre(Attributes v) : SV_POSITION
            {
                float3 w = TransformObjectToWorld(v.positionOS.xyz);
                float4 c = TransformWorldToHClip(ApplyShadowBias(w, TransformObjectToWorldNormal(v.normalOS), _LightDirection));
                #if UNITY_REVERSED_Z
                c.z = min(c.z, UNITY_NEAR_CLIP_VALUE);
                #else
                c.z = max(c.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                return c;
            }
            half4 fragOmbre() : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ColorMask R
            Cull Off
            HLSLPROGRAM
            #pragma vertex vertProf
            #pragma fragment fragProf
            float4 vertProf(Attributes v) : SV_POSITION { return TransformObjectToHClip(v.positionOS.xyz); }
            half4 fragProf(float4 p : SV_POSITION) : SV_Target { return p.z; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormals" }
            Cull Off
            HLSLPROGRAM
            #pragma vertex vertDN
            #pragma fragment fragDN
            struct V { float4 p : SV_POSITION; float3 n : TEXCOORD0; };
            V vertDN(Attributes v)
            {
                V o;
                o.p = TransformObjectToHClip(v.positionOS.xyz);
                o.n = TransformObjectToWorldNormal(v.normalOS);
                return o;
            }
            half4 fragDN(V i) : SV_Target { return half4(normalize(i.n), 0); }
            ENDHLSL
        }
    }
}
