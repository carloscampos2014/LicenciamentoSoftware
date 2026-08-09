# EspecificaÃ§Ã£o do Frontend Web â€” LicenciamentoSoftware

**VersÃ£o:** 1.0  
**Fase:** 9  
**Projeto:** `LicenciamentoSoftware.Web` + `LicenciamentoSoftware.Web.Server`  
**Stack:** Blazor WASM (.NET 10) + ASP.NET Core BFF + YARP  

---

## 1. VisÃ£o Geral

O frontend web Ã© um **portal de gestÃ£o** destinado aos clientes da plataforma (empresas que contratam o sistema de licenciamento). Cada empresa gerencia seus prÃ³prios dados de forma isolada â€” nenhum dado de uma empresa Ã© visÃ­vel para outra.

### 1.1 O que o portal permite

- Cadastrar a empresa e criar a conta de administrador
- Fazer login com e-mail, senha e segundo fator TOTP
- Gerenciar clientes finais, usuÃ¡rios internos e aplicaÃ§Ãµes
- Emitir e gerenciar licenÃ§as de software por tipo
- Gerar e renovar tokens HMAC para autenticaÃ§Ã£o dos aplicativos clientes
- Monitorar sessÃµes ativas e instalaÃ§Ãµes registradas
- Executar operaÃ§Ãµes manuais: renovar perÃ­odo, encerrar sessÃ£o, liberar instalaÃ§Ã£o

### 1.2 O que o portal NÃƒO faz

- **NÃ£o Ã© o portal do cliente final** â€” o usuÃ¡rio final (Pedro da Empresa XYZ) nÃ£o usa este portal. Ele usa o aplicativo cliente que valida licenÃ§a via API HMAC
- **NÃ£o tem painel de administraÃ§Ã£o de plataforma** â€” nÃ£o existe um superadmin que gerencia todas as empresas
- **NÃ£o expÃµe a API de validaÃ§Ã£o** â€” os endpoints `/api/validacao/*` sÃ£o usados pelos aplicativos, nÃ£o pelo portal

---

## 2. Arquitetura

### 2.1 Projetos envolvidos

```
LicenciamentoSoftware.Web          Blazor WASM â€” pÃ¡ginas e componentes (roda no browser)
LicenciamentoSoftware.Web.Server   BFF â€” serve o WASM, gerencia cookies, proxy para API
LicenciamentoSoftware.Client       Biblioteca HTTP â€” DTOs e services compartilhados
```

### 2.2 Fluxo de comunicaÃ§Ã£o

```
Browser
  â”‚  Baixa os arquivos WASM (.js, .wasm, .dll)
  â”‚  e executa o C# localmente
  â”‚
  â†• HTTPS / mesma origem (localhost:7152)
  â”‚
Web.Server (BFF)
  â”‚  â”œâ”€â”€ Serve os arquivos estÃ¡ticos do WASM
  â”‚  â”œâ”€â”€ POST /bff/login      â†’ autentica, emite cookie HttpOnly
  â”‚  â”œâ”€â”€ POST /bff/login/2fa  â†’ valida TOTP, emite cookie HttpOnly
  â”‚  â”œâ”€â”€ POST /bff/refresh    â†’ renova access token via cookie
  â”‚  â”œâ”€â”€ POST /bff/logout     â†’ revoga sessÃ£o, apaga cookie
  â”‚  â”œâ”€â”€ POST /bff/cadastrar  â†’ proxy pÃºblico para /auth/cadastrar
  â”‚  â””â”€â”€ /* (demais rotas)    â†’ YARP repassa para a API com Authorization header
  â”‚
  â†• HTTPS (localhost:7075)
  â”‚
API (LicenciamentoSoftware.Api)
     â””â”€â”€ Processa as requisiÃ§Ãµes com JWT Bearer
```

### 2.3 SeguranÃ§a dos tokens

| Token | Onde fica | Acesso JavaScript |
|---|---|---|
| Access Token (JWT, 60 min) | MemÃ³ria C# no WASM | ImpossÃ­vel â€” nunca serializado |
| Refresh Token | Cookie `HttpOnly; Secure; SameSite=Strict` | ImpossÃ­vel â€” HttpOnly |

