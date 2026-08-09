using LicenciamentoSoftware.Application.Abstractions;

namespace LicenciamentoSoftware.Application.Auth.Handlers;

public abstract record ResetarTotpAdminResult
{
    public sealed record Sucesso : ResetarTotpAdminResult;
    public sealed record UsuarioNaoEncontrado : ResetarTotpAdminResult;
}

/// <summary>
/// Reseta o 2FA TOTP de um usuário pelo operador Admin (Painel Admin via SSH tunnel).
/// Não exige o código TOTP atual — usado quando o usuário perdeu acesso ao autenticador.
/// Apaga totp_secret_hash e totp_secret_pendente. Registra auditoria.
/// </summary>
public sealed class ResetarTotpAdminHandler
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IUnitOfWork _uow;

    public ResetarTotpAdminHandler(IUsuarioRepository usuarioRepository, IUnitOfWork uow)
    {
        _usuarioRepository = usuarioRepository;
        _uow = uow;
    }

    public async Task<ResetarTotpAdminResult> HandleAsync(
        Guid idUsuario,
        CancellationToken cancellationToken = default)
    {
        var usuario = await _usuarioRepository.BuscarPorIdAsync(idUsuario, cancellationToken);

        if (usuario is null)
            return new ResetarTotpAdminResult.UsuarioNaoEncontrado();

        await _uow.BeginAsync(cancellationToken: cancellationToken);
        try
        {
            // Remove segredo confirmado e pendente
            await _usuarioRepository.AtualizarTotpSecretAsync(idUsuario, null, cancellationToken);
            await _uow.CommitAsync(cancellationToken);
        }
        catch
        {
            await _uow.RollbackAsync(cancellationToken);
            throw;
        }

        return new ResetarTotpAdminResult.Sucesso();
    }
}
