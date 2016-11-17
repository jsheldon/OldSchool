using System.IO;
using System.Threading.Tasks;

namespace OldSchool.Extensibility
{
    public interface ISessionResponse
    {
        Stream Body { get; set; }
        ISessionContext Context { get; }
        T Get<T>(string key);
        ISessionResponse Set<T>(string key, T value);
        Task Append(string data);
        Task Append(byte[] data);
    }
}