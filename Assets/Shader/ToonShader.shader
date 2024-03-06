// Custom toon shader definition
Shader "Custom/ToonShader"
{
    // Define properties that can be set in the material inspector
    Properties{
        _Color("Main Color", Color) = (1,1,1,1) // Base color of the material
        _OutlineColor("Outline Color", Color) = (0,0,0,1) // Color of the outline
        _OutlineWidth("Outline Width", Range(0.0, 0.2)) = 0.1 // Width of the outline
        _ToonThreshold1("Toon Threshold 1", Range(0.0, 1.0)) = 0.5 // First threshold for toon shading
        _ToonThreshold2("Toon Threshold 2", Range(0.0, 1.0)) = 0.3 // Second threshold
        _ToonThreshold3("Toon Threshold 3", Range(0.0, 1.0)) = 0.2 // Third threshold
        _ToonThreshold4("Toon Threshold 4", Range(0.0, 1.0)) = 0.1 // Fourth threshold
    }

    SubShader{
        Tags { "RenderType" = "Opaque" }
        LOD 100

        // Outline pass
        Pass {
            ZWrite On // Enable depth writing
            Cull Front // Render only the back faces (to create an outline effect)
            Offset 1, 1 // Apply depth offset to prevent z-fighting

            CGPROGRAM
            #pragma vertex vertOutline
            #pragma fragment fragOutline
            #include "UnityCG.cginc"

            struct appdataOutline {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2fOutline {
                float4 pos : SV_POSITION;
            };

            float _OutlineWidth;
            float4 _OutlineColor;

            v2fOutline vertOutline(appdataOutline v) {
                v2fOutline o;
                v.vertex.xyz += v.normal * _OutlineWidth;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 fragOutline(v2fOutline i) : SV_Target {
                return _OutlineColor;
            }
            ENDCG
        }

        // Toon shading pass
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float3 normal : TEXCOORD0;
            };

            // Toon shading parameters
            fixed4 _Color;
            float _ToonThreshold1;
            float _ToonThreshold2;
            float _ToonThreshold3;
            float _ToonThreshold4;

            v2f vert(appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            // Fragment shader for toon shading
            fixed4 frag(v2f i) : SV_Target {
                // Calculate lighting
                float3 worldNormal = normalize(i.normal);
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float ndotl = max(0, dot(worldNormal, lightDir));

                // Apply toon shading based on light intensity
                float4 color = _Color;
                if (ndotl > _ToonThreshold1) {
                    color *= 1.0; // Full color intensity
                }
                else if (ndotl > _ToonThreshold2) {
                    color *= 0.7; // Slightly dimmed intensity
                }
                else if (ndotl > _ToonThreshold3) {
                    color *= 0.5; // Moderately dimmed intensity
                }
                else if (ndotl > _ToonThreshold4) {
                    color *= 0.3; // Heavily dimmed intensity
                }
                else {
                    color *= 0.1; // Darkest areas
                }

                return color;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
