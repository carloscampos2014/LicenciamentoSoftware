# Plano de desenvolvimento

## Critério de avanço

Cada etapa só avança quando compila, passa nos testes e não cria dependência proibida pela arquitetura. Não haverá implementação de CRUD antes da base de segurança, validação e testes. O ciclo é sempre Red → Green → Refactor.

## Decisões registradas (Fase 0)

| Decisão | Escolha |
|---|---|
| Interfaces de administração | Web (Blazor WASM), Desktop (MAUI Windows), Mobile (MAUI Android) |
| Paridade funcional entre interfaces | Sim — todas as interfaces têm acesso completo |
| API de gestão | Uma REST única consumida pelas três interfaces |
| Cliente HTTP compartilhado | `LicenciamentoSoftware.Client` — usado por Web e MAUI |
| Autenticação da gestão | JWT local + 2FA TOTP (Google Authenticator / Authy) |
| Credencial da API de validação | Token por licença com expiração automática + assinatura HMAC-SHA256 com timestamp |
| Proteção anti-replay | Janela de ±5 minutos no timestamp da assinatura HMAC |
| Renovação de token de licença | Manual pelo portal (`AdministradorCliente`), invalida token anterior imediatamente |
| Isolamento por tenant | `IdCliente` sempre da identidade autenticada, nunca do body |
| Primeiro usuário da empresa | Recebe papel `AdministradorCliente` automaticamente |
| Exclusão | Sempre lógica (`Ativo = false`), nunca física |
| Auditoria | Transacional, via interceptor EF Core, registra diff de campos |
| Jobs | `BackgroundService` com interface `IScheduledJob` (migrável para Hangfire/Quartz) |
| Distribuição Web | Oracle Cloud VM — Nginx serve o Blazor WASM estático + BFF (ASP.NET Core) |
| Distribuição Desktop | Instalador direto (Windows) |
| Distribuição Mobile | Google Play Store (Android) |
| Banco de dados | PostgreSQL local na Oracle Cloud VM (mesma VM da API) |

---

## Fase 1 — Fundação da solução ✅ Concluída

**Objetivo:** estrutura de projetos, build centralizado e regras de dependência verificáveis por teste.

1. Criar os oito projetos: `Domain`, `Application`, `Infrastructure`, `Api`, `Client`, `Web`, `Maui` e os três projetos de teste.
2. Configurar `Directory.Build.props` com nullable, warnings como erro e analyzers.
3. Incluir xUnit, FluentAssertions e Testcontainers nos projetos de teste.
4. Escrever teste de arquitetura (NetArchTest) que impede `Application` de depender de `Infrastructure` ou `Api`.
5. Configurar tratamento global de erros, ProblemDetails, logging estruturado e health check na API.
6. Configurar Docker Compose com PostgreSQL local e secrets por ambiente.

**Testes mínimos:** solução compila sem warnings; health check retorna 200; teste de arquitetura passa.

**Demo:** `dotnet build` verde na raiz; `dotnet test` passa com o teste de arquitetura; health check acessível.

---

## Fase 2 — Domínio e schema ✅ Concluída

**Objetivo:** modelar todas as entidades com invariantes testáveis e gerar o schema no banco.

1. Modelar entidades e value objects no `Domain` sem qualquer dependência de EF Core.
2. Implementar métodos de negócio: `Desativar`, `AtualizarDados`, `CriarSessao`, `EncerrarSessao`.
3. Escrever testes de invariantes: limites positivos, `DataInicio < DataFim`, vínculo de cliente, estado ativo.
4. Mapear EF Core com Fluent API em `Infrastructure`: chaves, índices, unique constraints e seed dos 4 tipos de licença.
5. Gerar migration inicial e validar contra PostgreSQL real via Testcontainers.

**Testes mínimos:** invariantes de domínio — limite negativo lança exceção; data inválida lança exceção; migration sobe em banco vazio; seeds e constraints verificados por integração.

**Demo:** suite de testes do domínio verde sem nenhuma referência a EF Core; migration aplica sem erro em banco limpo.

---

## Fase 3 — Identidade e auditoria ✅ Concluída

