namespace LicenciamentoSoftware.Infrastructure.Persistence;

/// <summary>
/// Lançada quando o DbUp falha ao aplicar um script de migration.
/// </summary>
public sealed class MigrationException : Exception
{
    public MigrationException(string message, Exception? innerException = null)
        : base(message, innerException) { }
}
