# Build a partir da raiz da solution (contexto precisa dos Directory.*.props e global.json):
#   docker build -f src/Authentication.Api/Dockerfile -t authentication-api .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Opcional: CA raiz adicional (conteudo PEM) para redes cujo proxy/antivirus inspeciona HTTPS e
# faria o restore falhar com NU1301. Vazio por padrao (nenhuma mudanca). Afeta apenas este estagio de
# build; a imagem final nao recebe o certificado. Ex.:
#   docker build --build-arg CORPORATE_CA_CERT="$(cat minha-ca.crt)" -f src/Authentication.Api/Dockerfile .
ARG CORPORATE_CA_CERT=""
RUN if [ -n "$CORPORATE_CA_CERT" ]; then \
        printf '%s\n' "$CORPORATE_CA_CERT" | tr -d '\r' > /usr/local/share/ca-certificates/corporate-ca.crt \
        && openssl x509 -in /usr/local/share/ca-certificates/corporate-ca.crt -noout \
           || { echo "CORPORATE_CA_CERT nao contem um certificado PEM valido." >&2; exit 1; } \
        && update-ca-certificates; \
    fi

# Restore em camada separada para aproveitar cache.
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/Authentication.Api/Authentication.Api.csproj src/Authentication.Api/
COPY src/Authentication.Application/Authentication.Application.csproj src/Authentication.Application/
COPY src/Authentication.Infrastructure/Authentication.Infrastructure.csproj src/Authentication.Infrastructure/
COPY aspire/Authentication.ServiceDefaults/Authentication.ServiceDefaults.csproj aspire/Authentication.ServiceDefaults/
RUN dotnet restore src/Authentication.Api/Authentication.Api.csproj

COPY src/ src/
COPY aspire/Authentication.ServiceDefaults/ aspire/Authentication.ServiceDefaults/
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
