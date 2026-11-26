---

description: "Task list for 001-project-foundation"
---

# Tasks: Base do Projeto (Fundação da Instância QueryAI)

**Input**: Design documents from `specs/001-project-foundation/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/ (openapi.yaml,
environment.md, ui-routes.md), quickstart.md

**Tests**: INCLUDED. A constituição (Princípio V) torna os testes automatizados critério de pronto, e
a spec exige verificação automatizada em SC-004 e SC-007. Dentro de cada história, os testes vêm
primeiro e devem falhar antes da implementação.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- Backend: `src/QueryAI.{Domain,Application,Infrastructure,Api}/`
- Backend tests: `tests/QueryAI.{Domain.Tests,Application.Tests,Api.IntegrationTests,ArchitectureTests}/`
- Frontend: `web/src/`
- Distribution: `docker-compose.yml`, `.env.example`, `deploy/`, `docs/` at repo root

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure

- [X] T001 Create `global.json` pinning .NET SDK 10.0.x (rollForward `latestFeature`) and `QueryAI.slnx` at repo root
- [X] T002 Create `Directory.Build.props` at repo root: `TargetFramework` net10.0, `Nullable` enable, `ImplicitUsings` enable, `TreatWarningsAsErrors` true, `AnalysisLevel` latest-recommended, `InvariantGlobalization` true
- [X] T003 Create `Directory.Packages.props` at repo root with Central Package Management (`ManagePackageVersionsCentrally`) listing all packages from research.md R2–R16 (Microsoft.AspNetCore.OpenApi, Scalar.AspNetCore, Npgsql.EntityFrameworkCore.PostgreSQL, EFCore.NamingConventions, Microsoft.EntityFrameworkCore.Design, Microsoft.Extensions.Identity.Core, FluentValidation.DependencyInjectionExtensions, Serilog.AspNetCore, Serilog.Formatting.Compact, Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore, xunit.v3, Shouldly, NSubstitute, Testcontainers.PostgreSql, Microsoft.AspNetCore.Mvc.Testing, NetArchTest.Rules)
- [X] T004 Create class library projects `src/QueryAI.Domain/QueryAI.Domain.csproj` (no package references), `src/QueryAI.Application/QueryAI.Application.csproj` (references Domain), `src/QueryAI.Infrastructure/QueryAI.Infrastructure.csproj` (references Application) and web project `src/QueryAI.Api/QueryAI.Api.csproj` (references Application and Infrastructure); add all to `QueryAI.slnx`
- [X] T005 Create test projects `tests/QueryAI.Domain.Tests`, `tests/QueryAI.Application.Tests`, `tests/QueryAI.Api.IntegrationTests` and `tests/QueryAI.ArchitectureTests` (xUnit v3 + Shouldly + NSubstitute; IntegrationTests also Testcontainers.PostgreSql + Mvc.Testing) with the corresponding project references; add them to `QueryAI.slnx`
- [X] T006 [P] Create `.editorconfig` (C# style, file-scoped namespaces, 4-space C#, 2-space JS/JSON/YAML), `.gitignore` (bin/, obj/, node_modules/, dist/, `.env`, coverage) and `.dockerignore` at repo root
- [X] T007 [P] Scaffold the frontend in `web/` with Vite + React 19 in **JavaScript** (no TypeScript): `web/package.json` (scripts `dev`, `build`, `preview`, `test`, `lint`, `format`), `web/index.html` (`lang="pt-BR"`), `web/vite.config.js` (dev proxy `/api` and `/health` → `http://localhost:5080`, `define` for `VITE_APP_VERSION`, Vitest config with jsdom and `web/src/test/setup.js`), `web/src/main.jsx`; dependencies: react, react-dom, react-router, @tanstack/react-query, lucide-react; dev: vitest, @testing-library/react, @testing-library/user-event, @testing-library/jest-dom, jsdom
- [X] T008 [P] Configure ESLint flat config in `web/eslint.config.js` (react, react-hooks, jsx-a11y) and Prettier in `web/.prettierrc`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

### Domain core

- [X] T009 [P] Create `src/QueryAI.Domain/Common/Entity.cs` (base with `Guid Id` from `Guid.CreateVersion7()`), `src/QueryAI.Domain/Common/Error.cs` (record `Code`, `Message`, `ErrorKind` enum: Validation, Unauthorized, Forbidden, NotFound, Conflict, Locked) and `src/QueryAI.Domain/Common/Result.cs` (`Result` and `Result<T>` with `Success`/`Failure`)
- [X] T010 [P] Create `src/QueryAI.Domain/Common/IClock.cs` (`DateTimeOffset UtcNow`)
- [X] T011 [P] Create `src/QueryAI.Domain/Users/Role.cs` enum `Admin`, `Analyst` and `src/QueryAI.Domain/Users/UserErrors.cs` with codes `invalid_credentials`, `account_locked`, `invalid_current_password`, `same_password`, `email_already_exists`, `last_admin`, `not_found`, `password_change_required`
- [X] T012 Create aggregate `src/QueryAI.Domain/Users/User.cs` per data-model.md: fields `Name` ("obrigatório, 2–120 caracteres após trim"), `Email` ("obrigatório, formato de e-mail válido, armazenado como informado (trim)", max 254), `NormalizedEmail` ("`UPPER(trim(email))`"), `PasswordHash`, `Role`, `IsActive` (default true), `MustChangePassword`, `FailedLoginCount` ("≥ 0, zerado ao logar com sucesso"), `LockoutEndUtc?`, `SecurityStamp` (Guid), `CreatedAtUtc`, `UpdatedAtUtc`, `LastLoginAtUtc?`; static factory `Create(name, email, passwordHash, role, mustChangePassword, clock)` and methods `RegisterFailedLogin(now)` (5 falhas → `LockoutEndUtc = now + 15 min` e zera contador), `RegisterSuccessfulLogin(now)`, `IsLockedOut(now)`, `ChangePassword(newHash)` (`MustChangePassword=false`, novo stamp), `ResetPassword(tempHash)` (`MustChangePassword=true`, novo stamp, desbloqueia), `Rename(name)`, `ChangeRole(role)` (novo stamp), `Deactivate()` (novo stamp), `Activate()`
- [X] T013 [P] Create `src/QueryAI.Domain/Users/PasswordPolicy.cs` with `Validate(string password)` → `Result` ("mínimo de 12 e máximo de 128 caracteres")
- [X] T014 [P] Create `src/QueryAI.Domain/Auditing/SecurityEventType.cs` (`LoginSucceeded`, `LoginFailed`, `AccountLockedOut`, `Logout`, `PasswordChanged`, `PasswordReset`, `UserCreated`, `UserUpdated`, `UserRoleChanged`, `UserDeactivated`, `UserActivated`, `InitialAdminCreated`) and `src/QueryAI.Domain/Auditing/SecurityAuditEvent.cs` (insert-only: `EventType`, `ActorUserId?`, `TargetUserId?`, `OccurredAtUtc`, `IpAddress?` max 45, `UserAgent?` truncado em 512, `Details?` dicionário sem dados sensíveis)