**Por que BFF?**  
Em SPA puro o refresh token precisaria de `localStorage` (exposto a XSS) ou `sessionStorage` (sem persistÃªncia). O BFF elimina esse problema: o cookie HttpOnly Ã© invisÃ­vel para qualquer JavaScript, inclusive o prÃ³prio cÃ³digo da aplicaÃ§Ã£o.

**Fluxo de renovaÃ§Ã£o silenciosa:**  
Quando o access token expira (401), o `BearerTokenHandler` chama automaticamente `POST /bff/refresh`. O browser envia o cookie automaticamente (mesma origem). Um novo access token Ã© emitido sem que o usuÃ¡rio perceba.

---

## 3. Estrutura de arquivos

```
src/LicenciamentoSoftware.Web/
â”œâ”€â”€ App.razor                          Roteador principal com AuthorizeRouteView
â”œâ”€â”€ _Imports.razor                     Usings globais
â”œâ”€â”€ Program.cs                         Bootstrap: DI, HttpClients, AuthProvider
â”‚
â”œâ”€â”€ Layout/
â”‚   â”œâ”€â”€ MainLayout.razor               Shell autenticado (sidebar + conteÃºdo)
â”‚   â””â”€â”€ PublicLayout.razor             Shell pÃºblico (login, cadastro, totp)
â”‚
â”œâ”€â”€ Shared/
â”‚   â”œâ”€â”€ ApiError.razor                 Exibe erros de validaÃ§Ã£o da API (400/409/422)
â”‚   â”œâ”€â”€ ConfirmDialog.razor            Modal de confirmaÃ§Ã£o para aÃ§Ãµes destrutivas
â”‚   â”œâ”€â”€ Modal.razor                    Modal genÃ©rico reutilizÃ¡vel com ChildContent + Footer
â”‚   â”œâ”€â”€ Paginacao.razor                Componente de paginaÃ§Ã£o numÃ©rica
â”‚   â””â”€â”€ RedirectToLogin.razor         Redireciona para /login quando nÃ£o autenticado
â”‚
â”œâ”€â”€ Pages/
â”‚   â”œâ”€â”€ Index.razor                    Redirect para /clientes-finais ou /login
â”‚   â”œâ”€â”€ Login.razor                    FormulÃ¡rio de login (layout pÃºblico)
â”‚   â”œâ”€â”€ Totp.razor                     VerificaÃ§Ã£o de segundo fator TOTP
â”‚   â”œâ”€â”€ Cadastro.razor                 Cadastro pÃºblico de empresa + responsÃ¡vel
â”‚   â”œâ”€â”€ Logout.razor                   Encerra sessÃ£o e redireciona
â”‚   â”‚
â”‚   â”œâ”€â”€ ClientesFinais/
â”‚   â”‚   â””â”€â”€ Lista.razor               Grid de cards + modal criar/editar/desativar
â”‚   â”œâ”€â”€ Usuarios/
â”‚   â”‚   â””â”€â”€ Lista.razor               Grid de cards + modal criar/editar/desativar
â”‚   â”œâ”€â”€ Aplicacoes/
â”‚   â”‚   â””â”€â”€ Lista.razor               Grid de cards + modal criar/editar/desativar
â”‚   â””â”€â”€ Licencas/
â”‚       â””â”€â”€ Lista.razor               Grid de cards + modal emitir + modal detalhe
â”‚
â”œâ”€â”€ Services/
â”‚   â”œâ”€â”€ JwtAuthStateProvider.cs       AuthenticationStateProvider â€” access token em memÃ³ria
â”‚   â”œâ”€â”€ BearerTokenHandler.cs         DelegatingHandler â€” adiciona Authorization header
â”‚   â”œâ”€â”€ TokenRefreshHandler.cs        DelegatingHandler â€” renova token silenciosamente em 401
â”‚   â””â”€â”€ ApiHttpClientFactory.cs       Factory singleton â€” HttpClients com DefaultRequestHeaders
â”‚
â””â”€â”€ wwwroot/
    â”œâ”€â”€ index.html                     Entry point HTML do WASM
    â””â”€â”€ css/
        â””â”€â”€ app.css                    Estilos globais (sidebar, cards, badges, modais)

src/LicenciamentoSoftware.Web.Server/
â”œâ”€â”€ Program.cs                         Bootstrap: BFF, YARP, cookie policy
â”œâ”€â”€ appsettings.json                   ApiSettings:BaseUrl, ReverseProxy config
â”œâ”€â”€ Configuration/
â”‚   â””â”€â”€ BffServiceExtensions.cs       Helpers de configuraÃ§Ã£o (cookie policy, HttpClients)
â””â”€â”€ Controllers/
    â””â”€â”€ BffController.cs              Endpoints /bff/* (login, 2fa, refresh, logout, cadastrar)

src/LicenciamentoSoftware.Client/
â”œâ”€â”€ Models/
â”‚   â”œâ”€â”€ Auth/                         LoginRequest, LoginResponse, AutoCadastroRequest
â”‚   â”œâ”€â”€ Common/                       PagedResult<T>
â”‚   â”œâ”€â”€ ClientesFinais/               ClienteFinalResult, CriarClienteFinalRequest, Atualizar...
â”‚   â”œâ”€â”€ Usuarios/                     UsuarioResult, CriarUsuarioRequest, Atualizar...
â”‚   â”œâ”€â”€ Aplicacoes/                   AplicacaoResult, CriarAplicacaoRequest, Atualizar...
â”‚   â”œâ”€â”€ TiposLicenca/                 TipoLicencaResult
â”‚   â””â”€â”€ Licencas/                     LicencaResult, EmitirLicencaRequest, RenovarPeriodoRequest, TokenInfoResult
â””â”€â”€ Services/
    â”œâ”€â”€ AuthApiService.cs             login, verify-2fa, refresh, logout, cadastrar
    â”œâ”€â”€ ClienteFinalApiService.cs     CRUD + paginaÃ§Ã£o
    â”œâ”€â”€ UsuarioApiService.cs          CRUD + paginaÃ§Ã£o
    â”œâ”€â”€ AplicacaoApiService.cs        CRUD + paginaÃ§Ã£o
    â”œâ”€â”€ TipoLicencaApiService.cs      listagem
    â””â”€â”€ LicencaApiService.cs          emitir, listar, desativar, renovar, sessÃµes, instalaÃ§Ãµes, token
```

