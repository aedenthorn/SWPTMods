using UnityEngine;

namespace MirrorMod
{
    public class MirrorScript : MonoBehaviour
    {
        public Transform playerCam;
        public Transform mirrorCam;
        public void Update()
        {
            CalcRotation();
        }
        public void CalcRotation()
        {
            Vector3 dir = (playerCam.position - transform.position).normalized;
            Quaternion rot = Quaternion.LookRotation(dir);

            rot.eulerAngles = transform.eulerAngles - rot.eulerAngles;

            mirrorCam.localRotation = rot;
        }
    }
}