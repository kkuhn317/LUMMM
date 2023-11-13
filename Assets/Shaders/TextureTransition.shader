Shader "Custom/TextureTransition"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _Color("Color", Color) = (1,1,1,1)
        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.01
    }

        SubShader
        {
            Tags
            {
                "RenderType" = "Opaque"
            }
            LOD 100

            CGPROGRAM
            #pragma surface surf Lambert

            sampler2D _MainTex;
            fixed4 _Color;
            float _Smoothness;

            struct Input
            {
                float2 uv_MainTex;
            };

            void surf(Input IN, inout SurfaceOutput o)
            {
                // Sample the texture to get the alpha value
                fixed4 texColor = tex2D(_MainTex, IN.uv_MainTex);

                // Smoothstep using the alpha value from the texture
                o.Alpha = smoothstep(0.5 - _Smoothness, 0.5 + _Smoothness, texColor.a);

                // Set the final color
                o.Albedo = _Color.rgb;
            }
            ENDCG
        }
            FallBack "Diffuse"
}
