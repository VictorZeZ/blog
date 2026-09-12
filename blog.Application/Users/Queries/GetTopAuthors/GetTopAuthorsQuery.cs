using blog.Domain.Common.Interfaces;
using blog.Domain.Common.Reports;
using blog.Domain.Users.Common;
using blog.Domain.Users.Enums;
using MediatR;

namespace blog.Application.Users.Queries.GetTopAuthors
{
    public class GetTopAuthorsQuery : IRequest<IReadOnlyList<TopAuthorResult>>, IRequireActorLevel
    {
        public Guid ActorId { get; init; }
        public DateOnly? From { get; init; }
        public DateOnly? To { get; init; }
        public int TopN { get; init; } = TopNRules.DefaultTopN;

        public UserLevel MinimumLevel => UserLevel.Admin;
    }
}