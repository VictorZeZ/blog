using blog.Domain.Common.Interfaces;
using blog.Domain.Exceptions;
using blog.Domain.Users.Enums;
using blog.Domain.Users.Extensions;
using blog.Domain.Users.Repository;
using blog.Domain.Users.Types;
using MediatR;

namespace blog.Application.Users.Commands.BanUser
{
    public class BanUserCommandHandler(IUserRepository userRepository, IUnitOfWork unitOfWork) : IRequestHandler<BanUserCommand, BanUserResponse>
    {
        public async Task<BanUserResponse> Handle(BanUserCommand request, CancellationToken cancellationToken)
        {
            var actor = await userRepository.GetByIdAsync(new UserId(request.ActorId), cancellationToken);
            if (actor is null)
                throw new NotFoundException("User", request.ActorId);

            actor.EnsureActive();

            if (!actor.IsElevated())
                throw new ForbiddenException("ban_user");

            var target = await userRepository.GetByIdAsync(new UserId(request.TargetUserId), cancellationToken);
            if (target is null)
                throw new NotFoundException("User", request.TargetUserId);

            if (target.IsDeleted)
                throw new InvalidStateException("User", "Deleted", "Active");

            if (target.Level == UserLevel.Owner)
                throw new ForbiddenException("ban_owner");

            if (actor.Level == UserLevel.Admin && target.Level == UserLevel.Admin)
                throw new ForbiddenException("ban_admin");

            if (target.IsBanned == request.IsBanned)
                throw new InvalidStateException(
                    "User",
                    request.IsBanned ? "Already banned" : "Already active",
                    request.IsBanned ? "Active" : "Banned");

            if (request.IsBanned)
                target.Ban();
            else
                target.Unban();

            userRepository.Update(target);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new BanUserResponse
            {
                Id = target.Id.Value,
                FullName = target.FullName,
                IsBanned = target.IsBanned
            };
        }
    }
}
