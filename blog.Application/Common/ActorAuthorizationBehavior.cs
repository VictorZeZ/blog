using blog.Domain.Common.Interfaces;
using blog.Domain.Exceptions;
using blog.Domain.Users.Extensions;
using blog.Domain.Users.Repository;
using blog.Domain.Users.Types;
using MediatR;

namespace blog.Application.Common
{
    public class ActorAuthorizationBehavior<TRequest, TResponse>(IUserRepository userRepository) : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
    {
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            if (request is IRequireActorLevel actorRequest)
            {
                var actor = await userRepository.GetByIdAsync(new UserId(actorRequest.ActorId), cancellationToken);
                if (actor is null)
                    throw new NotFoundException("User", actorRequest.ActorId);

                actor.EnsureActive();

                if (actor.Level < actorRequest.MinimumLevel)
                    throw new ForbiddenException(typeof(TRequest).Name);
            }

            return await next();
        }
    }
}
