using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.Licenca.Abstractions;
using LicenciamentoSoftware.Application.Licenca.Commands;

namespace LicenciamentoSoftware.Application.Licenca.Handlers;

public sealed class RenovarPeriodoHandler
{
    private readonly ILicencaGestaoRepository _repo;
    private readonly IUnitOfWork _uow;

    public RenovarPeriodoHandler(ILicencaGestaoRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow  = uow;
    }

    public async Task<RenovarPeriodoResult> HandleAsync(
        RenovarPeriodoCommand command, CancellationToken ct = default)
    {
        // 1. Licença existe?
        var licenca = await _repo.BuscarPorIdAsync(command.IdLicenca, ct);
        if (licenca is null)
            return new RenovarPeriodoResult.LicencaNaoEncontrada();

        if (!licenca.Ativo)
            return new RenovarPeriodoResult.LicencaInativa();

        // 2. Tem detalhe de período?
        if (licenca.Periodo is null)
            return new RenovarPeriodoResult.LicencaSemPeriodo();

        // 3. Validar nova data usando invariante do domínio
        if (command.NovaDataFim <= licenca.Periodo.DataInicio)
            return new RenovarPeriodoResult.DataInvalida(
                $"NovaDataFim deve ser posterior a DataInicio ({licenca.Periodo.DataInicio:yyyy-MM-dd}).");

        await _uow.BeginAsync(cancellationToken: ct);
        await _repo.AtualizarDataFimPeriodoAsync(command.IdLicenca, command.NovaDataFim, ct);
        await _uow.CommitAsync(ct);

        return new RenovarPeriodoResult.Sucesso(command.NovaDataFim);
    }
}

public abstract record RenovarPeriodoResult
{
    private RenovarPeriodoResult() { }
    public sealed record Sucesso(DateTime NovaDataFim) : RenovarPeriodoResult;
    public sealed record LicencaNaoEncontrada : RenovarPeriodoResult;
    public sealed record LicencaInativa : RenovarPeriodoResult;
    public sealed record LicencaSemPeriodo : RenovarPeriodoResult;
    public sealed record DataInvalida(string Motivo) : RenovarPeriodoResult;
}
