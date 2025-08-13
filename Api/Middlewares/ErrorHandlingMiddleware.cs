using Microsoft.AspNetCore.Mvc;

public class ErrorHandlingMiddleware : IMiddleware
{
    private readonly ILogger<ErrorHandlingMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ErrorHandlingMiddleware(ILogger<ErrorHandlingMiddleware> logger, IHostEnvironment env)
    {
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            var correlationId = context.TraceIdentifier;

            var (status, title) = MapException(ex);
            var problem = new ProblemDetails
            {
                Title = title,
                Status = status,
                Type = "about:blank",
                Detail = _env.IsDevelopment() ? ex.ToString() : "An unexpected error occurred.",
                Instance = context.Request.Path
            };

            // Log full details regardless of environment
            _logger.LogError(ex, "Unhandled exception. Status={Status} CorrelationId={CorrelationId}", status, correlationId);

            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = status;
            await context.Response.WriteAsJsonAsync(problem);
        }
    }

    private static (int Status, string Title) MapException(Exception ex) =>
        ex switch
        {
            KeyNotFoundException    => (StatusCodes.Status404NotFound, "Not Found"),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            ArgumentException       => (StatusCodes.Status400BadRequest, "Bad Request"),
            // Add domain-specific mappings as needed (e.g., DuplicateEmailException -> 409)
            _                       => (StatusCodes.Status500InternalServerError, "Internal Server Error"),
        };
}