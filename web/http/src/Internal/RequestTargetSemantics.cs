using System.Text;

namespace http;

internal static class RequestTargetSemantics
{
    internal static HttpRequestTargetForm Determine(HttpRequest request)
    {
        if (request == null || request.Target == null || request.Headers == null ||
            request.Headers.Fields == null)
        {
            throw CreateProtocolError("invalid-request", "The HTTP request is incomplete.");
        }

        MethodSemantics.Describe(request.Method);
        string target = request.Target;
        HttpRequestTargetForm form;
        UriValidationResult? absolute = null;
        if (target == "*")
        {
            if (request.Method != HttpMethods.Options)
            {
                throw CreateProtocolError(
                    "invalid-request-target-form",
                    "The asterisk-form target is only valid for OPTIONS.");
            }

            form = HttpRequestTargetForm.Asterisk;
        }
        else if (request.Method == HttpMethods.Connect)
        {
            ValidateAuthorityValue(target, true);
            form = HttpRequestTargetForm.Authority;
        }
        else if (target.Length > 0 && target[0] == '/')
        {
            if (!ValidateOriginTarget(target))
            {
                throw CreateProtocolError(
                    "invalid-request-target",
                    "The origin-form request target is malformed.");
            }

            form = HttpRequestTargetForm.Origin;
        }
        else
        {
            absolute = UriCodec.ValidateUriReference(target, int.MaxValue);
            if (absolute.Status != HttpOperationStatus.Done || !absolute.IsAbsolute ||
                absolute.HasFragment)
            {
                throw CreateProtocolError(
                    "invalid-request-target",
                    "The absolute-form request target is malformed.");
            }

            ValidateAbsoluteAuthority(absolute);
            form = HttpRequestTargetForm.Absolute;
        }

        string? host = ReadHost(request);
        ValidateControlAuthority(request, host, form, absolute);
        return form;
    }

    internal static string BuildEffectiveUri(HttpRequest request)
    {
        HttpRequestTargetForm form = Determine(request);
        if (form == HttpRequestTargetForm.Absolute)
        {
            return request.Target;
        }

        string scheme = ResolveScheme(request);
        string? host = ReadHost(request);
        string authority;
        string suffix = "";
        if (form == HttpRequestTargetForm.Authority)
        {
            authority = request.Target;
        }
        else
        {
            authority = request.Authority == null ? host ?? "" : request.Authority;
            if (form == HttpRequestTargetForm.Origin)
            {
                suffix = request.Target;
            }
        }

        UriValidationResult validated = ValidateAuthorityValue(authority, false);
        if ((Ascii.EqualsStringsIgnoreCase(scheme, "http") ||
            Ascii.EqualsStringsIgnoreCase(scheme, "https")) && validated.Host.Length == 0)
        {
            throw CreateProtocolError(
                "missing-request-authority",
                "An HTTP or HTTPS target URI requires a non-empty authority.");
        }

        return scheme + "://" + authority + suffix;
    }

    internal static bool AreAuthoritiesEquivalent(
        string left,
        string right,
        string scheme)
    {
        if (left == null || right == null || scheme == null)
        {
            return false;
        }

        UriValidationResult leftResult = UriCodec.ValidateAuthority(left, int.MaxValue);
        UriValidationResult rightResult = UriCodec.ValidateAuthority(right, int.MaxValue);
        if (!IsComparableAuthority(leftResult) || !IsComparableAuthority(rightResult))
        {
            return false;
        }

        string leftHost = NormalizeHost(leftResult.Host);
        string rightHost = NormalizeHost(rightResult.Host);
        if (leftHost != rightHost)
        {
            return false;
        }

        return GetEffectivePort(leftResult, scheme) == GetEffectivePort(rightResult, scheme);
    }

