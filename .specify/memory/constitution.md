<!--
Sync Impact Report
==================
Version change: 2.2.0 → 2.3.0
Bump rationale: MINOR — Principle III expanded: folder-per-artifact-type inside each context.

Changes in 2.3.0:
  - III: Domain/Application/Infrastructure contexts split files into standard type folders
    (Entities, Services, Requests, Validations, Dtos, ...); Api organized by type; namespaces
    follow folders.

History (2.2.0): MINOR — Principle III expanded with two new rules.
  Changes in 2.2.0:
  - III: use cases are Application services called directly by endpoints; Command/Query +
    Handler, mediators, in-process event dispatch and generic pipeline decorators are forbidden.
  - III: exactly one type per file (no nested types), including tests.

History (2.1.0): MINOR — Principle IV expanded: AI model must be selectable from the frontend.
  Changes in 2.1.0:
  - IV: OpenRouter model id is runtime configuration persisted in the app DB, managed via the
    frontend by admins (available models + instance default); backend validates the model
    (exists, supports tool calling + structured outputs); model used is recorded per query.

History (2.0.0): MAJOR — Principle VI redefined: deployment model changed from multi-tenant SaaS
to self-hosted single-tenant server run by each customer.

Changes in 2.0.0:
  - VI. Isolamento Multi-Tenant e Proteção de Segredos →
    VI. Implantação Self-Hosted Single-Tenant e Proteção de Segredos
    (no multi-tenancy; users/roles + per-connection permissions; operator-provided keys;
    no phone-home telemetry)
  - II: tool scope is now "current user + permitted connections" (was user/tenant)
  - VIII: correlation by UserId (was TenantId); observability stack optional per instance
  - IX: AI usage/quotas per user and per instance (was per tenant); customer's own provider key
  - Restrições: added "Distribuição self-hosted" (versioned images, reference compose,
    env-var config, automatic idempotent migrations, safe upgrades, health checks)
  - Quality gates: security review trigger "isolamento de tenant" → "controle de acesso"

History (1.0.0): first ratification; all template placeholders replaced.

Modified principles (placeholder → new title):
  - [PRINCIPLE_1_NAME] → I. Execução de SQL Segura por Padrão (INEGOCIÁVEL)
  - [PRINCIPLE_2_NAME] → II. IA Mediada por Ferramentas e Isolada de Credenciais (INEGOCIÁVEL)
  - [PRINCIPLE_3_NAME] → III. Clean Architecture e Baixo Acoplamento
  - [PRINCIPLE_4_NAME] → IV. Extensibilidade por Abstrações de Provedor
  - [PRINCIPLE_5_NAME] → V. Testes Automatizados como Critério de Pronto
Added principles:
  - VI. Implantação Self-Hosted Single-Tenant e Proteção de Segredos
  - VII. Operações Assíncronas, Canceláveis e Resilientes
  - VIII. Observabilidade Estruturada
  - IX. Controle de Custos e Limites de Uso da IA
  - X. Entrega Incremental e Simplicidade
Added sections:
  - Restrições de Tecnologia e Arquitetura (was [SECTION_2_NAME])
  - Fluxo de Desenvolvimento e Quality Gates (was [SECTION_3_NAME])
Removed sections: none

Templates reviewed (not modified by this command; they read the constitution at runtime):
  - .specify/templates/plan-template.md ("Constitution Check" gate) — ✅ compatible
  - .specify/templates/spec-template.md — ✅ compatible
  - .specify/templates/tasks-template.md — ✅ compatible (tests are mandatory per Principle V)

Deferred TODOs: none
-->

# QueryAI Constitution

## Core Principles

### I. Execução de SQL Segura por Padrão (INEGOCIÁVEL)

Toda consulta executada em um banco de dados externo MUST passar pelo pipeline de validação da
aplicação, sem exceções nem atalhos.

- Somente consultas de leitura são permitidas. Comandos DML/DDL/DCL e equivalentes (`INSERT`,
  `UPDATE`, `DELETE`, `MERGE`, `DROP`, `ALTER`, `CREATE`, `TRUNCATE`, `GRANT`, `REVOKE`, `EXEC`,
  `CALL`, `COPY`, `SELECT ... INTO`, múltiplos statements, entre outros) MUST ser rejeitados.
