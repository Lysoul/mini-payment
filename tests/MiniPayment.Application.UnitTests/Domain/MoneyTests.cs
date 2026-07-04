using FluentAssertions;
using MiniPayment.Domain.ValueObjects;

namespace MiniPayment.Application.UnitTests.Domain;

public class MoneyTests
{
    [Theory]
    [InlineData(10.00, true, "00")]
    [InlineData(100.00, true, "00")]
    [InlineData(10.05, false, "05")]
    [InlineData(10.99, false, "99")]
    [InlineData(10.10, false, "10")]
    public void IsApprovable_And_ResponseCode_AreCorrect(decimal amount, bool approvable, string code)
    {
        var money = new Money(amount, "USD");
        money.IsApprovable().Should().Be(approvable);
        money.ResponseCode().Should().Be(code);
    }
}
