using blog.Domain.Common.Interfaces;
using blog.Domain.Exceptions;
using blog.Domain.Tokens.Repository;
using blog.Domain.Tokens.Types;
using MediatR;

namespace blog.Application.Users.Commands.RevokeSession
{
    public class RevokeSessionCommandHandler(IRefreshTokenRepository refreshTokenRepository, IUnitOfWork unitOfWork) : IRequestHandler<RevokeSessionCommand, RevokeSessionResponse>
    {
        public async Task<RevokeSessionResponse> Handle(RevokeSessionCommand request, CancellationToken cancellationToken)
        {
            var token = await refreshTokenRepository.GetByIdAsync(new RefreshTokenId(request.SessionId), cancellationToken);
            if (token is null)
                throw new NotFoundException("RefreshToken", request.SessionId);

            if (token.UserId.Value != request.ActorId)
                throw new ForbiddenException("revoke_session");

            if (!token.IsValid())
                throw new ExpiredException("RefreshToken");

            token.Revoke();

            refreshTokenRepository.Update(token);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new RevokeSessionResponse { Success = true };
        }
    }
}