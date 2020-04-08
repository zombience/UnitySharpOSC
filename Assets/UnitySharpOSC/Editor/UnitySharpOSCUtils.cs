using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.IO;


namespace UnitySharpOSC
{
    public class OSCDataWindow : EditorWindow
    {
        #region osc 
        List<string> displayMessages            = new List<string>();
        Queue<string> receivedMessages          = new Queue<string>();
        Queue<System.Action> receivedActions    = new Queue<System.Action>();
        HashSet<string> tempAddresses           = new HashSet<string>();
        HashSet<string> storedAddresses         = new HashSet<string>();
        List<string> addressList                = new List<string>();
        OSCConfig config;

        #endregion

        #region gui
        Vector2
            addressScroll,
            messagesScroll;

        GUIStyle style;
        string addressToAdd;
        #endregion

        [MenuItem("Window/SharpOSC")]
        static void Init()
        {
            EditorWindow.GetWindow<OSCDataWindow>().Show();
        }

        void OnEnable()
        {
            if (Application.isPlaying) return;

            style = new GUIStyle();
            style.normal.background = EditorUtilities.BackgroundTexture(Color.black, 1, 1);

            //EditorApplication.update += Update;
            config = OSCReceiver.GetConfig();
            OSCReceiver.InitializeOSCListenerAddressOnly(config, receivedActions, receivedMessages);
            storedAddresses = EditorUtilities.LoadOSCAddresses();
        }

