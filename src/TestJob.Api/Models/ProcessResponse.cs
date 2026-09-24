namespace TestJob.Api.Models;

public sealed record ProcessResponse
{
    public int IsError { get; init; }

    public string? ErrorCode { get; init; }

    public string? ErrorMessage { get; init; }

    public int ElementsCount { get; init; }

    public int EmailsCount { get; init; }

    public string? Url { get; init; }

    public string? DecryptedPlainText { get; init; }

    public IReadOnlyList<string> ElementsAttrList { get; init; } = [];

    public IReadOnlyList<string> EmailsList { get; init; } = [];

    public static ProcessResponse Success(
        string url,
        string decryptedPlainText,
        IReadOnlyList<string> attributes,
        IReadOnlyList<string> emails) => new()
    {
        IsError = 0,
        ElementsCount = attributes.Count,
        EmailsCount = emails.Count,
        Url = url,
        DecryptedPlainText = decryptedPlainText,
        ElementsAttrList = attributes,
        EmailsList = emails,
    };

    public static ProcessResponse Error(string code, string message) => new()
    {
        IsError = 1,
        ErrorCode = code,
        ErrorMessage = message,
    };
}
