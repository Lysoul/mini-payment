using Microsoft.EntityFrameworkCore;
using MiniPayment.Domain.Entities;

namespace MiniPayment.Application.Common.Abstractions;

public interface IApplicationDbContext
{
    DbSet<Transaction> Transactions { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
