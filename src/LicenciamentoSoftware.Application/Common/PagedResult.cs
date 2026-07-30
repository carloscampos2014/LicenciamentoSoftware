namespace LicenciamentoSoftware.Application.Common;

/// <summary>
/// Resultado paginado genérico retornado por todos os handlers de listagem.
/// </summary>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Itens,
    int Total,
    int Pagina,
    int TamanhoPagina)
{
    public int TotalPaginas => TamanhoPagina > 0
        ? (int)Math.Ceiling((double)Total / TamanhoPagina)
        : 0;
}
