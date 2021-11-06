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
    [BepInPlugin("aedenthorn.PoseAnimations", "Pose Animations", "0.7.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<int> nexusID;

        public static ConfigEntry<string> addModKey;
        public static ConfigEntry<string> deleteFrameModKey;
        public static ConfigEntry<string> resetStartPosKey;
        public static ConfigEntry<string> setReverseKey;
        public static ConfigEntry<string> setLoopKey;
        public static ConfigEntry<string> saveKey;
        public static ConfigEntry<string> fromFileKey;
        public static ConfigEntry<string> deleteAnimationKey;

        public static ConfigEntry<bool> reverseByDefault;
        public static ConfigEntry<bool> loopByDefault;
        public static ConfigEntry<string> catName;
        public static ConfigEntry<float> minBoneRotation;
        public static ConfigEntry<float> maxBoneRotationPerFrame;
        public static ConfigEntry<float> maxBoneMovementPerFrame;
        public static ConfigEntry<float> maxModelRotationPerFrame;
        public static ConfigEntry<float> maxModelMovementPerFrame;

        public static Dictionary<string, PoseAnimationData> animationDict = new Dictionary<string, PoseAnimationData>();
        public static GameObject posesGameObject;
        public static Transform addNewAnimationButton;
        public static Dictionary<CharacterCustomization,PoseAnimationInstance> currentlyPosing = new Dictionary<CharacterCustomization, PoseAnimationInstance>();

        public static List<string> ignoreBones = new List<string>()
        {

        };

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
            minBoneRotation = Config.Bind<float>("Options", "MinBoneRotation", 0.5f, "Minimum bone rotation in degrees to be recorded (avoids idling jitter from breathing, etc.)");
            maxBoneRotationPerFrame = Config.Bind<float>("Options", "MaxBoneRotationPerFrame", 1, "Maximum rotation in degrees per bone per frame");
            maxBoneMovementPerFrame = Config.Bind<float>("Options", "MaxBoneRotationPerFrame", 0.01f, "Maximum movement in meters per bone per frame");
            maxModelRotationPerFrame = Config.Bind<float>("Options", "MaxModelRotationPerFrame", 1, "Maximum model rotation in degrees per frame");
            maxModelMovementPerFrame = Config.Bind<float>("Options", "MaxModelMovementPerFrame", 0.01f, "Maximum model movement in meters per frame");

            addModKey = Config.Bind<string>("HotKeys", "AddModKey", "left shift", "Modifier key to add current pose to selected animation");
            deleteFrameModKey = Config.Bind<string>("HotKeys", "DeleteFrameModKey", "left ctrl", "Modifier key to delete last frame from selected animation");
            deleteAnimationKey = Config.Bind<string>("HotKeys", "DeleteAnimationKey", "del", "Key to delete hovered animation");
            setReverseKey = Config.Bind<string>("HotKeys", "SetReverseKey", "r", "Key to toggle reversing for hovered animation");
            resetStartPosKey = Config.Bind<string>("HotKeys", "ResetStartPosKey", "p", "Key to reset the animation's reference position to the current character's position");
            setLoopKey = Config.Bind<string>("HotKeys", "SetLoopKey", "l", "Key to toggle looping for hovered animation");
            saveKey = Config.Bind<string>("HotKeys", "SaveKey", "v", "Key to save hovered animation to disk");
            fromFileKey = Config.Bind<string>("HotKeys", "FromFileKey", "f", "Key to reload hovered animation from disk");
            
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
            
            if(Global.code.uiFreePose.curCategory == catName.Value && (AedenthornUtils.CheckKeyDown(setReverseKey.Value) || AedenthornUtils.CheckKeyDown(setLoopKey.Value) || AedenthornUtils.CheckKeyDown(saveKey.Value) || AedenthornUtils.CheckKeyDown(fromFileKey.Value) || AedenthornUtils.CheckKeyDown(deleteAnimationKey.Value)))
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
                        if (rcr.gameObject.name == "AddNewAnimation" || !animationDict.ContainsKey(rcr.gameObject.name))
                            break;

                        PoseAnimationData pad = animationDict[rcr.gameObject.name];

                        if (AedenthornUtils.CheckKeyDown(setReverseKey.Value))
                        {
                            pad.reverse = !pad.reverse;
                            foreach(var kvp in currentlyPosing)
                            {
                                if (kvp.Value.name == pad.name)
                                    kvp.Value.data.reverse = pad.reverse;
                            }
                            Global.code.uiCombat.ShowHeader(Localization.GetContent("Reverse: "+pad.reverse, new object[0]));
                        }
                        else if (AedenthornUtils.CheckKeyDown(setLoopKey.Value))
                        {
                            pad.loop = !pad.loop;
                            foreach (var kvp in currentlyPosing)
                            {
                                if (kvp.Value.name == pad.name)
                                    kvp.Value.data.loop = pad.loop;
                            }
                            Global.code.uiCombat.ShowHeader(Localization.GetContent("Loop: " + pad.loop, new object[0]));
                        }
                        else if (AedenthornUtils.CheckKeyDown(resetStartPosKey.Value))
                        {
                            pad.StartPos = Global.code.uiFreePose.selectedCharacter.position;
                            Global.code.uiCombat.ShowHeader(Localization.GetContent("Animation start position reset to " + pad.StartPos, new object[0]));
                        }
                        else if (AedenthornUtils.CheckKeyDown(saveKey.Value))
                        {
                            File.WriteAllText(Path.Combine(AedenthornUtils.GetAssetPath(context), pad.name + ".json"), JsonConvert.SerializeObject(pad, Formatting.Indented));
                            Global.code.uiCombat.ShowHeader(string.Format(Localization.GetContent("Pose animation {0} saved", new object[0]), pad.name));
                        }
                        else if (AedenthornUtils.CheckKeyDown(fromFileKey.Value))
                        {
                            animationDict[pad.name] = JsonConvert.DeserializeObject<PoseAnimationData>(File.ReadAllText(Path.Combine(AedenthornUtils.GetAssetPath(this), pad.name+".json")));
                            try
                            {
                                Texture2D icon = new Texture2D(1, 1);
                                icon.LoadImage(File.ReadAllBytes(Path.Combine(AedenthornUtils.GetAssetPath(context), pad.name + ".png")));
                                RM.code.allFreePoses.GetItemWithName(pad.name).GetComponent<Pose>().icon = icon;
                                rcr.gameObject.GetComponent<RawImage>().texture = icon;
                            }
                            catch { }
                            Global.code.uiCombat.ShowHeader(string.Format(Localization.GetContent("Pose animation {0} reloaded", new object[0]), pad.name));
                        }
                        else if (AedenthornUtils.CheckKeyDown(deleteAnimationKey.Value))
                        {
                            animationDict.Remove(pad.name);
                            File.Delete(Path.Combine(AedenthornUtils.GetAssetPath(context), pad.name + ".json"));
                            RM.code.allFreePoses.RemoveItemWithName(pad.name);
                            Global.code.uiFreePose.Refresh();
                            Global.code.uiCombat.ShowHeader(string.Format(Localization.GetContent("Pose animation {0} deleted", new object[0]), pad.name));
                        }
                        break;
                    }
                }
            }

            if (currentlyPosing.Any())
            {
                for (int i = currentlyPosing.Keys.Count - 1; i >= 0; i--)
                {
                    PoseAnimationInstance pi = currentlyPosing[currentlyPosing.Keys.ElementAt(i)];
                    pi.deltaTime += Time.deltaTime;

                    if (pi.deltaTime < pi.data.rate)
                        continue;

                    int nextFrame;
                    int nextDeltaIndex;
                    pi.deltaTime = 0;

                    if (pi.reversing)
                    {
                        if (pi.currentFrame <= 0)
                        {
                            pi.currentFrame = 0;
                            if (pi.currentDelta <= 0)
                            {
                                pi.currentDelta = 0;
                                if (pi.data.loop)
                                {
                                    pi.reversing = false;
                                    nextFrame = 1;
                                    nextDeltaIndex = pi.currentDelta;
                                }
                                else
                                {
                                    currentlyPosing.Remove(currentlyPosing.Keys.ElementAt(i));
                                    continue;
                                }
                            }
                            else
                            {
                                nextDeltaIndex = pi.currentDelta - 1;
                                nextFrame = pi.data.deltas[nextDeltaIndex].frames - 1;
                            }
                        }
                        else
                        {
                            nextFrame = pi.currentFrame - 1;
                            nextDeltaIndex = pi.currentDelta;
                        }
                    }
                    else if (pi.currentFrame >= pi.data.deltas[pi.currentDelta].frames - 1)
                    {
                        if (pi.currentDelta >= pi.data.deltas.Count - 1)
                        {
                            if (pi.data.reverse)
                            {
                                if (pi.currentFrame <= 0)
                                {
                                    if (pi.currentDelta <= 0)
                                    {
                                        nextDeltaIndex = 0;
                                        nextFrame = 0;
                                    }
                                    else
                                    {
                                        nextDeltaIndex = pi.currentDelta - 1;
                                        nextFrame = pi.data.deltas[nextDeltaIndex].frames - 1;
                                    }
                                }
                                else
                                {
                                    //Dbgl("reversing");
                                    pi.reversing = true;
                                    nextFrame = pi.data.deltas[pi.currentDelta].frames - 2;
                                    nextDeltaIndex = pi.currentDelta;
                                }
                            }
                            else if (pi.data.loop)
                            {
                                //Dbgl("looping");
                                nextDeltaIndex = 0;
                                nextFrame = 0;
                            }
                            else
                            {
                                //Dbgl($"ending animation for {currentlyPosing.Keys.ElementAt(i).name}");

                                currentlyPosing.Remove(currentlyPosing.Keys.ElementAt(i));
                                continue;
                            }
                        }
                        else
                        {
                            nextDeltaIndex = pi.currentDelta + 1;
                            nextFrame = 0;
                            //Dbgl($"advancing delta {nextDeltaIndex}");
                        }
                    }
                    else
                    {
                        nextFrame = pi.currentFrame + 1;
                        nextDeltaIndex = pi.currentDelta;
                        //Dbgl($"advancing delta {pi.currentDelta} frame {nextFrame}");
                    }

                    try
                    {
                        var currentDelta = pi.data.deltas[pi.currentDelta];
                        float fraction = (pi.currentFrame + 1) / (float)currentDelta.frames;

                        foreach (var boneData in currentDelta.boneDatas)
                        {
                            if (pi.bones.ContainsKey(boneData.boneName))
                            {
                                pi.bones[boneData.boneName].localPosition = Vector3.Lerp(boneData.StartPos, boneData.EndPos, fraction);
                                pi.bones[boneData.boneName].localRotation = Quaternion.Lerp(Quaternion.Euler(boneData.StartRot), Quaternion.Euler(boneData.EndRot), fraction);
                            }
                        }

                        var rotOffset =  Quaternion.Euler(pi.StartRot) * Quaternion.Inverse(Quaternion.Euler(pi.data.StartRot));
                        //var posOffset = rotOffset * (pi.StartPos - pi.data.StartPos);
                        //var posOffset = pi.StartPos - pi.data.StartPos;

                        var lastRot = Quaternion.Euler(pi.StartRot);
                        var lastPos = pi.StartPos;
                        if (pi.currentDelta > 0)
                        {
                            lastRot *= Quaternion.Euler(pi.data.deltas[pi.currentDelta - 1].EndRotDelta);
                            lastPos += rotOffset * (pi.data.deltas[pi.currentDelta - 1].EndPosDelta);
                            //lastPos += pi.data.deltas[pi.currentDelta - 1].EndPosDelta;
                        }

                        var nextRot = Quaternion.Euler(pi.StartRot) * Quaternion.Euler(pi.data.deltas[pi.currentDelta].EndRotDelta);
                        var nextPos = pi.StartPos + rotOffset * pi.data.deltas[pi.currentDelta].EndPosDelta;

                        currentlyPosing.Keys.ElementAt(i).transform.position = Vector3.Lerp(lastPos, nextPos, fraction);
                        currentlyPosing.Keys.ElementAt(i).transform.rotation = Quaternion.Lerp(lastRot, nextRot, fraction);
                        pi.currentFrame = nextFrame;
                        pi.currentDelta = nextDeltaIndex;
                    }
                    catch (Exception ex)
                    {
                        currentlyPosing.Remove(currentlyPosing.Keys.ElementAt(i));
                        Dbgl($"Exception {pi.currentDelta}:{pi.currentFrame}: \n\n{ex}");
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
                        AddNewAnimation(data);
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
        private static void AddNewAnimation(PoseAnimationData data)
        {
            animationDict[data.name] = data;
            Transform t = Instantiate(RM.code.allFreePoses.items[0], posesGameObject.transform);
            t.name = data.name;
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
                    deltas = new List<PoseAnimationDelta>(),
                    reverse = reverseByDefault.Value,
                    loop = loopByDefault.Value,
                    StartPos = Global.code.uiFreePose.selectedCharacter.position,
                    StartRot = Global.code.uiFreePose.selectedCharacter.eulerAngles
                };
                foreach (Transform t in Global.code.uiFreePose.selectedCharacter.GetComponent<CharacterCustomization>().bonesNeedSave)
                {
                    var name = FixName(t.name);
                    if (!ignoreBones.Contains(name))
                        pad.boneStarts.Add(new BoneDelta(name, t.localPosition, t.localRotation.eulerAngles));
                }

                //Dbgl($"Adding {boneDatas.Count} bones to first frame");
                //pad.deltas.Add(new PoseAnimationDelta(boneDatas, 0, 1, Vector3.zero, Vector3.zero));

                AddNewAnimation(pad);
                File.WriteAllText(Path.Combine(AedenthornUtils.GetAssetPath(context), animationName + ".json"), JsonConvert.SerializeObject(pad, Formatting.Indented));
                Global.code.uiFreePose.Refresh();
                Global.code.uiCombat.ShowHeader(string.Format(Localization.GetContent("Pose animation {0} created", new object[0]), pad.name));
            }
            else if (AedenthornUtils.CheckKeyHeld(deleteFrameModKey.Value))
            {
                if(animationDict[buttonName].deltas.Count > 1)
                {
                    Dbgl($"Removing last delta from animation {buttonName}");
                    animationDict[buttonName].deltas.RemoveAt(animationDict[buttonName].deltas.Count - 1);
                    Global.code.uiCombat.ShowHeader(string.Format(Localization.GetContent("Removed last transformation from animation {0}", new object[0]), buttonName));
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

                PoseAnimationData pad = animationDict[buttonName];
                
                List<BoneDelta> boneDatas = new List<BoneDelta>();
                foreach(Transform t in Global.code.uiFreePose.selectedCharacter.GetComponent<CharacterCustomization>().bonesNeedSave)
                {
                    var name = FixName(t.name);
                    if (ignoreBones.Contains(name))
                        continue;

                    bool found = false;
                    for(int i = pad.deltas.Count - 1; i >= 0; i--)
                    {
                        var oldFrameBoneData = pad.deltas[i].boneDatas.Find(f => f.boneName == name);
                        if (oldFrameBoneData == null)
                            continue;
                        if (Quaternion.Angle(Quaternion.Euler(oldFrameBoneData.EndRot), Quaternion.Euler(t.localEulerAngles)) < minBoneRotation.Value && Vector3.Distance(t.localPosition, oldFrameBoneData.EndPos) == 0)
                            continue;

                        boneDatas.Add(new BoneDelta(FixName(name), oldFrameBoneData.EndPos, t.localPosition, oldFrameBoneData.EndRot, t.localRotation.eulerAngles));

                        //Dbgl($"frame {idx + 1}/{pad.frames.Count}, bone {boneData.boneName} total distance {Vector3.Distance(oldFrameBoneData.BonePos, boneData.BonePos)}, total rotation {Quaternion.Angle(oldFrameBoneData.BoneRotation, boneData.BoneRotation)}");
                        if (Mathf.Abs(Quaternion.Angle(Quaternion.Euler(oldFrameBoneData.EndRot), Quaternion.Euler(t.localEulerAngles))) > maxBoneRotationPerFrame.Value)
                        {
                            int reqFrames = Mathf.Abs(Mathf.CeilToInt(Quaternion.Angle(Quaternion.Euler(oldFrameBoneData.EndRot), Quaternion.Euler(t.localEulerAngles)) / maxBoneRotationPerFrame.Value));
                            if (reqFrames > totalFrames)
                                totalFrames = reqFrames;
                        }
                        if (Vector3.Distance(oldFrameBoneData.EndPos, t.localPosition) > maxBoneMovementPerFrame.Value)
                        {
                            int reqFrames = Mathf.CeilToInt(Vector3.Distance(oldFrameBoneData.EndPos, t.localPosition) / maxBoneMovementPerFrame.Value);
                            if (reqFrames > totalFrames)
                                totalFrames = reqFrames;
                        }
                        found = true;
                        break;
                    }
                    if (!found)
                    {
                        var oldFrameBoneData = pad.boneStarts.Find(f => f.boneName == name);
                        if (Quaternion.Angle(Quaternion.Euler(oldFrameBoneData.EndRot), Quaternion.Euler(t.localEulerAngles)) < minBoneRotation.Value && Vector3.Distance(t.localPosition, oldFrameBoneData.EndPos) == 0)
                            continue;
                        boneDatas.Add(new BoneDelta(FixName(name), oldFrameBoneData.EndPos, t.localPosition, oldFrameBoneData.EndRot, t.localRotation.eulerAngles));

                        //Dbgl($"frame {idx + 1}/{pad.frames.Count}, bone {boneData.boneName} total distance {Vector3.Distance(oldFrameBoneData.BonePos, boneData.BonePos)}, total rotation {Quaternion.Angle(oldFrameBoneData.BoneRotation, boneData.BoneRotation)}");
                        if (Mathf.Abs(Quaternion.Angle(Quaternion.Euler(oldFrameBoneData.EndRot), Quaternion.Euler(t.localEulerAngles))) > maxBoneRotationPerFrame.Value)
                        {
                            int reqFrames = Mathf.Abs(Mathf.CeilToInt(Quaternion.Angle(Quaternion.Euler(oldFrameBoneData.EndRot), Quaternion.Euler(t.localEulerAngles)) / maxBoneRotationPerFrame.Value));
                            if (reqFrames > totalFrames)
                                totalFrames = reqFrames;
                        }
                    }
                }


                var lastPos = pad.deltas.Count > 0 ? pad.StartPos + pad.deltas[pad.deltas.Count - 1].EndPosDelta : pad.StartPos;
                var lastRot = pad.deltas.Count > 0 ? Quaternion.Euler(pad.StartRot) * Quaternion.Euler(pad.deltas[pad.deltas.Count - 1].EndRotDelta) : Quaternion.Euler(pad.StartRot);
                
                float rotationFrames = Mathf.Abs(Quaternion.Angle(Global.code.uiFreePose.selectedCharacter.rotation, lastRot)) / maxModelRotationPerFrame.Value;
                if (rotationFrames > totalFrames)
                    totalFrames = Mathf.CeilToInt(rotationFrames);

                float movementFrames = Vector3.Distance(Global.code.uiFreePose.selectedCharacter.position, lastPos) / maxModelMovementPerFrame.Value;
                if (movementFrames > totalFrames)
                    totalFrames = Mathf.CeilToInt(movementFrames);

                pad.deltas.Add(new PoseAnimationDelta(boneDatas, totalFrames, Global.code.uiFreePose.selectedCharacter.position - pad.StartPos, (Global.code.uiFreePose.selectedCharacter.rotation * Quaternion.Inverse(Quaternion.Euler(pad.StartRot))).eulerAngles));

                /*
                for (int i = 1; i <= totalFrames; i++)
                {
                    List<MyPoseData> poseDatas = new List<MyPoseData>();
                    foreach (var kvp in modifiedBones)
                    {
                        Transform bone = kvp.Key;
                        MyPoseData oldFrameBoneData = kvp.Value;
                        MyPoseData boneData = new MyPoseData(FixName(bone.name), bone.localPosition, bone.localRotation);

                        boneData.DeltaPos = Vector3.Lerp(oldFrameBoneData.DeltaPos, boneData.DeltaPos, i / (float)totalFrames);
                        boneData.DeltaRot = Quaternion.Lerp(oldFrameBoneData.DeltaRot, boneData.DeltaRot, i / (float)totalFrames);
                        poseDatas.Add(boneData);
                    }
                    Vector3 shifted = Vector3.Lerp(lastPos, Global.code.uiFreePose.selectedCharacter.position, i / (float)totalFrames);
                    Vector3 rotated = Vector3.Lerp(lastRot, Global.code.uiFreePose.selectedCharacter.rotation.eulerAngles, i / (float)totalFrames);
                    pad.frames.Add(new PoseAnimationDelta(poseDatas, pad.frames.Count, shifted - pad.StartPos, rotated - pad.StartRot));
                }
                */
                Dbgl($"Moved {boneDatas.Count} bones in {totalFrames} frames.");

                animationDict[buttonName] = pad;

                Global.code.uiCombat.ShowHeader(string.Format(Localization.GetContent("{0} frames added to animation {1}", new object[0]), totalFrames, pad.name));

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

                PoseAnimationData pad = animationDict[buttonName];
                if (currentlyPosing.ContainsKey(Global.code.uiFreePose.selectedCharacter.GetComponent<CharacterCustomization>()))
                {
                    string oldName = currentlyPosing[Global.code.uiFreePose.selectedCharacter.GetComponent<CharacterCustomization>()].data.name;
                    Dbgl($"Stopping animation {oldName} for {Global.code.uiFreePose.selectedCharacter.name} on frame {currentlyPosing[Global.code.uiFreePose.selectedCharacter.GetComponent<CharacterCustomization>()].currentFrame}");
                    currentlyPosing.Remove(Global.code.uiFreePose.selectedCharacter.GetComponent<CharacterCustomization>());
                    if (oldName == buttonName) { }
                        return;
                }
                Dbgl($"Setting {Global.code.uiFreePose.selectedCharacter.name} to animation {buttonName}");
                PoseAnimationInstance instance = new PoseAnimationInstance()
                {
                    data = pad,
                    bones = new Dictionary<string, Transform>(),
                    StartPos = Global.code.uiFreePose.selectedCharacter.position,
                    StartRot = Global.code.uiFreePose.selectedCharacter.rotation.eulerAngles
                };
                foreach (Transform t in Global.code.uiFreePose.selectedCharacter.GetComponent<CharacterCustomization>().bonesNeedSave)
                {
                    var name = FixName(t.name);
                    if (ignoreBones.Contains(name))
                        continue;

                    instance.bones[name] = t;
                }
                currentlyPosing.Add(Global.code.uiFreePose.selectedCharacter.GetComponent<CharacterCustomization>(), instance);
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
