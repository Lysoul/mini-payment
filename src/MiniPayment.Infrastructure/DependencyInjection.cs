using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MiniPayment.Application.Common.Abstractions;
using MiniPayment.Infrastructure.Persistence;
using MiniPayment.Infrastructure.Persistence.Repositories;
using MiniPayment.Infrastructure.Services;

namespace MiniPayment.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName))
                   .UseSnakeCaseNamingConvention());

        services.AddScoped<IApplicationDbContext>(sp =>
            sp.GetRequiredService<ApplicationDbContext>());

        services.AddScoped<ITransactionRepository, TransactionRepository>();

        services.AddSingleton<IAcquirerSimulator, AcquirerSimulator>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        return services;
    }
}
