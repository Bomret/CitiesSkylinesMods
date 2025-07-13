/*
 * Chromatic Aberration Composite Shader
 * ====================================
 *
 * This shader simulates chromatic aberration by splitting the color channels (RGB)
 * and shifting them radially from the center of the screen, creating a realistic
 * optical distortion effect similar to camera lenses.
 *
 * Optimized for Unity 5.6.7f1 (Cities: Skylines).
 *
 * Pass Overview:
 * 1. Full Screen Effect - Applies chromatic aberration to the entire screen buffer
 */
Shader "Hidden/ChromaticAberration" {
    Properties {
        _MainTex ("Base Texture", 2D) = "white" {}
        _Aberration ("Aberration Strength", Range(0.0, 1.0)) = 0.04
        _Distortion ("Distortion Amount", Range(0.0, 1.0)) = 0.6
    }

    CGINCLUDE
    #include "UnityCG.cginc"

    // Texture and sampler declarations
    sampler2D _MainTex;
    float4 _MainTex_ST;
    float4 _MainTex_TexelSize;
    
    // Effect parameters
    float _Aberration;
    float _Distortion;

    /*
     * Vertex to fragment structure
     */
    struct v2f {
        float4 pos : SV_POSITION;
        half2 uv : TEXCOORD0;

        #if UNITY_UV_STARTS_AT_TOP
        half2 uv1 : TEXCOORD1;
        half2 direction : TEXCOORD2;
        half distance : TEXCOORD3;
        #else
        half2 direction : TEXCOORD1;
        half distance : TEXCOORD2;
        #endif
    };

    /*
     * Vertex shader - prepares UV coordinates, direction, and distance
     */
    v2f vert(appdata_img v) {
        v2f o;
        o.pos = UnityObjectToClipPos(v.vertex);
        o.uv = UnityStereoScreenSpaceUVAdjust(v.texcoord, _MainTex_ST);

        #if UNITY_UV_STARTS_AT_TOP
        o.uv1 = v.texcoord;
        if (_MainTex_TexelSize.y < 0)
            o.uv1.y = 1 - o.uv1.y;
        #endif

        half2 offset = o.uv - half2(0.5, 0.5);
        o.distance = length(offset);
        o.direction = o.distance > 0.0 ? normalize(offset) : half2(0.0, 0.0);

        return o;
    }

    /*
     * Consolidated UV coordinate helper function
     * Ensures correct UVs for all platforms
     */
    half2 GetCorrectUV(v2f i) {
        #if UNITY_UV_STARTS_AT_TOP
        return i.uv1;
        #else
        return i.uv;
        #endif
    }

    /*
     * Fragment shader - applies chromatic aberration effect
     */
    fixed4 frag(v2f i) : SV_Target {
        half2 uv = GetCorrectUV(i);
        half distance = i.distance;
        half2 direction = i.direction;

        // Early exit for center pixels
        if (distance < half(0.0001)) {
            return tex2D(_MainTex, uv);
        }

        half aberrationAmount = min(_Aberration * _Distortion * distance * distance, half(0.02));
        half mask = step(half(0.3), distance);
        fixed4 original = tex2D(_MainTex, uv);

        half2 aberrDir = direction * aberrationAmount;

        half2 redUV = saturate(uv + aberrDir * half(0.6));
        half2 greenUV = saturate(uv + aberrDir * half(0.15));
        half2 blueUV = saturate(uv - aberrDir * half(0.5));
        
        half redChannel = tex2D(_MainTex, redUV).r;
        half greenChannel = tex2D(_MainTex, greenUV).g;
        half blueChannel = tex2D(_MainTex, blueUV).b;
        half alpha = tex2D(_MainTex, uv).a;
        
        fixed4 aberrated = fixed4(redChannel, greenChannel, blueChannel, alpha);

        return lerp(original, aberrated, mask);
    }
    ENDCG

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
            ENDCG
        }
    }

    Fallback off
}
