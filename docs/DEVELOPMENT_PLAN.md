# Plano de desenvolvimento

## CritÃ©rio de avanÃ§o

Cada etapa sÃ³ avanÃ§a quando compila, passa nos testes e nÃ£o cria dependÃªncia proibida pela arquitetura. NÃ£o haverÃ¡ implementaÃ§Ã£o de CRUD antes da base de seguranÃ§a, validaÃ§Ã£o e testes. O ciclo Ã© sempre Red â†’ Green â†’ Refactor.

## DecisÃµes registradas (Fase 0)

| DecisÃ£o | Escolha |
|---|---|
| Interfaces de administraÃ§Ã£o | Web (Blazor WASM), Desktop (MAUI Windows), Mobile (MAUI Android) |
| Paridade funcional entre interfaces | Sim â€” todas as interfaces tÃªm acesso completo |
| API de gestÃ£o | Uma REST Ãºnica consumida pelas trÃªs interfaces |
| Cliente HTTP compartilhado | `LicenciamentoSoftware.Client` â€” usado por Web e MAUI |
| AutenticaÃ§Ã£o da gestÃ£o | JWT local + 2FA TOTP (Google Authenticator / Authy) |
| Credencial da API de validaÃ§Ã£o | Token por licenÃ§a com expiraÃ§Ã£o automÃ¡tica + assinatura HMAC-SHA256 com timestamp |
| ProteÃ§Ã£o anti-replay | Janela de Â±5 minutos no timestamp da assinatura HMAC |
| RenovaÃ§Ã£o de token de licenÃ§a | Manual pelo portal (`AdministradorCliente`), invalida token anterior imediatamente |
| Isolamento por tenant | `IdCliente` sempre da identidade autenticada, nunca do body |
| Primeiro usuÃ¡rio da empresa | Recebe papel `AdministradorCliente` automaticamente |
| ExclusÃ£o | Sempre lÃ³gica (`Ativo = false`), nunca fÃ­sica |
| Auditoria | Transacional, via interceptor EF Core, registra diff de campos |
| Jobs | `BackgroundService` com interface `IScheduledJob` (migrÃ¡vel para Hangfire/Quartz) |
| DistribuiÃ§Ã£o Web | Oracle Cloud VM â€” Nginx serve o Blazor WASM estÃ¡tico + BFF (ASP.NET Core) |
| DistribuiÃ§Ã£o Desktop | Instalador direto (Windows) |
| DistribuiÃ§Ã£o Mobile | Google Play Store (Android) |
| Banco de dados | PostgreSQL local na Oracle Cloud VM (mesma VM da API) |

---

## Fase 1 â€” FundaÃ§Ã£o da soluÃ§Ã£o âœ… ConcluÃ­da

**Objetivo:** estrutura de projetos, build centralizado e regras de dependÃªncia verificÃ¡veis por teste.

1. Criar os oito projetos: `Domain`, `Application`, `Infrastructure`, `Api`, `Client`, `Web`, `Maui` e os trÃªs projetos de teste.
2. Configurar `Directory.Build.props` com nullable, warnings como erro e analyzers.
3. Incluir xUnit, FluentAssertions e Testcontainers nos projetos de teste.
4. Escrever teste de arquitetura (NetArchTest) que impede `Application` de depender de `Infrastructure` ou `Api`.
5. Configurar tratamento global de erros, ProblemDetails, logging estruturado e health check na API.
6. Configurar Docker Compose com PostgreSQL local e secrets por ambiente.

**Testes mÃ­nimos:** soluÃ§Ã£o compila sem warnings; health check retorna 200; teste de arquitetura passa.

**Demo:** `dotnet build` verde na raiz; `dotnet test` passa com o teste de arquitetura; health check acessÃ­vel.

---

## Fase 2 â€” DomÃ­nio e schema âœ… ConcluÃ­da

**Objetivo:** modelar todas as entidades com invariantes testÃ¡veis e gerar o schema no banco.

1. Modelar entidades e value objects no `Domain` sem qualquer dependÃªncia de EF Core.
2. Implementar mÃ©todos de negÃ³cio: `Desativar`, `AtualizarDados`, `CriarSessao`, `EncerrarSessao`.
3. Escrever testes de invariantes: limites positivos, `DataInicio < DataFim`, vÃ­nculo de cliente, estado ativo.
4. Mapear EF Core com Fluent API em `Infrastructure`: chaves, Ã­ndices, unique constraints e seed dos 4 tipos de licenÃ§a.
5. Gerar migration inicial e validar contra PostgreSQL real via Testcontainers.

**Testes mÃ­nimos:** invariantes de domÃ­nio â€” limite negativo lanÃ§a exceÃ§Ã£o; data invÃ¡lida lanÃ§a exceÃ§Ã£o; migration sobe em banco vazio; seeds e constraints verificados por integraÃ§Ã£o.

**Demo:** suite de testes do domÃ­nio verde sem nenhuma referÃªncia a EF Core; migration aplica sem erro em banco limpo.

---

## Fase 3 â€” Identidade e auditoria âœ… ConcluÃ­da

**Objetivo:** autenticaÃ§Ã£o JWT com 2FA TOTP, isolamento por tenant e auditoria transacional.

