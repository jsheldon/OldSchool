using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using OldSchool.Extensibility;
using OldSchool.Ifx.Networking;

namespace OldSchool.Ifx.Session
{
    public class Session : ISession
    {
        private readonly INetworkClient m_Client;

        public Session(INetworkClient client)
        {
            m_Client = client;
            Properties = new Dictionary<string, object>();
            Properties.Set(SessionConstants.IsAuthenticated, false);
        }

        public IDependencyManager DependencyManager { get; set; }

        public Guid ClientId => m_Client.Id;
        public IPAddress ClientAddress => m_Client.ClientAddress;

        public string Username { get; set; }
        public IDictionary<string, object> Properties { get; set; }
        public IModule ActiveModule { get; set; }
        public bool IsLoggingOff { get; set; }

        public async Task Notify(string message)
        {
            await m_Client.Send(message, Properties);
        }

        public T GetDependency<T>()
        {
            return DependencyManager.GetInstance<T>();
        }
    }
}