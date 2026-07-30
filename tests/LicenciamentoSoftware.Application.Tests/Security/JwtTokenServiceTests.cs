using FluentAssertions;
using LicenciamentoSoftware.Infrastructure.Security;
using Microsoft.Extensions.Configuration;

namespace LicenciamentoSoftware.Application.Tests.Security;

public class JwtTokenServiceTests
{
    private static JwtTokenService CriarServico(int expiracaoMinutos = 60)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:Secret"] = "chave_super_secreta_para_teste_unitario_32chars!",
                ["JwtSettings:Emissor"] = "TestEmissor",
                ["JwtSettings:Audiencia"] = "TestAudiencia",
                ["JwtSettings:AccessTokenMinutos"] = expiracaoMinutos.ToString(System.Globalization.CultureInfo.InvariantCulture),
            })
            .Build();

        return new JwtTokenService(config);
    }

    [Fact]
    public void GerarTokenPar_RetornaAccessERefreshToken()
    {
        var service = CriarServico();
        var par = service.GerarTokenPar(
            Guid.NewGuid(), Guid.NewGuid(), "Teste", "OperadorCliente");

        par.AccessToken.Should().NotBeNullOrWhiteSpace();
        par.RefreshToken.Should().NotBeNullOrWhiteSpace();
        par.AccessTokenExpiracao.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public void ValidarAccessToken_TokenValido_RetornaTrueComId()
    {
        // Usa a MESMA instância para gerar e validar
        var service = CriarServico();
        var idUsuario = Guid.NewGuid();
        var par = service.GerarTokenPar(
            idUsuario, Guid.NewGuid(), "Teste", "Leitor");

        var valido = service.ValidarAccessToken(par.AccessToken, out var idExtraido);

        valido.Should().BeTrue();
        idExtraido.Should().Be(idUsuario);
    }

    [Fact]
    public void ValidarAccessToken_TokenInvalido_RetornaFalse()
    {
        var service = CriarServico();
        var valido = service.ValidarAccessToken("token.invalido.aqui", out var id);

        valido.Should().BeFalse();
        id.Should().Be(Guid.Empty);
    }

    [Fact]
    public void GerarRefreshToken_RetornaStringBase64Unica()
    {
        var service = CriarServico();
        var t1 = service.GerarRefreshToken();
        var t2 = service.GerarRefreshToken();

        t1.Should().NotBeNullOrWhiteSpace();
        t1.Should().NotBe(t2);
    }
}