1. Implementar endpoints de autenticaÃ§Ã£o: `POST /auth/register`, `POST /auth/login`, `POST /auth/verify-2fa`, `POST /auth/refresh`, `POST /auth/logout`.
2. Implementar geraÃ§Ã£o de segredo TOTP e QR code para ativaÃ§Ã£o do autenticador.
3. Implementar JWT com claims de tenant (`IdCliente`) e papel; refresh token rotacionÃ¡vel armazenado como hash.
4. Implementar `ICurrentUser` com tenant isolado â€” nunca confiar em `IdCliente` do body.
5. Configurar polÃ­ticas de autorizaÃ§Ã£o: `AdministradorPlataforma`, `AdministradorCliente`, `OperadorCliente`, `Leitor`.
6. Implementar interceptor EF Core para auditoria: detecta inserÃ§Ãµes/atualizaÃ§Ãµes/desativaÃ§Ãµes, captura diff em JSON, persiste na mesma transaÃ§Ã£o.
7. Implementar `IAuditLogWriter` como porta da aplicaÃ§Ã£o.
8. Proteger documentaÃ§Ã£o Swagger fora de ambiente de desenvolvimento.

**Testes mÃ­nimos:** login sem 2FA nega; TOTP invÃ¡lido nega; token expirado retorna 401; usuÃ¡rio de tenant A nÃ£o acessa tenant B; alteraÃ§Ã£o gera log com diff correto.

**Demo:** fluxo completo via Swagger â€” registro â†’ login â†’ scan QR no Google Authenticator â†’ verificar cÃ³digo TOTP â†’ receber JWT â†’ acessar endpoint protegido â†’ verificar log de auditoria gerado.

---

## Fase 4 â€” SeguranÃ§a da API de validaÃ§Ã£o âœ… ConcluÃ­da

**Objetivo:** token por licenÃ§a com expiraÃ§Ã£o automÃ¡tica e assinatura HMAC anti-replay.

1. Implementar geraÃ§Ã£o de token por licenÃ§a no momento da emissÃ£o (secret armazenado como hash).
2. Implementar middleware de validaÃ§Ã£o HMAC-SHA256: verifica assinatura + timestamp (janela Â±5 minutos).
3. Implementar rejeiÃ§Ã£o de replay: mesma assinatura nÃ£o pode ser reutilizada dentro da janela.
4. Implementar `POST /auth/licenca/renovar-token` para rotaÃ§Ã£o manual pelo `AdministradorCliente`.
5. Implementar expiraÃ§Ã£o automÃ¡tica de token (configurÃ¡vel por licenÃ§a).
6. Configurar rate limiting nos endpoints de validaÃ§Ã£o.

**Testes mÃ­nimos:** requisiÃ§Ã£o sem assinatura retorna 401; timestamp fora da janela retorna 401; replay rejeitado; token expirado retorna 401; token renovado invalida o anterior.

**Demo:** script de teste faz chamada assinada com HMAC â†’ validaÃ§Ã£o autorizada; mesma chamada repetida â†’ rejeitada; token renovado â†’ chamada com token antigo rejeitada.

---

## Fase 5 â€” CRUDs de gestÃ£o, um agregado por vez âœ… ConcluÃ­da

**Objetivo:** casos de uso de gestÃ£o para todos os agregados base, seguindo Clean Architecture.

Ordem: `Cliente` (+ primeiro admin) â†’ `Usuario` â†’ `ClienteFinal` â†’ `Aplicacao` â†’ `TipoLicenca` (somente leitura).

Para cada agregado:
1. Escrever testes do caso de uso (Red primeiro).
2. Criar command/query e validator (FluentValidation) na `Application`.
3. Criar handler e interface de repositÃ³rio especÃ­fica.
4. Implementar repositÃ³rio EF Core.
5. Criar controller fino e testes de API.

Requisitos transversais:
- PaginaÃ§Ã£o, filtros e ordenaÃ§Ã£o em todas as listagens.
- Sem `GenericRepository`, sem `ManagementService`, sem controller com `DbContext`.
- Toda escrita gera entrada no `LogOperacao` via interceptor.

**Testes mÃ­nimos:** validaÃ§Ãµes de negÃ³cio; handler retorna `NotFound` para inexistente; `403` para tenant errado; controller retorna cÃ³digos HTTP corretos.

**Demo:** fluxo via Swagger â€” criar empresa + admin â†’ logar com 2FA â†’ criar cliente final â†’ criar aplicaÃ§Ã£o â†’ listar com filtro e paginaÃ§Ã£o â†’ verificar log de auditoria.

---

## Fase 6 â€” EmissÃ£o e gestÃ£o de licenÃ§as âœ… ConcluÃ­da

**Objetivo:** emissÃ£o de licenÃ§a com detalhes por tipo e operaÃ§Ãµes manuais de manutenÃ§Ã£o.

1. Implementar emissÃ£o de licenÃ§a: validar tenant, vÃ­nculo cliente final + aplicaÃ§Ã£o ao mesmo tenant, bloco de detalhe correto por tipo, gerar token HMAC.
2. Tratar constraint de licenÃ§a ativa Ãºnica com `409 Conflict`.
3. Implementar operaÃ§Ãµes manuais com endpoints prÃ³prios: encerrar sessÃ£o, liberar instalaÃ§Ã£o, renovar perÃ­odo, desabilitar licenÃ§a, renovar token.
4. Implementar endpoints de histÃ³rico: sessÃµes, instalaÃ§Ãµes registradas, alteraÃ§Ãµes da licenÃ§a.
5. Garantir que histÃ³rico nunca apaga registros fÃ­sicos.

