using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.Auth.Commands;

namespace LicenciamentoSoftware.Application.Auth.Handlers;

public abstract record ConfirmarTotpResult
{
    /// <summary>Código válido — 2FA confirmado e ativo.</summary>
    public sealed record Sucesso : ConfirmarTotpResult;

    /// <summary>Código inválido ou expirado.</summary>
    public sealed record CodigoInvalido : ConfirmarTotpResult;

    /// <summary>Usuário não encontrado ou 2FA não configurado ainda.</summary>
    public sealed record NaoEncontrado : ConfirmarTotpResult;
}

/// <summary>
/// Confirma que o autenticador foi configurado corretamente validando
/// o primeiro código TOTP gerado pelo app do usuário.
/// </summary>
public sealed class ConfirmarTotpHandler
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly ITotpService _totpService;

    public ConfirmarTotpHandler(
        IUsuarioRepository usuarioRepository,
        ITotpService totpService)
    {
        _usuarioRepository = usuarioRepository;
        _totpService = totpService;
    }

    public async Task<ConfirmarTotpResult> HandleAsync(
        ConfirmarTotpCommand command,
        CancellationToken cancellationToken = default)
    {
        var usuario = await _usuarioRepository
            .BuscarPorIdAsync(command.IdUsuario, cancellationToken);

        if (usuario is null || !usuario.Ativo)
            return new ConfirmarTotpResult.NaoEncontrado();

        if (usuario.TotpSecretHash is null)
            return new ConfirmarTotpResult.NaoEncontrado();

        if (!_totpService.Validar(usuario.TotpSecretHash, command.Codigo))
            return new ConfirmarTotpResult.CodigoInvalido();

        return new ConfirmarTotpResult.Sucesso();
    }
}
