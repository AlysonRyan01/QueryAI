# Feature Specification: Base do Projeto (Fundação da Instância QueryAI)

**Feature Branch**: `001-project-foundation`

**Created**: 2026-11-16

**Status**: Draft

**Input**: User description: "base do projeto, src em .NET em camadas e o web em react puro com js"

## Clarifications

### Session 2026-11-16

- Q: O banco interno da aplicação deve rodar dentro do pacote de instalação ou ser um banco que o cliente já tem? → A: Apenas banco incluído no pacote; o operador cuida do volume de dados e do backup.
- Q: Quem cuida do HTTPS quando a instância roda no ambiente do cliente? → A: O cliente coloca um proxy com HTTPS na frente da instância; o pacote documenta isso e marca os cookies de sessão como "somente HTTPS".
- Q: Os administradores devem ver o registro de eventos de segurança pela interface nesta etapa? → A: Não agora; os eventos são gravados e emitidos nos logs, e a tela de consulta fica para uma etapa futura.
- Q: Como deve ser criado o primeiro administrador de uma instância recém-instalada? → A: E-mail e senha definidos pelo operador no arquivo de variáveis de ambiente da instalação; na inicialização, o serviço de backend cria o administrador caso ainda não exista usuário com esse e-mail (troca de senha obrigatória no primeiro login).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Subir uma instância funcional com um único comando (Priority: P1)

O operador do cliente (responsável técnico pela instalação) obtém o pacote de distribuição do
QueryAI, preenche as configurações obrigatórias (segredos e senhas) e inicia a instância com um
único comando. Ao final, a aplicação web abre no navegador, o serviço reporta estar saudável e o
banco interno da aplicação está criado e atualizado, sem nenhum passo manual adicional.

**Why this priority**: é a fundação sobre a qual todas as demais funcionalidades serão entregues.
Sem uma instância que sobe de forma confiável e reproduzível, nenhuma outra etapa pode ser
desenvolvida, testada ou entregue ao cliente.

**Independent Test**: em uma máquina limpa com apenas o runtime de contêineres instalado, seguir o
guia de instalação, iniciar a instância e verificar que a página inicial carrega e que o
indicador de saúde responde como saudável.

**Acceptance Scenarios**:

1. **Given** uma máquina limpa com as configurações obrigatórias preenchidas, **When** o operador
   inicia a instância, **Then** a aplicação web fica acessível e o indicador de saúde informa
   "saudável" em até 3 minutos.
2. **Given** uma configuração obrigatória ausente (ex.: chave de criptografia), **When** o
   operador inicia a instância, **Then** a inicialização é interrompida com mensagem clara
   indicando qual configuração está faltando, e nenhum valor padrão inseguro é usado.
3. **Given** uma instância já em execução com dados, **When** o operador a reinicia ou atualiza
   para uma versão mais nova, **Then** a estrutura interna é atualizada automaticamente e nenhum
   dado existente é perdido.
4. **Given** a instância em execução, **When** o banco interno fica indisponível, **Then** o
   indicador de saúde passa a informar "não saudável" identificando a dependência com falha.

---

### User Story 2 - Primeiro acesso do administrador e navegação pela aplicação (Priority: P2)

Na primeira inicialização, a instância cria um usuário administrador inicial com as credenciais
definidas pelo operador. O administrador acessa a aplicação, é obrigado a trocar a senha no
primeiro login e passa a ver o layout principal com navegação para as áreas do produto: Início
(dashboard), Conexões, Chat, Histórico e Configurações. As áreas ainda não implementadas exibem
um estado vazio informativo.

**Why this priority**: o produto manipula acesso a bancos de dados de clientes; nenhuma tela pode
ficar acessível sem autenticação. O layout e a navegação estabelecem a experiência (BI + chat)
sobre a qual as próximas funcionalidades se encaixam.

**Independent Test**: com a instância recém-instalada, acessar a aplicação, autenticar com as
credenciais iniciais, trocar a senha, navegar por todas as áreas e sair; depois confirmar que
nenhuma área é acessível sem login.

