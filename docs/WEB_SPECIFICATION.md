# Especificação do Frontend Web — LicenciamentoSoftware

**Versão:** 1.0  
**Fase:** 9  
**Projeto:** `LicenciamentoSoftware.Web` + `LicenciamentoSoftware.Web.Server`  
**Stack:** Blazor WASM (.NET 10) + ASP.NET Core BFF + YARP  

---

## 1. Visão Geral

O frontend web é um **portal de gestão** destinado aos clientes da plataforma (empresas que contratam o sistema de licenciamento). Cada empresa gerencia seus próprios dados de forma isolada — nenhum dado de uma empresa é visível para outra.

### 1.1 O que o portal permite

- Cadastrar a empresa e criar a conta de administrador
- Fazer login com e-mail, senha e segundo fator TOTP
- Gerenciar clientes finais, usuários internos e aplicações
- Emitir e gerenciar licenças de software por tipo
- Gerar e renovar tokens HMAC para autenticação dos aplicativos clientes
- Monitorar sessões ativas e instalações registradas
- Executar operações manuais: renovar período, encerrar sessão, liberar instalação

### 1.2 O que o portal NÃO faz

- **Não é o portal do cliente final** — o usuário final (Pedro da Empresa XYZ) não usa este portal. Ele usa o aplicativo cliente que valida licença via API HMAC
- **Não tem painel de administração de plataforma** — não existe um superadmin que gerencia todas as empresas
- **Não expõe a API de validação** — os endpoints `/api/validacao/*` são usados pelos aplicativos, não pelo portal

---

## 2. Arquitetura

### 2.1 Projetos envolvidos

```
LicenciamentoSoftware.Web          Blazor WASM — páginas e componentes (roda no browser)
LicenciamentoSoftware.Web.Server   BFF — serve o WASM, gerencia cookies, proxy para API
LicenciamentoSoftware.Client       Biblioteca HTTP — DTOs e services compartilhados
```

### 2.2 Fluxo de comunicação

```
Browser
  │  Baixa os arquivos WASM (.js, .wasm, .dll)
  │  e executa o C# localmente
  │
  ↕ HTTPS / mesma origem (localhost:7152)
  │
Web.Server (BFF)
  │  ├── Serve os arquivos estáticos do WASM
  │  ├── POST /bff/login      → autentica, emite cookie HttpOnly
  │  ├── POST /bff/login/2fa  → valida TOTP, emite cookie HttpOnly
  │  ├── POST /bff/refresh    → renova access token via cookie
  │  ├── POST /bff/logout     → revoga sessão, apaga cookie
  │  ├── POST /bff/cadastrar  → proxy público para /auth/cadastrar
  │  └── /* (demais rotas)    → YARP repassa para a API com Authorization header
  │
  ↕ HTTPS (localhost:7075)
  │
API (LicenciamentoSoftware.Api)
     └── Processa as requisições com JWT Bearer
```

### 2.3 Segurança dos tokens

| Token | Onde fica | Acesso JavaScript |
|---|---|---|
| Access Token (JWT, 60 min) | Memória C# no WASM | Impossível — nunca serializado |
| Refresh Token | Cookie `HttpOnly; Secure; SameSite=Strict` | Impossível — HttpOnly |

**Por que BFF?**  
Em SPA puro o refresh token precisaria de `localStorage` (exposto a XSS) ou `sessionStorage` (sem persistência). O BFF elimina esse problema: o cookie HttpOnly é invisível para qualquer JavaScript, inclusive o próprio código da aplicação.

**Fluxo de renovação silenciosa:**  
Quando o access token expira (401), o `BearerTokenHandler` chama automaticamente `POST /bff/refresh`. O browser envia o cookie automaticamente (mesma origem). Um novo access token é emitido sem que o usuário perceba.

---

## 3. Estrutura de arquivos