---

## 4. AutenticaÃ§Ã£o e autorizaÃ§Ã£o

### 4.1 Fluxo de login

```
1. POST /bff/login {email, senha}
   â”œâ”€â”€ Sucesso sem 2FA â†’ BFF emite cookie HttpOnly + retorna {accessToken, nome, papel}
   â”‚   WASM: JwtAuthStateProvider.MarcarAutenticado() â†’ redireciona para /
   â””â”€â”€ Requer 2FA â†’ BFF retorna {requer2FA: true, tokenTemporario}
       WASM: redireciona para /totp?token=...

2. POST /bff/login/2fa {tokenTemporario, codigo}
   â””â”€â”€ Sucesso â†’ BFF emite cookie HttpOnly + retorna {accessToken, nome, papel}
       WASM: JwtAuthStateProvider.MarcarAutenticado() â†’ redireciona para /
```

### 4.2 RenovaÃ§Ã£o silenciosa

```
Qualquer requisiÃ§Ã£o autenticada
  â†’ BearerTokenHandler adiciona Authorization: Bearer {accessToken}
  â†’ Se retornar 401:
      TokenRefreshHandler chama POST /bff/refresh
      Browser envia cookie automaticamente
      BFF valida cookie â†’ retorna novo accessToken
      JwtAuthStateProvider.AtualizarToken()
      RequisiÃ§Ã£o original Ã© retentada com novo token
```

### 4.3 ProteÃ§Ã£o de rotas

Todas as pÃ¡ginas protegidas usam `@attribute [Authorize]`. O `App.razor` usa `AuthorizeRouteView` com `<NotAuthorized>` que renderiza `<RedirectToLogin />`, redirecionando para `/login?returnUrl=...`.

