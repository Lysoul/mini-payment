using FluentAssertions;
using MiniPayment.Application.Features.Payments.ProcessPayment;

namespace MiniPayment.Application.UnitTests.Payments;

public class ProcessPaymentCommandValidatorTests
{
    private readonly ProcessPaymentCommandValidator _sut = new();

    [Fact]
    public void ValidCommand_PassesValidation()
    {
        var result = _sut.Validate(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void EmptyOrderNumber_Fails(string orderNumber)
    {
        var cmd = ValidCommand() with { OrderNumber = orderNumber };
        _sut.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void OrderNumber_Over64Chars_Fails()
    {
        var cmd = ValidCommand() with { OrderNumber = new string('A', 65) };
        _sut.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("411111111111111")]  // 15 digits
    [InlineData("41111111111111111")] // 17 digits
    [InlineData("411111111111111A")]  // non-digit
    public void InvalidCardNumberLength_Fails(string cardNumber)
    {
        var cmd = ValidCommand() with { CardNumber = cardNumber };
        _sut.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CardNumber_FailsLuhn_Fails()
    {
        var cmd = ValidCommand() with { CardNumber = "1234567890123456" };
        _sut.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("13/25")]   // invalid month
    [InlineData("00/25")]
    [InlineData("1225")]    // wrong format
    [InlineData("12/20")]   // past date
    public void InvalidExpiry_Fails(string expiry)
    {
        var cmd = ValidCommand() with { ExpiryDate = expiry };
        _sut.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("12")]      // too short
    [InlineData("12345")]   // too long
    [InlineData("12A")]     // non-digit
    public void InvalidCvv_Fails(string cvv)
    {
        var cmd = ValidCommand() with { Cvv = cvv };
        _sut.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("usd")]     // lowercase
    [InlineData("US")]      // 2 chars
    [InlineData("USDT")]    // 4 chars
    [InlineData("XYZ")]     // not in whitelist
    public void InvalidCurrency_Fails(string currency)
    {
        var cmd = ValidCommand() with { Currency = currency };
        _sut.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("notanemail")]
    [InlineData("@no-local.com")]
    public void InvalidEmail_Fails(string email)
    {
        var cmd = ValidCommand() with { Email = email };
        _sut.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void NegativeAmount_Fails()
    {
        var cmd = ValidCommand() with { Amount = -1m };
        _sut.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void AmountWithThreeDecimals_Fails()
    {
        var cmd = ValidCommand() with { Amount = 10.001m };
        _sut.Validate(cmd).IsValid.Should().BeFalse();
    }

    private static ProcessPaymentCommand ValidCommand() =>
        new()
        {
            OrderNumber = "ORD-001",
            CardNumber = "4111111111111111",
            ExpiryDate = "12/29",
            Cvv = "123",
            Currency = "USD",
            CardholderName = "John Doe",
            Email = "john@example.com",
            Amount = 10.00m
        };
}
