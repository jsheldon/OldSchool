using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace OldSchool.Extensibility
{
    public interface INetworkClient : IDisposable
    {
        IPAddress ClientAddress { get; }
        Action<Guid, byte[]> OnDataReceived { set; }
        Action<Guid> OnSendComplete { set; }
        Guid Id { get; }
        Task Send(Stream stream, IDictionary<string, object> properties);
        Task Send(string message, IDictionary<string, object> properties);
        void Disconnect();
    }
}