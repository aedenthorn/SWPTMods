using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace AdvancedGraphicsSettings
{
    [BepInPlugin("aedenthorn.AdvancedGraphicsSettings", "Advanced Graphics Settings", "0.1.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        //public static ConfigEntry<int> nexusID;

        public static ConfigEntry<string> refreshHotkey;
        public static ConfigEntry<string> dumpModKey;

        public static ConfigEntry<bool> depthOfField_active;
        public static ConfigEntry<float> depthOfField_nearFocusStart;
        public static ConfigEntry<float> depthOfField_nearFocusEnd;
        public static ConfigEntry<float> depthOfField_farFocusStart;
        public static ConfigEntry<float> depthOfField_farFocusEnd;

        public static ConfigEntry<bool> vignette_active;
        public static ConfigEntry<Color> vignette_color;
        public static ConfigEntry<float> vignette_intensity;
        public static ConfigEntry<VignetteMode> vignette_mode;
        public static ConfigEntry<float> vignette_opacity;
        public static ConfigEntry<bool> vignette_rounded;
        public static ConfigEntry<float> vignette_roundness;
        public static ConfigEntry<float> vignette_smoothness;

        public static ConfigEntry<bool> exposure_active;
        public static ConfigEntry<AdaptationMode> exposure_adaptationMode;
        public static ConfigEntry<float> exposure_adaptationSpeedDarkToLight;
        public static ConfigEntry<float> exposure_adaptationSpeedLightToDark;
        public static ConfigEntry<float> exposure_compensation;
        public static ConfigEntry<float> exposure_fixedExposure;
        public static ConfigEntry<float> exposure_limitMax;
        public static ConfigEntry<MeteringMode> exposure_meteringMode;
        public static ConfigEntry<ExposureMode> exposure_mode;

        public static ConfigEntry<bool> whiteBalance_active;
        public static ConfigEntry<float> whiteBalance_temperature;
        public static ConfigEntry<float> whiteBalance_tint;

        public static ConfigEntry<bool> chromaticAberration_active;
        public static ConfigEntry<float> chromaticAberration_intensity;
        public static ConfigEntry<int> chromaticAberration_quality;

        public static ConfigEntry<bool> bloom_active;
        public static ConfigEntry<bool> bloom_anamorphic;
        public static ConfigEntry<float> bloom_dirtIntensity;
        public static ConfigEntry<float> bloom_intensity;
        public static ConfigEntry<int> bloom_quality;
        public static ConfigEntry<float> bloom_threshold;
        public static ConfigEntry<Color> bloom_tint;

        public static ConfigEntry<bool> splitToning_active;
        public static ConfigEntry<float> splitToning_balance;
        public static ConfigEntry<Color> splitToning_highlights;
        public static ConfigEntry<Color> splitToning_shadows;



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
        
        private static BepInExPlugin context;
        private string assetPath;

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
            //nexusID = Config.Bind<int>("General", "NexusID", 169, "Nexus mod ID for updates");

            refreshHotkey = Config.Bind("Options", "RefreshHotkey", "page up", "Hotkey to refresh settings after changing them in-game.");
            dumpModKey = Config.Bind("Options", "RefreshHotkey", "left shift", "Hotkey to dump volume profiles instead of refreshing.");

            vignette_active = Config.Bind("Vignette", "vignette_active", true, "vignette_active");
            vignette_color = Config.Bind("Vignette", "vignette_color", new Color(0, 0, 0, 1), "vignette_color");
            vignette_intensity = Config.Bind("Vignette", "vignette_intensity", 0.293f, "vignette_intensity");
            vignette_mode = Config.Bind("Vignette", "vignette_mode", VignetteMode.Procedural, "vignette_mode");
            vignette_opacity = Config.Bind("Vignette", "vignette_opacity", 1f, "vignette_opacity");
            vignette_rounded = Config.Bind("Vignette", "vignette_rounded", false, "vignette_rounded");
            vignette_roundness = Config.Bind("Vignette", "vignette_roundness", 1f, "vignette_roundness");
            vignette_smoothness = Config.Bind("Vignette", "vignette_smoothness", 0.2f, "vignette_smoothness");

            exposure_active = Config.Bind("Exposure", "exposure_active", true, "exposure_active");
            exposure_adaptationMode = Config.Bind("Exposure", "exposure_adaptationMode", AdaptationMode.Progressive, "exposure_adaptationMode");
            exposure_adaptationSpeedDarkToLight = Config.Bind("Exposure", "exposure_adaptationSpeedDarkToLight", 3f, "exposure_adaptationSpeedDarkToLight");
            exposure_adaptationSpeedLightToDark = Config.Bind("Exposure", "exposure_adaptationSpeedLightToDark", 1f, "exposure_adaptationSpeedLightToDark");
            exposure_compensation = Config.Bind("Exposure", "exposure_compensation", 6.5f, "exposure_compensation");
            exposure_fixedExposure = Config.Bind("Exposure", "exposure_fixedExposure", 5.7f, "exposure_fixedExposure");
            exposure_limitMax = Config.Bind("Exposure", "exposure_limitMax", 8.5f, "exposure_limitMax");
            exposure_meteringMode = Config.Bind("Exposure", "exposure_meteringMode", MeteringMode.CenterWeighted, "exposure_meteringMode");
            exposure_mode = Config.Bind("Exposure", "exposure_mode", ExposureMode.UsePhysicalCamera, "exposure_mode");

            whiteBalance_active = Config.Bind("White Balance", "whiteBalance_active", false, "whiteBalance_active");
            whiteBalance_temperature = Config.Bind("White Balance", "whiteBalance_temperature", 20f, "whiteBalance_temperature");
            whiteBalance_tint = Config.Bind("White Balance", "whiteBalance_tint", 0f, "whiteBalance_tint");

            chromaticAberration_active = Config.Bind("Chromatic Aberration", "chromaticAberration_active", true, "chromaticAberration_active");
            chromaticAberration_intensity = Config.Bind("Chromatic Aberration", "chromaticAberration_intensity", 0.05f, "chromaticAberration_intensity");
            chromaticAberration_quality = Config.Bind("Chromatic Aberration", "chromaticAberration_quality", 1, "chromaticAberration_quality");

            bloom_active = Config.Bind("Bloom", "bloom_active", true, "bloom_active");
            bloom_anamorphic = Config.Bind("Bloom", "bloom_anamorphic", true, "bloom_anamorphic");
            bloom_dirtIntensity = Config.Bind("Bloom", "bloom_dirtIntensity", 0f, "bloom_dirtIntensity");
            bloom_intensity = Config.Bind("Bloom", "bloom_intensity", 0.426f, "bloom_intensity");
            bloom_quality = Config.Bind("Bloom", "bloom_quality", 2, "bloom_quality");
            bloom_threshold = Config.Bind("Bloom", "bloom_threshold", 0.71f, "bloom_threshold");
            bloom_tint = Config.Bind("Bloom", "bloom_tint", new Color(1, 1, 1, 1), "bloom_tint");

            splitToning_active = Config.Bind("Split Toning", "splitToning_active", true, "splitToning_active");
            splitToning_balance = Config.Bind("Split Toning", "splitToning_balance", 0f, "splitToning_balance");
            splitToning_highlights = Config.Bind("Split Toning", "splitToning_highlights", new Color(0.726f, 0.616f, 0.395f, 1), "splitToning_highlights");
            splitToning_shadows = Config.Bind("Split Toning", "splitToning_shadows", new Color(0.455f, 0.455f, 0.557f, 1), "splitToning_shadows");

            depthOfField_active = Config.Bind("PostProcessing", "depthOfField_active", false, "depthOfField_active");
            depthOfField_nearFocusStart = Config.Bind("PostProcessing", "depthOfField_nearFocusStart", 0f, "depthOfField_nearFocusStart");
            depthOfField_nearFocusEnd = Config.Bind("PostProcessing", "depthOfField_nearFocusEnd", 0f, "depthOfField_nearFocusEnd");
            depthOfField_farFocusStart = Config.Bind("PostProcessing", "depthOfField_farFocusStart", 5.41f, "depthOfField_farFocusStart");
            depthOfField_farFocusEnd = Config.Bind("PostProcessing", "depthOfField_farFocusEnd", 6.68f, "depthOfField_farFocusEnd");

            anisotropicFiltering = Config.Bind("Z Quality Settings", "anisotropicFiltering", AnisotropicFiltering.ForceEnable, "Global anisotropic filtering mode.");
            antiAliasing = Config.Bind("Z Quality Settings", "antiAliasing", 0, "Set The AA Filtering option.");
            asyncUploadBufferSize = Config.Bind("Z Quality Settings", "asyncUploadBufferSize", 4, "Asynchronous texture and mesh data upload provides timesliced async texture and mesh data upload on the render thread with tight control over memory and timeslicing. There are no allocations except for the ones which driver has to do. To read data and upload texture and mesh data, Unity re-uses a ringbuffer whose size can be controlled.Use asyncUploadBufferSize to set the buffer size for asynchronous texture and mesh data uploads. The size is in megabytes. The minimum value is 2 and the maximum value is 512. The buffer resizes automatically to fit the largest texture currently loading. To avoid re-sizing of the buffer, which can incur performance cost, set the value approximately to the size of biggest texture used in the Scene.");
            asyncUploadPersistentBuffer = Config.Bind("Z Quality Settings", "asyncUploadPersistentBuffer", true, "This flag controls if the async upload pipeline's ring buffer remains allocated when there are no active loading operations. Set this to true, to make the ring buffer allocation persist after all upload operations have completed. If you have issues with excessive memory usage, you can set this to false. This means you reduce the runtime memory footprint, but memory fragmentation can occur. The default value is true.");
            asyncUploadTimeSlice = Config.Bind("Z Quality Settings", "asyncUploadTimeSlice", 2, "Async texture upload provides timesliced async texture upload on the render thread with tight control over memory and timeslicing. There are no allocations except for the ones which driver has to do. To read data and upload texture data a ringbuffer whose size can be controlled is re-used.Use asyncUploadTimeSlice to set the time-slice in milliseconds for asynchronous texture uploads per frame. Minimum value is 1 and maximum is 33.");
            billboardsFaceCameraPosition = Config.Bind("Z Quality Settings", "billboardsFaceCameraPosition", true, "If enabled, billboards will face towards camera position rather than camera orientation.");
            lodBias = Config.Bind("Z Quality Settings", "lodBias", 2, "Global multiplier for the LOD's switching distance.");
            masterTextureLimit = Config.Bind("Z Quality Settings", "masterTextureLimit", 0, "A texture size limit applied to all textures.");
            maximumLODLevel = Config.Bind("Z Quality Settings", "maximumLODLevel", 0, "A maximum LOD level. All LOD groups.");
            maxQueuedFrames = Config.Bind("Z Quality Settings", "maxQueuedFrames", 2, "Maximum number of frames queued up by graphics driver.");
            particleRaycastBudget = Config.Bind("Z Quality Settings", "particleRaycastBudget", 4096, "Budget for how many ray casts can be performed per frame for approximate collision testing.");
            pixelLightCount = Config.Bind("Z Quality Settings", "pixelLightCount", 8, "The maximum number of pixel lights that should affect any object.");
            realtimeReflectionProbes = Config.Bind("Z Quality Settings", "realtimeReflectionProbes", true, "Enables realtime reflection probes.");
            resolutionScalingFixedDPIFactor = Config.Bind("Z Quality Settings", "resolutionScalingFixedDPIFactor", 1, "In resolution scaling mode, this factor is used to multiply with the target Fixed DPI specified to get the actual Fixed DPI to use for this quality setting.");
            shadowCascade2Split = Config.Bind("Z Quality Settings", "shadowCascade2Split", 0.3333333f, "The normalized cascade distribution for a 2 cascade setup. The value defines the position of the cascade with respect to Zero.");
            shadowCascade4Split = Config.Bind("Z Quality Settings", "shadowCascade4Split", new Vector3(0.1f, 0.2f, 0.5f), "The normalized cascade start position for a 4 cascade setup. Each member of the vector defines the normalized position of the coresponding cascade with respect to Zero.");
            shadowCascades = Config.Bind("Z Quality Settings", "shadowCascades", 4, "Number of cascades to use for directional light shadows.");
            shadowDistance = Config.Bind("Z Quality Settings", "shadowDistance", 150, "Shadow drawing distance.");
            shadowmaskMode = Config.Bind("Z Quality Settings", "shadowmaskMode", ShadowmaskMode.Shadowmask, "The rendering mode of Shadowmask.");
            shadowNearPlaneOffset = Config.Bind("Z Quality Settings", "shadowNearPlaneOffset", 3, "Offset shadow frustum near plane.");
            shadowProjection = Config.Bind("Z Quality Settings", "shadowProjection", ShadowProjection.StableFit, "Directional light shadow projection.");
            shadowResolution = Config.Bind("Z Quality Settings", "shadowResolution", ShadowResolution.High, "The default resolution of the shadow maps.");
            shadows = Config.Bind("Z Quality Settings", "shadows", ShadowQuality.All, "Realtime Shadows type to be used.");
            skinWeights = Config.Bind("Z Quality Settings", "skinWeights", SkinWeights.TwoBones, "The maximum number of bone weights that can affect a vertex, for all skinned meshes in the project.");
            softParticles = Config.Bind("Z Quality Settings", "softParticles", true, "Should soft blending be used for particles?");
            softVegetation = Config.Bind("Z Quality Settings", "softVegetation", true, "Use a two-pass shader for the vegetation in the terrain engine.");
            streamingMipmapsActive = Config.Bind("Z Quality Settings", "streamingMipmapsActive", true, "Enable automatic streaming of texture mipmap levels based on their distance from all active cameras.");
            streamingMipmapsAddAllCameras = Config.Bind("Z Quality Settings", "streamingMipmapsAddAllCameras", true, "Process all enabled Cameras for texture streaming (rather than just those with StreamingController components).");
            streamingMipmapsMaxFileIORequests = Config.Bind("Z Quality Settings", "streamingMipmapsMaxFileIORequests", 1024, "The maximum number of active texture file IO requests from the texture streaming system.");
            streamingMipmapsMaxLevelReduction = Config.Bind("Z Quality Settings", "streamingMipmapsMaxLevelReduction", 2, "The maximum number of mipmap levels to discard for each texture.");
            streamingMipmapsMemoryBudget = Config.Bind("Z Quality Settings", "streamingMipmapsMemoryBudget", 512, "The total amount of memory to be used by streaming and non-streaming textures.");
            vSyncCount = Config.Bind("Z Quality Settings", "vSyncCount", 1, "The VSync Count.");

            if (!modEnabled.Value)
                return;

            assetPath = AedenthornUtils.GetAssetPath(typeof(BepInExPlugin).Namespace);
            if (!Directory.Exists(assetPath))
            {
                Directory.CreateDirectory(assetPath);
            }

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);


        }

        private void Update()
        {
            if (AedenthornUtils.CheckKeyDown(refreshHotkey.Value))
            {
                Dbgl("Hotkey pressed");

                if (AedenthornUtils.CheckKeyHeld(dumpModKey.Value))
                {
                    foreach(Volume volume in FindObjectsOfType<Volume>())
                    {
                        VolumeProfileData data = new VolumeProfileData(volume.sharedProfile);
                        string json = JsonUtility.ToJson(data);
                        File.WriteAllText(Path.Combine(assetPath, volume.name+".json"), json);
                        Dbgl($"Writing volume profile {volume.name} to file");

                    }
                }
                else
                {
                    Config.Reload();
                    SetGraphicsSettings();
                }

            }
        }
        [HarmonyPatch(typeof(Scene), "Awake")]
        static class Scene_Awake_Patch
        {
            static void Postfix()
            {
                if (!modEnabled.Value)
                    return;

                Volume postProcessV = GameObject.Find("/Scene/Lights/Post Process")?.GetComponent<Volume>();
                Volume skyV = GameObject.Find("/Scene/Lights/Sky and Fog Volume")?.GetComponent<Volume>();

                if (postProcessV)
                {
                    var profile = postProcessV.sharedProfile;
                    for (int i = 0; i < profile.components.Count; i++)
                    {
                        if (profile.components[i] is DepthOfField)
                        {
                            DepthOfField c = (DepthOfField)profile.components[i];
                            Dbgl($"depthOfField_active = Config.Bind(\"PostProcessing\", \"depthOfField_active\", {c.active}, \"depthOfField_active\");");
                            Dbgl($"depthOfField_nearFocusStart = Config.Bind(\"PostProcessing\", \"depthOfField_nearFocusStart\", {c.nearFocusStart.value}, \"depthOfField_nearFocusStart\");");
                            Dbgl($"depthOfField_nearFocusEnd = Config.Bind(\"PostProcessing\", \"depthOfField_nearFocusEnd\", {c.nearFocusEnd.value}, \"depthOfField_nearFocusEnd\");");
                            Dbgl($"depthOfField_farFocusStart = Config.Bind(\"PostProcessing\", \"depthOfField_farFocusStart\", {c.farFocusStart.value}, \"depthOfField_farFocusStart\");");
                            Dbgl($"depthOfField_farFocusEnd = Config.Bind(\"PostProcessing\", \"depthOfField_farFocusEnd\", {c.farFocusEnd.value}, \"depthOfField_farFocusEnd\");");

                        }
                        else if (profile.components[i] is Vignette)
                        {
                            Vignette c = (Vignette)profile.components[i];
                            Dbgl($"vignette_active = Config.Bind(\"PostProcessing\", \"vignette_active\", {c.active}, \"vignette_active\");");
                            Dbgl($"vignette_color = Config.Bind(\"PostProcessing\", \"vignette_color\", {c.color.value}, \"vignette_color\");");
                            Dbgl($"vignette_intensity = Config.Bind(\"PostProcessing\", \"vignette_intensity\", {c.intensity.value}, \"vignette_intensity\");");
                            Dbgl($"vignette_mode = Config.Bind(\"PostProcessing\", \"vignette_mode\", {c.mode.value}, \"vignette_mode\");");
                            Dbgl($"vignette_opacity = Config.Bind(\"PostProcessing\", \"vignette_opacity\", {c.opacity.value}, \"vignette_opacity\");");
                            Dbgl($"vignette_rounded = Config.Bind(\"PostProcessing\", \"vignette_rounded\", {c.rounded.value}, \"vignette_rounded\");");
                            Dbgl($"vignette_roundness = Config.Bind(\"PostProcessing\", \"vignette_roundness\", {c.roundness.value}, \"vignette_roundness\");");
                            Dbgl($"vignette_smoothness = Config.Bind(\"PostProcessing\", \"vignette_smoothness\", {c.smoothness.value}, \"vignette_smoothness\");");
                        }
                        else if (profile.components[i] is Exposure)
                        {
                            Exposure c = (Exposure)profile.components[i];
                            Dbgl($"exposure_active = Config.Bind(\"PostProcessing\", \"exposure_active\", {c.active}, \"exposure_active\");");
                            Dbgl($"exposure_adaptationMode = Config.Bind(\"PostProcessing\", \"exposure_adaptationMode\", {c.adaptationMode.value}, \"exposure_adaptationMode\");");
                            Dbgl($"exposure_adaptationSpeedDarkToLight = Config.Bind(\"PostProcessing\", \"exposure_adaptationSpeedDarkToLight\", {c.adaptationSpeedDarkToLight.value}, \"exposure_adaptationSpeedDarkToLight\");");
                            Dbgl($"exposure_adaptationSpeedLightToDark = Config.Bind(\"PostProcessing\", \"exposure_adaptationSpeedLightToDark\", {c.adaptationSpeedLightToDark.value}, \"exposure_adaptationSpeedLightToDark\");");
                            Dbgl($"exposure_compensation = Config.Bind(\"PostProcessing\", \"exposure_compensation\", {c.compensation.value}, \"exposure_compensation\");");
                            Dbgl($"exposure_fixedExposure = Config.Bind(\"PostProcessing\", \"exposure_fixedExposure\", {c.fixedExposure.value}, \"exposure_fixedExposure\");");
                            Dbgl($"exposure_limitMax = Config.Bind(\"PostProcessing\", \"exposure_limitMax\", {c.limitMax.value}, \"exposure_limitMax\");");
                            Dbgl($"exposure_meteringMode = Config.Bind(\"PostProcessing\", \"exposure_meteringMode\", {c.meteringMode.value}, \"exposure_meteringMode\");");
                            Dbgl($"exposure_mode = Config.Bind(\"PostProcessing\", \"exposure_mode\", {c.mode.value}, \"exposure_mode\");");
                        }
                        else if (profile.components[i] is WhiteBalance)
                        {
                            WhiteBalance c = (WhiteBalance)profile.components[i];
                            Dbgl($"whiteBalance_active = Config.Bind(\"PostProcessing\", \"whiteBalance_active\", {c.active}, \"whiteBalance_active\");");
                            Dbgl($"whiteBalance_temperature = Config.Bind(\"PostProcessing\", \"whiteBalance_temperature\", {c.temperature.value}, \"whiteBalance_temperature\");");
                            Dbgl($"whiteBalance_tint = Config.Bind(\"PostProcessing\", \"whiteBalance_tint\", {c.tint.value}, \"whiteBalance_tint\");");
                        }
                        else if (profile.components[i] is ChromaticAberration)
                        {
                            ChromaticAberration c = (ChromaticAberration)profile.components[i];
                            Dbgl($"chromaticAberration_active = Config.Bind(\"PostProcessing\", \"chromaticAberration_active\", {c.active}, \"chromaticAberration_active\");");
                            Dbgl($"chromaticAberration_intensity = Config.Bind(\"PostProcessing\", \"chromaticAberration_intensity\", {c.intensity.value}, \"chromaticAberration_intensity\");");
                            Dbgl($"chromaticAberration_quality = Config.Bind(\"PostProcessing\", \"chromaticAberration_quality\", {c.quality.value}, \"chromaticAberration_quality\");");
                        }
                        else if (profile.components[i] is Bloom)
                        {
                            Bloom c = (Bloom)profile.components[i];
                            Dbgl($"bloom_active = Config.Bind(\"PostProcessing\", \"bloom_active\", {c.active}, \"bloom_active\");");
                            Dbgl($"bloom_anamorphic = Config.Bind(\"PostProcessing\", \"bloom_anamorphic\", {c.anamorphic.value}, \"bloom_anamorphic\");");
                            Dbgl($"bloom_dirtIntensity = Config.Bind(\"PostProcessing\", \"bloom_dirtIntensity\", {c.dirtIntensity.value}, \"bloom_dirtIntensity\");");
                            Dbgl($"bloom_intensity = Config.Bind(\"PostProcessing\", \"bloom_intensity\", {c.intensity.value}, \"bloom_intensity\");");
                            Dbgl($"bloom_quality = Config.Bind(\"PostProcessing\", \"bloom_quality\", {c.quality.value}, \"bloom_quality\");");
                            Dbgl($"bloom_threshold = Config.Bind(\"PostProcessing\", \"bloom_threshold\", {c.threshold.value}, \"bloom_threshold\");");
                            Dbgl($"bloom_tint = Config.Bind(\"PostProcessing\", \"bloom_tint\", {c.tint.value}, \"bloom_tint\");");
                        }
                        else if (profile.components[i] is SplitToning)
                        {
                            SplitToning c = (SplitToning)profile.components[i];
                            Dbgl($"splitToning_active = Config.Bind(\"PostProcessing\", \"splitToning_active\", {c.active}, \"splitToning_active\");");
                            Dbgl($"splitToning_balance = Config.Bind(\"PostProcessing\", \"splitToning_balance\", {c.balance.value}, \"splitToning_balance\");");
                            Dbgl($"splitToning_highlights = Config.Bind(\"PostProcessing\", \"splitToning_highlights\", {c.highlights.value}, \"splitToning_highlights\");");
                            Dbgl($"splitToning_shadows = Config.Bind(\"PostProcessing\", \"splitToning_shadows\", {c.shadows.value}, \"splitToning_shadows\");");
                        }
                    }
                    SetGraphicsSettings();
                }
            }
        }
        private static void SetGraphicsSettings()
        {
            Volume postProcessV = GameObject.Find("/Scene/Lights/Post Process")?.GetComponent<Volume>();
            if (postProcessV)
            {
                Dbgl("Modifying post processing variables");

                var profile = postProcessV.sharedProfile;
                for (int i = 0; i < profile.components.Count; i++)
                {
                    if (profile.components[i] is DepthOfField)
                    {
                        DepthOfField c = (DepthOfField)profile.components[i];
                        c.active = depthOfField_active.Value;
                        c.nearFocusStart.value = depthOfField_nearFocusStart.Value;
                        c.nearFocusEnd.value = depthOfField_nearFocusEnd.Value;
                        c.farFocusStart.value = depthOfField_farFocusStart.Value;
                        c.farFocusEnd.value = depthOfField_farFocusEnd.Value;

                    }
                    else if (profile.components[i] is Vignette)
                    {
                        Vignette c = (Vignette)profile.components[i];
                        c.active = vignette_active.Value;
                        c.color.value = vignette_color.Value;
                        c.intensity.value = vignette_intensity.Value;
                        c.mode.value = vignette_mode.Value;
                        c.opacity.value = vignette_opacity.Value;
                        c.rounded.value = vignette_rounded.Value;
                        c.roundness.value = vignette_roundness.Value;
                        c.smoothness.value = vignette_smoothness.Value;
                    }
                    else if (profile.components[i] is Exposure)
                    {
                        Exposure c = (Exposure)profile.components[i];
                        c.active = exposure_active.Value;
                        c.adaptationMode.value = exposure_adaptationMode.Value;
                        c.adaptationSpeedDarkToLight.value = exposure_adaptationSpeedDarkToLight.Value;
                        c.adaptationSpeedLightToDark.value = exposure_adaptationSpeedLightToDark.Value;
                        c.compensation.value = exposure_compensation.Value;
                        c.fixedExposure.value = exposure_fixedExposure.Value;
                        c.limitMax.value = exposure_limitMax.Value;
                        c.meteringMode.value = exposure_meteringMode.Value;
                        c.mode.value = exposure_mode.Value;
                    }
                    else if (profile.components[i] is WhiteBalance)
                    {
                        WhiteBalance c = (WhiteBalance)profile.components[i];
                        c.active = whiteBalance_active.Value;
                        c.temperature.value = whiteBalance_temperature.Value;
                        c.tint.value = whiteBalance_tint.Value;
                    }
                    else if (profile.components[i] is ChromaticAberration)
                    {
                        ChromaticAberration c = (ChromaticAberration)profile.components[i];
                        c.active = chromaticAberration_active.Value;
                        c.intensity.value = chromaticAberration_intensity.Value;
                        c.quality.value = chromaticAberration_quality.Value;
                    }
                    else if (profile.components[i] is Bloom)
                    {
                        Bloom c = (Bloom)profile.components[i];
                        c.active = bloom_active.Value;
                        c.anamorphic.value = bloom_anamorphic.Value;
                        c.dirtIntensity.value = bloom_dirtIntensity.Value;
                        c.intensity.value = bloom_intensity.Value;
                        c.quality.value = bloom_quality.Value;
                        c.threshold.value = bloom_threshold.Value;
                        c.tint.value = bloom_tint.Value;
                    }
                    else if (profile.components[i] is SplitToning)
                    {
                        SplitToning c = (SplitToning)profile.components[i];
                        c.active = splitToning_active.Value;
                        c.balance.value = splitToning_balance.Value;
                        c.highlights.value = splitToning_highlights.Value;
                        c.shadows.value = splitToning_shadows.Value;
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
    }
}
