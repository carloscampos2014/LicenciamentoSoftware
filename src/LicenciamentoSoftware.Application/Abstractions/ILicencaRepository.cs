namespace LicenciamentoSoftware.Application.Abstractions;

public record LicencaInfo(
    Guid Id,
    Guid IdCliente,
    Guid IdClienteFinal,
    Guid IdAplicativo,
    bool Ativo);

/// <summary>
/// Porta de leitura mínima de licenças — escopo da Fase 4.
/// </summary>
public interface ILicencaRepository
{
    Task<LicencaInfo?> BuscarPorIdAsync(Guid id,
        CancellationToken cancellationToken = default);
}
