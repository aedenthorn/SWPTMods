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
    [BepInPlugin("aedenthorn.PoseAnimations", "Pose Animations", "0.12.0")]
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
        
        public static ConfigEntry<string> framesTitle;

        public static ConfigEntry<bool> autoStart;
        public static ConfigEntry<bool> reverseByDefault;
        public static ConfigEntry<bool> loopByDefault;
        public static ConfigEntry<string> catName;
        public static ConfigEntry<float> minBoneRotation;
        public static ConfigEntry<float> maxBoneRotationPerFrame;
        public static ConfigEntry<float> maxBoneMovementPerFrame;
        public static ConfigEntry<float> maxModelRotationPerFrame;
        public static ConfigEntry<float> maxModelMovementPerFrame;

        public static Dictionary<string, PoseAnimationData> animationDict = new Dictionary<string, PoseAnimationData>();
        public static Dictionary<CharacterCustomization,PoseAnimationInstance> currentlyPosing = new Dictionary<CharacterCustomization, PoseAnimationInstance>();
        
        public static GameObject posesGameObject;
        public static Transform addNewAnimationButton;
        public static GameObject framesObject;
        public static GameObject playObject;
        public static GameObject autoPlayObject;
        public static InputField framesInput;
        public static int framesPerDelta;
        public static bool started;

        public static List<string> ignoreBones = new List<string>()
        {
            "makeup loc (1)",
            "lgroup",
            "rgroup",
            "head target"
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

            autoStart = Config.Bind<bool>("Options", "AutoStart", true, "Animations start on click");
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
            deleteAnimationKey = Config.Bind<string>("HotKeys", "DeleteAnimationKey", "delete", "Key to delete hovered animation");
            setReverseKey = Config.Bind<string>("HotKeys", "SetReverseKey", "r", "Key to toggle reversing for hovered animation");
            resetStartPosKey = Config.Bind<string>("HotKeys", "ResetStartPosKey", "p", "Key to reset the animation's reference position to the current character's position");
            setLoopKey = Config.Bind<string>("HotKeys", "SetLoopKey", "l", "Key to toggle looping for hovered animation");
            saveKey = Config.Bind<string>("HotKeys", "SaveKey", "v", "Key to save hovered animation to disk");
            fromFileKey = Config.Bind<string>("HotKeys", "FromFileKey", "f", "Key to reload hovered animation from disk");
            
            framesTitle = Config.Bind<string>("Text", "FramesText", "Frames", "Title for number of frames per delta");
            
            nexusID = Config.Bind<int>("General", "NexusID", 100, "Nexus mod ID for updates");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Dbgl("Plugin awake");
            posesGameObject = new GameObject();
            posesGameObject.name = "PoseAnimations";
            DontDestroyOnLoad(posesGameObject);
        }

        private void LateUpdate()
        {
            if (!modEnabled.Value || !Player.code || !Global.code.uiFreePose.enabled || !started)
                return;

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

                        foreach (var kvp in currentDelta.boneDatas)
                        {
                            if (pi.bones.ContainsKey(kvp.Key))
                            {
                                pi.bones[kvp.Key].localPosition = Vector3.Lerp(ToVector3(kvp.Value.startPos), ToVector3(kvp.Value.endPos), fraction);
                                pi.bones[kvp.Key].localRotation = Quaternion.Lerp(Quaternion.Euler(ToVector3(kvp.Value.startRot)), Quaternion.Euler(ToVector3(kvp.Value.endRot)), fraction);
                            }
                        }

                        var rotOffset = Quaternion.Euler(ToVector3(pi.startRot)) * Quaternion.Inverse(Quaternion.Euler(ToVector3(pi.data.startRot)));
                        //var posOffset = rotOffset * (ToVector3(pi.startPos) - pi.ToVector3(data.startPos));
                        //var posOffset = ToVector3(pi.startPos) - pi.ToVector3(data.startPos);

                        var lastRot = Quaternion.Euler(ToVector3(pi.startRot));
                        var lastPos = ToVector3(pi.startPos);
                        if (pi.currentDelta > 0)
                        {
                            lastRot *= Quaternion.Euler(ToVector3(pi.data.deltas[pi.currentDelta - 1].endRotDelta));
                            lastPos += rotOffset * ToVector3(pi.data.deltas[pi.currentDelta - 1].endPosDelta);
                            //lastPos += pi.data.deltas[pi.currentDelta - 1].EndPosDelta;
                        }

                        var nextRot = Quaternion.Euler(ToVector3(pi.startRot)) * Quaternion.Euler(ToVector3(pi.data.deltas[pi.currentDelta].endRotDelta));
                        var nextPos = ToVector3(pi.startPos) + rotOffset * ToVector3(pi.data.deltas[pi.currentDelta].endPosDelta);

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
        private void Update()
        {
            if (!modEnabled.Value || !Player.code || !Global.code.uiFreePose.enabled)
                return;

            if (Global.code.uiFreePose.curCategory == catName.Value && (AedenthornUtils.CheckKeyDown(setReverseKey.Value) || AedenthornUtils.CheckKeyDown(setLoopKey.Value) || AedenthornUtils.CheckKeyDown(saveKey.Value) || AedenthornUtils.CheckKeyDown(fromFileKey.Value) || AedenthornUtils.CheckKeyDown(deleteAnimationKey.Value)))
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
                            pad.startPos = ToArray(Global.code.uiFreePose.selectedCharacter.position);
                            Global.code.uiCombat.ShowHeader(Localization.GetContent("Animation start position reset to " + ToVector3(pad.startPos), new object[0]));
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

            
        }

        private static float[] ToArray(Vector3 v)
        {
            return new float[] { v.x, v.y, v.z };
        }

        private static Vector3 ToVector3(float[] array)
        {
            return new Vector3(array[0], array[1], array[2]);
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
                if (!modEnabled.Value)
                    return;

                if(__instance.curCategory != catName.Value)
                {
                    framesObject?.SetActive(false);
                    playObject?.SetActive(false);
                    autoPlayObject?.SetActive(false);
                    return;
                }


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

                if (!framesObject)
                {
                    Dbgl($"Creating frames object");

                    framesObject = new GameObject() { name = "Frames" };
                    framesObject.transform.SetParent(__instance.categoryDropdown.transform.parent);
                    framesObject.AddComponent<RectTransform>().anchoredPosition = __instance.categoryDropdown.GetComponent<RectTransform>().anchoredPosition + new Vector2(__instance.categoryDropdown.GetComponent<RectTransform>().rect.width, 0);

                    Text text = Instantiate(Global.code.uiCombat.lineName.transform, framesObject.transform).GetComponent<Text>();
                    text.text = framesTitle.Value;
                    text.gameObject.name = "Title";
                    text.GetComponent<RectTransform>().sizeDelta = new Vector2(80, 40);
                    text.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 22);
                    text.resizeTextMaxSize = 32;

                    GameObject ti = Instantiate(Global.code.uiPose.nameinput.gameObject, framesObject.transform);
                    ti.name = "Input Field";
                    ti.GetComponent<RectTransform>().sizeDelta = new Vector2(80, 40);
                    ti.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -10);

                    //ti.GetComponent<RectTransform>().anchoredPosition *= new Vector2(0, 1);
                    framesInput = ti.GetComponent<InputField>();
                    framesInput.onValueChanged = new InputField.OnChangeEvent();
                    framesInput.placeholder.GetComponent<Text>().text = "Auto";
                    framesInput.placeholder.GetComponent<Text>().resizeTextMaxSize = 32;
                    //framesInput.placeholder.GetComponent<Text>().resizeTextMaxSize = 32;
                    framesInput.textComponent.resizeTextMaxSize = 32;

                    
                    playObject = new GameObject() { name = "Play" };
                    playObject.transform.SetParent(__instance.categoryDropdown.transform.parent);
                    playObject.AddComponent<RectTransform>().anchoredPosition = framesObject.GetComponent<RectTransform>().anchoredPosition + new Vector2(52, 25);
                    
                    var playButtonObj = Instantiate(Mainframe.code.uiConfirmation.groupYesNo.transform.Find("yes"), playObject.transform);
                    Destroy(playButtonObj.GetComponentInChildren<LocalizationText>());
                    playButtonObj.gameObject.name = "Play Button";
                    playButtonObj.GetComponent<RectTransform>().sizeDelta = new Vector2(36, 15);
                    playButtonObj.GetComponent<RectTransform>().localScale = new Vector3(2, 2, 2);
                    playButtonObj.gameObject.GetComponentInChildren<Text>().text = started ? "||" : ">";
                    playButtonObj.gameObject.GetComponentInChildren<Button>().onClick = new Button.ButtonClickedEvent();
                    playButtonObj.gameObject.GetComponentInChildren<Button>().onClick.AddListener(ToggleStarted);

                    autoPlayObject = new GameObject() { name = "Autoplay" };
                    autoPlayObject.transform.SetParent(__instance.categoryDropdown.transform.parent);
                    autoPlayObject.AddComponent<RectTransform>().anchoredPosition = framesObject.GetComponent<RectTransform>().anchoredPosition + new Vector2(52, 11);

                    var apButtonObj = Instantiate(Mainframe.code.uiConfirmation.groupYesNo.transform.Find("yes"), autoPlayObject.transform);
                    apButtonObj.gameObject.name = "Autoplay Button";
                    Destroy(apButtonObj.GetComponentInChildren<LocalizationText>());
                    apButtonObj.GetComponent<RectTransform>().sizeDelta = new Vector2(36, 15);
                    apButtonObj.GetComponent<RectTransform>().localScale = new Vector3(2, 2, 2);
                    apButtonObj.GetComponentInChildren<Text>().text = "A";
                    apButtonObj.GetComponentInChildren<Text>().color = autoStart.Value ? Color.white : Color.grey;
                    apButtonObj.GetComponentInChildren<Button>().onClick = new Button.ButtonClickedEvent();
                    apButtonObj.GetComponentInChildren<Button>().onClick.AddListener(ToggleAutoStart);
                }
                framesObject.SetActive(true);
                playObject.SetActive(true);
                autoPlayObject.SetActive(true);
            }
            private static void ToggleStarted()
            {
                started = !started;
                playObject.GetComponentInChildren<Text>().text = started ? "||" : ">";
            }
            private static void ToggleAutoStart()
            {
                autoStart.Value = !autoStart.Value;
                autoPlayObject.GetComponentInChildren<Text>().color = autoStart.Value ? Color.white : Color.grey;
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
                    startPos = ToArray(Global.code.uiFreePose.selectedCharacter.position),
                    startRot = ToArray(Global.code.uiFreePose.selectedCharacter.eulerAngles)
                };
                foreach (Transform t in Global.code.uiFreePose.selectedCharacter.GetComponent<CharacterCustomization>().bonesNeedSave)
                {
                    var name = FixName(t.name);
                    if (!ignoreBones.Contains(name))
                        pad.boneStartDict.Add(name, new BoneDelta(ToArray(t.localPosition), ToArray(t.localRotation.eulerAngles)));
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

                if (!int.TryParse(framesInput.text, out framesPerDelta))
                    framesPerDelta = -1;

                Dictionary<string, BoneDelta> boneDatas = new Dictionary<string, BoneDelta>();
                foreach(Transform t in Global.code.uiFreePose.selectedCharacter.GetComponent<CharacterCustomization>().bonesNeedSave)
                {
                    var name = FixName(t.name);
                    if (ignoreBones.Contains(name))
                        continue;

                    bool found = false;
                    for(int i = pad.deltas.Count - 1; i >= 0; i--)
                    {
                        if (!pad.deltas[i].boneDatas.ContainsKey(name))
                            continue;
                        found = true;
                        var oldFrameBoneData = pad.deltas[i].boneDatas[name];
                        if (Quaternion.Angle(Quaternion.Euler(ToVector3(oldFrameBoneData.endRot)), Quaternion.Euler(t.localEulerAngles)) < minBoneRotation.Value && Vector3.Distance(t.localPosition, ToVector3(oldFrameBoneData.endPos)) == 0)
                            continue;

                        boneDatas.Add(FixName(name), new BoneDelta(oldFrameBoneData.endPos, ToArray(t.localPosition), oldFrameBoneData.endRot, ToArray(t.localRotation.eulerAngles)));

                        //Dbgl($"frame {idx + 1}/{pad.frames.Count}, bone {boneData.boneName} total distance {Vector3.Distance(oldFrameBoneData.BonePos, boneData.BonePos)}, total rotation {Quaternion.Angle(oldFrameBoneData.BoneRotation, boneData.BoneRotation)}");

                        if(framesPerDelta == -1)
                        {
                            if (Mathf.Abs(Quaternion.Angle(Quaternion.Euler(ToVector3(oldFrameBoneData.endRot)), Quaternion.Euler(t.localEulerAngles))) > maxBoneRotationPerFrame.Value)
                            {
                                int reqFrames = Mathf.Abs(Mathf.CeilToInt(Quaternion.Angle(Quaternion.Euler(ToVector3(oldFrameBoneData.endRot)), Quaternion.Euler(t.localEulerAngles)) / maxBoneRotationPerFrame.Value));
                                if (reqFrames > totalFrames)
                                    totalFrames = reqFrames;
                            }
                            if (Vector3.Distance(ToVector3(oldFrameBoneData.endPos), t.localPosition) > maxBoneMovementPerFrame.Value)
                            {
                                int reqFrames = Mathf.CeilToInt(Vector3.Distance(ToVector3(oldFrameBoneData.endPos), t.localPosition) / maxBoneMovementPerFrame.Value);
                                if (reqFrames > totalFrames)
                                    totalFrames = reqFrames;
                            }
                        }

                        break;
                    }
                    if (!found && pad.boneStartDict.ContainsKey(name))
                    {
                        var oldFrameBoneData = pad.boneStartDict[name];
                        if (Quaternion.Angle(Quaternion.Euler(ToVector3(oldFrameBoneData.endRot)), Quaternion.Euler(t.localEulerAngles)) < minBoneRotation.Value && Vector3.Distance(t.localPosition, ToVector3(oldFrameBoneData.endPos)) == 0)
                            continue;
                        boneDatas.Add(FixName(name), new BoneDelta(oldFrameBoneData.endPos, ToArray(t.localPosition), oldFrameBoneData.endRot, ToArray(t.localRotation.eulerAngles)));

                        //Dbgl($"frame {idx + 1}/{pad.frames.Count}, bone {boneData.boneName} total distance {Vector3.Distance(oldFrameBoneData.BonePos, boneData.BonePos)}, total rotation {Quaternion.Angle(oldFrameBoneData.BoneRotation, boneData.BoneRotation)}");
                        if (framesPerDelta == -1 && Mathf.Abs(Quaternion.Angle(Quaternion.Euler(ToVector3(oldFrameBoneData.endRot)), Quaternion.Euler(t.localEulerAngles))) > maxBoneRotationPerFrame.Value)
                        {
                            int reqFrames = Mathf.Abs(Mathf.CeilToInt(Quaternion.Angle(Quaternion.Euler(ToVector3(oldFrameBoneData.endRot)), Quaternion.Euler(t.localEulerAngles)) / maxBoneRotationPerFrame.Value));
                            if (reqFrames > totalFrames)
                                totalFrames = reqFrames;
                        }
                    }
                }


                var lastPos = pad.deltas.Count > 0 ? ToVector3(pad.startPos) + ToVector3(pad.deltas[pad.deltas.Count - 1].endPosDelta) : ToVector3(pad.startPos);
                var lastRot = pad.deltas.Count > 0 ? Quaternion.Euler(ToVector3(pad.startRot)) * Quaternion.Euler(ToVector3(pad.deltas[pad.deltas.Count - 1].endRotDelta)) : Quaternion.Euler(ToVector3(pad.startRot));
                
                if(framesPerDelta == -1)
                {
                    float rotationFrames = Mathf.Abs(Quaternion.Angle(Global.code.uiFreePose.selectedCharacter.rotation, lastRot)) / maxModelRotationPerFrame.Value;
                    if (rotationFrames > totalFrames)
                        totalFrames = Mathf.CeilToInt(rotationFrames);

                    float movementFrames = Vector3.Distance(Global.code.uiFreePose.selectedCharacter.position, lastPos) / maxModelMovementPerFrame.Value;
                    if (movementFrames > totalFrames)
                        totalFrames = Mathf.CeilToInt(movementFrames);
                }
                else
                {
                    totalFrames = framesPerDelta;
                }

                pad.deltas.Add(new PoseAnimationDelta(boneDatas, totalFrames, ToArray(Global.code.uiFreePose.selectedCharacter.position - ToVector3(pad.startPos)), ToArray((Global.code.uiFreePose.selectedCharacter.rotation * Quaternion.Inverse(Quaternion.Euler(ToVector3(pad.startRot)))).eulerAngles)));

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
                    pad.frames.Add(new PoseAnimationDelta(poseDatas, pad.frames.Count, shifted - ToVector3(pad.startPos), rotated - ToVector3(pad.startRot)));
                }
                */
                Dbgl($"Moved {boneDatas.Count} bones in {totalFrames} frames.");

                animationDict[buttonName] = pad;

                Global.code.uiCombat.ShowHeader(string.Format(Localization.GetContent("{0} frames added to animation {1}", new object[0]), totalFrames, pad.name));

            }
            else
            {
                /*
                Global.code.freeCamera.GetComponent<FreelookCamera>().LetRuntimeTransformRun();
                TransformGizmo.transformGizmo_.showTempGroup = false;
                foreach (Transform transform in TransformGizmo.transformGizmo_.tempTransform)
                {
                    transform.gameObject.SetActive(false);
                }
                TransformGizmo.transformGizmo_.selectNow = null;
                */

                PoseAnimationData pad = animationDict[buttonName];
                if (pad.deltas.Count == 0)
                {
                    Dbgl($"Animation contains no deltas!");
                    return;
                }
                if (currentlyPosing.ContainsKey(Global.code.uiFreePose.selectedCharacter.GetComponent<CharacterCustomization>()))
                {
                    string oldName = currentlyPosing[Global.code.uiFreePose.selectedCharacter.GetComponent<CharacterCustomization>()].data.name;
                    Dbgl($"Stopping animation {oldName} for {Global.code.uiFreePose.selectedCharacter.name} on frame {currentlyPosing[Global.code.uiFreePose.selectedCharacter.GetComponent<CharacterCustomization>()].currentFrame}");
                    currentlyPosing.Remove(Global.code.uiFreePose.selectedCharacter.GetComponent<CharacterCustomization>());
                    if (oldName == buttonName)
                    {
                        started = false;
                        return;
                    }
                }
                Dbgl($"Setting {Global.code.uiFreePose.selectedCharacter.name} to animation {buttonName}");
                PoseAnimationInstance instance = new PoseAnimationInstance()
                {
                    data = pad,
                    bones = new Dictionary<string, Transform>(),
                    startPos = ToArray(Global.code.uiFreePose.selectedCharacter.position),
                    startRot = ToArray(Global.code.uiFreePose.selectedCharacter.rotation.eulerAngles)
                };
                foreach (Transform t in Global.code.uiFreePose.selectedCharacter.GetComponent<CharacterCustomization>().bonesNeedSave)
                {
                    var name = FixName(t.name);
                    if (ignoreBones.Contains(name))
                        continue;

                    instance.bones[name] = t;
                    if (pad.boneStartDict.ContainsKey(name))
                    {
                        t.localPosition = ToVector3(pad.boneStartDict[name].endPos);
                        t.localEulerAngles = ToVector3(pad.boneStartDict[name].endRot);
                    }
                }
                currentlyPosing.Add(Global.code.uiFreePose.selectedCharacter.GetComponent<CharacterCustomization>(), instance);
                if (autoStart.Value)
                    started = true;
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
