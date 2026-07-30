namespace LicenciamentoSoftware.Application.Abstractions;

/// <summary>
/// Abstração de relógio para facilitar testes unitários determinísticos.
/// </summary>
public interface IClock
{
    DateTime UtcNow { get; }
}
