# Contract: Rotas da Aplicação Web

| Rota | Acesso | Conteúdo |
|---|---|---|
| `/login` | público | Formulário de e-mail/senha; mensagens para `invalid_credentials`, `account_locked` (com tempo restante), `rate_limited` e `https_required` |
| `/trocar-senha` | autenticado | Obrigatória quando `mustChangePassword`; também acessível pelo menu do usuário |
| `/` | autenticado | Início (dashboard) — estado vazio: "Suas consultas recentes aparecerão aqui" |
| `/conexoes` | autenticado | Estado vazio (etapa futura) |
| `/chat` | autenticado | Estado vazio (etapa futura) |
| `/historico` | autenticado | Estado vazio (etapa futura) |
| `/configuracoes` | Admin | Página com abas; nesta etapa só a aba **Usuários** |
| `/configuracoes/usuarios` | Admin | Lista com busca e paginação, criar, editar nome/papel, redefinir senha, desativar/reativar |
| `*` | — | Página "não encontrada" |

**Regras de navegação**

- Sem sessão (`/api/auth/me` → 401) → redireciona para `/login?redirect=<rota>`.
- `mustChangePassword` → qualquer rota redireciona para `/trocar-senha`.
- Analista acessando `/configuracoes/*` → página "acesso negado" (a API também retorna 403).
- O layout tem uma barra lateral recolhível (menu hambúrguer abaixo de 768 px, utilizável a partir de 360 px), a
  versão da aplicação e da API no rodapé da barra e o menu do usuário (trocar senha, sair).
- Todas as requisições exibem estado de carregamento; erros mostram a mensagem em pt-BR e o
  `traceId` para suporte.
