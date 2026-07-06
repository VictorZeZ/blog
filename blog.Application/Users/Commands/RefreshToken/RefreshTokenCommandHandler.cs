using blog.Domain.Common.Interfaces;
using blog.Domain.Exceptions;
using blog.Domain.Tokens.Repository;
using blog.Domain.Users.Extensions;
using blog.Domain.Users.Repository;
using MediatR;

namespace blog.Application.Users.Commands.RefreshToken
{
    public class RefreshTokenCommandHandler(IRefreshTokenRepository refreshTokenRepository, IUserRepository userRepository, ITokenHasher tokenHasher, IJwtService jwtService, IUnitOfWork unitOfWork) : IRequestHandler<RefreshTokenCommand, RefreshTokenResponse>
    {
        public async Task<RefreshTokenResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            var incomingTokenHash = tokenHasher.Hash(request.RefreshToken);

            var token = await refreshTokenRepository.GetByTokenHashAsync(incomingTokenHash, cancellationToken);
            if (token is null)
                throw new NotFoundException("RefreshToken", incomingTokenHash);

            if (!token.IsValid())
                throw new ExpiredException("RefreshToken");

            var user = await userRepository.GetByIdAsync(token.UserId, cancellationToken);
            if (user is null)
                throw new NotFoundException("User", token.UserId);

            user.EnsureActive();

            var newRefreshToken = jwtService.GenerateRefreshToken();
            var newRefreshTokenHash = tokenHasher.Hash(newRefreshToken);
            var rotated = token.Rotate(newRefreshTokenHash, request.DeviceInfo);

            var accessToken = jwtService.GenerateAccessToken(user);

            refreshTokenRepository.Update(token);
            await refreshTokenRepository.AddAsync(rotated, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new RefreshTokenResponse
            {
                AccessToken = accessToken,
                RefreshToken = newRefreshToken
            };
        }
    }
}
