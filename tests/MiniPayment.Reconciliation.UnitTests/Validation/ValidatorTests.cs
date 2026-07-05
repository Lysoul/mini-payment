using FluentAssertions;
using MiniPayment.Reconciliation.Models;
using MiniPayment.Reconciliation.Validation;

namespace MiniPayment.Reconciliation.UnitTests.Validation;

public class ValidatorTests
{
    [Fact]
    public void ListARecord_Valid_Passes()
    {
        var validator = new ListARecordValidator();
        var record = new ListARecord { OrderNumber = "ORD-001", Date = "2024-01-15", Amount = "100.00" };
        validator.Validate(record).IsValid.Should().BeTrue();
    }

    [Fact]
    public void ListARecord_EmptyOrderNumber_Fails()
    {
        var validator = new ListARecordValidator();
        var record = new ListARecord { OrderNumber = "", Date = "2024-01-15", Amount = "100.00" };
        validator.Validate(record).IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("notadate")]
    [InlineData("32/01/2024")]
    public void ListARecord_InvalidDate_Fails(string date)
    {
        var validator = new ListARecordValidator();
        var record = new ListARecord { OrderNumber = "ORD-001", Date = date, Amount = "100.00" };
        validator.Validate(record).IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("")]
    public void ListARecord_InvalidAmount_Fails(string amount)
    {
        var validator = new ListARecordValidator();
        var record = new ListARecord { OrderNumber = "ORD-001", Date = "2024-01-15", Amount = amount };
        validator.Validate(record).IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("dd-MM-yyyy date is valid", "16-04-2025")]
    [InlineData("dd/MM/yyyy date is valid", "16/04/2025")]
    [InlineData("yyyy-MM-dd date is valid", "2025-04-16")]
    public void ListARecord_SupportedDateFormats_Pass(string _, string date)
    {
        var validator = new ListARecordValidator();
        var record = new ListARecord { OrderNumber = "ORD-001", Date = date, Amount = "100.00" };
        validator.Validate(record).IsValid.Should().BeTrue();
    }

    [Fact]
    public void ListBRecord_Valid_Passes()
    {
        var validator = new ListBRecordValidator();
        var record = new ListBRecord { InvoiceNumber = "INV-001", Date = "16-04-2025", Amount = "100.00" };
        validator.Validate(record).IsValid.Should().BeTrue();
    }

    [Fact]
    public void ListBRecord_EmptyInvoiceNumber_Fails()
    {
        var validator = new ListBRecordValidator();
        var record = new ListBRecord { InvoiceNumber = "", Date = "2024-01-15", Amount = "100.00" };
        validator.Validate(record).IsValid.Should().BeFalse();
    }
}
