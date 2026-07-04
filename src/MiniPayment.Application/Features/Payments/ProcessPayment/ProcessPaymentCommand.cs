using Destructurama.Attributed;
using MediatR;

namespace MiniPayment.Application.Features.Payments.ProcessPayment;

public sealed record ProcessPaymentCommand : IRequest<ProcessPaymentResult>
{
    public string OrderNumber { get; init; } = null!;

    [NotLogged]
    public string CardNumber { get; init; } = null!;

    [NotLogged]
    public string ExpiryDate { get; init; } = null!;

    [NotLogged]
    public string Cvv { get; init; } = null!;

    public string Currency { get; init; } = null!;

    [NotLogged]
    public string CardholderName { get; init; } = null!;

    [NotLogged]
    public string Email { get; init; } = null!;
    public decimal Amount { get; init; }
}
