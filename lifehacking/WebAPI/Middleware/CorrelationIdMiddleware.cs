using System.Text.RegularExpressions;
using Microsoft.Extensions.Primitives;

namespace WebAPI.Middleware;

/// <summary>
/// Adds and propagates a correlation identifier for each HTTP request so that
/// logs and error responses can be tied together without exposing internals.
/// </summary>
public sealed partial class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ResolveCorrelationId(context.Request.Headers);

        context.TraceIdentifier = correlationId;
        context.Response.Headers[CorrelationIdDefaults.CorrelationIdHeaderName] = correlationId;

        using (_logger.BeginScope(new Dictionary<string, object?>
               {
                   { CorrelationIdDefaults.CorrelationIdLogScopeKey, correlationId }
               }))
        {
            await _next(context);
        }
    }

    private static string ResolveCorrelationId(IHeaderDictionary headers)
    {
        if (headers.TryGetValue(CorrelationIdDefaults.CorrelationIdHeaderName, out StringValues values) &&
            !StringValues.IsNullOrEmpty(values))
        {
            var candidate = values.ToString();

            if (IsValidCorrelationId(candidate))
            {
                return candidate;
            }

            // Client-provided value failed validation; generate a new one to prevent
            // log injection or oversized values from propagating through the pipeline.
        }

        return Guid.NewGuid().ToString("D");
    }

    /// <summary>
    /// Validates that a client-supplied correlation ID is safe for use in logs,
    /// response headers, and error payloads. Accepts only printable ASCII
    /// characters (letters, digits, hyphens, underscores, dots, colons) up to
    /// a maximum length to prevent log injection and oversized values.
    /// </summary>
    private static bool IsValidCorrelationId(string value)
    {
        return value.Length <= CorrelationIdDefaults.MaxCorrelationIdLength &&
               SafeCorrelationIdRegex().IsMatch(value);
    }

    [GeneratedRegex(@"^[a-zA-Z0-9\-_.:]+$")]
    private static partial Regex SafeCorrelationIdRegex();
}

/// <summary>
/// Centralizes correlation id–related constants to avoid magic strings and to
/// make it easy to audit how correlation is handled.
/// </summary>
public static class CorrelationIdDefaults
{
    public const string CorrelationIdHeaderName = "X-Correlation-ID";
    public const string CorrelationIdLogScopeKey = "CorrelationId";

    /// <summary>
    /// Maximum allowed length for a client-supplied correlation ID. Values
    /// exceeding this length are discarded to protect logging infrastructure.
    /// </summary>
    public const int MaxCorrelationIdLength = 128;
}
