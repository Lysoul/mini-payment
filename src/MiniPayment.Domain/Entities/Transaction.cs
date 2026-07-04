using MiniPayment.Domain.Enums;

namespace MiniPayment.Domain.Entities;

public class Transaction
{
    private Transaction() { }

    public Guid Id { get; private set; }
    public string OrderNumber { get; private set; } = null!;
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = null!;
    public string CardBin { get; private set; } = null!;
    public string CardLast4 { get; private set; } = null!;
    public string CardholderName { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public PaymentStatus Status { get; private set; }
    public string ResponseCode { get; private set; } = null!;
    public string AcquirerReference { get; private set; } = null!;
    public DateTimeOffset Timestamp { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static Transaction Create(
        Guid id,
        string orderNumber,
        decimal amount,
        string currency,
        string cardBin,
        string cardLast4,
        string cardholderName,
        string email,
        PaymentStatus status,
        string responseCode,
        string acquirerReference,
        DateTimeOffset timestamp)
    {
        return new Transaction
        {
            Id = id,
            OrderNumber = orderNumber,
            Amount = amount,
            Currency = currency,
            CardBin = cardBin,
            CardLast4 = cardLast4,
            CardholderName = cardholderName,
            Email = email,
            Status = status,
            ResponseCode = responseCode,
            AcquirerReference = acquirerReference,
            Timestamp = timestamp,
            CreatedAt = timestamp
        };
    }
}
