using System;
using System.IO;
using UnityEngine;

namespace RShell
{
    public static partial class Shell
    {
        private static FunctionEvaluator m_FunctionEvaluator;
        private static UdpHost m_UdpHost;

        private static FunctionEvaluator FunctionEvaluator
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
            get
            {
                return m_UdpHost.SendMTU;
            }
            set
            {
                m_UdpHost.SendMTU = value;
            }
        }

        public static int SendInterval
        {
            get
            {
                return m_UdpHost.SendInterval;
            }
            set
            {
                m_UdpHost.SendInterval = value;
            }
        }

        public static void Listen(int port = 9999)
        {
            if (m_UdpHost == null)
            {
                try
                {
                    m_UdpHost = new UdpHost(port);
                }
                catch (Exception e)
                {
                    Debug.LogWarning("RShell Listen failed: " + e.Message);
                    return;
                }
               
                m_UdpHost.MessageReceived += OnMessageReceived;
                m_UdpHost.Start();
                Application.quitting += OnApplicationQuit;
                Debug.Log($"RShell Listening... {port}");    
            }
            else
            {
                Debug.Log("RShell is already listening.. ");
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
            object returnObj;
            FunctionEvaluator.Execute(code, out returnObj);
            var msg = returnObj == null ? "ok" : returnObj.ToString();
            Debug.Log($"RShell Execute: {code}\n{returnObj}");
            m_UdpHost.Send(msg);
        }

        public static object Execute(string code)
        {
            object returnObj;
            if (FunctionEvaluator.Execute(code, out returnObj))
                return returnObj;
            throw new Exception($"RShell execute failed \n{returnObj}");
        }

        public static void AddGlobalEnvironmentNameSpace(string nameSpace)
        {
            FunctionEvaluator.AddGlobalEnvironmentNameSpace(nameSpace);
        }

        public static int LoadLibrary(string libname)
        {
            string tmpPath = $"/data/local/tmp/{libname}";
            string appPath = $"/data/data/{Application.identifier}/{libname}";
            bool tmpPathExists = File.Exists(tmpPath);
            bool appPathExists = File.Exists(appPath);
            if(!tmpPathExists && !appPathExists)
            {
                return 1;
            }

            if(tmpPathExists)
            {
                File.Copy(tmpPath, appPath, true);
            }
            
            var systemClass = new AndroidJavaClass("java.lang.System");
            try
            {
                systemClass.CallStatic("load", appPath);
            }
            catch(Exception e)
            {
                Debug.LogException(e);
                return 2;
            }

            return 0;
        }
    }
}