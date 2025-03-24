Shader "Unlit/BlitDrawShader"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "black" {}
        _BrushPosition ("Brush Position", Vector) = (0,0,0,0)
        _BrushSize ("Brush Size", Float) = 0.1
        _AspectRatio ("Aspect Ratio", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };
            
            sampler2D _MainTex;
            float4 _BrushPosition;
            float _BrushSize;
            float _AspectRatio;
            
            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            
            float4 frag (v2f i) : SV_Target
            {
                float4 texColor = tex2D(_MainTex, i.uv);
                
                // Create a circular brush by using an adjusted UV space
                float2 adjustedUV = i.uv;
                adjustedUV.x = adjustedUV.x * _AspectRatio;
                
                float2 adjustedBrush = _BrushPosition.xy;
                adjustedBrush.x = adjustedBrush.x * _AspectRatio;
                
                // Compute brush influence with aspect ratio correction
                float dist = distance(adjustedUV, adjustedBrush);
                float scaledBrushSize = _BrushSize * max(1.0, _AspectRatio);
                float influence = smoothstep(scaledBrushSize, 0.0, dist);
                
                // Blend new brush mark with existing texture
                return lerp(texColor, float4(1,1,1,1), influence); // White trail
            }
            ENDCG
        }
    }
}