**Testes mÃ­nimos:** tipo errado de detalhe retorna erro de validaÃ§Ã£o; licenÃ§a duplicada retorna 409; operaÃ§Ãµes manuais exigem `AdministradorCliente`; histÃ³rico retorna registros anteriores apÃ³s desativaÃ§Ã£o.

**Demo:** emitir licenÃ§a Por UsuÃ¡rios â†’ copiar token â†’ chamar `/validar-login` com HMAC â†’ ver sessÃ£o ativa â†’ encerrar sessÃ£o manualmente â†’ vaga liberada imediatamente.

---

## Fase 7 â€” API de validaÃ§Ã£o completa âœ… ConcluÃ­da

**Objetivo:** todos os endpoints de validaÃ§Ã£o com regras de negÃ³cio, operaÃ§Ãµes atÃ´micas e testes de concorrÃªncia.

1. âœ… Implementar `POST /validar-login`: Por UsuÃ¡rios (limite de simultÃ¢neos + por usuÃ¡rio), transaÃ§Ã£o serializÃ¡vel.
2. âœ… Implementar `POST /heartbeat`: atualiza `DataUltimaAtividade`.
3. âœ… Implementar `POST /logout`: encerra sessÃ£o explicitamente (idempotente).
4. âœ… Implementar `POST /validar-instalacao`: Por InstalaÃ§Ã£o, idempotente para mÃ¡quina jÃ¡ registrada, transaÃ§Ã£o serializÃ¡vel.
5. âœ… Integrar validaÃ§Ã£o Permanente e Por PerÃ­odo ao fluxo.
6. âœ… Escrever testes de concorrÃªncia: mÃºltiplas requisiÃ§Ãµes simultÃ¢neas para o Ãºltimo slot.

**Resultado:** 33 novos testes. AutenticaÃ§Ã£o HMAC em dois passos (BCrypt + HMAC-SHA256). Headers: `X-Token`, `X-Timestamp`, `X-Signature`, `X-Nonce`.

---

## Fase 8 â€” Jobs agendados âœ… ConcluÃ­da

**Objetivo:** rotinas automÃ¡ticas de manutenÃ§Ã£o como `BackgroundService`, rotaÃ§Ã£o de tokens e notificaÃ§Ãµes por e-mail.

1. âœ… Implementar interface `IScheduledJob` (migrÃ¡vel futuramente para Hangfire/Quartz).
2. âœ… Job de sessÃµes inativas: encerra `LicencaSessao` sem heartbeat alÃ©m de `TempoLimiteSessaoHoras`.
3. âœ… Job de expiraÃ§Ã£o: marca licenÃ§as Por PerÃ­odo vencidas sem renovaÃ§Ã£o automÃ¡tica como inativas.
4. âœ… Job de renovaÃ§Ã£o automÃ¡tica: estende `DataFim` de licenÃ§as com `RenovacaoAutomatica = true`.
5. âœ… Job de rotaÃ§Ã£o automÃ¡tica: renova tokens HMAC prÃ³ximos do vencimento via `RenovarTokenLicencaHandler`.
6. âœ… Job de notificaÃ§Ã£o: envia e-mail HTML ao `AdministradorCliente` para licenÃ§as e tokens prÃ³ximos de vencer.
7. âœ… Templates HTML embarcados no assembly (`EmbeddedResource`): `LicencaExpirando`, `TokenExpirando`, `TokenRenovado`.
8. âœ… `SmtpEmailService` via MailKit 4.17.0, configurÃ¡vel por `appsettings` + secrets, desabilitado por padrÃ£o.

**Resultado:** 15 novos testes. 207 testes no total. Todos os intervalos configurÃ¡veis via `JobSettings`. E-mail desabilitado por padrÃ£o (`Habilitado: false`).

---

## Fase 9 â€” Frontend Web (Blazor WASM) âœ… ConcluÃ­da

**Objetivo:** interface web completa com BFF para gestÃ£o de licenÃ§as.

1. âœ… Implementar BFF (`LicenciamentoSoftware.Web.Server`) com proxy reverso YARP e endpoints `/bff/*` para autenticaÃ§Ã£o via cookie HttpOnly.
2. âœ… Implementar `LicenciamentoSoftware.Client` â€” biblioteca HTTP compartilhada com todos os services e DTOs da API.
3. âœ… Fluxo de login + 2FA TOTP com JWT em memÃ³ria (access token) e cookie HttpOnly (refresh token via BFF).
4. âœ… Layout com sidebar adaptÃ¡vel, proteÃ§Ã£o de rotas e redirecionamento para login.
5. âœ… Telas CRUD em grid de cards com modais inline: Clientes Finais, UsuÃ¡rios, AplicaÃ§Ãµes.
6. âœ… GestÃ£o de licenÃ§as: emissÃ£o por tipo (Permanente/PerÃ­odo/UsuÃ¡rios/InstalaÃ§Ã£o), detalhe com sessÃµes ativas, instalaÃ§Ãµes registradas e operaÃ§Ãµes manuais (renovar perÃ­odo, encerrar sessÃ£o, liberar instalaÃ§Ã£o) â€” tudo em modais.
7. âœ… Token HMAC: gerar, renovar e copiar token no modal de detalhe da licenÃ§a com exibiÃ§Ã£o Ãºnica.
8. âœ… Auto-cadastro pÃºblico (`POST /bff/cadastrar`) â€” cria empresa + primeiro usuÃ¡rio (AdministradorCliente) em uma transaÃ§Ã£o.
9. âœ… Badges coloridos por tipo de licenÃ§a, feedback visual em aÃ§Ãµes destrutivas.
10. âœ… Pipeline CI atualizado para `.NET 10` e testes passando.

