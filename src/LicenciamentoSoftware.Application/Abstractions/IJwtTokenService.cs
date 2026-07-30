namespace LicenciamentoSoftware.Application.Abstractions;

public record TokenPar(string AccessToken, string RefreshToken, DateTime AccessTokenExpiracao);

/// <summary>
/// Porta para geração e validação de tokens JWT.
/// </summary>
public interface IJwtTokenService
{
    TokenPar GerarTokenPar(Guid idUsuario, Guid idCliente, string nome, string papel);
    string GerarRefreshToken();
    bool ValidarAccessToken(string token, out Guid idUsuario);
}
