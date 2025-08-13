using System.Collections.Concurrent;
using System.Diagnostics;

public class RequestResponseLoggingMiddleware 
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

    // Per-endpoint counter
    private static readonly ConcurrentDictionary<string, int> _endpointCounter = new();

    public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var endpointKey = $"{context.Request.Method} {context.Request.Path}";
        var count = _endpointCounter.AddOrUpdate(endpointKey, 1, (_, old) => old + 1);

        var requestTime = DateTime.UtcNow;
        var sw = Stopwatch.StartNew();

        _logger.LogInformation(
            "[{time}] Incoming Request #{count} to {endpoint} | Query: {query}",
            requestTime.ToString("yyyy-MM-dd HH:mm:ss.fff"),
            count,
            endpointKey,
            context.Request.QueryString.Value
        );

        // Keep original body stream
        var originalBodyStream = context.Response.Body;

        var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        // Call next middleware
        await _next(context);

        sw.Stop();

        // Log the response
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseText = await new StreamReader(context.Response.Body).ReadToEndAsync();
        context.Response.Body.Seek(0, SeekOrigin.Begin);

        _logger.LogInformation(
            "[{time}] Response to {endpoint} (#{count}) | Status: {statusCode} | Duration: {duration} ms | Body: {body}",
            DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff"),
            endpointKey,
            count,
            context.Response.StatusCode,
            sw.ElapsedMilliseconds,
            responseText
        );

        // Copy back to original stream
        await responseBody.CopyToAsync(originalBodyStream);
    }
}