### Application core

- [X] T015 [P] Create `src/QueryAI.Application/Abstractions/Messaging/ICommandHandler.cs` and `IQueryHandler.cs` (`Task<Result<T>> HandleAsync(TRequest, CancellationToken)`)
- [X] T016 [P] Create `src/QueryAI.Application/Abstractions/` interfaces: `IUserRepository.cs` (GetById, GetByNormalizedEmail, Add, List paginated with search, `CountOtherActiveAdminsForUpdateAsync(Guid excludeId, CancellationToken)`), `IUnitOfWork.cs` (`SaveChangesAsync`, `ExecuteInTransactionAsync`), `IPasswordHasher.cs` (Hash, Verify → `PasswordVerification` { Failed, Success, SuccessRehashNeeded }), `ISecurityAuditWriter.cs`, `ICurrentUser.cs` (UserId, IpAddress, UserAgent)
- [X] T017 Create `src/QueryAI.Application/Behaviors/ValidationDecorator.cs` (decorates command/query handlers, runs FluentValidation validators and returns `Error` kind Validation with field errors) and `src/QueryAI.Application/DependencyInjection.cs` (`AddApplication()` scans handlers/validators and applies the decorator)

### Persistence & infrastructure

- [X] T018 Create `src/QueryAI.Infrastructure/Persistence/AppDbContext.cs` with `DbSet<User>` and `DbSet<SecurityAuditEvent>`, `UseSnakeCaseNamingConvention()`, and configurations in `src/QueryAI.Infrastructure/Persistence/Configurations/UserConfiguration.cs` (table `users`, unique index on `normalized_email`, `role` as string max 20, `name` max 120, `email`/`normalized_email` max 254, `xmin` concurrency token via `IsRowVersion()`) and `SecurityAuditEventConfiguration.cs` (table `security_audit_events`, `event_type` string max 50, `details` jsonb, index on `occurred_at_utc`, FKs to `users` without cascade)
- [X] T019 Create `src/QueryAI.Infrastructure/Persistence/UnitOfWork.cs`, `src/QueryAI.Infrastructure/Persistence/Repositories/UserRepository.cs` (`CountOtherActiveAdminsForUpdateAsync` uses raw SQL `SELECT id FROM users WHERE role='Admin' AND is_active AND id <> @id FOR UPDATE`), `src/QueryAI.Infrastructure/Common/SystemClock.cs`
- [X] T020 Generate initial migration `InitialCreate` in `src/QueryAI.Infrastructure/Persistence/Migrations/` (users + security_audit_events) with a design-time factory `src/QueryAI.Infrastructure/Persistence/DesignTimeDbContextFactory.cs`
- [X] T021 Create `src/QueryAI.Infrastructure/DependencyInjection.cs` (`AddInfrastructure(IConfiguration)`: DbContext with Npgsql + `EnableRetryOnFailure`, repositories, UnitOfWork, SystemClock)

### API host core

- [X] T022 Create `src/QueryAI.Api/Program.cs` skeleton: Serilog bootstrap logger, `AddApplication()`, `AddInfrastructure()`, `AddProblemDetails()`, endpoint group registration placeholder, `public partial class Program;` for tests
- [X] T023 [P] Configure Serilog in `src/QueryAI.Api/Logging/LoggingSetup.cs`: `RenderedCompactJsonFormatter` to stdout, `UseSerilogRequestLogging` enriched with `TraceId`, `UserId`, `RequestPath`; minimum level from `QUERYAI_LOG_LEVEL`; destructuring policy `src/QueryAI.Api/Logging/SensitiveDataPolicy.cs` masking properties named `Password`, `NewPassword`, `CurrentPassword`, `TemporaryPassword`, `PasswordHash`, `Token`, `Secret`, `ConnectionString`
- [X] T024 [P] Create `src/QueryAI.Api/Errors/GlobalExceptionHandler.cs` (`IExceptionHandler` → 500 ProblemDetails with `code=unexpected_error` and `traceId`, never exception details) and `src/QueryAI.Api/Errors/ResultExtensions.cs` mapping `Error` kinds to HTTP (Validation 400 + `errors`, Unauthorized 401, Forbidden 403, NotFound 404, Conflict 409, Locked 423) with `code` and `traceId` per contracts/openapi.yaml `Problem` schema
- [X] T025 [P] Create `src/QueryAI.Api/Options/OptionsValidation.cs` with a reusable `NotPlaceholderAttribute` / validator rejecting empty and placeholder values (`change-me`, `changeme`, `example`, case-insensitive) and producing errors naming the environment variable

