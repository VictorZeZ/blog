using blog.Domain.Exceptions;
using System.Text.Json;

namespace blog.Api.Common
{
    public static class DomainExceptionResponseWriter
    {
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public static async Task WriteAsync(HttpContext context, DomainException exception)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = exception.StatusCode;

            var response = new
            {
                exception.StatusCode,
                exception.ErrorCode,
                exception.Title,
                exception.Details
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response, SerializerOptions));
        }
    }
}
