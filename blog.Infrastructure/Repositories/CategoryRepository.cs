using blog.Domain.Categories.Entities;
using blog.Domain.Categories.Repository;
using blog.Domain.Categories.Types;
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

        public async Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default)
            => await context.Categories.AnyAsync(x => x.Name == name, ct);

        public async Task AddAsync(Category category, CancellationToken ct = default)
            => await context.Categories.AddAsync(category, ct);

        public void Update(Category category)
            => context.Categories.Update(category);

        public void SoftDelete(Category category)
        {
            category.SoftDelete();
            context.Categories.Update(category);
        }
    }
}
