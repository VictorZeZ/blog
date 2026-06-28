using blog.Domain.Common.Interfaces;
using blog.Domain.Exceptions;
using blog.Domain.Tokens.Repository;
using blog.Domain.Users.Extensions;
using blog.Domain.Users.Repository;
using blog.Domain.Users.Types;
using MediatR;

namespace blog.Application.Users.Commands.DeleteAccount
{
    public class DeleteAccountCommandHandler(IUserRepository userRepository, IRefreshTokenRepository refreshTokenRepository, IUnitOfWork unitOfWork) : IRequestHandler<DeleteAccountCommand, DeleteAccountResponse>
    {
        public async Task<DeleteAccountResponse> Handle(DeleteAccountCommand request, CancellationToken cancellationToken)
        {
            var userId = new UserId(request.UserId);

            var user = await userRepository.GetByIdAsync(userId, cancellationToken);
            if (user is null)
                throw new NotFoundException("User", request.UserId);

            user.EnsureActive();

            var activeTokens = await refreshTokenRepository.GetActiveByUserIdAsync(userId, cancellationToken);

            foreach (var token in activeTokens)
            {
                token.Revoke();
                refreshTokenRepository.Update(token);
            }

            userRepository.SoftDelete(user);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new DeleteAccountResponse { Success = true };
        }
    }
}
