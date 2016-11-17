using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using OldSchool.Extensibility;

namespace OldSchool.Ifx.Session
{
    public class SessionResponse : ISessionResponse
    {
        public SessionResponse(ISessionContext context, IDictionary<string, object> environment)
        {
            Environment = environment;
            Context = context;
            Body = new MemoryStream();
        }

        public IDictionary<string, object> Environment { get; }
        public ISessionContext Context { get; }

        public Stream Body
        {
            get { return Get<Stream>(SessionConstants.ResponseBody); }
            set { Set(SessionConstants.ResponseBody, value); }
        }

        public T Get<T>(string key)
        {
            object value;
            return Environment.TryGetValue(key, out value) ? (T)value : default(T);
        }

        public ISessionResponse Set<T>(string key, T value)
        {
            Environment[key] = value;
            return this;
        }

        public async Task Append(byte[] data)
        {
            await Body.WriteAsync(data, 0, data.Length);
        }

        public async Task Append(string stringData)
        {
            var data = Encoding.ASCII.GetBytes(stringData);
            await Append(data);
        }
    }
}