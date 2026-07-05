using Microsoft.EntityFrameworkCore;
using MiniPayment.Application.Common.Abstractions;
using MiniPayment.Application.Common.Exceptions;
using MiniPayment.Domain.Entities;
using Npgsql;

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
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            throw new DuplicateOrderException(transaction.OrderNumber);
        }
    }
}
