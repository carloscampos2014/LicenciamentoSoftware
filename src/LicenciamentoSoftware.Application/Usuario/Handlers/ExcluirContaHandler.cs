using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.Cliente.Abstractions;
using LicenciamentoSoftware.Application.Usuario.Commands;
using LicenciamentoSoftware.Application.Usuario.Results;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace LicenciamentoSoftware.Application.Usuario.Handlers;

/// <summary>
/// LGPD Art. 18 — anonimiza os dados pessoais do titular.
///
/// Comportamento por papel:
///   - AdministradorCliente: nome/email substituídos pelos dados da empresa, senha/tokens revogados.
///     Conta permanece ativa sem senha — na próxima tentativa de login o sistema detecta a ausência
///     de senha e oferece criação de nova senha. Um e-mail é enviado ao email da empresa informando.
///   - Demais papéis: conta desativada + anonimização completa.
///
/// Em ambos os casos: senha, totp_secret e todos os refresh tokens são revogados.
/// Logs de auditoria são preservados (obrigação legal).
/// Não há bloqueio por ser o único admin — a LGPD prevalece sobre restrições operacionais.
/// </summary>
public sealed class ExcluirContaHandler
{
    private readonly IUsuarioRepository _usuarioRepo;
    private readonly IClienteRepository _clienteRepo;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailService _email;
    private readonly IEmailTemplateRenderer _templateRenderer;
    private readonly IClock _clock;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<ExcluirContaHandler> _logger;

    private static readonly Action<ILogger, string, Exception?> _logEmailErro =
        LoggerMessage.Define<string>(LogLevel.Warning,
            new EventId(1, "ExcluirConta_EmailErro"),
            "[ExcluirConta] Falha ao enviar e-mail de notificação para {Email}.");

    public ExcluirContaHandler(
        IUsuarioRepository usuarioRepo,
        IClienteRepository clienteRepo,
        IPasswordHasher passwordHasher,
        IEmailService email,
        IEmailTemplateRenderer templateRenderer,
        IClock clock,
        IUnitOfWork uow,
        ILogger<ExcluirContaHandler> logger)
    {
        _usuarioRepo      = usuarioRepo;
        _clienteRepo      = clienteRepo;
        _passwordHasher   = passwordHasher;
        _email            = email;
        _templateRenderer = templateRenderer;
        _clock            = clock;
        _uow              = uow;
        _logger           = logger;
    }

    public async Task<ExcluirContaResult> HandleAsync(
        ExcluirContaCommand command,
        CancellationToken ct = default)
    {
        // 1. Buscar usuário
        var usuario = await _usuarioRepo.BuscarPorIdAsync(command.IdUsuario, ct);
        if (usuario is null)
            return new ExcluirContaResult.NaoEncontrado();

        // 2. Verificar senha
        if (!_passwordHasher.Verificar(command.SenhaAtual, usuario.SenhaHash))
            return new ExcluirContaResult.SenhaInvalida();

        // 3. Verificar papel
        var papel = await _usuarioRepo.BuscarPapelAsync(command.IdUsuario, ct);
        var ehAdmin = papel == "AdministradorCliente";

        // 4. Buscar dados da empresa para substituição (admin) ou usar genérico (demais)
        string nomeSubstituto;
        string emailSubstituto;
        string? emailNotificacao = null;
        string? nomeEmpresa = null;

        if (ehAdmin)
        {
            // Admin: substitui nome/email pelos dados da empresa.
            // Conta permanece ativa mas sem senha — próximo login detecta SemSenha e oferece redefinição.
            var cliente = await _clienteRepo.BuscarPorIdAsync(command.IdCliente, ct);
            nomeSubstituto    = cliente?.RazaoSocial ?? "Empresa";
            emailSubstituto   = cliente?.Email       ?? $"empresa-{command.IdCliente}@anonimizado.local";
            emailNotificacao  = emailSubstituto;
            nomeEmpresa       = nomeSubstituto;
        }
        else
        {
            nomeSubstituto  = "Usuário Removido";
            emailSubstituto = $"removido-{command.IdUsuario}@anonimizado.local";
        }

        // 5. Persistir em transação
        await _uow.BeginAsync(cancellationToken: ct);
        try
        {
            await _usuarioRepo.AnonimizarAsync(
                command.IdUsuario, nomeSubstituto, emailSubstituto, ct);

            if (!ehAdmin)
                await _usuarioRepo.DesativarUsuarioAsync(command.IdUsuario, ct);

            await _usuarioRepo.RevogarTodosRefreshTokensAsync(command.IdUsuario, ct);

            await _uow.CommitAsync(ct);
        }
        catch
        {
            await _uow.RollbackAsync(ct);
            throw;
        }

        // 6. Enviar e-mail de notificação ao admin (fire-and-forget — não bloqueia o retorno)
        if (ehAdmin && emailNotificacao is not null)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    var corpo = _templateRenderer.Renderizar("ContaAnonimizada", new Dictionary<string, string>
                    {
                        ["{{NomeEmpresa}}"]  = nomeEmpresa ?? "Empresa",
                        ["{{EmailEmpresa}}"] = emailNotificacao,
                        ["{{DataRemocao}}"]  = _clock.UtcNow.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture),
                        ["{{UrlPortal}}"]    = "https://licensemanager.enzojb.com.br",
                    });

                    await _email.EnviarAsync(
                        emailNotificacao,
                        "Seus dados pessoais foram removidos — recupere o acesso",
                        corpo,
                        CancellationToken.None);
                }
                catch
                {
                    _logEmailErro(_logger, emailNotificacao, null);
                }
            }, CancellationToken.None);
        }

        return new ExcluirContaResult.Sucesso();
    }
}
