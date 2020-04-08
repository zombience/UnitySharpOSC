using UnityEngine;
namespace UnitySharpOSC
{
    public class SharpOSCIntListener : SharpOSCGameObjectBase
    {
        [SerializeField]
        OSCIntvalueHelper intHelper;

        protected override void OnEnable()
        {
            OSCDistributor.RegisterObject(intHelper);
        }

        void Update()
        {
            intHelper.Update();
        }


        protected override void OnDisable()
        {
            OSCDistributor.UnregisterObject(intHelper);
        }

    }
}
