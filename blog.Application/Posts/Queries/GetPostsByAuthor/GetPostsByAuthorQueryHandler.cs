using blog.Domain.Common;
using blog.Domain.Posts.Common;
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
            var authorId = new UserId(request.AuthorId);
            var (publishedOnly, includeDrafts) = await ResolveVisibilityAsync(request.ActorId, authorId, cancellationToken);

            return await postRepository.GetByAuthorAsync(
                request.Paging,
                authorId,
                request.SortBy,
                publishedOnly,
                includeDrafts,
                cancellationToken);
        }

        private async Task<(bool PublishedOnly, bool IncludeDrafts)> ResolveVisibilityAsync(Guid? actorId, UserId authorId, CancellationToken ct)
        {
            if (actorId is null)
                return (true, false);

            var actor = await userRepository.GetByIdAsync(new UserId(actorId.Value), ct);
            if (actor is null)
                return (true, false);

            var includeDrafts = actor.CanAccessDraftsOf(authorId);
            var publishedOnly = !(includeDrafts || actor.IsElevated());

            return (publishedOnly, includeDrafts);
        }
    }
}