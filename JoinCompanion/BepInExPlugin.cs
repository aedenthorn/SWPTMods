using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace JoinCompanion
{
    [BepInPlugin("aedenthorn.JoinCompanion", "Join Companion", "0.1.2")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;

        public static ConfigEntry<string> modKey;

        public static CharacterCustomization joinedChar;
        public static Furniture joinedFurniture;
        public static Pose joinedPose;

        public static ConfigEntry<int> nexusID;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug.Value)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }

        private void Awake()
        {

            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            isDebug = Config.Bind<bool>("General", "IsDebug", true, "Enable debug logs");

            modKey = Config.Bind<string>("Options", "ModKey", "left shift", "Modifier key to join companion.");

            nexusID = Config.Bind<int>("General", "NexusID", 27, "Nexus mod ID for updates");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Dbgl("Plugin awake");

        }

        [HarmonyPatch(typeof(CharacterCustomization), "Awake")]
        static class CharacterCustomization_Awake_Patch
        {
            static void Prefix(CharacterCustomization __instance)
            {
                if (!modEnabled.Value || __instance._Player)
                    return;

                if (!__instance.gameObject.GetComponent<Interaction>())
                    __instance.gameObject.AddComponent<Interaction>();
            }
        }

        [HarmonyPatch(typeof(Interaction), nameof(Interaction.Interact))]
        static class Interaction_Interact_Patch
        {
            static bool Prefix(Interaction __instance, CharacterCustomization customization)
            {

                if (!modEnabled.Value || !customization._Player)
                    return true;

                if(__instance.GetComponent<Furniture>() && __instance.GetComponent<Furniture>().user && !__instance.GetComponent<Furniture>().user._Player && AedenthornUtils.CheckKeyHeld(modKey.Value))
                {

                    Dbgl($"Interacting with furniture {__instance.transform.name}, companion: {__instance.GetComponent<Furniture>().user.characterName}");

                    joinedChar = __instance.GetComponent<Furniture>().user;

                    joinedFurniture = joinedChar.interactingObject.GetComponent<Furniture>();

                    foreach (Transform transform in joinedFurniture.poses.items)
                    {
                        if (transform && transform.gameObject.activeSelf)
                        {
                            joinedPose = transform.GetComponent<Pose>();
                            break;
                        }
                    }

                    Global.code.uiFreePose.Open();

                    return false;
                }
                if(__instance.GetComponent<CharacterCustomization>() && __instance.gameObject.tag != "D")
                {

                    Dbgl($"Interacting with character {__instance.GetComponent<CharacterCustomization>().characterName}");

                    Global.code.uiFreePose.Open();
                    Global.code.uiFreePose.AddCharacter(__instance.transform);
                    Global.code.uiFreePose.Refresh();

                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(UIFreePose), nameof(UIFreePose.Close))]
        static class UIFreePose_Close_Patch
        {
            static void Postfix(UIFreePose __instance)
            {

                if (!modEnabled.Value || !joinedChar)
                    return;

                Dbgl($"Putting joined character {joinedChar.characterName} back at {joinedFurniture.name}");

                joinedFurniture.InteractWithOnlyPoses(joinedChar);
                if(joinedPose)
                    joinedFurniture.DoPose(joinedPose);

                joinedChar = null;
                joinedFurniture = null;
                joinedPose = null;
            }
        }

        [HarmonyPatch(typeof(UIFreePose), nameof(UIFreePose.Open))]
        static class UIFreePose_Open_Patch
        {
            static void Postfix(UIFreePose __instance)
            {
                if (!modEnabled.Value || !joinedChar)
                    return;



                
                Vector3 position = joinedChar.curInteractionLoc.position;
                var rotation = joinedChar.curInteractionLoc.rotation;

                //joinedFurniture.QuitInteractWithOnlyPoses(joinedChar);
                //Scene.code.SpawnCompanion(joinedChar.transform);

                __instance.AddCharacter(joinedChar.transform);

                joinedChar.transform.position = position;
                joinedChar.transform.rotation = rotation;

                Player.code.transform.position = position + new Vector3(Random.Range(-0.5f, 0.5f), 0f, Random.Range(-0.5f, 0.5f));
                Player.code.transform.rotation = rotation;

                if (joinedPose != null)
                {
                    __instance.selectedCharacter = joinedChar.transform;
                    __instance.PoseButtonClicked(joinedPose);
                    __instance.selectedCharacter = Player.code.transform;
                    __instance.PoseButtonClicked(joinedPose);
                }

                AccessTools.Method(typeof(UIFreePose), "SwitchFreeCamera").Invoke(__instance, new object[] { });


                __instance.Refresh();

            }
        }
    }
}
