using blog.Domain.Common;
using blog.Domain.Common.Interfaces;
using System.Security.Cryptography;

namespace blog.Infrastructure.Services
{
    public class VerificationCodeGenerator : IVerificationCodeGenerator
    {
        private const string Alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
        private const int CodeLength = 6;
        private const int GroupSize = 3;

        public VerificationCode Generate()
        {
            var chars = new char[CodeLength];

            for (var i = 0; i < CodeLength; i++)
                chars[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];

            var code = new string(chars);
            var displayCode = $"{code[..GroupSize]} - {code[GroupSize..]}";

            return new VerificationCode(code, displayCode);
        }
    }
}
