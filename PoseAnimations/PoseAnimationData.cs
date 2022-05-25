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
        public Dictionary<string, BoneDelta> boneStartDict = new Dictionary<string, BoneDelta>();
    }
    public class PoseAnimationDelta
    {
        public int frames;
        public float[] endPosDelta = new float[3];
        public float[] endRotDelta = new float[3];
        public Dictionary<string, BoneDelta> boneDatas = new Dictionary<string, BoneDelta>();

        public PoseAnimationDelta()
        {

        }
        public PoseAnimationDelta(Dictionary<string, BoneDelta> boneDatas, int frames, float[] endPosDelta, float[] endRotDelta)
        {
            this.boneDatas = boneDatas;
            this.frames = frames;
            this.endPosDelta = endPosDelta;
            this.endRotDelta = endRotDelta;
        }

    }
    public class BoneDelta
    {
        public float[] startPos = new float[3];
        public float[] endPos = new float[3];
        public float[] startRot = new float[3];
        public float[] endRot = new float[3];
        public BoneDelta()
        {

        }

        public BoneDelta(float[] endPos, float[] endRot)
        {
            this.endPos = endPos;
            this.endRot = endRot;
        }
        public BoneDelta(float[] startPos, float[] endPos, float[] startRot, float[] endRot)
        {
            this.startPos = startPos;
            this.endPos = endPos;
            this.startRot = startRot;
            this.endRot = endRot;
        }
    }
}