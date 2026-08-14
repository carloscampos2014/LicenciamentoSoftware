using System.Globalization;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LicenseManagerSdk;

/// <summary>
/// Cliente para a API de validação do LicenseManager.
/// Encapsula geração de HMAC-SHA256 e os 4 endpoints de validação.
/// </summary>
public sealed class LicenseManagerClient : IDisposable
{
    private readonly HttpClient _http;
    private readonly string _token;
    private readonly string _licenseId;
    private readonly bool _ownsHttpClient;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy        = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition      = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// Cria um novo cliente com URL base, token e ID da licença.
    /// </summary>
    /// <param name="baseUrl">URL base da API (ex: https://licensemanager-api.enzojb.com.br)</param>
    /// <param name="token">Token de autenticação da licença (obtido na emissão)</param>
    /// <param name="licenseId">GUID da licença</param>
    /// <param name="httpClient">HttpClient opcional (útil para testes e injeção de dependência)</param>
    public LicenseManagerClient(string baseUrl, string token, string licenseId, HttpClient? httpClient = null)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))   throw new ArgumentException("baseUrl é obrigatório.",   nameof(baseUrl));
        if (string.IsNullOrWhiteSpace(token))     throw new ArgumentException("token é obrigatório.",     nameof(token));
        if (string.IsNullOrWhiteSpace(licenseId)) throw new ArgumentException("licenseId é obrigatório.", nameof(licenseId));

        _token     = token;
        _licenseId = Guid.TryParse(licenseId, out var parsed)
            ? parsed.ToString("D")   // normaliza para lowercase com hífens uma única vez
            : licenseId;

        if (httpClient is not null)
        {
            _http           = httpClient;
            _ownsHttpClient = false;
        }
        else
        {
            _http = new HttpClient
            {
                BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/"),
                Timeout     = TimeSpan.FromSeconds(30),
            };
            _ownsHttpClient = true;
        }
    }

    // -------------------------------------------------------------------------
    // Endpoints públicos
    // -------------------------------------------------------------------------

    /// <summary>Valida login de um usuário numa licença.</summary>
    /// <param name="userId">Identificador único do usuário na aplicação cliente</param>
    /// <param name="ct">Token de cancelamento</param>
    /// <returns>Resultado com <see cref="LoginResult.SessionId"/> em caso de sucesso</returns>
    public async Task<LoginResult> LoginAsync(string userId, CancellationToken ct = default)
    {
        var body = new { IdLicenca = _licenseId, IdentificadorUsuario = userId };
        var response = await SendAsync("api/validacao/login", body, ct).ConfigureAwait(false);
        return await DeserializeAsync<LoginResult>(response, ct).ConfigureAwait(false);
    }

    /// <summary>Envia heartbeat para manter a sessão ativa.</summary>
    /// <param name="sessionId">ID da sessão retornado pelo <see cref="LoginAsync"/></param>
    /// <param name="ct">Token de cancelamento</param>
    public async Task HeartbeatAsync(string sessionId, CancellationToken ct = default)
    {
        var body = new { IdLicenca = _licenseId, IdSessao = sessionId };
        var response = await SendAsync("api/validacao/heartbeat", body, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>Encerra a sessão (idempotente).</summary>
    /// <param name="sessionId">ID da sessão a encerrar</param>
    /// <param name="ct">Token de cancelamento</param>
    public async Task LogoutAsync(string sessionId, CancellationToken ct = default)
    {
        var body = new { IdLicenca = _licenseId, IdSessao = sessionId };
        var response = await SendAsync("api/validacao/logout", body, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>Valida ou registra uma instalação da aplicação cliente.</summary>
    /// <param name="machineId">Identificador único da máquina/dispositivo</param>
    /// <param name="ct">Token de cancelamento</param>
    public async Task<InstallationResult> ValidateInstallationAsync(string machineId, CancellationToken ct = default)
    {
        var body = new { IdLicenca = _licenseId, IdentificadorMaquina = machineId };
        var response = await SendAsync("api/validacao/instalacao", body, ct).ConfigureAwait(false);
        return await DeserializeAsync<InstallationResult>(response, ct).ConfigureAwait(false);
    }

    // -------------------------------------------------------------------------
    // Infraestrutura HMAC
    // -------------------------------------------------------------------------

    private async Task<HttpResponseMessage> SendAsync(string path, object body, CancellationToken ct)
    {
        var bodyJson  = JsonSerializer.Serialize(body, JsonOpts);

        var attempts = 0;
        while (true)
        {
            attempts++;
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
            var nonce     = Guid.NewGuid().ToString("N");
            var signature = ComputeSignature(_licenseId, timestamp, bodyJson);

            var request = new HttpRequestMessage(HttpMethod.Post, path)
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
                response = await _http.SendAsync(request, ct).ConfigureAwait(false);
            }
            catch (HttpRequestException) when (attempts < 3)
            {
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempts)), ct).ConfigureAwait(false);
                continue;
            }

            if (attempts < 3 && ((int)response.StatusCode == 429 || (int)response.StatusCode >= 500))
            {
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempts)), ct).ConfigureAwait(false);
                continue;
            }

            if (!response.IsSuccessStatusCode)
                throw new LicenseManagerException(response.StatusCode, await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false));

            return response;
        }
    }

    private string ComputeSignature(string licenseId, string timestamp, string bodyJson)
    {
        // licenseId já foi normalizado no construtor (lowercase com hífens)
        var payload = $"{licenseId}:{timestamp}:{bodyJson}";
        var key     = Encoding.UTF8.GetBytes(_token);
        var data    = Encoding.UTF8.GetBytes(payload);
        var hash    = HMACSHA256.HashData(key, data);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static async Task<T> DeserializeAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        var result = await response.Content.ReadFromJsonAsync<T>(JsonOpts, ct).ConfigureAwait(false);
        return result ?? throw new LicenseManagerException(response.StatusCode, "Resposta vazia da API.");
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_ownsHttpClient)
            _http.Dispose();
    }
}

// -------------------------------------------------------------------------
// Modelos de resposta
// -------------------------------------------------------------------------

/// <summary>Resultado do endpoint de login.</summary>
public sealed record LoginResult(
    [property: JsonPropertyName("autorizado")] bool Authorized,
    [property: JsonPropertyName("idSessao")]   string? SessionId);

/// <summary>Resultado do endpoint de validação de instalação.</summary>
public sealed record InstallationResult(
    [property: JsonPropertyName("autorizado")]    bool Authorized,
    [property: JsonPropertyName("idInstalacao")]  string? InstallationId,
    [property: JsonPropertyName("jaRegistrada")]  bool AlreadyRegistered);

// -------------------------------------------------------------------------
// Exceção
// -------------------------------------------------------------------------

/// <summary>Exceção lançada quando a API retorna um erro HTTP.</summary>
public sealed class LicenseManagerException : Exception
{
    /// <summary>Código HTTP de status retornado pela API.</summary>
    public System.Net.HttpStatusCode StatusCode { get; }

    /// <summary>Corpo da resposta de erro.</summary>
    public string ResponseBody { get; }

    /// <summary>Cria uma nova instância com o status e corpo da resposta.</summary>
    /// <param name="statusCode">Código HTTP de status</param>
    /// <param name="responseBody">Corpo da resposta</param>
    public LicenseManagerException(System.Net.HttpStatusCode statusCode, string responseBody)
        : base($"LicenseManager API error {(int)statusCode}: {responseBody}")
    {
        StatusCode   = statusCode;
        ResponseBody = responseBody;
    }
}
