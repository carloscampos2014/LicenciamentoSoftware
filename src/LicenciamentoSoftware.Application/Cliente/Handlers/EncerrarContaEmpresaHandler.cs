using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.Cliente.Abstractions;
using LicenciamentoSoftware.Application.Cliente.Commands;
using LicenciamentoSoftware.Application.Cliente.Results;
using LicenciamentoSoftware.Application.ClienteFinal.Abstractions;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace LicenciamentoSoftware.Application.Cliente.Handlers;

/// <summary>
/// Encerra a conta de empresa do tenant autenticado.
///
/// Fluxo:
/// 1. Verifica senha do administrador
/// 2. Chama Cliente.EncerrarConta(exclusaoImediata) no domínio
/// 3. Persiste em transação: encerrar conta + revogar todos os refresh tokens do tenant
/// 4. Fire-and-forget: envia e-mail EmpresaEncerrada para cada ClienteFinal ativo
/// </summary>
public sealed class EncerrarContaEmpresaHandler
{
    private readonly IClienteRepository _clienteRepo;
    private readonly IClienteFinalRepository _clienteFinalRepo;
    private readonly IUsuarioRepository _usuarioRepo;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailService _email;
    private readonly IEmailTemplateRenderer _templateRenderer;
    private readonly IClock _clock;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<EncerrarContaEmpresaHandler> _logger;

    private static readonly Action<ILogger, Guid, Exception?> _logEncerrada =
        LoggerMessage.Define<Guid>(LogLevel.Information,
            new EventId(1, "Encerramento_Sucesso"),
            "[EncerrarContaEmpresa] Conta do cliente {IdCliente} encerrada com sucesso.");

    private static readonly Action<ILogger, string, Exception?> _logEmailErro =
        LoggerMessage.Define<string>(LogLevel.Warning,
            new EventId(2, "Encerramento_EmailErro"),
            "[EncerrarContaEmpresa] Falha ao notificar cliente final {Email}.");

    public EncerrarContaEmpresaHandler(
        IClienteRepository clienteRepo,
        IClienteFinalRepository clienteFinalRepo,
        IUsuarioRepository usuarioRepo,
        IPasswordHasher passwordHasher,
        IEmailService email,
        IEmailTemplateRenderer templateRenderer,
        IClock clock,
        IUnitOfWork uow,
        ILogger<EncerrarContaEmpresaHandler> logger)
    {
        _clienteRepo      = clienteRepo;
        _clienteFinalRepo = clienteFinalRepo;
        _usuarioRepo      = usuarioRepo;
        _passwordHasher   = passwordHasher;
        _email            = email;
        _templateRenderer = templateRenderer;
        _clock            = clock;
        _uow              = uow;
        _logger           = logger;
    }

    public async Task<EncerrarContaEmpresaResult> HandleAsync(
        EncerrarContaEmpresaCommand command,
        CancellationToken ct = default)
    {
        // 1. Buscar empresa
        var empresa = await _clienteRepo.BuscarPorIdAsync(command.IdCliente, ct);
        if (empresa is null || !empresa.Ativo)
            return new EncerrarContaEmpresaResult.NaoEncontrado();

        if (empresa.EncerradoEm.HasValue)
            return new EncerrarContaEmpresaResult.JaEncerrada();

        // 2. Verificar senha do administrador
        var usuario = await _usuarioRepo.BuscarPorIdAsync(command.IdUsuario, ct);
        if (usuario is null)
            return new EncerrarContaEmpresaResult.NaoEncontrado();

        if (!_passwordHasher.Verificar(command.SenhaAtual, usuario.SenhaHash))
            return new EncerrarContaEmpresaResult.SenhaInvalida();

        // 3. Aplicar regra de domínio
        var agora = _clock.UtcNow;

        // 4. Persistir em transação
        await _uow.BeginAsync(cancellationToken: ct);
        try
        {
            await _clienteRepo.EncerrarContaAsync(
                command.IdCliente,
                agora,
                command.ExclusaoImediata ? agora : agora.AddDays(90),
                ct);

            // Desativa todos os usuários do tenant — impede novos logins
            await _usuarioRepo.DesativarTodosPorClienteAsync(command.IdCliente, ct);

            // Revoga todos os refresh tokens ativos — encerra sessões existentes
            await _usuarioRepo.RevogarTodosRefreshTokensPorClienteAsync(command.IdCliente, ct);

            await _uow.CommitAsync(ct);
        }
        catch
        {
            await _uow.RollbackAsync(ct);
            throw;
        }

        _logEncerrada(_logger, command.IdCliente, null);

        // 5. Notificar clientes finais — fire-and-forget (falha não afeta o encerramento)
        _ = Task.Run(async () =>
        {
            try
            {
                var destinatarios = await _clienteFinalRepo
                    .ListarEmailsAtivosPorClienteAsync(command.IdCliente, CancellationToken.None);

                foreach (var dest in destinatarios)
                {
                    try
                    {
                        var corpo = _templateRenderer.Renderizar("EmpresaEncerrada", new Dictionary<string, string>
                        {
                            ["{{NomeClienteFinal}}"] = dest.RazaoSocial,
                            ["{{NomeEmpresa}}"]      = empresa.RazaoSocial,
                            ["{{DataEncerramento}}"] = agora.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
                        });

                        await _email.EnviarAsync(
                            dest.Email,
                            $"Encerramento de conta — {empresa.RazaoSocial}",
                            corpo,
                            CancellationToken.None);
                    }
                    catch
                    {
                        _logEmailErro(_logger, dest.Email, null);
                    }
                }
            }
            catch (Exception ex)
            {
                _logEmailErro(_logger, "(carregamento de destinatários falhou)", ex);
            }
        }, CancellationToken.None);

        return new EncerrarContaEmpresaResult.Sucesso();
    }
}