    static void ValidateAbsoluteAuthority(UriValidationResult absolute)
    {
        if (absolute.HasUserInfo)
        {
            throw CreateProtocolError(
                "request-target-userinfo",
                "Userinfo is not permitted in a request target.");
        }

        if (absolute.HasAuthority)
        {
            UriValidationResult authority = ValidateAuthorityValue(absolute.Authority, false);
            if (authority.Host.Length == 0 &&
                (Ascii.EqualsStringsIgnoreCase(absolute.Scheme, "http") ||
                Ascii.EqualsStringsIgnoreCase(absolute.Scheme, "https")))
            {
                throw CreateProtocolError(
                    "missing-request-authority",
                    "An HTTP or HTTPS request target requires a host.");
            }
        }
        else if (Ascii.EqualsStringsIgnoreCase(absolute.Scheme, "http") ||
            Ascii.EqualsStringsIgnoreCase(absolute.Scheme, "https"))
        {
            throw CreateProtocolError(
                "missing-request-authority",
                "An HTTP or HTTPS request target requires an authority.");
        }
    }

    static void ValidateControlAuthority(
        HttpRequest request,
        string? host,
        HttpRequestTargetForm form,
        UriValidationResult? absolute)
    {
        if (request.Authority != null)
        {
            ValidateAuthorityValue(request.Authority, false);
        }

        if (host != null)
        {
            ValidateAuthorityValue(host, false);
        }

        string scheme = request.Scheme ?? "";
        if (absolute != null)
        {
            scheme = absolute.Scheme;
        }

        if (request.Authority != null && host != null &&
            !AreAuthoritiesEquivalent(request.Authority, host, scheme))
        {
            throw CreateProtocolError(
                "conflicting-request-authority",
                "The Host field and request authority identify different entities.");
        }

        if (form == HttpRequestTargetForm.Authority && request.Authority != null &&
            !AreAuthoritiesEquivalent(request.Target, request.Authority, scheme))
        {
            throw CreateProtocolError(
                "conflicting-request-authority",
                "The CONNECT target and request authority identify different entities.");
        }
    }

    static string? ReadHost(HttpRequest request)
    {
        int count = 0;
        string? result = null;
        int index = 0;
        while (index < request.Headers.Fields.Count)
        {
            HttpHeaderField field = request.Headers.Fields[index];
            if (field == null || field.Name == null || field.Value == null)
            {
                throw CreateProtocolError(
                    "invalid-request-field",
                    "The request contains an incomplete field.");
            }

            if (Ascii.EqualsStringsIgnoreCase(field.Name, HttpHeaderNames.Host))
            {
                count = count + 1;
                result = DecodeHostValue(field.Value);
            }

            index = index + 1;
        }

        if (count > 1)
        {
            throw CreateProtocolError(
                "multiple-host-fields",
                "A request cannot contain more than one Host field.");
        }

        if (request.ProtocolVersion == HttpProtocolVersion.Http11 && count != 1)
        {
            throw CreateProtocolError(
                "missing-host-field",
                "An HTTP/1.1 request requires exactly one Host field.");
        }

        return result;
    }

    static string DecodeHostValue(byte[] value)
    {
        int start = 0;
        while (start < value.Length && (value[start] == 0x20 || value[start] == 0x09))
        {
            start = start + 1;
        }

        int end = value.Length;
        while (end > start && (value[end - 1] == 0x20 || value[end - 1] == 0x09))
        {
            end = end - 1;
        }

        StringBuilder result = new StringBuilder();
        int index = start;
        while (index < end)
        {
            byte character = value[index];
            if (character > 0x7f || character < 0x21 || character == 0x7f)
            {
                throw CreateProtocolError(
                    "invalid-host-field",
                    "The Host field value is malformed.");
            }

            result.Append((char)character);
            index = index + 1;
        }

        return result.ToString();
    }

    static UriValidationResult ValidateAuthorityValue(string authority, bool requirePort)
    {
        UriValidationResult result = UriCodec.ValidateAuthority(authority, int.MaxValue);
        if (result.Status != HttpOperationStatus.Done || result.HasUserInfo ||
            result.HasPort && result.HasPortValue && !result.IsPortInRange ||
            requirePort && (!result.HasPort || !result.HasPortValue || result.Host.Length == 0))
        {
            throw CreateProtocolError(
                result.HasUserInfo ? "request-target-userinfo" : "invalid-request-authority",
                "The request authority is malformed.");
        }

        return result;
    }