### 4.4 Logout

```
POST /bff/logout
  â†’ BFF revoga refresh token na API
  â†’ BFF apaga cookie
  â†’ JwtAuthStateProvider.MarcarDesautenticado()
  â†’ Redireciona para /login
```

---

## 5. PÃ¡ginas e funcionalidades

### 5.1 PÃ¡gina de Login (`/login`)

**Layout:** PublicLayout (sem sidebar)  
**Visual:** painel roxo esquerdo (branding) + formulÃ¡rio direito

**Campos:**
- E-mail (tipo email, autocomplete)
- Senha (tipo password, autocomplete)
- Enter submete o formulÃ¡rio

**Fluxo:**
1. UsuÃ¡rio preenche credenciais e clica "Entrar"
2. Se bem-sucedido â†’ redireciona para `returnUrl` ou `/`
3. Se 2FA necessÃ¡rio â†’ redireciona para `/totp?token=...`
4. Se erro â†’ exibe mensagem inline

**Links:** "Cadastre-se gratuitamente" â†’ `/cadastro`

---

### 5.2 PÃ¡gina de VerificaÃ§Ã£o TOTP (`/totp`)

**Layout:** PublicLayout  
**ParÃ¢metro de query:** `token` (token temporÃ¡rio de desafio 2FA)

**Campos:**
- CÃ³digo TOTP (6 dÃ­gitos, `autocomplete="one-time-code"`)

**Fluxo:**
1. UsuÃ¡rio digita cÃ³digo do autenticador
2. POST `/bff/login/2fa` â†’ se vÃ¡lido, autentica e redireciona para `/`
3. Se invÃ¡lido â†’ exibe erro inline

---

### 5.3 PÃ¡gina de Cadastro (`/cadastro`)

**Layout:** PublicLayout  
**Acesso:** PÃºblico (anÃ´nimo)

**Campos â€” Dados da empresa:**
- RazÃ£o Social *
- Tipo de inscriÃ§Ã£o (CPF / CNPJ)
- NÃºmero CPF/CNPJ *
- E-mail da empresa *
- Telefone (opcional)

**Campos â€” ResponsÃ¡vel pela conta:**
- Nome completo *
- E-mail de acesso *
- Senha * (mÃ­nimo 8 caracteres)
- Confirmar senha *

**Fluxo:**
1. FormulÃ¡rio validado no cliente (senhas coincidem, campos obrigatÃ³rios)
2. POST `/bff/cadastrar` â†’ proxy para `POST /auth/cadastrar`
3. Cria `Cliente` + primeiro `UsuÃ¡rio` como `AdministradorCliente` em uma transaÃ§Ã£o
4. Sucesso â†’ tela de confirmaÃ§Ã£o com link para `/login`
5. Conflito (409) â†’ erro de CPF/CNPJ ou e-mail duplicado
6. InvÃ¡lido (422) â†’ lista de erros de validaÃ§Ã£o

---

### 5.4 Clientes Finais (`/clientes-finais`)

**Layout:** MainLayout (autenticado)  
**Acesso:** Qualquer usuÃ¡rio autenticado

**Listagem:**
- Grid de cards (3 colunas em desktop)
- Busca por razÃ£o social (Enter ou botÃ£o)
- PaginaÃ§Ã£o numÃ©rica
- Card exibe: razÃ£o social, badge ativo/inativo, e-mail, telefone, CPF/CNPJ

**Criar (modal "Novo Cliente"):**
- RazÃ£o Social *
- Tipo (CPF/CNPJ) + NÃºmero *
- E-mail *
- Telefone

**Editar (modal "Editar Cliente"):**
- RazÃ£o Social, E-mail, Telefone (CPF/CNPJ nÃ£o editÃ¡vel apÃ³s criaÃ§Ã£o)

**Desativar:**
- ConfirmDialog antes de executar
- ExclusÃ£o lÃ³gica (`ativo = false`)

**Isolamento de tenant:** `IdCliente` vem do JWT, nunca do formulÃ¡rio.

---

