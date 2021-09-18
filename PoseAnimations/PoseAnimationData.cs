using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace PoseAnimations
{
    public class PoseAnimationInstance
    {
        public string name;
        public bool reversing = false;
        public int currentFrame = 0;
        public float deltaTime = 0;
        public Dictionary<string, Transform> bones;
        public PoseAnimationData data;
    }
    public class PoseAnimationData
    {
        public string name;
        public float rate = 0;
        public bool loop = true;
        public bool reverse = false;
        public List<PoseAnimationFrame> frames;
    }
    public class PoseAnimationFrame
    {
        public int index;
        public List<MyPoseData> poseDatas;

        public PoseAnimationFrame(List<MyPoseData> poseDatas, int index)
        {
            this.poseDatas = poseDatas;
            this.index = index;
        }
    }

    public class MyPoseData
    {
        public MyPoseData(string name, Vector3 bonePos_, Quaternion boneRotation_)
        {
            boneName = name; 
            bonePos = new float[] { bonePos_.x, bonePos_.y, bonePos_.z };
            boneRotation = new float[] { boneRotation_.x, boneRotation_.y, boneRotation_.z, boneRotation_.w };
        }

        public string boneName;

        public float[] bonePos = new float[3];

        public float[] boneRotation = new float[4];
        
        [JsonIgnore]
        public Vector3 BonePos
        { 
            get
            {
                return new Vector3(bonePos[0], bonePos[1], bonePos[2]);
            }
            set
            {
                bonePos = new float[] { value.x, value.y, value.z };
            }
        }
        
        [JsonIgnore]
        public Quaternion BoneRotation {
            get 
            {
                return new Quaternion(boneRotation[0], boneRotation[1], boneRotation[2], boneRotation[3]);
            }
            set
            {
                boneRotation = new float[] { value.x, value.y, value.z, value.w };
            }
        }
    }
}