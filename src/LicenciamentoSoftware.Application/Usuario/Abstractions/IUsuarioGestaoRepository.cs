using LicenciamentoSoftware.Application.Common;
using LicenciamentoSoftware.Application.Usuario.Commands;
using LicenciamentoSoftware.Application.Usuario.Results;

namespace LicenciamentoSoftware.Application.Usuario.Abstractions;

public interface IUsuarioGestaoRepository
{
    Task<UsuarioResult?> BuscarPorIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExisteEmailAsync(string email, Guid? ignorarId = null, CancellationToken ct = default);
    Task<PagedResult<UsuarioResult>> ListarAsync(Guid? idCliente, string? nome, bool? ativo, int pagina, int tamanhoPagina, CancellationToken ct = default);
    Task<Guid> InserirAsync(Domain.Entities.Usuario usuario, string papel, CancellationToken ct = default);
    Task AtualizarAsync(AtualizarUsuarioCommand command, CancellationToken ct = default);
    Task DesativarAsync(Guid id, CancellationToken ct = default);
}
