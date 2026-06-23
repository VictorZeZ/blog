using blog.Domain.Common;
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

        public static IQueryable<Post> ApplySorting(this IQueryable<Post> query, PostSortBy sortBy) => sortBy switch
        {
            PostSortBy.Oldest => query.OrderBy(x => x.CreatedAt),
            PostSortBy.MostViewed => query.OrderByDescending(x => x.ViewCount),
            _ => query.OrderByDescending(x => x.CreatedAt)
        };
    }
}
