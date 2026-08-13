using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.Licenca.Abstractions;
using LicenciamentoSoftware.Application.Licenca.Commands;
using LicenciamentoSoftware.Application.Licenca.Results;

namespace LicenciamentoSoftware.Application.Licenca.Handlers;

public sealed class EditarRenovacaoAutomaticaHandler
{
    private readonly ILicencaGestaoRepository _repo;
    private readonly IUnitOfWork _uow;

    public EditarRenovacaoAutomaticaHandler(ILicencaGestaoRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<EditarDetalhesResult> HandleAsync(
        EditarRenovacaoAutomaticaCommand command, CancellationToken ct = default)
    {
        if (command.IdLicenca == Guid.Empty)
            return new EditarDetalhesResult.Invalido(["IdLicenca é obrigatório."]);

        var licenca = await _repo.BuscarPorIdAsync(command.IdLicenca, ct);
        if (licenca is null)
            return new EditarDetalhesResult.LicencaNaoEncontrada();

        if (!licenca.Ativo)
            return new EditarDetalhesResult.LicencaInativa();

        if (licenca.Periodo is null)
            return new EditarDetalhesResult.TipoIncompativel(
                "Esta licença não é do tipo Por Período.");

        await _uow.BeginAsync(cancellationToken: ct);
        await _repo.AtualizarRenovacaoAutomaticaAsync(
            command.IdLicenca, command.RenovacaoAutomatica, ct);
        await _uow.CommitAsync(ct);

        return new EditarDetalhesResult.Sucesso();
    }
}