- A validação MUST ser feita por análise sintática (parser/AST específico do dialeto), não apenas
  por expressões regulares ou listas de palavras-chave.
- Defesa em profundidade: além da validação, as conexões externas MUST usar credenciais com
  permissão somente de leitura e, quando suportado, transação/sessão `READ ONLY`.
- Toda execução MUST ter timeout configurado (no comando e no banco) e limite máximo de linhas
  retornadas, aplicado pelo backend independentemente do SQL gerado.
- Acesso MUST ser restrito às tabelas e colunas autorizadas na conexão (allowlist/denylist);
  referências fora do escopo autorizado invalidam a consulta.
- Toda consulta executada MUST ser registrada (SQL final, usuário, conexão, duração, linhas,
  resultado da validação).

**Rationale**: o produto executa SQL gerado por um modelo não determinístico em bancos de clientes.
Um único comando destrutivo ou vazamento de dados compromete a confiança no produto inteiro.

### II. IA Mediada por Ferramentas e Isolada de Credenciais (INEGOCIÁVEL)

O modelo de linguagem é um componente não confiável e MUST interagir com o sistema exclusivamente
por ferramentas (tool calling) expostas pelo backend.

- O modelo MUST NOT receber strings de conexão, credenciais, hosts ou qualquer segredo.
- Ferramentas disponíveis ao agente (ex.: consultar metadados, validar SQL, executar consulta
  autorizada) MUST ter contratos tipados, entradas validadas e operar no escopo do usuário atual
  e das conexões que ele tem permissão para acessar.
- Saídas da IA consumidas pelo sistema (SQL proposto, tipo de gráfico, explicação) MUST usar
  Structured Outputs com schema validado; respostas fora do schema são tratadas como erro.
- O ciclo de autocorreção de consultas MUST ter número máximo de tentativas configurável.
- O SQL efetivamente executado MUST ser exibido ao usuário para transparência.
- Conteúdo vindo do banco do cliente (nomes, comentários, dados) MUST ser tratado como dado não
  confiável no prompt, mitigando prompt injection.

**Rationale**: isolar o modelo atrás de ferramentas validadas mantém a segurança independente da
qualidade ou do comportamento do modelo e do provedor escolhido.

### III. Clean Architecture e Baixo Acoplamento

O backend MUST seguir Clean Architecture com as camadas `Domain`, `Application`, `Infrastructure`
e `Api`.

- Dependências apontam apenas para dentro: `Domain` não depende de nenhuma outra camada nem de
  frameworks; `Application` depende apenas de `Domain`; `Infrastructure` e `Api` implementam ou
  consomem abstrações definidas em `Application`/`Domain`.
- Regras de negócio MUST residir em `Domain`/`Application`; controllers/endpoints MUST ser finos.
- Integrações externas (bancos de clientes, OpenRouter, Redis, persistência) MUST ficar atrás de
  interfaces e ser registradas via injeção de dependência.
- Exceções MUST ser tratadas de forma centralizada e retornadas como `ProblemDetails`
  (RFC 9457), sem expor stack traces, SQL interno de infraestrutura ou segredos.
- Regras de dependência entre camadas SHOULD ser verificadas por testes de arquitetura.
- **Chamadas diretas, sem mediação**: casos de uso MUST ser expostos como serviços da camada
  `Application` (ex.: `UserService.CreateAsync`) e invocados diretamente pelos endpoints. É
  proibido usar padrão Command/Query + Handler, mediator (ex.: MediatR), despacho por
  eventos/mensagens em processo ou decorators genéricos de pipeline. A validação de entrada é
  chamada explicitamente pelo próprio serviço.
- **Um tipo por arquivo**: cada arquivo MUST conter exatamente um tipo (classe, record,
  interface, enum ou struct), com o mesmo nome do arquivo. Tipos aninhados não são permitidos.
  A regra vale também para os testes.
