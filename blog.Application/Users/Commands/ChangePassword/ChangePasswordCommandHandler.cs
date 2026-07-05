using blog.Domain.Common.Interfaces;
using blog.Domain.Exceptions;
using blog.Domain.Tokens.Repository;
using blog.Domain.Users.Extensions;
using blog.Domain.Users.Repository;
using blog.Domain.Users.Types;
using MediatR;

namespace blog.Application.Users.Commands.ChangePassword
{
    public class ChangePasswordCommandHandler(IUserRepository userRepository, IRefreshTokenRepository refreshTokenRepository, IPasswordHasher passwordHasher, IUnitOfWork unitOfWork) : IRequestHandler<ChangePasswordCommand, ChangePasswordResponse>
    {
        public async Task<ChangePasswordResponse> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
        {
            var userId = new UserId(request.UserId);

            var user = await userRepository.GetByIdAsync(new UserId(request.UserId), cancellationToken);
            if (user is null)
                throw new NotFoundException("User", request.UserId);

            user.EnsureActive();

            var isCurrentPasswordValid = passwordHasher.Verify(request.CurrentPassword, user.PasswordHash);
            if (!isCurrentPasswordValid)
                throw new ValidationException("CurrentPassword", "Current password is incorrect");

            var newPasswordHash = passwordHasher.Hash(request.NewPassword);
            user.ChangePassword(newPasswordHash);
            userRepository.Update(user);

            var activeTokens = await refreshTokenRepository.GetActiveByUserIdAsync(userId, cancellationToken);
            foreach (var token in activeTokens)
            {
                token.Revoke();
                refreshTokenRepository.Update(token);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new ChangePasswordResponse { Success = true };
        }
    }
}
