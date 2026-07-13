using System.ComponentModel.DataAnnotations;

namespace blog.Domain.Common.Settings
{
    public class EmailSettings
    {
        [Required]
        public string SmtpHost { get; init; } = string.Empty;

        [Range(1, 65535)]
        public int SmtpPort { get; init; }

        [Required]
        public string SmtpUsername { get; init; } = string.Empty;

        [Required]
        public string SmtpPassword { get; init; } = string.Empty;

        [Required, EmailAddress]
        public string FromAddress { get; init; } = string.Empty;

        [Required]
        public string FromName { get; init; } = string.Empty;
    }
}
