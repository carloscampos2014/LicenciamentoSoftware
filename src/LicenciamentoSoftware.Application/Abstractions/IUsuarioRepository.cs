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

    // -------------------------------------------------------------------------
    // LGPD Art. 18 — exclusão e anonimização de dados pessoais
    // -------------------------------------------------------------------------

    /// <summary>
    /// Anonimiza os dados pessoais do usuário: nome, email, senha_hash e totp_secret_hash.
    /// Mantém o registro para preservar integridade referencial e logs de auditoria.
    /// </summary>
    Task AnonimizarAsync(Guid idUsuario, string nomeSubstituto, string emailSubstituto,
        CancellationToken cancellationToken = default);

    /// <summary>Revoga todos os refresh tokens ativos do usuário.</summary>
    Task RevogarTodosRefreshTokensAsync(Guid idUsuario, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revoga todos os refresh tokens ativos de todos os usuários de um tenant.
    /// Usado no encerramento de conta de empresa.
    /// </summary>
    Task RevogarTodosRefreshTokensPorClienteAsync(Guid idCliente, CancellationToken cancellationToken = default);

    /// <summary>
    /// Desativa todos os usuários ativos de um tenant (ativo = false).
    /// Usado no encerramento de conta de empresa para bloquear novos logins.
    /// </summary>
    Task DesativarTodosPorClienteAsync(Guid idCliente, CancellationToken cancellationToken = default);

    /// <summary>Desativa a conta do usuário (ativo = false).</summary>
    Task DesativarUsuarioAsync(Guid idUsuario, CancellationToken cancellationToken = default);

    /// <summary>
    /// Define a senha de um usuário sem exigir a senha anterior.
    /// Usado no fluxo de recuperação de acesso após exclusão LGPD (conta anonimizada sem senha).
    /// </summary>
    Task DefinirSenhaAsync(Guid idUsuario, string senhaHash, CancellationToken cancellationToken = default);

    /// <summary>Salva o segredo TOTP provisório antes da confirmação.</summary>
    Task SalvarTotpPendenteAsync(Guid idUsuario, string segredo, CancellationToken cancellationToken = default);

    /// <summary>
    /// Confirma o setup TOTP: move o segredo pendente para totp_secret_hash
    /// e limpa o campo pendente. Retorna false se não houver pendente.
    /// </summary>
    Task<bool> ConfirmarTotpPendenteAsync(Guid idUsuario, CancellationToken cancellationToken = default);

    /// <summary>Busca o segredo TOTP pendente do usuário para validação.</summary>
    Task<string?> BuscarTotpPendenteAsync(Guid idUsuario, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica se existe outro AdministradorCliente ativo no mesmo tenant,
    /// excluindo o usuário indicado. Usado para impedir que o último admin se exclua.
    /// </summary>
    Task<bool> ExisteOutroAdminAsync(Guid idCliente, Guid idUsuarioExcluindo,
        CancellationToken cancellationToken = default);
}
