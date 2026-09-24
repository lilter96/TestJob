namespace TestJob.Api.Models;

// Nullable so that a missing field reaches the validator (MISSING_PARAMETER) instead of the implicit required check.
public sealed record ProcessRequest
{
    /// <example>a[href]</example>
    public string? Selector { get; init; }

    /// <example>href</example>
    public string? Attribute { get; init; }

    /// <example>aHR0cHM6Ly90ZXN0LmNvbS9wYWdlMQ==</example>
    public string? UrlB64 { get; init; }

    /// <example>hXeVCcIEyC/5ovf4eyJCozhRbTUV5jjBzOUPBM6dgZnoGyY8CNFBYxffu9fHJp5bSPKzdsFbMZ9gNZfhCG17Sg==</example>
    public string? EncryptedTextBytesB64 { get; init; }

    /// <example>SGVsbG8gVGVzdEpvYiAyNTYgYml0IHNlY3JldCBrZXk=</example>
    public string? KeyBytesB64 { get; init; }

    /// <example>PGh0bWw+PGJvZHk+PGEgaHJlZj0iaHR0cHM6Ly9leGFtcGxlLmNvbSI+bWFpbEBleGFtcGxlLmNvbTwvYT48L2JvZHk+PC9odG1sPg==</example>
    public string? PageB64 { get; init; }
}