### Test infrastructure

- [X] T026 [P] Create `tests/QueryAI.Api.IntegrationTests/Infrastructure/PostgresFixture.cs` (Testcontainers `postgres:17-alpine`, shared collection fixture) and `QueryAIWebApplicationFactory.cs` (overrides connection string and admin env vars, environment `Testing`, `QUERYAI_ALLOW_INSECURE_HTTP=true`, captures Serilog output into an in-memory sink for later inspection)
- [X] T027 [P] Create `tests/QueryAI.ArchitectureTests/LayerDependencyTests.cs` (NetArchTest): Domain depends on no other QueryAI project nor on `Microsoft.EntityFrameworkCore`/`Microsoft.AspNetCore`; Application does not depend on Infrastructure/Api/EF Core; Infrastructure does not depend on Api
- [X] T028 [P] Create `tests/QueryAI.Domain.Tests/Users/UserTests.cs` covering `RegisterFailedLogin` (lockout on 5th failure for 15 min, counter reset), `RegisterSuccessfulLogin`, `IsLockedOut`, `ChangePassword`/`ResetPassword`/`ChangeRole`/`Deactivate` regenerating `SecurityStamp`, and `tests/QueryAI.Domain.Tests/Users/PasswordPolicyTests.cs` (11, 12, 128, 129 chars)

### Frontend core

- [X] T029 [P] Create design tokens and base styles in `web/src/styles/tokens.css` (cores neutras, cor de destaque, espaçamentos, raios, tipografia, suporte a `prefers-color-scheme`) and `web/src/styles/global.css`
- [X] T030 [P] Create `web/src/api/httpClient.js`: `fetch` wrapper with `credentials: 'same-origin'`, JSON body, header `X-Requested-With: QueryAI` on non-GET, parses `application/problem+json` into `ApiError { status, code, title, detail, traceId, errors }` (JSDoc-typed), throws on non-2xx
- [X] T031 [P] Create base UI components in `web/src/components/`: `Button.jsx`, `Input.jsx` (label + field error), `Spinner.jsx`, `ErrorMessage.jsx` (mensagem pt-BR + `traceId`), `EmptyState.jsx` (ícone, título, descrição), each with a `.module.css`
- [X] T032 Create `web/src/app/App.jsx` with `QueryClientProvider` and `web/src/app/router.jsx` (React Router data router) with a placeholder home route; wire into `web/src/main.jsx`
- [X] T033 [P] Create `web/src/test/setup.js` (jest-dom matchers) and `web/src/api/httpClient.test.js` (problem+json parsing, CSRF header on POST, none on GET)

**Checkpoint**: Foundation ready — `dotnet build` and `npm run build` pass; domain and architecture tests green

---

## Phase 3: User Story 1 - Subir uma instância funcional com um único comando (Priority: P1) 🎯 MVP

**Goal**: o operador preenche o `.env`, executa `docker compose up -d` e obtém uma instância saudável (banco migrado, health checks, versão, web acessível), com falha clara se faltar configuração.

**Independent Test**: em máquina limpa, seguir `docs/installation.md`; `docker compose ps` mostra db/api/web healthy em ≤ 3 min; `curl localhost:8080/health/ready` → Healthy; `curl localhost:8080/api/version` → versão; a página inicial carrega exibindo a versão; com `QUERYAI_ADMIN_PASSWORD=change-me` a api encerra citando a variável.

### Tests for User Story 1 ⚠️

- [X] T034 [P] [US1] Integration test `tests/QueryAI.Api.IntegrationTests/Operations/HealthTests.cs`: `/health/live` 200; `/health/ready` 200 with `database` check Healthy; with the Postgres container stopped (separate fixture) `/health/ready` 503 naming `database`
- [X] T035 [P] [US1] Integration test `tests/QueryAI.Api.IntegrationTests/Operations/VersionTests.cs`: `GET /api/version` 200 with non-empty `version`, anonymous
- [X] T036 [P] [US1] Integration test `tests/QueryAI.Api.IntegrationTests/Operations/MigrationTests.cs`: starting the host twice against the same DB is idempotent (no error, `__EFMigrationsHistory` unchanged, existing rows preserved); two hosts started concurrently both succeed
- [X] T037 [P] [US1] Integration test `tests/QueryAI.Api.IntegrationTests/Operations/ConfigurationValidationTests.cs`: host startup fails with `OptionsValidationException` mentioning `QUERYAI_ADMIN_PASSWORD` when it is empty or `change-me`, and mentioning `ConnectionStrings`/`POSTGRES_PASSWORD` when DB password missing

### Implementation for User Story 1

