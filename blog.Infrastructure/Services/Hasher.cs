using blog.Domain.Common.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace blog.Infrastructure.Services
{
    public class Hasher : IHasher
    {
        public string Hash(string token)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToHexString(bytes);
        }
    }
}
