using blog.Domain.Categories.Entities;
using blog.Domain.Categories.Types;

namespace blog.Domain.Categories.Repository
{
    public interface ICategoryRepository
    {
        // Read
        Task<Category?> GetByIdAsync(CategoryId id, CancellationToken ct = default);
        Task<Category?> GetBySlugAsync(string slug, CancellationToken ct = default);
        Task<IEnumerable<Category>> GetAllActiveAsync(CancellationToken ct = default);
        Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default);

        // Write
        Task AddAsync(Category category, CancellationToken ct = default);
        void Update(Category category);
        void SoftDelete(Category category);
    }
}
