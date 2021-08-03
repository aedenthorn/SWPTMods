using BepInEx;
using HarmonyLib;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace CustomMainMenu
{
    public partial class BepInExPlugin : BaseUnityPlugin
    {

        private static void LoadBackgroundImageFiles()
        {
            if (!modEnabled.Value)
                return;
            backgroundImages = Directory.GetFiles(AedenthornUtils.GetAssetPath(typeof(BepInExPlugin).Namespace)).ToList().FindAll(s => s.EndsWith(".png") || s.EndsWith(".jpg") || s.EndsWith(".jpeg") || s.EndsWith(".bmp"));
            AedenthornUtils.ShuffleList(backgroundImages);
        }

        private static void LoadBackgroundImage()
        {
            if (!modEnabled.Value)
                return;
            MeshRenderer mr = GameObject.Find("Inventory Scene/hide (2)/Plane").GetComponent<MeshRenderer>();
            string bkgPath = null;
            if (backgroundName.Value.Trim().Length != 0)
            {
                bkgPath = Path.Combine(AedenthornUtils.GetAssetPath(typeof(BepInExPlugin).Namespace), backgroundName.Value.Trim());
            }
            else
            {
                if (Directory.GetFiles(AedenthornUtils.GetAssetPath(typeof(BepInExPlugin).Namespace)).ToList().FindAll(s => s.EndsWith(".png") || s.EndsWith(".jpg") || s.EndsWith(".jpeg") || s.EndsWith(".bmp")).Count != backgroundImages.Count)
                {
                    LoadBackgroundImageFiles();
                    lastBackgroundUpdate = 0;
                    lastBackgroundIndex = 0;
                }
                lastBackgroundIndex++;
                lastBackgroundIndex %= backgroundImages.Count;

                bkgPath = backgroundImages[lastBackgroundIndex];

            }
            if (bkgPath != null && File.Exists(bkgPath) && mr.material.GetTexture("_UnlitColorMap").name != Path.GetFileName(bkgPath))
            {
                Dbgl($"switching background to {Path.GetFileName(bkgPath)}");
                Texture2D background = new Texture2D(1, 1);
                background.LoadImage(File.ReadAllBytes(bkgPath));
                background.name = Path.GetFileName(bkgPath);
                mr.material.SetTexture("_UnlitColorMap", background);
            }
        }


        private static void LoadCustomLightData()
        {
            if (!modEnabled.Value)
                return;


            GameObject.Find("Inventory Scene/hide (2)/Point Light (13)").transform.localPosition = lightPositions[0] + light1Position.Value;
            GameObject.Find("Inventory Scene/hide (2)/Point Light (20)").transform.localPosition = lightPositions[1] + light2Position.Value;
            GameObject.Find("Inventory Scene/hide (2)/Point Light (21)").transform.localPosition = lightPositions[2] + light3Position.Value;
            GameObject.Find("Inventory Scene/hide (2)/Point Light (22)").transform.localPosition = lightPositions[3] + light4Position.Value;
        }
        private static void LoadCustomCharacter()
        {
            if (!modEnabled.Value)
                return;

            if(charOrPresetName.Value.Trim().Length == 0)
            {
                Dbgl($"no character or preset set.");
                kiraCharacter.gameObject.SetActive(true);
                mmCharacter?.gameObject.SetActive(false);
                return;
            }
            if (saveFolder.Value.Trim().Length > 0)
            {
                if(!ES2.Exists(saveFolder.Value.Trim() + "/" + charOrPresetName.Value + ".txt"))
                {
                    Dbgl($"character {charOrPresetName.Value} for save {saveFolder.Value} not found.");
                    kiraCharacter.gameObject.SetActive(true);
                    mmCharacter?.gameObject.SetActive(false);
                    return;
                }
            }
            else if (!ES2.Exists("Character Presets/" + charOrPresetName.Value + "/CharacterPreset.txt"))
            {
                Dbgl($"preset {charOrPresetName.Value} not found.");
                kiraCharacter.gameObject.SetActive(true);
                mmCharacter?.gameObject.SetActive(false);
                return;
            }

            if (mmCharacter == null)
            {

                Player.code = new Player();

                Transform template;
                if (saveFolder.Value.Trim().Length > 0 && charOrPresetName.Value.Trim() != "Player")
                {
                    template = RM.code.allCompanions.GetItemWithName(charOrPresetName.Value.Trim());
                }
                else
                    template = RM.code.allCompanions.GetItemWithName("Kira");

                if (!template)
                {
                    Dbgl($"Error loading customization {(saveFolder.Value.Trim().Length > 0 ? saveFolder.Value.Trim() : "preset")} {charOrPresetName.Value}");
                    return;
                }

                Global.code = new Global();
                Global.code.uiPose = new GameObject().AddComponent<UIPose>();
                Global.code.uiPose.gameObject.SetActive(false);
                Global.code.uiCustomization = new GameObject().AddComponent<UICustomization>();
                Global.code.uiPose.gameObject.SetActive(false);

                Player.code.chestWidthIndex = template.GetComponent<CharacterCustomization>().body.sharedMesh.GetBlendShapeIndex("Genesis8Female__PBMRibcageSize");
                Player.code.stomachDepthIndex = template.GetComponent<CharacterCustomization>().body.sharedMesh.GetBlendShapeIndex("Genesis8Female__mine stomach depth");
                Player.code.nipplesLargeIndex = template.GetComponent<CharacterCustomization>().body.sharedMesh.GetBlendShapeIndex("Genesis8Female__PBMNipplesLarge");
                Player.code.nipplesDepthIndex = template.GetComponent<CharacterCustomization>().body.sharedMesh.GetBlendShapeIndex("Genesis8Female__PBMNipplesDepth");

                //Dbgl($"new player: npl {Player.code?.nipplesLargeIndex}, npd {Player.code?.nipplesDepthIndex}");

                mmCharacter = Instantiate(template, GameObject.Find("Kira").transform.parent);
                mmCharacter.name = charOrPresetName.Value;

                mmCharacter.GetComponent<CharacterCustomization>().isDisplay = true;

                if (mmCharacter.GetComponent<Companion>())
                {
                    Destroy(mmCharacter.GetComponent<Companion>());
                }
                if (mmCharacter.GetComponent<ThirdPersonCharacter>())
                {
                    Destroy(mmCharacter.GetComponent<ThirdPersonCharacter>());
                }
                if (mmCharacter.GetComponent<NavMeshAgent>())
                {
                    mmCharacter.GetComponent<NavMeshAgent>().enabled = false;
                }

                if (mmCharacter.GetComponent<CharacterCustomization>().weapon)
                {
                    mmCharacter.GetComponent<CharacterCustomization>().weapon.gameObject.SetActive(false);
                }
                if (mmCharacter.GetComponent<CharacterCustomization>().weapon2)
                {
                    mmCharacter.GetComponent<CharacterCustomization>().weapon2.gameObject.SetActive(false);
                }
                if (mmCharacter.GetComponent<CharacterCustomization>().shield)
                {
                    mmCharacter.GetComponent<CharacterCustomization>().shield.gameObject.SetActive(false);
                }


                Destroy(mmCharacter.GetComponent<Interaction>());
                Destroy(mmCharacter.GetComponent<ID>());

                mmCharacter.GetComponent<Animator>().runtimeAnimatorController = RM.code.combatController;
                //mmCharacter.GetComponent<Animator>().avatar = RM.code.flatFeetAvatar;

                //mmCharacter.GetComponent<Animator>().cullingMode = AnimatorCullingMode.AlwaysAnimate;
                mmCharacter.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
                //mmCharacter.GetComponent<Rigidbody>().isKinematic = true;
                //mmCharacter.GetComponent<Collider>().enabled = true;
                //mmCharacter.GetComponent<Animator>().applyRootMotion = false;
                mmCharacter.gameObject.SetActive(true);
            }
            else
            {
                mmCharacter.gameObject.SetActive(true);
            }

            CharacterCustomization customization = mmCharacter.GetComponent<CharacterCustomization>();

            if(saveFolder.Value.Trim().Length > 0)
            {
                Dbgl($"Loading character {charOrPresetName.Value} from save {saveFolder.Value}.");

                Mainframe.code.foldername = saveFolder.Value;
                AccessTools.Method(typeof(Mainframe), "LoadCharacterCustomization").Invoke(Mainframe.code, new object[] { customization } );
            }
            else
            {

                Dbgl($"Loading preset {charOrPresetName.Value}.");

                if (RM.code.allItems.GetItemWithName(armorItem.Value))
                {
                    Dbgl($"Adding armor {armorItem.Value}.");
                    if (customization.armor)
                        customization.armor.gameObject.SetActive(false);
                    customization.AddItem(Utility.Instantiate(RM.code.allItems.GetItemWithName(armorItem.Value)), "armor");
                    customization.armor.gameObject.SetActive(true);
                }
                Mainframe.code.LoadCharacterPreset(customization, charOrPresetName.Value);
            }

            mmCharacter.transform.position = new Vector3(88.95f, 83.65f, 94.652f) + characterPosition.Value;
            mmCharacter.transform.eulerAngles = kiraCharacter.rotation.eulerAngles + characterRotation.Value;
            kiraCharacter.gameObject.SetActive(false);

        }

        private static void LoadPoseData()
        {
            if (!modEnabled.Value || mmCharacter == null)
                return;

            CharacterCustomization customization = mmCharacter.GetComponent<CharacterCustomization>();

            Pose code = null;
            if (poseName.Value.Trim().Length > 0)
            {
                try
                {
                    code = RM.code.allFreePoses.GetItemWithName(poseName.Value).GetComponent<Pose>();
                }
                catch
                {
                    code = RM.code.allFreePoses.items.FindAll(t => t.GetComponent<Pose>().categoryName == "Standing")[0].GetComponent<Pose>();
                }
            }
            else
            {
                var poses = RM.code.allFreePoses.items.FindAll(t => t.GetComponent<Pose>().categoryName == "Standing");
                code = poses[Random.Range(0, poses.Count - 1)].GetComponent<Pose>();
            }

            if (code != null)
            {
                Dbgl($"pose: {code.name}");
                customization.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
                customization.anim.avatar = RM.code.genericAvatar;
                context.StartCoroutine(ChangePose(code));
            }
        }

        private static IEnumerator ChangePose(Pose code)
        {
            yield return new WaitForEndOfFrame();
            mmCharacter.GetComponent<CharacterCustomization>().anim.runtimeAnimatorController = code.controller;
        }
    }
}
