using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.Licenca.Abstractions;
using LicenciamentoSoftware.Application.Licenca.Commands;
using LicenciamentoSoftware.Application.Licenca.Results;
using LicenciamentoSoftware.Application.Licenca.Validators;

namespace LicenciamentoSoftware.Application.Licenca.Handlers;

public sealed class EditarDetalhesInstalacaoHandler
{
    private readonly ILicencaGestaoRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly EditarDetalhesInstalacaoValidator _validator = new();

    public EditarDetalhesInstalacaoHandler(ILicencaGestaoRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<EditarDetalhesResult> HandleAsync(
        EditarDetalhesInstalacaoCommand command, CancellationToken ct = default)
    {
        var validacao = await _validator.ValidateAsync(command, ct);
        if (!validacao.IsValid)
            return new EditarDetalhesResult.Invalido(
                validacao.Errors.Select(e => e.ErrorMessage).ToList());

        var licenca = await _repo.BuscarPorIdAsync(command.IdLicenca, ct);
        if (licenca is null)
            return new EditarDetalhesResult.LicencaNaoEncontrada();

        if (!licenca.Ativo)
            return new EditarDetalhesResult.LicencaInativa();

        if (licenca.Instalacao is null)
            return new EditarDetalhesResult.TipoIncompativel(
                "Esta licença não é do tipo Por Instalação.");

        await _uow.BeginAsync(cancellationToken: ct);
        await _repo.AtualizarDetalhesInstalacaoAsync(
            command.IdLicenca, command.QuantidadeMaxima, ct);
        await _uow.CommitAsync(ct);

        return new EditarDetalhesResult.Sucesso();
    }
}
