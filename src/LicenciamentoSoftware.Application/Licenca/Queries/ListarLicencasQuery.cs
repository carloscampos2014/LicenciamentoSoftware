using LicenciamentoSoftware.Application.Common;

namespace LicenciamentoSoftware.Application.Licenca.Queries;

public sealed record ListarLicencasQuery : PagedQuery
{
    public Guid? IdCliente { get; init; }
    public Guid? IdClienteFinal { get; init; }
    public Guid? IdAplicativo { get; init; }
    public bool? Ativo { get; init; }
}
