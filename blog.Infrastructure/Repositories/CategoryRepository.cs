using blog.Domain.Categories.Common;
using blog.Domain.Categories.Entities;
using blog.Domain.Categories.Repository;
using blog.Domain.Categories.Types;
using blog.Domain.Common.Enum;
using blog.Infrastructure.Persistence;
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

        public async Task<int> CountAsync(TriStateFilter deleted, CancellationToken ct = default)
        {
            var query = context.Categories.AsQueryable();

            query = deleted switch
            {
                TriStateFilter.Yes => query.Where(x => x.IsDeleted),
                TriStateFilter.No => query.Where(x => !x.IsDeleted),
                _ => query
            };

            return await query.CountAsync(ct);
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