using UnityEngine;
namespace UnitySharpOSC
{
    public class SharpOSCLocalPositionSetter : MonoBehaviour
    {
        public float LocalX
        {
            set
            {
                Vector3 pos = transform.localPosition;
                pos.x = value;
                transform.localPosition = pos;
            }
        }

        public float LocalY
        {
            set
            {
                Vector3 pos = transform.localPosition;
                pos.y = value;
                transform.localPosition = pos;
            }
        }

        public float LocalZ
        {
            set
            {
                Vector3 pos = transform.localPosition;
                pos.z = value;
                transform.localPosition = pos;
            }
        }

    }
}
