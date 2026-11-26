# Research: Base do Projeto (001-project-foundation)

Decisões técnicas da Fase 0. Cada item: **Decision**, **Rationale**, **Alternatives considered**.

## R1. Estrutura da solução backend

- **Decision**: Solução `QueryAI.slnx` com quatro projetos em `src/` (`QueryAI.Domain`,
  `QueryAI.Application`, `QueryAI.Infrastructure`, `QueryAI.Api`) e projetos de teste em `tests/`.
  `Directory.Build.props` (net10.0, `Nullable`, `ImplicitUsings`, `TreatWarningsAsErrors`,
  analyzers) e `Directory.Packages.props` (Central Package Management).
- **Rationale**: atende ao Princípio III; o versionamento central de pacotes evita divergência de
  versões entre camadas; `.slnx` é o formato padrão do .NET 10.
- **Alternatives considered**: projeto único com pastas (viola III); um projeto por feature
  (vertical slices em projetos separados), que é complexo demais para o tamanho atual (X).

## R2. Estilo da API HTTP

- **Decision**: Minimal APIs agrupadas por módulo (`MapGroup("/api/users")`) em classes de
  endpoint dentro de `QueryAI.Api/Endpoints`, chamando diretamente os serviços de caso de uso em
  `Application`. OpenAPI nativo (`Microsoft.AspNetCore.OpenApi`) com UI Scalar em desenvolvimento
  e opcional em produção.
- **Rationale**: endpoints finos, pouca cerimônia e geração de OpenAPI nativa no .NET 10.
- **Alternatives considered**: Controllers MVC (mais cerimônia, sem ganho aqui); Swashbuckle
  (dispensável com OpenAPI nativo).

## R3. Orquestração de casos de uso

- **Decision** (revisada em 2026-11-17, constituição v2.2.0): serviços da Application
  (`AuthService`, `UserService`, `InitialAdminService`) chamados diretamente pelos endpoints. Cada
  método valida a entrada explicitamente com o validador FluentValidation correspondente.
- **Rationale**: fluxo explícito e simples de seguir; sem reflexão, sem despacho genérico.
- **Alternatives considered**: Command/Query + Handler com decorator de validação (adotado
  inicialmente e removido por adicionar indireção sem ganho); MediatR (licença comercial desde
  2025 e indireção desnecessária).

## R4. Erros e resultados

- **Decision**: Erros de negócio esperados são retornados como `Result<T>` com `Error`
  (código + mensagem); a camada Api os traduz para `ProblemDetails` (RFC 9457) com status
  apropriado. Exceções inesperadas são capturadas por um `IExceptionHandler` global, que gera
  `ProblemDetails` 500 com `traceId` e nunca expõe detalhes internos (FR-007).
- **Rationale**: fluxo de controle explícito para erros previsíveis; um único ponto de
  tradução HTTP.
- **Alternatives considered**: exceções de domínio para tudo (fluxo implícito, custo maior).

## R5. Autenticação e sessão

- **Decision**: Autenticação por **cookie** do ASP.NET Core (`HttpOnly`, `Secure` fora de
  Development, `SameSite=Strict`, expiração deslizante de 8 h). A entidade `User` é do domínio
  (não `IdentityUser`). O hash de senha usa `PasswordHasher<User>` de
  `Microsoft.Extensions.Identity.Core` (PBKDF2-HMAC-SHA512, salt, iterações altas, com rehash
  automático). Cada requisição valida o `SecurityStamp` e o `IsActive` do usuário
  (`OnValidatePrincipal`), consultando o banco, de modo que desativação, troca de papel ou de
  senha invalidam a sessão na requisição seguinte.
- **Rationale**: SPA e API no mesmo domínio (via proxy do container web), então o cookie
  `HttpOnly` evita expor o token a scripts (FR-014a). Lógica de lockout e políticas sob controle
  do domínio. O custo de uma consulta por requisição é desprezível para o volume single-tenant.
- **Alternatives considered**: JWT no `localStorage` (exposto a XSS, revogação difícil);
  ASP.NET Core Identity completo (acopla o Domain ao `IdentityUser` e a tabelas próprias; traz
  mais do que é necessário); OIDC/IdP externo (fora do escopo, conforme a spec).

## R6. Proteção CSRF

- **Decision**: `SameSite=Strict` + exigência do header `X-Requested-With: QueryAI` em métodos
  que alteram estado (verificado por middleware); a API só aceita `application/json`.
