#if NET_4_6
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Networking;

namespace Unity.Tiny
{
    public class UTinyServer
    {
        #region Fields

        public static UTinyServer Instance { get; private set; }

        private bool m_IsListening;
        private int m_Port = 7000, m_HostId, m_ChannelId;
        private List<int> m_Connections;
        private byte[] m_Buffer;

        private Process m_LocalServerProcess;

        #endregion

        #region Properties

        public int Port
        {
            get { return m_Port; }
            set { m_Port = value; }
        }

        public bool Connected => m_Connections != null && m_Connections.Count > 0;

        public string LocalIPAddress
        {
            get
            {
                string localIP;
                try
                {
                    // Connect a UDP socket and read its local endpoint. This is more accurate
                    // way when there are multi ip addresses available on local machine.
                    using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
                    {
                        socket.Connect("8.8.8.8", 65530);
                        IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                        localIP = endPoint.Address.ToString();
                    }
                }
                catch (SocketException)
                {
                    // Network unreachable? Use loopback address
                    localIP = "127.0.0.1";
                }
                return $"{localIP}:{m_Port}";
            }
        }

        #endregion

        #region Private Methods

        [InitializeOnLoadMethod]
        private static void SetupEditorHooks()
        {
            // Instantiate unique server
            Instance = new UTinyServer();

            // Monitor context changed event
            UTinyEditorApplication.OnLoadProject += p => { Instance.Listen(); };
            UTinyEditorApplication.OnCloseProject += p => { Instance.Close(); };
        }


        private static void OnEditorUpdate()
        {
            Instance.Receive();
        }

        private UTinyServer()
        {
        }

        private bool HostContent(string contentDir)
        {
            StopHostingContent();

            // Setup a signal event with handlers
            string stdout = "", stderr = "";
            ManualResetEvent isRunning = new ManualResetEvent(false);
            DataReceivedEventHandler outputReceived = (sender, e) => { stdout += e.Data; isRunning.Set(); };
            DataReceivedEventHandler errorReceived = (sender, e) => { stderr += e.Data; isRunning.Set(); };

            // Start new local http server
            var ppid = Process.GetCurrentProcess().Id;
            var port = UTinyEditorApplication.Project.Settings.LocalHTTPServerPort;
            var httpServerDir = new DirectoryInfo(UTinyBuildPipeline.GetToolDirectory("httpserver"));
            var unityVersion = InternalEditorUtility.GetUnityVersion();
            var profilerVersion = unityVersion.Major > 2018 || (unityVersion.Major == 2018 && unityVersion.Minor > 1) ? 0x20180123 : 0x20170327;
            m_LocalServerProcess = UTinyBuildUtilities.RunNodeNoWait(httpServerDir, "index.js", $"--pid {ppid} --port {port} --dir \"{contentDir}\" --profiler " + profilerVersion, outputReceived, errorReceived);
            if (m_LocalServerProcess == null)
            {
                throw new Exception("Failed to create local http server process.");
            }

            // Wait for the process to write something in either stdout or stderr
            isRunning.WaitOne(3000);

            // Check if process state is valid
            if (m_LocalServerProcess.HasExited || stderr.Length > 0)
            {
                var errorMsg = "Failed to start local http server.";
                if (stderr.Length > 0)
                {
                    errorMsg += $"\n{stderr}";
                }
                else if (stdout.Length > 0)
                {
                    errorMsg += $"\n{stdout}";
                }
                UnityEngine.Debug.LogError(errorMsg);
                return false;
            }
            return true;
        }

        private void StopHostingContent()
        {
            if (m_LocalServerProcess != null)
            {
                if (!m_LocalServerProcess.HasExited)
                {
                    m_LocalServerProcess.Kill();
                }
                m_LocalServerProcess.Dispose();
                m_LocalServerProcess = null;
            }
        }

