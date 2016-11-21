using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using OldSchool.Common;
using OldSchool.Extensibility;

namespace OldSchool.Ifx.Networking
{
    public interface ISocketService : IDisposable
    {
        int Port { get; set; }
        Action<INetworkClient> OnClientConnected { get; set; }
        Action<Guid> OnClientDisconnected { get; set; }
        List<INetworkClient> Clients { get; }
        void Start();
        void Stop();
    }

    public class SocketService : ISocketService
    {
        private static readonly object m_Lock = new object();

        private Socket m_Socket;

        public List<INetworkClient> Clients { get; private set; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Start()
        {
            Clients = new List<INetworkClient>();
            if (OnClientConnected == null)
                throw new Exception("You must specify a callback on client connect.");

            if (OnClientDisconnected == null)
                throw new Exception("You must specify a callback on client connect.");

            m_Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var ipAddress = new IPAddress(0); // 0.0.0.0
            var ipEndPoint = new IPEndPoint(ipAddress, Port);
            m_Socket.Bind(ipEndPoint);
            m_Socket.Listen(50);
            m_Socket.BeginAccept(OnClientAccept, null);
        }

        public void Stop()
        {
            m_Socket.Close();
            m_Socket.Dispose();
            m_Socket = null;
            lock (m_Lock)
            {
                Clients.Each(a => { a.Dispose(); });
                Clients.Clear();
            }
        }

        public int Port { get; set; } = 23;
        public Action<INetworkClient> OnClientConnected { get; set; }
        public Action<Guid> OnClientDisconnected { get; set; }

        private void OnClientAccept(IAsyncResult asyncResult)
        {
            if (m_Socket == null)
                return; // Server Shutting Down

            var socket = m_Socket.EndAccept(asyncResult);

            if (socket == null)
                return; // Server Shutting Down

            var client = new TelnetClient(socket)
                         {
                             OnClientTerminated = OnClientTerminated
                         };

            lock (m_Lock)
            {
                Clients.Add(client);
            }

            OnClientConnected(client);
            m_Socket?.BeginAccept(OnClientAccept, null);
        }

        private void OnClientTerminated(INetworkClient client)
        {
            lock (m_Lock)
            {
                Clients.Remove(client);
            }

            OnClientDisconnected(client.Id);
            client.Dispose();
        }

        private void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            Stop();
        }
    }
}