### 5.5 UsuÃ¡rios (`/usuarios`)

**Layout:** MainLayout  
**Acesso:** Qualquer usuÃ¡rio autenticado

**Listagem:**
- Grid de cards com avatar de iniciais
- Card exibe: nome, e-mail, papel, badge ativo/inativo

**Criar (modal "Novo UsuÃ¡rio"):**
- Nome *, E-mail *, Senha *

**Editar (modal "Editar UsuÃ¡rio"):**
- Nome, E-mail (senha nÃ£o editÃ¡vel via ediÃ§Ã£o)

**ObservaÃ§Ã£o:** O `IdCliente` vem do JWT. Todos os usuÃ¡rios criados pertencem ao mesmo tenant.

---

### 5.6 AplicaÃ§Ãµes (`/aplicacoes`)

**Layout:** MainLayout  
**Acesso:** Qualquer usuÃ¡rio autenticado

**Listagem:**
- Grid de cards
- Card exibe: tÃ­tulo, tipo de licenÃ§a (badge colorido), descriÃ§Ã£o, status

**Criar (modal "Nova AplicaÃ§Ã£o"):**
- TÃ­tulo *, DescriÃ§Ã£o, Tipo de LicenÃ§a * (dropdown carregado de `/tipos-licenca`)

**Editar (modal "Editar AplicaÃ§Ã£o"):**
- TÃ­tulo, DescriÃ§Ã£o (tipo nÃ£o editÃ¡vel apÃ³s criaÃ§Ã£o)

**Badges de tipo:**

| Tipo | Cor |
|---|---|
| Permanente | Azul |
| Por PerÃ­odo | Amarelo |
| Por UsuÃ¡rios | Roxo |
| Por InstalaÃ§Ã£o | Rosa |

---

### 5.7 LicenÃ§as (`/licencas`)

**Layout:** MainLayout  
**Acesso:** Qualquer usuÃ¡rio autenticado

#### 5.7.1 Listagem

- Grid de cards
- Busca por cliente/aplicativo
- Card exibe: razÃ£o social do cliente final, aplicaÃ§Ã£o, tipo (badge), status, resumo (perÃ­odo/usuÃ¡rios/instalaÃ§Ãµes), chave abreviada

#### 5.7.2 EmissÃ£o (modal "Nova LicenÃ§a")

**Passo 1 â€” SeleÃ§Ã£o:**
- Dropdown de AplicaÃ§Ã£o (carregado com tipo embutido no label)
- Dropdown de Cliente Final
- Tipo detectado automaticamente ao selecionar a aplicaÃ§Ã£o

**Passo 2 â€” Campos dinÃ¢micos por tipo:**

| Tipo | Campos extras |
|---|---|
| Permanente | Nenhum |
| Por PerÃ­odo | Data inÃ­cio *, Data fim *, RenovaÃ§Ã£o automÃ¡tica (checkbox) |
| Por UsuÃ¡rios | MÃ¡x. usuÃ¡rios *, MÃ¡x. sessÃµes por usuÃ¡rio |
| Por InstalaÃ§Ã£o | MÃ¡x. instalaÃ§Ãµes * |

**OpÃ§Ã£o:** "Gerar token HMAC junto com a licenÃ§a" (checkbox)

**PÃ³s-emissÃ£o:**
- Tela de sucesso dentro do modal
- Se token foi gerado: exibe valor com botÃ£o "Copiar"
- Aviso: "Exibido uma Ãºnica vez. Guarde com seguranÃ§a."

#### 5.7.3 Detalhe (modal "Detalhe da LicenÃ§a")

Abre ao clicar "Detalhes" em qualquer card da lista. Carrega o detalhe completo da licenÃ§a via `GET /licencas/{id}`.

**ConteÃºdo do modal (adapta-se ao tipo):**

**CabeÃ§alho:**
- Nome do cliente final + aplicaÃ§Ã£o
- Badges: status (Ativa/Inativa) + tipo de licenÃ§a

**SeÃ§Ã£o PerÃ­odo** (apenas tipo "Por PerÃ­odo"):
- Datas inÃ­cio/fim, renovaÃ§Ã£o automÃ¡tica
- Campo "Nova data fim" + botÃ£o "Renovar perÃ­odo"

