using blog.Domain.EmailVerifications.Entities;
using blog.Domain.EmailVerifications.Types;
using blog.Domain.Users.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace blog.Infrastructure.Persistence.Configurations
{
    public class EmailVerificationConfiguration : IEntityTypeConfiguration<EmailVerification>
    {
        public void Configure(EntityTypeBuilder<EmailVerification> builder)
        {
            builder.ToTable("EmailVerifications");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasConversion(
                    v => v.Value,
                    v => new EmailVerificationId(v))
                .HasColumnType("uuid");

            builder.Property(x => x.CodeHash)
                .IsRequired()
                .HasColumnType("text");

            builder.Property(x => x.Purpose)
                .HasConversion<int>()
                .HasColumnType("integer")
                .IsRequired();

            builder.Property(x => x.TargetEmail)
                .HasMaxLength(256);

            builder.Property(x => x.ExpiresAt)
                .IsRequired();

            builder.Property(x => x.Status)
                .HasConversion<int>()
                .HasColumnType("integer")
                .IsRequired();

            builder.Property(x => x.AttemptCount)
                .HasColumnType("integer")
                .HasDefaultValue(0)
                .IsRequired();

            builder.Property(x => x.UserId)
                .HasConversion(
                    v => v.Value,
                    v => new UserId(v))
                .HasColumnType("uuid")
                .IsRequired();

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => new { x.UserId, x.Status });
        }
    }
}
