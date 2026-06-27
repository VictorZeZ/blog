using blog.Domain.Exceptions;
using System.Text.Json;

namespace blog.Api.Middlewares
{
    public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await next(context);
            }
            catch (DomainException ex)
            {
                logger.LogWarning(ex, "Domain exception occurred: {ErrorCode}", ex.ErrorCode);
                await HandleDomainExceptionAsync(context, ex);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected exception occurred");
                await HandleUnknownExceptionAsync(context);
            }
        }

        private static async Task HandleDomainExceptionAsync(HttpContext context, DomainException ex)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = ex.StatusCode;

            var response = new
            {
                ex.StatusCode,
                ex.ErrorCode,
                ex.Title,
                ex.Details
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }));
        }

        private static async Task HandleUnknownExceptionAsync(HttpContext context)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = 500;

            var response = new
            {
                StatusCode = 500,
                ErrorCode = "UNKNOWN_ERROR",
                Title = "An unexpected error occurred",
                Details = (object?)null
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }));
        }
    }
}
