using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace Authentication.Api.Security;

/// <summary>Configuracao do limite de tentativas de login (secao <c>RateLimiting:Login</c>).</summary>
[ExcludeFromCodeCoverage]
internal sealed class LoginRateLimitOptions
{
    public const string SectionName = "RateLimiting:Login";

    /// <summary>Tentativas permitidas por IP dentro da janela.</summary>
    public int PermitLimit { get; set; } = 5;

    /// <summary>Duracao da janela fixa, em segundos.</summary>
    public int WindowSeconds { get; set; } = 60;
}

/// <summary>
/// Rate limiting nativo do ASP.NET Core aplicado somente ao <c>POST /auth/login</c>:
/// janela fixa particionada por IP do cliente, sem fila (excedentes recebem 429 imediatamente).
/// </summary>
internal static partial class LoginRateLimiting
{
    public const string PolicyName = "login";

    public static IServiceCollection AddLoginRateLimiting(this IServiceCollection services)
    {
        services.AddOptions<LoginRateLimitOptions>()
            .BindConfiguration(LoginRateLimitOptions.SectionName)
            .Validate(
                options => options.PermitLimit > 0 && options.WindowSeconds > 0,
                $"{LoginRateLimitOptions.SectionName}: PermitLimit e WindowSeconds devem ser maiores que zero.")
            .ValidateOnStart();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = WriteRejectionAsync;

            options.AddPolicy(PolicyName, httpContext =>
            {
                var settings = httpContext.RequestServices.GetRequiredService<IOptions<LoginRateLimitOptions>>().Value;

                // Atras de load balancer/ingress, habilite ASPNETCORE_FORWARDEDHEADERS_ENABLED=true para que
                // RemoteIpAddress reflita o IP real do cliente (X-Forwarded-For). Ver README.
                var partitionKey = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = settings.PermitLimit,
                    Window = TimeSpan.FromSeconds(settings.WindowSeconds),
                    QueueLimit = 0,
                    AutoReplenishment = true,
                });
            });
        });

        return services;
    }

    private static async ValueTask WriteRejectionAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        var httpContext = context.HttpContext;

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            httpContext.Response.Headers.RetryAfter =
                ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
        }

        LogLoginRateLimited(httpContext.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(LoginRateLimiting).FullName!));

        var problemDetailsService = httpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
        await problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails =
            {
                Status = StatusCodes.Status429TooManyRequests,
                Title = "Muitas requisicoes",
                Detail = "Limite de tentativas de login excedido. Tente novamente mais tarde.",
                Extensions = { ["code"] = "auth.rate_limited" },
            },
        });
    }

    [LoggerMessage(EventId = 2003, Level = LogLevel.Warning, Message = "Tentativa de login bloqueada por rate limiting.")]
    private static partial void LogLoginRateLimited(ILogger logger);
}
