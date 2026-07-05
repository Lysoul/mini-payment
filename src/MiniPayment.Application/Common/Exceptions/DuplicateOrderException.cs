namespace MiniPayment.Application.Common.Exceptions;

public sealed class DuplicateOrderException(string orderNumber)
    : Exception($"Order '{orderNumber}' already exists.");
