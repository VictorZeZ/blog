using blog.Domain.Categories.Repository;
using blog.Domain.Common.Interfaces;
using blog.Domain.Common.Settings;
using blog.Domain.EmailVerifications.Repository;
using blog.Domain.Posts.Repository;
using blog.Domain.Tokens.Repository;
using blog.Domain.Users.Repository;
using blog.Infrastructure.Persistence;
using blog.Infrastructure.Repositories;
using blog.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace blog.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPostRepository, PostRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IEmailVerificationRepository, EmailVerificationRepository>();

        services.AddOptions<JwtSettings>()
            .BindConfiguration(nameof(JwtSettings))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<CloudinarySettings>()
            .BindConfiguration(nameof(CloudinarySettings))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<AccountLockoutSettings>()
            .BindConfiguration(nameof(AccountLockoutSettings))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<EmailSettings>()
            .BindConfiguration(nameof(EmailSettings))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IHasher, Hasher>();
        services.AddScoped<IFileStorageService, CloudinaryService>();
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddScoped<IEmailTemplateRenderer, EmailTemplateRenderer>();
        services.AddScoped<IVerificationCodeGenerator, VerificationCodeGenerator>();
        services.AddScoped<IEmailService, EmailService>();

        return services;
    }
}