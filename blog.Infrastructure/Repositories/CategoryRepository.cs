using blog.Domain.Categories.Common;
using blog.Domain.Categories.Entities;
using blog.Domain.Categories.Enums;
using blog.Domain.Categories.Repository;
using blog.Domain.Categories.Types;
using blog.Domain.Common;
using blog.Domain.Common.Enum;
using blog.Domain.Posts.Common;
using blog.Domain.Posts.Entities;
using blog.Domain.Posts.Enums;
using blog.Infrastructure.Persistence;
using blog.Infrastructure.Persistence.Extensions;
using Microsoft.EntityFrameworkCore;

namespace blog.Infrastructure.Repositories
{
    public class CategoryRepository(AppDbContext context) : ICategoryRepository
    {
        public async Task<Category?> GetByIdAsync(CategoryId id, CancellationToken ct = default)
            => await context.Categories.FirstOrDefaultAsync(x => x.Id == id, ct);

        public async Task<Category?> GetBySlugAsync(string slug, CancellationToken ct = default)
            => await context.Categories.FirstOrDefaultAsync(x => x.Slug == slug, ct);

        public async Task<IEnumerable<Category>> GetAllActiveAsync(CancellationToken ct = default)
            => await context.Categories
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.Name)
                .ToListAsync(ct);

        public async Task<IEnumerable<Category>> GetAllDeletedAsync(CancellationToken ct = default)
            => await context.Categories
                .Where(x => x.IsDeleted)
                .OrderByDescending(x => x.DeletedAt)
                .ToListAsync(ct);

        public async Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default)
            => await context.Categories.AnyAsync(x => x.Name == name, ct);

        public async Task<CategoryOverviewCounts> GetOverviewCountsAsync(CancellationToken ct = default)
        {
            var counts = await context.Categories
                .GroupBy(_ => 1)
                .Select(g => new CategoryOverviewCounts(
                    g.Count(x => !x.IsDeleted),
                    g.Count(x => x.IsDeleted)))
                .SingleOrDefaultAsync(ct);

            return counts ?? new CategoryOverviewCounts(0, 0);
        }

        public async Task<IReadOnlyList<CategoryDailyCount>> GetDailyActivityReportAsync(DateOnly from, DateOnly to, CancellationToken ct = default)
        {
            var fromUtc = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var toExclusiveUtc = to.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

            var created = await context.Categories
                .Where(x => x.CreatedAt >= fromUtc && x.CreatedAt < toExclusiveUtc)
                .GroupBy(x => x.CreatedAt.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync(ct);

            var deleted = await context.Categories
                .Where(x => x.DeletedAt >= fromUtc && x.DeletedAt < toExclusiveUtc)
                .GroupBy(x => x.DeletedAt!.Value.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync(ct);

            var createdByDate = created.ToDictionary(x => DateOnly.FromDateTime(x.Date), x => x.Count);
            var deletedByDate = deleted.ToDictionary(x => DateOnly.FromDateTime(x.Date), x => x.Count);

            var days = new List<CategoryDailyCount>();
            for (var date = from; date <= to; date = date.AddDays(1))
            {
                days.Add(new CategoryDailyCount(
                    date,
                    createdByDate.GetValueOrDefault(date),
                    deletedByDate.GetValueOrDefault(date)));
            }

            return days;
        }

        public async Task<PagedResult<CategoryWithPostCount>> GetAllWithPostCountAsync(PagedRequest paging, string? term, CategorySortBy sortBy, TriStateFilter deleted, IReadOnlyCollection<PostStatus> postStatuses, CancellationToken ct = default)
        {
            var categories = deleted switch
            {
                TriStateFilter.Yes => context.Categories.Where(x => x.IsDeleted),
                TriStateFilter.No => context.Categories.Where(x => !x.IsDeleted),
                _ => context.Categories.AsQueryable()
            };

            if (!string.IsNullOrWhiteSpace(term))
            {
                var pattern = LikePattern.Contains(term);
                categories = categories.Where(x => EF.Functions.ILike(x.Name, pattern));
            }

            IQueryable<Post> posts = context.Posts;
            if (postStatuses.Count > 0)
                posts = posts.Where(x => postStatuses.Contains(x.Status));

            var totalCount = await categories.CountAsync(ct);

            var counted = categories.Select(x => new
            {
                Category = x,
                PostsCount = posts.Count(p => p.CategoryId == x.Id)
            });

            var sorted = sortBy switch
            {
                CategorySortBy.Oldest => counted.OrderBy(x => x.Category.CreatedAt).ThenBy(x => x.Category.Id),
                CategorySortBy.NameAscending => counted.OrderBy(x => x.Category.Name).ThenBy(x => x.Category.Id),
                CategorySortBy.NameDescending => counted.OrderByDescending(x => x.Category.Name).ThenByDescending(x => x.Category.Id),
                CategorySortBy.MostPosts => counted.OrderByDescending(x => x.PostsCount).ThenByDescending(x => x.Category.Id),
                CategorySortBy.FewestPosts => counted.OrderBy(x => x.PostsCount).ThenBy(x => x.Category.Id),
                _ => counted.OrderByDescending(x => x.Category.CreatedAt).ThenByDescending(x => x.Category.Id)
            };

            var items = await sorted
                .ApplyPaging(paging)
                .Select(x => new CategoryWithPostCount(
                    x.Category.Id.Value,
                    x.Category.Name,
                    x.Category.Slug,
                    x.Category.IsDeleted,
                    x.Category.DeletedAt,
                    x.Category.CreatedAt,
                    x.PostsCount))
                .ToListAsync(ct);

            return new PagedResult<CategoryWithPostCount>(items, totalCount, paging.Page, paging.PageSize);
        }



        public async Task AddAsync(Category category, CancellationToken ct = default)
            => await context.Categories.AddAsync(category, ct);

        public void Update(Category category)
            => context.Categories.Update(category);

        public void SoftDelete(Category category)
        {
            category.SoftDelete();
            context.Categories.Update(category);
        }

        public void Restore(Category category)
        {
            category.Restore();
            context.Categories.Update(category);
        }
    }
}