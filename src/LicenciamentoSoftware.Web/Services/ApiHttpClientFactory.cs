using LicenciamentoSoftware.Client.Services;

namespace LicenciamentoSoftware.Web.Services;

/// <summary>
/// Fábrica central que mantém uma instância única de cada HttpClient autenticado.
/// Quando o token muda (login/refresh/logout), atualiza o DefaultRequestHeaders
/// de todos os clients de uma vez — eliminando a necessidade de DelegatingHandler.
/// </summary>
public sealed class ApiHttpClientFactory
{
    private readonly HttpClient _baseClient;

    public ClienteFinalApiService ClienteFinal { get; }
    public UsuarioApiService Usuario { get; }
    public AplicacaoApiService Aplicacao { get; }
    public TipoLicencaApiService TipoLicenca { get; }
    public LicencaApiService Licenca { get; }

    // Mantém referência para atualizar o token em todos os clients
    private readonly List<HttpClient> _authenticatedClients = [];

    public ApiHttpClientFactory(string baseAddress)
    {
        // Cada service recebe seu próprio HttpClient para evitar conflitos de estado
        ClienteFinal = CriarService(baseAddress, c => new ClienteFinalApiService(c));
        Usuario      = CriarService(baseAddress, c => new UsuarioApiService(c));
        Aplicacao    = CriarService(baseAddress, c => new AplicacaoApiService(c));
        TipoLicenca  = CriarService(baseAddress, c => new TipoLicencaApiService(c));
        Licenca      = CriarService(baseAddress, c => new LicencaApiService(c));

        // Client anônimo para uso interno
        _baseClient = new HttpClient { BaseAddress = new Uri(baseAddress) };
    }

    /// <summary>Define o Bearer token em todos os HttpClients autenticados.</summary>
    public void SetToken(string accessToken)
    {
        foreach (var client in _authenticatedClients)
        {
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        }
    }

    /// <summary>Remove o token de todos os HttpClients (logout).</summary>
    public void ClearToken()
    {
        foreach (var client in _authenticatedClients)
            client.DefaultRequestHeaders.Authorization = null;
    }

    private T CriarService<T>(string baseAddress, Func<HttpClient, T> factory)
    {
        var client = new HttpClient { BaseAddress = new Uri(baseAddress) };
        _authenticatedClients.Add(client);
        return factory(client);
    }
}