- [X] T038 [P] [US1] Create options in `src/QueryAI.Api/Options/`: `DatabaseOptions.cs` (host, port, db, user, password from `POSTGRES_*`, builds connection string), `InitialAdminOptions.cs` (`QUERYAI_ADMIN_EMAIL` required + e-mail format, `QUERYAI_ADMIN_PASSWORD` required + `PasswordPolicy`, `QUERYAI_ADMIN_NAME` default `Administrador`), `SecurityOptions.cs` (`QUERYAI_TRUSTED_PROXIES`, `QUERYAI_SESSION_IDLE_HOURS` default 8, `QUERYAI_ALLOW_INSECURE_HTTP` default false), `ApiDocsOptions.cs` (`QUERYAI_ENABLE_API_DOCS`); register all with `ValidateOnStart()` and the placeholder validator from T025 in `src/QueryAI.Api/Options/OptionsRegistration.cs`; map env vars per contracts/environment.md
- [X] T039 [US1] Create `src/QueryAI.Infrastructure/Startup/DatabaseMigrator.cs` (`IHostedService`, runs first): retries connection with exponential backoff up to 60 s, then `Database.MigrateAsync(ct)` (EF Core migration lock), logs applied migrations; fails host startup with clear message if DB unreachable after 60 s
- [X] T040 [US1] Register health checks in `src/QueryAI.Api/Operations/HealthEndpoints.cs`: `/health/live` (no checks, tag `live`) and `/health/ready` (`AddDbContextCheck<AppDbContext>("database")`, tag `ready`) with JSON writer `{ status, checks:[{name,status}] }` per contracts/openapi.yaml `Health`
- [X] T041 [P] [US1] Create `src/QueryAI.Api/Endpoints/VersionEndpoints.cs`: `GET /api/version` returning `AssemblyInformationalVersionAttribute` value; set `InformationalVersion` from MSBuild property `Version` in `src/QueryAI.Api/QueryAI.Api.csproj`
- [X] T042 [US1] Configure forwarded headers in `src/QueryAI.Api/Security/ForwardedHeadersSetup.cs`: `X-Forwarded-For` and `X-Forwarded-Proto`, `KnownNetworks/KnownProxies` only from `QUERYAI_TRUSTED_PROXIES` (default `172.16.0.0/12` compose network), clear defaults; wire into `Program.cs` before auth
- [X] T043 [US1] Configure OpenAPI in `src/QueryAI.Api/Operations/ApiDocsSetup.cs`: `AddOpenApi()`, map `/openapi/v1.json` and Scalar at `/scalar` only in Development or when `QUERYAI_ENABLE_API_DOCS=true`
- [X] T044 [US1] Create `src/QueryAI.Api/Dockerfile`: multi-stage `mcr.microsoft.com/dotnet/sdk:10.0` (restore with CPM, `dotnet publish -c Release -p:Version=$VERSION`) → `mcr.microsoft.com/dotnet/aspnet:10.0`, install `curl` only, non-root `app` user, `ASPNETCORE_HTTP_PORTS=8080`, `HEALTHCHECK` via `curl -fsS http://localhost:8080/health/ready`, build-arg `VERSION`
- [X] T045 [P] [US1] Create `web/nginx.conf`: listen 8080, serve `/usr/share/nginx/html` with SPA fallback (`try_files $uri /index.html`), long cache for hashed assets and `no-cache` for `index.html`, proxy `/api/` and `/health/` to `http://api:8080` forwarding `Host`, `X-Forwarded-For` and `X-Forwarded-Proto` (preserve incoming `X-Forwarded-Proto` from customer proxy via `map`), security headers (`X-Content-Type-Options`, `Referrer-Policy`, `X-Frame-Options DENY`, basic CSP `default-src 'self'`)
- [X] T046 [P] [US1] Create `web/Dockerfile`: `node:24-alpine` build (`npm ci`, `npm run build` with `VITE_APP_VERSION` build-arg) → `nginxinc/nginx-unprivileged:alpine` serving `dist/` with `web/nginx.conf`, `HEALTHCHECK` on `/`
- [X] T047 [US1] Create `docker-compose.yml` at repo root: services `db` (`postgres:17-alpine`, volume `queryai-db-data`, `pg_isready` healthcheck, no published port), `api` (build `src/QueryAI.Api/Dockerfile`, image `queryai/api:${QUERYAI_VERSION:-latest}`, env from `.env` using `${VAR:?mensagem}` for `POSTGRES_PASSWORD`, `QUERYAI_ADMIN_EMAIL`, `QUERYAI_ADMIN_PASSWORD`, `depends_on: db (service_healthy)`, no published port), `web` (build `web/Dockerfile`, image `queryai/web:${QUERYAI_VERSION:-latest}`, publishes `${QUERYAI_HTTP_PORT:-8080}:8080`, `depends_on: api (service_healthy)`); `restart: unless-stopped`; named network
- [X] T048 [P] [US1] Create `.env.example` at repo root documenting every variable in contracts/environment.md (required ones with value `change-me`, reserved future ones commented out)
- [X] T049 [P] [US1] Create minimal shell page `web/src/pages/Home/HomePage.jsx` showing app name, `import.meta.env.VITE_APP_VERSION` and API version from `GET /api/version` (via TanStack Query), used as the router index route in `web/src/app/router.jsx` (replaced by the authenticated layout in US2)
- [X] T050 [US1] Write operator guide `docs/installation.md`: prerequisites, `.env` setup, `docker compose up -d`, verifying health/version, where data lives (volume `queryai-db-data`) and backup/restore with `pg_dump`/`pg_restore` via `docker compose exec db`, upgrading (pull new tag, `up -d`, automatic migrations), HTTPS requirement and pointer to `deploy/examples/`

**Checkpoint**: User Story 1 fully functional — instance starts with one command, migrates, reports health and version

---

## Phase 4: User Story 2 - Primeiro acesso do administrador e navegação pela aplicação (Priority: P2)

**Goal**: admin inicial criado a partir do `.env`, login com cookie seguro, troca obrigatória de senha, lockout, logout, todas as rotas protegidas, layout com navegação e estados vazios.

