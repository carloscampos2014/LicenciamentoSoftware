using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.Licenca.Abstractions;
using LicenciamentoSoftware.Application.Licenca.Commands;
using LicenciamentoSoftware.Application.Licenca.Results;

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
    private readonly IUnitOfWork _uow;

    public LogoutValidacaoHandler(ILicencaSessaoRepository sessaoRepo, IUnitOfWork uow)
    {
        _sessaoRepo = sessaoRepo;
        _uow        = uow;
    }

    public async Task<LogoutValidacaoResult> HandleAsync(
        LogoutValidacaoCommand command,
        CancellationToken ct = default)
    {
        var sessao = await _sessaoRepo.BuscarPorIdAsync(command.IdSessao, ct);

        if (sessao is null)
            return new LogoutValidacaoResult.SessaoNaoEncontrada();

        // Isola a sessão ao tenant da licença informada (evita enumeração entre tenants)
        if (sessao.LicencaId != command.IdLicenca)
            return new LogoutValidacaoResult.AcessoNegado();

        // Idempotente: já encerrada = sucesso sem escritas adicionais
        if (!sessao.Ativo)
            return new LogoutValidacaoResult.Sucesso();

        await _uow.BeginAsync(cancellationToken: ct);
        await _sessaoRepo.EncerrarAsync(command.IdSessao, ct);
        await _uow.CommitAsync(ct);

        return new LogoutValidacaoResult.Sucesso();
    }
}
