namespace LicenciamentoSoftware.Application.Licenca.Commands;

/// <summary>
/// Solicita a emissão de um novo token HMAC para a licença informada.
/// </summary>
/// <param name="IdLicenca">ID da licença que receberá o token.</param>
/// <param name="ExpiracaoMinutosOverride">
/// Quando informado, sobrescreve o padrão configurado em appsettings.
/// </param>
public sealed record EmitirTokenLicencaCommand(
    Guid IdLicenca,
    int? ExpiracaoMinutosOverride = null);
