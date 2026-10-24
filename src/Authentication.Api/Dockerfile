# Build a partir da raiz da solution (contexto precisa dos Directory.*.props e global.json):
#   docker build -f src/Authentication.Api/Dockerfile -t authentication-api .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore em camada separada para aproveitar cache.
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/Authentication.Api/Authentication.Api.csproj src/Authentication.Api/
COPY src/Authentication.Application/Authentication.Application.csproj src/Authentication.Application/
COPY src/Authentication.Infrastructure/Authentication.Infrastructure.csproj src/Authentication.Infrastructure/
COPY src/Authentication.ServiceDefaults/Authentication.ServiceDefaults.csproj src/Authentication.ServiceDefaults/
RUN dotnet restore src/Authentication.Api/Authentication.Api.csproj

COPY src/ src/
RUN dotnet publish src/Authentication.Api/Authentication.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app/publish .

# Usuario nao-root ja provisionado nas imagens oficiais do .NET.
USER $APP_UID

ENTRYPOINT ["dotnet", "Authentication.Api.dll"]