**Independent Test**: instância recém-instalada → `/` redireciona para `/login` → login com admin do `.env` → forçado a `/trocar-senha` → após trocar, navega por Início/Conexões/Chat/Histórico/Configurações com estado vazio → sair → `/` exige login; 5 senhas erradas → aviso de bloqueio.

### Tests for User Story 2 ⚠️

- [X] T051 [P] [US2] Integration test `tests/QueryAI.Api.IntegrationTests/Auth/InitialAdminSeederTests.cs`: first start creates Admin with `MustChangePassword=true` and `InitialAdminCreated` audit event; restart does not duplicate nor overwrite password; changed `QUERYAI_ADMIN_EMAIL` creates a second admin and leaves the first unchanged; two concurrent hosts create exactly one user
- [X] T052 [P] [US2] Integration test `tests/QueryAI.Api.IntegrationTests/Auth/LoginTests.cs`: valid login → 200 `CurrentUser` + `Set-Cookie queryai.session` HttpOnly SameSite=Strict; wrong password and unknown e-mail → identical 401 `invalid_credentials`; deactivated user → 401; 5 failures → 423 `account_locked` with `Retry-After`; rate limit per IP → 429 `rate_limited`; missing `X-Requested-With` → 400; with `QUERYAI_ALLOW_INSECURE_HTTP=false` and plain HTTP → 400 `https_required`
- [X] T053 [P] [US2] Integration test `tests/QueryAI.Api.IntegrationTests/Auth/SessionTests.cs`: `/api/auth/me` 401 without cookie; with `MustChangePassword` any protected route → 403 `password_change_required` except `me`, `change-password`, `logout`; change-password (wrong current → 401 `invalid_current_password`, same password → 400 `same_password`, short → 400) then routes accessible; logout → subsequent `me` 401; after `SecurityStamp` changes in DB, old cookie → 401
- [X] T054 [P] [US2] Integration test `tests/QueryAI.Api.IntegrationTests/Auth/AuthorizationSweepTests.cs` (SC-004): enumerate all endpoints from `EndpointDataSource`; every endpoint except the allow-list (`/health/live`, `/health/ready`, `/api/version`, `/api/auth/login`, `/openapi/*`, `/scalar/*`) returns 401 anonymously
- [X] T055 [P] [US2] Unit tests `tests/QueryAI.Application.Tests/Auth/LoginHandlerTests.cs` and `ChangePasswordHandlerTests.cs` (NSubstitute fakes): lockout flow, rehash on `SuccessRehashNeeded`, audit events written for success/failure/lockout/password change
- [X] T056 [P] [US2] Frontend tests `web/src/pages/Login/LoginPage.test.jsx` (messages for `invalid_credentials`, `account_locked`, `https_required`) and `web/src/app/guards.test.jsx` (unauthenticated → `/login?redirect=`, `mustChangePassword` → `/trocar-senha`, Analyst on `/configuracoes` → Forbidden)

### Implementation for User Story 2

