Shader "Unlit/InfluenceVectorMapShaderExample"
{
    Properties
    {
        _Step("Step",float)=0.01
        _Thickness("Thickness",float)=0.01
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
            };

            struct MyStruct
            {
                int id;
                float4 color;
                float3 position;
                float radius;
            };

            int _BufferSize;
            int _IdSize;
            StructuredBuffer<int> _Ids;
            float _Step;
            float _Thickness;
            StructuredBuffer<MyStruct> _Buffer;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float4 col = fixed4(0,0,0,0);
                for(int k=0;k<_IdSize;k++){
                    float4 idColor=float4(0,0,0,0);
                    for (int j = 0; j < _BufferSize; j++)
                    {
                       float dist = distance(i.worldPos, _Buffer[j].position.xyz);
                       float fade = smoothstep(0,_Buffer[j].radius, dist);
                       if(_Ids[k]==_Buffer[j].id){
                           idColor += _Buffer[j].color * (1 - fade);
                       }else{
                           idColor -= float4(1,1,1,1)* (1 - fade);
                       }             
                    }
                    idColor=clamp(idColor,float4(0,0,0,0),float4(1,1,1,1));
                    col=clamp(idColor+col,float4(0,0,0,0),float4(1,1,1,1));
                }
                float4 step1=step(_Step,col);
                float4 step2=step(_Step+_Thickness,col);
                col=step1-step2;
                col=clamp(col,float4(0,0,0,0),float4(1,1,1,1));
                return col;
            }
            ENDCG
        }
    }
}
