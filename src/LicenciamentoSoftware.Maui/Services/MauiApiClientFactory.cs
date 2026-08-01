using LicenciamentoSoftware.Client.Services;

namespace LicenciamentoSoftware.Maui.Services;

/// <summary>
/// Fábrica central de HttpClients autenticados para o MAUI.
/// Mantém um HttpClient por service e atualiza o Bearer token em todos de uma vez.
/// Equivalente ao ApiHttpClientFactory do Blazor Web, mas sem BFF.
/// </summary>
public sealed class MauiApiClientFactory
{
    public AuthApiService Auth { get; }
    public ClienteFinalApiService ClienteFinal { get; }
    public UsuarioApiService Usuario { get; }
    public AplicacaoApiService Aplicacao { get; }
    public TipoLicencaApiService TipoLicenca { get; }
    public LicencaApiService Licenca { get; }
    public DashboardApiService Dashboard { get; }

    private readonly List<HttpClient> _clients = [];

    public MauiApiClientFactory(string baseUrl)
    {
        var uri = new Uri(baseUrl.TrimEnd('/') + "/");

        Auth        = Criar(uri, c => new AuthApiService(c));
        ClienteFinal = Criar(uri, c => new ClienteFinalApiService(c));
        Usuario     = Criar(uri, c => new UsuarioApiService(c));
        Aplicacao   = Criar(uri, c => new AplicacaoApiService(c));
        TipoLicenca = Criar(uri, c => new TipoLicencaApiService(c));
        Licenca     = Criar(uri, c => new LicencaApiService(c));
        Dashboard   = Criar(uri, c => new DashboardApiService(c));
    }

    /// <summary>Define o Bearer token em todos os HttpClients autenticados.</summary>
    public void SetToken(string accessToken)
    {
        foreach (var client in _clients)
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
    }

    /// <summary>Remove o token de todos os HttpClients (logout).</summary>
    public void ClearToken()
    {
        foreach (var client in _clients)
            client.DefaultRequestHeaders.Authorization = null;
    }

    private T Criar<T>(Uri baseUri, Func<HttpClient, T> factory)
    {
        // Android em dev requer bypass de certificado self-signed
        HttpMessageHandler handler;
#if DEBUG && ANDROID
        handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        };
#else
        handler = new HttpClientHandler();
#endif
        var client = new HttpClient(handler) { BaseAddress = baseUri };
        _clients.Add(client);
        return factory(client);
    }
}
