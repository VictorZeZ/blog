using blog.Domain.Tokens.Entities;
using blog.Domain.Tokens.Types;
using blog.Domain.Users.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace blog.Infrastructure.Persistence.Configurations
{
    public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.ToTable("RefreshTokens");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasConversion(
                    v => v.Value,
                    v => new RefreshTokenId(v))
                .HasColumnType("uuid");

            builder.Property(x => x.TokenHash)
                .IsRequired()
                .HasColumnType("text");

            builder.HasIndex(x => x.TokenHash)
                .IsUnique();

            builder.Property(x => x.ExpiresAt)
                .IsRequired();

            builder.Property(x => x.Status)
                .HasConversion<int>()
                .HasColumnType("integer")
                .IsRequired();

            builder.Property(x => x.DeviceInfo)
                .IsRequired()
                .HasMaxLength(512);

            builder.Property(x => x.UserId)
                .HasConversion(
                    v => v.Value,
                    v => new UserId(v))
                .HasColumnType("uuid")
                .IsRequired();

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.HasIndex(x => new { x.UserId, x.Status });
        }
    }
}