```
src/LicenciamentoSoftware.Web/
├── App.razor                          Roteador principal com AuthorizeRouteView
├── _Imports.razor                     Usings globais
├── Program.cs                         Bootstrap: DI, HttpClients, AuthProvider
│
├── Layout/
│   ├── MainLayout.razor               Shell autenticado (sidebar + conteúdo)
│   └── PublicLayout.razor             Shell público (login, cadastro, totp)
│
├── Shared/
│   ├── ApiError.razor                 Exibe erros de validação da API (400/409/422)
│   ├── ConfirmDialog.razor            Modal de confirmação para ações destrutivas
│   ├── Modal.razor                    Modal genérico reutilizável com ChildContent + Footer
│   ├── Paginacao.razor                Componente de paginação numérica
│   └── RedirectToLogin.razor         Redireciona para /login quando não autenticado
│
├── Pages/
│   ├── Index.razor                    Redirect para /clientes-finais ou /login
│   ├── Login.razor                    Formulário de login (layout público)
│   ├── Totp.razor                     Verificação de segundo fator TOTP
│   ├── Cadastro.razor                 Cadastro público de empresa + responsável
│   ├── Logout.razor                   Encerra sessão e redireciona
│   │
│   ├── ClientesFinais/
│   │   └── Lista.razor               Grid de cards + modal criar/editar/desativar
│   ├── Usuarios/
│   │   └── Lista.razor               Grid de cards + modal criar/editar/desativar
│   ├── Aplicacoes/
│   │   └── Lista.razor               Grid de cards + modal criar/editar/desativar
│   └── Licencas/
│       └── Lista.razor               Grid de cards + modal emitir + modal detalhe
│
├── Services/
│   ├── JwtAuthStateProvider.cs       AuthenticationStateProvider — access token em memória
│   ├── BearerTokenHandler.cs         DelegatingHandler — adiciona Authorization header
│   ├── TokenRefreshHandler.cs        DelegatingHandler — renova token silenciosamente em 401
│   └── ApiHttpClientFactory.cs       Factory singleton — HttpClients com DefaultRequestHeaders
│
└── wwwroot/
    ├── index.html                     Entry point HTML do WASM
    └── css/
        └── app.css                    Estilos globais (sidebar, cards, badges, modais)

src/LicenciamentoSoftware.Web.Server/
├── Program.cs                         Bootstrap: BFF, YARP, cookie policy
├── appsettings.json                   ApiSettings:BaseUrl, ReverseProxy config
├── Configuration/
│   └── BffServiceExtensions.cs       Helpers de configuração (cookie policy, HttpClients)
└── Controllers/
    └── BffController.cs              Endpoints /bff/* (login, 2fa, refresh, logout, cadastrar)

src/LicenciamentoSoftware.Client/
├── Models/
│   ├── Auth/                         LoginRequest, LoginResponse, AutoCadastroRequest
│   ├── Common/                       PagedResult<T>
│   ├── ClientesFinais/               ClienteFinalResult, CriarClienteFinalRequest, Atualizar...
│   ├── Usuarios/                     UsuarioResult, CriarUsuarioRequest, Atualizar...
│   ├── Aplicacoes/                   AplicacaoResult, CriarAplicacaoRequest, Atualizar...
│   ├── TiposLicenca/                 TipoLicencaResult
│   └── Licencas/                     LicencaResult, EmitirLicencaRequest, RenovarPeriodoRequest, TokenInfoResult
└── Services/
    ├── AuthApiService.cs             login, verify-2fa, refresh, logout, cadastrar
    ├── ClienteFinalApiService.cs     CRUD + paginação
    ├── UsuarioApiService.cs          CRUD + paginação
    ├── AplicacaoApiService.cs        CRUD + paginação
    ├── TipoLicencaApiService.cs      listagem
    └── LicencaApiService.cs          emitir, listar, desativar, renovar, sessões, instalações, token
```

---

## 4. Autenticação e autorização

### 4.1 Fluxo de login

```
1. POST /bff/login {email, senha}
   ├── Sucesso sem 2FA → BFF emite cookie HttpOnly + retorna {accessToken, nome, papel}
   │   WASM: JwtAuthStateProvider.MarcarAutenticado() → redireciona para /
   └── Requer 2FA → BFF retorna {requer2FA: true, tokenTemporario}
       WASM: redireciona para /totp?token=...

2. POST /bff/login/2fa {tokenTemporario, codigo}
   └── Sucesso → BFF emite cookie HttpOnly + retorna {accessToken, nome, papel}
       WASM: JwtAuthStateProvider.MarcarAutenticado() → redireciona para /
```

### 4.2 Renovação silenciosa

