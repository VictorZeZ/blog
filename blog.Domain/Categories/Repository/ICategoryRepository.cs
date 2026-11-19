using blog.Domain.Categories.Common;
using blog.Domain.Categories.Entities;
using blog.Domain.Categories.Enums;
using blog.Domain.Categories.Types;
using blog.Domain.Common;
using blog.Domain.Common.Enum;
using blog.Domain.Posts.Common;
using blog.Domain.Posts.Enums;

namespace blog.Domain.Categories.Repository
{
    public interface ICategoryRepository
    {
        // Read
        Task<Category?> GetByIdAsync(CategoryId id, CancellationToken ct = default);
        Task<Category?> GetBySlugAsync(string slug, CancellationToken ct = default);
        Task<IEnumerable<Category>> GetAllActiveAsync(CancellationToken ct = default);
        Task<IEnumerable<Category>> GetAllDeletedAsync(CancellationToken ct = default);
        Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default);
        Task<CategoryOverviewCounts> GetOverviewCountsAsync(CancellationToken ct = default);
        Task<IReadOnlyList<CategoryDailyCount>> GetDailyActivityReportAsync(DateOnly from, DateOnly to, CancellationToken ct = default);
        Task<PagedResult<CategoryWithPostCount>> GetAllWithPostCountAsync(PagedRequest paging, string? term, CategorySortBy sortBy, TriStateFilter deleted, IReadOnlyCollection<PostStatus> postStatuses, CancellationToken ct = default);
        // Write
        Task AddAsync(Category category, CancellationToken ct = default);
        void Update(Category category);
        void SoftDelete(Category category);
        void Restore(Category category);
    }
}