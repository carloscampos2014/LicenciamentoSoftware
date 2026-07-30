using LicenciamentoSoftware.Domain.Entities;

namespace LicenciamentoSoftware.Application.Abstractions;

/// <summary>
/// Porta para persistência de auditoria.
/// A implementação grava dentro da transação corrente do UnitOfWork.
/// </summary>
public interface IAuditLogWriter
{
    Task RegistrarAsync(LogOperacao log, CancellationToken cancellationToken = default);
}