- **Rationale**: requisições cross-site não conseguem enviar headers customizados sem preflight
  CORS, e o CORS não é habilitado (mesma origem).
- **Alternatives considered**: tokens antiforgery do ASP.NET (mais acoplados a forms/MVC).

## R7. Proteção contra força bruta

- **Decision**: Lockout por conta (5 falhas consecutivas → 15 min, FR-013) no domínio + rate
  limiter nativo (`AddRateLimiter`, janela fixa por IP) no endpoint de login. Conta bloqueada
  responde `423` com `Retry-After`; credenciais inválidas e e-mail inexistente respondem `401`
  com a mesma mensagem.
- **Rationale**: defesa por conta e por origem. Revelar o bloqueio (423) é uma troca aceitável
  em instância interna: melhora o suporte e expõe pouco, já que contas inexistentes nunca
  entram em bloqueio.
- **Alternatives considered**: `401` genérico para tudo (o usuário não entende o bloqueio);
  CAPTCHA (dependência externa, contraria o Princípio VI).

## R8. Persistência e migrations

- **Decision**: EF Core 10 + Npgsql, PostgreSQL 17 incluído no compose, convenção snake_case
  (`EFCore.NamingConventions`), IDs `Guid` v7 (`Guid.CreateVersion7()`), concorrência otimista via
  `xmin`. Migrations aplicadas na inicialização com `Database.MigrateAsync()`, que desde o EF Core
  9 adquire lock exclusivo no banco e é seguro com réplicas simultâneas. Retry de conexão com
  backoff por até 60 s enquanto o banco sobe.
- **Rationale**: FR-003 (idempotente, sem passo manual) e o caso de borda das réplicas
  simultâneas; o banco incluído no pacote foi definido no clarify.
- **Alternatives considered**: bundle de migrations executado por um container separado (mais
  uma peça para o operador); scripts SQL manuais (sujeitos a erro).

## R9. Admin inicial (seed)

- **Decision**: `InitialAdminSeeder` executado na inicialização, depois das migrations. Lê
  `QUERYAI_ADMIN_EMAIL`, `QUERYAI_ADMIN_PASSWORD` e `QUERYAI_ADMIN_NAME`. Se não existir usuário
  com o e-mail normalizado, cria um Administrador com `MustChangePassword = true`. O índice único
  em `normalized_email` garante que corridas entre réplicas não criem duplicatas: a violação de
  unicidade é capturada e tratada como "já existe". Senha fora da política (FR-012) → falha de
  inicialização.
- **Rationale**: exatamente o que foi definido no clarify (Q4).
- **Alternatives considered**: seed via `HasData` em migration (senha ficaria na migration);
  tela de setup (rejeitada no clarify).

## R10. Configuração e validação na inicialização

- **Decision**: Options fortemente tipadas (`DatabaseOptions`, `InitialAdminOptions`,
  `ForwardedHeadersSettings`, `AuthOptions`) com `ValidateOnStart()` e validadores customizados
  que rejeitam valores vazios ou de exemplo (`change-me`, `changeme`, `example`). Variáveis de
  ambiente com prefixo `QUERYAI_`. O compose usa a sintaxe `${VAR:?mensagem}` para falhar cedo.
- **Rationale**: FR-002 com mensagem clara já no primeiro segundo.
- **Alternatives considered**: validar apenas no uso (falha tardia e confusa).

## R11. Proxy reverso, HTTPS e topologia de rede

- **Decision**: O container `web` (nginx) serve o build estático do React e faz proxy de
  `/api/*` e `/health/*` para o container `api` na rede interna; apenas o `web` publica porta. O proxy HTTPS do
  cliente fica na frente do `web`. O nginx repassa `X-Forwarded-Proto/For`, e a API usa
  `ForwardedHeaders` confiando somente nas redes de `QUERYAI_TRUSTED_PROXIES` (padrão: a sub-rede
  interna do compose). Fora de Development, cookie `Secure`; se a requisição de login chegar como
  HTTP, a API responde `400` com o código `https_required`, que a tela de login usa para exibir o
  aviso (FR-014a). Para avaliação local existe `QUERYAI_ALLOW_INSECURE_HTTP=true`, que desliga essa
  exigência e emite aviso no log a cada inicialização. O guia traz exemplos de Caddy e nginx com HTTPS.
- **Rationale**: decisão do clarify (Q2); mesma origem elimina o CORS; a API não fica exposta
  diretamente.
- **Alternatives considered**: API publicada em porta própria com CORS (mais superfície e
  configuração).

## R12. Health checks e versão

