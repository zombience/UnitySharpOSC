using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;

namespace UnitySharpOSC
{

    [CustomEditor(typeof(SharpOSCGameObjectBase), true)]
    public class SharpOSCGameObjectInspector : Editor
    {

        SerializedProperty
            intHelper,
            floatHelper;

        Vector2 
            intScroll, 
            floatScroll;

        string[] addressArray;

        int selected;

        GUIStyle style;


        void OnEnable()
        {
            intHelper   = serializedObject.FindProperty("intHelper");
            floatHelper = serializedObject.FindProperty("floatHelper");

            addressArray = EditorUtilities.LoadOSCAddresses().ToArray();


            var prop = intHelper != null ? intHelper : floatHelper;
            var addressProp = prop.FindPropertyRelative("address");
            var name = string.IsNullOrEmpty(addressProp.stringValue) ? "no address assigned" : addressProp.stringValue;

            if(addressArray.Contains(name))
            {
                selected = addressArray
                    .ToList()
                    .IndexOf(name);
            }

            // remove the leading slash from osc addresses
            // EditorGUI popup interprets slash as a submenu indicator
            for (int i = 0; i < addressArray.Length; i++)
            {
                addressArray[i] = addressArray[i].Substring(1);
            }

            style = new GUIStyle();
            SetColor(Color.white, Color.black);
        }

        public override void OnInspectorGUI()
        {
            if(intHelper != null) DisplayProp(intHelper);
            if(floatHelper != null) DisplayProp(floatHelper);
            
            serializedObject.ApplyModifiedProperties();
        }

        void DisplayProp(SerializedProperty prop)
        {
            var addressProp = prop.FindPropertyRelative("address");
            var name = string.IsNullOrEmpty(addressProp.stringValue) ? "no address assigned" : addressProp.stringValue;

            EditorGUILayout.BeginHorizontal(style);
            EditorGUILayout.LabelField("available addresses:");
            EditorGUI.BeginChangeCheck();
            selected = EditorGUILayout.Popup(selected, addressArray);
            if (EditorGUI.EndChangeCheck())
            {
                addressProp.stringValue = '/' + addressArray[selected];
                serializedObject.ApplyModifiedProperties();
                if(Application.isPlaying)
                {
                    (target as SharpOSCGameObjectBase).OnAddressUpdated();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(prop, new GUIContent(addressProp.stringValue));
        }

        void SetColor(Color textCol, Color bgCol)
        {
            style.normal.textColor = textCol;
            style.normal.background = EditorUtilities.BackgroundTexture(bgCol, 1, 1);
        }
    }
}
