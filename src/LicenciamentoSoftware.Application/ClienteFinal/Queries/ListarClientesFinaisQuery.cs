using LicenciamentoSoftware.Application.Common;

namespace LicenciamentoSoftware.Application.ClienteFinal.Queries;

public sealed record ListarClientesFinaisQuery : PagedQuery
{
    public Guid? IdCliente { get; init; }
    public string? RazaoSocial { get; init; }
    public bool? Ativo { get; init; }
}
