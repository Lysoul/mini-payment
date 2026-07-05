using FluentValidation;
using MiniPayment.Reconciliation.Models;

namespace MiniPayment.Reconciliation.Validation;

public sealed class ListARecordValidator : AbstractValidator<ListARecord>
{
    public ListARecordValidator()
    {
        RuleFor(x => x.OrderNumber).NotEmpty().WithMessage("OrderNumber is required.");
        RuleFor(x => x.Date).NotEmpty().Must(BeAValidDate)
            .WithMessage("Date must be a valid date.");
        RuleFor(x => x.Amount).NotEmpty().Must(BeAValidDecimal)
            .WithMessage("Amount must be a valid decimal number.");
    }

    private static bool BeAValidDate(string date) =>
        DateOnly.TryParseExact(date, ["yyyy-MM-dd", "dd-MM-yyyy", "dd/MM/yyyy", "MM/dd/yyyy"],
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out _);

    private static bool BeAValidDecimal(string amount) =>
        decimal.TryParse(amount, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out _);
}
