using blog.Domain.Common.Interfaces;
using blog.Domain.Exceptions;
using blog.Domain.Tokens.Repository;
using blog.Domain.Users.Types;
using MediatR;

namespace blog.Application.Users.Commands.RevokeSession
{
    public class RevokeSessionCommandHandler(IRefreshTokenRepository refreshTokenRepository, IUnitOfWork unitOfWork) : IRequestHandler<RevokeSessionCommand, RevokeSessionResponse>
    {
        public async Task<RevokeSessionResponse> Handle(RevokeSessionCommand request, CancellationToken cancellationToken)
        {
            var activeTokens = await refreshTokenRepository.GetActiveByUserIdAsync(new UserId(request.ActorId), cancellationToken);

            var sessionTokens = activeTokens
                .Where(t => t.SessionId.Value == request.SessionId)
                .ToList();

            if (sessionTokens.Count == 0)
                throw new NotFoundException("Session", request.SessionId);

            if (request.SessionId == request.CurrentSessionId)
                throw new ForbiddenException("revoke_current_session");

            foreach (var token in sessionTokens)
            {
                token.Revoke();
                refreshTokenRepository.Update(token);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new RevokeSessionResponse { Success = true };
        }
    }
}