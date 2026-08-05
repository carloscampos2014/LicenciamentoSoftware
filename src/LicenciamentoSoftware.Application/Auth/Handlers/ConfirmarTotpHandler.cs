using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.Auth.Commands;

namespace LicenciamentoSoftware.Application.Auth.Handlers;

public abstract record ConfirmarTotpResult
{
    /// <summary>Código válido — 2FA ativado com sucesso.</summary>
    public sealed record Sucesso : ConfirmarTotpResult;

    /// <summary>Código inválido ou expirado.</summary>
    public sealed record CodigoInvalido : ConfirmarTotpResult;

    /// <summary>Usuário não encontrado ou nenhum setup em andamento.</summary>
    public sealed record NaoEncontrado : ConfirmarTotpResult;
}

/// <summary>
/// Confirma o setup do 2FA validando o primeiro código TOTP.
/// Valida contra o segredo pendente e, se correto, move para totp_secret_hash
/// tornando o 2FA ativo. Só a partir deste momento o login passará a exigir TOTP.
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
        // Busca o segredo pendente (não o definitivo)
        var segredoPendente = await _usuarioRepository
            .BuscarTotpPendenteAsync(command.IdUsuario, cancellationToken);

        if (segredoPendente is null)
            return new ConfirmarTotpResult.NaoEncontrado();

        // Valida o código contra o segredo pendente
        if (!_totpService.Validar(segredoPendente, command.Codigo))
            return new ConfirmarTotpResult.CodigoInvalido();

        // Move pendente → definitivo (ativa o 2FA)
        var confirmado = await _usuarioRepository
            .ConfirmarTotpPendenteAsync(command.IdUsuario, cancellationToken);

        if (!confirmado)
            return new ConfirmarTotpResult.NaoEncontrado();

        return new ConfirmarTotpResult.Sucesso();
    }
}
