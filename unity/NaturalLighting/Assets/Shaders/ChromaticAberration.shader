/*
 * Chromatic Aberration Shader
 * ===========================
 * 
 * This shader simulates chromatic aberration effect by splitting the color channels (RGB)
 * and shifting them radially from the center of the screen. This creates a realistic
 * optical distortion effect similar to what camera lenses produce.
 * 
 * Optimized for Unity 5.6.7f1 (Cities: Skylines) with efficient single-pass rendering.
 * 
 * Pass Overview:
 * 1. Full Screen Effect - Applies chromatic aberration to the entire screen buffer
 */

Shader "Hidden/ChromaticAberration" {
    Properties {
        _MainTex ("Base Texture", 2D) = "white" {}
        _Aberration ("Aberration Strength", Range(0.0, 1.0)) = 0.3
        _Distortion ("Distortion Amount", Range(0.0, 2.0)) = 1.0
        _CenterX ("Center X", Range(0.0, 1.0)) = 0.5
        _CenterY ("Center Y", Range(0.0, 1.0)) = 0.5
    }

    SubShader {
        Tags { 
            "RenderType" = "Overlay"
            "Queue" = "Overlay"
        }
        
        ZTest Always
        ZWrite Off
        Cull Off
        Fog { Mode Off }

        Pass {
            Name "ChromaticAberration"
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            
            #include "UnityCG.cginc"

            // Texture and sampler declarations
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            
            // Effect parameters
            float _Aberration;
            float _Distortion;
            float _CenterX;
            float _CenterY;

            /*
             * Vertex to fragment structure
             */
            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 center : TEXCOORD1;  // Precomputed center offset
            };

            /*
             * Vertex shader - prepares UV coordinates and center offset
             */
            v2f vert(appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = UnityStereoScreenSpaceUVAdjust(v.uv, _MainTex_ST);
                o.center = float2(_CenterX, _CenterY);
                
                #if UNITY_UV_STARTS_AT_TOP
                if (_MainTex_TexelSize.y < 0)
                    o.uv.y = 1 - o.uv.y;
                #endif
                
                return o;
            }

            /*
             * Fragment shader - applies chromatic aberration effect
             * 
             * Algorithm:
             * 1. Calculate distance from current pixel to aberration center
             * 2. Apply radial distortion based on distance
             * 3. Sample each color channel (R, G, B) with different UV offsets
             * 4. Red channel is shifted outward, blue inward, green stays centered
             * 5. Combine channels for final color
             */
            fixed4 frag(v2f i) : SV_Target {
                float2 uv = i.uv;
                float2 center = i.center;
                
                // Calculate distance from center (0.0 at center, increases toward edges)
                float2 offset = uv - center;
                float distance = length(offset);
                
                // Apply controlled distortion curve with clamping to prevent extreme effects
                float distortionFactor = _Distortion * distance * distance;
                
                // Cap the maximum effect strength to prevent extreme aberration at edges
                float aberrationAmount = _Aberration * distortionFactor;
                aberrationAmount = min(aberrationAmount, 0.02); // Clamp maximum effect
                
                // Apply effect starting from a reasonable distance from center
                if (distance < 0.15) {
                    return tex2D(_MainTex, uv);
                }
                
                // Normalize offset direction for radial displacement
                float2 direction = normalize(offset);
                
                // Controlled channel displacement - visible but capped
                // Red channel: moderate shift outward
                float2 redUV = uv + direction * aberrationAmount * 0.6;
                
                // Green channel: slight shift for realistic look
                float2 greenUV = uv + direction * aberrationAmount * 0.15;
                
                // Blue channel: moderate shift inward
                float2 blueUV = uv - direction * aberrationAmount * 0.5;
                
                // Clamp UV coordinates to prevent sampling outside texture bounds
                redUV = clamp(redUV, 0.0, 1.0);
                greenUV = clamp(greenUV, 0.0, 1.0);
                blueUV = clamp(blueUV, 0.0, 1.0);
                
                // Sample each color channel independently
                float redChannel = tex2D(_MainTex, redUV).r;
                float greenChannel = tex2D(_MainTex, greenUV).g;
                float blueChannel = tex2D(_MainTex, blueUV).b;
                
                // Get alpha from the original (unshifted) sample
                float alpha = tex2D(_MainTex, uv).a;
                
                // Combine channels into final color
                return fixed4(redChannel, greenChannel, blueChannel, alpha);
            }
            
            ENDCG
        }
    }

    /*
     * Fallback for older hardware that doesn't support the main shader
     */
    Fallback Off
}
