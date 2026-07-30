using LicenciamentoSoftware.Application.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace LicenciamentoSoftware.Infrastructure.Security;

/// <summary>
/// Gera e valida tokens JWT + refresh tokens.
/// Configuração lida de JwtSettings:Secret, JwtSettings:Emissor, JwtSettings:Audiencia.
/// </summary>
public sealed class JwtTokenService : IJwtTokenService
{
    private readonly string _secret;
    private readonly string _emissor;
    private readonly string _audiencia;
    private readonly int _accessTokenMinutos;

    public JwtTokenService(IConfiguration configuration)
    {
        _secret = configuration["JwtSettings:Secret"]
            ?? throw new InvalidOperationException("JwtSettings:Secret não configurado.");
        _emissor = configuration["JwtSettings:Emissor"] ?? "LicenciamentoSoftware";
        _audiencia = configuration["JwtSettings:Audiencia"] ?? "LicenciamentoSoftware";
        _accessTokenMinutos = int.TryParse(
            configuration["JwtSettings:AccessTokenMinutos"], out var min) ? min : 60;
    }

    public TokenPar GerarTokenPar(Guid idUsuario, Guid idCliente, string nome, string papel)
    {
        var chave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var credenciais = new SigningCredentials(chave, SecurityAlgorithms.HmacSha256);
        var expiracao = DateTime.UtcNow.AddMinutes(_accessTokenMinutos);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, idUsuario.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("id_cliente", idCliente.ToString()),
            new Claim("nome", nome),
            new Claim(ClaimTypes.Role, papel),
        };

        var token = new JwtSecurityToken(
            issuer: _emissor,
            audience: _audiencia,
            claims: claims,
            expires: expiracao,
            signingCredentials: credenciais);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        var refreshToken = GerarRefreshToken();

        return new TokenPar(accessToken, refreshToken, expiracao);
    }

    public string GerarRefreshToken()
    {
        var bytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }

    public bool ValidarAccessToken(string token, out Guid idUsuario)
    {
        idUsuario = Guid.Empty;

        try
        {
            var chave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
            var parametros = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = chave,
                ValidateIssuer = true,
                ValidIssuer = _emissor,
                ValidateAudience = true,
                ValidAudience = _audiencia,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
            };

            var handler = new JwtSecurityTokenHandler();
            // Desabilita o mapeamento automático de claims do .NET
            // para preservar os nomes originais do JWT (ex: "sub" em vez de "nameidentifier")
            handler.InboundClaimTypeMap.Clear();
            var principal = handler.ValidateToken(token, parametros, out _);
            var sub = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            if (Guid.TryParse(sub, out var id))
            {
                idUsuario = id;
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ValidarAccessToken falhou: {ex.Message}");
            return false;
        }
    }
}
