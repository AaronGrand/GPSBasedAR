Shader "Custom/TerrainGrid"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _DotSpacing("Dot Spacing", Range(1,1000)) = 5.0
        _DotAlpha("Dot Alpha", Range(0,1)) = 1.0
        _BaseAlpha("Base Alpha", Range(0,1)) = 1.0
        _Opacity("Opacity", Range(0,1)) = 1.0
    }

        SubShader
        {
            Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
            LOD 100

            Pass
            {
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #include "UnityCG.cginc"

                struct appdata
                {
                    float4 vertex : POSITION;
                    float2 uv : TEXCOORD0;
                };

                struct v2f
                {
                    float2 uv : TEXCOORD0;
                    float3 worldPos : TEXCOORD1;
                    float4 vertex : SV_POSITION;
                };

                sampler2D _MainTex;
                float _DotSpacing;
                float _DotAlpha;
                float _BaseAlpha;
                float _Opacity;

                v2f vert(appdata v)
                {
                    v2f o;
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.uv = v.uv;
                    o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                    return o;
                }

                half4 frag(v2f i) : SV_Target
                {
                    // Calculate the dot's position
                    float2 gridPos = i.worldPos.xz / _DotSpacing;
                    float2 fracPart = frac(gridPos);

                    // If the point is close to an integer (which represents a 5m interval), color it. 
                    float dot = step(0.95, fracPart.x) * step(0.95, fracPart.y) + step(0.05, fracPart.x) * step(0.05, fracPart.y);

                    half4 col = tex2D(_MainTex, i.uv);
                    col.a *= _BaseAlpha;

                    half4 result = dot ? half4(0, 0, 0, _DotAlpha) : col;
                    result.a *= _Opacity;
                    return result;
                }
                ENDCG
            }
        }
}