**Acceptance Scenarios**:

1. **Given** uma instância recém-instalada, **When** o administrador entra com as credenciais
   iniciais, **Then** é obrigado a definir uma nova senha antes de acessar qualquer outra tela.
2. **Given** um visitante não autenticado, **When** ele tenta acessar qualquer área ou operação
   da aplicação, **Then** é redirecionado ao login e nenhuma informação é exposta.
3. **Given** um usuário autenticado, **When** ele navega pelo menu, **Then** todas as áreas
   (Início, Conexões, Chat, Histórico, Configurações) abrem sem erro, com estado vazio quando
   ainda não há conteúdo.
4. **Given** credenciais incorretas repetidas, **When** o limite de tentativas é atingido,
   **Then** novas tentativas para aquela conta são temporariamente bloqueadas.
5. **Given** um usuário autenticado, **When** ele sai da aplicação ou a sessão expira, **Then** o
   acesso às áreas protegidas exige novo login.

---

### User Story 3 - Administrador gerencia usuários e papéis (Priority: P3)

O administrador cadastra outros usuários da organização, atribui a cada um o papel de
Administrador ou Analista, redefine senhas e desativa contas. Analistas não têm acesso à área
administrativa.

**Why this priority**: a instância é compartilhada por várias pessoas da organização e as próximas
etapas (conexões, permissões por conexão, escolha do modelo de IA) dependem de existirem
usuários e papéis distintos.

**Independent Test**: como administrador, criar um analista, entrar como esse analista, confirmar
que ele não acessa a área administrativa, desativá-lo e confirmar que não consegue mais entrar.

**Acceptance Scenarios**:

1. **Given** um administrador autenticado, **When** ele cadastra um usuário com e-mail, nome,
   papel e senha temporária, **Then** o usuário consegue entrar e é obrigado a trocar a senha.
2. **Given** um e-mail já cadastrado, **When** o administrador tenta cadastrá-lo novamente,
   **Then** o sistema recusa com mensagem clara.
3. **Given** um analista autenticado, **When** ele tenta acessar a gestão de usuários, **Then** o
   acesso é negado.
4. **Given** um usuário desativado, **When** ele tenta entrar ou usar uma sessão existente,
   **Then** o acesso é negado.
5. **Given** apenas um administrador ativo, **When** alguém tenta desativá-lo ou rebaixá-lo,
   **Then** o sistema impede a operação para que a instância nunca fique sem administrador.

---

### Edge Cases

- Instância iniciada com segredo/senha obrigatória vazia ou com valor de exemplo: a inicialização
  falha com mensagem explícita.
- Banco interno ainda não disponível no momento da inicialização: a aplicação aguarda/tenta
  novamente por um período limitado antes de falhar com mensagem clara.
- Duas réplicas iniciando ao mesmo tempo: a atualização da estrutura interna é aplicada uma única
  vez, sem corrupção.
- Senha do administrador inicial alterada na configuração após a conta existir: não sobrescreve a
  conta existente.
- E-mail do administrador inicial alterado na configuração: na próxima inicialização é criado um
  novo administrador com o novo e-mail, e a conta anterior permanece como está. Isso também serve
  como forma de recuperar o acesso caso todos os administradores percam suas senhas.
- Senha do administrador inicial que não atende à política de senhas (FR-012): a inicialização
  falha com mensagem clara.
- Erros inesperados no servidor: o usuário vê mensagem amigável com um identificador de
  rastreamento; detalhes internos nunca são exibidos.
- Usuário com sessão aberta é desativado ou tem o papel alterado: a mudança vale no máximo na
  próxima requisição.
- Instância acessada por HTTP simples (sem o proxy com HTTPS) fora do modo de desenvolvimento: o
  login não é concluído e o usuário vê aviso de que a instância exige HTTPS.
- Aplicação acessada em telas pequenas: o layout permanece utilizável (menu recolhível).

## Requirements *(mandatory)*

### Functional Requirements

**Instalação e operação**

