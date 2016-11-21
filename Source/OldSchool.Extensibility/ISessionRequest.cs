using System.IO;
using System.Threading.Tasks;

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
        Task<byte[]> GetBytes();
    }
}