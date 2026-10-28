using System.Text.Json;

namespace InterviewLab.Middleware;

public sealed class DemoExceptionMiddleware(RequestDelegate next, ILogger<DemoExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
        catch (Exception exception)
        {
            // Middleware Q355, Q356, Q364; Web API Q34, Q42: log details internally and return sanitized Problem Details.
            logger.LogError(exception, "Unhandled request failure. CorrelationId={CorrelationId}", context.TraceIdentifier);
            if (context.Response.HasStarted) throw;
            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/problem+json";
            await JsonSerializer.SerializeAsync(context.Response.Body, new
            {
                type = "about:blank", title = "An unexpected error occurred", status = 500,
                traceId = context.TraceIdentifier
            });
        }
    }
}
