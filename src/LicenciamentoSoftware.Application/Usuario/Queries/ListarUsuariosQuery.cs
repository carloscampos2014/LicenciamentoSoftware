using LicenciamentoSoftware.Application.Common;

namespace LicenciamentoSoftware.Application.Usuario.Queries;

public sealed record ListarUsuariosQuery : PagedQuery
{
    public Guid? IdCliente { get; init; }
    public string? Nome { get; init; }
    public bool? Ativo { get; init; }
}
