using System;
using UnityEngine;

namespace RShell
{
    public static partial class Shell
    {
        private static FunctionEvaluator m_FunctionEvaluator;
        private static UdpHost m_UdpHost;

        public static FunctionEvaluator FunctionEvaluator
        {
            get
            {
                if (m_FunctionEvaluator == null)
                    m_FunctionEvaluator = new FunctionEvaluator();
                return m_FunctionEvaluator;
            }
        }

        public static int SendMTU
        {
            get => m_UdpHost.SendMTU;
            set => m_UdpHost.SendMTU = value;
        }

        public static int SendInterval
        {
            get => m_UdpHost.SendInterval;
            set => m_UdpHost.SendInterval = value;
        }

        public static void Listen(int port = 9999)
        {
            if (m_UdpHost == null)
            {
                m_UdpHost = new UdpHost(port);
                m_UdpHost.MessageReceived += OnMessageReceived;
                m_UdpHost.Start();
                Application.quitting += OnApplicationQuit;
                Debug.Log($"Shell Listening... {port}");    
            }
            else
            {
                Debug.Log("Listen: Shell is already listening.. ");
            }
        }

        public static void StopListen()
        {
            if (m_UdpHost != null)
            {
                m_UdpHost.Stop();
                m_UdpHost = null;
                Application.quitting -= OnApplicationQuit;
            }
        }

        private static void OnApplicationQuit()
        {
            StopListen();
        }

        private static void OnMessageReceived(string code)
        {
            if (string.IsNullOrEmpty(code)) return;

            FunctionEvaluator.Execute(code, out var returnObj);
            var msg = returnObj == null ? "ok" : returnObj.ToString();
            Debug.Log($"Shell Execute: {code}\n{returnObj}");
            m_UdpHost.Send(msg);
        }

        public static object Execute(string code)
        {
            if (FunctionEvaluator.Execute(code, out var returnObj))
                return returnObj;
            throw new Exception($"execute failed \n{returnObj}");
        }

        public static void AddGlobalEnvironmentNameSpace(string nameSpace)
        {
            FunctionEvaluator.AddGlobalEnvironmentNameSpace(nameSpace);
        }
    }
}