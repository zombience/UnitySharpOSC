using UnityEngine;

using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using SharpOSC;

namespace UnitySharpOSC
{
    [System.Serializable]
    public class OSCConfig
    {
        public int port = 9023;
        public bool 
            allowDebugLog = false,
            // toggle osc listening in builds if necessary
            oscEnabled = true;
        public override string ToString()
        {
            return JsonUtility.ToJson(this);
        }
    }

    

    /// <summary>
    /// only one OSC Receiver on one port can be running
    /// leaving it as such for now. If you need more ports, 
    /// there are other unity osc packages out there such as Keijiro Takahashi's implementation
    /// </summary>
    static public class OSCReceiver
    {
        #region vars
        static public bool              isListening;
        static int                      listenPort;
        static Thread                   oscThread;
        static UdpClient                listenClient, sendClient;
        static IPEndPoint               sendTarget, listenEndPoint;
        static Queue<System.Action>     actionQueue;
        static Queue<string>            messageQueue;
        static UDPListener              sharpListener;
        static HandleOscPacket          packetHandler;
        #endregion

        #region OSC

        static public void InitializeOSCListener(OSCConfig config)
        {
            if(isListening && oscThread != null)
            {
                if(config.allowDebugLog)
                {
                    Debug.LogFormat("OSC thread already running... aborting");
                }
                return;
            }

            // osc has been disabled via config
            // this may be done for debug purposes, for example
            if (!config.oscEnabled) return;

            isListening             = true;
            listenPort              = config.port;
            packetHandler           = HandleOSCMessage;

            if(config.allowDebugLog)
            {
                SharpOSCLogListener bridge = new GameObject("osc_log_obj").AddComponent<SharpOSCLogListener>();
                actionQueue             = bridge.InvocationQueue;
            }

            oscThread               = new Thread(new ThreadStart(ReceiveOSC));
            oscThread.IsBackground  = true;
            oscThread.Name          = "OSCReceiver";
            oscThread.Start();
        }

        /// <summary>
        /// for editor use only, to display incoming messages 
        /// and capture addresses to file
        /// </summary>
        /// <param name="config"></param>
        /// <param name="invQueue"></param>
        /// <param name="queue"></param>
        static public void InitializeOSCListenerAddressOnly(OSCConfig config, Queue<System.Action> invQueue, Queue<string> queue)
        {
            Debug.LogFormat("SharpOSC| initializing receiver with settings {0}", config);
            if (isListening && oscThread != null)
            {
                Debug.LogFormat("OSC thread already running... aborting");
                return;
            }

            isListening             = true;
            listenPort              = config.port;
            actionQueue             = invQueue;
            messageQueue            = queue;
            packetHandler           = ExtractAddresses;

            oscThread               = new Thread(new ThreadStart(ReceiveOSC));
            oscThread.IsBackground  = true;
            oscThread.Name          = "OSCReceiver";
            oscThread.Start();
        }


        static public OSCConfig GetConfig()
        {
            string fullPath = Path.Combine(Application.streamingAssetsPath, "osc_config.json");
            if (!Directory.Exists(Application.streamingAssetsPath)) Directory.CreateDirectory(Application.streamingAssetsPath);
            if(!File.Exists(fullPath))
            {
                File.Create(fullPath);
                return new OSCConfig();
            }

            string contents;
            OSCConfig config;
            using (StreamReader sr = new StreamReader(fullPath))
            {
                contents = sr.ReadToEnd();
            }
            try
            {
                config = JsonUtility.FromJson<OSCConfig>(contents);
            }
            catch(System.Exception e)
            {
                config = new OSCConfig();
            }
            return config;
        }

        static public void WriteConfig(OSCConfig config)
        {
            string fullPath = Path.Combine(Application.streamingAssetsPath, "osc_config.json");
            if (!Directory.Exists(Application.streamingAssetsPath)) Directory.CreateDirectory(Application.streamingAssetsPath);

            string contents = JsonUtility.ToJson(config, true);
            using (StreamWriter sw = new StreamWriter(fullPath))
            {
                sw.WriteLine(contents);
            }
        }


        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void StartupOnLoad()
        {
            var config = GetConfig();
            Debug.LogFormat("SharpOSC| loading config. logging is {0}. if disabled during runtime, no more messages will follow. editor mode will always allow debug", config.allowDebugLog ? "enabled" : "disabled");
            InitializeOSCListener(config);
        }
        

