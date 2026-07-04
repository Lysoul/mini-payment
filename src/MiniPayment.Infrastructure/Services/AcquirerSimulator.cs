using System.Security.Cryptography;
using MiniPayment.Application.Common.Abstractions;

namespace MiniPayment.Infrastructure.Services;

public sealed class AcquirerSimulator : IAcquirerSimulator
{
    private const string Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    public string GenerateReference()
    {
        var buffer = new char[12];
        for (int i = 0; i < buffer.Length; i++)
            buffer[i] = Chars[RandomNumberGenerator.GetInt32(Chars.Length)];

        return new string(buffer);
    }
}
