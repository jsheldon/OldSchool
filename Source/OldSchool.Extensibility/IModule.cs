using System.Threading.Tasks;

namespace OldSchool.Extensibility
{
    /// <summary>
    ///     This is the main interface implemented to add a module to the system.
    /// </summary>
    public interface IModule
    {
        string Name { get; set; }
        Task OnSessionDisconnecting(ISessionContext context);
        Task OnDataReceived(ISessionContext context);
        Task Activate(ISessionContext context);
        Task Initialize();
    }
}