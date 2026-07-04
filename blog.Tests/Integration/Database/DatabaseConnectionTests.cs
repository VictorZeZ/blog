using blog.Infrastructure.Persistence;
using DotNet.Testcontainers.Builders;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace blog.Tests.Integration.Database
{
    public class DatabaseConnectionTests : IAsyncLifetime
    {
        private readonly PostgreSqlContainer? _postgresContainer;

        public DatabaseConnectionTests()
        {
            try
            {
                _postgresContainer = new PostgreSqlBuilder("postgres:16-alpine").Build();
            }
            catch (DockerUnavailableException)
            {
                Assert.Skip("Docker is not available on this machine; skipping database integration test.");
            }
        }

        public async ValueTask InitializeAsync()
        {
            if (_postgresContainer is not null)
                await _postgresContainer.StartAsync(TestContext.Current.CancellationToken);
        }

        public async ValueTask DisposeAsync()
        {
            if (_postgresContainer is not null)
                await _postgresContainer.DisposeAsync();
        }

        [Fact]
        public async Task Database_Should_Be_Available()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(_postgresContainer!.GetConnectionString())
                .Options;

            await using var context = new AppDbContext(options);

            var canConnect = await context.Database.CanConnectAsync(TestContext.Current.CancellationToken);

            Assert.True(canConnect);
        }
    }
}
