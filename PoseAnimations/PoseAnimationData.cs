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
        public List<BoneDelta> boneStarts = new List<BoneDelta>();

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
        public float[] endPosDelta = new float[3];
        public float[] endRotDelta = new float[3];
        public List<BoneDelta> boneDatas = new List<BoneDelta>();

        public PoseAnimationDelta()
        {

        }
        public PoseAnimationDelta(List<BoneDelta> boneDatas, int frames, Vector3 endPosDelta, Vector3 endRotDelta)
        {
            this.boneDatas = boneDatas;
            this.frames = frames;
            EndPosDelta = endPosDelta;
            EndRotDelta = endRotDelta;
        }

        [JsonIgnore]
        public Vector3 EndPosDelta
        {
            get
            {
                return new Vector3(endPosDelta[0], endPosDelta[1], endPosDelta[2]);
            }
            set
            {
                endPosDelta = new float[] { value.x, value.y, value.z };
            }
        }
        [JsonIgnore]
        public Vector3 EndRotDelta
        {
            get
            {
                return new Vector3(endRotDelta[0], endRotDelta[1], endRotDelta[2]);
            }
            set
            {
                endRotDelta = new float[] { value.x, value.y, value.z };
            }
        }
    }
    public class BoneDelta
    {
        public string boneName;
        public float[] startPos = new float[3];
        public float[] endPos = new float[3];
        public float[] startRot = new float[3];
        public float[] endRot = new float[3];
        public BoneDelta()
        {

        }

        public BoneDelta(string name, Vector3 endPos, Vector3 endRot)
        {
            boneName = name;
            EndPos = endPos;
            EndRot = endRot;
        }
        public BoneDelta(string name, Vector3 startPos, Vector3 endPos, Vector3 startRot, Vector3 endRot)
        {
            boneName = name; 
            StartPos = startPos;
            EndPos = endPos;
            StartRot = startRot;
            EndRot = endRot;
        }

        [JsonIgnore]
        public Vector3 StartPos {
            get 
            {
                return new Vector3(startPos[0], startPos[1], startPos[2]);
            }
            set
            {
                startPos = new float[] { value.x, value.y, value.z};
            }
        }
        [JsonIgnore]
        public Vector3 EndPos {
            get 
            {
                return new Vector3(endPos[0], endPos[1], endPos[2]);
            }
            set
            {
                endPos = new float[] { value.x, value.y, value.z};
            }
        }
        [JsonIgnore]
        public Vector3 StartRot {
            get 
            {
                return new Vector3(startRot[0], startRot[1], startRot[2]);
            }
            set
            {
                startRot = new float[] { value.x, value.y, value.z};
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