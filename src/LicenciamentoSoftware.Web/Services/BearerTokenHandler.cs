namespace LicenciamentoSoftware.Web.Services;

/// <summary>
/// DelegatingHandler simples que adiciona o Bearer token de memória
/// em todas as requisições HTTP saintes.
/// Diferente do TokenRefreshHandler, este não tenta renovar — só adiciona.
/// O TokenRefreshHandler cuida da renovação quando recebe 401.
/// </summary>
public sealed class BearerTokenHandler(JwtAuthStateProvider authProvider) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var token = authProvider.AccessToken;
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
