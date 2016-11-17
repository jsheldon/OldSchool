using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OldSchool.Extensibility;
using OldSchool.Ifx.Networking;
using OldSchool.Ifx.Providers;
using OldSchool.Ifx.Session;

namespace OldSchool.Ifx.Managers
{
    public interface ISessionManager
    {
        List<ISession> Sessions { get; }

        void Register<T>()
            where T : IProvider;

        Task OnDataReceived(Guid id, byte[] data);
        Task Add(INetworkClient client);
        Task Remove(Guid id);

        Task Broadcast<T>(string message, params Guid[] exclusions)
            where T : IModule;
    }


    public class SessionManager : ISessionManager
    {
        private readonly IDependencyManager m_DependencyManager;
        private readonly object m_Lock = new object();
        private readonly IList<IProvider> m_Providers;
        private readonly ISocketService m_SocketService;

        public SessionManager(IDependencyManager dependencyManager, ISocketService socketService)
        {
            m_DependencyManager = dependencyManager;
            m_SocketService = socketService;
            m_Providers = new List<IProvider>();
            Sessions = new List<ISession>();
        }

        public async Task Broadcast<T>(string message, params Guid[] exclusions)
            where T : IModule
        {
            var query = from a in Sessions
                        where a.ActiveModule.GetType() == typeof(T)
                        select a;

            if (exclusions.Length > 0)
            {
                foreach (var exclusion in exclusions)
                    query = query.Where(a => a.ClientId != exclusion);
            }

            var clientList = query.ToList();
            foreach (var client in clientList)
            {
                await client.Notify(message);
            }
        }

        public List<ISession> Sessions { get; }

        public void Register<T>()
            where T : IProvider
        {
            var instance = m_DependencyManager.GetInstance<T>();
            if (m_Providers.Count != 0)
            {
                var last = m_Providers.Last();
                last.Next = instance;
            }

            m_Providers.Add(instance);
        }

        public async Task OnDataReceived(Guid id, byte[] data)
        {
            ISession session;
            lock (m_Lock)
            {
                session = Sessions.FirstOrDefault(a => a.ClientId == id);
            }

            if (session == null)
                return;

            var context = new SessionContext { Session = session };
            context.Request.Append(data);
            var provider = m_Providers.FirstOrDefault();
            if (provider == null)
                return;

            await provider.OnDataReceived(context);

            if (session.ActiveModule != null)
                await session.ActiveModule.OnDataReceived(context);

            var client = m_SocketService.Clients.FirstOrDefault(a => a.Id == id);

            // Currently no awaited intentionally, don't want to disconnect before this has completed.  
#pragma warning disable 4014
            client?.Send(context.Response.Body, context.Session.Properties);
#pragma warning restore 4014

            if (session.IsLoggingOff)
                client?.Disconnect();
        }

        public async Task Add(INetworkClient client)
        {
            var session = new Session.Session(client);
            lock (m_Lock)
            {
                Sessions.Add(session);
            }

            session.DependencyManager = m_DependencyManager.CreateChildContainer();

            await OnSessionCreated(session);

            //await Invoke(new ConnectedEvent(), client.Id, m_Empty);
        }

        public async Task Remove(Guid id)
        {
            await OnSessionDisconnecting(id);
            //await Invoke(new DisconnectEvent(), id, m_Empty);
            lock (m_Lock)
            {
                Sessions.RemoveAll(a => a.ClientId == id);
            }
        }

        private async Task OnSessionCreated(ISession session)
        {
            var provider = m_Providers.FirstOrDefault();
            if (provider == null)
                return;

            var context = new SessionContext { Session = session };
            await provider.OnSessionCreated(context);

            var client = m_SocketService.Clients.FirstOrDefault(a => a.Id == session.ClientId);

            // Currently no awaited intentionally, don't want to disconnect before this has completed.  
#pragma warning disable 4014
            client?.Send(context.Response.Body, context.Session.Properties);
#pragma warning restore 4014

            if (session.IsLoggingOff)
                client?.Disconnect();
        }

        private async Task OnSessionDisconnecting(Guid id)
        {
            ISession session;
            lock (m_Lock)
            {
                session = Sessions.FirstOrDefault(a => a.ClientId == id);
            }

            if (session == null)
                return;

            var provider = m_Providers.FirstOrDefault();
            if (provider == null)
                return;

            var context = new SessionContext { Session = session };
            await provider.OnSessionDisconnecting(context);

            if (session.ActiveModule != null)
                await session.ActiveModule.OnSessionDisconnecting(context);

            var client = m_SocketService.Clients.FirstOrDefault(a => a.Id == id);

            // Currently no awaited intentionally, don't want to disconnect before this has completed.  
#pragma warning disable 4014
            client?.Send(context.Response.Body, context.Session.Properties);
#pragma warning restore 4014

            if (session.IsLoggingOff)
                client?.Disconnect();
        }
    }
}