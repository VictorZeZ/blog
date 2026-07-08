using blog.Domain.Categories.Types;
using blog.Domain.Posts.Entities;
using blog.Domain.Posts.Types;
using blog.Domain.Users.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace blog.Infrastructure.Persistence.Configurations
{
    public class PostConfiguration : IEntityTypeConfiguration<Post>
    {
        public void Configure(EntityTypeBuilder<Post> builder)
        {
            builder.ToTable("Posts");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasConversion(
                    v => v.Value,
                    v => new PostId(v))
                .HasColumnType("uuid");

            builder.Property(x => x.Title)
                .IsRequired();

            builder.Property(x => x.TitleImageUrl)
                .HasMaxLength(512);

            builder.Property(x => x.Content)
                .IsRequired()
                .HasColumnType("text");

            builder.Property(x => x.Slug)
                .IsRequired();

            builder.HasIndex(x => x.Slug)
                .IsUnique();

            builder.Property(x => x.Tags)
                .HasColumnType("text[]")
                .IsRequired();

            builder.HasIndex(x => x.Tags)
                .HasMethod("GIN");

            builder.Property(x => x.Status)
                .HasConversion<int>()
                .HasColumnType("integer")
                .IsRequired();

            builder.Property(x => x.ViewCount)
                .HasColumnType("integer")
                .HasDefaultValue(0)
                .IsRequired();

            builder.Property(x => x.AuthorId)
                .HasConversion(
                    v => v.Value,
                    v => new UserId(v))
                .HasColumnType("uuid")
                .IsRequired();

            builder.Property(x => x.CategoryId)
                .HasConversion(
                    v => v.Value,
                    v => new CategoryId(v))
                .HasColumnType("uuid")
                .IsRequired();

            builder.HasOne(x => x.Category)
                .WithMany()
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.HasOne(x => x.Author)
                .WithMany(x => x.Posts)
                .HasForeignKey(x => x.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasGeneratedTsVectorColumn(
                    x => x.SearchVector,
                    "english",
                    x => new { x.Title, x.Content })
                .HasIndex(x => x.SearchVector)
                .HasMethod("GIN");
        }
    }
}
