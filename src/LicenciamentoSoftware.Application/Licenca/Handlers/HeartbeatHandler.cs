using LicenciamentoSoftware.Application.Licenca.Abstractions;
using LicenciamentoSoftware.Application.Licenca.Commands;
using LicenciamentoSoftware.Application.Licenca.Results;
using Microsoft.Extensions.Logging;

namespace LicenciamentoSoftware.Application.Licenca.Handlers;

/// <summary>
/// Registra atividade em uma sessão ativa (keep-alive / heartbeat).
/// Atualiza <c>data_ultima_atividade</c> para o instante atual.
/// </summary>
public sealed class HeartbeatHandler
{
    private readonly ILicencaSessaoRepository _sessaoRepo;
    private readonly IValidacaoLogRepository _logRepo;
    private readonly ILogger<HeartbeatHandler> _logger;

    public HeartbeatHandler(
        ILicencaSessaoRepository sessaoRepo,
        IValidacaoLogRepository logRepo,
        ILogger<HeartbeatHandler> logger)
    {
        _sessaoRepo = sessaoRepo;
        _logRepo    = logRepo;
        _logger     = logger;
    }

    public async Task<HeartbeatResult> HandleAsync(
        HeartbeatCommand command,
        CancellationToken ct = default)
    {
        var sessao = await _sessaoRepo.BuscarPorIdAsync(command.IdSessao, ct);

        if (sessao is null)
        {
            await GravarLogAsync(command.IdLicenca, "erro",
                MotivoErroValidacao.SessaoInvalida, command.IpOrigem, ct);
            return new HeartbeatResult.SessaoNaoEncontrada();
        }

        // Isola a sessão ao tenant da licença informada
        if (sessao.LicencaId != command.IdLicenca)
        {
            await GravarLogAsync(command.IdLicenca, "erro",
                MotivoErroValidacao.SessaoInvalida, command.IpOrigem, ct);
            return new HeartbeatResult.AcessoNegado();
        }

        if (!sessao.Ativo)
        {
            await GravarLogAsync(command.IdLicenca, "erro",
                MotivoErroValidacao.SessaoInvalida, command.IpOrigem, ct);
            return new HeartbeatResult.SessaoEncerrada();
        }

        await _sessaoRepo.AtualizarAtividadeAsync(command.IdSessao, ct);
        await GravarLogAsync(command.IdLicenca, "sucesso", null, command.IpOrigem, ct);
        return new HeartbeatResult.Sucesso();
    }

    private static readonly Action<ILogger, Guid, Exception?> _logFalhaLog =
        LoggerMessage.Define<Guid>(LogLevel.Warning,
            new EventId(1, "FalhaGravarLog"),
            "Falha ao gravar validacao_log heartbeat para licença {IdLicenca}");

    private async Task GravarLogAsync(
        Guid idLicenca, string resultado, string? motivoErro, string? ipOrigem, CancellationToken ct)
    {
        try
        {
            await _logRepo.InserirAsync(idLicenca, TipoOperacaoValidacao.Heartbeat,
                resultado, motivoErro, ipOrigem, ct);
        }
        catch (Exception ex) { _logFalhaLog(_logger, idLicenca, ex); }
    }
}