```
Qualquer requisição autenticada
  → BearerTokenHandler adiciona Authorization: Bearer {accessToken}
  → Se retornar 401:
      TokenRefreshHandler chama POST /bff/refresh
      Browser envia cookie automaticamente
      BFF valida cookie → retorna novo accessToken
      JwtAuthStateProvider.AtualizarToken()
      Requisição original é retentada com novo token
```

### 4.3 Proteção de rotas

Todas as páginas protegidas usam `@attribute [Authorize]`. O `App.razor` usa `AuthorizeRouteView` com `<NotAuthorized>` que renderiza `<RedirectToLogin />`, redirecionando para `/login?returnUrl=...`.

### 4.4 Logout

```
POST /bff/logout
  → BFF revoga refresh token na API
  → BFF apaga cookie
  → JwtAuthStateProvider.MarcarDesautenticado()
  → Redireciona para /login
```

---

## 5. Páginas e funcionalidades

### 5.1 Página de Login (`/login`)

**Layout:** PublicLayout (sem sidebar)  
**Visual:** painel roxo esquerdo (branding) + formulário direito

**Campos:**
- E-mail (tipo email, autocomplete)
- Senha (tipo password, autocomplete)
- Enter submete o formulário

**Fluxo:**
1. Usuário preenche credenciais e clica "Entrar"
2. Se bem-sucedido → redireciona para `returnUrl` ou `/`
3. Se 2FA necessário → redireciona para `/totp?token=...`
4. Se erro → exibe mensagem inline

**Links:** "Cadastre-se gratuitamente" → `/cadastro`

---

### 5.2 Página de Verificação TOTP (`/totp`)

**Layout:** PublicLayout  
**Parâmetro de query:** `token` (token temporário de desafio 2FA)

**Campos:**
- Código TOTP (6 dígitos, `autocomplete="one-time-code"`)

**Fluxo:**
1. Usuário digita código do autenticador
2. POST `/bff/login/2fa` → se válido, autentica e redireciona para `/`
3. Se inválido → exibe erro inline

---

### 5.3 Página de Cadastro (`/cadastro`)

**Layout:** PublicLayout  
**Acesso:** Público (anônimo)

**Campos — Dados da empresa:**
- Razão Social *
- Tipo de inscrição (CPF / CNPJ)
- Número CPF/CNPJ *
- E-mail da empresa *
- Telefone (opcional)

**Campos — Responsável pela conta:**
- Nome completo *
- E-mail de acesso *
- Senha * (mínimo 8 caracteres)
- Confirmar senha *

**Fluxo:**
1. Formulário validado no cliente (senhas coincidem, campos obrigatórios)
2. POST `/bff/cadastrar` → proxy para `POST /auth/cadastrar`
3. Cria `Cliente` + primeiro `Usuário` como `AdministradorCliente` em uma transação
4. Sucesso → tela de confirmação com link para `/login`
5. Conflito (409) → erro de CPF/CNPJ ou e-mail duplicado
6. Inválido (422) → lista de erros de validação

---

### 5.4 Clientes Finais (`/clientes-finais`)

**Layout:** MainLayout (autenticado)  
**Acesso:** Qualquer usuário autenticado

**Listagem:**
- Grid de cards (3 colunas em desktop)
- Busca por razão social (Enter ou botão)
- Paginação numérica
- Card exibe: razão social, badge ativo/inativo, e-mail, telefone, CPF/CNPJ

**Criar (modal "Novo Cliente"):**
- Razão Social *
- Tipo (CPF/CNPJ) + Número *
- E-mail *
- Telefone

**Editar (modal "Editar Cliente"):**
- Razão Social, E-mail, Telefone (CPF/CNPJ não editável após criação)

**Desativar:**
- ConfirmDialog antes de executar
- Exclusão lógica (`ativo = false`)

**Isolamento de tenant:** `IdCliente` vem do JWT, nunca do formulário.

---

### 5.5 Usuários (`/usuarios`)

**Layout:** MainLayout  
**Acesso:** Qualquer usuário autenticado

**Listagem:**
- Grid de cards com avatar de iniciais
- Card exibe: nome, e-mail, papel, badge ativo/inativo

**Criar (modal "Novo Usuário"):**
- Nome *, E-mail *, Senha *

**Editar (modal "Editar Usuário"):**
- Nome, E-mail (senha não editável via edição)

