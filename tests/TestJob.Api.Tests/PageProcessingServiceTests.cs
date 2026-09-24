using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using TestJob.Api.Models;
using TestJob.Api.Services;
using TestJob.Api.Validation;

namespace TestJob.Api.Tests;

public sealed class PageProcessingServiceTests
{
    private const string ExpectedPlainText = "AES Error: Object reference not set to an instance of an object.";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    private static ProcessRequest LoadPayload(int n) =>
        JsonSerializer.Deserialize<ProcessRequest>(
            File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestData", $"json_payload_{n}.txt"), Encoding.UTF8),
            JsonOptions)!;

    private static PageProcessingService CreateService() => new(
        new ProcessRequestValidator(),
        NpgsqlDataSource.Create("Host=unreachable.invalid;Database=none;Username=none;Password=none"),
        NullLogger<PageProcessingService>.Instance);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public void Decrypt_SamplePayload_ReturnsExpectedPlainText()
    {
        var request = LoadPayload(1);

        var plain = PageProcessingService.Decrypt(
            Convert.FromBase64String(request.EncryptedTextBytesB64!),
            Convert.FromBase64String(request.KeyBytesB64!));

        Assert.Equal(ExpectedPlainText, plain);
    }

    [Fact]
    public void Decrypt_TrimsZeroPadding()
    {
        using var aes = System.Security.Cryptography.Aes.Create();
        aes.GenerateKey();
        var padded = new byte[32];
        Encoding.UTF8.GetBytes("hello").CopyTo(padded, 0);
        var cipher = aes.EncryptEcb(padded, System.Security.Cryptography.PaddingMode.None);

        Assert.Equal("hello", PageProcessingService.Decrypt(cipher, aes.Key));
    }

    [Theory]
    [InlineData(16, 16, ErrorCodes.InvalidKeyLength)]
    [InlineData(32, 15, ErrorCodes.InvalidCiphertextLength)]
    [InlineData(32, 0, ErrorCodes.InvalidCiphertextLength)]
    public void Decrypt_InvalidSizes_Throws(int keySize, int cipherSize, string expectedCode)
    {
        var ex = Assert.Throws<ProcessingException>(() =>
            PageProcessingService.Decrypt(new byte[cipherSize], new byte[keySize]));

        Assert.Equal(expectedCode, ex.ErrorCode);
    }

    [Fact]
    public void FindEmails_SamplePage_ReturnsAllOccurrencesInOrder()
    {
        var page = Encoding.UTF8.GetString(Convert.FromBase64String(LoadPayload(1).PageB64!));

        var emails = PageProcessingService.FindEmails(page);

        Assert.Equal(
            ["webmaster@rbc.ru", "privet@test.com", "hh_test_task@gmail.com", "letters@rbc.ru", "letters@rbc.ru"],
            emails);
    }

    [Fact]
    public async Task SelectElementsAsync_ReturnsAttributesAndOuterHtml_InDocumentOrder()
    {
        const string html = """<div><a href="/1">one</a><a>no href</a><a href="">empty</a></div>""";

        var elements = await PageProcessingService.SelectElementsAsync(html, "a", "href", Ct);

        Assert.Collection(elements,
            e => { Assert.Equal("/1", e.AttributeValue); Assert.Equal("""<a href="/1">one</a>""", e.Html); },
            e => Assert.Null(e.AttributeValue),
            e => Assert.Equal(string.Empty, e.AttributeValue));
    }

    [Fact]
    public async Task SelectElementsAsync_InvalidSelector_Throws()
    {
        var ex = await Assert.ThrowsAsync<ProcessingException>(() =>
            PageProcessingService.SelectElementsAsync("<p></p>", "a[[", "href", Ct));

        Assert.Equal(ErrorCodes.InvalidSelector, ex.ErrorCode);
    }

    [Fact]
    public void DecodeUtf8_InvalidUtf8_Throws()
    {
        var notUtf8 = Convert.ToBase64String([0xC3, 0x28]);

        var ex = Assert.Throws<ProcessingException>(() =>
            PageProcessingService.DecodeUtf8(notUtf8, ErrorCodes.PageBase64DecodeError, "page_b64"));

        Assert.Equal(ErrorCodes.PageBase64DecodeError, ex.ErrorCode);
    }

    public static TheoryData<Func<ProcessRequest, ProcessRequest>, string> InvalidRequests => new()
    {
        { r => r with { Selector = null }, ErrorCodes.MissingParameter },
        { r => r with { KeyBytesB64 = null }, ErrorCodes.MissingParameter },
        { r => r with { Selector = "" }, ErrorCodes.EmptySelector },
        { r => r with { Attribute = "   " }, ErrorCodes.EmptyAttribute },
        { r => r with { UrlB64 = "not base64!" }, ErrorCodes.UrlBase64DecodeError },
        { r => r with { PageB64 = "%%%" }, ErrorCodes.PageBase64DecodeError },
        { r => r with { EncryptedTextBytesB64 = "***" }, ErrorCodes.EncryptedTextBase64DecodeError },
        { r => r with { KeyBytesB64 = "@@" }, ErrorCodes.KeyBase64DecodeError },
        { r => r with { KeyBytesB64 = Convert.ToBase64String(new byte[16]) }, ErrorCodes.InvalidKeyLength },
        { r => r with { Selector = "a[[" }, ErrorCodes.InvalidSelector },
    };

    [Theory]
    [MemberData(nameof(InvalidRequests))]
    public async Task ProcessAsync_InvalidInput_FailsWithSpecificCode(Func<ProcessRequest, ProcessRequest> mutate, string expectedCode)
    {
        var request = mutate(LoadPayload(1));

        var ex = await Assert.ThrowsAsync<ProcessingException>(() => CreateService().ProcessAsync(request, Ct));

        Assert.Equal(expectedCode, ex.ErrorCode);
        Assert.Equal(400, ex.StatusCode);
    }
}
