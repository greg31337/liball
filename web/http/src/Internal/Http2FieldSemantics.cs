using System.Text;

namespace http;

internal class Http2FieldValidationResult
{
    internal bool IsValid = true;
    internal string ErrorCodeName = "";
    internal string ErrorMessage = "";
}

internal static class Http2FieldSemantics
{
    internal static Http2FieldValidationResult ValidateFields(
        HttpHeaders headers,
        bool request,
        HttpLimits limits)
    {
        Http2FieldValidationResult result = new Http2FieldValidationResult();
        if (headers.Fields.Count > limits.MaximumFieldCount)
        {
            return RejectFieldValidation(
                result,
                "http2-header-count-limit-exceeded",
                "The HTTP/2 header count exceeds the configured limit.");
        }

        long headerListSize = 0;
        int index = 0;
        while (index < headers.Fields.Count)
        {
            HttpHeaderField field = headers.Fields[index];
            if (field == null || field.Name == null || field.Value == null)
            {
                return RejectFieldValidation(
                    result,
                    "invalid-http2-field",
                    "The HTTP/2 field section contains an incomplete field.");
            }

            if (!IsValidHttp2Name(field.Name))
            {
                return RejectFieldValidation(
                    result,
                    "invalid-http2-field-name",
                    "An HTTP/2 field name must be a lowercase ASCII token.");
            }

            if (!IsValidHttp2Value(field.Value))
            {
                return RejectFieldValidation(
                    result,
                    "invalid-http2-field-value",
                    "An HTTP/2 field value contains prohibited octets or surrounding whitespace.");
            }

            if (field.Value.Length > limits.MaximumFieldValueSize)
            {
                return RejectFieldValidation(
                    result,
                    "http2-field-value-limit-exceeded",
                    "An HTTP/2 field value exceeds the configured limit.");
            }

            headerListSize = headerListSize + field.Name.Length +
                field.Value.Length + 32L;
            if (headerListSize > limits.MaximumHeaderListSize)
            {
                return RejectFieldValidation(
                    result,
                    "http2-header-list-size-limit-exceeded",
                    "The HTTP/2 header-list size exceeds the configured limit.");
            }

            if (field.Name[0] != ':' && IsConnectionSpecific(field.Name))
            {
                return RejectFieldValidation(
                    result,
                    "http2-connection-specific-field",
                    "HTTP/2 messages cannot contain connection-specific fields.");
            }

            if (field.Name == "te" &&
                (!request || !IsTrailersValue(field.Value)))
            {
                return RejectFieldValidation(
                    result,
                    "invalid-http2-te-field",
                    "TE is permitted only in requests and only with the value trailers.");
            }

            index = index + 1;
        }

        return result;
    }

    internal static Http2FieldValidationResult ValidateRequestAuthority(
        HttpHeaders headers,
        bool authoritySeen,
        string authority,
        string scheme)
    {
        Http2FieldValidationResult result = new Http2FieldValidationResult();
        int hostCount = 0;
        string host = "";
        int index = 0;
        while (index < headers.Fields.Count)
        {
            HttpHeaderField field = headers.Fields[index];
            if (field.Name == "host")
            {
                hostCount = hostCount + 1;
                if (hostCount > 1)
                {
                    return RejectFieldValidation(
                        result,
                        "multiple-http2-host-fields",
                        "An HTTP/2 request cannot contain multiple Host fields.");
                }

                host = DecodeHost(field.Value);
                if (!IsValidHost(host))
                {
                    return RejectFieldValidation(
                        result,
                        "invalid-http2-host-field",
                        "The HTTP/2 Host field value is malformed or unsafe.");
                }
            }

            index = index + 1;
        }

        if (authoritySeen && hostCount != 0 &&
            !RequestTargetSemantics.AreAuthoritiesEquivalent(
                authority, host, scheme))
        {
            return RejectFieldValidation(
                result,
                "conflicting-http2-authority",
                "The Host and :authority fields identify different entities.");
        }

        bool httpScheme = Ascii.EqualsStringsIgnoreCase(scheme, "http") ||
            Ascii.EqualsStringsIgnoreCase(scheme, "https");
        if (httpScheme && !authoritySeen && hostCount == 0)
        {
            return RejectFieldValidation(
                result,
                "missing-http2-request-authority",
                "An HTTP or HTTPS request requires :authority or Host information.");
        }

        return result;
    }

    internal static bool IsValidHttp2Name(string name)
    {
        if (name.Length == 0)
        {
            return false;
        }

        int index = 0;
        if (name[0] == ':')
        {
            index = 1;
            if (index == name.Length)
            {
                return false;
            }
        }

        while (index < name.Length)
        {
            char character = name[index];
            if (character > 0x7f ||
                character >= 'A' && character <= 'Z' ||
                !HttpGrammar.IsTokenCharacter((byte)character))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool IsValidHttp2Value(byte[] value)
    {
        if (!HeaderStore.IsValidValue(value))
        {
            return false;
        }

        return value.Length == 0 ||
            value[0] != 0x20 && value[0] != 0x09 &&
            value[value.Length - 1] != 0x20 &&
            value[value.Length - 1] != 0x09;
    }

    internal static bool IsConnectionSpecific(string name)
    {
        return name == "connection" || name == "proxy-connection" ||
            name == "keep-alive" || name == "transfer-encoding" ||
            name == "upgrade";
    }

    internal static bool IsTrailersValue(byte[] value)
    {
        if (value.Length != 8)
        {
            return false;
        }

        string text = Encoding.ASCII.GetString(value);
        return Ascii.EqualsStringsIgnoreCase(text, "trailers");
    }

    internal static string DecodeHost(byte[] value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] > 0x7f)
            {
                return "";
            }

            index = index + 1;
        }

        return Encoding.ASCII.GetString(value);
    }

    internal static bool IsValidHost(string host)
    {
        UriValidationResult validation = UriCodec.ValidateAuthority(
            host, int.MaxValue);
        return validation.Status == HttpOperationStatus.Done &&
            !validation.HasUserInfo && validation.Host.Length != 0 &&
            (!validation.HasPort || validation.HasPortValue &&
                validation.IsPortInRange);
    }

    internal static Http2FieldValidationResult RejectFieldValidation(
        Http2FieldValidationResult result,
        string errorCodeName,
        string errorMessage)
    {
        result.IsValid = false;
        result.ErrorCodeName = errorCodeName;
        result.ErrorMessage = errorMessage;
        return result;
    }
}
