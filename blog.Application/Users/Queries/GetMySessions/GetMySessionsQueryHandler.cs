using blog.Domain.Tokens.Repository;
using blog.Domain.Users.Types;
using MediatR;

namespace blog.Application.Users.Queries.GetMySessions
{
    public class GetMySessionsQueryHandler(IRefreshTokenRepository refreshTokenRepository) : IRequestHandler<GetMySessionsQuery, IReadOnlyList<GetMySessionsResponse>>
    {
        public async Task<IReadOnlyList<GetMySessionsResponse>> Handle(GetMySessionsQuery request, CancellationToken cancellationToken)
        {
            var tokens = await refreshTokenRepository.GetActiveByUserIdAsync(new UserId(request.ActorId), cancellationToken);

            return tokens
                .GroupBy(t => t.SessionId)
                .Select(g => g.OrderByDescending(t => t.CreatedAt).First())
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new GetMySessionsResponse
                {
                    Id = t.SessionId.Value,
                    DeviceInfo = t.DeviceInfo,
                    IsCurrent = t.SessionId.Value == request.CurrentSessionId,
                    CreatedAt = t.CreatedAt,
                    ExpiresAt = t.ExpiresAt
                })
                .ToList();
        }
    }
}