**Objetivo:** autenticação JWT com 2FA TOTP, isolamento por tenant e auditoria transacional.

1. Implementar endpoints de autenticação: `POST /auth/register`, `POST /auth/login`, `POST /auth/verify-2fa`, `POST /auth/refresh`, `POST /auth/logout`.
2. Implementar geração de segredo TOTP e QR code para ativação do autenticador.
3. Implementar JWT com claims de tenant (`IdCliente`) e papel; refresh token rotacionável armazenado como hash.
4. Implementar `ICurrentUser` com tenant isolado — nunca confiar em `IdCliente` do body.
5. Configurar políticas de autorização: `AdministradorPlataforma`, `AdministradorCliente`, `OperadorCliente`, `Leitor`.
6. Implementar interceptor EF Core para auditoria: detecta inserções/atualizações/desativações, captura diff em JSON, persiste na mesma transação.
7. Implementar `IAuditLogWriter` como porta da aplicação.
8. Proteger documentação Swagger fora de ambiente de desenvolvimento.

**Testes mínimos:** login sem 2FA nega; TOTP inválido nega; token expirado retorna 401; usuário de tenant A não acessa tenant B; alteração gera log com diff correto.

**Demo:** fluxo completo via Swagger — registro → login → scan QR no Google Authenticator → verificar código TOTP → receber JWT → acessar endpoint protegido → verificar log de auditoria gerado.

---

## Fase 4 — Segurança da API de validação ✅ Concluída

**Objetivo:** token por licença com expiração automática e assinatura HMAC anti-replay.

1. Implementar geração de token por licença no momento da emissão (secret armazenado como hash).
2. Implementar middleware de validação HMAC-SHA256: verifica assinatura + timestamp (janela ±5 minutos).
3. Implementar rejeição de replay: mesma assinatura não pode ser reutilizada dentro da janela.
4. Implementar `POST /auth/licenca/renovar-token` para rotação manual pelo `AdministradorCliente`.
5. Implementar expiração automática de token (configurável por licença).
6. Configurar rate limiting nos endpoints de validação.

**Testes mínimos:** requisição sem assinatura retorna 401; timestamp fora da janela retorna 401; replay rejeitado; token expirado retorna 401; token renovado invalida o anterior.

**Demo:** script de teste faz chamada assinada com HMAC → validação autorizada; mesma chamada repetida → rejeitada; token renovado → chamada com token antigo rejeitada.

---

## Fase 5 — CRUDs de gestão, um agregado por vez ✅ Concluída

**Objetivo:** casos de uso de gestão para todos os agregados base, seguindo Clean Architecture.

Ordem: `Cliente` (+ primeiro admin) → `Usuario` → `ClienteFinal` → `Aplicacao` → `TipoLicenca` (somente leitura).

Para cada agregado:
1. Escrever testes do caso de uso (Red primeiro).
2. Criar command/query e validator (FluentValidation) na `Application`.
3. Criar handler e interface de repositório específica.
4. Implementar repositório EF Core.
5. Criar controller fino e testes de API.

Requisitos transversais:
- Paginação, filtros e ordenação em todas as listagens.
- Sem `GenericRepository`, sem `ManagementService`, sem controller com `DbContext`.
- Toda escrita gera entrada no `LogOperacao` via interceptor.

**Testes mínimos:** validações de negócio; handler retorna `NotFound` para inexistente; `403` para tenant errado; controller retorna códigos HTTP corretos.

**Demo:** fluxo via Swagger — criar empresa + admin → logar com 2FA → criar cliente final → criar aplicação → listar com filtro e paginação → verificar log de auditoria.

---

## Fase 6 — Emissão e gestão de licenças ✅ Concluída

**Objetivo:** emissão de licença com detalhes por tipo e operações manuais de manutenção.

1. Implementar emissão de licença: validar tenant, vínculo cliente final + aplicação ao mesmo tenant, bloco de detalhe correto por tipo, gerar token HMAC.
2. Tratar constraint de licença ativa única com `409 Conflict`.
3. Implementar operações manuais com endpoints próprios: encerrar sessão, liberar instalação, renovar período, desabilitar licença, renovar token.
4. Implementar endpoints de histórico: sessões, instalações registradas, alterações da licença.
5. Garantir que histórico nunca apaga registros físicos.

