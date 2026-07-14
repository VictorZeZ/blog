using blog.Domain.Categories.Entities;
using blog.Domain.EmailVerifications.Entities;
using blog.Domain.Posts.Entities;
using blog.Domain.Tokens.Entities;
using blog.Domain.Users.Entities;
using Microsoft.EntityFrameworkCore;

namespace blog.Infrastructure.Persistence
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<User> Users => Set<User>();
        public DbSet<Post> Posts => Set<Post>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
        public DbSet<EmailVerification> EmailVerifications => Set<EmailVerification>();


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
            base.OnModelCreating(modelBuilder);
        }

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            configurationBuilder.Properties<DateTime>()
                .HaveColumnType("timestamp with time zone");

            configurationBuilder.Properties<string>()
                .HaveMaxLength(256);
        }
    }
}
