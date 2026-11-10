using blog.Api.Common;
using blog.Domain.Exceptions;

namespace blog.Api.Middlewares
{
    public class ExceptionMiddleware(RequestDelegate next, IWebHostEnvironment env)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await next(context);
            }
            catch (DomainException ex)
            {
                context.Items[HttpContextItemKeys.HandledException] = ex;
                await DomainExceptionResponseWriter.WriteAsync(context, ex);
            }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
            {
                context.Response.StatusCode = CustomStatusCodes.ClientClosedRequest;
            }
            catch (Exception ex)
            {
                context.Items[HttpContextItemKeys.HandledException] = ex;

                var unknownException = env.IsDevelopment()
                    ? new UnknownException(ex.Message)
                    : new UnknownException();

                await DomainExceptionResponseWriter.WriteAsync(context, unknownException);
            }
        }
    }
}