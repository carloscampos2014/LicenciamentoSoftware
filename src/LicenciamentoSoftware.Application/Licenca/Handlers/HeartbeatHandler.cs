using LicenciamentoSoftware.Application.Licenca.Abstractions;
using LicenciamentoSoftware.Application.Licenca.Commands;
using LicenciamentoSoftware.Application.Licenca.Results;

namespace LicenciamentoSoftware.Application.Licenca.Handlers;

/// <summary>
/// Registra atividade em uma sessão ativa (keep-alive / heartbeat).
/// Atualiza <c>data_ultima_atividade</c> para o instante atual.
/// </summary>
public sealed class HeartbeatHandler
{
    private readonly ILicencaSessaoRepository _sessaoRepo;

    public HeartbeatHandler(ILicencaSessaoRepository sessaoRepo)
        => _sessaoRepo = sessaoRepo;

    public async Task<HeartbeatResult> HandleAsync(
        HeartbeatCommand command,
        CancellationToken ct = default)
    {
        var sessao = await _sessaoRepo.BuscarPorIdAsync(command.IdSessao, ct);

        if (sessao is null)
            return new HeartbeatResult.SessaoNaoEncontrada();

        // Isola a sessão ao tenant da licença informada (evita enumeração entre tenants)
        if (sessao.LicencaId != command.IdLicenca)
            return new HeartbeatResult.AcessoNegado();

        if (!sessao.Ativo)
            return new HeartbeatResult.SessaoEncerrada();

        await _sessaoRepo.AtualizarAtividadeAsync(command.IdSessao, ct);

        return new HeartbeatResult.Sucesso();
    }
}
