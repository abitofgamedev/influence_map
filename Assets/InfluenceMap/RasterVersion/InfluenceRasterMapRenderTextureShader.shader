Shader "CustomRenderTexture/InfluenceRasterMapRenderTextureShader"
{
    Properties
     {   
        _Smoothstep1("smoothstep1",Vector)=(0,1,0,0)
        _Smoothstep2("smoothstep2",Vector)=(0,1,0,0)
     }

     SubShader
     {
        Blend One Zero

        Pass
        {
            Name "InfluenceRasterMapRenderTextureShader"

            CGPROGRAM
            #include "UnityCustomRenderTexture.cginc"
            #pragma vertex CustomRenderTextureVertexShader
            #pragma fragment frag

            #pragma target 3.0

            struct MyStruct
            {
                int id;
                float4 color;
                float2 position;
                float radius;
            };
            int _BufferSize;
            int _IdSize;
            StructuredBuffer<int> _Ids;
            float _Step;
            float _Thickness;
            StructuredBuffer<MyStruct> _Buffer;
            float4 _Smoothstep1;
            float4 _Smoothstep2;

            float4 frag(v2f_customrendertexture  i) : SV_Target
            {
                float4 whiteColor=float4(1,1,1,1);
                float4 col = fixed4(0,0,0,0); // Initial color is transparent black
                for(int k=0;k<_IdSize;k++){
                    float4 idColor=float4(0,0,0,0);
                    for (int j = 0; j < _BufferSize; j++)
                    {
                        float dist = distance(i.globalTexcoord, _Buffer[j].position.xy);
                        float fade = smoothstep(0,_Buffer[j].radius, dist);
                        if(_Ids[k]==_Buffer[j].id){
                            idColor += _Buffer[j].color * (1 - fade);
                        }else{
                            idColor -= whiteColor* (1 - fade);
                        }
                    }
                    idColor=clamp(idColor,float4(0,0,0,0),float4(1,1,1,1));
                    col=clamp(idColor+col,float4(0,0,0,0),float4(1,1,1,1));
                }
                float4 step1=smoothstep(_Smoothstep1.x,_Smoothstep1.y,col);
                float4 step2=smoothstep(_Smoothstep2.x,_Smoothstep2.y,col);
                col=step1-step2;
                col=clamp(col,float4(0,0,0,0),float4(1,1,1,1));
                return col; // Return the accumulated color
            }
            ENDCG
        }
    }
}
