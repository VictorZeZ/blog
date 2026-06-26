using blog.Domain.Common.Interfaces;
using blog.Domain.Exceptions;
using blog.Domain.Users.Repository;
using blog.Domain.Users.Types;
using MediatR;

namespace blog.Application.Users.Commands.ChangePassword
{
    public class ChangePasswordCommandHandler(IUserRepository userRepository, IPasswordHasher passwordHasher, IUnitOfWork unitOfWork) : IRequestHandler<ChangePasswordCommand, ChangePasswordResponse>
    {
        public async Task<ChangePasswordResponse> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
        {
            var user = await userRepository.GetByIdAsync(new UserId(request.UserId), cancellationToken);
            if (user is null)
                throw new NotFoundException("User", request.UserId);

            if (user.IsDeleted)
                throw new InvalidStateException("User", "Deleted", "Active");

            var isCurrentPasswordValid = passwordHasher.Verify(request.CurrentPassword, user.PasswordHash);
            if (!isCurrentPasswordValid)
                throw new ValidationException("CurrentPassword", "Current password is incorrect");

            var newPasswordHash = passwordHasher.Hash(request.NewPassword);

            user.ChangePassword(newPasswordHash);

            userRepository.Update(user);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new ChangePasswordResponse { Success = true };
        }
    }
}
