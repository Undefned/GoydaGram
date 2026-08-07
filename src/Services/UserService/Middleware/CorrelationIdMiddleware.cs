using Serilog.Context;

namespace UserService.Middleware;

public class CorrelationIdMiddleware(RequestDelegate next)
{
    private const string CORRELATION_ID_HEADER = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context)
    {
        string correlationId;

        if (!context.Request.Headers.TryGetValue(CORRELATION_ID_HEADER, out var extractedId))
        {
            correlationId = Guid.NewGuid().ToString();
        }
        else
        {
            correlationId = extractedId.ToString();
        }

        context.Response.Headers[CORRELATION_ID_HEADER] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("Service", "UserService"))
        {
            await next(context);
        }
    }
}