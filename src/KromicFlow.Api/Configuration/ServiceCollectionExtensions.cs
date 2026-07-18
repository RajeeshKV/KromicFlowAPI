using System.Text;
using Microsoft.AspNetCore.RateLimiting;
using KromicFlow.Application.Options;
using KromicFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace KromicFlow.Api.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "Kromic Flow API", Version = "v1" });
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header
            });
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }] = []
            });
        });

        services.AddProblemDetails();
        services.AddCors(options => options.AddPolicy("Frontend", policy =>
        {
            var origins = GetAllowedOrigins(configuration);
            policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
        }));

        var jwt = configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateIssuerSigningKey = true,
                ValidateLifetime = true,
                ValidIssuer = jwt.Issuer,
                ValidAudience = jwt.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key))
            };
        });
        services.AddAuthorization(options => options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin")));

        services.AddRateLimiter(options =>
        {
            options.AddFixedWindowLimiter("auth", limiter => { limiter.PermitLimit = 20; limiter.Window = TimeSpan.FromMinutes(1); });
            options.AddFixedWindowLimiter("api", limiter => { limiter.PermitLimit = 120; limiter.Window = TimeSpan.FromMinutes(1); });
            options.AddFixedWindowLimiter("webhooks", limiter => { limiter.PermitLimit = 300; limiter.Window = TimeSpan.FromMinutes(1); });
        });

        services.AddOpenTelemetry().WithTracing(tracing => tracing
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("KromicFlow.Api"))
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddConsoleExporter());

        return services;
    }

    private static string[] GetAllowedOrigins(IConfiguration configuration)
    {
        var csvOrigins = configuration["Cors:AllowedOrigins"] ?? string.Empty;
        var arrayOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

        var origins = csvOrigins
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Concat(arrayOrigins)
            .Where(origin => !string.IsNullOrWhiteSpace(origin))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return origins.Length > 0 ? origins : ["http://localhost:3000"];
    }
}
