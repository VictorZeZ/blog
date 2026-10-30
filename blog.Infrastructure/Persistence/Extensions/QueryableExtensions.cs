using blog.Domain.Common;
using blog.Domain.Posts.Common;
using blog.Domain.Posts.Entities;
using blog.Domain.Posts.Enums;
using Microsoft.EntityFrameworkCore;

namespace blog.Infrastructure.Persistence.Extensions
{
    public static class QueryableExtensions
    {
        public static IQueryable<T> ApplyPaging<T>(this IQueryable<T> query, PagedRequest paging)
        {
            return query
                .Skip((paging.Page - 1) * paging.PageSize)
                .Take(paging.PageSize);
        }

        public static async Task<PagedResult<T>> ToPagedResultAsync<T>(this IQueryable<T> query, PagedRequest paging, CancellationToken ct = default)
        {
            var totalCount = await query.CountAsync(ct);
            var items = await query.ApplyPaging(paging).ToListAsync(ct);

            return new PagedResult<T>(items, totalCount, paging.Page, paging.PageSize);
        }

        public static async Task<PagedResult<PostSummaryResponse>> ToSummaryPagedResultAsync(this IQueryable<Post> query, PagedRequest paging, PostSortBy sortBy, CancellationToken ct = default)
        {
            var totalCount = await query.CountAsync(ct);
            var items = await query
                .ApplySorting(sortBy)
                .ApplyPaging(paging)
                .SelectSummary()
                .ToListAsync(ct);

            return new PagedResult<PostSummaryResponse>(items, totalCount, paging.Page, paging.PageSize);
        }

        public static IQueryable<PostSummaryResponse> SelectSummary(this IQueryable<Post> query)
            => query.Select(x => new PostSummaryResponse
            {
                Id = x.Id.Value,
                Title = x.Title,
                Summary = x.Summary,
                TitleImageUrl = x.TitleImageUrl,
                Slug = x.Slug,
                Tags = x.Tags,
                Status = x.Status,
                ViewCount = x.ViewCount,
                AuthorId = x.AuthorId.Value,
                AuthorFullName = EF.Property<string>(x.Author, "FullNameSearch"),
                CategoryName = x.Category.Name,
                CreatedAt = x.CreatedAt
            });

        public static IQueryable<Post> ApplySorting(this IQueryable<Post> query, PostSortBy sortBy) => sortBy switch
        {
            PostSortBy.Oldest => query.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id),
            PostSortBy.MostViewed => query.OrderByDescending(x => x.ViewCount).ThenByDescending(x => x.Id),
            _ => query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id)
        };
    }
}