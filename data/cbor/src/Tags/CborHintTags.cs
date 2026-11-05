namespace cbor;

internal class CborHintTags : ICborTagHandler
{
    internal CborHintTags()
    {
    }

    public CborTagResult ValidateTag(ulong tagNumber, CborValue content)
    {
        if (tagNumber == CborTagNumbers.ExpectedBase64Url
            || tagNumber == CborTagNumbers.ExpectedBase64
            || tagNumber == CborTagNumbers.ExpectedBase16)
        {
            return CborBuiltInTags.Valid(null);
        }
        if (tagNumber == CborTagNumbers.Uri
            || tagNumber == CborTagNumbers.Base64Url
            || tagNumber == CborTagNumbers.Base64
            || tagNumber == CborTagNumbers.RegularExpression
            || tagNumber == CborTagNumbers.MimeMessage)
        {
            if (content.Kind != CborValueKind.TextString
                || content.TextStringValue == null)
            {
                return CborBuiltInTags.Invalid(
                    "CBOR_TEXT_HINT_TYPE",
                    "A URI or text-hint tag requires a text string."
                );
            }
            if (tagNumber == CborTagNumbers.Base64Url
                && (!IsBase64Url(content.TextStringValue)
                    || EncodeBase64Url(DecodeBase64Url(content.TextStringValue))
                        != content.TextStringValue))
            {
                return CborBuiltInTags.Invalid(
                    "CBOR_BASE64URL_TEXT_INVALID",
                    "The tagged text is not valid unpadded base64url."
                );
            }
            if (tagNumber == CborTagNumbers.Base64
                && (!IsBase64(content.TextStringValue)
                    || System.Convert.ToBase64String(
                        System.Convert.FromBase64String(content.TextStringValue))
                        != content.TextStringValue))
            {
                return CborBuiltInTags.Invalid(
                    "CBOR_BASE64_TEXT_INVALID",
                    "The tagged text is not valid base64."
                );
            }
            if (tagNumber == CborTagNumbers.Uri
                && !IsUriReference(content.TextStringValue))
            {
                return CborBuiltInTags.Invalid(
                    "CBOR_URI_TEXT_INVALID",
                    "The tagged text is not a valid URI reference."
                );
            }
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
        if (tagNumber == CborTagNumbers.ExpectedBase64Url)
        {
            if (content.Kind != CborValueKind.ByteString)
            {
                return CborBuiltInTags.Valid(content);
            }
            return CborBuiltInTags.Valid(
                EncodeBase64Url(content.ByteStringValue ?? [])
            );
        }
        if (tagNumber == CborTagNumbers.ExpectedBase64)
        {
            if (content.Kind != CborValueKind.ByteString)
            {
                return CborBuiltInTags.Valid(content);
            }
            return CborBuiltInTags.Valid(
                System.Convert.ToBase64String(content.ByteStringValue ?? [])
            );
        }
        if (tagNumber == CborTagNumbers.ExpectedBase16)
        {
            if (content.Kind != CborValueKind.ByteString)
            {
                return CborBuiltInTags.Valid(content);
            }
            return CborBuiltInTags.Valid(
                System.Convert.ToHexString(content.ByteStringValue ?? [])
            );
        }
        string text = content.TextStringValue ?? "";
        if (tagNumber == CborTagNumbers.Uri)
        {
            return CborBuiltInTags.Valid(
                new System.Uri(text, System.UriKind.RelativeOrAbsolute)
            );
        }
        if (tagNumber == CborTagNumbers.Base64Url)
        {
            return CborBuiltInTags.Valid(DecodeBase64Url(text));
        }
        if (tagNumber == CborTagNumbers.Base64)
        {
            return CborBuiltInTags.Valid(System.Convert.FromBase64String(text));
        }
        if (tagNumber == CborTagNumbers.RegularExpression)
        {
            return CborBuiltInTags.Valid(
                new System.Text.RegularExpressions.Regex(text)
            );
        }
        return CborBuiltInTags.Valid(text);
    }

    public CborTagResult EncodeTag(ulong tagNumber, object nativeValue)
    {
        if (tagNumber == CborTagNumbers.ExpectedBase64Url
            || tagNumber == CborTagNumbers.ExpectedBase64
            || tagNumber == CborTagNumbers.ExpectedBase16)
        {
            byte[]? bytes = nativeValue as byte[];
            if (bytes == null)
            {
                CborValue? value = nativeValue as CborValue;
                if (value != null)
                {
                    return CborBuiltInTags.Encoded(value);
                }
                return CborBuiltInTags.Invalid(
                    "CBOR_ENCODING_HINT_NATIVE_TYPE",
                    "Expected-encoding tags require a byte array or CborValue native value."
                );
            }
            return CborBuiltInTags.Encoded(CreateBytes(bytes));
        }
        if (tagNumber == CborTagNumbers.Uri)
        {
            System.Uri? uri = nativeValue as System.Uri;
            if (uri == null)
            {
                return CborBuiltInTags.Invalid(
                    "CBOR_URI_NATIVE_TYPE",
                    "The URI tag requires a Uri native value."
                );
            }
            return CborBuiltInTags.Encoded(CreateText(uri.OriginalString));
        }
        if (tagNumber == CborTagNumbers.Base64Url
            || tagNumber == CborTagNumbers.Base64)
        {
            byte[]? bytes = nativeValue as byte[];
            if (bytes == null)
            {
                return CborBuiltInTags.Invalid(
                    "CBOR_BASE_TEXT_NATIVE_TYPE",
                    "Base-encoding text tags require a byte-array native value."
                );
            }
            string text = tagNumber == CborTagNumbers.Base64Url
                ? EncodeBase64Url(bytes)
                : System.Convert.ToBase64String(bytes);
            return CborBuiltInTags.Encoded(CreateText(text));
        }
        if (tagNumber == CborTagNumbers.RegularExpression)
        {
            System.Text.RegularExpressions.Regex? expression =
                nativeValue as System.Text.RegularExpressions.Regex;
            if (expression == null)
            {
                return CborBuiltInTags.Invalid(
                    "CBOR_REGULAR_EXPRESSION_NATIVE_TYPE",
                    "The regular-expression tag requires a Regex native value."
                );
            }
            return CborBuiltInTags.Encoded(CreateText(expression.ToString()));
        }
        if (tagNumber == CborTagNumbers.MimeMessage)
        {
            string? text = nativeValue as string;
            if (text == null)
            {
                return CborBuiltInTags.Invalid(
                    "CBOR_MIME_NATIVE_TYPE",
                    "The MIME-message tag requires a string native value."
                );
            }
            return CborBuiltInTags.Encoded(CreateText(text));
        }
        return CborBuiltInTags.Unhandled();
    }

    internal CborValue CreateBytes(byte[] value)
    {
        CborValue bytes = new CborValue();
        bytes.Kind = CborValueKind.ByteString;
        bytes.ByteStringValue = value;
        return bytes;
    }

    internal CborValue CreateText(string value)
    {
        CborValue text = new CborValue();
        text.Kind = CborValueKind.TextString;
        text.TextStringValue = value;
        return text;
    }

    internal string EncodeBase64Url(byte[] value)
    {
        return System.Convert.ToBase64String(value)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    internal byte[] DecodeBase64Url(string value)
    {
        string normalized = value.Replace('-', '+').Replace('_', '/');
        int remainder = normalized.Length % 4;
        if (remainder == 2)
        {
            normalized = normalized + "==";
        }
        else if (remainder == 3)
        {
            normalized = normalized + "=";
        }
        return System.Convert.FromBase64String(normalized);
    }

    internal bool IsBase64Url(string value)
    {
        if (value.Length % 4 == 1)
        {
            return false;
        }
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            bool allowed = character >= 'A' && character <= 'Z'
                || character >= 'a' && character <= 'z'
                || character >= '0' && character <= '9'
                || character == '-'
                || character == '_';
            if (!allowed)
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal bool IsBase64(string value)
    {
        if (value.Length % 4 != 0)
        {
            return false;
        }
        int padding = 0;
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == '=')
            {
                padding = padding + 1;
                if (padding > 2 || index < value.Length - 2)
                {
                    return false;
                }
            }
            else
            {
                if (padding > 0)
                {
                    return false;
                }
                bool allowed = character >= 'A' && character <= 'Z'
                    || character >= 'a' && character <= 'z'
                    || character >= '0' && character <= '9'
                    || character == '+'
                    || character == '/';
                if (!allowed)
                {
                    return false;
                }
            }
            index = index + 1;
        }
        return true;
    }

    internal bool IsUriReference(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character <= 0x20 || character >= 0x7f)
            {
                return false;
            }
            if (character == '%')
            {
                if (index > value.Length - 3
                    || !IsHex(value[index + 1])
                    || !IsHex(value[index + 2]))
                {
                    return false;
                }
                index = index + 2;
            }
            index = index + 1;
        }
        return true;
    }

    internal bool IsHex(char character)
    {
        return character >= '0' && character <= '9'
            || character >= 'A' && character <= 'F'
            || character >= 'a' && character <= 'f';
    }
}