- **FR-001**: O sistema MUST ser distribuído de forma que o operador inicie a instância completa
  (aplicação web, serviço de backend e banco interno) com um único comando. O banco interno é
  sempre o incluído no pacote; o uso de um banco interno externo fornecido pelo cliente não é
  suportado nesta versão.
- **FR-001a**: Os dados do banco interno MUST ser mantidos em armazenamento persistente separado
  dos contêineres, sobrevivendo a reinício, remoção e atualização dos contêineres.
- **FR-002**: O sistema MUST ler toda configuração de ambiente (segredos, senhas, endereços,
  credenciais iniciais do administrador) de configuração fornecida pelo operador, e MUST recusar
  iniciar quando uma configuração obrigatória estiver ausente ou com valor de exemplo.
- **FR-003**: O sistema MUST criar e atualizar automaticamente a estrutura do banco interno na
  inicialização, de forma idempotente e sem perda de dados entre versões.
- **FR-004**: O sistema MUST expor indicadores de "vivo" e "pronto" que reflitam o estado das
  dependências essenciais (no mínimo, o banco interno).
- **FR-005**: O sistema MUST exibir a versão instalada (na aplicação web e no serviço).
- **FR-006**: O sistema MUST registrar logs estruturados de cada requisição com identificador de
  correlação, sem registrar senhas, tokens ou segredos.
- **FR-007**: O sistema MUST retornar erros em formato padronizado contendo um identificador de
  rastreamento, sem expor detalhes internos.
- **FR-008**: O serviço de backend MUST publicar documentação navegável de suas operações.
- **FR-008a**: O HTTPS MUST ser feito por um proxy reverso do cliente, posicionado na frente da
  instância; o pacote não gerencia certificados. A instância MUST confiar nas informações de
  origem e protocolo encaminhadas apenas por proxies configurados pelo operador, e o guia de
  instalação MUST incluir um exemplo de configuração de proxy com HTTPS.
- **FR-009**: Componentes auxiliares opcionais (cache, observabilidade) MUST poder ficar
  desligados sem impedir o funcionamento desta etapa.

**Autenticação e sessão**

- **FR-010**: Em toda inicialização, o serviço de backend MUST verificar se existe usuário com o
  e-mail de administrador inicial definido pelo operador no arquivo de variáveis de ambiente da
  instalação; se não existir, MUST criá-lo com papel Administrador, a senha configurada e troca
  de senha obrigatória no primeiro login. Se já existir, nada é alterado. A verificação MUST ser
  segura contra inicializações simultâneas (nunca cria duplicatas).
- **FR-011**: Todas as telas e operações, exceto login, indicadores de saúde e versão, MUST exigir
  usuário autenticado e ativo.
- **FR-012**: O sistema MUST exigir senhas com no mínimo 12 caracteres e armazená-las apenas de
  forma irreversível (hash forte com salt).
- **FR-013**: O sistema MUST bloquear temporariamente uma conta após 5 tentativas de login
  incorretas consecutivas (bloqueio de 15 minutos).
- **FR-014**: Sessões MUST expirar após 8 horas de inatividade e MUST poder ser encerradas pelo
  usuário (logout).
- **FR-014a**: Fora do modo de desenvolvimento, os cookies de sessão MUST ser marcados como
  "somente HTTPS" e inacessíveis a scripts da página; acessar a instância por HTTP simples
  impede o login, e a tela de login MUST exibir um aviso claro de que é necessário HTTPS.
- **FR-015**: Usuários MUST poder trocar a própria senha informando a senha atual.

**Usuários e papéis**

- **FR-016**: O sistema MUST suportar os papéis Administrador e Analista.
- **FR-017**: Administradores MUST poder cadastrar, editar (nome e papel), redefinir senha,
  desativar e reativar usuários; e-mail é único e identifica o usuário.
- **FR-018**: Analistas MUST NOT acessar funcionalidades administrativas, e essa restrição MUST
  ser aplicada pelo serviço de backend, não apenas ocultada na interface.