        private void Listen()
        {
            if (m_IsListening)
                return;

            // Make sure server is closed before we reload assemblies
            AssemblyReloadEvents.beforeAssemblyReload += () => { Close(); };

            // Make sure server is closed if we exit Unity
            AppDomain.CurrentDomain.ProcessExit += (sender, e) => { Close(); };
            AppDomain.CurrentDomain.DomainUnload += (sender, e) => { Close(); };

            // Initialize live link websocket server
            NetworkTransport.Init();

            var connectionConfig = new ConnectionConfig();
            m_ChannelId = connectionConfig.AddChannel(QosType.ReliableSequenced);

            var hostTopology = new HostTopology(connectionConfig, 10);
            m_HostId = NetworkTransport.AddWebsocketHost(hostTopology, m_Port);

            m_IsListening = true;
            m_Connections = new List<int>();
            m_Buffer = new byte[1024];

            // Hook up editor update event
            EditorApplication.update += OnEditorUpdate;
        }

        private int Write(string[] arguments)
        {
            if (m_Buffer == null)
            {
                return 0;
            }

            int index = 0;
            foreach (var argument in arguments)
            {
                var data = Encoding.ASCII.GetBytes(argument);
                if (data.Length > 255)
                {
                    throw new ArgumentException("Argument length does not fit in a byte.");
                }

                m_Buffer[index++] = (byte)data.Length;
                data.CopyTo(m_Buffer, index);
                index += data.Length;
            }
            return index;
        }

        private void Receive()
        {
            if (!m_IsListening)
            {
                return;
            }

            int connectionId, channelId, received;
            byte error;

            var eventType = NetworkTransport.ReceiveFromHost(m_HostId, out connectionId, out channelId, m_Buffer, m_Buffer.Length, out received, out error);
            if ((NetworkError)error != NetworkError.Ok)
            {
                HandleError((NetworkError)error);
                return;
            }

            switch (eventType)
            {
                case NetworkEventType.ConnectEvent:
                    m_Connections.Add(connectionId);
                    break;
                case NetworkEventType.DataEvent:
                    // handle data received...
                    break;
                case NetworkEventType.DisconnectEvent:
                    m_Connections.Remove(connectionId);
                    break;
                case NetworkEventType.BroadcastEvent:
                case NetworkEventType.Nothing:
                    break;
            }
        }

        private void HandleError(NetworkError error)
        {
            UnityEngine.Debug.LogError(error.ToString());
            Close();
        }

        #endregion

        #region Public Methods

        public void ReloadOrOpen(string contentDir)
        {
            if (!string.IsNullOrEmpty(contentDir))
            {
                var port = UTinyEditorApplication.Project.Settings.LocalHTTPServerPort;
                var contentURL = $"http://localhost:{port}/";

                // Update content hosting server with new content directory
                if (!HostContent(contentDir))
                {
                    contentURL = $"file://{Path.Combine(contentDir, "index.html")}";
                }

                // Reload or open content URL
                if (Connected)
                {
                    Send("reload", contentURL);
                }
                else
                {
                    Application.OpenURL(contentURL);
                }
            }
        }

        public void Close()
        {
            if (!m_IsListening)
            {
                return;
            }

            EditorApplication.update -= OnEditorUpdate;

            m_Buffer = null;
            m_Connections = null;
            m_IsListening = false;
            m_HostId = m_ChannelId = 0;

            NetworkTransport.Shutdown();
            StopHostingContent();
        }

        public void Send(params string[] arguments)
        {
            if (!m_IsListening)
            {
                return;
            }

            byte error;
            var size = Write(arguments);
            foreach (var connection in m_Connections)
            {
                NetworkTransport.Send(m_HostId, connection, m_ChannelId, m_Buffer, size, out error);
                if ((NetworkError)error != NetworkError.Ok)
                {
                    HandleError((NetworkError)error);
                    return;
                }
            }
            UnityEngine.Debug.Log($"Sent {arguments[0]} command to {m_Connections.Count} {UTinyConstants.ApplicationName} instance{(m_Connections.Count > 1 ? "s" : "")}");
        }

        #endregion
    }
}
#endif // NET_4_6