**Testes mínimos:** tipo errado de detalhe retorna erro de validação; licença duplicada retorna 409; operações manuais exigem `AdministradorCliente`; histórico retorna registros anteriores após desativação.

**Demo:** emitir licença Por Usuários → copiar token → chamar `/validar-login` com HMAC → ver sessão ativa → encerrar sessão manualmente → vaga liberada imediatamente.

---

## Fase 7 — API de validação completa ✅ Concluída

**Objetivo:** todos os endpoints de validação com regras de negócio, operações atômicas e testes de concorrência.

1. ✅ Implementar `POST /validar-login`: Por Usuários (limite de simultâneos + por usuário), transação serializável.
2. ✅ Implementar `POST /heartbeat`: atualiza `DataUltimaAtividade`.
3. ✅ Implementar `POST /logout`: encerra sessão explicitamente (idempotente).
4. ✅ Implementar `POST /validar-instalacao`: Por Instalação, idempotente para máquina já registrada, transação serializável.
5. ✅ Integrar validação Permanente e Por Período ao fluxo.
6. ✅ Escrever testes de concorrência: múltiplas requisições simultâneas para o último slot.

**Resultado:** 33 novos testes. Autenticação HMAC em dois passos (BCrypt + HMAC-SHA256). Headers: `X-Token`, `X-Timestamp`, `X-Signature`, `X-Nonce`.

---

## Fase 8 — Jobs agendados ✅ Concluída

**Objetivo:** rotinas automáticas de manutenção como `BackgroundService`, rotação de tokens e notificações por e-mail.

1. ✅ Implementar interface `IScheduledJob` (migrável futuramente para Hangfire/Quartz).
2. ✅ Job de sessões inativas: encerra `LicencaSessao` sem heartbeat além de `TempoLimiteSessaoHoras`.
3. ✅ Job de expiração: marca licenças Por Período vencidas sem renovação automática como inativas.
4. ✅ Job de renovação automática: estende `DataFim` de licenças com `RenovacaoAutomatica = true`.
5. ✅ Job de rotação automática: renova tokens HMAC próximos do vencimento via `RenovarTokenLicencaHandler`.
6. ✅ Job de notificação: envia e-mail HTML ao `AdministradorCliente` para licenças e tokens próximos de vencer.
7. ✅ Templates HTML embarcados no assembly (`EmbeddedResource`): `LicencaExpirando`, `TokenExpirando`, `TokenRenovado`.
8. ✅ `SmtpEmailService` via MailKit 4.17.0, configurável por `appsettings` + secrets, desabilitado por padrão.

**Resultado:** 15 novos testes. 207 testes no total. Todos os intervalos configuráveis via `JobSettings`. E-mail desabilitado por padrão (`Habilitado: false`).

---

## Fase 9 — Frontend Web (Blazor WASM) ✅ Concluída

**Objetivo:** interface web completa com BFF para gestão de licenças.

1. ✅ Implementar BFF (`LicenciamentoSoftware.Web.Server`) com proxy reverso YARP e endpoints `/bff/*` para autenticação via cookie HttpOnly.
2. ✅ Implementar `LicenciamentoSoftware.Client` — biblioteca HTTP compartilhada com todos os services e DTOs da API.
3. ✅ Fluxo de login + 2FA TOTP com JWT em memória (access token) e cookie HttpOnly (refresh token via BFF).
4. ✅ Layout com sidebar adaptável, proteção de rotas e redirecionamento para login.
5. ✅ Telas CRUD em grid de cards com modais inline: Clientes Finais, Usuários, Aplicações.
6. ✅ Gestão de licenças: emissão por tipo (Permanente/Período/Usuários/Instalação), detalhe com sessões ativas, instalações registradas e operações manuais (renovar período, encerrar sessão, liberar instalação) — tudo em modais.
7. ✅ Token HMAC: gerar, renovar e copiar token no modal de detalhe da licença com exibição única.
8. ✅ Auto-cadastro público (`POST /bff/cadastrar`) — cria empresa + primeiro usuário (AdministradorCliente) em uma transação.
9. ✅ Badges coloridos por tipo de licença, feedback visual em ações destrutivas.
10. ✅ Pipeline CI atualizado para `.NET 10` e testes passando.