- **FR-019**: O sistema MUST impedir que a instância fique sem nenhum administrador ativo.
- **FR-020**: O sistema MUST registrar eventos de segurança (login com sucesso/falha, bloqueio,
  troca de senha, criação/alteração/desativação de usuário) com autor e data/hora, gravando-os
  de forma persistente e também emitindo-os no log estruturado. Nesta etapa não há tela para
  consultar esses eventos.

**Interface**

- **FR-021**: A aplicação web MUST apresentar um layout com navegação para Início, Conexões, Chat,
  Histórico e Configurações (esta visível apenas para administradores), exibindo estado vazio
  informativo nas áreas ainda não implementadas.
- **FR-022**: A interface MUST ser limpa, minimalista e utilizável em telas a partir de 360 px de
  largura.
- **FR-023**: A interface MUST exibir mensagens de erro amigáveis em português e estados de
  carregamento para operações em andamento.

### Key Entities

- **Usuário**: pessoa da organização que usa a instância. Atributos: nome, e-mail (único), papel,
  situação (ativo/desativado), exige troca de senha, tentativas falhas e bloqueio até, datas de
  criação e último acesso.
- **Papel**: nível de acesso (Administrador, Analista). Determina acesso às áreas administrativas;
  nesta etapa é um conjunto fixo.
- **Evento de Auditoria de Segurança**: registro imutável de um evento de segurança. Atributos:
  tipo, usuário autor, usuário afetado (quando houver), data/hora, origem da requisição.
- **Configuração da Instância**: valores fornecidos pelo operador na inicialização (não editáveis
  pela interface nesta etapa).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Um operador sem conhecimento prévio do produto consegue instalar e acessar a
  instância seguindo apenas o guia de instalação em até 15 minutos.
- **SC-002**: A partir do comando de inicialização em máquina com imagens já baixadas, a
  aplicação fica utilizável em até 3 minutos.
- **SC-003**: Atualizar a instância para uma nova versão preserva 100% dos usuários e dados
  existentes.
- **SC-004**: 100% das telas e operações protegidas recusam acesso sem autenticação, e 100% das
  operações administrativas recusam acesso de analistas (verificado por testes automatizados).
- **SC-005**: Um administrador consegue cadastrar um novo usuário em menos de 1 minuto.
- **SC-006**: As telas da aplicação carregam e respondem à navegação em menos de 2 segundos em
  rede local.
- **SC-007**: Nenhum segredo, senha ou token aparece em logs, respostas de erro ou no código
  versionado (verificado por inspeção automatizada).

## Assumptions

- **Restrições técnicas definidas pela constituição e pelo pedido**: backend em .NET separado em
  camadas (Domain, Application, Infrastructure, Api) e aplicação web em React com JavaScript puro
  (sem TypeScript), distribuídos como contêineres; esses detalhes pertencem ao plano técnico.
- Escopo desta etapa: fundação (instalação, operação, autenticação, usuários/papéis e layout).
  Conexões com bancos externos, agente de IA, execução de SQL, chat funcional, histórico e
  escolha do modelo de IA ficam para etapas seguintes; as áreas correspondentes aparecem apenas
  como navegação com estado vazio.
- Autenticação local por e-mail e senha; SSO/LDAP e recuperação de senha por e-mail ficam fora do
  escopo (redefinição é feita por um administrador).
- O administrador inicial é definido por configuração do operador (arquivo de variáveis de
  ambiente), e não por tela pública de primeiro acesso, para evitar que alguém se apodere de uma
  instância recém-exposta.
- Instância single-tenant: uma organização por instalação, sem separação por organização.
- A interface é exclusivamente em português do Brasil nesta etapa.
- A tela de consulta da auditoria de segurança fica para uma etapa futura; até lá, os eventos
  ficam disponíveis no banco interno e nos logs.
- A stack de observabilidade e o cache são opcionais e podem ser habilitados em etapas futuras;
  nesta etapa basta log estruturado na saída padrão.
- Operadores têm um ambiente com runtime de contêineres e acesso às imagens de distribuição.
- Backup e restauração do armazenamento persistente do banco interno são responsabilidade do
  operador; o guia de instalação MUST documentar onde os dados ficam e como fazer o backup.
