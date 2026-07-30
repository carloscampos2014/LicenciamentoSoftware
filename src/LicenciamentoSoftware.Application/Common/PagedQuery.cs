namespace LicenciamentoSoftware.Application.Common;

/// <summary>
/// Base para queries de listagem paginada.
/// Herdado por todas as queries que retornam PagedResult.
/// </summary>
public abstract record PagedQuery
{
    /// <summary>Número da página (base 1). Mínimo: 1.</summary>
    public int Pagina { get; init; } = 1;

    /// <summary>Registros por página. Mínimo: 1, máximo: 100.</summary>
    public int TamanhoPagina { get; init; } = 20;

    /// <summary>Offset calculado para uso nas queries SQL.</summary>
    public int Offset => (Pagina - 1) * TamanhoPagina;
}
