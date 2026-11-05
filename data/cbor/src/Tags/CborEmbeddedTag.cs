namespace cbor;

internal class CborEmbeddedTag : ICborTagHandler
{
    internal CborEmbeddedTag()
    {
    }

    public CborTagResult ValidateTag(ulong tagNumber, CborValue content)
    {
        if (tagNumber == CborTagNumbers.EncodedCbor)
        {
            if (content.Kind != CborValueKind.ByteString
                || content.ByteStringValue == null)
            {
                return CborBuiltInTags.Invalid(
                    "CBOR_ENCODED_CBOR_TYPE",
                    "The encoded-CBOR tag requires a byte string."
                );
            }
            return CborBuiltInTags.Valid(null);
        }
        if (tagNumber == CborTagNumbers.SelfDescribedCbor)
        {
            return CborBuiltInTags.Valid(null);
        }
        return CborBuiltInTags.Unhandled();
    }

    public CborTagResult DecodeTag(ulong tagNumber, CborValue content)
    {
        CborTagResult validation = ValidateTag(tagNumber, content);
        if (!validation.Handled || !validation.IsValid)
        {
            return validation;
        }
        if (tagNumber == CborTagNumbers.SelfDescribedCbor)
        {
            return CborBuiltInTags.Valid(content);
        }
        CborDecodeOptions options = new CborDecodeOptions();
        options.TagPolicy = CborTagPolicy.ValidateKnown;
        options.SharedValuePolicy = CborSharedValuePolicy.Preserve;
        CborReadResult decoded = new CborDecoder().Decode(
            content.ByteStringValue ?? [],
            options
        );
        if (decoded.Status != CborReadStatus.Done || decoded.Value == null)
        {
            return CborBuiltInTags.Invalid(
                decoded.ErrorCode ?? "CBOR_EMBEDDED_CBOR_INVALID",
                decoded.ErrorMessage ?? "The embedded byte string is not one CBOR data item."
            );
        }
        return CborBuiltInTags.Valid(decoded.Value);
    }

    public CborTagResult EncodeTag(ulong tagNumber, object nativeValue)
    {
        if (tagNumber != CborTagNumbers.EncodedCbor
            && tagNumber != CborTagNumbers.SelfDescribedCbor)
        {
            return CborBuiltInTags.Unhandled();
        }
        CborValue? value = nativeValue as CborValue;
        if (value == null)
        {
            return CborBuiltInTags.Invalid(
                "CBOR_EMBEDDED_CBOR_NATIVE_TYPE",
                "Embedded and self-described CBOR tags require a CborValue native value."
            );
        }
        if (tagNumber == CborTagNumbers.SelfDescribedCbor)
        {
            return CborBuiltInTags.Encoded(value);
        }
        CborValidationResult validation = new CborValidator().ValidateValue(
            value,
            CborValidationLevel.Basic
        );
        if (!validation.IsValid)
        {
            string code = validation.Issues.Length == 0
                ? "CBOR_EMBEDDED_CBOR_VALUE_INVALID"
                : validation.Issues[0].Code
                    ?? "CBOR_EMBEDDED_CBOR_VALUE_INVALID";
            string message = validation.Issues.Length == 0
                ? "The embedded CBOR value is invalid."
                : validation.Issues[0].Message
                    ?? "The embedded CBOR value is invalid.";
            return CborBuiltInTags.Invalid(code, message);
        }
        CborEncodeOptions options = new CborEncodeOptions();
        options.SharedValuePolicy = CborSharedValuePolicy.Preserve;
        CborValue bytes = new CborValue();
        bytes.Kind = CborValueKind.ByteString;
        bytes.ByteStringValue = new CborEncoder().Encode(value, options);
        return CborBuiltInTags.Encoded(bytes);
    }
}
