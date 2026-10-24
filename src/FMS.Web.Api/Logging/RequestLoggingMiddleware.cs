using System.Diagnostics;

namespace FMS.Web.Api.Logging;

/// <summary>
/// Uma linha por requisição: <c>GET /health · 200 · 3 ms</c>.
/// 4xx sai como WARN, 5xx como FAIL. O /health fica em Debug para não poluir o console com o healthcheck do Docker.
/// </summary>
public sealed class RequestLoggingMiddleware(RequestDelegate next, ILoggerFactory loggerFactory)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger("HTTP");

    public async Task InvokeAsync(HttpContext context)
    {
        long start = Stopwatch.GetTimestamp();
        try
        {
            await next(context);
        }
        finally
        {
            int status = context.Response.StatusCode;
            LogLevel level = status switch
            {
                >= 500 => LogLevel.Error,
                >= 400 => LogLevel.Warning,
                _ when context.Request.Path.StartsWithSegments("/health") => LogLevel.Debug,
                _ => LogLevel.Information
            };

            _logger.Log(level, "{Method} {Path} · {StatusCode} · {ElapsedMs} ms",
                context.Request.Method,
                context.Request.Path.Value,
                status,
                (int)Stopwatch.GetElapsedTime(start).TotalMilliseconds);
        }
    }
}
