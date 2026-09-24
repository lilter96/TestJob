using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Dapper;
using FluentValidation;
using Npgsql;
using TestJob.Api.Models;

namespace TestJob.Api.Services;

public sealed partial class PageProcessingService(
    IValidator<ProcessRequest> validator,
    NpgsqlDataSource dataSource,
    ILogger<PageProcessingService> logger)
{
    private const int Aes256KeySize = 32;

    // One round trip; WITH ORDINALITY keeps ids in document order.
    private const string InsertElementsSql =
        """
        INSERT INTO elements (attribute_value, html)
        SELECT v, h
        FROM unnest(@Values::text[], @Html::text[]) WITH ORDINALITY AS t(v, h, ord)
        ORDER BY ord;
        """;

    // Throws on invalid bytes instead of silently substituting U+FFFD.
    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    public async Task<ProcessResponse> ProcessAsync(ProcessRequest request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            var failure = validation.Errors[0];
            throw new ProcessingException(failure.ErrorCode, failure.ErrorMessage);
        }

        var url = DecodeUtf8(request.UrlB64!, ErrorCodes.UrlBase64DecodeError, "url_b64");
        var page = DecodeUtf8(request.PageB64!, ErrorCodes.PageBase64DecodeError, "page_b64");
        var cipherText = DecodeBytes(request.EncryptedTextBytesB64!, ErrorCodes.EncryptedTextBase64DecodeError, "encrypted_text_bytes_b64");
        var key = DecodeBytes(request.KeyBytesB64!, ErrorCodes.KeyBase64DecodeError, "key_bytes_b64");

        var elements = await SelectElementsAsync(page, request.Selector!, request.Attribute!, cancellationToken);
        var emails = FindEmails(page);
        var plainText = Decrypt(cipherText, key);

        // Persist last: a request that fails at any earlier step writes nothing.
        await SaveElementsAsync(elements, cancellationToken);

        logger.LogInformation(
            "Processed {Url}: {ElementsCount} element(s) for '{Selector}', {EmailsCount} email(s)",
            url, elements.Count, request.Selector, emails.Count);

        // Missing attribute: "" in the response (list stays aligned with elements_count), NULL in the DB.
        var attributes = elements.Select(e => e.AttributeValue ?? string.Empty).ToArray();
        return ProcessResponse.Success(url, plainText, attributes, emails);
    }

    internal static byte[] DecodeBytes(string base64, string errorCode, string parameter)
    {
        try
        {
            return Convert.FromBase64String(base64);
        }
        catch (FormatException ex)
        {
            throw new ProcessingException(errorCode, $"Parameter '{parameter}' is not a valid Base64 string.", innerException: ex);
        }
    }

    internal static string DecodeUtf8(string base64, string errorCode, string parameter)
    {
        var bytes = DecodeBytes(base64, errorCode, parameter);
        try
        {
            return StrictUtf8.GetString(bytes);
        }
        catch (DecoderFallbackException ex)
        {
            throw new ProcessingException(errorCode, $"Parameter '{parameter}' does not decode to a valid UTF-8 string.", innerException: ex);
        }
    }

    internal static async Task<IReadOnlyList<ElementInfo>> SelectElementsAsync(
        string html, string selector, string attribute, CancellationToken cancellationToken)
    {
        var parser = new HtmlParser();
        using var document = await parser.ParseDocumentAsync(html, cancellationToken);

        IHtmlCollection<IElement> matches;
        try
        {
            matches = document.QuerySelectorAll(selector);
        }
        catch (DomException ex)
        {
            throw new ProcessingException(ErrorCodes.InvalidSelector, $"CSS selector '{selector}' is invalid: {ex.Message}", innerException: ex);
        }

        var result = new ElementInfo[matches.Length];
        for (var i = 0; i < result.Length; i++)
        {
            var element = matches[i];
            result[i] = new ElementInfo(element.GetAttribute(attribute), element.OuterHtml);
        }

        return result;
    }

    internal static IReadOnlyList<string> FindEmails(string html)
    {
        try
        {
            var matches = EmailRegex().Matches(html);
            var result = new string[matches.Count];
            for (var i = 0; i < result.Length; i++)
            {
                result[i] = matches[i].Value;
            }

            return result;
        }
        catch (RegexMatchTimeoutException ex)
        {
            throw new ProcessingException(ErrorCodes.EmailSearchTimeout, "Email search exceeded the time limit.",
                StatusCodes.Status500InternalServerError, ex);
        }
    }

    internal static string Decrypt(byte[] cipherText, byte[] key)
    {
        if (key.Length != Aes256KeySize)
        {
            throw new ProcessingException(ErrorCodes.InvalidKeyLength,
                $"AES-256 key must be {Aes256KeySize} bytes, got {key.Length}.");
        }

        if (cipherText.Length == 0 || cipherText.Length % 16 != 0)
        {
            throw new ProcessingException(ErrorCodes.InvalidCiphertextLength,
                $"Ciphertext length must be a positive multiple of the AES block size (16 bytes), got {cipherText.Length}.");
        }

        using var aes = Aes.Create();
        aes.Key = key;
        var plain = aes.DecryptEcb(cipherText, PaddingMode.None);

        // PaddingMode.None: the last block is padded manually, usually with zero bytes.
        return Encoding.UTF8.GetString(plain).TrimEnd('\0');
    }

    private async Task SaveElementsAsync(IReadOnlyList<ElementInfo> elements, CancellationToken cancellationToken)
    {
        if (elements.Count == 0)
        {
            return;
        }

        var values = new string?[elements.Count];
        var html = new string[elements.Count];
        for (var i = 0; i < elements.Count; i++)
        {
            (values[i], html[i]) = elements[i];
        }

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            InsertElementsSql,
            new { Values = values, Html = html },
            cancellationToken: cancellationToken));
    }

    // Compiled at build time by the source generator; the timeout guards against ReDoS.
    [GeneratedRegex(
        @"[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        matchTimeoutMilliseconds: 2000)]
    private static partial Regex EmailRegex();

    internal sealed record ElementInfo(string? AttributeValue, string Html);
}
