using System.Threading.Tasks;

namespace OldSchool.Extensibility
{
    public interface IProvider
    {
        IProvider Next { get; set; }
        Task OnDataReceived(ISessionContext context);
        Task OnSessionCreated(ISessionContext context);
        Task OnSessionDisconnecting(ISessionContext context);
        Task OnModulesProcessed(ISessionContext context);
    }
}