**Observação:** O `IdCliente` vem do JWT. Todos os usuários criados pertencem ao mesmo tenant.

---

### 5.6 Aplicações (`/aplicacoes`)

**Layout:** MainLayout  
**Acesso:** Qualquer usuário autenticado

**Listagem:**
- Grid de cards
- Card exibe: título, tipo de licença (badge colorido), descrição, status

**Criar (modal "Nova Aplicação"):**
- Título *, Descrição, Tipo de Licença * (dropdown carregado de `/tipos-licenca`)

**Editar (modal "Editar Aplicação"):**
- Título, Descrição (tipo não editável após criação)

**Badges de tipo:**

| Tipo | Cor |
|---|---|
| Permanente | Azul |
| Por Período | Amarelo |
| Por Usuários | Roxo |
| Por Instalação | Rosa |

---

### 5.7 Licenças (`/licencas`)

**Layout:** MainLayout  
**Acesso:** Qualquer usuário autenticado

#### 5.7.1 Listagem

- Grid de cards
- Busca por cliente/aplicativo
- Card exibe: razão social do cliente final, aplicação, tipo (badge), status, resumo (período/usuários/instalações), chave abreviada

#### 5.7.2 Emissão (modal "Nova Licença")

**Passo 1 — Seleção:**
- Dropdown de Aplicação (carregado com tipo embutido no label)
- Dropdown de Cliente Final
- Tipo detectado automaticamente ao selecionar a aplicação

**Passo 2 — Campos dinâmicos por tipo:**

| Tipo | Campos extras |
|---|---|
| Permanente | Nenhum |
| Por Período | Data início *, Data fim *, Renovação automática (checkbox) |
| Por Usuários | Máx. usuários *, Máx. sessões por usuário |
| Por Instalação | Máx. instalações * |

**Opção:** "Gerar token HMAC junto com a licença" (checkbox)

**Pós-emissão:**
- Tela de sucesso dentro do modal
- Se token foi gerado: exibe valor com botão "Copiar"
- Aviso: "Exibido uma única vez. Guarde com segurança."

#### 5.7.3 Detalhe (modal "Detalhe da Licença")

Abre ao clicar "Detalhes" em qualquer card da lista. Carrega o detalhe completo da licença via `GET /licencas/{id}`.

**Conteúdo do modal (adapta-se ao tipo):**

**Cabeçalho:**
- Nome do cliente final + aplicação
- Badges: status (Ativa/Inativa) + tipo de licença

**Seção Período** (apenas tipo "Por Período"):
- Datas início/fim, renovação automática
- Campo "Nova data fim" + botão "Renovar período"

**Seção Usuários** (apenas tipo "Por Usuários"):
- Máx. simultâneos, sessões por usuário

**Seção Sessões ativas** (quando há sessões):
- Tabela: identificador do usuário, data login, última atividade
- Botão "Encerrar" por linha (com ConfirmDialog)

**Seção Instalações** (quando há instalações):
- Tabela: identificador da máquina, data de registro
- Botão "Liberar" por linha (com ConfirmDialog)
- Contador: ativas/máximo

**Seção Token HMAC:**
- Status: "Sem token" / "Ativo · exp. dd/MM/yyyy" / "Expirado"
- Botão "Gerar token" (se sem token ou expirado)
- Botão "Renovar token" (se ativo — revoga o anterior)
- Após gerar/renovar: exibe valor com botão "Copiar" e aviso de exibição única

---

### 5.8 Dashboard (`/dashboard`)

**Layout:** MainLayout (autenticado) — primeiro item do menu

**Carregamento:** resumo e alertas carregados em paralelo via `Task.WhenAll`. Skeleton loader exibido durante carregamento.

**Cards de métricas (sempre visíveis após carregamento):**

| Card | Métrica | Alerta visual |
|---|---|---|
| Clientes Finais | Total ativos + novos 30d | — |
| Aplicações | Total ativas | — |
| Licenças Ativas | Total + inativas | — |
| Expirando em 7 dias | Contagem | Laranja se > 0 |
| Sessões abertas agora | Total ativas | — |
| Tokens expirando em 7 dias | Contagem | Laranja se > 0 |
| Novas licenças (30 dias) | Contagem | — |

**Breakdown por tipo:** badges coloridos mostrando Permanente / Por Período / Por Usuários / Por Instalação.

