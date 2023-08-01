using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace RShell
{
    public class UdpHost
    {
        [Serializable]
        private class Message
        { 
            public int MsgId;
            public int FragIndex;
            public int FragCount;
            public string Content;
        }
        
        public int SendMTU = 1000; // bytes
        public int SendInterval = 100; // ms
        
        public event Action<string> MessageReceived;
        private readonly UdpClient m_UdpClient;
        private readonly ConcurrentQueue<Message> m_MessagesToSend = new ConcurrentQueue<Message>();
        private bool m_IsSending;

        private IPEndPoint m_ReceiveRemoteEndPoint;
        private IPEndPoint m_SendRemoteEndPoint;
        private string m_RemoteIP; 
        private int m_RemotePort;
        private SynchronizationContext m_MainContext;
        private Thread m_Thread;
        private int m_MsgId;

        public UdpHost(int localPort)
        {
            m_UdpClient = new UdpClient(localPort);
        }

        public void Send(string content)
        {
            m_MsgId++;
            
            if (content.Length < SendMTU)
            {
                AddToSendQueue(content, m_MsgId);
            }
            else
            {
                var total = Mathf.CeilToInt(content.Length / (float) SendMTU);
                for (int i = 0; i < total; i++)
                {
                    var index = i * SendMTU;
                    var len = Mathf.Min(SendMTU, content.Length - index);
                    AddToSendQueue(content.Substring(index, len), m_MsgId, i, total);
                }
            }
        }

        private void AddToSendQueue(string content, int msgId, int fragIndex = 0, int fragCount = 1)
        {
            var message = new Message
            {
                Content = content,
                MsgId = msgId,
                FragIndex = fragIndex,
                FragCount = fragCount
            };

            m_MessagesToSend.Enqueue(message);
            
            if(!m_IsSending)
                SendAsync();
        }

        async void SendAsync()
        {
            m_IsSending = true;

            try
            {
                while (m_MessagesToSend.TryDequeue(out var message))
                {
                    var sendBytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(message));
                    lock (this)
                    {
                        m_UdpClient.Send(sendBytes, sendBytes.Length, m_SendRemoteEndPoint);
                    }

                    await System.Threading.Tasks.Task.Delay(SendInterval);
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }
            finally
            {
                m_IsSending = false;    
            }
        }

        public void Start()
        {
            m_MainContext = SynchronizationContext.Current;
            m_Thread = new Thread(Run) { Name = "UdpShell" };
            m_Thread.Start();
        }

        public void Stop()
        {
            m_UdpClient?.Close();
            m_Thread?.Abort();
        }
        
        private void Run()
        {
            m_ReceiveRemoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
            while (true)
            {
                try
                {
                    var receiveBytes = m_UdpClient.Receive(ref m_ReceiveRemoteEndPoint);
                    lock (this)
                    {
                        m_SendRemoteEndPoint = m_ReceiveRemoteEndPoint;
                    }
                    
                    OnReceiveMessage(receiveBytes);
                }
                catch (Exception e)
                {
                    if(e is ThreadAbortException)
                        return;
                    Debug.LogError(e.Message);
                }

                Thread.Sleep(200);
            }
        }
        
        private void OnReceiveMessage(byte[] data)
        {
            if (data == null)
            {
                return;
            }
            
            var text = Encoding.UTF8.GetString(data, 0, data.Length);
            if (text == "hi")
            {
                Send("welcome");
            }
            else
            {
                m_MainContext.Post((_) =>
                {
                    MessageReceived?.Invoke(text);
                }, null);
            }
        }
    }
}
