using FluentValidation;
using MediatR;
using ValidationException = blog.Domain.Exceptions.ValidationException;

namespace blog.Application.Common
{
    public class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators) : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
    {
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            if (!validators.Any())
                return await next();

            var context = new ValidationContext<TRequest>(request);

            var errors = validators
                .Select(v => v.Validate(context))
                .SelectMany(r => r.Errors)
                .Where(e => e is not null)
                .Select(e => new { Field = e.PropertyName, Reason = e.ErrorMessage })
                .ToList();

            if (errors.Count != 0)
                throw new ValidationException(errors);

            return await next();
        }
    }
}
