namespace cbor;

internal static class CborBuiltInTags
{
    internal static readonly ICborTagHandler DateTimeHandler = new CborDateTimeTags();
    internal static readonly ICborTagHandler BigNumberHandler = new CborBigNumberTags();
    internal static readonly ICborTagHandler EmbeddedHandler = new CborEmbeddedTag();
    internal static readonly ICborTagHandler HintHandler = new CborHintTags();

    internal static CborTagRegistration? FindRegistration(ulong tagNumber)
    {
        ICborTagHandler? handler = null;
        string? name = null;
        switch (tagNumber)
        {
            case CborTagNumbers.DateTimeString:
                handler = DateTimeHandler;
                name = "date-time string";
                break;
            case CborTagNumbers.EpochDateTime:
                handler = DateTimeHandler;
                name = "epoch date-time";
                break;
            case CborTagNumbers.PositiveBignum:
                handler = BigNumberHandler;
                name = "positive bignum";
                break;
            case CborTagNumbers.NegativeBignum:
                handler = BigNumberHandler;
                name = "negative bignum";
                break;
            case CborTagNumbers.DecimalFraction:
                handler = BigNumberHandler;
                name = "decimal fraction";
                break;
            case CborTagNumbers.Bigfloat:
                handler = BigNumberHandler;
                name = "bigfloat";
                break;
            case CborTagNumbers.EncodedCbor:
                handler = EmbeddedHandler;
                name = "encoded CBOR";
                break;
            case CborTagNumbers.ExpectedBase64Url:
                handler = HintHandler;
                name = "expected base64url encoding";
                break;
            case CborTagNumbers.ExpectedBase64:
                handler = HintHandler;
                name = "expected base64 encoding";
                break;
            case CborTagNumbers.ExpectedBase16:
                handler = HintHandler;
                name = "expected base16 encoding";
                break;
            case CborTagNumbers.Uri:
                handler = HintHandler;
                name = "URI";
                break;
            case CborTagNumbers.Base64Url:
                handler = HintHandler;
                name = "base64url text";
                break;
            case CborTagNumbers.Base64:
                handler = HintHandler;
                name = "base64 text";
                break;
            case CborTagNumbers.RegularExpression:
                handler = HintHandler;
                name = "regular expression";
                break;
            case CborTagNumbers.MimeMessage:
                handler = HintHandler;
                name = "MIME message";
                break;
            case CborTagNumbers.SelfDescribedCbor:
                handler = EmbeddedHandler;
                name = "self-described CBOR";
                break;
            default:
                return null;
        }
        CborTagRegistration registration = new CborTagRegistration();
        registration.TagNumber = tagNumber;
        registration.Name = name;
        registration.Handler = handler;
        registration.ValidateContent = true;
        return registration;
    }

    internal static CborTagResult Valid(object? nativeValue)
    {
        CborTagResult result = new CborTagResult();
        result.Handled = true;
        result.IsValid = true;
        result.NativeValue = nativeValue;
        return result;
    }

    internal static CborTagResult Encoded(CborValue value)
    {
        CborTagResult result = new CborTagResult();
        result.Handled = true;
        result.IsValid = true;
        result.Value = value;
        return result;
    }

    internal static CborTagResult Invalid(string code, string message)
    {
        CborTagResult result = new CborTagResult();
        result.Handled = true;
        result.IsValid = false;
        result.ErrorCode = code;
        result.ErrorMessage = message;
        return result;
    }

    internal static CborTagResult Unhandled()
    {
        return new CborTagResult();
    }

    internal static bool IsRuntimeType(object? value, string fullName)
    {
        return value != null && value.GetType().FullName == fullName;
    }
}
