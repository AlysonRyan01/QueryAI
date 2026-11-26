# Implementation Plan: Base do Projeto (Fundação da Instância QueryAI)

**Branch**: `001-project-foundation` | **Date**: 2026-11-16 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/001-project-foundation/spec.md`

## Summary

Criar a fundação self-hosted do QueryAI: solução .NET 10 em Clean Architecture
(`Domain`/`Application`/`Infrastructure`/`Api`), aplicação web em React com JavaScript puro, e
distribuição em Docker Compose (`db` PostgreSQL incluído, `api` e `web`/nginx). A instância
valida a configuração do `.env` na inicialização, aplica as migrations automaticamente com lock,
cria o administrador inicial se ele não existir e expõe health checks e versão. A autenticação é
por cookie de sessão `HttpOnly`/`Secure`, com lockout, troca obrigatória de senha, papéis
Admin/Analista e auditoria de segurança. A UI entrega o layout BI + chat com navegação e estados
vazios. Decisões detalhadas em [research.md](./research.md).

## Technical Context

**Language/Version**: C# 14 / .NET 10 (backend); JavaScript ES2023 + JSX, React 19 (frontend)

**Primary Dependencies**: ASP.NET Core (Minimal APIs, cookie auth, rate limiting, OpenAPI nativo
+ Scalar), EF Core 10 + Npgsql + EFCore.NamingConventions, Microsoft.Extensions.Identity.Core
(apenas o `PasswordHasher`), FluentValidation, Serilog; Vite, React Router 7, TanStack Query,
lucide-react

**Storage**: PostgreSQL 17 (banco interno incluído no compose, volume nomeado)

**Testing**: xUnit v3, Shouldly, NSubstitute, Testcontainers.PostgreSql, WebApplicationFactory,
NetArchTest.Rules; Vitest + React Testing Library

**Target Platform**: contêineres Linux (amd64; arm64 desejável), navegadores modernos (2 últimas
versões de Chrome, Edge, Firefox e Safari)

**Project Type**: aplicação web (API + SPA) distribuída como servidor self-hosted

**Performance Goals**: instância pronta em ≤ 3 min (SC-002); telas respondendo em < 2 s em rede
local (SC-006); login em < 1 s, p95

**Constraints**: single-tenant; sem chamadas externas; segredos apenas via `.env`; HTTPS feito
pelo proxy do cliente; containers sem root; nenhum segredo em logs

**Scale/Scope**: uma organização por instância, até ~200 usuários e ~50 sessões simultâneas;
8 telas; ~12 endpoints

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Princípio | Avaliação | Status |
|---|---|---|
| I. SQL seguro | Nenhuma execução em bancos externos nesta feature. O EF Core acessa apenas o banco interno | ✅ N/A |
| II. IA mediada por ferramentas | Sem IA nesta feature | ✅ N/A |
| III. Clean Architecture | 4 camadas com dependências para dentro, endpoints finos, `ProblemDetails` global, testes de arquitetura (R1–R4, R16) | ✅ |
| IV. Extensibilidade | As abstrações de provedor ainda não são necessárias; `Application` define interfaces (`IUserRepository`, `IPasswordHasher`, `IClock`, `ISecurityAuditWriter`) | ✅ |
| V. Testes | Unitários (domínio/aplicação), integração com PostgreSQL real (Testcontainers), arquitetura e frontend; sem dependências pagas | ✅ |
| VI. Self-hosted single-tenant | Sem `TenantId`; papéis Admin/Analista; segredos só pelo `.env` com validação na inicialização; sem telemetria externa; validação de entrada com FluentValidation | ✅ |
| VII. Async/cancelável | I/O 100% assíncrono com `CancellationToken` propagado de `HttpContext.RequestAborted`; retry de conexão na inicialização | ✅ |
| VIII. Observabilidade | Serilog JSON no stdout, correlação por `TraceId`/`UserId`, filtro de segredos; stack Grafana opcional (profile futuro) | ✅ (`ConversationId` só existe em features futuras) |
| IX. Custos de IA | Sem IA nesta feature | ✅ N/A |
| X. Incremental/simplicidade | Sem MediatR, sem Redis, sem Identity completo, sem UI kit; cada dependência justificada em research.md | ✅ |
| Distribuição self-hosted | Imagens versionadas, compose de referência, config via env, migrations automáticas e idempotentes, health checks, observabilidade opcional | ✅ |
| Quality gates | Nullable + `TreatWarningsAsErrors`; OpenAPI gerado; revisão de segurança exigida (auth/controle de acesso) | ✅ (revisão de segurança antes do merge) |

**Pós-design (Fase 1)**: reavaliado após data-model e contratos. Nenhuma violação: os contratos
não expõem `password_hash` nem segredos; a auditoria é somente inserção; a regra de "último
admin" é protegida contra concorrência (R14). A tabela de Complexity Tracking não é necessária.

## Project Structure

### Documentation (this feature)

```text
specs/001-project-foundation/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── openapi.yaml
│   ├── environment.md
│   └── ui-routes.md
├── checklists/requirements.md
└── tasks.md             # gerado por /speckit-tasks
```

### Source Code (repository root)

```text
QueryAI.slnx
Directory.Build.props
Directory.Packages.props
global.json
docker-compose.yml
.env.example
.editorconfig
.gitignore
.dockerignore

