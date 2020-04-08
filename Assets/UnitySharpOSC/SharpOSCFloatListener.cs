using UnityEngine;

namespace UnitySharpOSC
{
    public class SharpOSCFloatListener : SharpOSCGameObjectBase
    {
        [SerializeField]
        OSCFloatValueHelper floatHelper;

        protected override void OnEnable()
        {
            OSCDistributor.RegisterObject(floatHelper);
        }

        void Update()
        {
            floatHelper.Update();
        }

        protected override void OnDisable()
        {
            OSCDistributor.UnregisterObject(floatHelper);
        }
    }
}
