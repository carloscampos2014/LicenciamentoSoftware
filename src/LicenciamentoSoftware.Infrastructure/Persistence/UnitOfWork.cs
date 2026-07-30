using LicenciamentoSoftware.Application.Abstractions;
using System.Data;

namespace LicenciamentoSoftware.Infrastructure.Persistence;

/// <summary>
/// Abre uma transação Npgsql e expõe IDbConnection + IDbTransaction
/// para que repositórios Dapper participem da mesma unidade de trabalho.
/// </summary>
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly DbConnectionFactory _factory;
    private IDbConnection? _connection;
    private IDbTransaction? _transaction;
    private bool _disposed;

    public UnitOfWork(DbConnectionFactory factory)
    {
        _factory = factory;
    }

    public IDbConnection Connection =>
        _connection ?? throw new InvalidOperationException(
            "UnitOfWork não foi iniciado. Chame BeginAsync() primeiro.");

    public IDbTransaction Transaction =>
        _transaction ?? throw new InvalidOperationException(
            "UnitOfWork não foi iniciado. Chame BeginAsync() primeiro.");

    public Task BeginAsync(
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default)
    {
        _connection = _factory.CreateConnection();
        _transaction = _connection.BeginTransaction(isolationLevel);
        return Task.CompletedTask;
    }

    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        _transaction?.Commit();
        return Task.CompletedTask;
    }

    public Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        _transaction?.Rollback();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _transaction?.Dispose();
        _connection?.Dispose();
        GC.SuppressFinalize(this);
    }
}
