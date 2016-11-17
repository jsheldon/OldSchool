using System;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;
using OldSchool.Extensibility;
using OldSchool.Ifx;
using OldSchool.Ifx.Managers;
using OldSchool.Ifx.Networking;
using OldSchool.Ifx.Providers;
using OldSchool.Ifx.Session;

namespace OldSchool.Service
{
    public partial class TelnetHost : ServiceBase, IService
    {
        private readonly IDependencyManager m_DependencyManager;
        private readonly ISessionManager m_SessionManager;
        private ISocketService m_SocketService;

        public TelnetHost(IDependencyManager dependencyManager, ISessionManager sessionManager)
        {
            m_DependencyManager = dependencyManager;
            m_SessionManager = sessionManager;
            m_SessionManager.Register<AuthenticationProvider>();
            m_SessionManager.Register<MenuProvider>();
            m_SessionManager.Register<LogOffProvider>();
            InitializeComponent();
        }

        public void Start()
        {
            OnStart(null);
        }

        protected override void OnStart(string[] args)
        {
            if (m_SocketService != null)
                return;

            Console.WriteLine("Starting server...");

            m_SocketService = m_DependencyManager.GetInstance<ISocketService>();
            m_SocketService.Port = 23; // TODO: make this configurable
            m_SocketService.OnClientConnected = async a => await OnClientConnected(a);
            m_SocketService.OnClientDisconnected = async id => await m_SessionManager.Remove(id);
            m_SocketService.Start();

            Console.WriteLine($"Server Started.  Listening on port {m_SocketService.Port}");
        }

        private async Task OnClientConnected(INetworkClient client)
        {
            await m_SessionManager.Add(client);
            client.OnSendComplete = OnSendComplete;
            client.OnDataReceived = async (id, data) => await m_SessionManager.OnDataReceived(id, data);
        }

        private void OnSendComplete(Guid id)
        {
            var session = m_SessionManager.Sessions.FirstOrDefault(a => a.ClientId == id);
            session?.Properties.Set(SessionConstants.MaskNextInput, false);
        }

        protected override void OnStop()
        {
            if (m_SocketService == null)
                return;

            Console.WriteLine("Shutting down server...");
            m_SocketService?.Dispose();
            m_SocketService = null;
            Console.WriteLine("Server shutdown.");
        }
    }
}