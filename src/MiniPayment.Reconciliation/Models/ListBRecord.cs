using CsvHelper.Configuration.Attributes;

namespace MiniPayment.Reconciliation.Models;

public sealed class ListBRecord
{
    [Name("Invoice Number")]
    public string InvoiceNumber { get; set; } = null!;

    [Name("Transaction Date")]
    public string Date { get; set; } = null!;

    [Name("Amount")]
    public string Amount { get; set; } = null!;

    [Name("Fees1")]
    [Optional]
    public string? Fees1 { get; set; }

    [Name("Fees2")]
    [Optional]
    public string? Fees2 { get; set; }

    [Name("Net Total")]
    [Optional]
    public string? NetTotal { get; set; }

    [Name("Card Number")]
    [Optional]
    public string? CardNumber { get; set; }

    [Name("Status")]
    [Optional]
    public string? Status { get; set; }
}
