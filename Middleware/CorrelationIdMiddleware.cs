namespace InterviewLab.Middleware;

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        // Middleware Q366: accept a valid caller correlation ID or create one, then return it in the response.
        var id = context.Request.Headers.TryGetValue("X-Correlation-ID", out var incoming) &&
                 Guid.TryParse(incoming, out var parsed) ? parsed.ToString() : Guid.NewGuid().ToString();
        context.TraceIdentifier = id;
        context.Response.Headers["X-Correlation-ID"] = id;
        await next(context);
    }
}
