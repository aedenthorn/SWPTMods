using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace AdvancedGraphicsSettings
{
    public class VolumeProfileData
    {

        public bool depthOfField_active;
        public float depthOfField_nearFocusStart;
        public float depthOfField_nearFocusEnd;
        public float depthOfField_farFocusStart;
        public float depthOfField_farFocusEnd;

        public bool vignette_active;
        public Color vignette_color;
        public float vignette_intensity;
        public VignetteMode vignette_mode;
        public float vignette_opacity;
        public bool vignette_rounded;
        public float vignette_roundness;
        public float vignette_smoothness;

        public bool exposure_active;
        public AdaptationMode exposure_adaptationMode;
        public float exposure_adaptationSpeedDarkToLight;
        public float exposure_adaptationSpeedLightToDark;
        public float exposure_compensation;
        public float exposure_fixedExposure;
        public float exposure_limitMax;
        public MeteringMode exposure_meteringMode;
        public ExposureMode exposure_mode;

        public bool whiteBalance_active;
        public float whiteBalance_temperature;
        public float whiteBalance_tint;

        public bool chromaticAberration_active;
        public float chromaticAberration_intensity;
        public int chromaticAberration_quality;

        public bool bloom_active;
        public bool bloom_anamorphic;
        public float bloom_dirtIntensity;
        public float bloom_intensity;
        public int bloom_quality;
        public float bloom_threshold;
        public Color bloom_tint;

        public bool splitToning_active;
        public float splitToning_balance;
        public Color splitToning_highlights;
        public Color splitToning_shadows;

        public bool fog_active;
        public Color fog_albedo;
        public float fog_anisotropy;
        public float fog_baseHeight;
        public Color fog_color;
        public FogColorMode fog_colorMode;
        public float fog_depthExtent;
        public bool fog_enabled;
        public bool fog_enableVolumetricFog;
        public bool fog_filter;
        public float fog_globalLightProbeDimmer;
        public float fog_maxFogDistance;
        public float fog_maximumHeight;
        public float fog_meanFreePath;
        public float fog_mipFogFar;
        public float fog_mipFogMaxMip;
        public float fog_mipFogNear;
        public float fog_sliceDistributionUniformity;
        public Color fog_tint;

        public bool indirectLightingController_active;
        public float indirectLightingController_indirectDiffuseIntensity;
        public LightLayerEnum indirectLightingController_indirectDiffuseLightingLayers;
        public float indirectLightingController_indirectSpecularIntensity;
        public LightLayerEnum indirectLightingController_reflectionLightingLayers;
        public float indirectLightingController_reflectionLightingMultiplier;

        public bool visualEnvironment_active;
        public SkyAmbientMode visualEnvironment_skyAmbientMode;
        public int visualEnvironment_skyType;

        public bool ambientOcclusion_active;
        public float ambientOcclusion_blurSharpness;
        public bool ambientOcclusion_denoise;
        public float ambientOcclusion_denoiserRadius;
        public int ambientOcclusion_directionCount;
        public float ambientOcclusion_directLightingStrength;
        public float ambientOcclusion_ghostingReduction;
        public float ambientOcclusion_intensity;
        public int ambientOcclusion_quality;
        public float ambientOcclusion_radius;
        public float ambientOcclusion_rayLength;
        public bool ambientOcclusion_rayTracing;
        public int ambientOcclusion_sampleCount;
        public bool ambientOcclusion_temporalAccumulation;

        public bool gradientSky_active;
        public Color gradientSky_bottom;
        public float gradientSky_desiredLuxValue;
        public float gradientSky_exposure;
        public float gradientSky_gradientDiffusion;
        public bool gradientSky_includeSunInBaking;
        public Color gradientSky_middle;
        public float gradientSky_multiplier;
        public float gradientSky_rotation;
        public SkyIntensityMode gradientSky_skyIntensityMode;
        public Color gradientSky_top;
        public EnvironmentUpdateMode gradientSky_updateMode;
        public float gradientSky_updatePeriod;
        public Vector3 gradientSky_upperHemisphereLuxColor;
        public float gradientSky_upperHemisphereLuxValue;

        public bool HDRISky_active;
        public BackplateType HDRISky_backplateType;
        public float HDRISky_blendAmount;
        public float HDRISky_desiredLuxValue;
        public bool HDRISky_dirLightShadow;
        public bool HDRISky_enableBackplate;
        public float HDRISky_exposure;
        public float HDRISky_groundLevel;
        public bool HDRISky_includeSunInBaking;
        public float HDRISky_multiplier;
        public float HDRISky_plateRotation;
        public Vector2 HDRISky_plateTexOffset;
        public float HDRISky_plateTexRotation;
        public bool HDRISky_pointLightShadow;
        public float HDRISky_projectionDistance;
        public bool HDRISky_rectLightShadow;
        public float HDRISky_rotation;
        public Vector2 HDRISky_scale;
        public Color HDRISky_shadowTint;
        public SkyIntensityMode HDRISky_skyIntensityMode;
        public EnvironmentUpdateMode HDRISky_updateMode;
        public float HDRISky_updatePeriod;
        public Vector3 HDRISky_upperHemisphereLuxColor;
        public float HDRISky_upperHemisphereLuxValue;

        public bool colorAdjustments_active;
        public Color colorAdjustments_colorFilter;
        public float colorAdjustments_contrast;
        public float colorAdjustments_hueShift;
        public float colorAdjustments_postExposure;
        public float colorAdjustments_saturation;

        public VolumeProfileData(VolumeProfile profile)
        {
            for (int i = 0; i < profile.components.Count; i++)
            {
                if (profile.components[i] is DepthOfField)
                {
                    DepthOfField c = (DepthOfField)profile.components[i];
                    depthOfField_active = c.active;
                    depthOfField_nearFocusStart = c.nearFocusStart.value;
                    depthOfField_nearFocusEnd = c.nearFocusEnd.value;
                    depthOfField_farFocusStart = c.farFocusStart.value;
                    depthOfField_farFocusEnd = c.farFocusEnd.value;

                }
                else if (profile.components[i] is Vignette)
                {
                    Vignette c = (Vignette)profile.components[i];
                    vignette_active = c.active;
                    vignette_color = c.color.value;
                    vignette_intensity = c.intensity.value;
                    vignette_mode = c.mode.value;
                    vignette_opacity = c.opacity.value;
                    vignette_rounded = c.rounded.value;
                    vignette_roundness = c.roundness.value;
                    vignette_smoothness = c.smoothness.value;
                }
                else if (profile.components[i] is Exposure)
                {
                    Exposure c = (Exposure)profile.components[i];
                    exposure_active = c.active;
                    exposure_adaptationMode = c.adaptationMode.value;
                    exposure_adaptationSpeedDarkToLight = c.adaptationSpeedDarkToLight.value;
                    exposure_adaptationSpeedLightToDark = c.adaptationSpeedLightToDark.value;
                    exposure_compensation = c.compensation.value;
                    exposure_fixedExposure = c.fixedExposure.value;
                    exposure_limitMax = c.limitMax.value;
                    exposure_meteringMode = c.meteringMode.value;
                    exposure_mode = c.mode.value;
                }
                else if (profile.components[i] is WhiteBalance)
                {
                    WhiteBalance c = (WhiteBalance)profile.components[i];
                    whiteBalance_active = c.active;
                    whiteBalance_temperature = c.temperature.value;
                    whiteBalance_tint = c.tint.value;
                }
                else if (profile.components[i] is ChromaticAberration)
                {
                    ChromaticAberration c = (ChromaticAberration)profile.components[i];
                    chromaticAberration_active = c.active;
                    chromaticAberration_intensity = c.intensity.value;
                    chromaticAberration_quality = c.quality.value;
                }
                else if (profile.components[i] is Bloom)
                {
                    Bloom c = (Bloom)profile.components[i];
                    bloom_active = c.active;
                    bloom_anamorphic = c.anamorphic.value;
                    bloom_dirtIntensity = c.dirtIntensity.value;
                    bloom_intensity = c.intensity.value;
                    bloom_quality = c.quality.value;
                    bloom_threshold = c.threshold.value;
                    bloom_tint = c.tint.value;
                }
                else if (profile.components[i] is SplitToning)
                {
                    SplitToning c = (SplitToning)profile.components[i];
                    splitToning_active = c.active;
                    splitToning_balance = c.balance.value;
                    splitToning_highlights = c.highlights.value;
                    splitToning_shadows = c.shadows.value;
                }
                else if (profile.components[i] is Fog)
                {
                    Fog c = (Fog)profile.components[i];
                    fog_active = c.active;
                    fog_albedo = c.albedo.value;
                    fog_anisotropy = c.anisotropy.value;
                    fog_baseHeight = c.baseHeight.value;
                    fog_color = c.color.value;
                    fog_colorMode = c.colorMode.value;
                    fog_depthExtent = c.depthExtent.value;
                    fog_enabled = c.enabled.value;
                    fog_enableVolumetricFog = c.enableVolumetricFog.value;
                    fog_filter = c.filter.value;
                    fog_globalLightProbeDimmer = c.globalLightProbeDimmer.value;
                    fog_maxFogDistance = c.maxFogDistance.value;
                    fog_maximumHeight = c.maximumHeight.value;
                    fog_meanFreePath = c.meanFreePath.value;
                    fog_mipFogFar = c.mipFogFar.value;
                    fog_mipFogMaxMip = c.mipFogMaxMip.value;
                    fog_mipFogNear = c.mipFogNear.value;
                    fog_sliceDistributionUniformity = c.sliceDistributionUniformity.value;
                    fog_tint = c.tint.value;
                }
                else if (profile.components[i] is IndirectLightingController)
                {
                    IndirectLightingController c = (IndirectLightingController)profile.components[i];
                    indirectLightingController_active = c.active;
                    indirectLightingController_indirectDiffuseIntensity = c.indirectDiffuseIntensity.value;
                    indirectLightingController_indirectDiffuseLightingLayers = c.indirectDiffuseLightingLayers.value;
                    indirectLightingController_indirectSpecularIntensity = c.indirectSpecularIntensity.value;
                    indirectLightingController_reflectionLightingLayers = c.reflectionLightingLayers.value;
                    indirectLightingController_reflectionLightingMultiplier = c.reflectionLightingMultiplier.value;
                }
                else if (profile.components[i] is VisualEnvironment)
                {
                    VisualEnvironment c = (VisualEnvironment)profile.components[i];
                    visualEnvironment_active = c.active;
                    visualEnvironment_skyAmbientMode = c.skyAmbientMode.value;
                    visualEnvironment_skyType = c.skyType.value;
                }
                else if (profile.components[i] is AmbientOcclusion)
                {
                    AmbientOcclusion c = (AmbientOcclusion)profile.components[i];
                    ambientOcclusion_active = c.active;
                    ambientOcclusion_blurSharpness = c.blurSharpness.value;
                    ambientOcclusion_denoise = c.denoise.value;
                    ambientOcclusion_denoiserRadius = c.denoiserRadius.value;
                    ambientOcclusion_directionCount = c.directionCount;
                    ambientOcclusion_directLightingStrength = c.directLightingStrength.value;
                    ambientOcclusion_ghostingReduction = c.ghostingReduction.value;
                    ambientOcclusion_intensity = c.intensity.value;
                    ambientOcclusion_quality = c.quality.value;
                    ambientOcclusion_radius = c.radius.value;
                    ambientOcclusion_rayLength = c.rayLength.value;
                    ambientOcclusion_rayTracing = c.rayTracing.value;
                    ambientOcclusion_sampleCount = c.sampleCount.value;
                    ambientOcclusion_temporalAccumulation = c.temporalAccumulation.value;
                }
                else if (profile.components[i] is GradientSky)
                {
                    GradientSky c = (GradientSky)profile.components[i];
                    gradientSky_active = c.active;
                    gradientSky_bottom = c.bottom.value;
                    gradientSky_desiredLuxValue = c.desiredLuxValue.value;
                    gradientSky_exposure = c.exposure.value;
                    gradientSky_gradientDiffusion = c.gradientDiffusion.value;
                    gradientSky_includeSunInBaking = c.includeSunInBaking.value;
                    gradientSky_middle = c.middle.value;
                    gradientSky_multiplier = c.multiplier.value;
                    gradientSky_rotation = c.rotation.value;
                    gradientSky_skyIntensityMode = c.skyIntensityMode.value;
                    gradientSky_top = c.top.value;
                    gradientSky_updateMode = c.updateMode.value;
                    gradientSky_updatePeriod = c.updatePeriod.value;
                    gradientSky_upperHemisphereLuxColor = c.upperHemisphereLuxColor.value;
                    gradientSky_upperHemisphereLuxValue = c.upperHemisphereLuxValue.value;
                }
                else if (profile.components[i] is HDRISky)
                {
                    HDRISky c = (HDRISky)profile.components[i];
                    HDRISky_active = c.active;
                    HDRISky_backplateType = c.backplateType.value;
                    HDRISky_blendAmount = c.blendAmount.value;
                    HDRISky_desiredLuxValue = c.desiredLuxValue.value;
                    HDRISky_dirLightShadow = c.dirLightShadow.value;
                    HDRISky_enableBackplate = c.enableBackplate.value;
                    HDRISky_exposure = c.exposure.value;
                    HDRISky_groundLevel = c.groundLevel.value;
                    HDRISky_includeSunInBaking = c.includeSunInBaking.value;
                    HDRISky_multiplier = c.multiplier.value;
                    HDRISky_plateRotation = c.plateRotation.value;
                    HDRISky_plateTexOffset = c.plateTexOffset.value;
                    HDRISky_plateTexRotation = c.plateTexRotation.value;
                    HDRISky_pointLightShadow = c.pointLightShadow.value;
                    HDRISky_projectionDistance = c.projectionDistance.value;
                    HDRISky_rectLightShadow = c.rectLightShadow.value;
                    HDRISky_rotation = c.rotation.value;
                    HDRISky_scale = c.scale.value;
                    HDRISky_shadowTint = c.shadowTint.value;
                    HDRISky_skyIntensityMode = c.skyIntensityMode.value;
                    HDRISky_updateMode = c.updateMode.value;
                    HDRISky_updatePeriod = c.updatePeriod.value;
                    HDRISky_upperHemisphereLuxColor = c.upperHemisphereLuxColor.value;
                    HDRISky_upperHemisphereLuxValue = c.upperHemisphereLuxValue.value;
                }
                else if (profile.components[i] is ColorAdjustments)
                {
                    ColorAdjustments c = (ColorAdjustments)profile.components[i];
                    colorAdjustments_active = c.active;
                    colorAdjustments_colorFilter = c.colorFilter.value;
                    colorAdjustments_contrast = c.contrast.value;
                    colorAdjustments_hueShift = c.hueShift.value;
                    colorAdjustments_postExposure = c.postExposure.value;
                    colorAdjustments_saturation = c.saturation.value;
                }
            }
        }
    }
}