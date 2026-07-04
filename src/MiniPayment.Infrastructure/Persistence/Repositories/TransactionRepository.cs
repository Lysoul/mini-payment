using Microsoft.EntityFrameworkCore;
using MiniPayment.Application.Common.Abstractions;
using MiniPayment.Domain.Entities;

namespace MiniPayment.Infrastructure.Persistence.Repositories;

public sealed class TransactionRepository(ApplicationDbContext db) : ITransactionRepository
{
    public Task<Transaction?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default)
        => db.Transactions
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.OrderNumber == orderNumber, cancellationToken);

    public async Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        db.Transactions.Add(transaction);
        await db.SaveChangesAsync(cancellationToken);
    }
}
