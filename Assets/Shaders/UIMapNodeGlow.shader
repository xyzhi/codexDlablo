Shader "UI/MapNodeGlow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _OuterGlowColor ("Outer Glow Color", Color) = (0.68,0.16,0.08,1)
        _InnerGlowColor ("Inner Glow Color", Color) = (1,0.52,0.18,1)
        _OuterGlowSize ("Outer Glow Size", Range(0.5, 8)) = 5.1
        _InnerGlowSize ("Inner Glow Size", Range(0.5, 8)) = 2.25
        _OuterGlowStrength ("Outer Glow Strength", Range(0, 4)) = 1.08
        _InnerGlowStrength ("Inner Glow Strength", Range(0, 4)) = 0.9
        _InnerFade ("Inner Fade", Range(0, 1)) = 0.9
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            fixed4 _OuterGlowColor;
            fixed4 _InnerGlowColor;
            float _OuterGlowSize;
            float _InnerGlowSize;
            float _OuterGlowStrength;
            float _InnerGlowStrength;
            float _InnerFade;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.texcoord;
                float baseAlpha = tex2D(_MainTex, uv).a;
                float2 innerTexel = _MainTex_TexelSize.xy * _InnerGlowSize;
                float2 outerTexel = _MainTex_TexelSize.xy * _OuterGlowSize;

                float innerExpanded = 0.0;
                float outerExpanded = 0.0;

                const float2 directions[12] =
                {
                    float2(1.0, 0.0),
                    float2(0.866, 0.5),
                    float2(0.5, 0.866),
                    float2(0.0, 1.0),
                    float2(-0.5, 0.866),
                    float2(-0.866, 0.5),
                    float2(-1.0, 0.0),
                    float2(-0.866, -0.5),
                    float2(-0.5, -0.866),
                    float2(0.0, -1.0),
                    float2(0.5, -0.866),
                    float2(0.866, -0.5)
                };

                [unroll]
                for (int d = 0; d < 12; d++)
                {
                    innerExpanded = max(innerExpanded, tex2D(_MainTex, uv + directions[d] * innerTexel).a);
                    outerExpanded = max(outerExpanded, tex2D(_MainTex, uv + directions[d] * outerTexel).a);
                }

                float innerAlpha = saturate(innerExpanded - baseAlpha * _InnerFade) * _InnerGlowStrength;
                float outerAlpha = saturate(outerExpanded - innerExpanded * 0.72) * _OuterGlowStrength;

                fixed4 innerGlow = _InnerGlowColor * i.color;
                innerGlow.a = innerAlpha * innerGlow.a;
                innerGlow.rgb *= innerGlow.a;

                fixed4 outerGlow = _OuterGlowColor * i.color;
                outerGlow.a = outerAlpha * outerGlow.a;
                outerGlow.rgb *= outerGlow.a;

                fixed4 result = outerGlow + innerGlow;
                result.a = saturate(outerGlow.a + innerGlow.a);
                return result;
            }
            ENDCG
        }
    }
}