- **Pastas por tipo de artefato dentro de cada contexto**: em `Domain`, `Application` e
  `Infrastructure`, cada contexto (ex.: `Users`, `Auth`, `Persistence`) MUST separar seus arquivos
  em subpastas por tipo, com nomes padronizados: `Entities`, `Enums`, `Errors`, `Policies`,
  `Results`, `Interfaces`, `Exceptions`, `Models`, `Services`, `Requests`, `Validations`, `Dtos`,
  `Settings`, `Extensions`, `Contexts`, `Configurations`, `Repositories`, `Transactions`,
  `Migrations`, `HostedServices`. Nenhum arquivo fica solto na pasta do contexto; na raiz do
  projeto só é permitido `DependencyInjection.cs`. A `Api`, por ser a camada de composição, é
  organizada diretamente por tipo (`Endpoints`, `Responses`, `Setup`, `Options`, `Validations`,
  `Middlewares`, `Handlers`, `Requirements`, `Extensions`, `Services`, `Policies`), com apenas
  `Program.cs` na raiz. Novos nomes de pasta exigem emenda a esta lista. Namespaces MUST
  corresponder ao caminho das pastas.

**Rationale**: separação de responsabilidades permite evoluir provedores e funcionalidades sem
reescrever o núcleo, e torna o sistema testável. Chamadas diretas deixam o fluxo explícito e fácil
de seguir; um tipo por arquivo facilita localizar código e revisar mudanças.

### IV. Extensibilidade por Abstrações de Provedor

Novos bancos de dados e novos provedores de IA MUST poder ser adicionados implementando
abstrações, sem alterar o núcleo da aplicação.

- Cada banco suportado (inicialmente PostgreSQL e SQL Server) MUST ser implementado como um
  provedor que encapsula: conexão, introspecção de schema, dialeto SQL, validação sintática e
  aplicação de limites.
- O acesso ao modelo de IA MUST ocorrer por uma abstração própria; OpenRouter é a implementação
  inicial, não um acoplamento do domínio.
- O modelo de IA do OpenRouter MUST ser selecionável pelo frontend, sem rebuild ou redeploy: o
  identificador do modelo (ex.: `anthropic/claude-sonnet-4`) é configuração persistida no banco da
  aplicação, nunca fixado no código. Apenas usuários com permissão administrativa podem alterar
  os modelos disponíveis e o modelo padrão da instância.
- O backend MUST validar o modelo escolhido (existência no OpenRouter e suporte a Tool Calling e
  Structured Outputs) antes de aceitá-lo, e MUST registrar em cada conversa/consulta qual modelo
  foi usado.
- Lógica específica de dialeto MUST NOT vazar para `Domain` ou para fluxos genéricos de
  `Application`.

**Rationale**: o objetivo de produto é suportar mais bancos, provedores de IA e capacidades
analíticas ao longo do tempo.

### V. Testes Automatizados como Critério de Pronto

Nenhuma funcionalidade é considerada concluída sem testes automatizados correspondentes.

- Testes unitários MUST cobrir regras de domínio, casos de uso e, obrigatoriamente, o validador de
  SQL com casos maliciosos e de borda (comentários, statements múltiplos, CTEs com escrita,
  funções perigosas, variações de caixa e encoding).
- Testes de integração MUST usar bancos reais em contêiner (ex.: Testcontainers) para PostgreSQL e
  SQL Server, cobrindo introspecção, execução, timeout e limite de linhas.
- Chamadas ao provedor de IA MUST ser substituíveis por dublês determinísticos nos testes; testes
  automatizados MUST NOT depender de chamadas reais pagas.
- Correções de bugs MUST incluir um teste que reproduz o problema.

**Rationale**: a segurança e a correção do pipeline só são confiáveis se verificadas
continuamente e de forma reproduzível.

### VI. Implantação Self-Hosted Single-Tenant e Proteção de Segredos

O QueryAI é distribuído como servidor self-hosted: cada cliente executa sua própria instância, em
sua própria infraestrutura, atendendo a uma única organização (single-tenant).

- A aplicação MUST NOT implementar multi-tenancy (sem `TenantId`, sem particionamento por
  organização); a instância inteira pertence a um único cliente.