- [X] T057 [P] [US2] Create `src/QueryAI.Infrastructure/Security/PasswordHasherAdapter.cs` implementing `IPasswordHasher` with `Microsoft.AspNetCore.Identity.PasswordHasher<User>` (V3 / PBKDF2-HMAC-SHA512)
- [X] T058 [P] [US2] Create `src/QueryAI.Infrastructure/Security/SecurityAuditWriter.cs` implementing `ISecurityAuditWriter`: adds `SecurityAuditEvent` to the DbContext (persisted with the same unit of work) and logs `Information` "Security event {EventType}" with actor/target ids (FR-020, never passwords)
- [X] T059 [P] [US2] Create `src/QueryAI.Api/Security/HttpCurrentUser.cs` implementing `ICurrentUser` from `HttpContext` (claim `sub`, `RemoteIpAddress`, `User-Agent`)
- [X] T060 [US2] Create `src/QueryAI.Application/Setup/EnsureInitialAdmin.cs` handler: if no user with normalized e-mail, validate `PasswordPolicy` (failure → throw to abort startup), create Admin with `MustChangePassword=true`, write `InitialAdminCreated`; treat unique-violation (`DbUpdateException` with Postgres `23505` translated by repository to a `Conflict` result) as "already exists"
- [X] T061 [US2] Create `src/QueryAI.Infrastructure/Startup/InitialAdminSeeder.cs` (`IHostedService` registered after `DatabaseMigrator`) invoking `EnsureInitialAdmin` with `InitialAdminOptions`
- [X] T062 [P] [US2] Create `src/QueryAI.Application/Auth/Login/LoginCommand.cs`, `LoginCommandValidator.cs` (email required max 254, password required max 128) and `LoginHandler.cs`: unknown/inactive → `invalid_credentials` + `LoginFailed` audit; locked → `account_locked` with remaining seconds; wrong password → `RegisterFailedLogin` (+ `AccountLockedOut` audit when locking); success → `RegisterSuccessfulLogin`, rehash if needed, `LoginSucceeded` audit; returns `CurrentUserDto` + `SecurityStamp`
- [X] T063 [P] [US2] Create `src/QueryAI.Application/Auth/ChangePassword/` (command, validator: newPassword 12–128, handler: verify current → `invalid_current_password`, equal → `same_password`, `User.ChangePassword`, `PasswordChanged` audit) and `src/QueryAI.Application/Auth/GetCurrentUser/` (query + handler returning `CurrentUserDto { Id, Name, Email, Role, MustChangePassword }`)
- [X] T064 [US2] Configure cookie authentication in `src/QueryAI.Api/Security/AuthenticationSetup.cs`: cookie `queryai.session`, `HttpOnly`, `SameSite=Strict`, `SecurePolicy=Always` unless `QUERYAI_ALLOW_INSECURE_HTTP` or Development, `ExpireTimeSpan = QUERYAI_SESSION_IDLE_HOURS`, `SlidingExpiration=true`, return 401/403 ProblemDetails instead of redirects; claims `sub`, `role`, `stamp`, `mcp` (must change password); `OnValidatePrincipal` loads user and rejects (+ sign-out) if missing, inactive or stamp differs, refreshing `mcp`/`role` claims; log startup warning when insecure HTTP is allowed
- [X] T065 [US2] Create authorization in `src/QueryAI.Api/Security/AuthorizationSetup.cs`: fallback policy requires authenticated user; policy `AdminOnly` (role Admin); endpoint filter `src/QueryAI.Api/Security/PasswordChangeRequiredFilter.cs` returning 403 `password_change_required` when claim `mcp=true`, applied to all protected groups except `/api/auth/me`, `/api/auth/change-password`, `/api/auth/logout`
- [X] T066 [P] [US2] Create `src/QueryAI.Api/Security/CsrfHeaderMiddleware.cs`: for POST/PUT/PATCH/DELETE under `/api/`, require `X-Requested-With: QueryAI` else 400 ProblemDetails `code=validation_failed`
- [X] T067 [P] [US2] Configure rate limiting in `src/QueryAI.Api/Security/RateLimitingSetup.cs`: policy `login` fixed window per client IP (10 requests/minute), rejection → 429 ProblemDetails `rate_limited` with `Retry-After`
- [X] T068 [US2] Create `src/QueryAI.Api/Endpoints/AuthEndpoints.cs` per contracts/openapi.yaml: `POST /api/auth/login` (anonymous, rate limit `login`, returns 400 `https_required` when `!Request.IsHttps` and insecure HTTP not allowed; on success `SignInAsync` with claims), `POST /api/auth/logout` (sign-out + `Logout` audit, 204), `GET /api/auth/me`, `POST /api/auth/change-password` (204 and re-issue cookie with new stamp and `mcp=false`); wire setup from T064–T067 and the endpoints into `Program.cs` in order: forwarded headers → exception handler → Serilog request logging → rate limiter → CSRF → authentication → authorization
- [X] T069 [P] [US2] Create `web/src/api/auth.js` (`login`, `logout`, `getMe`, `changePassword`) and `web/src/app/AuthProvider.jsx` (TanStack Query `['me']`; exposes `user`, `isLoading`, `login`, `logout`, `refresh`; on 401 from any query clears `['me']`)
- [X] T070 [US2] Create route guards in `web/src/app/guards.jsx`: `RequireAuth` (redirect `/login?redirect=<path>`), `RequirePasswordChanged` (redirect `/trocar-senha`), `RequireAdmin` (render `ForbiddenPage`)
- [X] T071 [P] [US2] Create `web/src/pages/Login/LoginPage.jsx` (+ `.module.css`): e-mail/senha, loading state, pt-BR messages: `invalid_credentials` "E-mail ou senha inválidos.", `account_locked` "Conta bloqueada temporariamente. Tente novamente em N minutos.", `rate_limited`, `https_required` "Esta instância exige HTTPS. Acesse pelo endereço seguro."; redirect to `redirect` param after success
- [X] T072 [P] [US2] Create `web/src/pages/ChangePassword/ChangePasswordPage.jsx` (+ `.module.css`): senha atual, nova, confirmação; client-side rule ≥ 12 chars and match; server errors `invalid_current_password`, `same_password`; on success refresh `me` and go to `/`
- [X] T073 [US2] Create layout `web/src/components/Layout/AppLayout.jsx`, `Sidebar.jsx`, `UserMenu.jsx` (+ `.module.css`): sidebar with Início, Conexões, Chat, Histórico, Configurações (only for Admin) using lucide icons, collapsible into hamburger below 768 px and usable from 360 px, footer with web and API versions, user menu (Trocar senha, Sair)
- [X] T074 [P] [US2] Create empty-state pages `web/src/pages/Home/HomePage.jsx` (replace US1 shell: "Suas consultas recentes aparecerão aqui"), `web/src/pages/Connections/ConnectionsPage.jsx`, `web/src/pages/Chat/ChatPage.jsx`, `web/src/pages/History/HistoryPage.jsx`, `web/src/pages/Settings/SettingsPage.jsx` (tabs shell, empty until US3), `web/src/pages/NotFound/NotFoundPage.jsx`, `web/src/pages/Forbidden/ForbiddenPage.jsx`, all using `EmptyState`
- [X] T075 [US2] Update `web/src/app/router.jsx` with the routes in contracts/ui-routes.md: public `/login`; `RequireAuth` → `/trocar-senha`; `RequireAuth`+`RequirePasswordChanged`+`AppLayout` → `/`, `/conexoes`, `/chat`, `/historico`, `RequireAdmin` → `/configuracoes/*`; `*` → NotFound

**Checkpoint**: User Stories 1 and 2 work — secure first access and full navigation

---

## Phase 5: User Story 3 - Administrador gerencia usuários e papéis (Priority: P3)

**Goal**: admin cria, edita (nome/papel), redefine senha, desativa e reativa usuários; analistas não acessam funções administrativas; nunca fica sem admin ativo.

**Independent Test**: como admin, criar analista → analista entra, troca senha, não vê Configurações e recebe 403 em `/api/users` → admin desativa analista → próxima ação do analista volta ao login → desativar o único admin retorna `last_admin`.

### Tests for User Story 3 ⚠️