**Resultado:** 207 testes aprovados. Três projetos implementados: `Web.Server` (BFF + YARP), `Web` (Blazor WASM), `Client` (HTTP services). Nenhuma página de formulário separada — tudo em modais inline.

**Demo:** login com 2FA funciona; emitir licença com token HMAC; copiar token; ver sessões ativas; encerrar sessão manualmente; gerar/renovar token no detalhe da licença.

---

## Fase 9.1 — Dashboard Web + Instrumentação de Métricas

**Objetivo:** adicionar tela de Dashboard ao portal Web com métricas gerais e alertas operacionais, instrumentando o backend para coletar os dados necessários. A mesma API de métricas será reutilizada pelo MAUI na Fase 10.

1. ✅ **[#41]** Migration V004 — adicionar `data_ultima_validacao` em `licenca_instalacao_registrada` e criar tabela `validacao_log` (tipo_operacao, resultado, motivo_erro, ip_origem, criado_em).
2. ✅ **[#42]** Instrumentar API de validação — gravar `validacao_log` em todos os handlers (`ValidarLoginHandler`, `ValidarInstalacaoHandler`, `HeartbeatHandler`, `LogoutValidacaoHandler`) e atualizar `data_ultima_validacao` nas instalações. `IpOrigem` adicionado a todos os commands.
3. ✅ **[#43]** Endpoint `GET /dashboard/resumo` — métricas gerais do tenant: total de clientes finais, aplicações, licenças ativas/inativas por tipo, licenças expirando em 7 dias, sessões abertas, tokens expirando, novos cadastros nos últimos 30 dias. Implementado com CTEs PostgreSQL em uma única query.
4. ✅ **[#44]** Endpoint `GET /dashboard/alertas` — alertas operacionais: sessões inativas prolongadas, instalações adormecidas (>30 dias sem validação), licenças no limite de capacidade (usuários/instalações), erros de validação nas últimas 24h com breakdown por motivo e top 5 licenças.
5. ✅ **[#45]** Dashboard Web — página inicial do portal com 7 cards de métricas (Clientes, Aplicações, Licenças, Expirando, Sessões, Tokens, Novos), seção de alertas oculta quando não há dados, carregamento paralelo, componente `MetricaCard` reutilizável com skeleton loader.
6. ✅ **[#46]** Atualizar documentação — `WEB_SPECIFICATION.md`, `ARCHITECTURE.md`, `DEVELOPMENT_PLAN.md` e `README.md` com Fase 9.1 concluída.

**Resultado:** 211 testes aprovados. Backend instrumentado para coleta de métricas. Dashboard Web com visão operacional em tempo real do tenant.

**Demo:** após login, dashboard exibe métricas do tenant; seção de alertas aparece quando há sessões inativas ou erros de validação; cards de licenças expirando ficam em laranja quando há dados.

---
6. **[#46]** Atualizar documentação — `WEB_SPECIFICATION.md`, `ARCHITECTURE.md`, `DEVELOPMENT_PLAN.md` e `README.md` com Fase 9.1 concluída.

**Testes mínimos:** handler de dashboard retorna dados corretos isolados por tenant; métricas de erro retornam 0 quando log está vazio; dashboard oculta seção de alertas quando não há dados.

**Demo:** dashboard exibe métricas reais do tenant; alerta aparece quando sessão está inativa por tempo excessivo; gráfico de erros mostra pico após tentativas de validação inválidas.

---

## Fase 10 — MAUI Desktop e Mobile ✅ Concluída

**Objetivo:** aplicativo MAUI com paridade funcional ao Blazor Web, para Windows e Android.

1. ✅ **[#24]** Configurar projeto MAUI com `CommunityToolkit.Mvvm 8.4.0`, `MauiApiClientFactory`, `MauiAuthService` (SecureStorage), Shell com flyout, guard de rotas, Views de autenticação (Login, TOTP, Cadastro) e Converters.
2. ✅ **[#25]** Implementar telas de gestão com paridade ao Blazor: Dashboard (7 métricas + alertas), Clientes Finais, Usuários, Aplicações (lista paginada + formulário overlay), Licenças (lista + detalhe), Emitir Licença (wizard 3 passos), Controls reutilizáveis (MetricaCardView, ConfirmPopup).
3. ✅ Build aprovado: Windows (0 erros) e Android (0 erros, 1 warning pré-existente).
4. ✅ Projeto de testes `LicenciamentoSoftware.Maui.Tests` com 46 testes aprovados.

**Resultado:** 253 testes aprovados (207 backend + 46 MAUI). Paridade funcional completa com o portal Web. Targets: `net10.0-windows10.0.19041.0` e `net10.0-android`.

**Demo:** login com 2FA → dashboard com métricas do tenant → emitir licença via wizard → ver sessões ativas → encerrar sessão → renovar token HMAC.

---

## Fase 11 — CI/CD e Infraestrutura ✅ Concluída

**Objetivo:** pipeline completo de CI/CD e deploy automatizado para todos os componentes.

1. ✅ **[#26]** GitHub Actions CI — restore → build → testes unitários (Domain, Application, MAUI) → testes de integração (Testcontainers). Falha bloqueia merge.
2. ✅ **[#27]** Deploy da API na Oracle Cloud VM — Ubuntu 24.04, `.NET 10`, `systemd` service, `Nginx` como reverse proxy, deploy via SSH + rsync no push para `master`.
3. ✅ Deploy do Blazor WASM na Oracle VM — Nginx serve os assets estáticos, SPA routing configurado, cache agressivo para assets imutáveis.
4. ✅ `appsettings.Production.json` — CORS apontando para `licensemanager.enzojb.com.br`, logs em `/var/log/licenciamento/`, jobs com intervalos de produção.
5. ✅ Script `setup-vm.sh` — configura VM do zero: instala .NET 10, Nginx, cria usuário de service, cria systemd unit, configura ufw.
6. ✅ Script `setup-github-secrets.ps1` — cria todos os secrets necessários via GitHub CLI.
7. ✅ DNS via Cloudflare: `licensemanager.enzojb.com.br` e `licensemanager-api.enzojb.com.br` apontando para a VM (Proxied — SSL e CDN automáticos).
8. ✅ **[#65]** Cabeçalhos de segurança HTTP no Nginx — `X-Frame-Options`, `X-Content-Type-Options`, `X-XSS-Protection`, `Referrer-Policy`, `Content-Security-Policy`, `HSTS`.
9. ✅ **[#66]** Dependabot habilitado — varredura semanal de NuGet e GitHub Actions, PRs automáticos agrupados.

**Infraestrutura:**
- API: Oracle Cloud VM (Ubuntu 24.04, .NET 10, systemd)
- Web: Oracle Cloud VM Nginx estático
- Banco: PostgreSQL local na Oracle Cloud VM (`localhost:5432`)
- DNS/SSL/CDN: Cloudflare

**GitHub Secrets necessários:** `SSH_HOST`, `SSH_PORT`, `SSH_USER`, `SSH_KEY`, `DB_CONNECTION_STRING`, `JWT_SECRET`, `HMAC_SECRET`

**Resultado:** pipeline CI/CD completo, deploy automático em produção a cada push no `master`, infraestrutura segura com cabeçalhos HTTP e varredura automática de vulnerabilidades.

---

## LGPD — Conformidade ✅ Concluída

**Objetivo:** adequar o sistema à Lei Geral de Proteção de Dados (Lei 13.709/2018).

1. ✅ **[#60]** Registro de consentimento no cadastro (Art. 7 e 8) — checkbox obrigatório na tela de cadastro; campos `lgpd_aceito`, `lgpd_aceito_em`, `lgpd_ip_origem` na tabela `usuario` (migration V005); IP do titular registrado automaticamente.
2. ✅ **[#61]** Páginas públicas de Política de Privacidade (`/privacidade`) e Termos de Uso (`/termos`) — acessíveis sem autenticação, com conteúdo exigido pelo Art. 9; links no rodapé de login e cadastro.
3. ✅ **[#62]** Mecanismo de exclusão e anonimização de conta (Art. 18) — `POST /usuarios/minha-conta/excluir` com confirmação de senha; `AdministradorCliente` tem dados substituídos pelos dados da empresa; demais papéis têm conta desativada e dados anonimizados; refresh tokens revogados; page `/minha-conta` com modal de confirmação.

**Resultado:** 381 testes aprovados (107 Domain + 226 Application + 48 MAUI). Dados pessoais protegidos conforme LGPD.

---

## Backlog posterior

- **Painel de métricas e observabilidade centralizada** — Prometheus + Grafana ou Application Insights
- **Migração dos jobs para Hangfire/Quartz** — painel de monitoramento, retry automático, histórico de execuções
- **Suporte a iOS no MAUI** — requer conta Apple Developer
- **Paginação cursor-based** para listagens de alto volume
- **Fila de e-mails com retry automático** — para garantia de entrega em caso de falha SMTP
- **Múltiplos provedores de e-mail** — SendGrid, Amazon SES, além de SMTP genérico

---

## Fase 12 — Melhorias do portal e adequações legais

**Objetivo:** adequações legais, funcionalidades de perfil e qualidade de testes.

1. **[#95]** CNPJ alfanumérico — atualizar `Inscricao.cs` para aceitar letras nas 8 primeiras posições conforme IN RFB 2.229/2024. Algoritmo de validação atualizado (A=10...Z=35). Máscara do frontend atualizada. Testes de domínio com casos alfanuméricos.
2. **[#96]** Setup e gerenciamento de 2FA TOTP no portal Web — seção em `/minha-conta` para ativar/desativar autenticador, exibir QR code e confirmar primeiro código.
3. **[#97]** Setup e gerenciamento de 2FA TOTP no app MAUI — mesma funcionalidade do item anterior, adaptada para Windows e Android.
4. **[#98]** Tela de perfil da empresa — seção "Minha Empresa" em `/minha-conta` para editar razão social, e-mail e telefone do cliente. Endpoint `PUT /clientes/{id}` já existe no backend.
5. **[#100]** Habilitar testes de integração no CI — adicionar etapa no `ci.yml` com Testcontainers + PostgreSQL e garantir que falha bloqueia o merge.

---

## Fase 12.1 — Encerramento de conta de empresa ✅ Concluída

**Objetivo:** permitir que um `AdministradorCliente` encerre a conta da empresa no portal, com bloqueio de operações, notificação aos clientes finais e limpeza automática posterior.

1. ✅ **[#124]** Página `/minha-empresa` separada no menu lateral para `AdministradorCliente` — edição de razão social, e-mail e telefone do cliente (reutiliza `PUT /clientes/{id}`).
2. ✅ **[#125]** Botão "Encerrar conta" na página `/minha-empresa` — modal com senha + checkbox "Excluir dados imediatamente"; `POST /clientes/{id}/encerrar`; desativa o cliente (`Ativo = false`), define `encerrado_em` e `exclusao_programada_em`, revoga todos os refresh tokens do tenant.
3. ✅ **[#126]** Bloquear renovação de token se empresa estiver inativa — `ValidacaoController.VerificarHmacAsync` verifica `Ativo` do cliente antes de renovar; retorna `401` se inativo.
4. ✅ **[#127]** Notificar clientes finais quando empresa encerrar conta — fire-and-forget no `EncerrarContaEmpresaHandler` envia template `EmpresaEncerrada` para cada `ClienteFinal` ativo do tenant.
5. ✅ **[#128]** Job de exclusão física — `ExcluirEmpresasEncerradasJob` (`BackgroundService`) exclui fisicamente clientes com `exclusao_programada_em <= now()`, ativado diariamente (`ExclusaoEmpresasIntervaloMinutos: 1440`).
6. ✅ **[#129]** Template de e-mail `EmpresaEncerrada` — HTML embarcado como `EmbeddedResource`, mesmo padrão dos templates existentes.
7. ✅ **[#97]** Setup/gerenciamento de 2FA TOTP no MAUI — tela "Minha Conta" com seção de ativação (segredo copiável), confirmação com código e desativação com código.
8. ✅ Migration V007 — `encerrado_em TIMESTAMPTZ` e `exclusao_programada_em TIMESTAMPTZ` na tabela `cliente`.

**Resultado:** 388 testes aprovados (340 backend + 48 MAUI). Encerramento de conta com exclusão imediata ou em 90 dias. Bloqueio automático da API de validação para empresas encerradas. Paridade Web + MAUI para gestão de empresa e 2FA.

**Demo:** AdministradorCliente acessa `/minha-empresa` → clica "Encerrar conta" → confirma senha e marca checkbox de exclusão imediata → conta bloqueada na hora → clientes finais notificados por e-mail → tentativa de validação HMAC retorna 401 → job noturno exclui os dados fisicamente.

---

## Fase 13 — Instaladores

**Objetivo:** distribuição do app MAUI para Windows e Android sem dependência de lojas.

1. **[#101]** Instalador MSIX para Windows — pacote assinado com certificado autoassinado para distribuição direta. Incluir instruções de instalação do certificado.
2. **[#102]** APK/AAB assinado para Android — keystore guardado como GitHub Secret, APK pronto para distribuição direta.

---

## Fase 14 — SDKs cliente (linguagens principais)

**Objetivo:** bibliotecas prontas para uso nas linguagens mais relevantes do mercado, encapsulando a autenticação HMAC e os endpoints de validação.

Cada SDK implementa:
- Geração de HMAC-SHA256 com timestamp, nonce e assinatura
- Chamadas aos 4 endpoints de validação (login, heartbeat, logout, instalação)
- Retry automático em falhas de rede
- Modelos de resposta tipados

1. **[#108]** SDK C#/.NET — pacote NuGet. Targets: `net6.0`, `net8.0`, `net10.0`.
2. **[#109]** SDK Java/Kotlin — pacote Maven/Gradle.
3. **[#110]** SDK Python — pacote PyPI. Suporte Python 3.9+.
4. **[#111]** SDK JavaScript + TypeScript — pacote npm com tipagem completa. Suporte Node.js e browser.

---

## Fase 15 — SDKs cliente (linguagens secundárias)

**Objetivo:** atender sistemas legados e outras linguagens relevantes no mercado brasileiro.

1. **[#112]** SDK Delphi — biblioteca nativa para sistemas Delphi/Pascal.
2. **[#113]** SDK PHP — pacote Composer/Packagist. Suporte PHP 8.0+.
3. **[#114]** SDK VB6 — DLL COM feita em C# expondo os métodos de validação para chamada via referência ActiveX no VB6.

---

## Fase 16 — Painel de Administração da Plataforma

**Objetivo:** painel de monitoramento acessível via SSH tunnel para o operador da plataforma monitorar o sistema em tempo real, sem exposição pública.

Acesso: `ssh -L 16000:localhost:5020 <vm>` → `http://localhost:16000`

1. **[#116]** Projeto `LicenciamentoSoftware.Admin` — aplicação ASP.NET Core leve na porta `5020` (`localhost` only, nunca exposta pelo Nginx). HTTP Basic Auth. Página HTML com Bootstrap e atualização automática a cada 30 segundos. Consulta o banco diretamente via Dapper.
2. **[#117]** Métricas globais da plataforma — total de clientes, usuários ativos, sessões abertas, licenças ativas/inativas, validações nas últimas 24h e 7 dias, erros por motivo, últimos logins (hora + IP), tamanho do banco, status dos serviços (API/BFF up/down), último horário de execução dos jobs.
3. **[#120]** Monitoramento e backup do banco — script `setup-backup.sh` com cron diário às 2h (`pg_dump + gzip`, retenção de 7 dias em `/opt/backups/`). Painel exibe data/hora do último backup, tamanho, status (vermelho se mais de 24h sem backup) e botão para disparar backup manual.
4. **[#121]** Configurar SSH tunnel — documentar acesso ao painel via SSH tunnel no README. Garantir que a porta `5020` não é exposta pelo Nginx nem pelo `ufw`.
