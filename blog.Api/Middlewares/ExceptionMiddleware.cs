using blog.Api.Common;
using blog.Domain.Exceptions;

namespace blog.Api.Middlewares
{
    public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IWebHostEnvironment env)
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
                await DomainExceptionResponseWriter.WriteAsync(context, ex);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected exception occurred");

                var unknownException = env.IsDevelopment()
                    ? new UnknownException(ex.Message)
                    : new UnknownException();

                await DomainExceptionResponseWriter.WriteAsync(context, unknownException);
            }
        }
    }
}
