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
        public float[] startPos = new float[3];
        public PoseAnimationData data;
        [JsonIgnore]
        public Vector3 StartPos
        {
            get
            {
                return new Vector3(startPos[0], startPos[1], startPos[2]);
            }
            set
            {
                startPos = new float[] { value.x, value.y, value.z };
            }
        }
    }
    public class PoseAnimationData
    {
        public string name;
        public float rate = 0;
        public bool loop = true;
        public bool reverse = true;
        public float[] startPos = new float[3];
        public List<PoseAnimationFrame> frames;
        [JsonIgnore]
        public Vector3 StartPos
        {
            get
            {
                return new Vector3(startPos[0], startPos[1], startPos[2]);
            }
            set
            {
                startPos = new float[] { value.x, value.y, value.z };
            }
        }
    }
    public class PoseAnimationFrame
    {
        public int index;
        public float[] deltaPos = new float[3];
        public List<MyPoseData> poseDatas;

        public PoseAnimationFrame(List<MyPoseData> poseDatas, int index, Vector3 deltaPos)
        {
            this.poseDatas = poseDatas;
            this.index = index;
            DeltaPosition = deltaPos;
        }

        [JsonIgnore]
        public Vector3 DeltaPosition
        {
            get
            {
                return new Vector3(deltaPos[0], deltaPos[1], deltaPos[2]);
            }
            set
            {
                deltaPos = new float[] { value.x, value.y, value.z };
            }
        }
    }

    public class MyPoseData
    {
        public MyPoseData(string name, Vector3 bonePos_, Quaternion boneRotation_)
        {
            boneName = name; 
            BonePos = bonePos_;
            BoneRotation = boneRotation_;
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