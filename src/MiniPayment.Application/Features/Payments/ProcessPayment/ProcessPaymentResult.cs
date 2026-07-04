using MiniPayment.Domain.Enums;

namespace MiniPayment.Application.Features.Payments.ProcessPayment;

public sealed record ProcessPaymentResult(
    Guid TransactionId,
    string AcquirerReference,
    string ResponseCode,
    PaymentStatus Status,
    DateTimeOffset Timestamp,
    decimal Amount,
    string Message);
