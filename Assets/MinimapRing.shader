Shader "UI/MinimapRing"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _RingColor ("Ring Color", Color) = (0.53, 0.47, 1.0, 1.0)
        _GlowStrength ("Glow Strength", Float) = 1.5
        _InnerRingAlpha ("Inner Ring Alpha", Float) = 0.15
        _OuterRingAlpha ("Outer Ring Alpha", Float) = 0.2
        _InnerRingRadius ("Inner Ring Radius", Float) = 0.42
        _OuterRingRadius ("Outer Ring Radius", Float) = 0.52
        _RingEdgeWidth ("Ring Edge Width", Float) = 0.01
        
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };
            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };
            sampler2D _MainTex;
            float4 _RingColor;
            float _GlowStrength;
            float _InnerRingAlpha;
            float _OuterRingAlpha;
            float _InnerRingRadius;
            float _OuterRingRadius;
            float _RingEdgeWidth;
            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv - 0.5;
                float dist = length(uv);
                // 外側リング
                float outerRing = smoothstep(_RingEdgeWidth, 0.0,
                                  abs(dist - _OuterRingRadius));
                // 内側リング
                float innerRing = smoothstep(_RingEdgeWidth, 0.0,
                                  abs(dist - _InnerRingRadius));
                fixed4 col = fixed4(0, 0, 0, 0);
                // 外側リング
                col.rgb += _RingColor.rgb * outerRing * _GlowStrength * _OuterRingAlpha;
                col.a = max(col.a, outerRing * _OuterRingAlpha);
                // 内側リング
                col.rgb += _RingColor.rgb * innerRing * _GlowStrength * _InnerRingAlpha;
                col.a = max(col.a, innerRing * _InnerRingAlpha);
                return col;
            }
            ENDCG
        }
    }
}