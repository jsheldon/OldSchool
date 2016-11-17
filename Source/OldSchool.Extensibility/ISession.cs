using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace OldSchool.Extensibility
{
    public interface ISession
    {
        Guid ClientId { get; }
        IPAddress ClientAddress { get; }
        string Username { get; set; }
        IDictionary<string, object> Properties { get; set; }
        IModule ActiveModule { get; set; }
        bool IsLoggingOff { get; set; }
        Task Notify(string message);
        T GetDependency<T>();
    }
}