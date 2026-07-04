using System.Text.Json.Serialization;

namespace MiniPayment.Api.Models;

/// <summary>Payment request payload.</summary>
public sealed class PayRequest
{
    /// <summary>Merchant order number (unique per transaction attempt).</summary>
    [JsonPropertyName("order_number")]
    public string OrderNumber { get; set; } = null!;

    /// <summary>16-digit card number.</summary>
    [JsonPropertyName("card_number")]
    public string CardNumber { get; set; } = null!;

    /// <summary>Card expiry date in MM/YY format.</summary>
    [JsonPropertyName("expiry_date")]
    public string ExpiryDate { get; set; } = null!;

    /// <summary>Card CVV/CVC (3 or 4 digits).</summary>
    [JsonPropertyName("cvv")]
    public string Cvv { get; set; } = null!;

    /// <summary>ISO 4217 currency code (e.g. USD).</summary>
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = null!;

    /// <summary>Name of the cardholder.</summary>
    [JsonPropertyName("cardholder_name")]
    public string CardholderName { get; set; } = null!;

    /// <summary>Cardholder email address.</summary>
    [JsonPropertyName("email")]
    public string Email { get; set; } = null!;

    /// <summary>Transaction amount (e.g. 10.00).</summary>
    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }
}
