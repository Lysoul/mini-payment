namespace MiniPayment.Application.Common.Abstractions;

public interface IAcquirerSimulator
{
    string GenerateReference();
}