- [X] T076 [P] [US3] Integration test `tests/QueryAI.Api.IntegrationTests/Users/UserManagementTests.cs`: create (201 + `Location`, new user must change password), duplicate e-mail case-insensitive → 409 `email_already_exists`, validation (name < 2, bad e-mail, password < 12) → 400, list with `search`/`page`/`pageSize` (max 100), get 404, update name/role, reset-password unlocks and forces change, deactivate → that user's existing cookie gets 401 on next request, activate; audit events recorded for each operation
- [X] T077 [P] [US3] Integration test `tests/QueryAI.Api.IntegrationTests/Users/LastAdminTests.cs`: deactivating or demoting the only active admin → 409 `last_admin`; with two admins, concurrent deactivation of each other → exactly one succeeds
- [X] T078 [P] [US3] Integration test `tests/QueryAI.Api.IntegrationTests/Users/AdminAuthorizationTests.cs` (SC-004): Analyst receives 403 on every `/api/users*` endpoint (enumerated from `EndpointDataSource` metadata `AdminOnly`)
- [X] T079 [P] [US3] Unit tests `tests/QueryAI.Application.Tests/Users/CreateUserHandlerTests.cs` and `DeactivateUserHandlerTests.cs` (validation, duplicate e-mail, last-admin guard, audit events)
- [X] T080 [P] [US3] Frontend test `web/src/pages/Settings/Users/UsersPage.test.jsx`: lists users, create modal validation, shows `email_already_exists` and `last_admin` messages

### Implementation for User Story 3

- [X] T081 [P] [US3] Create `src/QueryAI.Application/Users/CreateUser/` (command, validator: name "2–120 caracteres após trim", email valid max 254, role in `Admin|Analyst`, temporaryPassword 12–128; handler: duplicate normalized e-mail → `email_already_exists`, create with `MustChangePassword=true`, `UserCreated` audit) and `src/QueryAI.Application/Users/UserDto.cs` (`Id, Name, Email, Role, IsActive, IsLockedOut, MustChangePassword, CreatedAtUtc, LastLoginAtUtc`)
- [X] T082 [P] [US3] Create `src/QueryAI.Application/Users/ListUsers/` (query `search` max 100, `page` ≥ 1, `pageSize` 1–100 default 20; search on name/e-mail; ordered by name) and `src/QueryAI.Application/Users/GetUser/`
- [X] T083 [US3] Create `src/QueryAI.Application/Users/UpdateUser/` (name + role; demoting an Admin runs inside `IUnitOfWork.ExecuteInTransactionAsync` with `CountOtherActiveAdminsForUpdateAsync` → `last_admin` if 0; `UserUpdated` and, when role changes, `UserRoleChanged` audit with old/new role in `Details`; concurrency conflict → `concurrency_conflict`)
- [X] T084 [US3] Create `src/QueryAI.Application/Users/DeactivateUser/` (same transactional last-admin guard for active admins, `UserDeactivated` audit) and `src/QueryAI.Application/Users/ActivateUser/` (`UserActivated` audit)
- [X] T085 [P] [US3] Create `src/QueryAI.Application/Users/ResetPassword/` (validator 12–128; `User.ResetPassword`; `PasswordReset` audit)
- [X] T086 [US3] Create `src/QueryAI.Api/Endpoints/UserEndpoints.cs`: group `/api/users` with `RequireAuthorization("AdminOnly")` and `PasswordChangeRequiredFilter`; `GET /`, `POST /` (201 + Location), `GET /{id}`, `PUT /{id}`, `POST /{id}/reset-password`, `POST /{id}/deactivate`, `POST /{id}/activate` per contracts/openapi.yaml; register in `Program.cs`
- [X] T087 [P] [US3] Create `web/src/api/users.js` (`listUsers`, `getUser`, `createUser`, `updateUser`, `resetPassword`, `deactivateUser`, `activateUser`) and base components `web/src/components/Table.jsx`, `web/src/components/Modal.jsx`, `web/src/components/Badge.jsx` (+ `.module.css`)
- [X] T088 [US3] Create `web/src/pages/Settings/Users/UsersPage.jsx` (+ `.module.css`): table (nome, e-mail, papel, situação, bloqueado, último acesso), search with debounce, pagination, actions; `UserFormModal.jsx` (create/edit), `ResetPasswordModal.jsx`, confirm dialog for deactivate; pt-BR messages for `email_already_exists` "Já existe um usuário com este e-mail.", `last_admin` "A instância precisa de pelo menos um administrador ativo."; invalidate `['users']` queries after mutations
- [X] T089 [US3] Add the Usuários tab to `web/src/pages/Settings/SettingsPage.jsx` and route `/configuracoes/usuarios` (default redirect from `/configuracoes`) in `web/src/app/router.jsx`