        void OnGUI()
        {
            if(Application.isPlaying)
            {
                EditorGUILayout.LabelField("disabled during play mode");
                return;
            }

            // allow one frame in between shutting down listener
            // and starting it back up
            if(!OSCReceiver.isListening)
            {
                OSCReceiver.InitializeOSCListenerAddressOnly(config, receivedActions, receivedMessages);   
            }

            Color c = GUI.backgroundColor;
            SetColor(Color.black, Color.white);
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("osc config");

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("port: ");
            config.port = EditorGUILayout.DelayedIntField(config.port);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("allow debug: ");
            config.allowDebugLog = EditorGUILayout.Toggle(config.allowDebugLog);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("enable osc: ");
            config.oscEnabled = EditorGUILayout.Toggle(config.oscEnabled);
            EditorGUILayout.EndHorizontal();
            if(EditorGUI.EndChangeCheck())
            {
                OSCReceiver.WriteConfig(config);
                OSCReceiver.isListening = false;
            }


            EditorGUILayout.EndVertical();
            EditorGUILayout.LabelField("Stored Addresses", style);

            SetColor(Color.white, Color.cyan * .35f);
            GUILayout.BeginVertical(style);
            EditorGUILayout.Space();
            GUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("manually add address:");
            addressToAdd = EditorGUILayout.DelayedTextField(addressToAdd);
            GUILayout.FlexibleSpace();

            if(!string.IsNullOrEmpty(addressToAdd))
            {
                GUI.backgroundColor = Color.green;
                if(GUILayout.Button(" add "))
                {
                    if( addressToAdd[0] != '/')
                    {
                        addressToAdd = '/' + addressToAdd;
                    }
                    storedAddresses.Add(addressToAdd);
                    EditorUtilities.WriteOSCAddresses(storedAddresses);
                    addressToAdd = string.Empty;
                }
                GUI.backgroundColor = c;
            }
            GUILayout.EndHorizontal();
            EditorGUILayout.Space();
            GUILayout.EndVertical();

            SetColor(Color.white, Color.black);

            GUILayout.BeginVertical(style);
            string remove = null;
            foreach (var item in storedAddresses)
            {
                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(item);
                GUI.backgroundColor = Color.red;
                if(GUILayout.Button("remove"))
                {
                    remove = item;
                }
                GUI.backgroundColor = c;
                GUILayout.EndHorizontal();
            }
            if (remove != null)
            {
                storedAddresses.Remove(remove);
                EditorUtilities.WriteOSCAddresses(storedAddresses);
            }

            GUILayout.EndVertical();

            lock (receivedMessages)
            {
                SetColor(Color.black, Color.white);
                EditorGUILayout.LabelField("Live Addresses Received", style);
                SetColor(Color.white, Color.black);
                GUILayout.BeginVertical(style);
                addressScroll = GUILayout.BeginScrollView(addressScroll, false, false);
                foreach (var item in addressList)
                {
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(item);
                    if(!storedAddresses.Contains(item))
                    {
                        GUI.backgroundColor = Color.green;
                        if(GUILayout.Button("add"))
                        {
                            storedAddresses.Add(item);
                            EditorUtilities.WriteOSCAddresses(storedAddresses);
                        }
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndScrollView();
                GUILayout.EndVertical();

                SetColor(Color.black, Color.white * .05f);
                GUILayout.Space(10);

                SetColor(Color.black, Color.white);
                EditorGUILayout.LabelField("Live OSC Input", style);
                SetColor(Color.white, Color.black);
                GUILayout.BeginVertical(style);
                messagesScroll = GUILayout.BeginScrollView(messagesScroll, false, false);
                while (displayMessages.Count > 20)
                {
                    displayMessages.RemoveAt(0);
                }
                foreach (var msg in displayMessages)
                {
                    EditorGUILayout.LabelField(msg);
                }
                GUILayout.EndScrollView();
                GUILayout.EndVertical();
            }
        }

        void Update()
        {
            if (Application.isPlaying)
            {
                EditorApplication.update -= Update;
                return;
            }

            lock (receivedMessages)
            {
                while (receivedMessages.Count > 0)
                {
                    var msg = receivedMessages.Dequeue();
                    displayMessages.Add(msg);
                    int idx = msg.IndexOf(" val");
                    string addr = msg.Substring(0, idx).Replace("addr: ", "");
                    tempAddresses.Add(addr);
                }

                addressList = new List<string>();
                foreach (var item in tempAddresses)
                {
                    addressList.Add(item);
                }
                addressList.Sort();
            }
            lock (receivedActions)
            {
                while (receivedActions.Count > 0)
                {
                    receivedActions.Dequeue()?.Invoke();
                }
            }
            Repaint();
        }

        private void OnDestroy()
        {
            if (!Application.isPlaying)
            {
                OSCReceiver.isListening = false;
            }
            //EditorApplication.update -= Update;   
        }

        void SetColor(Color textCol, Color bgCol)
        {
            style.normal.textColor = textCol;
            style.normal.background = EditorUtilities.BackgroundTexture(bgCol, 1, 1);
        }
    }

    static public partial class EditorUtilities
    {
        static public Texture2D BackgroundTexture(this Color color, int width, int height)
        {
            Color[] pix = new Color[width * height];

            for (int i = 0; i < pix.Length; i++)
                pix[i] = color;

            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();

            return result;
        }


        #region address storage
        static public HashSet<string> LoadOSCAddresses()
        {
            var path = GetAddressStorageFilePath();
            HashSet<string> set = new HashSet<string>();
            if(!File.Exists(path))
            {
                if(!Directory.Exists(Application.streamingAssetsPath))
                {
                    Directory.CreateDirectory(Application.streamingAssetsPath);
                }
                File.Create(path);
                return set;
            }

            using (StreamReader sr = new StreamReader(path))
            {
                string line; 
                while((line = sr.ReadLine()) != null)
                {
                    set.Add(line);
                }
            }
            return set;
        }

        static public string GetAddressStorageFilePath()
        {
            var receiverClass = AssetDatabase.FindAssets("OSCReceiver t: TextAsset")
                    .Select(a => AssetDatabase.GUIDToAssetPath(a))
                    .FirstOrDefault();
            Debug.LogFormat("#EDITOR# OSCReceiver found at: {0}", receiverClass);

            // need system path to get full Assets/ project path
            var fullpath = Directory.GetParent(receiverClass).FullName;
            Debug.LogFormat("#EDITOR# OSCReceiver full path: {0}", fullpath);

            // convert back to assetdatabase-friendly path
            return Path.Combine(SystemToAssetPath(fullpath), "osc_addresses.txt");
        }

        /// <summary>
        /// takes a full system path and returns unity's assetdatabase-friendly path
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        static public string SystemToAssetPath(string path)
        {
            if (!Directory.Exists(path) && !File.Exists(path))
            {
                Debug.LogFormat("#EDITOR# couldn't convert to asset path: path {0} was invalid", path);
                return null;
            }
            var sub = path.Substring(path.IndexOf(@"Assets\"));
            //Debug.LogFormat("#EDITOR# converted {0} to asset path {1}", path, sub);
            return sub;
        }

        static public void WriteOSCAddresses(HashSet<string> set)
        {
            using (StreamWriter sw = new StreamWriter(GetAddressStorageFilePath()))
            {
                foreach (var item in set)
                {
                    sw.WriteLine(item);
                }
            }
        }
        #endregion
    }

}