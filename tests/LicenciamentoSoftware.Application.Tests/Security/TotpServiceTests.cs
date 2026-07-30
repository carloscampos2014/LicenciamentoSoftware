using FluentAssertions;
using LicenciamentoSoftware.Infrastructure.Security;

namespace LicenciamentoSoftware.Application.Tests.Security;

public class TotpServiceTests
{
    private readonly TotpService _service = new();

    [Fact]
    public void GerarSegredo_RetornaStringBase32NaoVazia()
    {
        var segredo = _service.GerarSegredo();
        segredo.Should().NotBeNullOrWhiteSpace();
        segredo.Length.Should().BeGreaterThan(10);
    }

    [Fact]
    public void GerarQrCodeUri_RetornaUriOtpauth()
    {
        var segredo = _service.GerarSegredo();
        var uri = _service.GerarQrCodeUri(segredo, "teste@email.com");
        uri.Should().StartWith("otpauth://totp/");
        uri.Should().Contain("secret=");
        uri.Should().Contain("issuer=");
    }

    [Fact]
    public void Validar_CodigoVazio_RetornaFalse()
    {
        var segredo = _service.GerarSegredo();
        _service.Validar(segredo, "").Should().BeFalse();
        _service.Validar(segredo, "   ").Should().BeFalse();
    }

    [Fact]
    public void Validar_CodigoComMenosDe6Digitos_RetornaFalse()
    {
        var segredo = _service.GerarSegredo();
        _service.Validar(segredo, "12345").Should().BeFalse();
    }

    [Fact]
    public void Validar_CodigoIncorreto_RetornaFalse()
    {
        var segredo = _service.GerarSegredo();
        _service.Validar(segredo, "000000").Should().BeFalse();
    }
}
