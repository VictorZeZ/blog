using blog.Domain.Categories.Repository;
using blog.Domain.Categories.Types;
using blog.Domain.Common;
using blog.Domain.Common.Reports;
using blog.Domain.Exceptions;
using blog.Domain.Posts.Common;
using blog.Domain.Posts.Extensions;
using blog.Domain.Posts.Repository;
using blog.Domain.Users.Entities;
using blog.Domain.Users.Extensions;
using blog.Domain.Users.Repository;
using blog.Domain.Users.Types;
using MediatR;

namespace blog.Application.Posts.Queries.GetPostsDetail
{
    public class GetPostsDetailQueryHandler(IPostRepository postRepository, ICategoryRepository categoryRepository, IUserRepository userRepository) : IRequestHandler<GetPostsDetailQuery, PagedResult<PostSummaryResponse>>
    {
        public async Task<PagedResult<PostSummaryResponse>> Handle(GetPostsDetailQuery request, CancellationToken cancellationToken)
        {
            var actor = await userRepository.GetByIdAsync(new UserId(request.ActorId), cancellationToken);
            if (actor is null)
                throw new NotFoundException("User", request.ActorId);

            actor.EnsureActive();

            var authorId = await ResolveAuthorIdAsync(actor, request.AuthorId, cancellationToken);
            var canViewDraftDetails = actor.IsOwner() || (authorId is not null && authorId == actor.Id);

            var range = ReportDateRangeRules.Resolve(request.From, request.To);

            CategoryId? categoryId = null;
            if (request.CategoryId is not null)
            {
                categoryId = new CategoryId(request.CategoryId.Value);
                var category = await categoryRepository.GetByIdAsync(categoryId.Value, cancellationToken);
                if (category is null)
                    throw new NotFoundException("Category", request.CategoryId.Value);
            }

            var result = await postRepository.GetReportAsync(request.Paging, range.From, range.To, request.Filter, request.SortBy, categoryId, authorId, canViewDraftDetails, cancellationToken);

            return new PagedResult<PostSummaryResponse>(
                result.Items.Select(p => p.ToSummaryResponse()),
                result.TotalCount,
                result.Page,
                result.PageSize);
        }

        private async Task<UserId?> ResolveAuthorIdAsync(User actor, Guid? requestedAuthorId, CancellationToken ct)
        {
            if (!actor.IsElevated())
            {
                if (requestedAuthorId is not null && requestedAuthorId != actor.Id.Value)
                    throw new ForbiddenException("view_other_authors_posts");

                return actor.Id;
            }

            if (requestedAuthorId is null)
                return null;

            var authorId = new UserId(requestedAuthorId.Value);
            var author = await userRepository.GetByIdAsync(authorId, ct);
            if (author is null)
                throw new NotFoundException("User", requestedAuthorId.Value);

            return authorId;
        }
    }
}