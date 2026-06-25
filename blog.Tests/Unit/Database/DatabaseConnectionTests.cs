using blog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace blog.Tests.Unit.Database
{
    public class DatabaseConnectionTests
    {
        [Fact]
        public async Task Database_Should_Be_Available()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(
                    "Host=localhost;Database=blog;Username=postgres;Password=1384")
                .Options;

            await using var context = new AppDbContext(options);

            var canConnect = await context.Database.CanConnectAsync();

            Assert.True(canConnect);
        }
    }
}
