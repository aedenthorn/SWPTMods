using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace AdvancedGraphicsSettings
{
    public class VolumeProfileData
    {

        public bool depthOfField_has;
        public bool depthOfField_active;
        public float depthOfField_nearFocusStart;
        public float depthOfField_nearFocusEnd;
        public float depthOfField_farFocusStart;
        public float depthOfField_farFocusEnd;

        public bool vignette_has;
        public bool vignette_active;
        public Color vignette_color;
        public float vignette_intensity;
        public VignetteMode vignette_mode;
        public float vignette_opacity;
        public bool vignette_rounded;
        public float vignette_roundness;
        public float vignette_smoothness;

        public bool exposure_has;
        public bool exposure_active;
        public AdaptationMode exposure_adaptationMode;
        public float exposure_adaptationSpeedDarkToLight;
        public float exposure_adaptationSpeedLightToDark;
        public float exposure_compensation;
        public float exposure_fixedExposure;
        public float exposure_limitMax;
        public MeteringMode exposure_meteringMode;
        public ExposureMode exposure_mode;

        public bool whiteBalance_has;
        public bool whiteBalance_active;
        public float whiteBalance_temperature;
        public float whiteBalance_tint;

        public bool chromaticAberration_active;
        public float chromaticAberration_intensity;
        public int chromaticAberration_quality;

        public bool bloom_has;
        public bool bloom_active;
        public bool bloom_anamorphic;
        public float bloom_dirtIntensity;
        public float bloom_intensity;
        public int bloom_quality;
        public float bloom_threshold;
        public Color bloom_tint;

        public bool splitToning_has;
        public bool splitToning_active;
        public float splitToning_balance;
        public Color splitToning_highlights;
        public Color splitToning_shadows;

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
            }
        }
    }
}