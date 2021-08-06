namespace Jiggle
{
    public class JiggleData
    {
        public Assets.DuckType.Jiggle.Jiggle jiggle;

        public float SpringStrength;
        public float Dampening;
        public bool AddNoise;
        public float NoiseStrength;
        public float NoiseSpeed;
        public float NoiseScale;
        public bool UseSoftLimit;
        public float SoftLimitInfluence;
        public float SoftLimitStrength;

        public JiggleData(Assets.DuckType.Jiggle.Jiggle _jiggle)
        {
            jiggle = _jiggle;
            SpringStrength = _jiggle.SpringStrength;
            Dampening = _jiggle.Dampening;
            AddNoise = _jiggle.AddNoise;
            NoiseStrength = _jiggle.NoiseStrength;
            NoiseSpeed = _jiggle.NoiseSpeed;
            NoiseScale = _jiggle.NoiseScale;
            UseSoftLimit = _jiggle.UseSoftLimit;
            SoftLimitInfluence = _jiggle.SoftLimitInfluence;
            SoftLimitStrength = _jiggle.SoftLimitStrength;

        }
    }
}