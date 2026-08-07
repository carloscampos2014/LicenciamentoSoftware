using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Xunit;
using LicenseManagerSdk;

namespace LicenseManagerSdk.Tests;

public class HmacTests
{
    // -------------------------------------------------------------------------
    // Testes de geração de HMAC via reflexão (método privado)
    // -------------------------------------------------------------------------

    [Fact]
    public void ComputeSignature_MesmoInput_RetornaMesmoHash()
    {
        var client = new LicenseManagerClient(
            "https://api.example.com", "secret-token", "license-id-123",
            new HttpClient());

        var sig1 = InvokeComputeSignature(client, "license-id-123", "2026-01-01T00:00:00Z", "{\"test\":1}");
        var sig2 = InvokeComputeSignature(client, "license-id-123", "2026-01-01T00:00:00Z", "{\"test\":1}");

        sig1.Should().Be(sig2);
    }

    [Fact]
    public void ComputeSignature_InputDiferente_RetornaHashDiferente()
    {
        var client = new LicenseManagerClient(
            "https://api.example.com", "secret-token", "license-id-123",
            new HttpClient());

        var sig1 = InvokeComputeSignature(client, "license-id-123", "2026-01-01T00:00:00Z", "{\"test\":1}");
        var sig2 = InvokeComputeSignature(client, "license-id-123", "2026-01-01T00:00:01Z", "{\"test\":1}");

        sig1.Should().NotBe(sig2);
    }

    [Fact]
    public void ComputeSignature_Resultado_EHexStringMinuscula()
    {
        var client = new LicenseManagerClient(
            "https://api.example.com", "secret-token", "license-id-123",
            new HttpClient());

        var sig = InvokeComputeSignature(client, "lid", "2026-01-01T00:00:00Z", "{}");

        sig.Should().MatchRegex("^[0-9a-f]{64}$");
    }

    [Fact]
    public void ComputeSignature_ValorConhecido_BateComCalculo()
    {
        // Calcula o valor esperado manualmente
        const string token     = "my-secret";
        const string licenseId = "abc-123";
        const string timestamp = "2026-08-06T12:00:00Z";
        const string body      = "{\"idLicenca\":\"abc-123\"}";
        var payload  = $"{licenseId}:{timestamp}:{body}";
        var expected = Convert.ToHexString(
            HMACSHA256.HashData(Encoding.UTF8.GetBytes(token), Encoding.UTF8.GetBytes(payload))
        ).ToLowerInvariant();

        var client = new LicenseManagerClient("https://api.example.com", token, licenseId, new HttpClient());
        var actual = InvokeComputeSignature(client, licenseId, timestamp, body);

        actual.Should().Be(expected);
    }

    // -------------------------------------------------------------------------
    // Testes de comportamento do cliente com HttpClient mockado
    // -------------------------------------------------------------------------

    [Fact]
    public async Task LoginAsync_RespostaAutorizada_RetornaSessionId()
    {
        var handler = new FakeHttpHandler(HttpStatusCode.OK,
            """{"autorizado":true,"idSessao":"sess-999"}""");

        var client = new LicenseManagerClient(
            "https://api.example.com", "tok", "lic",
            new HttpClient(handler) { BaseAddress = new Uri("https://api.example.com/") });

        var result = await client.LoginAsync("user-1");

        result.Authorized.Should().BeTrue();
        result.SessionId.Should().Be("sess-999");
    }

    [Fact]
    public async Task LoginAsync_Erro401_LancaLicenseManagerException()
    {
        var handler = new FakeHttpHandler(HttpStatusCode.Unauthorized, """{"erro":"Token inválido"}""");

        var client = new LicenseManagerClient(
            "https://api.example.com", "tok", "lic",
            new HttpClient(handler) { BaseAddress = new Uri("https://api.example.com/") });

        var act = async () => await client.LoginAsync("user-1");

        await act.Should().ThrowAsync<LicenseManagerException>()
            .Where(e => e.StatusCode == HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task HeartbeatAsync_Resposta204_NaoLancaExcecao()
    {
        var handler = new FakeHttpHandler(HttpStatusCode.NoContent, "");

        var client = new LicenseManagerClient(
            "https://api.example.com", "tok", "lic",
            new HttpClient(handler) { BaseAddress = new Uri("https://api.example.com/") });

        var act = async () => await client.HeartbeatAsync("sess-1");

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task LogoutAsync_Resposta204_NaoLancaExcecao()
    {
        var handler = new FakeHttpHandler(HttpStatusCode.NoContent, "");

        var client = new LicenseManagerClient(
            "https://api.example.com", "tok", "lic",
            new HttpClient(handler) { BaseAddress = new Uri("https://api.example.com/") });

        var act = async () => await client.LogoutAsync("sess-1");

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ValidateInstallationAsync_RespostaAutorizada_RetornaInstallationId()
    {
        var handler = new FakeHttpHandler(HttpStatusCode.OK,
            """{"autorizado":true,"idInstalacao":"inst-42","jaRegistrada":false}""");

        var client = new LicenseManagerClient(
            "https://api.example.com", "tok", "lic",
            new HttpClient(handler) { BaseAddress = new Uri("https://api.example.com/") });

        var result = await client.ValidateInstallationAsync("machine-xyz");

        result.Authorized.Should().BeTrue();
        result.InstallationId.Should().Be("inst-42");
        result.AlreadyRegistered.Should().BeFalse();
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
        var act = () => new LicenseManagerClient("https://api.example.com", "", "lic");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_LicenseIdVazio_LancaArgumentException()
    {
        var act = () => new LicenseManagerClient("https://api.example.com", "tok", "");
        act.Should().Throw<ArgumentException>();
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static string InvokeComputeSignature(LicenseManagerClient client,
        string licenseId, string timestamp, string body)
    {
        var method = typeof(LicenseManagerClient)
            .GetMethod("ComputeSignature",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        return (string)method.Invoke(client, [licenseId, timestamp, body])!;
    }
}

// -------------------------------------------------------------------------
// Fake HttpHandler para testes
// -------------------------------------------------------------------------

internal sealed class FakeHttpHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _statusCode;
    private readonly string _body;

    public FakeHttpHandler(HttpStatusCode statusCode, string body)
    {
        _statusCode = statusCode;
        _body       = body;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(_body, Encoding.UTF8, "application/json"),
        };
        return Task.FromResult(response);
    }
}
