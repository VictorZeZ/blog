namespace blog.Api.Extensions
{
    public static class OpenApiExtensions
    {
        public static IServiceCollection AddOpenApiDocumentation(this IServiceCollection services)
        {
            services.AddOpenApi(options =>
            {
                options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
                options.AddOperationTransformer<AuthorizationOperationTransformer>();
            });

            return services;
        }
    }
}