**Seção de alertas** (oculta quando não há dados):
- Erros de validação nas últimas 24h com breakdown por motivo
- Licenças no limite de capacidade (usuários ou instalações)
- Sessões inativas prolongadas (> 2× TempoLimiteSessaoHoras)
- Instalações adormecidas (> 30 dias sem validação)

**Componente `MetricaCard.razor`:** reutilizável, aceita `Titulo`, `Valor`, `Subtitulo`, `Cor`, `Icone` e `Alerta`. Inclui skeleton loader via CSS animation.

---

## 6. Componentes compartilhados

### 6.1 Modal.razor

```razor
<Modal Visivel="@_aberto" Titulo="Título" OnFechar="Fechar">
    <ChildContent>
        <!-- conteúdo do formulário -->
    </ChildContent>
    <Footer>
        <button @onclick="Fechar">Cancelar</button>
        <button @onclick="Salvar">Salvar</button>
    </Footer>
</Modal>
```

- Overlay escurecido, fecha ao clicar fora
- `max-height: 85vh` com scroll interno no body
- Header e footer fixos, apenas o body rola
- Largura máxima de 560px (responsivo)

### 6.2 ConfirmDialog.razor

```csharp
_confirmDialog.Mensagem = "Mensagem de confirmação";
bool confirmado = await _confirmDialog.MostrarAsync();
if (!confirmado) return;
// executa ação
```

- Bloqueante via `TaskCompletionSource<bool>`
- Botão de confirmação vermelho, cancelar neutro

### 6.3 ApiError.razor

```razor
<ApiError Erro="@_erro" Erros="@_erros" />
```

- Exibe `Erro` (string única) ou `Erros` (lista) como alert vermelho
- Visível apenas quando há conteúdo

### 6.4 Paginacao.razor

```razor
<Paginacao PaginaAtual="_pagina" TotalPaginas="_totalPaginas"
           Total="_total" OnPaginaMudou="MudarPagina" />
```

- Exibe até 5 páginas numeradas com `...` implícito
- Texto "Página X de Y (Z itens)"
- Oculto quando `TotalPaginas <= 1`

---

## 7. Serviços de autenticação

### 7.1 JwtAuthStateProvider

Herda de `AuthenticationStateProvider`. Implementação central de autenticação.

**Responsabilidades:**
- Armazena `AccessToken` em campo privado (never serialized)
- Parseia claims do JWT payload (base64url, sem verificação de assinatura — já validada pela API)
- Notifica Blazor quando o estado muda (`NotifyAuthenticationStateChanged`)
- Expõe `AccessToken` para os handlers HTTP
- Mantém referência ao `ApiHttpClientFactory` para atualizar headers em todos os clients

**Métodos públicos:**
- `MarcarAutenticado(token, nome, papel)` — chamado após login bem-sucedido
- `AtualizarToken(token, nome, papel)` — chamado após refresh silencioso
- `MarcarDesautenticado()` — chamado no logout

### 7.2 ApiHttpClientFactory

Singleton que cria e mantém um `HttpClient` para cada service. Quando o token muda, atualiza o `DefaultRequestHeaders.Authorization` de todos os clients de uma vez.

**Por que singleton?**
Em WASM o escopo do browser é toda a vida da página. Usar `Scoped` criaria uma nova instância a cada navegação de página, perdendo os tokens nos headers.

### 7.3 BearerTokenHandler

`DelegatingHandler` que adiciona `Authorization: Bearer {token}` em toda requisição sainte dos services. Usa `JwtAuthStateProvider.AccessToken` — sempre a versão mais atual em memória.

### 7.4 TokenRefreshHandler

`DelegatingHandler` que intercepta respostas 401. Faz `POST /bff/refresh`, obtém novo token, chama `JwtAuthStateProvider.AtualizarToken()` e retenta a requisição original.

---

## 8. Configuração de portas (desenvolvimento)

| Projeto | HTTP | HTTPS |
|---|---|---|
| `LicenciamentoSoftware.Api` | 5016 | 7075 |
| `LicenciamentoSoftware.Web` | 5075 | 7153 (sem browser) |
| `LicenciamentoSoftware.Web.Server` | 5074 | **7152** ← acesso do usuário |

