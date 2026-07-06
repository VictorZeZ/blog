using blog.Domain.Common.Interfaces;
using blog.Domain.Exceptions;
using blog.Domain.Tokens.Repository;
using MediatR;

namespace blog.Application.Users.Commands.Logout
{
    public class LogoutCommandHandler(IRefreshTokenRepository refreshTokenRepository, ITokenHasher tokenHasher, IUnitOfWork unitOfWork) : IRequestHandler<LogoutCommand, LogoutResponse>
    {
        public async Task<LogoutResponse> Handle(LogoutCommand request, CancellationToken cancellationToken)
        {
            var tokenHash = tokenHasher.Hash(request.RefreshToken);

            var token = await refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);
            if (token is null)
                throw new NotFoundException("RefreshToken", tokenHash);

            if (!token.IsValid())
                throw new ExpiredException("RefreshToken");

            token.Revoke();

            refreshTokenRepository.Update(token);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new LogoutResponse { Success = true };
        }
    }
}
