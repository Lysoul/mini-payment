using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MiniPayment.Application.Common.Abstractions;
using MiniPayment.Application.Features.Payments.ProcessPayment;
using MiniPayment.Domain.Entities;
using MiniPayment.Domain.Enums;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace MiniPayment.Application.UnitTests.Payments;

public class ProcessPaymentCommandHandlerTests
{
    private readonly ITransactionRepository _repository;
    private readonly IAcquirerSimulator _acquirer;
    private readonly IDateTimeProvider _clock;
    private readonly ProcessPaymentCommandHandler _sut;

    private static readonly DateTimeOffset FixedTime =
        new(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public ProcessPaymentCommandHandlerTests()
    {
        _repository = Substitute.For<ITransactionRepository>();
        _acquirer = Substitute.For<IAcquirerSimulator>();
        _acquirer.GenerateReference().Returns("MOCK123456789");
        _clock = Substitute.For<IDateTimeProvider>();
        _clock.UtcNow.Returns(FixedTime);

        _sut = new ProcessPaymentCommandHandler(
            _repository, _acquirer, _clock,
            NullLogger<ProcessPaymentCommandHandler>.Instance);
    }

    [Theory]
    [InlineData(10.00, "APPROVED", "00")]
    [InlineData(100.00, "APPROVED", "00")]
    [InlineData(1000.00, "APPROVED", "00")]
    public async Task Amount_EndingIn00_ReturnsApproved(decimal amount, string expectedStatus, string expectedCode)
    {
        _repository.GetByOrderNumberAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Transaction?)null);

        var result = await _sut.Handle(BuildCommand("ORD-A", amount), CancellationToken.None);

        result.Status.ToString().ToUpper().Should().Be(expectedStatus);
        result.ResponseCode.Should().Be(expectedCode);
        result.TransactionId.Should().NotBe(Guid.Empty);
        result.AcquirerReference.Should().Be("MOCK123456789");
    }

    [Theory]
    [InlineData(10.05, "DECLINED", "05")]
    [InlineData(10.99, "DECLINED", "99")]
    [InlineData(10.10, "DECLINED", "10")]
    public async Task Amount_NotEndingIn00_ReturnsDeclined(decimal amount, string expectedStatus, string expectedCode)
    {
        _repository.GetByOrderNumberAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Transaction?)null);

        var result = await _sut.Handle(BuildCommand("ORD-D", amount), CancellationToken.None);

        result.Status.ToString().ToUpper().Should().Be(expectedStatus);
        result.ResponseCode.Should().Be(expectedCode);
    }

    [Fact]
    public async Task DuplicateOrderNumber_ReturnsSameTransactionId()
    {
        var stored = Transaction.Create(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "ORD-IDEM", 10.00m, "USD", "411111", "1111",
            "John Doe", "j@ex.com", PaymentStatus.Approved, "00", "REF001", FixedTime);

        _repository.GetByOrderNumberAsync("ORD-IDEM", Arg.Any<CancellationToken>())
            .Returns(stored);

        var result = await _sut.Handle(BuildCommand("ORD-IDEM", 10.00m), CancellationToken.None);

        result.TransactionId.Should().Be(stored.Id);
        result.AcquirerReference.Should().Be("REF001");
        await _repository.DidNotReceive().AddAsync(Arg.Any<Transaction>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NewTransaction_CallsRepositoryAdd()
    {
        _repository.GetByOrderNumberAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Transaction?)null);

        await _sut.Handle(BuildCommand("ORD-NEW", 10.00m), CancellationToken.None);

        await _repository.Received(1).AddAsync(
            Arg.Is<Transaction>(t => t.OrderNumber == "ORD-NEW"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CardNumberIsNeverStoredFull()
    {
        _repository.GetByOrderNumberAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Transaction?)null);

        Transaction? captured = null;
        await _repository.AddAsync(
            Arg.Do<Transaction>(t => captured = t),
            Arg.Any<CancellationToken>());

        await _sut.Handle(BuildCommand("ORD-PCI", 10.00m), CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.CardBin.Should().Be("411111");
        captured.CardLast4.Should().Be("1111");
    }

    [Fact]
    public async Task Timestamp_IsSetFromClock()
    {
        _repository.GetByOrderNumberAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Transaction?)null);

        var result = await _sut.Handle(BuildCommand("ORD-TS", 10.00m), CancellationToken.None);

        result.Timestamp.Should().Be(FixedTime);
    }

    private static ProcessPaymentCommand BuildCommand(string orderNumber, decimal amount) =>
        new()
        {
            OrderNumber = orderNumber,
            CardNumber = "4111111111111111",
            ExpiryDate = "12/29",
            Cvv = "123",
            Currency = "USD",
            CardholderName = "John Doe",
            Email = "john@example.com",
            Amount = amount
        };
}
