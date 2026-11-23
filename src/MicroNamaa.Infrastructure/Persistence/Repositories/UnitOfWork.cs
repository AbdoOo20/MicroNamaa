using Microsoft.EntityFrameworkCore.Storage;

using MicroNamaa.Application.Abstractions;

namespace MicroNamaa.Infrastructure.Persistence.Repositories;

public class UnitOfWork(AppDbContext context) : IUnitOfWork, IAsyncDisposable, IDisposable
{
    private readonly Dictionary<Type, object> repositories = [];

    private IDbContextTransaction? transaction;

    public IRepository<TEntity> Repository<TEntity>() where TEntity : class
    {
        if (repositories.TryGetValue(typeof(TEntity), out var existing))
        {
            return (IRepository<TEntity>)existing;
        }

        var repository = new GenericRepository<TEntity>(context);

        repositories[typeof(TEntity)] = repository;

        return repository;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => context.SaveChangesAsync(cancellationToken);

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        transaction ??= await context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (transaction is null)
        {
            return;
        }

        await transaction.CommitAsync(cancellationToken);

        await DisposeTransactionAsync();
    }

    public void ClearChanges()
    {
        context.ChangeTracker.Clear();
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (transaction is null)
        {
            return;
        }

        await transaction.RollbackAsync(cancellationToken);

        await DisposeTransactionAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeTransactionAsync();

        GC.SuppressFinalize(this);
    }

    public void Dispose()
    {
        transaction?.Dispose();

        transaction = null;

        GC.SuppressFinalize(this);
    }

    private async Task DisposeTransactionAsync()
    {
        if (transaction is null)
        {
            return;
        }

        await transaction.DisposeAsync();

        transaction = null;
    }
}
