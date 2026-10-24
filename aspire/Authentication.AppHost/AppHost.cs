var builder = DistributedApplication.CreateBuilder(args);

// Chave de assinatura do JWT: informe via user-secrets do AppHost
// (dotnet user-secrets set "Parameters:jwt-signing-key" "<chave com 32+ caracteres>")
// ou pelo prompt do dashboard do Aspire. Nunca versionar.
var jwtSigningKey = builder.AddParameter("jwt-signing-key", secret: true);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var authDb = postgres.AddDatabase("authdb");

builder.AddProject<Projects.Authentication_Api>("authentication-api")
    .WithReference(authDb)
    .WaitFor(authDb)
    .WithEnvironment("Jwt__SigningKey", jwtSigningKey)
    .WithHttpHealthCheck("/healthcheck");

await builder.Build().RunAsync();
