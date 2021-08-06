using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;

namespace AdvancedGraphicsSettings
{
    [BepInPlugin("aedenthorn.AdvancedGraphicsSettings", "Advanced Graphics Settings", "0.1.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<int> nexusID;

        public static ConfigEntry<string> refreshHotkey;
        public static ConfigEntry<string> dumpModKey;

        public static ConfigEntry<AnisotropicFiltering> anisotropicFiltering;
        public static ConfigEntry<int> antiAliasing;
        public static ConfigEntry<int> asyncUploadBufferSize;
        public static ConfigEntry<bool> asyncUploadPersistentBuffer;
        public static ConfigEntry<int> asyncUploadTimeSlice;
        public static ConfigEntry<bool> billboardsFaceCameraPosition;
        public static ConfigEntry<int> lodBias;
        public static ConfigEntry<int> masterTextureLimit;
        public static ConfigEntry<int> maximumLODLevel;
        public static ConfigEntry<int> maxQueuedFrames;
        public static ConfigEntry<int> particleRaycastBudget;
        public static ConfigEntry<int> pixelLightCount;
        public static ConfigEntry<bool> realtimeReflectionProbes;
        public static ConfigEntry<int> resolutionScalingFixedDPIFactor;
        public static ConfigEntry<float> shadowCascade2Split;
        public static ConfigEntry<Vector3> shadowCascade4Split;
        public static ConfigEntry<int> shadowCascades;
        public static ConfigEntry<int> shadowDistance;
        public static ConfigEntry<ShadowmaskMode> shadowmaskMode;
        public static ConfigEntry<int> shadowNearPlaneOffset;
        public static ConfigEntry<ShadowProjection> shadowProjection;
        public static ConfigEntry<ShadowResolution> shadowResolution;
        public static ConfigEntry<ShadowQuality> shadows;
        public static ConfigEntry<SkinWeights> skinWeights;
        public static ConfigEntry<bool> softParticles;
        public static ConfigEntry<bool> softVegetation;
        public static ConfigEntry<bool> streamingMipmapsActive;
        public static ConfigEntry<bool> streamingMipmapsAddAllCameras;
        public static ConfigEntry<int> streamingMipmapsMaxFileIORequests;
        public static ConfigEntry<int> streamingMipmapsMaxLevelReduction;
        public static ConfigEntry<int> streamingMipmapsMemoryBudget;
        public static ConfigEntry<int> vSyncCount;

        public static ConfigEntry<float> depthOfFieldNearBlurStart;
        public static ConfigEntry<float> depthOfFieldFarBlurStart;
        public static ConfigEntry<float> depthOfFieldNearBlurEnd;
        public static ConfigEntry<float> depthOfFieldFarBlurEnd;

        public static ConfigEntry<bool> disableDepthOfField;
        public static ConfigEntry<bool> disableVignette;
        public static ConfigEntry<bool> disableExposure;
        public static ConfigEntry<bool> disableWhiteBalance;
        public static ConfigEntry<bool> disableChromaticAberration;
        public static ConfigEntry<bool> disableBloom;
        public static ConfigEntry<bool> disableSplitToning;
        public static ConfigEntry<bool> disableFog;
        public static ConfigEntry<bool> disableIndirectLightingController;
        public static ConfigEntry<bool> disableAmbientOcclusion;
        public static ConfigEntry<bool> disableGradientSky;
        public static ConfigEntry<bool> disableHDRISky;
        public static ConfigEntry<bool> disableColorAdjustments;

        private static BepInExPlugin context;
        private static string assetPath;
        private static Dictionary<string, VolumeProfileData> volumeProfileDataDict = new Dictionary<string, VolumeProfileData>();

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug.Value)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            context = this;
            modEnabled = Config.Bind("AA General", "Enabled", true, "Enable this mod");
            isDebug = Config.Bind<bool>("AA General", "IsDebug", true, "Enable debug logs");
            nexusID = Config.Bind<int>("General", "NexusID", 57, "Nexus mod ID for updates");

            refreshHotkey = Config.Bind("Options", "RefreshHotkey", "page up", "Hotkey to refresh settings after changing them in-game.");
            dumpModKey = Config.Bind("Options", "DumpModKey", "left shift", "Hotkey to dump volume profiles instead of refreshing.");
            
            disableDepthOfField = Config.Bind<bool>("Global", "disableDepthOfField", false, "Disable depth of field globally");
            disableVignette = Config.Bind<bool>("Global", "disableVignette", false, "Disable vignette globally");
            disableExposure = Config.Bind<bool>("Global", "disableExposure", false, "Disable exposure globally");
            disableWhiteBalance = Config.Bind<bool>("Global", "disableWhiteBalance", false, "Disable white balance globally");
            disableChromaticAberration = Config.Bind<bool>("Global", "disableChromaticAberration", false, "Disable chromatic aberration globally");
            disableBloom = Config.Bind<bool>("Global", "disableBloom", false, "Disable Bloom globally");
            disableSplitToning = Config.Bind<bool>("Global", "disableSplitToning", false, "Disable split toning globally");
            disableFog = Config.Bind<bool>("Global", "disableFog", false, "Disable Fog globally");
            disableIndirectLightingController = Config.Bind<bool>("Global", "disableIndirectLightingController", false, "Disable indirect lighting controller globally");
            disableAmbientOcclusion = Config.Bind<bool>("Global", "disableAmbientOcclusion", false, "Disable ambient occlusion globally");
            disableGradientSky = Config.Bind<bool>("Global", "disableGradientSky", false, "Disable gradient sky globally");
            disableHDRISky = Config.Bind<bool>("Global", "disableHDRISky", false, "Disable HDRI sky globally");
            disableColorAdjustments = Config.Bind<bool>("Global", "disableColorAdjustments", false, "Disable color adjustments globally");

            anisotropicFiltering = Config.Bind("Quality Settings", "anisotropicFiltering", AnisotropicFiltering.ForceEnable, "Global anisotropic filtering mode.");
            antiAliasing = Config.Bind("Quality Settings", "antiAliasing", 0, "Set The AA Filtering option.");
            asyncUploadBufferSize = Config.Bind("Quality Settings", "asyncUploadBufferSize", 4, "Asynchronous texture and mesh data upload provides timesliced async texture and mesh data upload on the render thread with tight control over memory and timeslicing. There are no allocations except for the ones which driver has to do. To read data and upload texture and mesh data, Unity re-uses a ringbuffer whose size can be controlled.Use asyncUploadBufferSize to set the buffer size for asynchronous texture and mesh data uploads. The size is in megabytes. The minimum value is 2 and the maximum value is 512. The buffer resizes automatically to fit the largest texture currently loading. To avoid re-sizing of the buffer, which can incur performance cost, set the value approximately to the size of biggest texture used in the Scene.");
            asyncUploadPersistentBuffer = Config.Bind("Quality Settings", "asyncUploadPersistentBuffer", true, "This flag controls if the async upload pipeline's ring buffer remains allocated when there are no active loading operations. Set this to true, to make the ring buffer allocation persist after all upload operations have completed. If you have issues with excessive memory usage, you can set this to false. This means you reduce the runtime memory footprint, but memory fragmentation can occur. The default value is true.");
            asyncUploadTimeSlice = Config.Bind("Quality Settings", "asyncUploadTimeSlice", 2, "Async texture upload provides timesliced async texture upload on the render thread with tight control over memory and timeslicing. There are no allocations except for the ones which driver has to do. To read data and upload texture data a ringbuffer whose size can be controlled is re-used.Use asyncUploadTimeSlice to set the time-slice in milliseconds for asynchronous texture uploads per frame. Minimum value is 1 and maximum is 33.");
            billboardsFaceCameraPosition = Config.Bind("Quality Settings", "billboardsFaceCameraPosition", true, "If enabled, billboards will face towards camera position rather than camera orientation.");
            lodBias = Config.Bind("Quality Settings", "lodBias", 2, "Global multiplier for the LOD's switching distance.");
            masterTextureLimit = Config.Bind("Quality Settings", "masterTextureLimit", 0, "A texture size limit applied to all textures.");
            maximumLODLevel = Config.Bind("Quality Settings", "maximumLODLevel", 0, "A maximum LOD level. All LOD groups.");
            maxQueuedFrames = Config.Bind("Quality Settings", "maxQueuedFrames", 2, "Maximum number of frames queued up by graphics driver.");
            particleRaycastBudget = Config.Bind("Quality Settings", "particleRaycastBudget", 4096, "Budget for how many ray casts can be performed per frame for approximate collision testing.");
            pixelLightCount = Config.Bind("Quality Settings", "pixelLightCount", 8, "The maximum number of pixel lights that should affect any object.");
            realtimeReflectionProbes = Config.Bind("Quality Settings", "realtimeReflectionProbes", true, "Enables realtime reflection probes.");
            resolutionScalingFixedDPIFactor = Config.Bind("Quality Settings", "resolutionScalingFixedDPIFactor", 1, "In resolution scaling mode, this factor is used to multiply with the target Fixed DPI specified to get the actual Fixed DPI to use for this quality setting.");
            shadowCascade2Split = Config.Bind("Quality Settings", "shadowCascade2Split", 0.3333333f, "The normalized cascade distribution for a 2 cascade setup. The value defines the position of the cascade with respect to Zero.");
            shadowCascade4Split = Config.Bind("Quality Settings", "shadowCascade4Split", new Vector3(0.1f, 0.2f, 0.5f), "The normalized cascade start position for a 4 cascade setup. Each member of the vector defines the normalized position of the coresponding cascade with respect to Zero.");
            shadowCascades = Config.Bind("Quality Settings", "shadowCascades", 4, "Number of cascades to use for directional light shadows.");
            shadowDistance = Config.Bind("Quality Settings", "shadowDistance", 150, "Shadow drawing distance.");
            shadowmaskMode = Config.Bind("Quality Settings", "shadowmaskMode", ShadowmaskMode.Shadowmask, "The rendering mode of Shadowmask.");
            shadowNearPlaneOffset = Config.Bind("Quality Settings", "shadowNearPlaneOffset", 3, "Offset shadow frustum near plane.");
            shadowProjection = Config.Bind("Quality Settings", "shadowProjection", ShadowProjection.StableFit, "Directional light shadow projection.");
            shadowResolution = Config.Bind("Quality Settings", "shadowResolution", ShadowResolution.High, "The default resolution of the shadow maps.");
            shadows = Config.Bind("Quality Settings", "shadows", ShadowQuality.All, "Realtime Shadows type to be used.");
            skinWeights = Config.Bind("Quality Settings", "skinWeights", SkinWeights.TwoBones, "The maximum number of bone weights that can affect a vertex, for all skinned meshes in the project.");
            softParticles = Config.Bind("Quality Settings", "softParticles", true, "Should soft blending be used for particles?");
            softVegetation = Config.Bind("Quality Settings", "softVegetation", true, "Use a two-pass shader for the vegetation in the terrain engine.");
            streamingMipmapsActive = Config.Bind("Quality Settings", "streamingMipmapsActive", true, "Enable automatic streaming of texture mipmap levels based on their distance from all active cameras.");
            streamingMipmapsAddAllCameras = Config.Bind("Quality Settings", "streamingMipmapsAddAllCameras", true, "Process all enabled Cameras for texture streaming (rather than just those with StreamingController components).");
            streamingMipmapsMaxFileIORequests = Config.Bind("Quality Settings", "streamingMipmapsMaxFileIORequests", 1024, "The maximum number of active texture file IO requests from the texture streaming system.");
            streamingMipmapsMaxLevelReduction = Config.Bind("Quality Settings", "streamingMipmapsMaxLevelReduction", 2, "The maximum number of mipmap levels to discard for each texture.");
            streamingMipmapsMemoryBudget = Config.Bind("Quality Settings", "streamingMipmapsMemoryBudget", 512, "The total amount of memory to be used by streaming and non-streaming textures.");
            vSyncCount = Config.Bind("Quality Settings", "vSyncCount", 1, "The VSync Count.");

            if (!modEnabled.Value)
                return;

            assetPath = AedenthornUtils.GetAssetPath(typeof(BepInExPlugin).Namespace);
            if (!Directory.Exists(assetPath))
            {
                Directory.CreateDirectory(assetPath);
            }
            LoadProfiles();

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);


        }

        private void Update()
        {
            if (AedenthornUtils.CheckKeyDown(refreshHotkey.Value))
            {
                Dbgl("Hotkey pressed");

                if (AedenthornUtils.CheckKeyHeld(dumpModKey.Value, true))
                {
                    foreach(Volume volume in FindObjectsOfType<Volume>())
                    {
                        string fileName = SceneManager.GetActiveScene().name + "_" + volume.name + ".json";

                        if (!File.Exists(Path.Combine(assetPath, fileName)))
                        {
                            VolumeProfileData data = new VolumeProfileData(volume.sharedProfile);
                            string json = JsonUtility.ToJson(data, true);
                            File.WriteAllText(Path.Combine(assetPath, fileName), json);
                            Dbgl($"Writing volume profile {volume.name} to file");
                        }
                    }
                }
                else
                {
                    Config.Reload();
                    LoadProfiles();
                    SetGraphicsSettings();
                }

            }
        }
        private static void LoadProfiles()
        {
            volumeProfileDataDict.Clear();
            foreach (string file in Directory.GetFiles(assetPath, "*.json"))
            {
                try
                {
                    volumeProfileDataDict.Add(Path.GetFileNameWithoutExtension(file), JsonUtility.FromJson<VolumeProfileData>(File.ReadAllText(file)));
                }
                catch
                {
                    Dbgl($"Error reading volume profile data for {file}");
                }
            }
        }
        private static void SetGraphicsSettings()
        {
            foreach(Volume volume in FindObjectsOfType<Volume>())
            {
                string key = SceneManager.GetActiveScene().name + "_" + volume.name;
                if (volumeProfileDataDict.ContainsKey(key))
                {
                    Dbgl($"Modifying post processing variables for scene {SceneManager.GetActiveScene().name}, profile {volume.name}");

                    var profile = volume.sharedProfile;
                    var profileData = volumeProfileDataDict[key];
                    for (int i = 0; i < profile.components.Count; i++)
                    {
                        if (IsDisabled(profile.components[i]))
                        {
                            profile.components[i].active = false;
                            continue;
                        }

                        if (profile.components[i] is DepthOfField)
                        {
                            DepthOfField c = (DepthOfField)profile.components[i];
                            c.active = profileData.depthOfField_active;
                            c.nearFocusStart.value = profileData.depthOfField_nearFocusStart;
                            c.nearFocusEnd.value = profileData.depthOfField_nearFocusEnd;
                            c.farFocusStart.value = profileData.depthOfField_farFocusStart;
                            c.farFocusEnd.value = profileData.depthOfField_farFocusEnd;

                        }
                        else if (profile.components[i] is Vignette)
                        {
                            Vignette c = (Vignette)profile.components[i];
                            c.active = profileData.vignette_active;
                            c.color.value = profileData.vignette_color;
                            c.intensity.value = profileData.vignette_intensity;
                            c.mode.value = profileData.vignette_mode;
                            c.opacity.value = profileData.vignette_opacity;
                            c.rounded.value = profileData.vignette_rounded;
                            c.roundness.value = profileData.vignette_roundness;
                            c.smoothness.value = profileData.vignette_smoothness;
                        }
                        else if (profile.components[i] is Exposure)
                        {
                            Exposure c = (Exposure)profile.components[i];
                            c.active = profileData.exposure_active;
                            c.adaptationMode.value = profileData.exposure_adaptationMode;
                            c.adaptationSpeedDarkToLight.value = profileData.exposure_adaptationSpeedDarkToLight;
                            c.adaptationSpeedLightToDark.value = profileData.exposure_adaptationSpeedLightToDark;
                            c.compensation.value = profileData.exposure_compensation;
                            c.fixedExposure.value = profileData.exposure_fixedExposure;
                            c.limitMax.value = profileData.exposure_limitMax;
                            c.meteringMode.value = profileData.exposure_meteringMode;
                            c.mode.value = profileData.exposure_mode;
                        }
                        else if (profile.components[i] is WhiteBalance)
                        {
                            WhiteBalance c = (WhiteBalance)profile.components[i];
                            c.active = profileData.whiteBalance_active;
                            c.temperature.value = profileData.whiteBalance_temperature;
                            c.tint.value = profileData.whiteBalance_tint;
                        }
                        else if (profile.components[i] is ChromaticAberration)
                        {
                            ChromaticAberration c = (ChromaticAberration)profile.components[i];
                            c.active = profileData.chromaticAberration_active;
                            c.intensity.value = profileData.chromaticAberration_intensity;
                            c.quality.value = profileData.chromaticAberration_quality;
                        }
                        else if (profile.components[i] is Bloom)
                        {
                            Bloom c = (Bloom)profile.components[i];
                            c.active = profileData.bloom_active;
                            c.anamorphic.value = profileData.bloom_anamorphic;
                            c.dirtIntensity.value = profileData.bloom_dirtIntensity;
                            c.intensity.value = profileData.bloom_intensity;
                            c.quality.value = profileData.bloom_quality;
                            c.threshold.value = profileData.bloom_threshold;
                            c.tint.value = profileData.bloom_tint;
                        }
                        else if (profile.components[i] is SplitToning)
                        {
                            SplitToning c = (SplitToning)profile.components[i];
                            c.active = profileData.splitToning_active;
                            c.balance.value = profileData.splitToning_balance;
                            c.highlights.value = profileData.splitToning_highlights;
                            c.shadows.value = profileData.splitToning_shadows;
                        }
                        else if (profile.components[i] is Fog)
                        {
                            Fog c = (Fog)profile.components[i];
                            c.active = profileData.fog_active;
                            c.albedo.value = profileData.fog_albedo;
                            c.anisotropy.value = profileData.fog_anisotropy;
                            c.baseHeight.value = profileData.fog_baseHeight;
                            c.color.value = profileData.fog_color;
                            c.colorMode.value = profileData.fog_colorMode;
                            c.depthExtent.value = profileData.fog_depthExtent;
                            c.enabled.value = profileData.fog_enabled;
                            c.enableVolumetricFog.value = profileData.fog_enableVolumetricFog;
                            c.filter.value = profileData.fog_filter;
                            c.globalLightProbeDimmer.value = profileData.fog_globalLightProbeDimmer;
                            c.maxFogDistance.value = profileData.fog_maxFogDistance;
                            c.maximumHeight.value = profileData.fog_maximumHeight;
                            c.meanFreePath.value = profileData.fog_meanFreePath;
                            c.mipFogFar.value = profileData.fog_mipFogFar;
                            c.mipFogMaxMip.value = profileData.fog_mipFogMaxMip;
                            c.mipFogNear.value = profileData.fog_mipFogNear;
                            c.sliceDistributionUniformity.value = profileData.fog_sliceDistributionUniformity;
                            c.tint.value = profileData.fog_tint;
                        }
                        else if (profile.components[i] is IndirectLightingController)
                        {
                            IndirectLightingController c = (IndirectLightingController)profile.components[i];
                            c.active = profileData.indirectLightingController_active;
                            c.indirectDiffuseIntensity.value = profileData.indirectLightingController_indirectDiffuseIntensity;
                            c.indirectDiffuseLightingLayers.value = profileData.indirectLightingController_indirectDiffuseLightingLayers;
                            c.indirectSpecularIntensity.value = profileData.indirectLightingController_indirectSpecularIntensity;
                            c.reflectionLightingLayers.value = profileData.indirectLightingController_reflectionLightingLayers;
                            c.reflectionLightingMultiplier.value = profileData.indirectLightingController_reflectionLightingMultiplier;
                        }
                        else if (profile.components[i] is VisualEnvironment)
                        {
                            VisualEnvironment c = (VisualEnvironment)profile.components[i];
                            c.active = profileData.visualEnvironment_active;
                            c.skyAmbientMode.value = profileData.visualEnvironment_skyAmbientMode;
                            c.skyType.value = profileData.visualEnvironment_skyType;
                        }
                        else if (profile.components[i] is AmbientOcclusion)
                        {
                            AmbientOcclusion c = (AmbientOcclusion)profile.components[i];
                            c.active = profileData.ambientOcclusion_active;
                            c.blurSharpness.value = profileData.ambientOcclusion_blurSharpness;
                            c.denoise.value = profileData.ambientOcclusion_denoise;
                            c.denoiserRadius.value = profileData.ambientOcclusion_denoiserRadius;
                            c.directionCount = profileData.ambientOcclusion_directionCount;
                            c.directLightingStrength.value = profileData.ambientOcclusion_directLightingStrength;
                            c.ghostingReduction.value = profileData.ambientOcclusion_ghostingReduction;
                            c.intensity.value = profileData.ambientOcclusion_intensity;
                            c.quality.value = profileData.ambientOcclusion_quality;
                            c.radius.value = profileData.ambientOcclusion_radius;
                            c.rayLength.value = profileData.ambientOcclusion_rayLength;
                            c.rayTracing.value = profileData.ambientOcclusion_rayTracing;
                            c.sampleCount.value = profileData.ambientOcclusion_sampleCount;
                            c.temporalAccumulation.value = profileData.ambientOcclusion_temporalAccumulation;
                        }
                        else if (profile.components[i] is GradientSky)
                        {
                            GradientSky c = (GradientSky)profile.components[i];
                            c.active = profileData.gradientSky_active;
                            c.bottom.value = profileData.gradientSky_bottom;
                            c.desiredLuxValue.value = profileData.gradientSky_desiredLuxValue;
                            c.exposure.value = profileData.gradientSky_exposure;
                            c.gradientDiffusion.value = profileData.gradientSky_gradientDiffusion;
                            c.includeSunInBaking.value = profileData.gradientSky_includeSunInBaking;
                            c.middle.value = profileData.gradientSky_middle;
                            c.multiplier.value = profileData.gradientSky_multiplier;
                            c.rotation.value = profileData.gradientSky_rotation;
                            c.skyIntensityMode.value = profileData.gradientSky_skyIntensityMode;
                            c.top.value = profileData.gradientSky_top;
                            c.updateMode.value = profileData.gradientSky_updateMode;
                            c.updatePeriod.value = profileData.gradientSky_updatePeriod;
                            c.upperHemisphereLuxColor.value = profileData.gradientSky_upperHemisphereLuxColor;
                            c.upperHemisphereLuxValue.value = profileData.gradientSky_upperHemisphereLuxValue;
                        }
                        else if (profile.components[i] is HDRISky)
                        {
                            HDRISky c = (HDRISky)profile.components[i];
                            c.active = profileData.HDRISky_active;
                            c.backplateType.value = profileData.HDRISky_backplateType;
                            c.blendAmount.value = profileData.HDRISky_blendAmount;
                            c.desiredLuxValue.value = profileData.HDRISky_desiredLuxValue;
                            c.dirLightShadow.value = profileData.HDRISky_dirLightShadow;
                            c.enableBackplate.value = profileData.HDRISky_enableBackplate;
                            c.exposure.value = profileData.HDRISky_exposure;
                            c.groundLevel.value = profileData.HDRISky_groundLevel;
                            c.includeSunInBaking.value = profileData.HDRISky_includeSunInBaking;
                            c.multiplier.value = profileData.HDRISky_multiplier;
                            c.plateRotation.value = profileData.HDRISky_plateRotation;
                            c.plateTexOffset.value = profileData.HDRISky_plateTexOffset;
                            c.plateTexRotation.value = profileData.HDRISky_plateTexRotation;
                            c.pointLightShadow.value = profileData.HDRISky_pointLightShadow;
                            c.projectionDistance.value = profileData.HDRISky_projectionDistance;
                            c.rectLightShadow.value = profileData.HDRISky_rectLightShadow;
                            c.rotation.value = profileData.HDRISky_rotation;
                            c.scale.value = profileData.HDRISky_scale;
                            c.shadowTint.value = profileData.HDRISky_shadowTint;
                            c.skyIntensityMode.value = profileData.HDRISky_skyIntensityMode;
                            c.updateMode.value = profileData.HDRISky_updateMode;
                            c.updatePeriod.value = profileData.HDRISky_updatePeriod;
                            c.upperHemisphereLuxColor.value = profileData.HDRISky_upperHemisphereLuxColor;
                            c.upperHemisphereLuxValue.value = profileData.HDRISky_upperHemisphereLuxValue;
                        }
                        else if (profile.components[i] is ColorAdjustments)
                        {
                            ColorAdjustments c = (ColorAdjustments)profile.components[i];
                            c.active = profileData.colorAdjustments_active;
                            c.colorFilter.value = profileData.colorAdjustments_colorFilter;
                            c.contrast.value = profileData.colorAdjustments_contrast;
                            c.hueShift.value = profileData.colorAdjustments_hueShift;
                            c.postExposure.value = profileData.colorAdjustments_postExposure;
                            c.saturation.value = profileData.colorAdjustments_saturation;
                        }
                    }
                }
            }


            QualitySettings.anisotropicFiltering = anisotropicFiltering.Value;
            QualitySettings.antiAliasing = antiAliasing.Value;
            QualitySettings.asyncUploadBufferSize = asyncUploadBufferSize.Value;
            QualitySettings.asyncUploadPersistentBuffer = asyncUploadPersistentBuffer.Value;
            QualitySettings.asyncUploadTimeSlice = asyncUploadTimeSlice.Value;
            QualitySettings.billboardsFaceCameraPosition = billboardsFaceCameraPosition.Value;
            QualitySettings.lodBias = lodBias.Value;
            QualitySettings.masterTextureLimit = masterTextureLimit.Value;
            QualitySettings.maximumLODLevel = maximumLODLevel.Value;
            QualitySettings.maxQueuedFrames = maxQueuedFrames.Value;
            QualitySettings.particleRaycastBudget = particleRaycastBudget.Value;
            QualitySettings.pixelLightCount = pixelLightCount.Value;
            QualitySettings.realtimeReflectionProbes = realtimeReflectionProbes.Value;
            QualitySettings.resolutionScalingFixedDPIFactor = resolutionScalingFixedDPIFactor.Value;
            QualitySettings.shadowCascade2Split = shadowCascade2Split.Value;
            QualitySettings.shadowCascade4Split = shadowCascade4Split.Value;
            QualitySettings.shadowCascades = shadowCascades.Value;
            QualitySettings.shadowDistance = shadowDistance.Value;
            QualitySettings.shadowmaskMode = shadowmaskMode.Value;
            QualitySettings.shadowNearPlaneOffset = shadowNearPlaneOffset.Value;
            QualitySettings.shadowProjection = shadowProjection.Value;
            QualitySettings.shadowResolution = shadowResolution.Value;
            QualitySettings.shadows = shadows.Value;
            QualitySettings.skinWeights = skinWeights.Value;
            QualitySettings.softParticles = softParticles.Value;
            QualitySettings.softVegetation = softVegetation.Value;
            QualitySettings.streamingMipmapsActive = streamingMipmapsActive.Value;
            QualitySettings.streamingMipmapsAddAllCameras = streamingMipmapsAddAllCameras.Value;
            QualitySettings.streamingMipmapsMaxFileIORequests = streamingMipmapsMaxFileIORequests.Value;
            QualitySettings.streamingMipmapsMaxLevelReduction = streamingMipmapsMaxLevelReduction.Value;
            QualitySettings.streamingMipmapsMemoryBudget = streamingMipmapsMemoryBudget.Value;
            QualitySettings.vSyncCount = vSyncCount.Value;

            Dbgl("Set custom graphics settings");
        }

        private static bool IsDisabled(VolumeComponent volumeComponent)
        {
            return (
                (volumeComponent is DepthOfField && disableDepthOfField.Value) ||
                (volumeComponent is Vignette && disableVignette.Value) ||
                (volumeComponent is Exposure && disableExposure.Value) ||
                (volumeComponent is WhiteBalance && disableWhiteBalance.Value) ||
                (volumeComponent is ChromaticAberration && disableChromaticAberration.Value) ||
                (volumeComponent is Bloom && disableBloom.Value) ||
                (volumeComponent is SplitToning && disableSplitToning.Value) ||
                (volumeComponent is Fog && disableFog.Value) ||
                (volumeComponent is IndirectLightingController && disableIndirectLightingController.Value) ||
                (volumeComponent is AmbientOcclusion && disableAmbientOcclusion.Value) ||
                (volumeComponent is GradientSky && disableGradientSky.Value) ||
                (volumeComponent is HDRISky && disableHDRISky.Value) ||
                (volumeComponent is ColorAdjustments && disableColorAdjustments.Value)
            );
        }

        [HarmonyPatch(typeof(Scene), "Awake")]
        static class Scene_Awake_Patch
        {
            static void Postfix()
            {
                if (!modEnabled.Value)
                    return;

                SetGraphicsSettings();
            }
        }
    }
}
