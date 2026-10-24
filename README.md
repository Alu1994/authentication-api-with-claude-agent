# Authentication

Servico de identidade reutilizavel: cadastro de usuarios (CRUD com soft delete), catalogo de permissoes,
roles que agrupam permissoes, atribuicao de roles a usuarios e emissao de JWT com as roles e permissoes do
usuario. Pensado para ser consumido por varios sistemas, sem regras especificas de um frontend.

> A checagem de permissoes nos proprios endpoints desta API (enforcement) e a auditoria ficam para as
> proximas historias.

## Stack

- .NET 10 (LTS) + ASP.NET Core Minimal APIs
- PostgreSQL + EF Core 10 (migrations, nomes em `snake_case`)
- JWT HS256 (`Microsoft.AspNetCore.Authentication.JwtBearer`)
- Hash de senha: `PasswordHasher` do ASP.NET Core Identity (PBKDF2/HMAC-SHA512, 100k iteracoes)
- OpenAPI nativo (`Microsoft.AspNetCore.OpenApi`) + UI Scalar
- OpenTelemetry (traces, metricas e logs) com exportacao OTLP
- .NET Aspire para orquestracao local
- Testes: xUnit v3 (Microsoft Testing Platform) + Moq + Testcontainers (PostgreSQL)

## Arquitetura

Tres camadas, com CQRS sem dispatcher (cada Command/Query tem seu handler injetado direto no endpoint):

```
src/
  Authentication.Api              Presentation: endpoints, contratos HTTP, JWT bearer, OpenAPI, health checks
  Authentication.Application      Entidades (User, Permission, Role e vinculos), Commands/Queries + Handlers, regras de validacao, portas
  Authentication.Infrastructure   EF Core/PostgreSQL, repositorios, migrations, hasher de senha, emissao de JWT
aspire/
  Authentication.ServiceDefaults  OpenTelemetry, health checks base, service discovery, resiliencia HTTP
  Authentication.AppHost          Aspire: PostgreSQL (container) + API
tests/
  Authentication.Application.Tests     Unitarios: regras, entidade e handlers
  Authentication.Infrastructure.Tests  Unitarios: JWT, opcoes de JWT e hasher
  Authentication.Api.IntegrationTests  Ponta a ponta (API real + PostgreSQL em container)
```

Erros de negocio trafegam como `Result`/`Error` (sem excecoes para fluxo esperado) e viram
`ProblemDetails` (RFC 9457) na API, com o campo `code` (ex.: `user.email_in_use`).

## Documentacao da API

