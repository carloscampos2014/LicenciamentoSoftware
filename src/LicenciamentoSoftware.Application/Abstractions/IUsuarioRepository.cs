using LicenciamentoSoftware.Domain.Entities;

namespace LicenciamentoSoftware.Application.Abstractions;

public interface IUsuarioRepository
{
    Task<Domain.Entities.Usuario?> BuscarPorEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<Domain.Entities.Usuario?> BuscarPorIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<string> BuscarPapelAsync(Guid idUsuario, CancellationToken cancellationToken = default);
    Task<bool> ExisteAdminParaClienteAsync(Guid idCliente, CancellationToken cancellationToken = default);
    Task SalvarAsync(Domain.Entities.Usuario usuario, string papel, CancellationToken cancellationToken = default);
    Task AtualizarTotpSecretAsync(Guid idUsuario, string? totpSecret, CancellationToken cancellationToken = default);
}
