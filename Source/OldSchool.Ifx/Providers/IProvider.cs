using System.Threading.Tasks;
using OldSchool.Extensibility;

namespace OldSchool.Ifx.Providers
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