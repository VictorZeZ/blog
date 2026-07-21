using blog.Domain.Users.Entities;
using blog.Domain.Users.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace blog.Infrastructure.Persistence.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("Users");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasConversion(
                    v => v.Value,
                    v => new UserId(v))
                .HasColumnType("uuid");

            builder.Property(x => x.Email)
                .IsRequired();

            builder.HasIndex(x => x.Email)
                .IsUnique();

            builder.Property(x => x.FirstName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.LastName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.PasswordHash)
                .IsRequired()
                .HasColumnType("text");

            builder.Property(x => x.Level)
                .HasConversion<int>()
                .HasColumnType("integer")
                .IsRequired();

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.Property(x => x.IsBanned)
                .HasColumnType("boolean")
                .HasDefaultValue(false)
                .IsRequired();

            builder.Property(x => x.IsDeleted)
                .HasColumnType("boolean")
                .HasDefaultValue(false)
                .IsRequired();

            builder.Property(x => x.FailedLoginAttempts)
                .HasColumnType("integer")
                .HasDefaultValue(0)
                .IsRequired();

            builder.Property(x => x.IsEmailConfirmed)
                .HasColumnType("boolean")
                .HasDefaultValue(false)
                .IsRequired();

            builder.Property(x => x.TwoFactorEnabled)
                .HasColumnType("boolean")
                .HasDefaultValue(false)
                .IsRequired();

            // Postgres-generated STORED column so it can be indexed for trigram search.
            // Shadow property (no CLR field): accessed via EF.Property<string>(user, "FullNameSearch").
            builder.Property<string>("FullNameSearch")
                .HasComputedColumnSql("\"FirstName\" || ' ' || \"LastName\"", stored: true);

            // The explicit name argument here (not a later .HasDatabaseName() call) is required:
            // it tells EF Core this is a distinct index object on "FullNameSearch", separate from
            // any other index on the same property set.
            builder.HasIndex(["FullNameSearch"], "IX_Users_FullNameSearch_Trgm")
                .HasMethod("gin")
                .HasOperators("gin_trgm_ops");

            // Same reasoning as above: passing the name directly to HasIndex (rather than chaining
            // .HasDatabaseName() after) prevents EF Core from merging this into the unique index
            // already declared on Email above, which would otherwise produce an invalid unique GIN index.
            builder.HasIndex(x => x.Email, "IX_Users_Email_Trgm")
                .HasMethod("gin")
                .HasOperators("gin_trgm_ops");

            builder.HasMany(x => x.Posts)
                .WithOne(x => x.Author)
                .HasForeignKey(x => x.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.RefreshTokens)
                .WithOne(x => x.User)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}