- Dentro da instância, o acesso MUST ser controlado por usuários e papéis (ex.: `Admin`,
  `Analyst`) e por permissões por conexão: um usuário só consulta conexões e objetos que lhe foram
  concedidos, com autorização por políticas explícitas.
- Credenciais de bancos externos MUST ser criptografadas em repouso com chave fornecida pelo
  operador da instância, nunca registradas em log, nunca retornadas pela API e descriptografadas
  apenas no momento da conexão.
- Segredos (chave do OpenRouter, chave de criptografia, senhas de infraestrutura) MUST vir de
  configuração do operador (variáveis de ambiente/arquivos de secrets), nunca do repositório nem
  de valores padrão embutidos nas imagens.
- A instância MUST NOT enviar dados, telemetria ou metadados para serviços do fornecedor; a única
  comunicação externa obrigatória é com o provedor de IA configurado pelo cliente.
- Toda entrada da API MUST ser validada (ex.: FluentValidation) antes de chegar aos casos de uso.

**Rationale**: o cliente roda o produto dentro do próprio perímetro justamente para manter controle
sobre seus dados; a instância deve ser segura por padrão e não depender do fornecedor para operar.

### VII. Operações Assíncronas, Canceláveis e Resilientes

- Todo I/O (banco, HTTP, cache, IA) MUST ser assíncrono; chamadas bloqueantes (`.Result`,
  `.Wait()`) são proibidas.
- `CancellationToken` MUST ser propagado da requisição HTTP até o driver do banco e o cliente da
  IA, de modo que o cancelamento pelo usuário interrompa a operação de fato.
- Chamadas a serviços externos MUST ter timeouts e políticas de resiliência (retry com backoff
  apenas para operações idempotentes, circuit breaker quando adequado).

**Rationale**: consultas analíticas e chamadas a LLMs são lentas e caras; operações órfãs
desperdiçam recursos dos clientes e custos de IA.

### VIII. Observabilidade Estruturada

- Logs MUST ser estruturados (ex.: Serilog) e correlacionados por `TraceId`/`CorrelationId`,
  `UserId` e `ConversationId`.
- A stack de observabilidade é Grafana, Loki, Prometheus e Alloy; métricas MUST cobrir latência da
  API, execuções de SQL (duração, rejeições, timeouts) e uso de IA (tokens, custo, latência,
  erros por modelo). Essa stack é opcional na instância do cliente; a aplicação MUST continuar
  funcional sem ela, emitindo logs estruturados no stdout.
- Logs MUST NOT conter credenciais, strings de conexão nem dados de resultado dos clientes; SQL
  pode ser logado.

**Rationale**: um agente de IA é difícil de depurar sem rastreabilidade de cada passo, e custos só
são controláveis se medidos.

### IX. Controle de Custos e Limites de Uso da IA

- Todo uso de IA MUST ser contabilizado por usuário e por instância (tokens de entrada/saída, custo
  estimado, modelo).
- O sistema MUST aplicar limites configuráveis (rate limit por usuário, cota por usuário e cota global
  da instância, máximo de
  iterações de ferramentas por pergunta, máximo de tokens por requisição).
- O contexto enviado ao modelo MUST ser seletivo: apenas o subconjunto relevante do schema e do
  histórico, nunca o schema completo de forma indiscriminada nem dados brutos de resultado além do
  necessário para a explicação.
- Cache (Redis) SHOULD ser usado para metadados de schema e respostas reutilizáveis quando seguro.

**Rationale**: custos de LLM crescem com uso e tamanho de contexto; o cliente paga o provedor de IA
com a própria chave e precisa de custo previsível e visível.

### X. Entrega Incremental e Simplicidade

- O desenvolvimento MUST ocorrer em incrementos verticais, pequenos e funcionais, cada um
  especificado (`/speckit-specify`), planejado e testado antes do próximo.
- Decisões técnicas relevantes MUST ser documentadas no plano da feature (ou em ADR) com
  justificativa.
