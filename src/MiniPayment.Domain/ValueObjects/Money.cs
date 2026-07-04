namespace MiniPayment.Domain.ValueObjects;

public sealed record Money(decimal Amount, string Currency)
{
    /// <summary>
    /// Returns true when the two decimal digits of the amount are "00"
    /// (e.g. 10.00 → approved, 10.05 → not approved).
    /// </summary>
    public bool IsApprovable()
    {
        // Multiply by 100 and take the last two digits of the integer part.
        var minorUnits = (long)Math.Round(Amount * 100, MidpointRounding.AwayFromZero);
        return minorUnits % 100 == 0;
    }

    /// <summary>
    /// Returns the two-digit response code derived from the decimal portion.
    /// e.g. 10.00 → "00", 10.05 → "05".
    /// </summary>
    public string ResponseCode()
    {
        var minorUnits = (long)Math.Round(Amount * 100, MidpointRounding.AwayFromZero);
        return (minorUnits % 100).ToString("D2");
    }
}
