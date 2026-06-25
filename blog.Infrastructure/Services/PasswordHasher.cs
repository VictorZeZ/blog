using blog.Domain.Common.Interfaces;
using Isopoh.Cryptography.Argon2;

namespace blog.Infrastructure.Services
{
    public class PasswordHasher : IPasswordHasher
    {
        public string Hash(string password)
            => Argon2.Hash(password);

        public bool Verify(string password, string hash)
            => Argon2.Verify(hash, password);
    }
}
