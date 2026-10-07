using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Employees.Api;

public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var isClientError = exception is BadHttpRequestException;
        var status = exception is BadHttpRequestException bad ? bad.StatusCode : StatusCodes.Status500InternalServerError;

        if (isClientError)
            logger.LogWarning("Rejected request: {Message}", exception.Message);
        else
            logger.LogError(exception, "Unhandled exception while processing {Path}", context.Request.Path);

        context.Response.StatusCode = status;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = isClientError ? "The request could not be processed." : "Something went wrong on our side.",
                Detail = isClientError ? exception.Message : "Please try again. If it keeps happening, contact the developer.",
            },
        });
    }
}
