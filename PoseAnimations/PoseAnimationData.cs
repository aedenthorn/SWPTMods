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
        public int currentDelta = 0;
        public float deltaTime = 0;
        public Dictionary<string, Transform> bones;
        public float[] startPos = new float[3];
        public float[] startRot = new float[3];
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
        [JsonIgnore]
        public Vector3 StartRot
        {
            get
            {
                return new Vector3(startRot[0], startRot[1], startRot[2]);
            }
            set
            {
                startRot = new float[] { value.x, value.y, value.z };
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
        public float[] startRot = new float[3];
        public List<PoseAnimationDelta> deltas = new List<PoseAnimationDelta>();
        public List<BoneStart> boneStarts = new List<BoneStart>();

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
        [JsonIgnore]
        public Vector3 StartRot
        {
            get
            {
                return new Vector3(startRot[0], startRot[1], startRot[2]);
            }
            set
            {
                startRot = new float[] { value.x, value.y, value.z };
            }
        }
    }
    public class PoseAnimationDelta
    {
        public int frames;
        public float[] deltaPos = new float[3];
        public float[] deltaRot = new float[3];
        public List<BoneDelta> boneDatas = new List<BoneDelta>();

        public PoseAnimationDelta()
        {

        }
        public PoseAnimationDelta(List<BoneDelta> boneDatas, int frames, Vector3 deltaPos, Vector3 deltaRot)
        {
            this.boneDatas = boneDatas;
            this.frames = frames;
            DeltaPos = deltaPos;
            DeltaRot = deltaRot;
        }

        [JsonIgnore]
        public Vector3 DeltaPos
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
        [JsonIgnore]
        public Vector3 DeltaRot
        {
            get
            {
                return new Vector3(deltaRot[0], deltaRot[1], deltaRot[2]);
            }
            set
            {
                deltaRot = new float[] { value.x, value.y, value.z };
            }
        }
    }

    public class BoneStart
    {
        public BoneStart()
        {

        }

        public BoneStart(string name, Vector3 rot)
        {
            boneName = name; 
            EndRot = rot;
        }

        public string boneName;
        public float[] startRot = new float[3];


        [JsonIgnore]
        public Vector3 EndRot {
            get 
            {
                return new Vector3(startRot[0], startRot[1], startRot[2]);
            }
            set
            {
                startRot = new float[] { value.x, value.y, value.z};
            }
        }
    }    
    public class BoneDelta
    {
        public BoneDelta()
        {

        }

        public BoneDelta(string name, Vector3 endRot)
        {
            boneName = name; 
            EndRot = endRot;
        }
        public BoneDelta(string name, Vector3 startRot, Vector3 endRot)
        {
            boneName = name; 
            StartRot = startRot;
            EndRot = endRot;
        }

        public string boneName;
        public float[] startRot = new float[3];
        public float[] endRot = new float[3];

        [JsonIgnore]
        public Vector3 StartRot
        {
            get
            {
                return new Vector3(startRot[0], startRot[1], startRot[2]);
            }
            set
            {
                startRot = new float[] { value.x, value.y, value.z };
            }
        }

        [JsonIgnore]
        public Vector3 EndRot {
            get 
            {
                return new Vector3(endRot[0], endRot[1], endRot[2]);
            }
            set
            {
                endRot = new float[] { value.x, value.y, value.z};
            }
        }
    }
}