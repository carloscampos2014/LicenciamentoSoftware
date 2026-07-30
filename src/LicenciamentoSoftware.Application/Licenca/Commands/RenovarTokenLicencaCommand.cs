namespace LicenciamentoSoftware.Application.Licenca.Commands;

/// <summary>
/// Revoga o token HMAC atual da licença e emite um novo.
/// </summary>
/// <param name="IdLicenca">ID da licença cujo token será renovado.</param>
/// <param name="ExpiracaoMinutosOverride">
/// Quando informado, sobrescreve o padrão configurado em appsettings.
/// </param>
public sealed record RenovarTokenLicencaCommand(
    Guid IdLicenca,
    int? ExpiracaoMinutosOverride = null);
