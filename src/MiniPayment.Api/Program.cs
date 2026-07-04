using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MiniPayment.Api.Middleware;
using MiniPayment.Application;
using MiniPayment.Infrastructure;
using MiniPayment.Infrastructure.Logging;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog(LoggingRegistration.ConfigureSerilog);

// Application + Infrastructure
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Controllers + problem details
builder.Services.AddControllers();
builder.Services.AddProblemDetails();

// JWT authentication
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is not configured.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Mini Payment API",
        Version = "v1",
        Description = "Simulated payment gateway — Assignment 1"
    });

    c.EnableAnnotations();

    // JWT security definition
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token. Obtain one from POST /api/v1/dev/token (Development only)."
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            []
        }
    });

    // Include XML comments
    var xmlFile = $"{typeof(Program).Assembly.GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        c.IncludeXmlComments(xmlPath);
});

// Health check
builder.Services.AddHealthChecks();

var app = builder.Build();

// Auto-apply migrations in Development
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<MiniPayment.Infrastructure.Persistence.ApplicationDbContext>();
    dbContext.Database.Migrate();
}

// Middleware pipeline
app.UseMiddleware<CorrelationIdMiddleware>();   // must be outermost — pushes CorrelationId for all downstream logs
app.UseSerilogRequestLogging(opts =>
{
    opts.MessageTemplate =
        "HTTP {RequestMethod} {StatusCode} in {Elapsed:0.0}ms | ip={ClientIp} ua={UserAgent}";
    opts.EnrichDiagnosticContext = (diag, ctx) =>
    {
        diag.Set("RequestMethod", ctx.Request.Method);
        diag.Set("UserAgent",     ctx.Request.Headers.UserAgent.ToString());
        diag.Set("ClientIp",      ctx.Connection.RemoteIpAddress?.ToString() ?? "-");
    };
});
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Mini Payment API v1"));
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

// Required for WebApplicationFactory in integration tests
public partial class Program { }
