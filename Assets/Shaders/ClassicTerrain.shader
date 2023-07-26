// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "OpenTS2/ClassicTerrain"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Variation1("Variation 1", 2D) = "white" {}
        _Variation2("Variation 2", 2D) = "white" {}
        _CliffTex("Cliff Texture", 2D) = "white" {}
        _MatCap("MatCap", 2D) = "white" {}
        _LightVector("Light Vector", Vector) = (.33, .33, -.33, 0)
        _Ambient("Ambient", float) = 0
        _Subtract("Subtract", float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
                float4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float2 matcapUv : TEXCOORD1;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float cliff : TEXCOORD2;
            };

            sampler2D _MainTex;
            sampler2D _Variation1;
            sampler2D _Variation2;
            sampler2D _MatCap;
            sampler2D _CliffTex;
            float4 _MainTex_ST;
            float4 _LightVector;
            float _Subtract;
            float _Ambient;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                //o.vertex = float4(v.vertex.x/128, v.vertex.z/128, 0, 0);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                //o.uv = float2(v.normal.x, v.normal.y);
                float3 lightingDirection = -normalize(_LightVector.xyz);
                float3 lightCross = cross(lightingDirection, float3(0.0, 1.0, 0.0));
                float3 worldNormal = mul(unity_ObjectToWorld, float4(v.normal, 0.0)).xyz;
                float lightDot = dot(worldNormal, lightingDirection);
                float lightDotRight = dot(worldNormal, lightCross);
                
                
                //o.uv = float2(lightDotRight, lightDot) - float2(v.color.x,v.color.x) + float2(0.4,0.4);
                //o.uv.x = max(0.2, o.uv.x);
                //o.uv.y = max(0.2, o.uv.y);
                o.matcapUv = max(0,((float2(lightDot, lightDot) + float2(lightDotRight, 0.0)) * -(v.color.x - 1)) - _Subtract);
                o.matcapUv += float2(_Ambient, _Ambient);
                o.uv = float2(v.vertex.x, v.vertex.z) * float2(_MainTex_ST.x, _MainTex_ST.y);
                o.uv += float2(_MainTex_ST.x, _MainTex_ST.y);
                o.color = v.color;
                o.cliff = max(0,min(1,pow(-(dot(worldNormal, float3(0.0, 1.0, 0.0)) - 1), 2) * 3));
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
            if (i.color.g >= 0.99)
                col = tex2D(_Variation1, i.uv);
            if (i.color.b >= 0.99)
                col = tex2D(_Variation2, i.uv);
            fixed4 cliffCol = tex2D(_CliffTex, i.uv);
            col = lerp(col, cliffCol, i.cliff);
            col *= tex2D(_MatCap, i.matcapUv);
            
            //col = i.color;
                //fixed4 col = ;
            //col.z = 0;
            //col.x = i.uv.x;
            //col.y = i.uv.y;
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
