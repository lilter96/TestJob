namespace TestJob.Api.Models;

public sealed class ProcessingException(
    string errorCode,
    string message,
    int statusCode = StatusCodes.Status400BadRequest,
    Exception? innerException = null)
    : Exception(message, innerException)
{
    public string ErrorCode { get; } = errorCode;

    public int StatusCode { get; } = statusCode;
}
