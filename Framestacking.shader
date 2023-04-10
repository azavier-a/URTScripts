Shader "Hidden/Framestacking"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "black" {}
        _MainTexOld ("TextureOld", 2D) = "black" {}
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

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
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            sampler2D _MainTexOld;

            int NumRenderedFrames;

            fixed4 frag (v2f i) : SV_Target
            {
                float4 oldFrame = tex2D(_MainTexOld, i.uv);
                float4 newFrame = tex2D(_MainTex, i.uv);
                
                float weight = 1.0 / (NumRenderedFrames+1);
                float4 accumulatedAverage = oldFrame*(1 - weight) + newFrame*weight;
                return accumulatedAverage;
            }
            ENDCG
        }
    }
}
