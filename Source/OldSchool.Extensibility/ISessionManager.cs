using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OldSchool.Extensibility
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
}