using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Authentication.Api.OpenApi;

internal static class OpenApiExtensions
{
    public const string BearerSchemeName = "Bearer";

    public static IServiceCollection AddApiDocumentation(this IServiceCollection services) =>
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info = new OpenApiInfo
                {
                    Title = "Authentication API",
                    Version = "v1",
                    Description =
                        "Servico de identidade: cadastro de usuarios e emissao de JWT. " +
                        "Endpoints protegidos exigem o header `Authorization: Bearer {token}` obtido em `POST /auth/login`.",
                };

                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
                document.Components.SecuritySchemes[BearerSchemeName] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = JwtBearerDefaults.AuthenticationScheme.ToLowerInvariant(),
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "JWT obtido em POST /auth/login.",
                };

                return Task.CompletedTask;
            });

            options.AddOperationTransformer((operation, context, _) =>
            {
                var metadata = context.Description.ActionDescriptor.EndpointMetadata;
                var requiresAuthorization = metadata.OfType<IAuthorizeData>().Any()
                    && !metadata.OfType<IAllowAnonymous>().Any();

                if (requiresAuthorization)
                {
                    operation.Security ??= [];
                    operation.Security.Add(new OpenApiSecurityRequirement
                    {
                        [new OpenApiSecuritySchemeReference(BearerSchemeName, context.Document)] = [],
                    });
                }

                return Task.CompletedTask;
            });
        });

    /// <summary>Adiciona um exemplo de request body (application/json) a operacao.</summary>
    public static RouteHandlerBuilder WithRequestExample(this RouteHandlerBuilder builder, object example) =>
        builder.AddOpenApiOperationTransformer((operation, _, _) =>
        {
            if (operation.RequestBody?.Content?.TryGetValue("application/json", out var mediaType) == true)
            {
                mediaType.Example = JsonSerializer.SerializeToNode(example, JsonSerializerOptions.Web);
            }

            return Task.CompletedTask;
        });
}
