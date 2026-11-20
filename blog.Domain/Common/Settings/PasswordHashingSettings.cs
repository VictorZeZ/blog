using blog.Domain.Common.Enum;
using System.ComponentModel.DataAnnotations;

namespace blog.Domain.Common.Settings
{
    public class PasswordHashingSettings
    {
        [EnumDataType(typeof(Argon2Variant))]
        public Argon2Variant Variant { get; init; }

        [Range(1, 20)]
        public int TimeCost { get; init; }

        [Range(19456, 1048576)]
        public int MemoryCostKiB { get; init; }

        [Range(1, 16)]
        public int Parallelism { get; init; }

        [Range(16, 64)]
        public int HashLength { get; init; }

        [Range(16, 64)]
        public int SaltLength { get; init; }
    }
}