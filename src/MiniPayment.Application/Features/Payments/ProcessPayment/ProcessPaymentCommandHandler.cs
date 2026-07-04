using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MiniPayment.Application.Common.Abstractions;
using MiniPayment.Domain.Entities;
using MiniPayment.Domain.Enums;
using MiniPayment.Domain.ValueObjects;

namespace MiniPayment.Application.Features.Payments.ProcessPayment;

public sealed class ProcessPaymentCommandHandler(
    ITransactionRepository repository,
    IAcquirerSimulator acquirer,
    IDateTimeProvider clock,
    ILogger<ProcessPaymentCommandHandler> logger)
    : IRequestHandler<ProcessPaymentCommand, ProcessPaymentResult>
{
    public async Task<ProcessPaymentResult> Handle(
        ProcessPaymentCommand request,
        CancellationToken cancellationToken)
    {
        // Idempotency: return cached result for the same order_number
        var existing = await repository.GetByOrderNumberAsync(request.OrderNumber, cancellationToken);
        if (existing is not null)
        {
            logger.LogInformation(
                "Idempotent replay for order {OrderNumber} → transaction {TransactionId}",
                request.OrderNumber, existing.Id);

            return ToResult(existing);
        }

        var money = new Money(request.Amount, request.Currency);
        var (status, message) = money.IsApprovable()
            ? (PaymentStatus.Approved, "Payment Approved")
            : (PaymentStatus.Declined, "Payment Declined");

        string cardBin, cardLast4;
        using (var card = CardNumber.Parse(request.CardNumber))
        {
            cardBin = card.Bin;
            cardLast4 = card.Last4;
        }

        var transaction = Transaction.Create(
            id: Guid.NewGuid(),
            orderNumber: request.OrderNumber,
            amount: request.Amount,
            currency: request.Currency,
            cardBin: cardBin,
            cardLast4: cardLast4,
            cardholderName: request.CardholderName,
            email: request.Email,
            status: status,
            responseCode: money.ResponseCode(),
            acquirerReference: acquirer.GenerateReference(),
            timestamp: clock.UtcNow);

        try
        {
            await repository.AddAsync(transaction, cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Concurrent duplicate — re-read and return the winner's stored data
            var winner = await repository.GetByOrderNumberAsync(request.OrderNumber, cancellationToken);
            return ToResult(winner!);
        }

        return ToResult(transaction);
    }

    private static ProcessPaymentResult ToResult(Transaction t) => new(
        t.Id,
        t.AcquirerReference,
        t.ResponseCode,
        t.Status,
        t.Timestamp,
        t.Amount,
        t.Status == PaymentStatus.Approved ? "Payment Approved" : "Payment Declined");
}
