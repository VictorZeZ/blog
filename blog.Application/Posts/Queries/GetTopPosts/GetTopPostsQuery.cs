using blog.Domain.Common.Interfaces;
using blog.Domain.Common.Reports;
using blog.Domain.Posts.Common;
using blog.Domain.Users.Enums;
using MediatR;

namespace blog.Application.Posts.Queries.GetTopPosts
{
    public class GetTopPostsQuery : IRequest<IReadOnlyList<PostSummaryResponse>>, IRequireActorLevel
    {
        public Guid ActorId { get; init; }
        public DateOnly? From { get; init; }
        public DateOnly? To { get; init; }
        public int TopN { get; init; } = TopNRules.DefaultTopN;
        public Guid? CategoryId { get; init; }

        public UserLevel MinimumLevel => UserLevel.Admin;
    }
}