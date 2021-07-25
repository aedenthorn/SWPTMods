using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace AdvancedGraphicsSettings
{
    [BepInPlugin("aedenthorn.AdvancedGraphicsSettings", "Advanced Graphics Settings", "0.1.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        //public static ConfigEntry<int> nexusID;

        public static ConfigEntry<string> refreshHotkey;

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

        private static BepInExPlugin context;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug.Value)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            context = this;
            modEnabled = Config.Bind("General", "Enabled", true, "Enable this mod");
            isDebug = Config.Bind<bool>("General", "IsDebug", true, "Enable debug logs");
            //nexusID = Config.Bind<int>("General", "NexusID", 169, "Nexus mod ID for updates");

            refreshHotkey = Config.Bind("Options", "RefreshHotkey", "page up", "Hotkey to refresh settings after changing them in-game.");

            anisotropicFiltering = Config.Bind("QualitySettings", "anisotropicFiltering", AnisotropicFiltering.ForceEnable, "Global anisotropic filtering mode.");
            antiAliasing = Config.Bind("QualitySettings", "antiAliasing", 0, "Set The AA Filtering option.");
            asyncUploadBufferSize = Config.Bind("QualitySettings", "asyncUploadBufferSize", 4, "Asynchronous texture and mesh data upload provides timesliced async texture and mesh data upload on the render thread with tight control over memory and timeslicing. There are no allocations except for the ones which driver has to do. To read data and upload texture and mesh data, Unity re-uses a ringbuffer whose size can be controlled.Use asyncUploadBufferSize to set the buffer size for asynchronous texture and mesh data uploads. The size is in megabytes. The minimum value is 2 and the maximum value is 512. The buffer resizes automatically to fit the largest texture currently loading. To avoid re-sizing of the buffer, which can incur performance cost, set the value approximately to the size of biggest texture used in the Scene.");
            asyncUploadPersistentBuffer = Config.Bind("QualitySettings", "asyncUploadPersistentBuffer", true, "This flag controls if the async upload pipeline's ring buffer remains allocated when there are no active loading operations. Set this to true, to make the ring buffer allocation persist after all upload operations have completed. If you have issues with excessive memory usage, you can set this to false. This means you reduce the runtime memory footprint, but memory fragmentation can occur. The default value is true.");
            asyncUploadTimeSlice = Config.Bind("QualitySettings", "asyncUploadTimeSlice", 2, "Async texture upload provides timesliced async texture upload on the render thread with tight control over memory and timeslicing. There are no allocations except for the ones which driver has to do. To read data and upload texture data a ringbuffer whose size can be controlled is re-used.Use asyncUploadTimeSlice to set the time-slice in milliseconds for asynchronous texture uploads per frame. Minimum value is 1 and maximum is 33.");
            billboardsFaceCameraPosition = Config.Bind("QualitySettings", "billboardsFaceCameraPosition", true, "If enabled, billboards will face towards camera position rather than camera orientation.");
            lodBias = Config.Bind("QualitySettings", "lodBias", 2, "Global multiplier for the LOD's switching distance.");
            masterTextureLimit = Config.Bind("QualitySettings", "masterTextureLimit", 0, "A texture size limit applied to all textures.");
            maximumLODLevel = Config.Bind("QualitySettings", "maximumLODLevel", 0, "A maximum LOD level. All LOD groups.");
            maxQueuedFrames = Config.Bind("QualitySettings", "maxQueuedFrames", 2, "Maximum number of frames queued up by graphics driver.");
            particleRaycastBudget = Config.Bind("QualitySettings", "particleRaycastBudget", 4096, "Budget for how many ray casts can be performed per frame for approximate collision testing.");
            pixelLightCount = Config.Bind("QualitySettings", "pixelLightCount", 8, "The maximum number of pixel lights that should affect any object.");
            realtimeReflectionProbes = Config.Bind("QualitySettings", "realtimeReflectionProbes", true, "Enables realtime reflection probes.");
            resolutionScalingFixedDPIFactor = Config.Bind("QualitySettings", "resolutionScalingFixedDPIFactor", 1, "In resolution scaling mode, this factor is used to multiply with the target Fixed DPI specified to get the actual Fixed DPI to use for this quality setting.");
            shadowCascade2Split = Config.Bind("QualitySettings", "shadowCascade2Split", 0.3333333f, "The normalized cascade distribution for a 2 cascade setup. The value defines the position of the cascade with respect to Zero.");
            shadowCascade4Split = Config.Bind("QualitySettings", "shadowCascade4Split", new Vector3(0.1f, 0.2f, 0.5f), "The normalized cascade start position for a 4 cascade setup. Each member of the vector defines the normalized position of the coresponding cascade with respect to Zero.");
            shadowCascades = Config.Bind("QualitySettings", "shadowCascades", 4, "Number of cascades to use for directional light shadows.");
            shadowDistance = Config.Bind("QualitySettings", "shadowDistance", 150, "Shadow drawing distance.");
            shadowmaskMode = Config.Bind("QualitySettings", "shadowmaskMode", ShadowmaskMode.Shadowmask, "The rendering mode of Shadowmask.");
            shadowNearPlaneOffset = Config.Bind("QualitySettings", "shadowNearPlaneOffset", 3, "Offset shadow frustum near plane.");
            shadowProjection = Config.Bind("QualitySettings", "shadowProjection", ShadowProjection.StableFit, "Directional light shadow projection.");
            shadowResolution = Config.Bind("QualitySettings", "shadowResolution", ShadowResolution.High, "The default resolution of the shadow maps.");
            shadows = Config.Bind("QualitySettings", "shadows", ShadowQuality.All, "Realtime Shadows type to be used.");
            skinWeights = Config.Bind("QualitySettings", "skinWeights", SkinWeights.TwoBones, "The maximum number of bone weights that can affect a vertex, for all skinned meshes in the project.");
            softParticles = Config.Bind("QualitySettings", "softParticles", true, "Should soft blending be used for particles?");
            softVegetation = Config.Bind("QualitySettings", "softVegetation", true, "Use a two-pass shader for the vegetation in the terrain engine.");
            streamingMipmapsActive = Config.Bind("QualitySettings", "streamingMipmapsActive", true, "Enable automatic streaming of texture mipmap levels based on their distance from all active cameras.");
            streamingMipmapsAddAllCameras = Config.Bind("QualitySettings", "streamingMipmapsAddAllCameras", true, "Process all enabled Cameras for texture streaming (rather than just those with StreamingController components).");
            streamingMipmapsMaxFileIORequests = Config.Bind("QualitySettings", "streamingMipmapsMaxFileIORequests", 1024, "The maximum number of active texture file IO requests from the texture streaming system.");
            streamingMipmapsMaxLevelReduction = Config.Bind("QualitySettings", "streamingMipmapsMaxLevelReduction", 2, "The maximum number of mipmap levels to discard for each texture.");
            streamingMipmapsMemoryBudget = Config.Bind("QualitySettings", "streamingMipmapsMemoryBudget", 512, "The total amount of memory to be used by streaming and non-streaming textures.");
            vSyncCount = Config.Bind("QualitySettings", "vSyncCount", 1, "The VSync Count.");

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);

            SetGraphicsSettings();

        }

        private void Update()
        {
            if (AedenthornUtils.CheckKeyDown(refreshHotkey.Value))
            {
                Config.Reload();
                SetGraphicsSettings();
            }
        }

        private static void SetGraphicsSettings()
        {
            QualitySettings.anisotropicFiltering = (AnisotropicFiltering) anisotropicFiltering.Value;
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
            QualitySettings.shadowmaskMode = (ShadowmaskMode) shadowmaskMode.Value;
            QualitySettings.shadowNearPlaneOffset = shadowNearPlaneOffset.Value;
            QualitySettings.shadowProjection = (ShadowProjection) shadowProjection.Value;
            QualitySettings.shadowResolution = (ShadowResolution) shadowResolution.Value;
            QualitySettings.shadows = (ShadowQuality) shadows.Value;
            QualitySettings.skinWeights = (SkinWeights) skinWeights.Value;
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
