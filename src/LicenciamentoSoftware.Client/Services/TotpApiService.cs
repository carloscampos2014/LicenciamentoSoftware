using System.Net.Http.Json;

namespace LicenciamentoSoftware.Client.Services;

/// <summary>
/// Proxy HTTP para os endpoints de gerenciamento de 2FA TOTP.
/// Usado pelo Blazor WASM — o Bearer token é adicionado automaticamente
/// pelo TokenRefreshHandler.
/// </summary>
public sealed class TotpApiService(HttpClient http)
{
    /// <summary>
    /// Retorna se o 2FA está ativo para o usuário autenticado.
    /// </summary>
    public async Task<bool?> BuscarStatusAsync(CancellationToken ct = default)
    {
        try
        {
            var resultado = await http.GetFromJsonAsync<TotpStatusResponse>(
                "auth/totp/status", ct);
            return resultado?.Ativo;
        }
        catch { return null; }
    }

    /// <summary>
    /// Inicia o setup do 2FA — gera segredo e URI do QR code.
    /// Retorna o segredo em texto puro e a URI otpauth:// para o QR code.
    /// </summary>
    public async Task<(string? Segredo, string? QrCodeUri, string? Erro)> IniciarSetupAsync(
        Guid idUsuario, string email, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync(
            "auth/totp/setup", new { IdUsuario = idUsuario, Email = email }, ct);

        if (response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadFromJsonAsync<TotpSetupResponse>(ct);
            return (body?.Segredo, body?.QrCodeUri, null);
        }

        return (null, null, "Erro ao iniciar configuração do 2FA.");
    }

    /// <summary>
    /// Confirma que o autenticador foi configurado corretamente
    /// validando o primeiro código TOTP gerado pelo app.
    /// </summary>
    public async Task<(bool Sucesso, string? Erro)> ConfirmarAsync(
        string codigo, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync(
            "auth/totp/confirmar", new { Codigo = codigo }, ct);

        if (response.IsSuccessStatusCode) return (true, null);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            return (false, "Código inválido ou expirado. Verifique o autenticador.");

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return (false, "2FA não configurado. Inicie o setup novamente.");

        return (false, "Erro inesperado. Tente novamente.");
    }

    /// <summary>
    /// Desativa o 2FA após confirmação com o código atual.
    /// </summary>
    public async Task<(bool Sucesso, string? Erro)> DesativarAsync(
        string codigoAtual, CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, "auth/totp");
        request.Content = JsonContent.Create(new { CodigoAtual = codigoAtual });
        var response = await http.SendAsync(request, ct);

        if (response.IsSuccessStatusCode) return (true, null);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            return (false, "Código TOTP inválido.");

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return (false, "2FA não está ativo.");

        return (false, "Erro inesperado. Tente novamente.");
    }

    private sealed record TotpStatusResponse(bool Ativo);
    private sealed record TotpSetupResponse(string? Segredo, string? QrCodeUri);
}