- **Decision**: `/health/live` (somente o processo) e `/health/ready` (inclui
  `AddDbContextCheck<AppDbContext>`), em JSON com o status de cada dependência. `GET /api/version`
  retorna a versão informacional injetada no build (`VERSION` build-arg → `InformationalVersion`).
  O frontend recebe `VITE_APP_VERSION` no build e exibe ambas no rodapé/menu.
- **Rationale**: FR-004, FR-005; healthchecks do compose usam `/health/ready`.
- **Alternatives considered**: pacotes Xabaril (dispensáveis nesta etapa).

## R13. Logging e auditoria

- **Decision**: Serilog com `RenderedCompactJsonFormatter` no stdout, `UseSerilogRequestLogging`
  e enriquecimento com `TraceId`, `UserId` e `RequestPath`. Um destructuring policy/filtro
  remove propriedades sensíveis (`Password`, `Token`, `Secret`, `ConnectionString`). Eventos de
  segurança são gravados na tabela `security_audit_events` pela `ISecurityAuditWriter` e também
  emitidos como log `Information` com `EventType`.
- **Rationale**: Princípio VIII; FR-006, FR-020 e Q3 do clarify. A stack Grafana/Loki fica
  opcional e futura.
- **Alternatives considered**: logging padrão do Microsoft.Extensions (sem destructuring
  policy).

## R14. Garantia de "pelo menos um admin ativo"

- **Decision**: O caso de uso que desativa ou rebaixa um usuário roda em transação com
  `SELECT ... FOR UPDATE` sobre os administradores ativos (`pg_advisory_xact_lock` dedicado como
  alternativa), conta os demais administradores ativos e rejeita com o erro `last_admin` se o
  resultado for zero.
- **Rationale**: FR-019 sob concorrência (dois admins desativando um ao outro ao mesmo tempo).
- **Alternatives considered**: apenas verificação em memória (sujeita a corrida).

## R15. Frontend

- **Decision**: React 19 + Vite, **JavaScript (ESM, JSX)**, React Router 7 (modo biblioteca),
  TanStack Query para estado de servidor, um cliente `fetch` próprio (`credentials: 'same-origin'`,
  header `X-Requested-With`, tradução de `ProblemDetails`), CSS Modules + variáveis CSS para o
  tema minimalista, sem biblioteca de UI. Validação de formulários com regras simples e
  mensagens em pt-BR. Ícones: `lucide-react`. ESLint (flat config) + Prettier. PropTypes não são usados e o código
  não tem comentários (regra em `CLAUDE.md`).
- **Rationale**: "React puro com JS" pedido pelo usuário; poucas dependências; o TanStack Query
  resolve cache e loading/erro de forma consistente (FR-023).
- **Alternatives considered**: Next.js (SSR desnecessário e incompatível com "React puro");
  Redux (excesso para o estado atual); Material UI/Ant Design (pesados, fogem do visual
  minimalista).

## R16. Testes

- **Decision**: Backend com xUnit v3 + Shouldly + NSubstitute. `QueryAI.Domain.Tests` e
  `QueryAI.Application.Tests` (unitários), `QueryAI.Api.IntegrationTests`
  (`WebApplicationFactory` + `Testcontainers.PostgreSql`, cobrindo auth, autorização, seed,
  migrations, health) e `QueryAI.ArchitectureTests` (NetArchTest.Rules, regras de dependência
  entre camadas). Frontend com Vitest + React Testing Library + jsdom. Um teste automatizado
  varre os logs da suíte de integração em busca de segredos conhecidos (SC-007).
- **Rationale**: Princípio V; FluentAssertions foi evitado por ter passado a licença comercial
  (v8).
- **Alternatives considered**: banco em memória/SQLite nos testes de integração (comportamento
  diferente do PostgreSQL, viola V).

## R17. Distribuição

- **Decision**: Dockerfile multi-stage para a API (`sdk:10.0` → `aspnet:10.0`, usuário não-root,
  `HEALTHCHECK` com `curl` instalado na imagem de runtime) e para o web (`node:24-alpine` → `nginx:alpine` não-root). `docker-compose.yml`
  na raiz com `db`, `api`, `web`, volume nomeado `queryai-db-data` e `.env.example` documentado.
  Imagens tagueadas com SemVer. Profiles reservados `observability` e `cache` para etapas futuras
  (FR-009).
- **Rationale**: FR-001/FR-001a e a regra de distribuição self-hosted da constituição.
- **Alternatives considered**: Helm/Kubernetes (fora do escopo inicial).
