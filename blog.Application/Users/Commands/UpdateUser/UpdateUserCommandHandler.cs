using blog.Domain.Common.Interfaces;
using blog.Domain.Exceptions;
using blog.Domain.Users.Extensions;
using blog.Domain.Users.Repository;
using blog.Domain.Users.Types;
using MediatR;

namespace blog.Application.Users.Commands.UpdateUser
{
    public class UpdateUserCommandHandler(IUserRepository userRepository, IUnitOfWork unitOfWork) : IRequestHandler<UpdateUserCommand, UpdateUserResponse>
    {
        public async Task<UpdateUserResponse> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
        {
            var user = await userRepository.GetByIdAsync(new UserId(request.UserId), cancellationToken);
            if (user is null)
                throw new NotFoundException("User", request.UserId);

            user.EnsureActive();

            user.UpdateProfile(request.FirstName, request.LastName);

            userRepository.Update(user);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new UpdateUserResponse
            {
                Id = user.Id.Value,
                FullName = user.FullName
            };
        }
    }
}
