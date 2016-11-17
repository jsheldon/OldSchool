using System.IO;

namespace OldSchool.Extensibility
{
    public interface ISessionRequest
    {
        Stream Body { get; set; }
        ISessionContext Context { get; }
        T Get<T>(string key);
        ISessionRequest Set<T>(string key, T value);
        void Append(string data);
        void Append(byte[] data);
        void Clear();
    }
}