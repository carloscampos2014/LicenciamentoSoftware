using FluentAssertions;
using LicenciamentoSoftware.Infrastructure.Security;

namespace LicenciamentoSoftware.Application.Tests.Security;

public class HmacLicencaTokenServiceTests
{
    private readonly HmacLicencaTokenService _sut = new();

    private static readonly Guid IdLicenca = Guid.Parse("11111111-2222-3333-4444-555555555555");
    private const string Timestamp = "2026-07-30T12:00:00Z";
    private const string Payload = "corpo-da-requisicao";

    // -------------------------------------------------------------------------
    // GerarSegredo
    // -------------------------------------------------------------------------

    [Fact]
    public void GerarSegredo_RetornaStringNaoVazia()
    {
        var segredo = _sut.GerarSegredo();
        segredo.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void GerarSegredo_CadaChamadaRetornaValorDiferente()
    {
        var s1 = _sut.GerarSegredo();
        var s2 = _sut.GerarSegredo();
        s1.Should().NotBe(s2);
    }

    [Fact]
    public void GerarSegredo_Base64Valido_32BytesMinimo()
    {
        var segredo = _sut.GerarSegredo();
        var bytes = Convert.FromBase64String(segredo);
        bytes.Length.Should().BeGreaterThanOrEqualTo(32);
    }

    // -------------------------------------------------------------------------
    // GerarAssinatura / ValidarAssinatura
    // -------------------------------------------------------------------------

    [Fact]
    public void GerarAssinatura_RetornaHexMinusculo()
    {
        var segredo = _sut.GerarSegredo();
        var assinatura = _sut.GerarAssinatura(IdLicenca, Payload, Timestamp, segredo);

        assinatura.Should().NotBeNullOrWhiteSpace();
        assinatura.Should().MatchRegex("^[0-9a-f]+$");
    }

    [Fact]
    public void ValidarAssinatura_AssinaturaCorreta_RetornaTrue()
    {
        var segredo = _sut.GerarSegredo();
        var assinatura = _sut.GerarAssinatura(IdLicenca, Payload, Timestamp, segredo);

        var valido = _sut.ValidarAssinatura(IdLicenca, Payload, Timestamp, segredo, assinatura);

        valido.Should().BeTrue();
    }

    [Fact]
    public void ValidarAssinatura_SegredoDiferente_RetornaFalse()
    {
        var segredo = _sut.GerarSegredo();
        var assinatura = _sut.GerarAssinatura(IdLicenca, Payload, Timestamp, segredo);
        var outroSegredo = _sut.GerarSegredo();

        var valido = _sut.ValidarAssinatura(IdLicenca, Payload, Timestamp, outroSegredo, assinatura);

        valido.Should().BeFalse();
    }

    [Fact]
    public void ValidarAssinatura_PayloadAdulterado_RetornaFalse()
    {
        var segredo = _sut.GerarSegredo();
        var assinatura = _sut.GerarAssinatura(IdLicenca, Payload, Timestamp, segredo);

        var valido = _sut.ValidarAssinatura(IdLicenca, "payload-adulterado", Timestamp, segredo, assinatura);

        valido.Should().BeFalse();
    }

    [Fact]
    public void ValidarAssinatura_TimestampDiferente_RetornaFalse()
    {
        var segredo = _sut.GerarSegredo();
        var assinatura = _sut.GerarAssinatura(IdLicenca, Payload, Timestamp, segredo);

        var valido = _sut.ValidarAssinatura(IdLicenca, Payload, "2026-07-30T13:00:00Z", segredo, assinatura);

        valido.Should().BeFalse();
    }

    [Fact]
    public void ValidarAssinatura_IdLicencaDiferente_RetornaFalse()
    {
        var segredo = _sut.GerarSegredo();
        var assinatura = _sut.GerarAssinatura(IdLicenca, Payload, Timestamp, segredo);
        var outroId = Guid.NewGuid();

        var valido = _sut.ValidarAssinatura(outroId, Payload, Timestamp, segredo, assinatura);

        valido.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void ValidarAssinatura_AssinaturaVaziaOuNula_RetornaFalse(string? assinatura)
    {
        var segredo = _sut.GerarSegredo();

        var valido = _sut.ValidarAssinatura(IdLicenca, Payload, Timestamp, segredo, assinatura!);

        valido.Should().BeFalse();
    }

    // -------------------------------------------------------------------------
    // HashSegredo / VerificarHashSegredo
    // -------------------------------------------------------------------------

    [Fact]
    public void HashSegredo_RetornaHashBcrypt()
    {
        var segredo = _sut.GerarSegredo();
        var hash = _sut.HashSegredo(segredo);

        hash.Should().NotBeNullOrWhiteSpace();
        hash.Should().StartWith("$2");  // prefixo BCrypt
    }

    [Fact]
    public void VerificarHashSegredo_SegredoCorreto_RetornaTrue()
    {
        var segredo = _sut.GerarSegredo();
        var hash = _sut.HashSegredo(segredo);

        var resultado = _sut.VerificarHashSegredo(segredo, hash);

        resultado.Should().BeTrue();
    }

    [Fact]
    public void VerificarHashSegredo_SegredoErrado_RetornaFalse()
    {
        var segredo = _sut.GerarSegredo();
        var hash = _sut.HashSegredo(segredo);

        var resultado = _sut.VerificarHashSegredo("segredo-errado", hash);

        resultado.Should().BeFalse();
    }

    [Theory]
    [InlineData("", "hash")]
    [InlineData("segredo", "")]
    [InlineData(null!, "hash")]
    [InlineData("segredo", null!)]
    public void VerificarHashSegredo_ValoresVaziosOuNulos_RetornaFalse(string? segredo, string? hash)
    {
        var resultado = _sut.VerificarHashSegredo(segredo!, hash!);
        resultado.Should().BeFalse();
    }
}
