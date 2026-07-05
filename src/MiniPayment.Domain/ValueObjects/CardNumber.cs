namespace MiniPayment.Domain.ValueObjects;

/// <summary>
/// Wraps a PAN as a char[] so the buffer can be zeroed after use.
/// Never converts to string internally.
/// </summary>
public sealed class CardNumber : IDisposable
{
    private readonly char[] _digits;
    private bool _disposed;

    private CardNumber(char[] digits) => _digits = digits;

    public static CardNumber Parse(string raw)
    {
        var trimmed = raw.AsSpan().Trim();
        if (trimmed.Length != 16 || !IsAllDigits(trimmed))
            throw new ArgumentException("Card number must be exactly 16 digits.", nameof(raw));

        return new CardNumber(trimmed.ToArray());
    }

    /// <summary>First 6 digits (BIN / IIN).</summary>
    public string Bin => new(_digits, 0, 6);

    /// <summary>Last 4 digits.</summary>
    public string Last4 => new(_digits, 12, 4);

    /// <summary>Masked representation safe for logging: 411111******1111.</summary>
    public string Masked => $"{Bin}******{Last4}";

    public static bool PassesLuhn(string number)
    {
        var span = number.AsSpan().Trim();
        if (span.Length < 2) return false;

        int sum = 0;
        bool doubleIt = false;
        for (int i = span.Length - 1; i >= 0; i--)
        {
            if (!char.IsDigit(span[i])) return false;
            int digit = span[i] - '0';
            if (doubleIt)
            {
                digit *= 2;
                if (digit > 9) digit -= 9;
            }
            sum += digit;
            doubleIt = !doubleIt;
        }
        // sum > 0 rejects the all-zero PAN, which is divisible by 10 but not a valid card.
        return sum > 0 && sum % 10 == 0;
    }

    public void Dispose()
    {
        if (_disposed) return;
        Array.Clear(_digits, 0, _digits.Length);
        _disposed = true;
    }

    private static bool IsAllDigits(ReadOnlySpan<char> s)
    {
        foreach (var c in s)
            if (!char.IsDigit(c)) return false;
        return true;
    }
}