src/
├── QueryAI.Domain/
│   ├── Common/            # Entities/ Enums/ Errors/ Interfaces/ Results/
│   ├── Users/             # Entities/User, Enums/Role, Errors/UserErrors, Policies/PasswordPolicy
│   └── Auditing/          # Entities/SecurityAuditEvent, Enums/SecurityEventType
├── QueryAI.Application/
│   ├── Abstractions/
│   │   ├── Persistence/   # Interfaces/ Exceptions/ Models/
│   │   └── Security/      # Interfaces/ Enums/
│   ├── Common/            # Extensions/RequestValidation, Validations/ValidationRules
│   ├── Auth/              # Services/ Requests/ Validations/ Dtos/
│   ├── Users/             # Services/ Requests/ Validations/ Dtos/
│   ├── Setup/             # Services/ Settings/ Enums/
│   └── DependencyInjection.cs
├── QueryAI.Infrastructure/
│   ├── Common/            # Services/SystemClock
│   ├── Persistence/       # Contexts/ Configurations/ Repositories/ Transactions/ Migrations/
│   ├── Security/          # Services/PasswordHasherAdapter, SecurityAuditWriter
│   ├── Startup/           # HostedServices/DatabaseMigrator, InitialAdminSeeder
│   └── DependencyInjection.cs
└── QueryAI.Api/
    ├── Endpoints/ Responses/ Setup/ Options/ Validations/
    ├── Middlewares/ Handlers/ Requirements/ Extensions/ Services/ Policies/
    ├── Program.cs
    └── Dockerfile

tests/
├── QueryAI.Domain.Tests/
├── QueryAI.Application.Tests/
├── QueryAI.Api.IntegrationTests/   # Testcontainers PostgreSQL + WebApplicationFactory
└── QueryAI.ArchitectureTests/

web/
├── index.html
├── package.json
├── vite.config.js
├── eslint.config.js
├── nginx.conf               # serve SPA + proxy /api e /health → api
├── Dockerfile
└── src/
    ├── main.jsx
    ├── app/                 # App, router, QueryClient, AuthProvider, guards
    ├── api/                 # httpClient (fetch + ProblemDetails), auth.js, users.js
    ├── components/          # Layout, Sidebar, EmptyState, Button, Input, Modal, Table, ErrorMessage
    ├── pages/               # Login, ChangePassword, Home, Connections, Chat, History,
    │                        # Settings/Users, NotFound, Forbidden
    ├── styles/              # tokens.css (variáveis), global.css
    └── test/                # setup do Vitest

deploy/
└── examples/                # Caddyfile e nginx-https.conf (proxy HTTPS do cliente)

docs/
└── installation.md          # guia do operador: .env, subir, backup do volume, atualização
```

**Structure Decision**: aplicação web com backend em `src/` (Clean Architecture, quatro
projetos), frontend independente em `web/` e testes .NET em `tests/`. O compose e os arquivos
de build ficam na raiz, para que o operador use `docker compose up` diretamente. `deploy/` e
`docs/` atendem à distribuição self-hosted.

## Complexity Tracking

Nenhuma violação constitucional a justificar.