O usuário acessa sempre `https://localhost:7152`.  
O `Web.Server` faz proxy interno para a API em `https://localhost:7075`.  
O projeto `Web` não precisa estar em Start — o `Web.Server` já inclui seus arquivos na build.

---

## 9. CSS e identidade visual

**Paleta principal:**

| Uso | Cor |
|---|---|
| Cor primária (botões, sidebar ativa, logo) | `#6c63ff` (roxo) |
| Background da página | `#f8f9fa` (cinza claro) |
| Cards | `#ffffff` (branco) com borda `#e9ecef` |
| Texto principal | `#212529` |
| Texto secundário | `#6c757d` |

**Badges de status:**

| Badge | Background | Texto |
|---|---|---|
| Ativo | `#d1fae5` | `#065f46` (verde) |
| Inativo | `#f3f4f6` | `#6b7280` (cinza) |
| Permanente | `#dbeafe` | `#1e40af` (azul) |
| Por Período | `#fef3c7` | `#92400e` (amarelo) |
| Por Usuários | `#ede9fe` | `#5b21b6` (roxo) |
| Por Instalação | `#fce7f3` | `#9d174d` (rosa) |

**Classes CSS principais:**

| Classe | Descrição |
|---|---|
| `.app-shell` | Flex container principal (sidebar + conteúdo) |
| `.sidebar` | Sidebar fixa 220px |
| `.main-content` | Área de conteúdo com `margin-left: 220px` |
| `.page-header-row` | Flex entre título e botão de ação |
| `.cards-grid` | Grid responsivo `minmax(280px, 1fr)` |
| `.item-card` | Card branco com bordas e hover shadow |
| `.badge-pill` | Badge arredondado para status e tipos |
| `.modal-overlay` | Overlay fixo com fundo semitransparente |
| `.modal-card` | Container do modal com flex-column e scroll |
| `.search-bar` | Input de busca estilizado |

---

## 10. Fluxo completo — exemplo de uso

### 10.1 Primeiro acesso (novo cliente)

```
1. Acessa https://localhost:7152
2. Não autenticado → redireciona para /login
3. Clica "Cadastre-se" → /cadastro
4. Preenche dados da empresa + responsável → POST /bff/cadastrar
5. Conta criada → tela de sucesso → clica "Ir para login"
6. Login com e-mail + senha → POST /bff/login
7. Se 2FA não configurado → autenticado diretamente
8. Se 2FA configurado → redireciona para /totp
9. Dashboard em /clientes-finais
```

### 10.2 Emitir uma licença com token

```
1. Acessa /licencas
2. Clica "+ Nova Licença"
3. Modal abre — seleciona Aplicação (ex: "Meu CRM (Por Usuários)")
4. Seleciona Cliente Final
5. Campos de usuários aparecem: máx. 10 simultâneos
6. Marca "Gerar token HMAC"
7. Clica "Emitir licença"
8. Modal mostra: "Licença emitida com sucesso!"
9. Token exibido com botão "Copiar"
10. Administrador copia o token e configura no software cliente
11. Fecha o modal
```

### 10.3 Gerenciar sessões ativas

```
1. Na lista de licenças, clica "Detalhes" em uma licença "Por Usuários"
2. Modal abre com dados completos
3. Seção "Sessões ativas" mostra as 3 sessões abertas
4. Clica "Encerrar" na sessão do usuário "pedro@empresa.com"
5. ConfirmDialog: "Encerrar esta sessão?"
6. Confirma → sessão encerrada imediatamente
7. Modal atualiza com 2 sessões
8. Slot liberado para novo login
```

### 10.4 Renovar token expirado

```
1. Clica "Detalhes" em uma licença
2. Seção "Token HMAC" mostra: "Expirado"
3. Clica "Gerar token"
4. Novo token gerado e exibido com botão "Copiar"
5. Administrador copia e atualiza a configuração do software cliente
```

---

## 11. Limitações conhecidas e trabalhos futuros

| Limitação | Fase planejada |
|---|---|
| Portal do Cliente Final (ver suas próprias licenças) | Fase 9.2 |
| Login social (Google, Microsoft, GitHub) via OAuth | Fase 9.2 |
| Setup de TOTP via QR code no portal | Fase 9.2 |
| App Desktop/Mobile (MAUI) | Fase 10 |
| Deploy em produção com host real | Fase 11 |
