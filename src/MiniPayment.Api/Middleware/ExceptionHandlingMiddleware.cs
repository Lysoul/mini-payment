using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace MiniPayment.Api.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IHostEnvironment env)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException ex)
        {
            var correlationId = context.Response.Headers["X-Correlation-Id"].FirstOrDefault();
            logger.LogWarning("Validation failed for {Path}: {Errors}", context.Request.Path,
                string.Join("; ", ex.Errors.Select(e => e.ErrorMessage)));

            context.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
            context.Response.ContentType = "application/problem+json";

            var problem = new ValidationProblemDetails
            {
                Title = "Validation failed",
                Status = StatusCodes.Status422UnprocessableEntity,
                Instance = correlationId
            };
            foreach (var error in ex.Errors.GroupBy(e => e.PropertyName))
                problem.Errors[error.Key] = error.Select(e => e.ErrorMessage).ToArray();

            await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
        }
        catch (Exception ex)
        {
            var correlationId = context.Response.Headers["X-Correlation-Id"].FirstOrDefault();
            logger.LogError(ex, "Unhandled exception for {Path}", context.Request.Path);

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/problem+json";

            var problem = new ProblemDetails
            {
                Title = "An unexpected error occurred.",
                Status = StatusCodes.Status500InternalServerError,
                Detail = env.IsDevelopment() ? ex.ToString() : null,
                Instance = correlationId
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
        }
    }
}
