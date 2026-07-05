using blog.Api.Common;
using blog.Domain.Exceptions;
using System.Threading.RateLimiting;

namespace blog.Api.Extensions
{
    public static class RateLimitingExtensions
    {
        public static IServiceCollection AddApiRateLimiting(this IServiceCollection services)
        {
            services.AddRateLimiter(options =>
            {
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                    RateLimitPartition.GetSlidingWindowLimiter(
                        partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        factory: _ => new SlidingWindowRateLimiterOptions
                        {
                            PermitLimit = 100,
                            Window = TimeSpan.FromMinutes(1),
                            SegmentsPerWindow = 4,
                            QueueLimit = 0
                        }));

                options.AddPolicy(RateLimitPolicies.Auth, context =>
                    RateLimitPartition.GetSlidingWindowLimiter(
                        partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        factory: _ => new SlidingWindowRateLimiterOptions
                        {
                            PermitLimit = 5,
                            Window = TimeSpan.FromMinutes(1),
                            SegmentsPerWindow = 4,
                            QueueLimit = 0
                        }));

                options.OnRejected = async (context, cancellationToken) =>
                {
                    var retryAfterSeconds = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
                        ? (int)retryAfter.TotalSeconds
                        : 60;

                    context.HttpContext.Response.Headers.RetryAfter = retryAfterSeconds.ToString();

                    var exception = new RateLimitException(context.HttpContext.Request.Path.Value ?? "unknown", retryAfterSeconds);

                    await DomainExceptionResponseWriter.WriteAsync(context.HttpContext, exception);
                };
            });

            return services;
        }
    }
}
