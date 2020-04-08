using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitySharpOSC
{
    static public class OSCDistributor
    {
        static Dictionary<string, List<OSCFloatValueHelper>> floatCallbacks = new Dictionary<string, List<OSCFloatValueHelper>>();
        static Dictionary<string, List<OSCIntvalueHelper>> intCallbacks  = new Dictionary<string, List<OSCIntvalueHelper>>();

        static public void RegisterObject(OSCFloatValueHelper obj)
        {
            lock (floatCallbacks)
            {
                List<OSCFloatValueHelper> floatList;

                bool floatSuccess = floatCallbacks.TryGetValue(obj.address, out floatList);
                if (!floatSuccess)
                {
                    floatList = new List<OSCFloatValueHelper>();
                    floatCallbacks.Add(obj.address, floatList);
                }
                floatList.Add(obj as OSCFloatValueHelper);
            }
        }   
        static public void RegisterObject(OSCIntvalueHelper obj)
        { 
            lock(intCallbacks)
            {
                List<OSCIntvalueHelper> intList;
                bool intSuccess = intCallbacks.TryGetValue(obj.address, out intList);
                if (!intSuccess)
                {
                    intList = new List<OSCIntvalueHelper>();
                    intCallbacks.Add(obj.address, intList);
                }
                intList.Add(obj as OSCIntvalueHelper);
            }
        }

        static public void UnregisterObject(OSCFloatValueHelper obj)
        {
            lock (floatCallbacks)
            {
                List<OSCFloatValueHelper> floatList;
                bool success = floatCallbacks.TryGetValue(obj.address, out floatList);
                if (!success)
                {
                    return;
                }
                floatCallbacks.Remove(obj.address);
            }
        }
        static public void UnregisterObject(OSCIntvalueHelper obj)
        {
            lock (intCallbacks)
            {
                List<OSCIntvalueHelper> intList;
                bool intSuccess = intCallbacks.TryGetValue(obj.address, out intList);
                if (!intSuccess)
                {
                    return;
                }
                intList.Remove(obj as OSCIntvalueHelper);
            }
        }

        static public void Broadcast(string address, int value)
        {
            List<OSCIntvalueHelper> list;
            bool success = intCallbacks.TryGetValue(address, out list);
            if (success)
            {
                foreach (var obj in list)
                {
                    obj.ReceiveInt(value);
                }
            }
        }

        static public void Broadcast(string address, float value)
        {
            List<OSCFloatValueHelper> list;
            bool success = floatCallbacks.TryGetValue(address, out list);
            if(success)
            {
                foreach (var obj in list)
                {
                    obj.ReceiveFloat(value);
                }
            }
        }
    }
}
