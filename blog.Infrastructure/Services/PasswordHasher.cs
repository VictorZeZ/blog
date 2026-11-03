using blog.Domain.Common.Enum;
using blog.Domain.Common.Interfaces;
using blog.Domain.Common.Settings;
using blog.Domain.Exceptions;
using Isopoh.Cryptography.Argon2;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace blog.Infrastructure.Services
{
    public class PasswordHasher(IOptions<PasswordHashingSettings> settings) : IPasswordHasher
    {
        private readonly PasswordHashingSettings _settings = settings.Value;

        public string Hash(string password)
        {
            var config = new Argon2Config
            {
                Type = ResolveType(_settings.Variant),
                Version = Argon2Version.Nineteen,
                TimeCost = _settings.TimeCost,
                MemoryCost = _settings.MemoryCostKiB,
                Lanes = _settings.Parallelism,
                Threads = _settings.Parallelism,
                Password = Encoding.UTF8.GetBytes(password),
                Salt = RandomNumberGenerator.GetBytes(_settings.SaltLength),
                HashLength = _settings.HashLength
            };

            using var argon2 = new Argon2(config);
            using var hash = argon2.Hash();

            return config.EncodeString(hash.Buffer);
        }

        public bool Verify(string password, string hash)
            => Argon2.Verify(hash, password);

        private static Argon2Type ResolveType(Argon2Variant variant) => variant switch
        {
            Argon2Variant.Argon2i => Argon2Type.DataIndependentAddressing,
            Argon2Variant.Argon2id => Argon2Type.HybridAddressing,
            _ => throw new UnsupportedOperationException($"Argon2Variant.{variant}")
        };
    }
}