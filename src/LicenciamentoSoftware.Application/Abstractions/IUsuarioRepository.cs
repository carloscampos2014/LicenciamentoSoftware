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

    // -------------------------------------------------------------------------
    // Fase 8 — jobs de notificação
    // -------------------------------------------------------------------------

    /// <summary>
    /// Busca o e-mail e nome do primeiro AdministradorCliente ativo de um tenant.
    /// Usado para envio de notificações automáticas.
    /// </summary>
    Task<Jobs.AdminClienteInfo?> BuscarEmailAdminPorClienteAsync(
        Guid idCliente, CancellationToken cancellationToken = default);
}
