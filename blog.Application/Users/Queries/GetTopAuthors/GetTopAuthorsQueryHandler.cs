using blog.Domain.Common.Reports;
using blog.Domain.Posts.Repository;
using blog.Domain.Users.Common;
using MediatR;

namespace blog.Application.Users.Queries.GetTopAuthors
{
    public class GetTopAuthorsQueryHandler(IPostRepository postRepository) : IRequestHandler<GetTopAuthorsQuery, IReadOnlyList<TopAuthorResult>>
    {
        public async Task<IReadOnlyList<TopAuthorResult>> Handle(GetTopAuthorsQuery request, CancellationToken cancellationToken)
        {
            var range = ReportDateRangeRules.Resolve(request.From, request.To);

            return await postRepository.GetTopAuthorsAsync(range.From, range.To, request.TopN, cancellationToken);
        }
    }
}