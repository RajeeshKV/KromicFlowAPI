using Serilog;
using KromicFlow.Api.Configuration;
using KromicFlow.Application;
using KromicFlow.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) => configuration.ReadFrom.Configuration(context.Configuration).WriteTo.Console());

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddApiServices(builder.Configuration);

var app = builder.Build();

await app.ApplyMigrationsAsync();
if (args.Contains("--migrate-only", StringComparer.OrdinalIgnoreCase)) return;

app.UseApiPipeline();
await app.RunAsync();