**SeÃ§Ã£o UsuÃ¡rios** (apenas tipo "Por UsuÃ¡rios"):
- MÃ¡x. simultÃ¢neos, sessÃµes por usuÃ¡rio

**SeÃ§Ã£o SessÃµes ativas** (quando hÃ¡ sessÃµes):
- Tabela: identificador do usuÃ¡rio, data login, Ãºltima atividade
- BotÃ£o "Encerrar" por linha (com ConfirmDialog)

**SeÃ§Ã£o InstalaÃ§Ãµes** (quando hÃ¡ instalaÃ§Ãµes):
- Tabela: identificador da mÃ¡quina, data de registro
- BotÃ£o "Liberar" por linha (com ConfirmDialog)
- Contador: ativas/mÃ¡ximo

**SeÃ§Ã£o Token HMAC:**
- Status: "Sem token" / "Ativo Â· exp. dd/MM/yyyy" / "Expirado"
- BotÃ£o "Gerar token" (se sem token ou expirado)
- BotÃ£o "Renovar token" (se ativo â€” revoga o anterior)
- ApÃ³s gerar/renovar: exibe valor com botÃ£o "Copiar" e aviso de exibiÃ§Ã£o Ãºnica

---

### 5.8 Dashboard (`/dashboard`)

**Layout:** MainLayout (autenticado) â€” primeiro item do menu

**Carregamento:** resumo e alertas carregados em paralelo via `Task.WhenAll`. Skeleton loader exibido durante carregamento.

**Cards de mÃ©tricas (sempre visÃ­veis apÃ³s carregamento):**

| Card | MÃ©trica | Alerta visual |
|---|---|---|
| Clientes Finais | Total ativos + novos 30d | â€” |
| AplicaÃ§Ãµes | Total ativas | â€” |
| LicenÃ§as Ativas | Total + inativas | â€” |
| Expirando em 7 dias | Contagem | Laranja se > 0 |
| SessÃµes abertas agora | Total ativas | â€” |
| Tokens expirando em 7 dias | Contagem | Laranja se > 0 |
| Novas licenÃ§as (30 dias) | Contagem | â€” |

**Breakdown por tipo:** badges coloridos mostrando Permanente / Por PerÃ­odo / Por UsuÃ¡rios / Por InstalaÃ§Ã£o.

**SeÃ§Ã£o de alertas** (oculta quando nÃ£o hÃ¡ dados):
- Erros de validaÃ§Ã£o nas Ãºltimas 24h com breakdown por motivo
- LicenÃ§as no limite de capacidade (usuÃ¡rios ou instalaÃ§Ãµes)
- SessÃµes inativas prolongadas (> 2Ã— TempoLimiteSessaoHoras)
- InstalaÃ§Ãµes adormecidas (> 30 dias sem validaÃ§Ã£o)

**Componente `MetricaCard.razor`:** reutilizÃ¡vel, aceita `Titulo`, `Valor`, `Subtitulo`, `Cor`, `Icone` e `Alerta`. Inclui skeleton loader via CSS animation.

---

## 6. Componentes compartilhados

### 6.1 Modal.razor

```razor
<Modal Visivel="@_aberto" Titulo="TÃ­tulo" OnFechar="Fechar">
    <ChildContent>
        <!-- conteÃºdo do formulÃ¡rio -->
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
- Largura mÃ¡xima de 560px (responsivo)

### 6.2 ConfirmDialog.razor

```csharp
_confirmDialog.Mensagem = "Mensagem de confirmaÃ§Ã£o";
bool confirmado = await _confirmDialog.MostrarAsync();
if (!confirmado) return;
// executa aÃ§Ã£o
```

- Bloqueante via `TaskCompletionSource<bool>`
- BotÃ£o de confirmaÃ§Ã£o vermelho, cancelar neutro

### 6.3 ApiError.razor

```razor
<ApiError Erro="@_erro" Erros="@_erros" />
```

- Exibe `Erro` (string Ãºnica) ou `Erros` (lista) como alert vermelho
- VisÃ­vel apenas quando hÃ¡ conteÃºdo

### 6.4 Paginacao.razor

```razor
<Paginacao PaginaAtual="_pagina" TotalPaginas="_totalPaginas"
           Total="_total" OnPaginaMudou="MudarPagina" />
