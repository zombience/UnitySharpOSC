using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

namespace UnitySharpOSC
{
    public enum OSCDataType
    {
        None    = 0,
        Int     = 1,
        Float   = 2,
    }

    [System.Serializable]
    public class FloatEvent : UnityEvent<float> { }
    [System.Serializable]
    public class IntEvent : UnityEvent<int> { }

    public abstract class SharpOSCGameObjectBase : MonoBehaviour
    {
        /// <summary>
        /// for editor script access: 
        /// allow for changing address in play mode from inspector
        /// </summary>
        public void OnAddressUpdated()
        {
            OnDisable();
            OnEnable();
        }

        protected abstract void OnEnable();
        protected abstract void OnDisable();
        
    }


    [System.Serializable]
    public class OSCFloatValueHelper
    {
        [HideInInspector]
        public string address = "no address assigned";

        Queue<float> FloatQueue => floatQueue ?? (floatQueue = new Queue<float>(1));
        Queue<float> floatQueue;

        [SerializeField, Header("remap input values")]
        float
            inMin = 0;
        [SerializeField]
        float
            inMax = 1f,
            outMin = -1f,
            outMax = 1f;

        [SerializeField]
        FloatEvent floatEvent = new FloatEvent();

        public void ReceiveFloat(float value)
        {
            // only maintain latest value
            lock (FloatQueue)
            {
                FloatQueue.Clear();
                FloatQueue.Enqueue(value);
            }
        }

        public void Update()
        {
            lock (FloatQueue)
            {
                while (FloatQueue.Count > 0)
                {
                    float val   = FloatQueue.Dequeue();
                    val         = val.MapClamp(inMin, inMax, outMin, outMax);
                    floatEvent.Invoke(val);
                }
            }
        }
    }

    [System.Serializable]
    public class OSCIntvalueHelper
    {
        [HideInInspector]
        public string address = "no address assigned";

        Queue<int> IntQueue => intQueue ?? (intQueue = new Queue<int>(1));
        Queue<int> intQueue;

        [SerializeField, Header("multiply received values")]
        int multiplier = 1;

        [SerializeField]
        IntEvent intEvent = new IntEvent();

        public void ReceiveInt(int value)
        {
            // only maintain latest value
            lock (IntQueue)
            {
                IntQueue.Clear();
                IntQueue.Enqueue(value);
            }
        }

        public void Update()
        {
            lock(IntQueue)
            {
                while(IntQueue.Count > 0)
                {
                    int val = IntQueue.Dequeue();
                    val     *= multiplier;
                    intEvent.Invoke(val);
                }
            }
        }
    }
}
