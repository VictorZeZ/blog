using blog.Domain.Categories.Entities;
using blog.Domain.Categories.Types;
using blog.Domain.Common;
using blog.Domain.Common.Enum;

namespace blog.Domain.Categories.Repository
{
    public interface ICategoryRepository
    {
        // Read
        Task<Category?> GetByIdAsync(CategoryId id, CancellationToken ct = default);
        Task<Category?> GetBySlugAsync(string slug, CancellationToken ct = default);
        Task<IEnumerable<Category>> GetAllActiveAsync(CancellationToken ct = default);
        Task<IEnumerable<Category>> GetAllDeletedAsync(CancellationToken ct = default);
        Task<PagedResult<Category>> GetPagedAsync(PagedRequest paging, DateOnly from, DateOnly to, TriStateFilter deleted, CancellationToken ct = default);
        Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default);
        Task<int> CountAsync(TriStateFilter deleted, CancellationToken ct = default);

        // Write
        Task AddAsync(Category category, CancellationToken ct = default);
        void Update(Category category);
        void SoftDelete(Category category);
        void Restore(Category category);
    }
}