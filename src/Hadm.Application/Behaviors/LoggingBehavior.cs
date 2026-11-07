using Microsoft.Extensions.Logging;

namespace Hadm.Application.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var name = typeof(TRequest).Name;
        logger.LogInformation("Handling {RequestName}", name);
        try
        {
            var response = await next();
            logger.LogInformation("Handled {RequestName}", name);
            return response;
        }
        catch (Exception ex) when (ex is not AppValidationException)
        {
            logger.LogWarning(ex, "Request {RequestName} failed", name);
            throw;
        }
    }
}
