using blog.Domain.EmailVerifications.Repository;
using blog.Domain.Tokens.Repository;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace blog.Infrastructure.Services
{
    public class ExpiredDataCleanupService(IServiceScopeFactory scopeFactory, ILogger<ExpiredDataCleanupService> logger) : BackgroundService
    {
        private static readonly TimeSpan Interval = TimeSpan.FromHours(6);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(Interval);

            do
            {
                await CleanupAsync(stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }

        private async Task CleanupAsync(CancellationToken ct)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();

                var refreshTokenRepository = scope.ServiceProvider.GetRequiredService<IRefreshTokenRepository>();
                var emailVerificationRepository = scope.ServiceProvider.GetRequiredService<IEmailVerificationRepository>();

                await refreshTokenRepository.DeleteExpiredAsync(ct);
                await emailVerificationRepository.DeleteExpiredAsync(ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Expired data cleanup failed");
            }
        }
    }
}