using LicenciamentoSoftware.Domain.Entities;

namespace LicenciamentoSoftware.Application.Abstractions;

public interface IUsuarioRepository
{
    Task<Usuario?> BuscarPorEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<Usuario?> BuscarPorIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<string> BuscarPapelAsync(Guid idUsuario, CancellationToken cancellationToken = default);
    Task<bool> ExisteAdminParaClienteAsync(Guid idCliente, CancellationToken cancellationToken = default);
    Task SalvarAsync(Usuario usuario, string papel, CancellationToken cancellationToken = default);
    Task AtualizarTotpSecretAsync(Guid idUsuario, string? totpSecret, CancellationToken cancellationToken = default);
}
