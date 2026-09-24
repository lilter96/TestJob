using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Options;
using Npgsql;
using TestJob.Api.Models;

namespace TestJob.Api.Infrastructure;

public sealed class GlobalExceptionHandler(
    IOptions<Microsoft.AspNetCore.Mvc.JsonOptions> mvcJsonOptions,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    private const int ClientClosedRequest = 499;

    private readonly JsonSerializerOptions _jsonOptions = mvcJsonOptions.Value.JsonSerializerOptions;

    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && context.RequestAborted.IsCancellationRequested)
        {
            logger.LogInformation("Request {Path} was aborted by the client", context.Request.Path);
            context.Response.StatusCode = ClientClosedRequest;
            return true;
        }

        var (status, response) = exception switch
        {
            ProcessingException e => (e.StatusCode, ProcessResponse.Error(e.ErrorCode, e.Message)),
            BadHttpRequestException { StatusCode: StatusCodes.Status413PayloadTooLarge } e =>
                (e.StatusCode, ProcessResponse.Error(ErrorCodes.RequestTooLarge, e.Message)),
            BadHttpRequestException e => (e.StatusCode, ProcessResponse.Error(ErrorCodes.InvalidRequestBody, e.Message)),
            NpgsqlException e => (StatusCodes.Status500InternalServerError, ProcessResponse.Error(ErrorCodes.DatabaseError, e.Message)),
            _ => (StatusCodes.Status500InternalServerError, ProcessResponse.Error(ErrorCodes.InternalError, exception.Message)),
        };

        if (status >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Request {Path} failed with {ErrorCode}", context.Request.Path, response.ErrorCode);
        }
        else
        {
            logger.LogInformation("Request {Path} rejected with {ErrorCode}: {Message}", context.Request.Path, response.ErrorCode, response.ErrorMessage);
        }

        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(response, _jsonOptions, contentType: "application/json; charset=utf-8", cancellationToken);
        return true;
    }
}
