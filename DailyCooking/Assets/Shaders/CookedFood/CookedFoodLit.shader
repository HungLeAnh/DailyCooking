Shader "Cooked Food/CookedFoodLit"
{
    // URP/Lit + an additive procedural "cooking" effect.
    //
    // The Ultimate Food Pack collection ships most food items as RAW meshes.
    // Swap any raw food material that uses URP/Lit onto this shader: because the
    // property names (_BaseMap, _BaseColor, _BumpMap, ...) match URP/Lit exactly,
    // Unity keeps every texture/tint/scale when you switch shaders. The _Cook*
    // properties then bake a Maillard browning + sear/caramel look into the albedo,
    // so raw food reads as "cooked" without needing separate cooked meshes/textures.
    //
    // Simplified from the original 3-file setup: DOTS instancing, parallax, detail
    // maps, clear coat and rendering layers were removed. The remaining passes
    // (Forward, GBuffer, ShadowCaster, DepthOnly, DepthNormals, Meta) reuse the
    // standard URP pass files.

    Properties
    {
        // --- Specular vs Metallic workflow ---
        _WorkflowMode("WorkflowMode", Float) = 1.0

        [MainTexture] _BaseMap("Albedo", 2D) = "white" {}
        [MainColor] _BaseColor("Color", Color) = (1,1,1,1)

        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5
        _SmoothnessTextureChannel("Smoothness texture channel", Float) = 0

        _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        _MetallicGlossMap("Metallic", 2D) = "white" {}

        _SpecColor("Specular", Color) = (0.2, 0.2, 0.2)
        _SpecGlossMap("Specular", 2D) = "white" {}

        [ToggleOff] _SpecularHighlights("Specular Highlights", Float) = 1.0
        [ToggleOff] _EnvironmentReflections("Environment Reflections", Float) = 1.0

        _BumpScale("Scale", Float) = 1.0
        _BumpMap("Normal Map", 2D) = "bump" {}

        _Parallax("Scale", Range(0.005, 0.08)) = 0.005
        _ParallaxMap("Height Map", 2D) = "black" {}

        _OcclusionStrength("Strength", Range(0.0, 1.0)) = 1.0
        _OcclusionMap("Occlusion", 2D) = "white" {}

        [HDR] _EmissionColor("Color", Color) = (0,0,0)
        _EmissionMap("Emission", 2D) = "white" {}

        _DetailMask("Detail Mask", 2D) = "white" {}
        _DetailAlbedoMapScale("Scale", Range(0.0, 2.0)) = 1.0
        _DetailAlbedoMap("Detail Albedo x2", 2D) = "linearGrey" {}
        _DetailNormalMapScale("Scale", Range(0.0, 2.0)) = 1.0
        [Normal] _DetailNormalMap("Normal Map", 2D) = "bump" {}

        [HideInInspector] _ClearCoatMask("_ClearCoatMask", Float) = 0.0
        [HideInInspector] _ClearCoatSmoothness("_ClearCoatSmoothness", Float) = 0.0

        // --- Blending state ---
        _Surface("__surface", Float) = 0.0
        _Blend("__blend", Float) = 0.0
        _Cull("__cull", Float) = 2.0
        [ToggleUI] _AlphaClip("__clip", Float) = 0.0
        [HideInInspector] _SrcBlend("__src", Float) = 1.0
        [HideInInspector] _DstBlend("__dst", Float) = 0.0
        [HideInInspector] _SrcBlendAlpha("__srcA", Float) = 1.0
        [HideInInspector] _DstBlendAlpha("__dstA", Float) = 0.0
        [HideInInspector] _ZWrite("__zw", Float) = 1.0
        [HideInInspector] _BlendModePreserveSpecular("_BlendModePreserveSpecular", Float) = 1.0
        [HideInInspector] _AlphaToMask("__alphaToMask", Float) = 0.0
        [HideInInspector] _AddPrecomputedVelocity("_AddPrecomputedVelocity", Float) = 0.0
        [HideInInspector] _XRMotionVectorsPass("_XRMotionVectorsPass", Float) = 1.0

        [ToggleUI] _ReceiveShadows("Receive Shadows", Float) = 1.0
        _QueueOffset("Queue offset", Float) = 0.0

        // --- ObsoleteProperties ---
        [HideInInspector] _MainTex("BaseMap", 2D) = "white" {}
        [HideInInspector] _Color("Base Color", Color) = (1, 1, 1, 1)
        [HideInInspector] _GlossMapScale("Smoothness", Float) = 0.0
        [HideInInspector] _Glossiness("Smoothness", Float) = 0.0
        [HideInInspector] _GlossyReflections("EnvironmentReflections", Float) = 0.0

        [HideInInspector][NoScaleOffset]unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset]unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset]unity_ShadowMasks("unity_ShadowMasks", 2DArray) = "" {}

        // --- COOKING EFFECT PARAMETERS ---
        // 0 = raw (no change), 1 = fully cooked.
        _CookAmount("Cook Amount", Range(0.0, 1.0)) = 1.0
        // Warm brown tint applied via the Maillard reaction (multiply onto albedo).
        _CookTint("Cook Tint", Color) = (0.55, 0.36, 0.22, 1.0)
        // Dark sear marks blended into the mid tones.
        _SearedColor("Seared Color", Color) = (0.22, 0.13, 0.08, 1.0)
        // Lighter caramel glaze blended into the highlights.
        _CaramelColor("Caramel Color", Color) = (0.66, 0.45, 0.28, 1.0)
        // Tiling of the procedural sear/caramel noise.
        _CaramelTiling("Caramel Tiling", Range(1.0, 40.0)) = 14.0
        // How much the surface dries out (smoothness drops) while cooking.
        _CookSmoothnessReduction("Cook Smoothness Reduction", Range(0.0, 0.5)) = 0.1
        // How much saturation is lost as the food cooks.
        _CookDesaturation("Cook Desaturation", Range(0.0, 1.0)) = 0.4
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Lit"
            "IgnoreProjector" = "True"
        }
        LOD 300

        // ===================================================================
        // Shared input + cooking effect, inserted at the start of every pass.
        // ===================================================================
        HLSLINCLUDE
            // Use URP's real LitInput.hlsl (cbuffer, textures, InitializeStandardLitSurfaceData).
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"

            // Cooking effect parameters. Declared as plain global uniforms because
            // the UnityPerMaterial cbuffer is owned by LitInput.hlsl and can't be
            // extended. This makes the shader ineligible for the SRP Batcher.
            half  _CookAmount;
            half4 _CookTint;
            half4 _SearedColor;
            half4 _CaramelColor;
            half  _CaramelTiling;
            half  _CookSmoothnessReduction;
            half  _CookDesaturation;

            // Deterministic, tileable value noise (no time dependency).
            float CookedHash21(float2 p)
            {
                p = frac(p * float2(5.792349, 3.141593));
                p += dot(p, p + 45.3213);
                return frac(p.x * p.y);
            }

            float CookedNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = CookedHash21(i);
                float b = CookedHash21(i + float2(1.0, 0.0));
                float c = CookedHash21(i + float2(0.0, 1.0));
                float d = CookedHash21(i + float2(1.0, 1.0));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            // Fractal brown noise in [0,1] with a fixed seed offset per octave.
            float CookedFbm(float2 p)
            {
                float value = 0.0;
                float amplitude = 0.5;
                float2 shift = float2(0.0, 0.0);
                [unroll]
                for (int i = 0; i < 3; i++)
                {
                    value += amplitude * CookedNoise(p - shift);
                    shift += float2(0.733, 1.117) * amplitude;
                    p *= 2.2045;
                    amplitude *= 0.45;
                }
                return saturate(value);
            }

            // Core cooking effect: browns + sears a raw albedo value.
            half3 CookedFoodAlbedo(half3 rawAlbedo, float2 uv)
            {
                half amount = _CookAmount;
                if (amount < 1e-3)
                    return rawAlbedo;

                // 1) Maillard browning: multiply the albedo toward a warm brown tint.
                half3 browned = rawAlbedo * _CookTint.rgb;

                // 2) Mild desaturation as the food "dries out" while cooking.
                half lum = dot(rawAlbedo, half3(0.2126, 0.7152, 0.0722));
                half3 desaturated = lerp(rawAlbedo, lum.xxx, amount * _CookDesaturation);

                // Blend raw -> browned by cook amount.
                half3 cooked = lerp(desaturated, browned, amount);

                // 3) Procedural sear / caramel glaze variation (deterministic, tileable).
                float noise = CookedFbm(uv * _CaramelTiling + float2(17.23, 5.89));
                noise = pow(noise, 1.7);

                // Dark sear marks sitting in the mid tones.
                float sear = smoothstep(0.28, 0.55, noise);
                cooked = lerp(cooked, _SearedColor.rgb, sear * amount);

                // Lighter caramelized glaze on the high tones.
                float glaze = smoothstep(0.66, 0.95, noise);
                cooked = lerp(cooked, _CaramelColor.rgb, glaze * 0.7 * amount);

                return cooked;
            }

            // Called right after InitializeStandardLitSurfaceData in the lit passes.
            void ApplyCookingToSurfaceData(inout SurfaceData surfaceData, float2 uv)
            {
                half amount = _CookAmount;
                if (amount < 1e-3)
                    return;

                surfaceData.albedo = CookedFoodAlbedo(surfaceData.albedo, uv);
                surfaceData.smoothness = max(0.0, surfaceData.smoothness - _CookSmoothnessReduction * amount);
            }
        ENDHLSL

        // ===================================================================
        // Forward pass (injects the cooking effect)
        // ===================================================================
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend[_SrcBlend][_DstBlend], [_SrcBlendAlpha][_DstBlendAlpha]
            ZWrite[_ZWrite]
            Cull[_Cull]
            AlphaToMask[_AlphaToMask]

            HLSLPROGRAM
            #pragma target 3.0

            #pragma vertex LitPassVertex
            #pragma fragment CookedFoodLitPassFragment

            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _RECEIVE_SHADOWS_OFF
            #pragma shader_feature_local_fragment _SURFACE_TYPE_TRANSPARENT
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON
            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local_fragment _METALLICSPECGLOSSMAP
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            #pragma shader_feature_local_fragment _OCCLUSIONMAP
            #pragma shader_feature_local_fragment _SPECULARHIGHLIGHTS_OFF
            #pragma shader_feature_local_fragment _ENVIRONMENTREFLECTIONS_OFF
            #pragma shader_feature_local_fragment _SPECULAR_SETUP

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile_fragment _ DEBUG_DISPLAY
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"

            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitForwardPass.hlsl"

            void CookedFoodLitPassFragment(
                Varyings input
                , out half4 outColor : SV_Target0
            )
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                SurfaceData surfaceData;
                InitializeStandardLitSurfaceData(input.uv, surfaceData);

                // >>> COOKING EFFECT INJECTION <<<
                ApplyCookingToSurfaceData(surfaceData, input.uv);

            #ifdef LOD_FADE_CROSSFADE
                LODFadeCrossFade(input.positionCS);
            #endif

                InputData inputData;
                InitializeInputData(input, surfaceData.normalTS, inputData);
                SETUP_DEBUG_TEXTURE_DATA(inputData, UNDO_TRANSFORM_TEX(input.uv, _BaseMap));

                InitializeBakedGIData(input, inputData);

                half4 color = UniversalFragmentPBR(inputData, surfaceData);
                color.rgb = MixFog(color.rgb, inputData.fogCoord);
                color.a = OutputAlpha(color.a, IsSurfaceTypeTransparent(_Surface));

                outColor = color;
            }
            ENDHLSL
        }

        // ===================================================================
        // Shadow caster pass (position only; identical to URP/Lit)
        // ===================================================================
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull[_Cull]

            HLSLPROGRAM
            #pragma target 2.0

            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #pragma shader_feature_local _ALPHATEST_ON

            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }

        // ===================================================================
        // G-Buffer pass (injects the cooking effect) - used by the Deferred renderer.
        // ===================================================================
        Pass
        {
            Name "GBuffer"
            Tags { "LightMode" = "UniversalGBuffer" }

            ZWrite[_ZWrite]
            ZTest LEqual
            Cull[_Cull]

            HLSLPROGRAM
            #pragma target 4.5
            #pragma exclude_renderers gles3 glcore

            #pragma vertex LitGBufferPassVertex
            #pragma fragment CookedFoodGBufferPassFragment

            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local_fragment _METALLICSPECGLOSSMAP
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            #pragma shader_feature_local_fragment _OCCLUSIONMAP
            #pragma shader_feature_local_fragment _SPECULARHIGHLIGHTS_OFF
            #pragma shader_feature_local_fragment _ENVIRONMENTREFLECTIONS_OFF
            #pragma shader_feature_local_fragment _SPECULAR_SETUP
            #pragma shader_feature_local _RECEIVE_SHADOWS_OFF

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX

            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_IRRADIANCE
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"

            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitGBufferPass.hlsl"

            GBufferFragOutput CookedFoodGBufferPassFragment(Varyings input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                SurfaceData surfaceData;
                InitializeStandardLitSurfaceData(input.uv, surfaceData);

                // >>> COOKING EFFECT INJECTION <<<
                ApplyCookingToSurfaceData(surfaceData, input.uv);

            #ifdef LOD_FADE_CROSSFADE
                LODFadeCrossFade(input.positionCS);
            #endif

                InputData inputData;
                InitializeInputData(input, surfaceData.normalTS, inputData);
                SETUP_DEBUG_TEXTURE_DATA(inputData, UNDO_TRANSFORM_TEX(input.uv, _BaseMap));

                InitializeBakedGIData(input, inputData);

                BRDFData brdfData;
                InitializeBRDFData(surfaceData.albedo, surfaceData.metallic, surfaceData.specular, surfaceData.smoothness, surfaceData.alpha, brdfData);

                Light mainLight = GetMainLight(inputData.shadowCoord, inputData.positionWS, inputData.shadowMask);
                MixRealtimeAndBakedGI(mainLight, inputData.normalWS, inputData.bakedGI, inputData.shadowMask);

                half3 color = GlobalIllumination(brdfData, (BRDFData)0, 0,
                                                 inputData.bakedGI, surfaceData.occlusion, inputData.positionWS,
                                                 inputData.normalWS, inputData.viewDirectionWS, inputData.normalizedScreenSpaceUV);

                return PackGBuffersBRDFData(brdfData, inputData, surfaceData.smoothness, surfaceData.emission + color, surfaceData.occlusion);
            }
            ENDHLSL
        }

        // ===================================================================
        // Depth only pass (identical to URP/Lit)
        // ===================================================================
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ZTest LEqual
            ColorMask R
            Cull[_Cull]

            HLSLPROGRAM
            #pragma target 2.0

            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            #pragma shader_feature_local _ALPHATEST_ON

            #pragma multi_compile _ LOD_FADE_CROSSFADE

            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }

        // ===================================================================
        // Depth / normals pass (identical to URP/Lit)
        // ===================================================================
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On
            ZTest LEqual
            Cull[_Cull]

            HLSLPROGRAM
            #pragma target 2.0

            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment

            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _ALPHATEST_ON

            #pragma multi_compile _ LOD_FADE_CROSSFADE

            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitDepthNormalsPass.hlsl"
            ENDHLSL
        }

        // ===================================================================
        // Meta pass (lightmap baking; identical to URP/Lit)
        // ===================================================================
        Pass
        {
            Name "Meta"
            Tags { "LightMode" = "Meta" }

            Cull Off

            HLSLPROGRAM
            #pragma target 2.0

            #pragma vertex UniversalVertexMeta
            #pragma fragment UniversalFragmentMetaLit

            #pragma shader_feature_local_fragment _SPECULAR_SETUP
            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local_fragment _METALLICSPECGLOSSMAP
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            #pragma shader_feature_local_fragment _SPECGLOSSMAP
            #pragma shader_feature EDITOR_VISUALIZATION

            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitMetaPass.hlsl"
            ENDHLSL
        }
    }

    Fallback "Hidden/Universal Render Pipeline/FallbackError"
    CustomEditor "UnityEditor.Rendering.Universal.ShaderGUI.LitShader"
}
