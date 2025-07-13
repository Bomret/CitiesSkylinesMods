/*
 * Sun Shafts Composite Shader
 * =========================
 * 
 * This shader creates volumetric sun shafts (god rays) by rendering the sun's influence
 * on the scene in multiple passes. It's optimized for Unity 5.6.7f1 which is used by Cities: Skylines.
 * The radial blur uses 6 samples per pixel for smooth ray appearance with good performance.
 * 
 * Pass Overview:
 * 1. Screen Blend - Composites sun rays with the main scene using screen blend mode
 * 2. Radial Blur - Creates the ray effect by sampling along rays from each pixel to the sun (6 samples)
 * 3. Depth Occlusion - Masks rays based on scene depth (objects block sunlight)  
 * 4. Sky Occlusion - Masks rays based on sky visibility (alternate occlusion method)
 * 5. Additive Blend - Alternative compositing method using additive blending
 */

Shader "Hidden/SunShafts" {
    Properties {
        _MainTex ("Base", 2D) = "" {}     // Main scene texture (what we're adding rays to)
        _ColorBuffer ("Color", 2D) = "" {} // Processed color data for sun rays
        _Skybox ("Skybox", 2D) = "" {}     // Sky texture for occlusion calculations
    }

    CGINCLUDE

    #include "UnityCG.cginc"
    
    /*
     * Standard vertex-to-fragment struct for basic passes
     * Used by: screen blend, depth occlusion, sky occlusion, additive blend
     */
    struct v2f {
        float4 pos : SV_POSITION;  // Clip space position
        float2 uv : TEXCOORD0;     // Main texture coordinates
        #if UNITY_UV_STARTS_AT_TOP
        float2 uv1 : TEXCOORD1;    // Corrected UV coordinates for platforms where UV origin is top-left
        #endif
    };

    /*
     * Optimized vertex-to-fragment struct for radial blur pass
     * Precalculates all 6 sample UV coordinates in the vertex shader to reduce fragment shader work
     * This moves computation from fragment (runs per pixel) to vertex (runs per vertex)
     */
    struct v2f_radial {
        float4 pos : SV_POSITION;
        float2 uv : TEXCOORD0;              // Base UV coordinates
        float2 blurVector : TEXCOORD1;      // Direction and magnitude of blur (toward sun)
        float4 packedUV01 : TEXCOORD2;      // Packed UV coordinates for samples 0-1 (xy=sample0, zw=sample1)
        float4 packedUV23 : TEXCOORD3;      // Packed UV coordinates for samples 2-3 (xy=sample2, zw=sample3)
        float4 packedUV45 : TEXCOORD4;      // Packed UV coordinates for samples 4-5 (xy=sample4, zw=sample5)
    };

    // Texture samplers - these reference the textures passed in from C# code
    sampler2D _MainTex;                    // Main scene color buffer
    sampler2D _ColorBuffer;                // Processed sun ray color data  
    sampler2D _Skybox;                     // Sky-only rendering for occlusion detection
    sampler2D_float _CameraDepthTexture;   // Scene depth buffer for occlusion

    // Uniform parameters set by C# code - these control the sun shaft appearance
    uniform half4 _SunThreshold;    // RGB threshold for what counts as "sun" in skybox (brightness cutoff)
    uniform half4 _SunColor;        // Color tint applied to sun rays
    uniform half4 _BlurRadius4;     // Controls how far the radial blur extends (ray length)
    uniform half4 _SunPosition;     // Screen space position of sun (xy=position, w=falloff radius)
    uniform half4 _MainTex_TexelSize; // Texture size info for UV correction calculations	

    // Threshold constants for occlusion detection
    #define DEPTH_THRESHOLD 0.99f       // Objects closer than this depth value block sun rays
    #define LUMINANCE_THRESHOLD 0.2f    // Luminance difference threshold for sky occlusion detection

    /*
     * Standard vertex shader for most passes
     * Transforms vertex to clip space and sets up UV coordinates with platform correction
     */
    v2f vert( appdata_img v ) {
        v2f o;
        o.pos = UnityObjectToClipPos(v.vertex);
        o.uv = v.texcoord.xy;
        
        // Handle different UV coordinate systems (some platforms have UV origin at top)
        #if UNITY_UV_STARTS_AT_TOP
        o.uv1 = v.texcoord.xy;
        if (_MainTex_TexelSize.y < 0)
            o.uv1.y = 1-o.uv1.y;
        #endif

        return o;
    }

    /*
     * Consolidated UV coordinate helper function
     * Different platforms handle UV coordinates differently, this ensures we use the right ones
     * All UV sampling goes through this function for consistency
     */
    float2 GetCorrectUV(v2f i) {
        #if UNITY_UV_STARTS_AT_TOP
        return i.uv1.xy;    // Use corrected coordinates on platforms with top-left UV origin
        #else
        return i.uv.xy;     // Use standard coordinates on platforms with bottom-left UV origin
        #endif
    }
    
    /*
     * Calculate distance-based falloff from sun position
     * Returns how strongly this pixel should be affected by sun rays (0 = no effect, 1 = full effect)
     * Uses _SunPosition.w as the falloff radius - rays get weaker as you move away from sun center
     */
    half GetSunDistance(v2f i) {
        half2 correctUV = GetCorrectUV(i);
        half2 sunOffset = _SunPosition.xy - correctUV;  // Vector from pixel to sun position
        return saturate (_SunPosition.w - length (sunOffset));  // Saturated distance falloff
    }

    /*
     * PASS 1: Screen Blend Fragment Shader
     * Composites sun rays with the main scene using screen blend mode
     * Screen blend: result = 1 - (1-A) * (1-B) where A=scene, B=rays
     * This creates a brightening effect that never fully saturates to white
     */
    half4 fragScreen(v2f i) : SV_Target { 
        half4 colorA = tex2D (_MainTex, i.uv.xy);           // Original scene color
        half4 colorB = tex2D (_ColorBuffer, GetCorrectUV(i)); // Sun ray color data
        half4 depthMask = saturate (colorB * _SunColor);     // Apply sun color tint and clamp

        // Screen blend formula: brightens scene without over-saturation
        return 1.0f - (1.0f-colorA) * (1.0f-depthMask);
    }

    /*
     * PASS 5: Additive Blend Fragment Shader  
     * Alternative compositing method - simply adds sun ray color to scene
     * This can create brighter, more saturated results than screen blend
     */
    half4 fragAdd(v2f i) : SV_Target { 
        half4 colorA = tex2D (_MainTex, i.uv.xy);           // Original scene color
        half4 colorB = tex2D (_ColorBuffer, GetCorrectUV(i)); // Sun ray color data
        half4 depthMask = saturate (colorB * _SunColor);     // Apply sun color tint and clamp

        return colorA + depthMask;  // Simple additive blend
    }
    
    /*
     * PASS 2: Radial Blur Vertex Shader (PERFORMANCE OPTIMIZED)
     * This is where the magic happens - creates the "ray" effect by precalculating sample positions
     * 
     * Key optimization: Instead of calculating sample positions in the fragment shader
     * (which runs millions of times), we do it here in the vertex shader (runs much less)
     * This moves heavy computation from fragment to vertex, dramatically improving performance
     */
    v2f_radial vert_radial( appdata_img v ) {
        v2f_radial o;
        o.pos = UnityObjectToClipPos(v.vertex);
        o.uv.xy = v.texcoord.xy;
        
        // Calculate the direction from this pixel toward the sun position
        // This vector defines the direction of our blur samples (the "ray" direction)
        half2 toSunVector = _SunPosition.xy - v.texcoord.xy;
        o.blurVector = toSunVector * _BlurRadius4.xy;  // Scale by blur radius setting
        
        // CRITICAL OPTIMIZATION: Precalculate all 6 sample UV coordinates here
        // Instead of doing this math 6 times per pixel in fragment shader,
        // we do it once per vertex and interpolate across triangle faces
        half2 baseUV = v.texcoord.xy;
        o.packedUV01.xy = baseUV;                        // Sample 0: starting position (no offset)
        o.packedUV01.zw = baseUV + o.blurVector;         // Sample 1: 1x blur distance toward sun
        o.packedUV23.xy = baseUV + o.blurVector * 2.0f;  // Sample 2: 2x blur distance toward sun  
        o.packedUV23.zw = baseUV + o.blurVector * 3.0f;  // Sample 3: 3x blur distance toward sun
        o.packedUV45.xy = baseUV + o.blurVector * 4.0f;  // Sample 4: 4x blur distance toward sun
        o.packedUV45.zw = baseUV + o.blurVector * 5.0f;  // Sample 5: 5x blur distance toward sun

        return o; 
    }

    /*
     * PASS 2: Radial Blur Fragment Shader (PERFORMANCE OPTIMIZED)
     * Creates the "sun ray" effect by sampling texture along a line toward the sun
     * 
     * Traditional approach: Use a loop to calculate and sample multiple UV coordinates
     * Optimized approach: All UV coordinates were precalculated in vertex shader,
     * so we just do 6 texture samples and average the results
     * 
     * This creates a directional blur effect that simulates light rays streaming
     * from the sun position toward each pixel
     */
    half4 frag_radial(v2f_radial i) : SV_Target 
    {
        half4 color;
        
        // Sample the texture at 6 precalculated positions along the ray to the sun
        // These coordinates were calculated in vert_radial and interpolated by GPU
        color  = tex2D(_MainTex, i.packedUV01.xy); // Sample 0: starting position
        color += tex2D(_MainTex, i.packedUV01.zw); // Sample 1: 1/6 of way to sun
        color += tex2D(_MainTex, i.packedUV23.xy); // Sample 2: 2/6 of way to sun
        color += tex2D(_MainTex, i.packedUV23.zw); // Sample 3: 3/6 of way to sun
        color += tex2D(_MainTex, i.packedUV45.xy); // Sample 4: 4/6 of way to sun
        color += tex2D(_MainTex, i.packedUV45.zw); // Sample 5: 5/6 of way to sun

        // Average the samples: divide by 6 (0.16666667 = 1/6)
        // This creates a smooth blur along the ray direction
        return color * 0.16666667f;
    }
    
    /*
     * Helper function: Extract sun contribution from skybox
     * Determines how much a skybox pixel contributes to sun rays
     * Only pixels brighter than _SunThreshold are considered "sun"
     */
    half TransformColor (half4 skyboxValue) {
        // Threshold the RGB channels - only keep values above sun threshold
        half3 thresholded = saturate(skyboxValue.rgb - _SunThreshold.rgb);

        // Sum the RGB components to get overall sun contribution
        // This converts the color to a single intensity value
        return thresholded.r + thresholded.g + thresholded.b;
    }

    /*
     * PASS 3: Depth-Based Occlusion Fragment Shader
     * Determines which pixels should receive sun rays based on scene depth
     * Objects in the scene block sunlight from reaching pixels behind them
     * 
     * Key insight: Far away pixels (sky) receive full sun rays,
     * close pixels (blocked by objects) receive no rays
     */
    half4 frag_depth (v2f i) : SV_Target {
        // Sample the depth buffer to see how far away this pixel is
        float depthSample = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, GetCorrectUV(i));
        half4 tex = tex2D (_MainTex, i.uv.xy);  // Sample the scene color

        // Convert depth to linear 0-1 range (0=near camera, 1=far/sky)
        half linearDepth = Linear01Depth (depthSample);

        // Calculate sun influence based on distance from sun center
        half dist = GetSunDistance(i);

        // Determine occlusion: pixels far enough from camera receive sun rays
        // step(a,b) returns 1 if b >= a, otherwise 0 (branchless GPU-friendly operation)
        half depthMask = step(DEPTH_THRESHOLD, linearDepth);
        
        // Combine: sun intensity * distance falloff * occlusion mask
        half4 outColor = TransformColor (tex) * dist * depthMask;

        return outColor;
    }

    /*
     * PASS 4: Sky-Based Occlusion Fragment Shader  
     * Alternative occlusion method that compares scene rendering with sky-only rendering
     * If a pixel looks very different between scene and sky, it's probably blocked by an object
     * 
     * This method works well when you have a clear sky and distinct objects
     * More complex than depth-based but can handle transparent objects better
     */
    half4 frag_nodepth (v2f i) : SV_Target {
        half4 sky = (tex2D (_Skybox, GetCorrectUV(i)));  // Sky-only rendering
        half4 tex = (tex2D (_MainTex, i.uv.xy));         // Full scene rendering

        // Calculate sun influence based on distance from sun center  
        half dist = GetSunDistance(i);

        // Compare sky vs scene: if they're very different, pixel is occluded
        // Luminance() converts RGB to grayscale intensity for comparison
        half luminanceDiff = Luminance(abs(sky.rgb - tex.rgb));
        
        // Determine visibility: pixels with small luminance differences are unoccluded
        // step(a,b) returns 1 if b >= a, we want 1 when diff <= threshold (branchless operation)
        half occlusionMask = step(luminanceDiff, LUMINANCE_THRESHOLD);
        
        // Combine: sun intensity from sky * distance falloff * occlusion mask
        half4 outColor = TransformColor (sky) * dist * occlusionMask;

        return outColor;
    }

    ENDCG

    /*
     * Multi-Pass Rendering Setup
     * ==========================
     * 
     * This shader defines 5 different passes that work together to create sun shafts.
     * The C# code chooses which passes to use and in what order based on the effect settings.
     * 
     * Each pass is optimized for GPU performance:
     * - ZTest Always: Don't depth test (we're doing screen-space effects)
     * - Cull Off: Render both front and back faces (full-screen quads)  
     * - ZWrite Off: Don't write to depth buffer (preserve existing depth)
     */

    Subshader {

     /*
      * PASS 1: Screen Blend Composite
      * Combines sun rays with the main scene using screen blend mode
      * Screen blend creates realistic light brightening without over-saturation
      */
     Pass {
          ZTest Always Cull Off ZWrite Off

          CGPROGRAM

          #pragma vertex vert          // Use standard vertex shader
          #pragma fragment fragScreen  // Use screen blend fragment shader
          
          ENDCG
      }

     /*
      * PASS 2: Radial Blur  
      * Creates the directional "ray" effect by blurring toward the sun position
      * This is the core pass that generates the sun shaft appearance
      */
     Pass {
          ZTest Always Cull Off ZWrite Off

          CGPROGRAM

          #pragma vertex vert_radial   // Use optimized radial blur vertex shader
          #pragma fragment frag_radial // Use radial blur fragment shader

          ENDCG
      }
      
      /*
       * PASS 3: Depth-Based Occlusion
       * Masks sun rays based on scene depth - objects block sunlight  
       * Fast and reliable occlusion method using the depth buffer
       */
      Pass {
          ZTest Always Cull Off ZWrite Off

          CGPROGRAM

          #pragma vertex vert       // Use standard vertex shader
          #pragma fragment frag_depth // Use depth occlusion fragment shader

          ENDCG
      }
      
      /*
       * PASS 4: Sky-Based Occlusion
       * Alternative occlusion method comparing scene vs sky-only rendering
       * More complex but handles transparent objects and complex lighting
       */
      Pass {
          ZTest Always Cull Off ZWrite Off

          CGPROGRAM
          
          #pragma vertex vert         // Use standard vertex shader
          #pragma fragment frag_nodepth // Use sky occlusion fragment shader

          ENDCG
      } 
      
      /*
       * PASS 5: Additive Blend Composite
       * Alternative to screen blend - simply adds sun ray color to scene
       * Can create more intense, saturated sun ray effects
       */
      Pass {
          ZTest Always Cull Off ZWrite Off

          CGPROGRAM

          #pragma vertex vert      // Use standard vertex shader
          #pragma fragment fragAdd // Use additive blend fragment shader

          ENDCG
      } 
    }

    /*
     * No Fallback
     * This shader is designed specifically for the platform and settings it targets.
     * If it can't run, the effect simply won't appear rather than falling back to 
     * a potentially broken or much slower alternative.
     */
    Fallback off
}
