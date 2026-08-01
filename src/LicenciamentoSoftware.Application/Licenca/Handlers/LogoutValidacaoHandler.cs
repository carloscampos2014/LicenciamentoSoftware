using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.Licenca.Abstractions;
using LicenciamentoSoftware.Application.Licenca.Commands;
using LicenciamentoSoftware.Application.Licenca.Results;
using Microsoft.Extensions.Logging;

namespace LicenciamentoSoftware.Application.Licenca.Handlers;

/// <summary>
/// Encerra explicitamente uma sessão de validação.
/// <para>
/// A operação é <b>idempotente</b>: se a sessão já estiver encerrada, retorna
/// <see cref="LogoutValidacaoResult.Sucesso"/> sem erro.
/// </para>
/// </summary>
public sealed class LogoutValidacaoHandler
{
    private readonly ILicencaSessaoRepository _sessaoRepo;
    private readonly IValidacaoLogRepository _logRepo;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<LogoutValidacaoHandler> _logger;

    public LogoutValidacaoHandler(
        ILicencaSessaoRepository sessaoRepo,
        IValidacaoLogRepository logRepo,
        IUnitOfWork uow,
        ILogger<LogoutValidacaoHandler> logger)
    {
        _sessaoRepo = sessaoRepo;
        _logRepo    = logRepo;
        _uow        = uow;
        _logger     = logger;
    }

    public async Task<LogoutValidacaoResult> HandleAsync(
        LogoutValidacaoCommand command,
        CancellationToken ct = default)
    {
        var sessao = await _sessaoRepo.BuscarPorIdAsync(command.IdSessao, ct);

        if (sessao is null)
        {
            await GravarLogAsync(command.IdLicenca, "erro",
                MotivoErroValidacao.SessaoInvalida, command.IpOrigem, ct);
            return new LogoutValidacaoResult.SessaoNaoEncontrada();
        }

        // Isola a sessão ao tenant da licença informada
        if (sessao.LicencaId != command.IdLicenca)
        {
            await GravarLogAsync(command.IdLicenca, "erro",
                MotivoErroValidacao.SessaoInvalida, command.IpOrigem, ct);
            return new LogoutValidacaoResult.AcessoNegado();
        }

        // Idempotente: já encerrada = sucesso sem escritas adicionais
        if (!sessao.Ativo)
        {
            await GravarLogAsync(command.IdLicenca, "sucesso", null, command.IpOrigem, ct);
            return new LogoutValidacaoResult.Sucesso();
        }

        await _uow.BeginAsync(cancellationToken: ct);
        await _sessaoRepo.EncerrarAsync(command.IdSessao, ct);
        await _uow.CommitAsync(ct);

        await GravarLogAsync(command.IdLicenca, "sucesso", null, command.IpOrigem, ct);
        return new LogoutValidacaoResult.Sucesso();
    }

    private static readonly Action<ILogger, Guid, Exception?> _logFalhaLog =
        LoggerMessage.Define<Guid>(LogLevel.Warning,
            new EventId(1, "FalhaGravarLog"),
            "Falha ao gravar validacao_log logout para licença {IdLicenca}");

    private async Task GravarLogAsync(
        Guid idLicenca, string resultado, string? motivoErro, string? ipOrigem, CancellationToken ct)
    {
        try
        {
            await _logRepo.InserirAsync(idLicenca, TipoOperacaoValidacao.Logout,
                resultado, motivoErro, ipOrigem, ct);
        }
        catch (Exception ex) { _logFalhaLog(_logger, idLicenca, ex); }
    }
}
