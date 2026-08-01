namespace LicenciamentoSoftware.Client.Models.Common;

public sealed record PagedResult<T>(
    IReadOnlyList<T> Itens,
    int Total,
    int Pagina,
    int TamanhoPagina,
    int TotalPaginas);
