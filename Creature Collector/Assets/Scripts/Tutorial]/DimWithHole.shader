Shader "UI/DimWithHole"
{
    // Draws a flat-colored dim overlay, but fully discards (skips) pixels
    // that fall inside a rectangular "hole" region - producing a clean cutout
    // without needing any extra GameObjects or RectTransforms.
    //
    // _HoleMin / _HoleMax are in the Image's own normalized UV space (0-1),
    // where (0,0) is the bottom-left of the Image and (1,1) is the top-right.
    // TutorialManager.cs sets these two values per tutorial step.

    Properties
    {
        _Color ("Dim Color", Color) = (0, 0, 0, 0.6)
        _HoleMin ("Hole Min (UV 0-1)", Vector) = (2, 2, 0, 0)
        _HoleMax ("Hole Max (UV 0-1)", Vector) = (-2, -2, 0, 0)

        // Standard UI shader plumbing so this still works inside Masks/RectMask2D
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

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 texcoord : TEXCOORD0;
            };

            fixed4 _Color;
            float4 _HoleMin;
            float4 _HoleMax;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Inside the hole rect -> discard this pixel entirely (fully see-through)
                if (i.texcoord.x >= _HoleMin.x && i.texcoord.x <= _HoleMax.x &&
                    i.texcoord.y >= _HoleMin.y && i.texcoord.y <= _HoleMax.y)
                {
                    discard;
                }

                return _Color;
            }
            ENDCG
        }
    }
}
