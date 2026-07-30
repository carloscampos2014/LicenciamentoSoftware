using System.Data;

namespace LicenciamentoSoftware.Application.Abstractions;

/// <summary>
/// Unidade de trabalho baseada em Dapper.
/// Abre uma transação serializable, expõe a conexão para os repositórios
/// e faz commit/rollback atômico ao final do caso de uso.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    IDbConnection Connection { get; }
    IDbTransaction Transaction { get; }

    Task BeginAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default);

    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