        static void ReceiveOSC()
        {
            if(actionQueue != null)
            {
                lock(actionQueue)
                {
                    actionQueue.Enqueue(() => Debug.LogFormat("OSCListener| Starting OSC Listener thread"));
                }
            }

            try
            {
                sharpListener = new UDPListener(listenPort, packetHandler);
            }
            catch (System.Exception e)
            {
                if(actionQueue != null)
                {
                    lock(actionQueue)
                    {
                        actionQueue.Enqueue(() => Debug.LogFormat("#NETWORKING#| failed on OSC thread, error: \n{0}", e.Message));
                    }
                }
            }

            while (isListening)
            {   
                Thread.Sleep(5);
            }
            if (actionQueue != null)
            {
                lock (actionQueue)
                {
                    if (actionQueue != null)
                    {
                        actionQueue.Enqueue(() => Debug.LogFormat("OSCListener| shutdown osc listener"));
                    }
                }
            }
            sharpListener?.Close();
        }

        static void HandleOSCMessage(OscPacket packet)
        {
            var msg = packet as OscMessage;
            if (msg != null)
            {
                HandleMessage(msg);
                return;
            }
            
            var bundle = packet as OscBundle;
            if (bundle == null)
            {
                actionQueue.Enqueue(() => Debug.LogFormat("dropping failed OSC Packet: couldn't cast to message or bundle"));
                return;
            }

            for (int i = 0; i < bundle.Messages.Count; i++)
            {
                HandleMessage(bundle.Messages[i]);
            }
        }

        /// <summary>
        /// for editor window use in order to capture and display
        /// incoming addresses and store to file
        /// </summary>
        /// <param name="packet"></param>
        static void ExtractAddresses(OscPacket packet)
        {
            var msg = packet as OscMessage;
            if (msg != null)
            {
                lock(messageQueue)
                {
                    messageQueue.Enqueue(string.Format("addr: {0} val: {1}", msg.Address, msg.Arguments[0].ToString()));
                }
                return;
            }

            var bundle = packet as OscBundle;
            if (bundle == null)
            {
                //actionQueue.Enqueue(() => Debug.LogFormat("dropping failed OSC Packet: couldn't cast to message or bundle"));
                return;
            }
            lock(messageQueue)
            {
                for (int i = 0; i < bundle.Messages.Count; i++)
                {
                    messageQueue.Enqueue(string.Format("addr: {0} val: {1}", bundle.Messages[i].Address, bundle.Messages[i].Arguments[0].ToString()));
                }
            }
        }

        static void HandleMessage(OscMessage msg)
        {
            try
            {
                foreach (var arg in msg.Arguments)
                {
                    //actionQueue.Enqueue(() => Debug.LogFormat("received arg {0} of type {1} for addr: {2}", arg, arg.GetType(), msg.Address));
                    var type = arg.GetType();
                    if (type == typeof(float))
                    {
                        OSCDistributor.Broadcast(msg.Address, (float)arg);
                    }
                    else if (type == typeof(int))
                    {
                        OSCDistributor.Broadcast(msg.Address, (int)arg);
                    }
                    else if (type == typeof(double))
                    {
                        // ignoring double precision for now, not really necessary
                        var d = (double)arg;
                        OSCDistributor.Broadcast(msg.Address, (float)d);
                    }
                    else
                    {
                        actionQueue.Enqueue(() => Debug.LogFormat("arg {0} of address {1} is of not-implemented type {2}", msg.Arguments, msg.Address, type));
                    }
                }
            }
            catch (System.Exception e)
            {
                actionQueue.Enqueue(() => Debug.LogFormat("SharpOSC| failed to handle osc message, error: {0}", e.Message));
            }
        }
        #endregion
    }

    public class SharpOSCLogListener : MonoBehaviour
    {
        public Queue<System.Action> InvocationQueue { get => invocations; }
        Queue<System.Action> invocations = new Queue<System.Action>();

        #region mono implementation
        void Update()
        {
            lock (InvocationQueue)
            {
                while (invocations.Count > 0)
                {
                    InvocationQueue.Dequeue()?.Invoke();
                }
            }
        }
        #endregion
    }
}
