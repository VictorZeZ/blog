using blog.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace blog.Tests.Integration.Database
{
    public class DatabaseConnectionTests
    {
        [Fact]
        public async Task Database_Should_Be_Available()
        {
            // Use the same configuration sources as the main API.
            var apiProjectPath = Path.GetFullPath(
                Path.Combine(
                    AppContext.BaseDirectory,
                    "../../../../blog.Api"));

            var environmentName =
                Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                ?? Environments.Development;

            var builder = WebApplication.CreateBuilder(
                new WebApplicationOptions
                {
                    ApplicationName = "blog.Api",
                    ContentRootPath = apiProjectPath,
                    EnvironmentName = environmentName
                });

            var connectionString =
                builder.Configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                Assert.Fail(
                    "The PostgreSQL connection string 'ConnectionStrings:DefaultConnection' was not found. " +
                    "Configure it in User Secrets, appsettings, or environment variables.");
            }

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(connectionString)
                .EnableDetailedErrors()
                .Options;

            await using var context = new AppDbContext(options);

            try
            {
                var canConnect =
                    await context.Database.CanConnectAsync(
                        TestContext.Current.CancellationToken);

                Assert.True(
                    canConnect,
                    "The application could not connect to PostgreSQL.");
            }
            catch (Exception exception)
            {
                Assert.Fail(
                    $"PostgreSQL connection failed.{Environment.NewLine}" +
                    $"{exception.GetType().Name}: {exception.Message}");
            }
        }
    }
}