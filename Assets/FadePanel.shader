Shader "Custom/FadePanel"
{
    Properties
    {
        _Color ("Color", Color) = (0,0,0,1)
        _FadeStrength ("Fade Strength", Range(0,1)) = 0.8
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

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
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float4 _Color;
            float _FadeStrength;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;

                // 左右のフェード
                float fadeX = smoothstep(0.0, _FadeStrength, uv.x) 
                            * smoothstep(0.0, _FadeStrength, 1.0 - uv.x);

                // 下のフェード
                float fadeY = smoothstep(0.0, _FadeStrength, uv.y);

                float alpha = _Color.a * fadeX * fadeY;
                return fixed4(_Color.rgb, alpha);
            }
            ENDCG
        }
    }
}