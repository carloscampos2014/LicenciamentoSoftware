using LicenciamentoSoftware.Application.Abstractions;

namespace LicenciamentoSoftware.Infrastructure.Security;

/// <summary>
/// Implementação de IClock que usa DateTime.UtcNow do sistema.
/// Substituída por FakeClock nos testes unitários.
/// </summary>
public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
