Shader "Custom/VertexAngleColor" {
    Properties{
        _ColorFlat("Flat Color", Color) = (0,0,0,1)
        _Color45Deg("45 Degrees Color", Color) = (0.5,0.5,0.5,1)
        _Color180Deg("180 Degrees Color", Color) = (1,1,1,1)
    }
        SubShader{
            Tags { "RenderType" = "Opaque" }
            LOD 100

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
                    float colorLerp : TEXCOORD0;
                };

                fixed4 _ColorFlat;
                fixed4 _Color45Deg;
                fixed4 _Color180Deg;

                v2f vert(appdata v) {
                    v2f o;
                    o.pos = UnityObjectToClipPos(v.vertex);

                    // Calculate angle between up vector and vertex normal
                    float3 up = float3(0, 1, 0);
                    float angle = acos(dot(normalize(v.normal), up));

                    // Normalize the angle to 0-1 for a flat angle to 180 degrees
                    o.colorLerp = angle / UNITY_PI;

                    return o;
                }

                fixed4 frag(v2f i) : SV_Target {
                    // Lerp between black and grey at 45 degrees (0.25 * PI), then grey and white at 180 degrees (1.0 * PI)
                    fixed4 color;
                    if (i.colorLerp <= 0.25) { // 45 degrees
                        color = lerp(_ColorFlat, _Color45Deg, i.colorLerp / 0.25);
                    }
                    else {
                        color = lerp(_Color45Deg, _Color180Deg, (i.colorLerp - 0.25) / 0.75);
                    }

                    return color;
                }
                ENDCG
            }
        }
        FallBack "Diffuse"
}
