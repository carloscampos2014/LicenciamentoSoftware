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
/// O secret é lido sob demanda para suportar injeção de config em testes
/// sem lançar exceção na startup quando o appsettings base está vazio.
/// </summary>
public sealed class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;
    private readonly string _emissor;
    private readonly string _audiencia;
    private readonly int _accessTokenMinutos;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
        _emissor = configuration["JwtSettings:Emissor"] ?? "LicenciamentoSoftware";
        _audiencia = configuration["JwtSettings:Audiencia"] ?? "LicenciamentoSoftware";
        _accessTokenMinutos = int.TryParse(
            configuration["JwtSettings:AccessTokenMinutos"], out var min) ? min : 60;
    }

    private SymmetricSecurityKey GetChave()
    {
        var secret = _configuration["JwtSettings:Secret"]
            ?? throw new InvalidOperationException("JwtSettings:Secret não configurado.");
        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
    }

    public TokenPar GerarTokenPar(Guid idUsuario, Guid idCliente, string nome, string papel, string? email = null)
    {
        var chave = GetChave();
        var credenciais = new SigningCredentials(chave, SecurityAlgorithms.HmacSha256);
        var expiracao = DateTime.UtcNow.AddMinutes(_accessTokenMinutos);

        var claimsList = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, idUsuario.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("id_cliente", idCliente.ToString()),
            new Claim("nome", nome),
            new Claim(ClaimTypes.Role, papel),
        };

        if (!string.IsNullOrWhiteSpace(email))
            claimsList.Add(new Claim(JwtRegisteredClaimNames.Email, email));

        var token = new JwtSecurityToken(
            issuer: _emissor,
            audience: _audiencia,
            claims: claimsList,
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
            var chave = GetChave();
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
            // Desabilita o mapeamento automático para preservar claim names originais do JWT
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
        catch
        {
            return false;
        }
    }

    public ClaimsPrincipal? ValidarToken(string token)
    {
        try
        {
            var chave = GetChave();
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
            handler.InboundClaimTypeMap.Clear();
            return handler.ValidateToken(token, parametros, out _);
        }
        catch
        {
            return null;
        }
    }
}
