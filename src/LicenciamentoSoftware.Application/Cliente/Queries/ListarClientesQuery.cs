using LicenciamentoSoftware.Application.Common;

namespace LicenciamentoSoftware.Application.Cliente.Queries;

public sealed record ListarClientesQuery : PagedQuery
{
    public string? RazaoSocial { get; init; }
    public bool? Ativo { get; init; }
}
