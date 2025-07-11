Shader "Hidden/SunShaftsComposite" {
    Properties {
        _MainTex ("Base", 2D) = "" {}
        _ColorBuffer ("Color", 2D) = "" {}
        _Skybox ("Skybox", 2D) = "" {}
    }
    
    CGINCLUDE

    #include "UnityCG.cginc"
    
    struct v2f {
        float4 pos : SV_POSITION;
        float2 uv : TEXCOORD0;
        #if UNITY_UV_STARTS_AT_TOP
        float2 uv1 : TEXCOORD1;
        #endif
    };

    struct v2f_radial {
        float4 pos : SV_POSITION;
        float2 uv : TEXCOORD0;
        float2 blurVector : TEXCOORD1;
    };

    sampler2D _MainTex;
    sampler2D _ColorBuffer;
    sampler2D _Skybox;
    sampler2D_float _CameraDepthTexture;

    uniform half4 _SunThreshold;

    uniform half4 _SunColor;
    uniform half4 _BlurRadius4;
    uniform half4 _SunPosition;
    uniform half4 _MainTex_TexelSize;	

    #define SAMPLES_FLOAT 6.0f
    #define SAMPLES_INT 6

    v2f vert( appdata_img v ) {
        v2f o;
        o.pos = UnityObjectToClipPos(v.vertex);
        o.uv = v.texcoord.xy;
        
        #if UNITY_UV_STARTS_AT_TOP
        o.uv1 = v.texcoord.xy;
        if (_MainTex_TexelSize.y < 0)
            o.uv1.y = 1-o.uv1.y;
        #endif

        return o;
    }

    // Helper to get the correct UV coordinates for ColorBuffer sampling
    float2 GetColorBufferUV(v2f i) {
        #if UNITY_UV_STARTS_AT_TOP
        return i.uv1.xy;
        #else
        return i.uv.xy;
        #endif
    }
    
    // Helper to get the correct UV coordinates for depth texture sampling
    float2 GetDepthUV(v2f i) {
        #if UNITY_UV_STARTS_AT_TOP
        return i.uv1.xy;
        #else
        return i.uv.xy;
        #endif
    }
    
    // Helper to get the correct UV coordinates for skybox sampling
    float2 GetSkyboxUV(v2f i) {
        #if UNITY_UV_STARTS_AT_TOP
        return i.uv1.xy;
        #else
        return i.uv.xy;
        #endif
    }
    
    // Helper to calculate sun distance with correct UV coordinates
    half GetSunDistance(v2f i) {
        #if UNITY_UV_STARTS_AT_TOP
        half2 sunOffset = _SunPosition.xy - i.uv1.xy;
        #else
        half2 sunOffset = _SunPosition.xy - i.uv.xy;		
        #endif
        return saturate (_SunPosition.w - length (sunOffset));
    }

    half4 fragScreen(v2f i) : SV_Target { 
        half4 colorA = tex2D (_MainTex, i.uv.xy);
        half4 colorB = tex2D (_ColorBuffer, GetColorBufferUV(i));
        half4 depthMask = saturate (colorB * _SunColor);

        return 1.0f - (1.0f-colorA) * (1.0f-depthMask);
    }

    half4 fragAdd(v2f i) : SV_Target { 
        half4 colorA = tex2D (_MainTex, i.uv.xy);
        half4 colorB = tex2D (_ColorBuffer, GetColorBufferUV(i));
        half4 depthMask = saturate (colorB * _SunColor);

        return colorA + depthMask;	
    }
    
    v2f_radial vert_radial( appdata_img v ) {
        v2f_radial o;
        o.pos = UnityObjectToClipPos(v.vertex);
        o.uv.xy = v.texcoord.xy;
        
        // Precalculate blur vector: direction from UV to sun position, scaled by blur radius
        half2 toSunVector = _SunPosition.xy - v.texcoord.xy;
        o.blurVector = toSunVector * _BlurRadius4.xy;

        return o; 
    }

    half4 frag_radial(v2f_radial i) : SV_Target 
    {
        half4 color = half4(0,0,0,0);

        for(int sampleIndex = 0; sampleIndex < SAMPLES_INT; sampleIndex++)
        {
            half4 sampleColor = tex2D(_MainTex, i.uv.xy);
            color += sampleColor;
            i.uv.xy += i.blurVector; 
        }

        return color * 0.16666667f; // Optimized: precomputed 1/6 instead of division
    }
    
    half TransformColor (half4 skyboxValue) {
        half3 thresholded = saturate(skyboxValue.rgb - _SunThreshold.rgb);

        return thresholded.r + thresholded.g + thresholded.b;
    }

    half4 frag_depth (v2f i) : SV_Target {
        float depthSample = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, GetDepthUV(i));
        half4 tex = tex2D (_MainTex, i.uv.xy);

        half linearDepth = Linear01Depth (depthSample);

        // consider maximum radius
        half dist = GetSunDistance(i);

        // consider shafts blockers - eliminate branching with step function
        half depthMask = step(0.99, linearDepth);
        half4 outColor = TransformColor (tex) * dist * depthMask;

        return outColor;
    }

    half4 frag_nodepth (v2f i) : SV_Target {
        half4 sky = (tex2D (_Skybox, GetSkyboxUV(i)));
        half4 tex = (tex2D (_MainTex, i.uv.xy));

        // consider maximum radius
        half dist = GetSunDistance(i);

        // find unoccluded sky pixels - eliminate branching with step function
        // consider pixel values that differ significantly between framebuffer and sky-only buffer as occluded
        half luminanceDiff = Luminance(abs(sky.rgb - tex.rgb));
        half occlusionMask = step(luminanceDiff, 0.2);
        half4 outColor = TransformColor (sky) * dist * occlusionMask;

        return outColor;
    }

    ENDCG

    Subshader {

     Pass {
          ZTest Always Cull Off ZWrite Off

          CGPROGRAM

          #pragma vertex vert
          #pragma fragment fragScreen
          
          ENDCG
      }

     Pass {
          ZTest Always Cull Off ZWrite Off

          CGPROGRAM

          #pragma vertex vert_radial
          #pragma fragment frag_radial

          ENDCG
      }
      
      Pass {
          ZTest Always Cull Off ZWrite Off

          CGPROGRAM

          #pragma vertex vert
          #pragma fragment frag_depth

          ENDCG
      }
      
      Pass {
          ZTest Always Cull Off ZWrite Off

          CGPROGRAM
          
          #pragma vertex vert
          #pragma fragment frag_nodepth

          ENDCG
      } 
      
      Pass {
          ZTest Always Cull Off ZWrite Off

          CGPROGRAM

          #pragma vertex vert
          #pragma fragment fragAdd

          ENDCG
      } 
    }

    Fallback off
}
