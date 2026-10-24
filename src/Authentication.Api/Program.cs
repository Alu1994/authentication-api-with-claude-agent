using Authentication.Api.Endpoints;
using Authentication.Api.OpenApi;
using Authentication.Api.Security;
using Authentication.Application;
using Authentication.Infrastructure;
using Authentication.Infrastructure.Persistence;
using Npgsql;
using OpenTelemetry.Trace;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddProblemDetails();
builder.Services.AddApplication();
builder.Services.AddInfrastructure();
builder.Services.AddJwtAuthentication();
builder.Services.AddLoginRateLimiting();
builder.Services.AddApiDocumentation();

builder.Services.AddOpenTelemetry().WithTracing(tracing => tracing.AddNpgsql());
builder.Services.AddHealthChecks().AddDbContextCheck<AuthenticationDbContext>("postgresql");

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    // Em Development/Aspire as migrations sao aplicadas no startup.
    // Em EKS/ECS use o migrations bundle (ver README) como Job/tarefa antes do deploy.
    await app.Services.MigrateDatabaseAsync(app.Lifetime.ApplicationStopping);
}

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

// Documentacao disponivel em todos os ambientes: /openapi/v1.json e /scalar/v1.
app.MapOpenApi().AllowAnonymous();
app.MapScalarApiReference(options => options.WithTitle("Authentication API")).AllowAnonymous();

app.MapDefaultEndpoints();
app.MapAuthEndpoints();
app.MapUserEndpoints();
app.MapPermissionEndpoints();

await app.RunAsync();

/// <summary>Ponto de entrada (exposto para os testes de integracao via WebApplicationFactory).</summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public partial class Program;
