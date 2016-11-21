using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using OldSchool.Extensibility;

namespace OldSchool.Ifx.Session
{
    public class SessionRequest : ISessionRequest
    {
        public SessionRequest(ISessionContext context, IDictionary<string, object> environment)
        {
            Environment = environment;
            Context = context;
            Body = new MemoryStream();
        }

        public IDictionary<string, object> Environment { get; }
        public ISessionContext Context { get; }

        public Stream Body
        {
            get { return Get<Stream>(SessionConstants.RequestBody); }
            set { Set(SessionConstants.RequestBody, value); }
        }

        public T Get<T>(string key)
        {
            object value;
            return Environment.TryGetValue(key, out value) ? (T)value : default(T);
        }

        public ISessionRequest Set<T>(string key, T value)
        {
            Environment[key] = value;
            return this;
        }

        public void Append(byte[] data)
        {
            Body.Write(data, 0, data.Length);
        }

        public void Clear()
        {
            Body.Dispose();
            Body = new MemoryStream();
        }

        public async Task<byte[]> GetBytes()
        {
            if (Body.CanSeek)
                Body.Seek(0, SeekOrigin.Begin);

            using (var ms = new MemoryStream())
            {
                await Body.CopyToAsync(ms);
                return ms.ToArray();
            }
        }

        public void Append(string stringData)
        {
            var data = Encoding.ASCII.GetBytes(stringData);
            Append(data);
        }
    }
}