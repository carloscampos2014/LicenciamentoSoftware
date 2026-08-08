using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using LicenseManagerSdk;
using Xunit;

namespace LicenseManagerSdk.Tests;

public class HmacTests
{
    private static LicenseManagerClient MakeClient() =>
        new("https://api.example.com", "test-secret", "lic-123");

    [Fact]
    public void ComputeSignature_MesmoInput_RetornaMesmoHash()
    {
        var client = MakeClient();
        var s1 = client.ComputeSignature("lic", "2026-01-01T00:00:00Z", "{}");
        var s2 = client.ComputeSignature("lic", "2026-01-01T00:00:00Z", "{}");
        s1.Should().Be(s2);
    }

    [Fact]
    public void ComputeSignature_InputDiferente_RetornaHashDiferente()
    {
        var client = MakeClient();
        var s1 = client.ComputeSignature("lic", "2026-01-01T00:00:00Z", "{}");
        var s2 = client.ComputeSignature("lic", "2026-01-01T00:00:01Z", "{}");
        s1.Should().NotBe(s2);
    }

    [Fact]
    public void ComputeSignature_Resultado_EHexLowercaseDe64Chars()
    {
        var client = MakeClient();
        var sig = client.ComputeSignature("lic", "2026-01-01T00:00:00Z", "{}");
        sig.Should().MatchRegex("^[0-9a-f]{64}$");
    }

    [Fact]
    public void ComputeSignature_ValorConhecido_BateComCalculoManual()
    {
        const string token     = "test-secret";
        const string licenseId = "abc-123";
        const string timestamp = "2026-08-06T12:00:00Z";
        const string body      = "{\"idLicenca\":\"abc-123\"}";

        var payload  = $"{licenseId}:{timestamp}:{body}";
        var expected = Convert.ToHexString(
            HMACSHA256.HashData(Encoding.UTF8.GetBytes(token), Encoding.UTF8.GetBytes(payload))
        ).ToLowerInvariant();

        var client = new LicenseManagerClient("https://api.example.com", token, licenseId);
        client.ComputeSignature(licenseId, timestamp, body).Should().Be(expected);
    }

    [Fact]
    public void Constructor_BaseUrlVazia_LancaArgumentException()
    {
        var act = () => new LicenseManagerClient("", "tok", "lic");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_TokenVazio_LancaArgumentException()
    {
        var act = () => new LicenseManagerClient("https://api.test", "", "lic");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_LicenseIdVazio_LancaArgumentException()
    {
        var act = () => new LicenseManagerClient("https://api.test", "tok", "");
        act.Should().Throw<ArgumentException>();
    }
}