    static bool ValidateOriginTarget(string target)
    {
        int query = FindCharacter(target, '?');
        int fragment = FindCharacter(target, '#');
        if (fragment >= 0)
        {
            return false;
        }

        int pathEnd = query < 0 ? target.Length : query;
        string path = target.Substring(0, pathEnd);
        UriValidationResult pathResult = UriCodec.ValidatePath(path, int.MaxValue);
        if (pathResult.Status != HttpOperationStatus.Done || path.Length == 0 || path[0] != '/')
        {
            return false;
        }

        if (query >= 0)
        {
            string queryValue = target.Substring(query + 1);
            UriValidationResult queryResult = UriCodec.ValidateQuery(queryValue, int.MaxValue);
            if (queryResult.Status != HttpOperationStatus.Done)
            {
                return false;
            }
        }

        return true;
    }

    static string ResolveScheme(HttpRequest request)
    {
        if (request.Scheme == null)
        {
            throw CreateProtocolError(
                "missing-request-scheme",
                "A scheme is required to construct the effective request URI.");
        }

        UriValidationResult result = UriCodec.ValidateScheme(request.Scheme, int.MaxValue);
        if (result.Status != HttpOperationStatus.Done)
        {
            throw CreateProtocolError(
                "invalid-request-scheme",
                "The request scheme is malformed.");
        }

        return request.Scheme;
    }

    static bool IsComparableAuthority(UriValidationResult result)
    {
        return result.Status == HttpOperationStatus.Done && !result.HasUserInfo &&
            result.Host.Length != 0 &&
            (!result.HasPort || !result.HasPortValue || result.IsPortInRange);
    }

    static string NormalizeHost(string host)
    {
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < host.Length)
        {
            char character = host[index];
            if (character == '%' && index + 2 < host.Length)
            {
                int high = HexValue(host[index + 1]);
                int low = HexValue(host[index + 2]);
                char decoded = (char)(high * 16 + low);
                if (IsUnreserved(decoded))
                {
                    result.Append(FoldToLower(decoded));
                }
                else
                {
                    result.Append('%');
                    result.Append(HexCharacter(high));
                    result.Append(HexCharacter(low));
                }

                index = index + 3;
                continue;
            }

            result.Append(FoldToLower(character));
            index = index + 1;
        }

        return result.ToString();
    }

    static int GetEffectivePort(UriValidationResult authority, string scheme)
    {
        if (!authority.HasPort || !authority.HasPortValue)
        {
            return -1;
        }

        if (Ascii.EqualsStringsIgnoreCase(scheme, "http") && authority.Port == 80 ||
            Ascii.EqualsStringsIgnoreCase(scheme, "https") && authority.Port == 443)
        {
            return -1;
        }

        return authority.Port;
    }

    static int FindCharacter(string value, char character)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] == character)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    static int HexValue(char character)
    {
        if (character >= '0' && character <= '9')
        {
            return character - '0';
        }

        if (character >= 'A' && character <= 'F')
        {
            return character - 'A' + 10;
        }

        return character - 'a' + 10;
    }

    static char HexCharacter(int value)
    {
        if (value < 10)
        {
            return (char)('0' + value);
        }

        return (char)('A' + value - 10);
    }

    static bool IsUnreserved(char character)
    {
        return character >= 'A' && character <= 'Z' ||
            character >= 'a' && character <= 'z' ||
            character >= '0' && character <= '9' ||
            character == '-' || character == '.' || character == '_' || character == '~';
    }

    static char FoldToLower(char character)
    {
        if (character >= 'A' && character <= 'Z')
        {
            return (char)(character + 0x20);
        }

        return character;
    }

    static HttpProtocolException CreateProtocolError(string code, string message)
    {
        return new HttpProtocolException(message, code);
    }
}
