using Microsoft.AspNetCore.Mvc;
using BearNecessities.Application.Exceptions;

namespace BearNecessities.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (UnauthorizedException)
        {
            _logger.LogWarning("Unauthorized: {Method} {Path}",
                context.Request.Method, context.Request.Path);
            await WriteProblemAsync(context, StatusCodes.Status401Unauthorized,
                "Unauthorized.", "https://tools.ietf.org/html/rfc7235#section-3.1");
        }
        catch (ForbiddenException)
        {
            _logger.LogWarning("Forbidden: {Method} {Path}",
                context.Request.Method, context.Request.Path);
            await WriteProblemAsync(context, StatusCodes.Status403Forbidden,
                "Forbidden.", "https://tools.ietf.org/html/rfc7231#section-6.5.3");
        }
        catch (NotFoundException ex)
        {
            _logger.LogInformation("Not found: {Message}", ex.Message);
            await WriteProblemAsync(context, StatusCodes.Status404NotFound,
                "Not Found.", "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                detail: ex.Message);
        }
        catch (ValidationException ex)
        {
            _logger.LogInformation("Validation failed: {Method} {Path}",
                context.Request.Method, context.Request.Path);
            await WriteValidationProblemAsync(context, ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception for {Method} {Path}",
                context.Request.Method, context.Request.Path);
            await WriteInternalServerErrorAsync(context, ex);
        }
    }

    private static async Task WriteProblemAsync(
        HttpContext context, int status, string title, string type, string? detail = null)
    {
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Type = type,
            Detail = detail
        };

        await context.Response.WriteAsJsonAsync(problem, cancellationToken: default);
    }

    private static async Task WriteValidationProblemAsync(HttpContext context, ValidationException ex)
    {
        context.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status422UnprocessableEntity,
            Title = "Validation failed.",
            Type = "https://tools.ietf.org/html/rfc4918#section-11.2"
        };
        problem.Extensions["errors"] = ex.Errors;

        await context.Response.WriteAsJsonAsync(problem, cancellationToken: default);
    }

    private async Task WriteInternalServerErrorAsync(HttpContext context, Exception ex)
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected error occurred.",
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1"
        };

        if (_env.IsDevelopment())
        {
            problem.Detail = ex.Message;
            problem.Extensions["exceptionType"] = ex.GetType().FullName;
            problem.Extensions["stackTrace"] = ex.StackTrace;
        }

        await context.Response.WriteAsJsonAsync(problem, cancellationToken: default);
    }
}
