using FluentValidation;
using MiniPayment.Domain.ValueObjects;

namespace MiniPayment.Application.Features.Payments.ProcessPayment;

public sealed class ProcessPaymentCommandValidator : AbstractValidator<ProcessPaymentCommand>
{
    private static readonly HashSet<string> ValidCurrencies =
    [
        "USD", "EUR", "GBP", "JPY", "THB", "SGD", "HKD", "AUD", "CAD", "CHF",
        "CNY", "SEK", "NZD", "MXN", "NOK", "KRW", "TRY", "INR", "BRL", "ZAR"
    ];

    public ProcessPaymentCommandValidator()
    {
        RuleFor(x => x.OrderNumber)
            .NotEmpty().WithMessage("order_number is required.")
            .MaximumLength(64).WithMessage("order_number must not exceed 64 characters.");

        RuleFor(x => x.CardNumber)
            .NotEmpty().WithMessage("card_number is required.")
            .Must(n => n.Length == 16 && n.All(char.IsDigit))
                .WithMessage("card_number must be exactly 16 digits.")
            .Must(CardNumber.PassesLuhn)
                .WithMessage("card_number failed Luhn check.");

        RuleFor(x => x.ExpiryDate)
            .NotEmpty().WithMessage("expiry_date is required.")
            .Matches(@"^(0[1-9]|1[0-2])\/\d{2}$").WithMessage("expiry_date must be in MM/YY format.")
            .Must(IsExpiryInFuture).WithMessage("expiry_date must be a future date.");

        RuleFor(x => x.Cvv)
            .NotEmpty().WithMessage("cvv is required.")
            .Matches(@"^\d{3,4}$").WithMessage("cvv must be 3 or 4 digits.");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("currency is required.")
            .Matches(@"^[A-Z]{3}$").WithMessage("currency must be ISO 4217 format (3 uppercase letters).")
            .Must(c => ValidCurrencies.Contains(c)).WithMessage("currency is not supported.");

        RuleFor(x => x.CardholderName)
            .NotEmpty().WithMessage("cardholder_name is required.")
            .MaximumLength(255);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("email is required.")
            .Matches(@"^[^@\s]+@[^@\s]+\.[^@\s]{2,}$")
                .WithMessage("email is not a valid email address.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("amount must be greater than 0.")
            .LessThanOrEqualTo(1_000_000).WithMessage("amount exceeds maximum allowed.")
            .Must(HaveAtMostTwoDecimalPlaces).WithMessage("amount must have at most 2 decimal places.");
    }

    private static bool IsExpiryInFuture(string expiry)
    {
        if (!System.Text.RegularExpressions.Regex.IsMatch(expiry, @"^(0[1-9]|1[0-2])\/\d{2}$"))
            return false;

        var parts = expiry.Split('/');
        int month = int.Parse(parts[0]);
        int year = 2000 + int.Parse(parts[1]);
        var expiryEndOfMonth = new DateTimeOffset(year, month, 1, 0, 0, 0, TimeSpan.Zero)
            .AddMonths(1).AddTicks(-1);

        return expiryEndOfMonth > DateTimeOffset.UtcNow;
    }

    private static bool HaveAtMostTwoDecimalPlaces(decimal amount)
    {
        return amount == Math.Round(amount, 2);
    }
}
