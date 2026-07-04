using MiniPayment.Domain.Entities;

namespace MiniPayment.Application.Common.Abstractions;

public interface ITransactionRepository
{
    Task<Transaction?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default);
    Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default);
}
