using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace MerchantEdit
{
    [BepInPlugin("aedenthorn.MerchantEdit", "Merchant Edit", "0.1.2")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<bool> removeMaidOutfit;
        public static ConfigEntry<string> maidModel;
        public static ConfigEntry<string> fatOrcModel;
        public static ConfigEntry<string> orcFighterModel;
        public static ConfigEntry<string> littleDemonModel;

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

            maidModel = Config.Bind<string>("Options", "MaidModel", "Bartender", "Model to use for Maid");
            fatOrcModel = Config.Bind<string>("Options", "FatOrcModel", "Fat_Orc", "Model to use for armour merchant");
            orcFighterModel = Config.Bind<string>("Options", "OrcFighterModel", "Ork_fighter", "Model to use for weapon merchant");
            littleDemonModel = Config.Bind<string>("Options", "LittleDemonModel", "Little_Demon", "Model to use for black market merchant");
            removeMaidOutfit = Config.Bind<bool>("Options", "RemoveMaidOutfit", false, "Remove the maid's clothing.");

            nexusID = Config.Bind<int>("General", "NexusID", 6, "Nexus mod ID for updates");

            SceneManager.sceneLoaded += SceneManager_sceneLoaded;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), Info.Metadata.GUID);
            Dbgl("Plugin awake");

        }

        private void SceneManager_sceneLoaded(UnityEngine.SceneManagement.Scene arg0, LoadSceneMode arg1)
        {
            if (!modEnabled.Value)
                return;
            if (arg0.name == "The Palace")
            {
                Dbgl("Altering merchants. Available companion presets:");
                
                foreach (Transform t in RM.code.allCompanions.items)
                    Dbgl($"\t{t.name}");

                Dictionary<string, GameObject> modelObjects = new Dictionary<string, GameObject>()
                {
                    { "Bartender", GameObject.Find("/Scene/Interactions/Bartender") },
                    { "Fat_Orc", GameObject.Find("/Scene/Statics/Bar/Fat_Orc") },
                    { "Ork_fighter", GameObject.Find("/Scene/Statics/Smith Model")},
                    { "Little_Demon", GameObject.Find("/Scene/Statics/Bar/Little Demon Skin3 Base mesh") },
                };

                if (!modelObjects["Bartender"])
                    Dbgl("No bartender");
                if (!modelObjects["Fat_Orc"])
                    Dbgl("No fat orc");
                if (!modelObjects["Ork_fighter"])
                    Dbgl("No orc fighter");
                if (!modelObjects["Little_Demon"])
                    Dbgl("No demon");

                if (removeMaidOutfit.Value)
                {
                    modelObjects["Bartender"].transform.Find("Morganas Desire").gameObject.SetActive(false);
                }
                if (maidModel.Value != "Bartender")
                {
                    SwitchModel(modelObjects, maidModel.Value, "Bartender");
                }
                if (fatOrcModel.Value != "Fat_Orc")
                {
                    SwitchModel(modelObjects, fatOrcModel.Value, "Fat_Orc");
                }
                if (orcFighterModel.Value != "Ork_fighter")
                {
                    SwitchModel(modelObjects, orcFighterModel.Value, "Ork_fighter");

                }
                if (littleDemonModel.Value != "Little_Demon")
                {
                    SwitchModel(modelObjects, littleDemonModel.Value, "Little_Demon");

                }
            }
        }

        private void SwitchModel(Dictionary<string, GameObject> modelObjects, string custom, string vanilla)
        {
            GameObject go = null;


            if (modelObjects.ContainsKey(custom))
            {
                Dbgl($"Using static model {custom} for {vanilla}");

                go = Instantiate(modelObjects[custom], modelObjects[vanilla].transform.parent);
            }
            else if (RM.code.allCompanions.items.Exists(t => t.name == custom))
            {

                Dbgl($"Using companion model {custom} for {vanilla}");

                Transform companion = RM.code.allCompanions.items.First(t => t.name == custom);
                CharacterCustomization ccc = companion.GetComponent<CharacterCustomization>();

                go = Instantiate(modelObjects["Bartender"]);
                Destroy(go.transform.Find("Morganas Desire").gameObject);
                var rb = go.AddComponent<Rigidbody>();
                rb.constraints = RigidbodyConstraints.FreezeAll;
                var cc = go.AddComponent<CharacterCustomization>();
                cc.isDisplay = true;
                cc.showArmor = false;
                
                cc.rh = ccc.rh;
                cc.anim = go.AddComponent<Animator>();

                //cc._ID = companion.GetComponent<CharacterCustomization>()._ID;
                //cc.anim = companion.GetComponent<CharacterCustomization>().anim;
                cc.lipstickModel = new GameObject();
                //DestroyImmediate(go.transform.Find("Genesis8Female").Find("Genesis8Female.Shape").gameObject);
                //DestroyImmediate(go.transform.Find("Genesis8Female").Find("Genesis8FemaleEyelashes.Shape").gameObject);
                cc.body = go.transform.Find("Genesis8Female").Find("Genesis8Female.Shape").GetComponent<SkinnedMeshRenderer>();
                cc.eyelash = go.transform.Find("Genesis8Female").Find("Genesis8FemaleEyelashes.Shape").GetComponent<SkinnedMeshRenderer>();
                
                cc.locatorHelmet = go.transform.Find("Genesis8Female").Find("hip").GetComponentsInChildren<CustomizationItem>().First(i => i.name.StartsWith("hair")).transform.parent;
                cc.wingLoc = go.transform.Find("Genesis8Female").Find("hip").GetComponentInChildren<Wing>().transform.parent;
                
                Destroy(go.transform.Find("Genesis8Female").Find("hip").GetComponentInChildren<Wing>().gameObject);
                Destroy(go.transform.Find("Genesis8Female").Find("hip").GetComponentsInChildren<CustomizationItem>().First(i => i.name.StartsWith("hair")).gameObject);
                Destroy(go.transform.Find("Genesis8Female").Find("hip").GetComponentsInChildren<CustomizationItem>().First(i => i.name.StartsWith("horn")).gameObject);

                go.transform.SetParent(modelObjects[vanilla].transform.parent);

                cc.SetToPreset(companion.GetComponent<CharacterCustomization>().preset);

                cc.leggings = ccc.leggings;
                cc.stockings = ccc.stockings;
                cc.bra = ccc.bra;
                cc.lingerieGloves = ccc.lingerieGloves;
                cc.panties = ccc.panties;
                cc.suspenders = ccc.suspenders;
                cc.heels = ccc.heels;
                cc.RefreshEquipment();


                //go.transform.Find("Genesis8Female").Find("Genesis8Female.Shape").GetComponent<SkinnedMeshRenderer>().sharedMesh = RM.code.allCompanions.items.First(t => t.name == custom).Find("Genesis8Female").Find("Genesis8Female.Shape").GetComponent<SkinnedMeshRenderer>().sharedMesh;
            }
            else
            {
                Dbgl($"Error finding model {custom} for {vanilla}");
                foreach (Transform t in RM.code.allCompanions.items)
                    Dbgl($"companion: {t.name}");
                return;
            }
            go.transform.position = modelObjects[vanilla].transform.position;
            go.transform.rotation = modelObjects[vanilla].transform.rotation;
            go.SetActive(true);
            modelObjects[vanilla].SetActive(false);
        }

        [HarmonyPatch(typeof(CharacterCustomization), "FixedUpdate")]
        static class CharacterCustomization_FixedUpdate_Patch
        {
            static bool Prefix(CharacterCustomization __instance)
            {
                if (!modEnabled.Value || !__instance.gameObject.name.StartsWith("Bartender"))
                    return true;
                return false;
            }
        }

        [HarmonyPatch(typeof(CharacterCustomization), "UpdateStats")]
        static class CharacterCustomization_UpdateStats_Patch
        {
            static bool Prefix(CharacterCustomization __instance)
            {
                if (!modEnabled.Value || !__instance.gameObject.name.StartsWith("Bartender"))
                    return true;
                return false;
            }
        }

        [HarmonyPatch(typeof(CharacterCustomization), "RefreshClothesVisibility")]
        static class CharacterCustomization_RefreshClothesVisibility_Patch
        {
            static void Prefix(CharacterCustomization __instance)
            {
                if (!modEnabled.Value || !__instance.gameObject.name.StartsWith("Bartender"))
                    return;

                Dbgl($"anim {__instance.anim == null}");
                Dbgl($"body {__instance.body == null}");
            }
        }

    }
}
