using blog.Domain.Categories.Entities;
using blog.Domain.Categories.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace blog.Infrastructure.Persistence.Configurations
{
    public class CategoryConfiguration : IEntityTypeConfiguration<Category>
    {
        public void Configure(EntityTypeBuilder<Category> builder)
        {
            builder.ToTable("Categories");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasConversion(
                    v => v.Value,
                    v => new CategoryId(v))
                .HasColumnType("uuid");

            builder.Property(x => x.Name)
                .IsRequired();

            builder.Property(x => x.Slug)
                .IsRequired();

            builder.HasIndex(x => x.Slug)
                .IsUnique();

            builder.Property(x => x.IsDeleted)
                .HasColumnType("boolean")
                .HasDefaultValue(false)
                .IsRequired();

            builder.Property(x => x.CreatedAt)
                .IsRequired();
        }
    }
}
