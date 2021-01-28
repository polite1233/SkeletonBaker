Shader "Custom/Animate"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" { }
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" }

        Pass
        {
            HLSLPROGRAM
            
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "GPUAnimation.hlsl"
            
            CBUFFER_START(UnityPerMaterials)
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            half4 _MainTex_ST;
            CBUFFER_END
            
            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _TextureCoord)
            UNITY_INSTANCING_BUFFER_END(Props)

            struct appdata
            {
                float4 vertex: POSITION;
                float2 uv: TEXCOORD0;
                UNITY_VERTEX_INPUT_GPUANIMATION
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv: TEXCOORD0;
                float4 pos: SV_POSITION;
            };
            
            v2f vert(appdata v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                float4 coord= UNITY_ACCESS_INSTANCED_PROP(Props, _TextureCoord);
                
                float4x4 skinMatrix = CalculateSkinMatrix(coord.xyz, v.boneIds, v.boneInfluences);
                v2f o;
                o.pos = TransformObjectToHClip(mul(skinMatrix,v.vertex));
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                
                return o;
            }

            half4 frag(v2f i): SV_Target
            {
                //UNITY_SETUP_INSTANCE_ID(v);
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                return  col;
            }
            ENDHLSL
            
        }
    }
    FallBack "Specular"
}
