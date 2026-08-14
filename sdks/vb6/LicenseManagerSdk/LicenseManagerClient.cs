using System;
using System.Globalization;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;

[assembly: InternalsVisibleTo("LicenseManagerSdk.Tests")]

namespace LicenseManagerSdk;

/// <summary>
/// Implementação COM do cliente LicenseManager.
/// Registrar com: regasm /tlb LicenseManagerSdk.dll
/// Usar no VB6: Set client = CreateObject("LicenseManagerSdk.LicenseManagerClient")
/// </summary>
[ComVisible(true)]
[Guid("B2C3D4E5-F6A7-8901-BCDE-F12345678901")]
[ProgId("LicenseManagerSdk.LicenseManagerClient")]
[ClassInterface(ClassInterfaceType.None)]
public sealed class LicenseManagerClient : ILicenseManagerClient
{
    private readonly string _baseUrl;
    private readonly string _token;
    private readonly string _licenseId;

    // HttpClient é thread-safe e deve ser reutilizado
    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromSeconds(30),
    };

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>
    /// Cria uma nova instância do cliente.
    /// </summary>
    /// <param name="baseUrl">URL base da API</param>
    /// <param name="token">Token da licença</param>
    /// <param name="licenseId">GUID da licença</param>
    public LicenseManagerClient(string baseUrl, string token, string licenseId)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))   throw new ArgumentException("baseUrl é obrigatório.");
        if (string.IsNullOrWhiteSpace(token))     throw new ArgumentException("token é obrigatório.");
        if (string.IsNullOrWhiteSpace(licenseId)) throw new ArgumentException("licenseId é obrigatório.");

        _baseUrl   = baseUrl.TrimEnd('/');
        _token     = token;
        // Normaliza GUID para lowercase com hífens — igual ao servidor (idLicenca:D)
        _licenseId = Guid.TryParse(licenseId, out var parsedGuid)
            ? parsedGuid.ToString("D")
            : licenseId;
    }

    /// <summary>Construtor sem parâmetros exigido pelo COM.</summary>
    public LicenseManagerClient()
        : this("https://licensemanager-api.enzojb.com.br", "token-nao-configurado", "license-id-nao-configurado")
    {
    }

    // -------------------------------------------------------------------------
    // Métodos COM (síncronos — VB6 não suporta async/await)
    // -------------------------------------------------------------------------

    /// <inheritdoc/>
    public string Login(string userId)
    {
        var body = JsonSerializer.Serialize(new
        {
            idLicenca = _licenseId,
            identificadorUsuario = userId,
        });
        return PostSync("api/validacao/login", body);
    }

    /// <inheritdoc/>
    public void Heartbeat(string sessionId)
    {
        var body = JsonSerializer.Serialize(new
        {
            idLicenca = _licenseId,
            idSessao  = sessionId,
        });
        PostSync("api/validacao/heartbeat", body);
    }

    /// <inheritdoc/>
    public void Logout(string sessionId)
    {
        var body = JsonSerializer.Serialize(new
        {
            idLicenca = _licenseId,
            idSessao  = sessionId,
        });
        PostSync("api/validacao/logout", body);
    }

    /// <inheritdoc/>
    public string ValidateInstallation(string machineId)
    {
        var body = JsonSerializer.Serialize(new
        {
            idLicenca            = _licenseId,
            identificadorMaquina = machineId,
        });
        return PostSync("api/validacao/instalacao", body);
    }

    // -------------------------------------------------------------------------
    // Infraestrutura
    // -------------------------------------------------------------------------

    internal string ComputeSignature(string licenseId, string timestamp, string bodyJson)
    {
        // Normaliza para lowercase com hífens — igual ao servidor (idLicenca:D)
        var normalizedId = Guid.TryParse(licenseId, out var parsed)
            ? parsed.ToString("D")
            : licenseId;
        var payload = $"{normalizedId}:{timestamp}:{bodyJson}";
        var key     = Encoding.UTF8.GetBytes(_token);
        var data    = Encoding.UTF8.GetBytes(payload);
        using var hmac = new HMACSHA256(key);
        var hash = hmac.ComputeHash(data);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    private string PostSync(string path, string bodyJson)
    {
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
            var nonce     = Guid.NewGuid().ToString("N");
            var signature = ComputeSignature(_licenseId, timestamp, bodyJson);

            var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/{path}")
            {
                Content = new StringContent(bodyJson, Encoding.UTF8, "application/json"),
            };
            request.Headers.Add("X-Token",     _token);
            request.Headers.Add("X-Timestamp", timestamp);
            request.Headers.Add("X-Nonce",     nonce);
            request.Headers.Add("X-Signature", signature);

            HttpResponseMessage response;
            try
            {
                response = Http.SendAsync(request).GetAwaiter().GetResult();
            }
            catch (HttpRequestException) when (attempt < 3)
            {
                Thread.Sleep((int)Math.Pow(2, attempt) * 1000);
                continue;
            }

            if (((int)response.StatusCode == 429 || (int)response.StatusCode >= 500) && attempt < 3)
            {
                Thread.Sleep((int)Math.Pow(2, attempt) * 1000);
                continue;
            }

            if (!response.IsSuccessStatusCode)
            {
                var error = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                throw new COMException($"LicenseManager API error {(int)response.StatusCode}: {error}");
            }

            return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        }

        throw new COMException("Número máximo de tentativas excedido.");
    }
}
