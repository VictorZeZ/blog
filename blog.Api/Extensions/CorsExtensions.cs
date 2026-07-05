namespace blog.Api.Extensions
{
    public static class CorsExtensions
    {
        private const string PolicyName = "DefaultCorsPolicy";

        public static IServiceCollection AddApiCors(this IServiceCollection services, IConfiguration configuration)
        {
            var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

            services.AddCors(options =>
            {
                options.AddPolicy(PolicyName, policy =>
                {
                    if (allowedOrigins.Length > 0)
                    {
                        policy
                            .WithOrigins(allowedOrigins)
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                            .AllowCredentials();
                    }
                });
            });

            return services;
        }

        public static IApplicationBuilder UseApiCors(this IApplicationBuilder app)
            => app.UseCors(PolicyName);
    }
}
