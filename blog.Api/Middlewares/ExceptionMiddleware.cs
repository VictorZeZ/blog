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
                await WriteDomainExceptionResponseAsync(context, ex);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected exception occurred");
                await WriteDomainExceptionResponseAsync(context, new UnknownException(ex.Message));
            }
        }

        private static async Task WriteDomainExceptionResponseAsync(HttpContext context, DomainException ex)
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
    }
}
