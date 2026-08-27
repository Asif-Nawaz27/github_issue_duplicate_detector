using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace IssueSense.Api;

/// <summary>
/// Catches anything an action didn't handle itself, logs it with full detail, and returns a
/// safe, generic ProblemDetails response — callers never see internal exception messages or
/// stack traces.
/// </summary>
public sealed partial class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        LogUnhandledException(exception, httpContext.Request.Method, httpContext.Request.Path.ToString());

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        await httpContext.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred.",
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1"
            },
            cancellationToken);

        return true;
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception processing {Method} {Path}")]
    private partial void LogUnhandledException(Exception exception, string method, string path);
}
