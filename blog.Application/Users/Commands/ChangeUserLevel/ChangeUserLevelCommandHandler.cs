using blog.Domain.Common.Interfaces;
using blog.Domain.Exceptions;
using blog.Domain.Users.Enums;
using blog.Domain.Users.Extensions;
using blog.Domain.Users.Repository;
using blog.Domain.Users.Types;
using MediatR;

namespace blog.Application.Users.Commands.ChangeUserLevel
{
    public class ChangeUserLevelCommandHandler(IUserRepository userRepository, IUnitOfWork unitOfWork) : IRequestHandler<ChangeUserLevelCommand, ChangeUserLevelResponse>
    {
        public async Task<ChangeUserLevelResponse> Handle(ChangeUserLevelCommand request, CancellationToken cancellationToken)
        {
            var actor = await userRepository.GetByIdAsync(new UserId(request.ActorId), cancellationToken);
            if (actor is null)
                throw new NotFoundException("User", request.ActorId);

            actor.EnsureActive();

            if (actor.Level == UserLevel.Normal || actor.Level == UserLevel.Author)
                throw new ForbiddenException("change_user_level");

            var target = await userRepository.GetByIdAsync(new UserId(request.TargetUserId), cancellationToken);
            if (target is null)
                throw new NotFoundException("User", request.TargetUserId);

            target.EnsureActive();

            if (target.Level == UserLevel.Owner)
                throw new ForbiddenException("change_owner_level");

            if (actor.Level == UserLevel.Admin && target.Level == UserLevel.Admin)
                throw new ForbiddenException("change_admin_level");

            if (target.Level == request.Level)
                throw new InvalidStateException("User", target.Level.ToString(), "Different level");

            target.Promote(request.Level);

            userRepository.Update(target);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new ChangeUserLevelResponse
            {
                Id = target.Id.Value,
                FullName = target.FullName,
                Level = target.Level
            };
        }
    }
}