- Complexidade adicional (novas camadas, bibliotecas, padrões) MUST ser justificada por uma
  necessidade concreta (YAGNI); a ausência de justificativa é motivo para rejeição.

**Rationale**: o projeto é amplo; entregar tudo de uma vez compromete a consistência da
arquitetura e a qualidade.

## Restrições de Tecnologia e Arquitetura

- **Backend**: C# / .NET 10, ASP.NET Core Web API, projetos `Domain`, `Application`,
  `Infrastructure`, `Api` e projetos de teste correspondentes.
- **Persistência da aplicação**: PostgreSQL com Entity Framework Core e migrations versionadas.
- **Consultas em bancos externos**: Dapper sobre os drivers nativos (Npgsql, Microsoft.Data.SqlClient).
  EF Core MUST NOT ser usado para executar SQL dinâmico em bancos de clientes.
- **Cache e auxiliares**: Redis, somente quando houver necessidade justificada.
- **IA**: OpenRouter via abstração própria, com Tool Calling e Structured Outputs; modelo
  configurável pelo frontend (Princípio IV).
- **Frontend**: React com JavaScript (sem TypeScript), interface limpa, profissional e
  minimalista no estilo BI + chat. O frontend MUST NOT conter regras de segurança das quais o
  backend dependa.
- **API**: documentada via OpenAPI; contratos versionados; erros em `ProblemDetails`.
- **Infraestrutura**: Docker e Docker Compose para o ambiente de desenvolvimento (API, frontend,
  PostgreSQL, Redis, stack de observabilidade e bancos de exemplo).
- **Distribuição self-hosted**: o produto é entregue como imagens Docker versionadas (SemVer) e um
  `docker-compose` de referência para o cliente. A instalação MUST funcionar com configuração
  apenas por variáveis de ambiente, MUST aplicar migrations do banco da aplicação de forma
  automática e idempotente, e MUST suportar atualização entre versões sem perda de dados. A stack
  de observabilidade MUST ser opcional (ex.: profile do Compose), e a API MUST expor health checks.
- Adoção de nova tecnologia fora desta lista MUST ser justificada no plano da feature.

## Fluxo de Desenvolvimento e Quality Gates

- Cada feature segue o fluxo Spec Kit: `specify` → `clarify` (quando houver ambiguidade) →
  `plan` → `tasks` → `implement`. O plano MUST conter a seção "Constitution Check" avaliando os
  princípios I a X.
- Gates obrigatórios antes de considerar uma feature concluída:
  1. Build sem erros e sem novos warnings relevantes (nullable reference types habilitado).
  2. Todos os testes unitários e de integração passando.
  3. Nenhuma rota de execução de SQL que contorne o pipeline de validação (Princípio I).
  4. Nenhum segredo ou credencial em código, log ou resposta de API (Princípio VI).
  5. Documentação OpenAPI atualizada para endpoints novos ou alterados.
- Mudanças que afetam o validador de SQL, o controle de acesso ou o manuseio de credenciais
  MUST receber revisão de segurança explícita.
- Commits SHOULD seguir Conventional Commits.

## Governance

Esta constituição prevalece sobre quaisquer outras práticas, convenções ou preferências do
projeto. Em caso de conflito, a constituição vence até ser formalmente emendada.

- **Emendas**: propostas via `/speckit-constitution`, com justificativa, avaliação de impacto
  nos templates e nas features existentes e, quando necessário, plano de migração.
- **Versionamento** (SemVer):
  - MAJOR: remoção ou redefinição incompatível de princípios ou regras de governança.
  - MINOR: novo princípio/seção ou expansão material de orientação.
  - PATCH: esclarecimentos, redação e correções sem mudança semântica.
- **Conformidade**: todo plano e toda revisão de código MUST verificar aderência aos princípios.
  Violações só são aceitas se registradas na tabela "Complexity Tracking" do plano, com
  justificativa e alternativa mais simples rejeitada. Os Princípios I e II não admitem exceção.
- **Revisão periódica**: a constituição SHOULD ser revisada ao final de cada marco do roadmap.

**Version**: 2.3.0 | **Ratified**: 2026-11-16 | **Last Amended**: 2026-11-17
