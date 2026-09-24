using FluentValidation;
using TestJob.Api.Models;

namespace TestJob.Api.Validation;

public sealed class ProcessRequestValidator : AbstractValidator<ProcessRequest>
{
    public ProcessRequestValidator()
    {
        ClassLevelCascadeMode = CascadeMode.Stop;
        RuleLevelCascadeMode = CascadeMode.Stop;

        Required(x => x.Selector, "selector", ErrorCodes.EmptySelector);
        Required(x => x.Attribute, "attribute", ErrorCodes.EmptyAttribute);
        Required(x => x.UrlB64, "url_b64", ErrorCodes.EmptyUrl);
        Required(x => x.PageB64, "page_b64", ErrorCodes.EmptyPage);
        Required(x => x.EncryptedTextBytesB64, "encrypted_text_bytes_b64", ErrorCodes.EmptyEncryptedText);
        Required(x => x.KeyBytesB64, "key_bytes_b64", ErrorCodes.EmptyKey);
    }

    private void Required(
        System.Linq.Expressions.Expression<Func<ProcessRequest, string?>> property,
        string jsonName,
        string emptyCode) =>
        RuleFor(property)
            .NotNull()
                .WithErrorCode(ErrorCodes.MissingParameter)
                .WithMessage($"Parameter '{jsonName}' is missing.")
            .NotEmpty()
                .WithErrorCode(emptyCode)
                .WithMessage($"Parameter '{jsonName}' is empty.")
            .OverridePropertyName(jsonName);
}