**Checkpoint**: All user stories independently functional

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [X] T090 [P] Integration test `tests/QueryAI.Api.IntegrationTests/Security/SecretLeakTests.cs` (SC-007): run login, change-password, create user and reset-password flows, then assert captured logs and all response bodies contain none of the passwords used, password hashes, nor the DB password
- [X] T091 [P] Integration test `tests/QueryAI.Api.IntegrationTests/Operations/ProblemDetailsTests.cs`: unhandled exception endpoint (Testing env only) → 500 with `traceId`, `code=unexpected_error`, no stack trace
- [X] T092 [P] Integration test `tests/QueryAI.Api.IntegrationTests/Operations/OpenApiContractTests.cs`: generated `/openapi/v1.json` contains every path/method in `specs/001-project-foundation/contracts/openapi.yaml`
- [X] T093 [P] Create HTTPS proxy examples `deploy/examples/Caddyfile` and `deploy/examples/nginx-https.conf` (TLS termination, `X-Forwarded-Proto https`, proxy to `web:8080`) and reference them in `docs/installation.md`
- [X] T094 [P] Create `README.md` at repo root (overview, architecture diagram of layers/containers, dev setup: `dotnet run --project src/QueryAI.Api` + `npm run dev` in `web/`, running tests, link to `docs/installation.md` and specs)
- [X] T095 [P] Add `docker compose` profiles placeholders comment for `observability` and `cache` in `docker-compose.yml` and a `docker-compose.override.example.yml` for local development (exposes db port, `QUERYAI_ALLOW_INSECURE_HTTP=true`, `ASPNETCORE_ENVIRONMENT=Development`)
- [X] T096 Accessibility and responsiveness pass on `web/src/components/Layout/` and pages: keyboard navigation, focus visible, labels, color contrast AA, layout at 360 px
- [X] T097 Security review of auth, session, CSRF, forwarded headers and user management changes (constitution quality gate) — record findings in `specs/001-project-foundation/security-review.md`
- [ ] T098 Run full validation: `dotnet build` (zero warnings), `dotnet test QueryAI.slnx`, `npm run lint && npm test` in `web/`, then execute every scenario in `specs/001-project-foundation/quickstart.md`

---

### Implementation notes

- T060/T061: `InitialAdminSettings` lives in `src/QueryAI.Application/Setup/` (not in Api options) so the Infrastructure seeder can consume it without depending on the Api layer.
- T065: the opt-in `PasswordChangeRequiredFilter` was replaced during the security review (T097) by `PasswordChangedRequirement` in the default/fallback/AdminOnly policies (secure by default); `me`, `change-password` and `logout` use policy `AllowPendingPasswordChange`.
- Refactor (2026-11-17, constitution v2.2.0): command/query handlers and the validation decorator were replaced by `AuthService`, `UserService` and `InitialAdminService`, called directly by the endpoints; every source file now holds exactly one type. Both rules are enforced by `tests/QueryAI.ArchitectureTests` (`DirectCallTests`, `OneTypePerFileTests`).
- Folder refactor (2026-11-17, constitution v2.3.0): files moved into per-context type folders (`Services/`, `Requests/`, `Validations/`, `Dtos/`, `Entities/`, ...); namespaces follow folders; enforced by `FolderStructureTests`.
- Data Protection keys are persisted in the internal DB (`data_protection_keys`) so sessions survive restarts and work across replicas.
- Integration tests (T026, T034–T037, T051–T054, T076–T078, T090–T092) are written and compile but were **not executed** in the implementation environment (no Docker access); T098 covers running them.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: no dependencies
- **Foundational (Phase 2)**: depends on Setup — BLOCKS all user stories
- **US1 (Phase 3)**: depends on Foundational
- **US2 (Phase 4)**: depends on Foundational; uses `DatabaseMigrator`/options from US1 at runtime (T038, T039) — implement after US1 or pull those two tasks forward
- **US3 (Phase 5)**: depends on Foundational and on US2 auth/authorization (T064, T065, T068), since every user endpoint is protected
- **Polish (Phase 6)**: after the desired stories

### Within Each User Story

- Tests first (must fail) → Application handlers → Infrastructure adapters → API endpoints → Frontend API module → Pages → Router wiring
- `Program.cs` edits (T068, T086) are sequential (same file)
- `web/src/app/router.jsx` edits (T032 → T049 → T075 → T089) are sequential

### Parallel Opportunities

- Setup: T006, T007, T008 in parallel after T001–T005
- Foundational: T009–T011, T013–T016 in parallel; T023–T028 in parallel; frontend T029–T031, T033 in parallel with backend work
- US1: tests T034–T037 together; T038, T041, T045, T046, T048, T049 in parallel
- US2: tests T051–T056 together; T057–T059, T062, T063, T066, T067 in parallel; frontend T069, T071, T072, T074 in parallel with backend
- US3: tests T076–T080 together; T081, T082, T085, T087 in parallel
- Polish: T090–T095 in parallel

---

## Parallel Example: User Story 2

```bash
# Tests together:
Task: "Integration test InitialAdminSeederTests in tests/QueryAI.Api.IntegrationTests/Auth/InitialAdminSeederTests.cs"
Task: "Integration test LoginTests in tests/QueryAI.Api.IntegrationTests/Auth/LoginTests.cs"
Task: "Integration test AuthorizationSweepTests in tests/QueryAI.Api.IntegrationTests/Auth/AuthorizationSweepTests.cs"

# Independent adapters/handlers together:
Task: "PasswordHasherAdapter in src/QueryAI.Infrastructure/Security/PasswordHasherAdapter.cs"
Task: "SecurityAuditWriter in src/QueryAI.Infrastructure/Security/SecurityAuditWriter.cs"
Task: "Login handler in src/QueryAI.Application/Auth/Login/"
Task: "LoginPage in web/src/pages/Login/LoginPage.jsx"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Phase 1 Setup → Phase 2 Foundational
2. Phase 3 (US1): instância sobe com um comando, migra, reporta saúde e versão
3. **STOP and VALIDATE**: seção 1 do quickstart.md

### Incremental Delivery

1. Setup + Foundational → base compilando e testada
2. US1 → instância instalável (MVP operacional)
3. US2 → acesso seguro e navegação (primeira versão utilizável por pessoas)
4. US3 → gestão de usuários (pronta para uso por equipes)
5. Polish → revisão de segurança, documentação, validação completa

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Verify tests fail before implementing
- Commit after each task or logical group (Conventional Commits)
- Every async method takes and forwards a `CancellationToken` (Princípio VII)
- Never log or return passwords, hashes or secrets (Princípio VI)
