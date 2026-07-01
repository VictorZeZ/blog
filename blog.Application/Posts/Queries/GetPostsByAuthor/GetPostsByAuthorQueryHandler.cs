using blog.Domain.Common;
using blog.Domain.Posts.Common;
using blog.Domain.Posts.Extensions;
using blog.Domain.Posts.Repository;
using blog.Domain.Users.Extensions;
using blog.Domain.Users.Repository;
using blog.Domain.Users.Types;
using MediatR;

namespace blog.Application.Posts.Queries.GetPostsByAuthor
{
    public class GetPostsByAuthorQueryHandler(IPostRepository postRepository, IUserRepository userRepository) : IRequestHandler<GetPostsByAuthorQuery, PagedResult<PostSummaryResponse>>
    {
        public async Task<PagedResult<PostSummaryResponse>> Handle(GetPostsByAuthorQuery request, CancellationToken cancellationToken)
        {
            var publishedOnly = !await CanViewAllAsync(request.ActorId, new UserId(request.AuthorId), cancellationToken);

            var result = await postRepository.GetByAuthorAsync(
                request.Paging,
                new UserId(request.AuthorId),
                request.SortBy,
                publishedOnly,
                cancellationToken);

            return new PagedResult<PostSummaryResponse>(
                result.Items.Select(p => p.ToSummaryResponse()),
                result.TotalCount,
                result.Page,
                result.PageSize);
        }

        private async Task<bool> CanViewAllAsync(Guid? actorId, UserId authorId, CancellationToken ct)
        {
            if (actorId is null)
                return false;

            var actor = await userRepository.GetByIdAsync(new UserId(actorId.Value), ct);
            return actor is not null && actor.CanManagePost(authorId);
        }
    }
}