**Resultado:** 207 testes aprovados. TrÃªs projetos implementados: `Web.Server` (BFF + YARP), `Web` (Blazor WASM), `Client` (HTTP services). Nenhuma pÃ¡gina de formulÃ¡rio separada â€” tudo em modais inline.

**Demo:** login com 2FA funciona; emitir licenÃ§a com token HMAC; copiar token; ver sessÃµes ativas; encerrar sessÃ£o manualmente; gerar/renovar token no detalhe da licenÃ§a.

---

## Fase 9.1 â€” Dashboard Web + InstrumentaÃ§Ã£o de MÃ©tricas

**Objetivo:** adicionar tela de Dashboard ao portal Web com mÃ©tricas gerais e alertas operacionais, instrumentando o backend para coletar os dados necessÃ¡rios. A mesma API de mÃ©tricas serÃ¡ reutilizada pelo MAUI na Fase 10.

1. âœ… **[#41]** Migration V004 â€” adicionar `data_ultima_validacao` em `licenca_instalacao_registrada` e criar tabela `validacao_log` (tipo_operacao, resultado, motivo_erro, ip_origem, criado_em).
2. âœ… **[#42]** Instrumentar API de validaÃ§Ã£o â€” gravar `validacao_log` em todos os handlers (`ValidarLoginHandler`, `ValidarInstalacaoHandler`, `HeartbeatHandler`, `LogoutValidacaoHandler`) e atualizar `data_ultima_validacao` nas instalaÃ§Ãµes. `IpOrigem` adicionado a todos os commands.
3. âœ… **[#43]** Endpoint `GET /dashboard/resumo` â€” mÃ©tricas gerais do tenant: total de clientes finais, aplicaÃ§Ãµes, licenÃ§as ativas/inativas por tipo, licenÃ§as expirando em 7 dias, sessÃµes abertas, tokens expirando, novos cadastros nos Ãºltimos 30 dias. Implementado com CTEs PostgreSQL em uma Ãºnica query.
4. âœ… **[#44]** Endpoint `GET /dashboard/alertas` â€” alertas operacionais: sessÃµes inativas prolongadas, instalaÃ§Ãµes adormecidas (>30 dias sem validaÃ§Ã£o), licenÃ§as no limite de capacidade (usuÃ¡rios/instalaÃ§Ãµes), erros de validaÃ§Ã£o nas Ãºltimas 24h com breakdown por motivo e top 5 licenÃ§as.
5. âœ… **[#45]** Dashboard Web â€” pÃ¡gina inicial do portal com 7 cards de mÃ©tricas (Clientes, AplicaÃ§Ãµes, LicenÃ§as, Expirando, SessÃµes, Tokens, Novos), seÃ§Ã£o de alertas oculta quando nÃ£o hÃ¡ dados, carregamento paralelo, componente `MetricaCard` reutilizÃ¡vel com skeleton loader.
6. âœ… **[#46]** Atualizar documentaÃ§Ã£o â€” `WEB_SPECIFICATION.md`, `ARCHITECTURE.md`, `DEVELOPMENT_PLAN.md` e `README.md` com Fase 9.1 concluÃ­da.

**Resultado:** 211 testes aprovados. Backend instrumentado para coleta de mÃ©tricas. Dashboard Web com visÃ£o operacional em tempo real do tenant.

**Demo:** apÃ³s login, dashboard exibe mÃ©tricas do tenant; seÃ§Ã£o de alertas aparece quando hÃ¡ sessÃµes inativas ou erros de validaÃ§Ã£o; cards de licenÃ§as expirando ficam em laranja quando hÃ¡ dados.

---
6. **[#46]** Atualizar documentaÃ§Ã£o â€” `WEB_SPECIFICATION.md`, `ARCHITECTURE.md`, `DEVELOPMENT_PLAN.md` e `README.md` com Fase 9.1 concluÃ­da.

**Testes mÃ­nimos:** handler de dashboard retorna dados corretos isolados por tenant; mÃ©tricas de erro retornam 0 quando log estÃ¡ vazio; dashboard oculta seÃ§Ã£o de alertas quando nÃ£o hÃ¡ dados.

**Demo:** dashboard exibe mÃ©tricas reais do tenant; alerta aparece quando sessÃ£o estÃ¡ inativa por tempo excessivo; grÃ¡fico de erros mostra pico apÃ³s tentativas de validaÃ§Ã£o invÃ¡lidas.

---

## Fase 10 â€” MAUI Desktop e Mobile âœ… ConcluÃ­da

**Objetivo:** aplicativo MAUI com paridade funcional ao Blazor Web, para Windows e Android.

1. âœ… **[#24]** Configurar projeto MAUI com `CommunityToolkit.Mvvm 8.4.0`, `MauiApiClientFactory`, `MauiAuthService` (SecureStorage), Shell com flyout, guard de rotas, Views de autenticaÃ§Ã£o (Login, TOTP, Cadastro) e Converters.
2. âœ… **[#25]** Implementar telas de gestÃ£o com paridade ao Blazor: Dashboard (7 mÃ©tricas + alertas), Clientes Finais, UsuÃ¡rios, AplicaÃ§Ãµes (lista paginada + formulÃ¡rio overlay), LicenÃ§as (lista + detalhe), Emitir LicenÃ§a (wizard 3 passos), Controls reutilizÃ¡veis (MetricaCardView, ConfirmPopup).
3. âœ… Build aprovado: Windows (0 erros) e Android (0 erros, 1 warning prÃ©-existente).
4. âœ… Projeto de testes `LicenciamentoSoftware.Maui.Tests` com 46 testes aprovados.

**Resultado:** 253 testes aprovados (207 backend + 46 MAUI). Paridade funcional completa com o portal Web. Targets: `net10.0-windows10.0.19041.0` e `net10.0-android`.

**Demo:** login com 2FA â†’ dashboard com mÃ©tricas do tenant â†’ emitir licenÃ§a via wizard â†’ ver sessÃµes ativas â†’ encerrar sessÃ£o â†’ renovar token HMAC.

---

## Fase 11 â€” CI/CD e Infraestrutura âœ… ConcluÃ­da

**Objetivo:** pipeline completo de CI/CD e deploy automatizado para todos os componentes.

1. âœ… **[#26]** GitHub Actions CI â€” restore â†’ build â†’ testes unitÃ¡rios (Domain, Application, MAUI) â†’ testes de integraÃ§Ã£o (Testcontainers). Falha bloqueia merge.
2. âœ… **[#27]** Deploy da API na Oracle Cloud VM â€” Ubuntu 24.04, `.NET 10`, `systemd` service, `Nginx` como reverse proxy, deploy via SSH + rsync no push para `master`.
3. âœ… Deploy do Blazor WASM na Oracle VM â€” Nginx serve os assets estÃ¡ticos, SPA routing configurado, cache agressivo para assets imutÃ¡veis.
4. âœ… `appsettings.Production.json` â€” CORS apontando para `licensemanager.enzojb.com.br`, logs em `/var/log/licenciamento/`, jobs com intervalos de produÃ§Ã£o.
5. âœ… Script `setup-vm.sh` â€” configura VM do zero: instala .NET 10, Nginx, cria usuÃ¡rio de service, cria systemd unit, configura ufw.
6. âœ… Script `setup-github-secrets.ps1` â€” cria todos os secrets necessÃ¡rios via GitHub CLI.
7. âœ… DNS via Cloudflare: `licensemanager.enzojb.com.br` e `licensemanager-api.enzojb.com.br` apontando para a VM (Proxied â€” SSL e CDN automÃ¡ticos).
8. âœ… **[#65]** CabeÃ§alhos de seguranÃ§a HTTP no Nginx â€” `X-Frame-Options`, `X-Content-Type-Options`, `X-XSS-Protection`, `Referrer-Policy`, `Content-Security-Policy`, `HSTS`.
9. âœ… **[#66]** Dependabot habilitado â€” varredura semanal de NuGet e GitHub Actions, PRs automÃ¡ticos agrupados.

**Infraestrutura:**
- API: Oracle Cloud VM (Ubuntu 24.04, .NET 10, systemd)
- Web: Oracle Cloud VM Nginx estÃ¡tico
- Banco: PostgreSQL local na Oracle Cloud VM (`localhost:5432`)
- DNS/SSL/CDN: Cloudflare

**GitHub Secrets necessÃ¡rios:** `SSH_HOST`, `SSH_PORT`, `SSH_USER`, `SSH_KEY`, `DB_CONNECTION_STRING`, `JWT_SECRET`, `HMAC_SECRET`

**Resultado:** pipeline CI/CD completo, deploy automÃ¡tico em produÃ§Ã£o a cada push no `master`, infraestrutura segura com cabeÃ§alhos HTTP e varredura automÃ¡tica de vulnerabilidades.

---

## LGPD â€” Conformidade âœ… ConcluÃ­da

**Objetivo:** adequar o sistema Ã  Lei Geral de ProteÃ§Ã£o de Dados (Lei 13.709/2018).

1. âœ… **[#60]** Registro de consentimento no cadastro (Art. 7 e 8) â€” checkbox obrigatÃ³rio na tela de cadastro; campos `lgpd_aceito`, `lgpd_aceito_em`, `lgpd_ip_origem` na tabela `usuario` (migration V005); IP do titular registrado automaticamente.
2. âœ… **[#61]** PÃ¡ginas pÃºblicas de PolÃ­tica de Privacidade (`/privacidade`) e Termos de Uso (`/termos`) â€” acessÃ­veis sem autenticaÃ§Ã£o, com conteÃºdo exigido pelo Art. 9; links no rodapÃ© de login e cadastro.
3. âœ… **[#62]** Mecanismo de exclusÃ£o e anonimizaÃ§Ã£o de conta (Art. 18) â€” `POST /usuarios/minha-conta/excluir` com confirmaÃ§Ã£o de senha; `AdministradorCliente` tem dados substituÃ­dos pelos dados da empresa; demais papÃ©is tÃªm conta desativada e dados anonimizados; refresh tokens revogados; page `/minha-conta` com modal de confirmaÃ§Ã£o.

**Resultado:** 381 testes aprovados (107 Domain + 226 Application + 48 MAUI). Dados pessoais protegidos conforme LGPD.

---

## Backlog posterior

- **Painel de mÃ©tricas e observabilidade centralizada** â€” Prometheus + Grafana ou Application Insights
- **MigraÃ§Ã£o dos jobs para Hangfire/Quartz** â€” painel de monitoramento, retry automÃ¡tico, histÃ³rico de execuÃ§Ãµes
- **Suporte a iOS no MAUI** â€” requer conta Apple Developer
- **PaginaÃ§Ã£o cursor-based** para listagens de alto volume
- **Fila de e-mails com retry automÃ¡tico** â€” para garantia de entrega em caso de falha SMTP
- **MÃºltiplos provedores de e-mail** â€” SendGrid, Amazon SES, alÃ©m de SMTP genÃ©rico

---

## Fase 12 â€” Melhorias do portal e adequaÃ§Ãµes legais âœ… ConcluÃ­da

**Objetivo:** adequaÃ§Ãµes legais, funcionalidades de perfil e qualidade de testes.

1. âœ… **[#95]** CNPJ alfanumÃ©rico â€” `Inscricao.cs` atualizado para aceitar letras nas 8 primeiras posiÃ§Ãµes conforme IN RFB 2.229/2024. Algoritmo de validaÃ§Ã£o atualizado (A=10...Z=35). Testes de domÃ­nio com casos alfanumÃ©ricos validados.
2. âœ… **[#96]** Setup e gerenciamento de 2FA TOTP no portal Web â€” seÃ§Ã£o em `/minha-conta` com QR code para ativar autenticador, confirmaÃ§Ã£o do primeiro cÃ³digo e desativaÃ§Ã£o com cÃ³digo TOTP.
3. âœ… **[#97]** Setup e gerenciamento de 2FA TOTP no app MAUI â€” tela "Minha Conta" com seÃ§Ã£o de ativaÃ§Ã£o (segredo copiÃ¡vel), confirmaÃ§Ã£o com cÃ³digo e desativaÃ§Ã£o com cÃ³digo. Implementado na Fase 12.1.
4. âœ… **[#98]** Tela de perfil da empresa â€” pÃ¡gina `/minha-empresa` separada no menu lateral para `AdministradorCliente`, com ediÃ§Ã£o de razÃ£o social, e-mail e telefone do cliente via `PUT /clientes/{id}`.
5. âœ… **[#100]** Testes de integraÃ§Ã£o no CI â€” etapa no `ci.yml` com Testcontainers + PostgreSQL bloqueia merge se falhar.

**Resultado:** 388 testes aprovados (340 backend + 48 MAUI). CNPJ alfanumÃ©rico conforme IN RFB 2.229/2024. 2FA TOTP gerenciÃ¡vel pelo usuÃ¡rio nas trÃªs interfaces.

---

## Fase 12.1 â€” Encerramento de conta de empresa âœ… ConcluÃ­da

**Objetivo:** permitir que um `AdministradorCliente` encerre a conta da empresa no portal, com bloqueio de operaÃ§Ãµes, notificaÃ§Ã£o aos clientes finais e limpeza automÃ¡tica posterior.

1. âœ… **[#124]** PÃ¡gina `/minha-empresa` separada no menu lateral para `AdministradorCliente` â€” ediÃ§Ã£o de razÃ£o social, e-mail e telefone do cliente (reutiliza `PUT /clientes/{id}`).
2. âœ… **[#125]** BotÃ£o "Encerrar conta" na pÃ¡gina `/minha-empresa` â€” modal com senha + checkbox "Excluir dados imediatamente"; `POST /clientes/{id}/encerrar`; desativa o cliente (`Ativo = false`), define `encerrado_em` e `exclusao_programada_em`, revoga todos os refresh tokens do tenant.
3. âœ… **[#126]** Bloquear renovaÃ§Ã£o de token se empresa estiver inativa â€” `ValidacaoController.VerificarHmacAsync` verifica `Ativo` do cliente antes de renovar; retorna `401` se inativo.
4. âœ… **[#127]** Notificar clientes finais quando empresa encerrar conta â€” fire-and-forget no `EncerrarContaEmpresaHandler` envia template `EmpresaEncerrada` para cada `ClienteFinal` ativo do tenant.
5. âœ… **[#128]** Job de exclusÃ£o fÃ­sica â€” `ExcluirEmpresasEncerradasJob` (`BackgroundService`) exclui fisicamente clientes com `exclusao_programada_em <= now()`, ativado diariamente (`ExclusaoEmpresasIntervaloMinutos: 1440`).
6. âœ… **[#129]** Template de e-mail `EmpresaEncerrada` â€” HTML embarcado como `EmbeddedResource`, mesmo padrÃ£o dos templates existentes.
7. âœ… **[#97]** Setup/gerenciamento de 2FA TOTP no MAUI â€” tela "Minha Conta" com seÃ§Ã£o de ativaÃ§Ã£o (segredo copiÃ¡vel), confirmaÃ§Ã£o com cÃ³digo e desativaÃ§Ã£o com cÃ³digo.
8. âœ… Migration V007 â€” `encerrado_em TIMESTAMPTZ` e `exclusao_programada_em TIMESTAMPTZ` na tabela `cliente`.

**Resultado:** 388 testes aprovados (340 backend + 48 MAUI). Encerramento de conta com exclusÃ£o imediata ou em 90 dias. Bloqueio automÃ¡tico da API de validaÃ§Ã£o para empresas encerradas. Paridade Web + MAUI para gestÃ£o de empresa e 2FA.

**Demo:** AdministradorCliente acessa `/minha-empresa` â†’ clica "Encerrar conta" â†’ confirma senha e marca checkbox de exclusÃ£o imediata â†’ conta bloqueada na hora â†’ clientes finais notificados por e-mail â†’ tentativa de validaÃ§Ã£o HMAC retorna 401 â†’ job noturno exclui os dados fisicamente.

---

## Fase 13 â€” Instaladores âœ… ConcluÃ­da

**Objetivo:** distribuiÃ§Ã£o do app MAUI para Windows e Android sem dependÃªncia de lojas.

1. âœ… **[#101]** Workflow `build-msix.yml` â€” roda em `windows-latest`, gera certificado autoassinado via `New-SelfSignedCertificate`, compila MAUI com `/p:WindowsPackageType=MSIX`, publica `.msix` + `.cer` como artefato (retenÃ§Ã£o 90 dias). Trigger: push em `master` (paths do MAUI) + `workflow_dispatch`.
2. âœ… **[#102]** Workflow `build-android.yml` â€” roda em `ubuntu-latest`, instala workload `maui-android`, restaura keystore do secret `ANDROID_KEYSTORE_BASE64`, compila MAUI com `AndroidPackageFormats=apk`, assina com `apksigner`, verifica assinatura e publica APK como artefato. Trigger: push em `master` (paths do MAUI) + `workflow_dispatch`.
3. âœ… `docs/instalacao-windows.md` â€” instruÃ§Ãµes para instalar o certificado autoassinado (GUI e PowerShell) e o MSIX, incluindo atualizaÃ§Ã£o, desinstalaÃ§Ã£o e soluÃ§Ã£o de problemas.
4. âœ… `docs/instalacao-android.md` â€” instruÃ§Ãµes para habilitar fontes desconhecidas (Android 7 e 8+), instalar via gerenciador de arquivos e ADB, atualizaÃ§Ã£o, desinstalaÃ§Ã£o e soluÃ§Ã£o de problemas.
5. âœ… `scripts/setup-github-secrets.ps1` â€” adicionados os 4 secrets Android (`ANDROID_KEYSTORE_BASE64`, `ANDROID_KEY_ALIAS`, `ANDROID_KEY_PASSWORD`, `ANDROID_STORE_PASSWORD`); novo parÃ¢metro `-ApenasAndroid` para configurar apenas os secrets do instalador.

**DecisÃµes:**
- Certificado autoassinado no Windows: sem custo, adequado para distribuiÃ§Ã£o direta. `/p:WindowsPackageType=MSIX` passado na linha de comando â€” o `.csproj` mantÃ©m `None` para dev local.
- APK para sideload (nÃ£o AAB): distribuiÃ§Ã£o direta sem Google Play Store conforme escopo da fase.
- O build local do MAUI Android pode falhar no pre-commit por lock de arquivo do VS/emulador; os workflows de CI rodam em runners limpos sem esse problema.

**Resultado:** 388 testes aprovados (inalterado â€” nenhum cÃ³digo C# foi modificado). Dois novos workflows independentes para geraÃ§Ã£o de instaladores. Artefatos disponÃ­veis em Actions a cada push no `master`.

**Demo:** disparar `workflow_dispatch` em `build-msix.yml` â†’ baixar artefato â†’ instalar `.cer` â†’ instalar `.msix` â†’ app abre no Windows. Disparar `build-android.yml` apÃ³s configurar os 4 secrets â†’ baixar APK â†’ instalar no Android via sideload.

---

## Fase 13 â€” Instaladores (seÃ§Ã£o original â€” ver âœ… acima)

**Objetivo:** distribuiÃ§Ã£o do app MAUI para Windows e Android sem dependÃªncia de lojas.

1. âœ… **[#101]** Instalador MSIX para Windows â€” pacote assinado com certificado autoassinado para distribuiÃ§Ã£o direta. Incluir instruÃ§Ãµes de instalaÃ§Ã£o do certificado.
2. âœ… **[#102]** APK/AAB assinado para Android â€” keystore guardado como GitHub Secret, APK pronto para distribuiÃ§Ã£o direta.

---

## Fase 14 â€” SDKs cliente (linguagens principais) âœ… ConcluÃ­da

**Objetivo:** bibliotecas prontas para uso nas linguagens mais relevantes do mercado, encapsulando a autenticaÃ§Ã£o HMAC e os endpoints de validaÃ§Ã£o.

Cada SDK implementa:
- GeraÃ§Ã£o de HMAC-SHA256 com timestamp, nonce e assinatura
- Chamadas aos 4 endpoints de validaÃ§Ã£o (login, heartbeat, logout, instalaÃ§Ã£o)
- Retry automÃ¡tico em falhas de rede (3 tentativas, backoff exponencial)
- Modelos de resposta tipados
- Testes unitÃ¡rios (HMAC + endpoints mockados)

1. âœ… **[#108]** SDK C#/.NET â€” `sdks/csharp/`. Multi-target `net6.0;net8.0;net10.0`. 12 testes xUnit aprovados.
2. âœ… **[#109]** SDK Java/Kotlin â€” `sdks/java/`. Maven, OkHttp + Jackson, JUnit 5 + MockWebServer.
3. âœ… **[#110]** SDK Python â€” `sdks/python/`. PyPI-ready, requests + HMAC stdlib, pytest + responses.
4. âœ… **[#111]** SDK JavaScript + TypeScript â€” `sdks/javascript/`. npm, fetch nativo (Node+browser), Jest.
5. âœ… **[#139]** SDK Rust â€” `sdks/rust/`. crates.io-ready, tokio + reqwest async, wiremock para testes.
6. âœ… **[#140]** SDK Ruby â€” `sdks/ruby/`. RubyGems, net/http + OpenSSL stdlib, RSpec + WebMock.

**Workflow CI:** `sdk-tests.yml` â€” roda todos os 6 SDKs em `ubuntu-latest` em jobs paralelos no push ao `master` ou mudanÃ§a em `sdks/**`.

**Resultado:** 12 testes C# aprovados localmente. Demais SDKs validados pelo CI (`sdk-tests.yml`) ao fazer merge no master.

**Demo:** `dotnet test sdks/csharp/LicenseManagerSdk.Tests` â†’ 12/12 aprovados.

---

## Fase 15 â€” SDKs cliente (linguagens secundÃ¡rias) âœ… ConcluÃ­da

**Objetivo:** atender sistemas legados e outras linguagens relevantes no mercado brasileiro.

1. âœ… **[#112]** SDK Delphi â€” `sdks/delphi/LicenseManagerSdk.pas`. Unit Pascal pura, sem dependÃªncias externas. CompatÃ­vel com Delphi 10.4 Sydney+. Usa `System.Net.HttpClient`, `System.Hash`, `System.JSON` da RTL padrÃ£o. DistribuiÃ§Ã£o: arquivo Ãºnico copiado para o projeto.
2. âœ… **[#113]** SDK PHP â€” `sdks/php/`. Pacote Composer `carloscampos2014/licensemanager-sdk`. PHP 7.4+, `ext-curl` + `ext-json`. PSR-4, testes PHPUnit. Suporte a PHP 7.4, 8.1 e 8.3 no CI.
3. âœ… **[#114]** SDK VB6 â€” `sdks/vb6/`. DLL COM feita em C# (`net48` + `netstandard2.0`). Interface `ILicenseManagerClient` com `[ComVisible(true)]`, ProgId `LicenseManagerSdk.LicenseManagerClient`. Registro via `regasm /tlb`. 7 testes xUnit aprovados localmente.

**Workflow CI:** `sdk-tests.yml` atualizado â€” adicionados jobs `sdk-php` (PHP 7.4/8.1/8.3) e `sdk-vb6` (C# net10.0).

**Resultado:** 7/7 testes VB6 aprovados localmente. PHP e VB6 validados pelo CI ao fazer merge no master.

**Demo:** `dotnet test sdks/vb6/LicenseManagerSdk.Tests` â†’ 7/7 aprovados.

---

## Fase 16 â€” Painel de AdministraÃ§Ã£o da Plataforma âœ… ConcluÃ­da

**Objetivo:** painel de monitoramento acessÃ­vel via SSH tunnel para o operador da plataforma monitorar o sistema em tempo real, sem exposiÃ§Ã£o pÃºblica.

Acesso: `ssh -L 16000:localhost:5020 <vm>` â†’ `http://localhost:16000`

1. âœ… **[#116]** Projeto `LicenciamentoSoftware.Admin` â€” aplicaÃ§Ã£o ASP.NET Core na porta `5020` (`localhost` only). HTTP Basic Auth (`Admin:Usuario` / `Admin:Senha`). Consulta o banco diretamente via Dapper + `DbConnectionFactory`. Deploy automÃ¡tico junto com a API (`deploy-api.yml`).
2. âœ… **[#117]** MÃ©tricas globais â€” clientes (total/ativos/encerrados), usuÃ¡rios ativos, licenÃ§as (ativas/inativas/expirando), sessÃµes abertas, validaÃ§Ãµes (24h / 7 dias), erros por motivo, Ãºltimos 20 logins (hora + IP), tamanho do banco, status API/BFF (ping nos `/health`). PÃ¡gina HTML com Bootstrap, auto-refresh a cada 30 segundos.
3. âœ… **[#120]** Backup do banco â€” script `setup-backup.sh`: cron diÃ¡rio Ã s 2h UTC, `pg_dump | gzip`, retenÃ§Ã£o de 7 dias em `/opt/backups/`. Painel exibe data/hora do Ãºltimo backup, tamanho e status (ðŸŸ¢/ðŸ”´). Endpoint `POST /backup/executar` dispara backup manual via tunnel.
4. âœ… **[#121]** SSH tunnel documentado no `README.md` e em `.kiro/steering/vm-oracle.md`. Service `licenciamento-admin.service` instalado via `setup-admin.sh`. Porta 5020 verificada â€” nÃ£o exposta pelo Nginx nem pelo ufw. Script `ssh-tunnels.ps1` atualizado com o tunnel `localhost:16000 â†’ 5020` e adicionado ao `.gitignore`.

**Demo:** `ssh -L 16000:localhost:5020 ...` â†’ `http://localhost:16000` â†’ painel com mÃ©tricas em tempo real; botÃ£o "Executar backup agora" dispara `pg_dump` remoto via tunnel.

---

## Fase 17 — Segurança de conta e paridade MAUI

**Objetivo:** fechar gaps de segurança de acesso (recuperação de senha, troca de senha, reset de 2FA) e garantir paridade funcional do app MAUI com o portal Web.

1. **[#168]** Reset do 2FA via Painel Admin — endpoint POST /admin/usuarios/{id}/reset-2fa + botão no painel Admin para operador redefinir o TOTP de usuário bloqueado.
2. **[#169]** Alterar própria senha — endpoint PUT /auth/minha-senha + seção em /minha-conta (Web) e MinhaContaPage (MAUI). Invalida refresh tokens ativos.
3. **[#170]** Recuperação de senha via e-mail — endpoints POST /auth/esqueci-senha + POST /auth/redefinir-senha, migration senha_redefinicao, template de e-mail, páginas no Web e link no MAUI.
4. **[#171]** Página "Minha Empresa" no MAUI — MinhaEmpresaPage.xaml + MinhaEmpresaViewModel.cs com edição de dados da empresa e encerramento de conta, paridade com /minha-empresa do Web.