```

- Exibe atÃ© 5 pÃ¡ginas numeradas com `...` implÃ­cito
- Texto "PÃ¡gina X de Y (Z itens)"
- Oculto quando `TotalPaginas <= 1`

---

## 7. ServiÃ§os de autenticaÃ§Ã£o

### 7.1 JwtAuthStateProvider

Herda de `AuthenticationStateProvider`. ImplementaÃ§Ã£o central de autenticaÃ§Ã£o.

**Responsabilidades:**
- Armazena `AccessToken` em campo privado (never serialized)
- Parseia claims do JWT payload (base64url, sem verificaÃ§Ã£o de assinatura â€” jÃ¡ validada pela API)
- Notifica Blazor quando o estado muda (`NotifyAuthenticationStateChanged`)
- ExpÃµe `AccessToken` para os handlers HTTP
- MantÃ©m referÃªncia ao `ApiHttpClientFactory` para atualizar headers em todos os clients

**MÃ©todos pÃºblicos:**
- `MarcarAutenticado(token, nome, papel)` â€” chamado apÃ³s login bem-sucedido
- `AtualizarToken(token, nome, papel)` â€” chamado apÃ³s refresh silencioso
- `MarcarDesautenticado()` â€” chamado no logout

### 7.2 ApiHttpClientFactory

Singleton que cria e mantÃ©m um `HttpClient` para cada service. Quando o token muda, atualiza o `DefaultRequestHeaders.Authorization` de todos os clients de uma vez.

**Por que singleton?**
Em WASM o escopo do browser Ã© toda a vida da pÃ¡gina. Usar `Scoped` criaria uma nova instÃ¢ncia a cada navegaÃ§Ã£o de pÃ¡gina, perdendo os tokens nos headers.

### 7.3 BearerTokenHandler

`DelegatingHandler` que adiciona `Authorization: Bearer {token}` em toda requisiÃ§Ã£o sainte dos services. Usa `JwtAuthStateProvider.AccessToken` â€” sempre a versÃ£o mais atual em memÃ³ria.

### 7.4 TokenRefreshHandler

`DelegatingHandler` que intercepta respostas 401. Faz `POST /bff/refresh`, obtÃ©m novo token, chama `JwtAuthStateProvider.AtualizarToken()` e retenta a requisiÃ§Ã£o original.

---

## 8. ConfiguraÃ§Ã£o de portas (desenvolvimento)

| Projeto | HTTP | HTTPS |
|---|---|---|
| `LicenciamentoSoftware.Api` | 5016 | 7075 |
| `LicenciamentoSoftware.Web` | 5075 | 7153 (sem browser) |
| `LicenciamentoSoftware.Web.Server` | 5074 | **7152** â† acesso do usuÃ¡rio |

O usuÃ¡rio acessa sempre `https://localhost:7152`.  
O `Web.Server` faz proxy interno para a API em `https://localhost:7075`.  
O projeto `Web` nÃ£o precisa estar em Start â€” o `Web.Server` jÃ¡ inclui seus arquivos na build.

---

## 9. CSS e identidade visual

**Paleta principal:**

| Uso | Cor |
|---|---|
| Cor primÃ¡ria (botÃµes, sidebar ativa, logo) | `#6c63ff` (roxo) |
| Background da pÃ¡gina | `#f8f9fa` (cinza claro) |
| Cards | `#ffffff` (branco) com borda `#e9ecef` |
| Texto principal | `#212529` |
| Texto secundÃ¡rio | `#6c757d` |

**Badges de status:**

