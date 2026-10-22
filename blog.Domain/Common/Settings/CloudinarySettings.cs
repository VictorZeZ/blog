using System.ComponentModel.DataAnnotations;

namespace blog.Domain.Common.Settings
{
    public class CloudinarySettings
    {
        [Required]
        public string CloudName { get; init; } = string.Empty;

        [Required]
        public string ApiKey { get; init; } = string.Empty;

        [Required]
        public string ApiSecret { get; init; } = string.Empty;
    }
}