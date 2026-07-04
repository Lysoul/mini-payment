using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using MiniPayment.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace MiniPayment.Api.IntegrationTests.Infrastructure;

public class PaymentApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    internal const string TestJwtKey = "INTEGRATION_TEST_SECRET_KEY_32CH!";
    internal const string TestJwtIssuer = "MiniPaymentApi";
    internal const string TestJwtAudience = "MiniPaymentClient";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("minipayment_test")
        .WithUsername("postgres")
        .WithPassword("postgres_test")
        .Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Replace the DbContext registration with one pointing to the test container
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor is not null)
                services.Remove(descriptor);

            services.AddDbContext<ApplicationDbContext>(opts =>
                opts.UseNpgsql(_postgres.GetConnectionString()));

            // Run migrations
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.Migrate();
        });

        // ConfigureTestServices runs AFTER Program.cs registers services, so it can
        // override the JWT options that Program.cs already baked in from appsettings.json.
        builder.ConfigureTestServices(services =>
        {
            services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, opts =>
            {
                opts.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = TestJwtIssuer,
                    ValidAudience = TestJwtAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtKey))
                };
            });
        });
    }
}
