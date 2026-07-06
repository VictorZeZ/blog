using blog.Domain.Posts.Common;
using Microsoft.AspNetCore.Http.Features;

namespace blog.Api.Extensions
{
    public static class KestrelExtensions
    {
        private const long MaxRequestBodySizeBytes = PostImageValidationRules.MaxTitleImageSizeBytes + 1024 * 1024;

        public static WebApplicationBuilder ConfigureRequestLimits(this WebApplicationBuilder builder)
        {
            builder.WebHost.ConfigureKestrel(options =>
                options.Limits.MaxRequestBodySize = MaxRequestBodySizeBytes);

            builder.Services.Configure<FormOptions>(options =>
                options.MultipartBodyLengthLimit = MaxRequestBodySizeBytes);

            return builder;
        }
    }
}
