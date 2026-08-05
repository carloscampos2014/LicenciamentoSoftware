using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.Auth.Commands;

namespace LicenciamentoSoftware.Application.Auth.Handlers;

public abstract record DesativarTotpResult
{
    /// <summary>2FA desativado com sucesso.</summary>
    public sealed record Sucesso : DesativarTotpResult;

    /// <summary>Código TOTP inválido — não autorizado a desativar.</summary>
    public sealed record CodigoInvalido : DesativarTotpResult;

    /// <summary>Usuário não encontrado ou 2FA já está inativo.</summary>
    public sealed record NaoEncontrado : DesativarTotpResult;
}

/// <summary>
/// Desativa o 2FA do usuário após confirmação com o código TOTP atual.
/// Remove o segredo do banco, impedindo futuros desafios TOTP no login.
/// </summary>
public sealed class DesativarTotpHandler
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly ITotpService _totpService;
    private readonly IUnitOfWork _uow;

    public DesativarTotpHandler(
        IUsuarioRepository usuarioRepository,
        ITotpService totpService,
        IUnitOfWork uow)
    {
        _usuarioRepository = usuarioRepository;
        _totpService = totpService;
        _uow = uow;
    }

    public async Task<DesativarTotpResult> HandleAsync(
        DesativarTotpCommand command,
        CancellationToken cancellationToken = default)
    {
        var usuario = await _usuarioRepository
            .BuscarPorIdAsync(command.IdUsuario, cancellationToken);

        if (usuario is null || !usuario.Ativo)
            return new DesativarTotpResult.NaoEncontrado();

        if (usuario.TotpSecretHash is null)
            return new DesativarTotpResult.NaoEncontrado();

        if (!_totpService.Validar(usuario.TotpSecretHash, command.CodigoAtual))
            return new DesativarTotpResult.CodigoInvalido();

        await _uow.BeginAsync(cancellationToken: cancellationToken);
        try
        {
            await _usuarioRepository.AtualizarTotpSecretAsync(
                usuario.Id, null, cancellationToken);
            await _uow.CommitAsync(cancellationToken);
        }
        catch
        {
            await _uow.RollbackAsync(cancellationToken);
            throw;
        }

        return new DesativarTotpResult.Sucesso();
    }
}
