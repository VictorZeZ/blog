namespace blog.Api.Extensions
{
    public static class OpenApiExtensions
    {
        public static IServiceCollection AddOpenApiDocumentation(this IServiceCollection services)
        {
            services.AddOpenApi();

            return services;
        }
    }
}
