using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Newtonsoft.Json;
using RuntimeGizmos;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PoseAnimations
{
    [BepInPlugin("aedenthorn.PoseAnimations", "Pose Animations", "0.1.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<int> nexusID;

        public static ConfigEntry<bool> reverseByDefault;
        public static ConfigEntry<bool> loopByDefault;
        public static ConfigEntry<string> addModKey;
        public static ConfigEntry<string> deleteModKey;
        public static ConfigEntry<string> deleteFrameModKey;
        public static ConfigEntry<string> setReverseKey;
        public static ConfigEntry<string> setLoopKey;
        public static ConfigEntry<string> catName;
        public static ConfigEntry<float> maxDistancePerFrame;
        public static ConfigEntry<float> maxRotationPerFrame;

        public static Dictionary<string, string> animationDict = new Dictionary<string, string>();
        public static GameObject posesGameObject;
        public static Transform addNewAnimationButton;
        public static Dictionary<CharacterCustomization,PoseAnimationData> currentlyPosing = new Dictionary<CharacterCustomization, PoseAnimationData>();

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
            
            reverseByDefault = Config.Bind<bool>("Options", "ReverseByDefault", false, "Set new animations to play in reverse after playing forward by default");
            loopByDefault = Config.Bind<bool>("Options", "LoopByDefault", false, "Set new animations to loop by default");
            catName = Config.Bind<string>("Options", "CatName", "Animations", "Animations category name in UI Free Pose");
            maxDistancePerFrame = Config.Bind<float>("Options", "MaxDistancePerFrame", 1, "Maximum distance moved in metres per bone per frame");
            maxRotationPerFrame = Config.Bind<float>("Options", "MaxRotationPerFrame", 1, "Maximum rotation in degrees per bone per frame");

            addModKey = Config.Bind<string>("HotKeys", "AddModKey", "left shift", "Modifier key to add current pose to selected animation");
            deleteModKey = Config.Bind<string>("HotKeys", "DeleteModKey", "left alt", "Modifier key to delete selected animation");
            deleteFrameModKey = Config.Bind<string>("HotKeys", "DeleteFrameModKey", "left ctrl", "Modifier key to delete last frame from selected animation");
            setReverseKey = Config.Bind<string>("HotKeys", "SetReverseKey", "r", "Key to toggle reversing for hovered animation");
            setLoopKey = Config.Bind<string>("HotKeys", "SetLoopKey", "l", "Key to toggle looping for hovered animation");
            
            //nexusID = Config.Bind<int>("General", "NexusID", 94, "Nexus mod ID for updates");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Dbgl("Plugin awake");
            posesGameObject = new GameObject();
            posesGameObject.name = "PoseAnimations";
            DontDestroyOnLoad(posesGameObject);
        }

        private void Update()
        {
            if (!modEnabled.Value || !Player.code || !Global.code.uiFreePose.enabled)
                return;
            
            if(Global.code.uiFreePose.curCategory == catName.Value && (AedenthornUtils.CheckKeyDown(setReverseKey.Value) || AedenthornUtils.CheckKeyDown(setLoopKey.Value)))
            {
                PointerEventData eventData = new PointerEventData(EventSystem.current)
                {
                    position = Input.mousePosition
                };
                List<RaycastResult> raycastResults = new List<RaycastResult>();
                EventSystem.current.RaycastAll(eventData, raycastResults);

                foreach (RaycastResult rcr in raycastResults)
                {
                    if (rcr.gameObject.layer == LayerMask.NameToLayer("UI") && rcr.gameObject.GetComponent<PoseIcon>())
                    {
                        if (!animationDict.ContainsKey(rcr.gameObject.name))
                            break;
                        PoseAnimationData pad = JsonConvert.DeserializeObject<PoseAnimationData>(File.ReadAllText(animationDict[rcr.gameObject.name]));

                        if (AedenthornUtils.CheckKeyDown(setReverseKey.Value))
                        {
                            pad.reverse = !pad.reverse;
                            foreach(var kvp in currentlyPosing)
                            {
                                if (kvp.Value.name == pad.name)
                                    kvp.Value.reverse = pad.reverse;
                            }
                            Global.code.uiCombat.ShowHeader(Localization.GetContent("Reverse: "+pad.reverse, new object[0]));
                        }
                        else
                        {
                            pad.loop = !pad.loop;
                            foreach (var kvp in currentlyPosing)
                            {
                                if (kvp.Value.name == pad.name)
                                    kvp.Value.loop = pad.loop;
                            }
                            Global.code.uiCombat.ShowHeader(Localization.GetContent("Loop: " + pad.loop, new object[0]));
                        }

                        File.WriteAllText(Path.Combine(AedenthornUtils.GetAssetPath(context), rcr.gameObject.name + ".json"), JsonConvert.SerializeObject(pad, Formatting.Indented));
                        break;
                    }
                }
            }

            if (currentlyPosing.Any())
            {
                for (int i = currentlyPosing.Keys.Count - 1; i >= 0; i--)
                {
                    PoseAnimationData pad = currentlyPosing[currentlyPosing.Keys.ElementAt(i)];
                    pad.deltaTime += Time.deltaTime;

                    if (pad.deltaTime >= pad.rate)
                    {
                        int nextIndex;
                        pad.deltaTime = 0;

                        if (pad.reversing)
                        {
                            if (pad.currentIndex <= 0)
                            {
                                if (pad.loop)
                                {
                                    //Dbgl("finished reversing");
                                    pad.reversing = false;
                                    nextIndex = 1;
                                }
                                else
                                {
                                    currentlyPosing.Remove(currentlyPosing.Keys.ElementAt(i));
                                    continue;
                                }
                            }
                            else
                            {
                                nextIndex = pad.currentIndex - 1;
                            }
                        }
                        else if (pad.currentIndex >= pad.frames.Count - 1)
                        {
                            if (pad.reverse)
                            {
                                //Dbgl("reversing");
                                pad.reversing = true;
                                nextIndex = pad.frames.Count - 2;
                            }
                            else if (pad.loop)
                            {
                                //Dbgl("looping");
                                nextIndex = 0;
                            }
                            else
                            {
                                currentlyPosing.Remove(currentlyPosing.Keys.ElementAt(i));
                                continue;
                            }
                        }
                        else
                            nextIndex = pad.currentIndex + 1;

                        List<MyPoseData> pdl = pad.frames[pad.currentIndex];
                        foreach (MyPoseData pd in pdl)
                        {
                            try
                            {
                                pad.bones[pd.boneName].localPosition = pd.BonePos;
                                pad.bones[pd.boneName].localRotation = pd.BoneRotation;
                            }
                            catch
                            {
                            }
                        }
                        pad.currentIndex = nextIndex;
                    }
                }
            }

        }


        [HarmonyPatch(typeof(RM), "LoadResources")]
        static class RM_LoadResources_Patch
        {
            static void Postfix(RM __instance)
            {
                if (!modEnabled.Value)
                    return;

                foreach (string path in Directory.GetFiles(AedenthornUtils.GetAssetPath(context, true), "*.json"))
                {
                    try
                    {
                        PoseAnimationData data = JsonConvert.DeserializeObject<PoseAnimationData>(File.ReadAllText(path));
                        data.name = Path.GetFileNameWithoutExtension(path);
                        AddNewAnimation(data, path);
                        Dbgl($"Added pose animations {data.name} ");
                    }
                    catch(Exception ex)
                    {
                        Dbgl($"Error loading pose animation from file {path}:\n\n{ex}");

                    }
                }
                Dbgl($"Added {animationDict.Count} pose animations from files");

                addNewAnimationButton = Instantiate(RM.code.allFreePoses.items[0], posesGameObject.transform);
                addNewAnimationButton.name = "AddNewAnimation";
                Texture2D addIcon = new Texture2D(1, 1);
                try
                {
                    addIcon.LoadImage(File.ReadAllBytes(Path.Combine(AedenthornUtils.GetAssetPath(context), "save_button.png")));
                }
                catch { }
                addNewAnimationButton.GetComponent<Pose>().icon = addIcon;
                addNewAnimationButton.GetComponent<Pose>().categoryName = catName.Value;

            }
        }
        private static void AddNewAnimation(PoseAnimationData data, string path)
        {
            animationDict[data.name] = path;
            Transform t = Instantiate(RM.code.allFreePoses.items[0], posesGameObject.transform);
            t.GetComponent<Pose>().categoryName = catName.Value;
            try
            {
                Texture2D icon = new Texture2D(1, 1);
                icon.LoadImage(File.ReadAllBytes(Path.Combine(AedenthornUtils.GetAssetPath(context), data.name + ".png")));
                t.GetComponent<Pose>().icon = icon;
            }
            catch { }
            t.name = data.name;
            RM.code.allFreePoses.AddItem(t);
        }

         [HarmonyPatch(typeof(UIFreePose), nameof(UIFreePose.Refresh))]
        public static class UIFreePose_Refresh_Patch
        {
            public static void Postfix(UIFreePose __instance)
            {
                if (!modEnabled.Value || __instance.curCategory != catName.Value)
                    return;

                Transform t = Instantiate(__instance.poseIconPrefab);
                t.SetParent(__instance.poseIconGroup);
                t.localScale = Vector3.one;
                t.name = "AddNewAnimation";
                t.GetComponent<PoseIcon>().Initiate(addNewAnimationButton);

                for(int i = 0; i < __instance.poseIconGroup.childCount; i++)
                {
                    Transform poseIconT = __instance.poseIconGroup.GetChild(i);
                    __instance.poseIconGroup.GetChild(i).GetComponent<Button>().onClick = new Button.ButtonClickedEvent();
                    __instance.poseIconGroup.GetChild(i).GetComponent<Button>().onClick.AddListener(delegate () { PoseButtonClicked(poseIconT); });

                }
            }
        }
        [HarmonyPatch(typeof(UIFreePose), nameof(UIFreePose.Open))]
        public static class UIFreePose_Open_Patch
        {
            public static void Postfix(UIFreePose __instance)
            {
                if (!modEnabled.Value || __instance.categoryDropdown.options.Exists(d => d.text == catName.Value))
                    return;

                __instance.categoryDropdown.options.Add(new Dropdown.OptionData(catName.Value));
            }
        }

        public static void PoseButtonClicked(Transform button)
        {
            if (!modEnabled.Value)
                return;

            string buttonName = button.name;

            if (buttonName == "AddNewAnimation")
            {
                int idx = 1;
                while (animationDict.ContainsKey("NewPoseAnimation" + idx))
                {
                    idx++;
                }

                string animationName = "NewPoseAnimation" + idx;

                Dbgl($"Creating new pose animation {animationName}");

                PoseAnimationData pad = new PoseAnimationData()
                {
                    name = animationName,
                    frames = new List<List<MyPoseData>>(),
                    reverse = reverseByDefault.Value,
                    loop = loopByDefault.Value
                };
                List<MyPoseData> poseDatas = new List<MyPoseData>();
                foreach (Transform t in Global.code.uiFreePose.selectedCharacter.GetComponent<CharacterCustomization>().bonesNeedSave)
                {
                    MyPoseData item = new MyPoseData(FixName(t.name), t.localPosition, t.localRotation);
                    poseDatas.Add(item);
                }
                Dbgl($"Adding {poseDatas.Count} bones to first frame");
                pad.frames.Add(poseDatas);

                AddNewAnimation(pad, Path.Combine(AedenthornUtils.GetAssetPath(context), pad.name+".json"));
                File.WriteAllText(Path.Combine(AedenthornUtils.GetAssetPath(context), animationName + ".json"), JsonConvert.SerializeObject(pad, Formatting.Indented));
                Global.code.uiFreePose.Refresh();
            }
            else if (AedenthornUtils.CheckKeyHeld(deleteModKey.Value))
            {
                File.Delete(animationDict[buttonName]);
                animationDict.Remove(buttonName);
                RM.code.allFreePoses.items.Remove(button.GetComponent<PoseIcon>().pose.transform);
                Global.code.uiFreePose.Refresh();
            }
            else if (AedenthornUtils.CheckKeyHeld(deleteFrameModKey.Value))
            {
                PoseAnimationData pad = JsonConvert.DeserializeObject<PoseAnimationData>(File.ReadAllText(animationDict[buttonName]));
                if(pad.frames.Count > 1)
                {
                    Dbgl($"Removing last frame from animation {buttonName}");
                    pad.frames.RemoveAt(pad.frames.Count - 1);
                    File.WriteAllText(Path.Combine(AedenthornUtils.GetAssetPath(context), pad.name + ".json"), JsonConvert.SerializeObject(pad, Formatting.Indented));
                }
            }
            else if (AedenthornUtils.CheckKeyHeld(addModKey.Value))
            {
                Dbgl($"Adding current pose to animation {buttonName}");

                if (currentlyPosing.ContainsKey(Global.code.uiFreePose.selectedCharacter.GetComponent<CharacterCustomization>()))
                {
                    Dbgl($"Stop animation first!");
                    return;
                }


                int totalFrames = 1;
                Dictionary<Transform, MyPoseData> modifiedBones = new Dictionary<Transform, MyPoseData>();

                PoseAnimationData pad = JsonConvert.DeserializeObject<PoseAnimationData>(File.ReadAllText(animationDict[buttonName]));
                foreach(Transform bone in Global.code.uiFreePose.selectedCharacter.GetComponent<CharacterCustomization>().bonesNeedSave)
                {
                    MyPoseData boneData = new MyPoseData(FixName(bone.name), bone.localPosition, bone.localRotation);

                    int idx = pad.frames.Count - 1;

                    while (idx >= 0)
                    {
                        var oldFrameBoneData = pad.frames[idx--].Find(f => f.boneName == bone.name);
                        if (oldFrameBoneData == null)
                            continue;
                        if (oldFrameBoneData.BonePos == boneData.BonePos && oldFrameBoneData.BoneRotation == boneData.BoneRotation)
                        {
                            break;
                        }
                        modifiedBones.Add(bone, oldFrameBoneData);
                        //Dbgl($"frame {idx + 1}/{pad.frames.Count}, bone {boneData.boneName} total distance {Vector3.Distance(oldFrameBoneData.BonePos, boneData.BonePos)}, total rotation {Quaternion.Angle(oldFrameBoneData.BoneRotation, boneData.BoneRotation)}");
                        if (Vector3.Distance(oldFrameBoneData.BonePos, boneData.BonePos) > maxDistancePerFrame.Value || Quaternion.Angle(oldFrameBoneData.BoneRotation, boneData.BoneRotation) > maxRotationPerFrame.Value)
                        {
                            if (Vector3.Distance(oldFrameBoneData.BonePos, boneData.BonePos) > maxDistancePerFrame.Value)
                            {
                                int reqFrames = Mathf.CeilToInt(Vector3.Distance(oldFrameBoneData.BonePos, boneData.BonePos) / maxDistancePerFrame.Value);
                                if (reqFrames > totalFrames)
                                    totalFrames = reqFrames;
                            }

                            if (Quaternion.Angle(oldFrameBoneData.BoneRotation, boneData.BoneRotation) > maxRotationPerFrame.Value)
                            {
                                int reqFrames = Mathf.CeilToInt(Quaternion.Angle(oldFrameBoneData.BoneRotation, boneData.BoneRotation) / maxRotationPerFrame.Value);
                                if (reqFrames > totalFrames)
                                    totalFrames = reqFrames;
                            }
                        }
                        break;
                    }
                }
                for (int i = 1; i <= totalFrames; i++)
                {
                    List<MyPoseData> poseDatas = new List<MyPoseData>();
                    foreach (var kvp in modifiedBones)
                    {
                        Transform bone = kvp.Key;
                        MyPoseData oldFrameBoneData = kvp.Value;
                        MyPoseData boneData = new MyPoseData(FixName(bone.name), bone.localPosition, bone.localRotation);

                        boneData.BonePos = Vector3.Lerp(oldFrameBoneData.BonePos, boneData.BonePos, i / (float)totalFrames);
                        boneData.BoneRotation = Quaternion.Lerp(oldFrameBoneData.BoneRotation, boneData.BoneRotation, i / (float)totalFrames);
                        poseDatas.Add(boneData);
                    }
                    pad.frames.Add(poseDatas);
                }
                Dbgl($"Moved {modifiedBones.Count} bones in {totalFrames} frames. Total frames {pad.frames}");

                File.WriteAllText(Path.Combine(AedenthornUtils.GetAssetPath(context), pad.name + ".json"), JsonConvert.SerializeObject(pad, Formatting.Indented));
            }
            else
            {
                Global.code.freeCamera.GetComponent<FreelookCamera>().LetRuntimeTransformRun();
                TransformGizmo.transformGizmo_.showTempGroup = false;
                foreach (Transform transform in TransformGizmo.transformGizmo_.tempTransform)
                {
                    transform.gameObject.SetActive(false);
                }
                TransformGizmo.transformGizmo_.selectNow = null;

                PoseAnimationData pad = JsonConvert.DeserializeObject<PoseAnimationData>(File.ReadAllText(animationDict[buttonName]));
                if (currentlyPosing.ContainsKey(Global.code.uiFreePose.selectedCharacter.GetComponent<CharacterCustomization>()))
                {
                    string oldName = currentlyPosing[Global.code.uiFreePose.selectedCharacter.GetComponent<CharacterCustomization>()].name;
                    Dbgl($"Stopping animation {oldName} for {Global.code.uiFreePose.selectedCharacter.name}");
                    currentlyPosing.Remove(Global.code.uiFreePose.selectedCharacter.GetComponent<CharacterCustomization>());
                    if (oldName == buttonName)
                        return;
                }
                Dbgl($"Setting {Global.code.uiFreePose.selectedCharacter.name} to animation {buttonName}");
                pad.bones = new Dictionary<string, Transform>();
                foreach (Transform t in Global.code.uiFreePose.selectedCharacter.GetComponent<CharacterCustomization>().bonesNeedSave)
                    pad.bones[FixName(t.name)] = t;
                currentlyPosing.Add(Global.code.uiFreePose.selectedCharacter.GetComponent<CharacterCustomization>(), pad);
            }
        }

        public static string FixName(string name)
        {
            if (name.EndsWith("eyesRoot"))
                name = "eyesRoot";
            if (name.EndsWith("head parent"))
                name = "head parent";
            if (name.EndsWith("head target"))
                name = "head target";
            return name;
        }
    }
}
