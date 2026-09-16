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
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new GetMySessionsResponse
                {
                    Id = t.Id.Value,
                    DeviceInfo = t.DeviceInfo,
                    CreatedAt = t.CreatedAt,
                    ExpiresAt = t.ExpiresAt
                })
                .ToList();
        }
    }
}