| Badge | Background | Texto |
|---|---|---|
| Ativo | `#d1fae5` | `#065f46` (verde) |
| Inativo | `#f3f4f6` | `#6b7280` (cinza) |
| Permanente | `#dbeafe` | `#1e40af` (azul) |
| Por PerÃ­odo | `#fef3c7` | `#92400e` (amarelo) |
| Por UsuÃ¡rios | `#ede9fe` | `#5b21b6` (roxo) |
| Por InstalaÃ§Ã£o | `#fce7f3` | `#9d174d` (rosa) |

**Classes CSS principais:**

| Classe | DescriÃ§Ã£o |
|---|---|
| `.app-shell` | Flex container principal (sidebar + conteÃºdo) |
| `.sidebar` | Sidebar fixa 220px |
| `.main-content` | Ãrea de conteÃºdo com `margin-left: 220px` |
| `.page-header-row` | Flex entre tÃ­tulo e botÃ£o de aÃ§Ã£o |
| `.cards-grid` | Grid responsivo `minmax(280px, 1fr)` |
| `.item-card` | Card branco com bordas e hover shadow |
| `.badge-pill` | Badge arredondado para status e tipos |
| `.modal-overlay` | Overlay fixo com fundo semitransparente |
| `.modal-card` | Container do modal com flex-column e scroll |
| `.search-bar` | Input de busca estilizado |

---

## 10. Fluxo completo â€” exemplo de uso

### 10.1 Primeiro acesso (novo cliente)

```
1. Acessa https://localhost:7152
2. NÃ£o autenticado â†’ redireciona para /login
3. Clica "Cadastre-se" â†’ /cadastro
4. Preenche dados da empresa + responsÃ¡vel â†’ POST /bff/cadastrar
5. Conta criada â†’ tela de sucesso â†’ clica "Ir para login"
6. Login com e-mail + senha â†’ POST /bff/login
7. Se 2FA nÃ£o configurado â†’ autenticado diretamente
8. Se 2FA configurado â†’ redireciona para /totp
9. Dashboard em /clientes-finais
```

### 10.2 Emitir uma licenÃ§a com token

```
1. Acessa /licencas
2. Clica "+ Nova LicenÃ§a"
3. Modal abre â€” seleciona AplicaÃ§Ã£o (ex: "Meu CRM (Por UsuÃ¡rios)")
4. Seleciona Cliente Final
5. Campos de usuÃ¡rios aparecem: mÃ¡x. 10 simultÃ¢neos
6. Marca "Gerar token HMAC"
7. Clica "Emitir licenÃ§a"
8. Modal mostra: "LicenÃ§a emitida com sucesso!"
9. Token exibido com botÃ£o "Copiar"
10. Administrador copia o token e configura no software cliente
11. Fecha o modal
```

### 10.3 Gerenciar sessÃµes ativas

```
1. Na lista de licenÃ§as, clica "Detalhes" em uma licenÃ§a "Por UsuÃ¡rios"
2. Modal abre com dados completos
3. SeÃ§Ã£o "SessÃµes ativas" mostra as 3 sessÃµes abertas
4. Clica "Encerrar" na sessÃ£o do usuÃ¡rio "pedro@empresa.com"
5. ConfirmDialog: "Encerrar esta sessÃ£o?"
6. Confirma â†’ sessÃ£o encerrada imediatamente
7. Modal atualiza com 2 sessÃµes
8. Slot liberado para novo login
```

### 10.4 Renovar token expirado

```
1. Clica "Detalhes" em uma licenÃ§a
2. SeÃ§Ã£o "Token HMAC" mostra: "Expirado"
3. Clica "Gerar token"
4. Novo token gerado e exibido com botÃ£o "Copiar"
5. Administrador copia e atualiza a configuraÃ§Ã£o do software cliente
```

---

## 11. LimitaÃ§Ãµes conhecidas e trabalhos futuros

| LimitaÃ§Ã£o | Fase planejada |
|---|---|
| Portal do Cliente Final (ver suas prÃ³prias licenÃ§as) | Fase 9.2 |
| Login social (Google, Microsoft, GitHub) via OAuth | Fase 9.2 |
| Setup de TOTP via QR code no portal | Fase 9.2 |
| App Desktop/Mobile (MAUI) | Fase 10 |
| Deploy em produÃ§Ã£o com host real | Fase 11 |
