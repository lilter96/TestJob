namespace TestJob.Api.Models;

public static class ErrorCodes
{
    public const string InvalidRequestBody = "INVALID_REQUEST_BODY";
    public const string RequestTooLarge = "REQUEST_TOO_LARGE";
    public const string MissingParameter = "MISSING_PARAMETER";
    public const string EmptySelector = "EMPTY_SELECTOR";
    public const string EmptyAttribute = "EMPTY_ATTRIBUTE";
    public const string EmptyUrl = "EMPTY_URL";
    public const string EmptyPage = "EMPTY_PAGE";
    public const string EmptyEncryptedText = "EMPTY_ENCRYPTED_TEXT";
    public const string EmptyKey = "EMPTY_KEY";
    public const string UrlBase64DecodeError = "URL_BASE64_DECODE_ERROR";
    public const string PageBase64DecodeError = "PAGE_BASE64_DECODE_ERROR";
    public const string EncryptedTextBase64DecodeError = "ENCRYPTED_TEXT_BASE64_DECODE_ERROR";
    public const string KeyBase64DecodeError = "KEY_BASE64_DECODE_ERROR";
    public const string InvalidSelector = "INVALID_SELECTOR";
    public const string InvalidKeyLength = "INVALID_KEY_LENGTH";
    public const string InvalidCiphertextLength = "INVALID_CIPHERTEXT_LENGTH";
    public const string EmailSearchTimeout = "EMAIL_SEARCH_TIMEOUT";
    public const string DatabaseError = "DATABASE_ERROR";
    public const string InternalError = "INTERNAL_ERROR";
}
