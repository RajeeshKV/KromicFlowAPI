using KromicFlow.Application.Abstractions;
using KromicFlow.Application.Options;
using KromicFlow.Infrastructure.Background;
using KromicFlow.Infrastructure.External;
using KromicFlow.Infrastructure.Persistence;
using KromicFlow.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection("Jwt"))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddOptions<MetaOptions>()
            .Bind(configuration.GetSection("Meta"))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddOptions<BrevoOptions>()
            .Bind(configuration.GetSection("Brevo"))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddOptions<PlatformOptions>()
            .Bind(configuration.GetSection("Platform"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddDbContext<KromicFlowDbContext>(options => options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
        services.AddMemoryCache();
        services.AddDataProtection();
        services.AddScoped<IKromicFlowDbContext>(provider => provider.GetRequiredService<KromicFlowDbContext>());
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IAuditWriter, AuditWriter>();
        services.AddScoped<IOAuthStateService, OAuthStateService>();
        services.AddScoped<IDataProtectionService, DataProtectionService>();
        services.AddScoped<IOutboxEventPublisher, OutboxEventPublisher>();
        services.AddHttpClient<IMetaApiClient, MetaApiClient>((provider, client) =>
        {
            var meta = configuration.GetSection("Meta").Get<MetaOptions>() ?? new MetaOptions();
            client.BaseAddress = new Uri(meta.GraphApiBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        services.AddHttpClient<INotificationSender, BrevoNotificationSender>((provider, client) =>
        {
            var brevo = configuration.GetSection("Brevo").Get<BrevoOptions>() ?? new BrevoOptions();
            client.BaseAddress = new Uri(brevo.BaseUrl);
        });
        services.AddHostedService<TokenRefreshBackgroundService>();
        services.AddHostedService<OutboxEventBackgroundService>();
        return services;
    }
}
