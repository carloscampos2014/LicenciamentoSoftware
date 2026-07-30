namespace LicenciamentoSoftware.Application.Licenca.Results;

/// <summary>
/// Resultado discriminado da operação de emissão/renovação de token HMAC.
/// </summary>
public abstract record EmitirTokenResult
{
    private EmitirTokenResult() { }

    /// <summary>
    /// Token emitido com sucesso.
    /// <para>
    /// <b>Atenção:</b> <see cref="TokenTexto"/> é o único momento em que o segredo
    /// é exposto em texto puro — exiba-o ao usuário e descarte após a resposta.
    /// </para>
    /// </summary>
    public sealed record Sucesso(
        Guid IdToken,
        Guid IdLicenca,
        string TokenTexto,
        int ExpiracaoMinutos) : EmitirTokenResult;

    /// <summary>Licença não encontrada para o ID informado.</summary>
    public sealed record LicencaNaoEncontrada : EmitirTokenResult;

    /// <summary>Licença existe mas está inativa.</summary>
    public sealed record LicencaInativa : EmitirTokenResult;

    /// <summary>Já existe um token ativo — use renovar ou revogue antes de emitir.</summary>
    public sealed record TokenJaExiste : EmitirTokenResult;
}
