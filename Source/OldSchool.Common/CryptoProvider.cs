using System.Security.Cryptography;
using System.Text;

namespace OldSchool.Common
{
    public interface ICryptoProvider
    {
        string Hash(string value, byte[] seed);
    }

    public class CryptoProvider : ICryptoProvider
    {
        public string Hash(string value, byte[] seed)
        {
            var data = Encoding.UTF8.GetBytes(value);

            using (var provider = new HMACSHA256(seed))
            {
                var bytes = provider.ComputeHash(data);
                var sb = new StringBuilder();
                foreach (var t in bytes)
                    sb.Append(t.ToString("X2"));

                return sb.ToString();
            }
        }
    }
}