using MediatR;
using Microsoft.Extensions.Logging;

namespace InvoiceFlow.Application.Common.Behaviours;

/// <summary>
/// MediatR pipeline behaviour that logs the start and completion of every request.
/// Logs request name, duration, and any unhandled exceptions for distributed tracing.
/// </summary>
public sealed class LoggingBehaviour<TRequest, TResponse>(
    ILogger<LoggingBehaviour<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        logger.LogInformation("Handling {RequestName}", requestName);

        var startTime = DateTime.UtcNow;
        try
        {
            var response = await next(cancellationToken);
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            logger.LogInformation(
                "Handled {RequestName} in {ElapsedMs}ms", requestName, elapsed);
            return response;
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            logger.LogError(ex,
                "Error handling {RequestName} after {ElapsedMs}ms", requestName, elapsed);
            throw;
        }
    }
}
