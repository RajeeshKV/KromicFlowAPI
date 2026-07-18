using KromicFlow.Application.Abstractions;
using Microsoft.EntityFrameworkCore.Storage;

namespace KromicFlow.Infrastructure.Persistence;

internal sealed class DbTransaction(IDbContextTransaction transaction) : IDbTransaction
{
    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        await transaction.RollbackAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await transaction.DisposeAsync();
    }
}
