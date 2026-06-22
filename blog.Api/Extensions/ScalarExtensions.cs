using Scalar.AspNetCore;

namespace blog.Api.Extensions
{
    public static class ScalarExtensions
    {
        public static WebApplication UseApiDocumentation(this WebApplication app)
        {
            app.MapOpenApi();

            app.MapScalarApiReference(options =>
            {
                options
                    .WithTitle("blog API")
                    .WithTheme(ScalarTheme.DeepSpace)
                    .WithDefaultHttpClient(ScalarTarget.JavaScript, ScalarClient.Fetch);
            });

            return app;
        }
    }
}
