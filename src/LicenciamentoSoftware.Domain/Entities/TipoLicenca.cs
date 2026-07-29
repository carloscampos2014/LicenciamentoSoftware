namespace LicenciamentoSoftware.Domain.Entities;

/// <summary>
/// Entidade de referência com IDs fixos — nunca alterada em runtime.
/// Os IDs são os mesmos do seed do banco conforme schema.sql.
/// </summary>
public sealed class TipoLicenca
{
    public static readonly Guid IdPermanente    = new("11111111-1111-1111-1111-111111111111");
    public static readonly Guid IdPorPeriodo    = new("22222222-2222-2222-2222-222222222222");
    public static readonly Guid IdPorUsuarios   = new("33333333-3333-3333-3333-333333333333");
    public static readonly Guid IdPorInstalacao = new("44444444-4444-4444-4444-444444444444");

    public Guid Id { get; private set; }
    public string Descricao { get; private set; } = string.Empty;

    private TipoLicenca() { }
}