A referencia dos endpoints (rotas, autenticacao, contratos e codigos de resposta) fica na propria API,
em todos os ambientes: UI interativa em `/scalar/v1` e documento OpenAPI em `/openapi/v1.json`.
Exemplos de requisicao prontos em [`src/Authentication.Api/Authentication.Api.http`](src/Authentication.Api/Authentication.Api.http).
Os health checks (`/heartbeat` e `/healthcheck`) estao descritos em [Docker](#docker).

> Nesta versao qualquer usuario autenticado pode consultar/alterar/excluir qualquer usuario e gerenciar
> permissoes e roles, inclusive atribuir qualquer role a si mesmo (risco aceito ate a historia de enforcement). A restricao por permissao sera feita em uma historia propria de enforcement.

## Proposta e principais decisoes

A API centraliza a identidade dos usuarios para que outros sistemas nao precisem reimplementar
cadastro e login: ela mantem o cadastro de usuarios e autentica por e-mail e senha, devolvendo um JWT
que os sistemas consumidores validam por conta propria.

Algumas decisoes de produto orientam o comportamento:

- **Identidade estavel:** e-mail e CPF identificam uma pessoa de forma unica e nunca sao reaproveitados,
  nem depois que o usuario e excluido. O CPF nao muda depois do cadastro.
- **Exclusao preserva historico:** usuarios excluidos deixam de aparecer e de autenticar, mas o registro
  e mantido (soft delete), garantindo a regra acima e permitindo auditoria futura.
- **Senha sob responsabilidade da API:** a senha e armazenada apenas como hash forte, e a troca exige a
  senha atual.
- **Login resistente a abuso:** a resposta de falha nao revela se o e-mail existe (nem pelo conteudo, nem
  pelo tempo de resposta), e o numero de tentativas de login por IP e limitado, de forma configuravel,
  para mitigar forca bruta e credential stuffing.
- **Permissoes como catalogo aberto:** permissoes sao pares recurso + acao (ex.: `users:read`) cadastrados
  pela propria API, de modo que novos recursos e acoes nao exigem alteracao de codigo. Uma permissao nao
  muda depois de criada (so e criada ou excluida), e a exclusao tambem e logica, liberando o mesmo par para
  ser recriado.
- **Roles como agrupamento:** roles reunem permissoes sob um nome unico e podem ser editadas livremente.
  Uma permissao em uso por alguma role, ou uma role atribuida a algum usuario, nao pode ser excluida,
  evitando que acessos sumam sem que alguem perceba.
- **Autorizacao no token:** o login inclui no JWT as roles do usuario e as permissoes dessas roles, para que
  os sistemas consumidores decidam o acesso sem consultar esta API a cada requisicao. Por isso, mudancas em
  roles e permissoes so passam a valer no proximo login; tokens ja emitidos seguem validos ate expirar.

O detalhe de campos, validacoes, contratos e limites fica no codigo, na configuracao, nas migrations e na
[documentacao da API](#documentacao-da-api).

## Configuracao

| Chave | Descricao |
|---|---|
| `ConnectionStrings__authdb` | Connection string do PostgreSQL (injetada automaticamente pelo Aspire) |
| `Jwt__Issuer` / `Jwt__Audience` | Padrao em `appsettings.json` (`authentication-api` / `authentication-clients`) |
| `Jwt__SigningKey` | **Segredo**, minimo de 32 caracteres. Nunca versionar: user-secrets em dev, Secrets Manager/variavel de ambiente em EKS/ECS |
| `Jwt__ExpirationMinutes` | Validade do token (padrao 60) |
| `RateLimiting__Login__PermitLimit` | Tentativas de login por IP por janela (padrao 5) |
| `RateLimiting__Login__WindowSeconds` | Duracao da janela, em segundos (padrao 60) |
| `ASPNETCORE_FORWARDEDHEADERS_ENABLED` | `true` atras de ALB/ingress, para o rate limit usar o IP real do cliente |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | Endpoint OTLP do collector/sidecar. Se ausente, nada e exportado |
| `OTEL_SERVICE_NAME` / `OTEL_RESOURCE_ATTRIBUTES` | Variaveis padrao do OpenTelemetry para identificar o servico |

As opcoes de JWT e de rate limiting sao validadas no startup: a aplicacao nao sobe com chave JWT ausente/curta
ou com limites menores ou iguais a zero.

## Execucao local

Pre-requisitos: .NET SDK 10.0.401+ (fixado em `global.json`) e Docker em execucao.

```bash
dotnet tool restore   # dotnet-ef e reportgenerator (tools locais)
```

### Com Aspire (recomendado)

```bash
dotnet user-secrets set "Parameters:jwt-signing-key" "<chave-com-32-ou-mais-caracteres>" --project aspire/Authentication.AppHost
dotnet run --project aspire/Authentication.AppHost
```

O AppHost sobe o PostgreSQL (container com volume persistente), injeta a connection string e a chave na API
e abre o dashboard do Aspire com logs, traces e metricas (o Aspire define `OTEL_EXPORTER_OTLP_ENDPOINT`).
Se o segredo nao estiver configurado, o dashboard pede o valor.

### Somente a API

```bash
dotnet user-secrets set "Jwt:SigningKey" "<chave-com-32-ou-mais-caracteres>" --project src/Authentication.Api
dotnet user-secrets set "ConnectionStrings:authdb" "Host=localhost;Port=5432;Database=authdb;Username=postgres;Password=<senha>" --project src/Authentication.Api
dotnet run --project src/Authentication.Api   # http://localhost:5080/scalar/v1
```

Em `Development` (inclusive via Aspire) as migrations sao aplicadas automaticamente no startup.

## Migrations

```bash
# nova migration
dotnet ef migrations add <Nome> --project src/Authentication.Infrastructure --startup-project src/Authentication.Infrastructure --output-dir Persistence/Migrations
```

### Deploy (EKS/ECS)

Fora de `Development` a API **nao** aplica migrations. Gere um bundle autocontido no pipeline e rode-o
como Kubernetes Job (ou init container/hook do Helm) no EKS, ou como tarefa ECS avulsa,
**antes** de publicar a nova versao da API:

```bash
dotnet ef migrations bundle --project src/Authentication.Infrastructure --startup-project src/Authentication.Infrastructure \
  --self-contained --target-runtime linux-x64 --output efbundle

./efbundle --connection "$ConnectionStrings__authdb"
```

O bundle e idempotente: aplica apenas as migrations pendentes.

## Docker

```bash
docker build -f src/Authentication.Api/Dockerfile -t authentication-api .
docker run -p 8080:8080 \
  -e ConnectionStrings__authdb="..." \
  -e Jwt__SigningKey="..." \
  -e ASPNETCORE_FORWARDEDHEADERS_ENABLED=true \
  -e OTEL_EXPORTER_OTLP_ENDPOINT="http://otel-collector:4317" \
  authentication-api
```

A imagem roda como usuario nao-root na porta 8080. Probes sugeridos:
`livenessProbe` -> `/heartbeat`, `readinessProbe` -> `/healthcheck`.

## Testes e cobertura

```bash
dotnet test
```

Os testes de integracao usam Testcontainers e precisam do Docker em execucao.
Cobertura consolidada (unitarios + integracao, meta de 90%):

```bash
dotnet test --coverlet --coverlet-output-format cobertura \
  --coverlet-exclude-by-attribute ExcludeFromCodeCoverage --coverlet-exclude-by-attribute GeneratedCode \
  --coverlet-exclude-by-file "**/Migrations/*.cs"
dotnet reportgenerator -reports:"TestResults/coverage.cobertura.*.xml" -targetdir:TestResults/report \
  -reporttypes:"TextSummary;Html" -assemblyfilters:"+Authentication.*;-*.Tests;-*.IntegrationTests"
```

DTOs, `Program` e classes de bootstrap estao marcados com `[ExcludeFromCodeCoverage]`.
