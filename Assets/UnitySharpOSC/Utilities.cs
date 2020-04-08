using UnityEngine;

namespace UnitySharpOSC
{
    static public partial class Utilities
    {

        /// <summary>
        /// Map a number from one range (between inMin and inMax) to another (between outMin and outMax)
        /// </summary>
        /// <param name="value"></param>
        /// <param name="inMin"></param>
        /// <param name="inMax"></param>
        /// <param name="outMin"></param>
        /// <param name="outMax"></param>
        /// <returns>the value mapped to the new range</returns>
        public static float Map(this float value, float inMin, float inMax, float outMin, float outMax)
        {
            return (value - inMin) / (inMax - inMin) * (outMax - outMin) + outMin;   
        }

        /// <summary>
        /// Map a number from one range (between inMin and inMax) to another (between outMin and outMax)
        /// Clamping the result makes sure the output never exceeds the boundaries of the output range, 
        /// even for cases where outMax is less than outMin
        /// </summary>
        /// <param name="value"></param>
        /// <param name="inMin"></param>
        /// <param name="inMax"></param>
        /// <param name="outMin"></param>
        /// <param name="outMax"></param>
        /// <returns>the value mapped to the new range with clamping</returns>
        public static float MapClamp(this float value, float inMin, float inMax, float outMin, float outMax)
        {
            return Mathf.Clamp((value - inMin) / (inMax - inMin) * (outMax - outMin) + outMin, outMin, outMax);
        }
    }
}
