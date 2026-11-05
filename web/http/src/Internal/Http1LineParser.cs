using System;
using System.Text;

namespace http;

internal class Http1RequestLineResult
{
    internal HttpOperationStatus Status;
    internal HttpRequest? Request;
    internal bool HasErrorPosition;
    internal int ErrorPosition;
    internal string? ErrorCode;
    internal string? ErrorMessage;
}

internal class Http1StatusLineResult
{
    internal HttpOperationStatus Status;
    internal HttpResponse? Response;
    internal bool HasErrorPosition;
    internal int ErrorPosition;
    internal string? ErrorCode;
    internal string? ErrorMessage;
}

internal class Http1FieldLineResult
{
    internal HttpOperationStatus Status;
    internal HttpHeaderField? Field;
    internal bool IsContinuation;
    internal byte[] Continuation = Array.Empty<byte>();
    internal bool HasErrorPosition;
    internal int ErrorPosition;
    internal string? ErrorCode;
    internal string? ErrorMessage;
}

internal static class Http1LineParser
{
    internal static Http1RequestLineResult ParseRequestLine(byte[] input)
    {
        if (input == null)
        {
            return CreateRejected(
                "invalid-http1-request-line",
                "The HTTP/1 request line is missing.",
                0);
        }

        int control = FindProhibitedControl(input);
        if (control >= 0)
        {
            return CreateRejected(
                "invalid-http1-request-line-control",
                "The HTTP/1 request line contains a prohibited control octet.",
                control);
        }

        int firstSpace = FindByte(input, 0x20, 0);
        int secondSpace = firstSpace < 0 ? -1 :
            FindByte(input, 0x20, firstSpace + 1);
        if (firstSpace <= 0 || secondSpace <= firstSpace + 1 ||
            secondSpace == input.Length - 1 ||
            FindByte(input, 0x20, secondSpace + 1) >= 0)
        {
            return CreateRejected(
                "invalid-http1-request-line-whitespace",
                "The request-line components require exactly one space separator.",
                FindWhitespaceError(input, firstSpace, secondSpace));
        }

        if (!IsAsciiRange(input, 0, input.Length))
        {
            return CreateRejected(
                "invalid-http1-request-line-octet",
                "The HTTP/1 request line contains a non-ASCII octet.",
                FindNonAscii(input));
        }

        string method = Encoding.ASCII.GetString(input, 0, firstSpace);
        if (!HeaderStore.IsValidName(method))
        {
            return CreateRejected(
                "invalid-http1-method",
                "The HTTP/1 request method is not a valid token.",
                0);
        }

        string target = Encoding.ASCII.GetString(
            input,
            firstSpace + 1,
            secondSpace - firstSpace - 1);
        HttpProtocolVersion version = HttpProtocolVersion.Http11;
        if (!ReadVersion(
            input,
            secondSpace + 1,
            input.Length - secondSpace - 1,
            version))
        {
            version = HttpProtocolVersion.Http10;
            if (!ReadVersion(
                input,
                secondSpace + 1,
                input.Length - secondSpace - 1,
                version))
            {
                return CreateRejected(
                    "invalid-http1-version",
                    "The request line does not contain HTTP/1.0 or HTTP/1.1.",
                    secondSpace + 1);
            }
        }

        HttpRequest request = new HttpRequest();
        request.ProtocolVersion = version;
        request.Method = method;
        request.MethodMetadata = MethodSemantics.Describe(method);
        request.Target = target;
        Http1RequestLineResult targetResult = ApplyRequestTarget(
            request,
            firstSpace + 1);
        if (targetResult.Status != HttpOperationStatus.Done)
        {
            return targetResult;
        }

        Http1RequestLineResult result = new Http1RequestLineResult();
        result.Status = HttpOperationStatus.Done;
        result.Request = request;
        return result;
    }

    internal static Http1StatusLineResult ParseStatusLine(byte[] input)
    {
        if (input == null)
        {
            return CreateRejectedStatus(
                "invalid-http1-status-line",
                "The HTTP/1 status line is missing.",
                0);
        }

        int firstSpace = FindByte(input, 0x20, 0);
        if (firstSpace <= 0)
        {
            return CreateRejectedStatus(
                "invalid-http1-status-version",
                "The status line does not begin with a supported HTTP version.",
                0);
        }

        HttpProtocolVersion version = HttpProtocolVersion.Http11;
        if (!ReadVersion(input, 0, firstSpace, version))
        {
            version = HttpProtocolVersion.Http10;
            if (!ReadVersion(input, 0, firstSpace, version))
            {
                return CreateRejectedStatus(
                    "invalid-http1-status-version",
                    "The status line does not begin with HTTP/1.0 or HTTP/1.1.",
                    0);
            }
        }

        int statusStart = firstSpace + 1;
        if (statusStart + 3 > input.Length ||
            !IsDigit(input[statusStart]) ||
            !IsDigit(input[statusStart + 1]) ||
            !IsDigit(input[statusStart + 2]) ||
            statusStart + 3 < input.Length &&
            IsDigit(input[statusStart + 3]))
        {
            return CreateRejectedStatus(
                "invalid-http1-status-code",
                "The HTTP/1 status code must contain exactly three digits.",
                statusStart);
        }

        if (statusStart + 3 >= input.Length || input[statusStart + 3] != 0x20)
        {
            return CreateRejectedStatus(
                "invalid-http1-status-line-whitespace",
                "A single space must follow the three-digit HTTP status code.",
                statusStart + 3);
        }

        int statusCode = (input[statusStart] - 0x30) * 100 +
            (input[statusStart + 1] - 0x30) * 10 +
            input[statusStart + 2] - 0x30;
        if (statusCode < 100)
        {
            return CreateRejectedStatus(
                "invalid-http1-status-code",
                "The HTTP/1 status code must be a supported three-digit value.",
                statusStart);
        }

        int reasonStart = statusStart + 4;
        int reasonError = FindReasonError(input, reasonStart);
        if (reasonError >= 0)
        {
            return CreateRejectedStatus(
                "invalid-http1-reason-phrase",
                "The HTTP/1 reason phrase contains a prohibited control octet.",
                reasonError);
        }

        HttpResponse response = new HttpResponse();
        response.ProtocolVersion = version;
        response.StatusCode = statusCode;
        response.HasReasonPhrase = true;
        response.ReasonPhrase = CopyRange(
            input,
            reasonStart,
            input.Length - reasonStart);
        response.IsInformational = statusCode < 200;
        Http1StatusLineResult result = new Http1StatusLineResult();
        result.Status = HttpOperationStatus.Done;
        result.Response = response;
        return result;
    }

    internal static Http1FieldLineResult ParseFieldLine(
        byte[] input,
        bool allowWhitespaceBeforeColon,
        bool allowObsoleteLineFolding)
    {
        if (input == null || input.Length == 0)
        {
            return CreateRejectedField(
                "invalid-http1-field-line",
                "An HTTP/1 field line cannot be empty.",
                0);
        }

        if (input[0] == 0x20 || input[0] == 0x09)
        {
            return ParseContinuationLine(input, allowObsoleteLineFolding);
        }

        int colon = FindByte(input, 0x3a, 0);
        if (colon <= 0)
        {
            return CreateRejectedField(
                "invalid-http1-field-name",
                "An HTTP/1 field line requires a non-empty name and colon.",
                colon < 0 ? input.Length : 0);
        }

        int nameEnd = colon;
        while (nameEnd > 0 &&
            (input[nameEnd - 1] == 0x20 || input[nameEnd - 1] == 0x09))
        {
            nameEnd = nameEnd - 1;
        }

        if (nameEnd != colon && !allowWhitespaceBeforeColon)
        {
            return CreateRejectedField(
                "http1-whitespace-before-colon",
                "Whitespace between an HTTP/1 field name and colon is prohibited.",
                nameEnd);
        }

        if (nameEnd == 0 || !IsAsciiRange(input, 0, nameEnd))
        {
            return CreateRejectedField(
                "invalid-http1-field-name",
                "The HTTP/1 field name is not an ASCII token.",
                0);
        }

        string name = Encoding.ASCII.GetString(input, 0, nameEnd);
        if (!HeaderStore.IsValidName(name))
        {
            return CreateRejectedField(
                "invalid-http1-field-name",
                "The HTTP/1 field name is not a valid token.",
                FindInvalidName(input, nameEnd));
        }

        int valueStart = colon + 1;
        while (valueStart < input.Length &&
            (input[valueStart] == 0x20 || input[valueStart] == 0x09))
        {
            valueStart = valueStart + 1;
        }

        int valueEnd = input.Length;
        while (valueEnd > valueStart &&
            (input[valueEnd - 1] == 0x20 || input[valueEnd - 1] == 0x09))
        {
            valueEnd = valueEnd - 1;
        }

        byte[] value = CopyRange(input, valueStart, valueEnd - valueStart);
        if (!HeaderStore.IsValidValue(value))
        {
            return CreateRejectedField(
                "invalid-http1-field-value",
                "The HTTP/1 field value contains a prohibited control octet.",
                FindInvalidValue(input, valueStart, valueEnd));
        }

        HttpHeaderField field = new HttpHeaderField();
        field.Name = name;
        field.Value = value;
        Http1FieldLineResult result = new Http1FieldLineResult();
        result.Status = HttpOperationStatus.Done;
        result.Field = field;
        return result;
    }

    static Http1FieldLineResult ParseContinuationLine(
        byte[] input,
        bool allowObsoleteLineFolding)
    {
        if (!allowObsoleteLineFolding)
        {
            return CreateRejectedField(
                "obsolete-http1-line-folding",
                "Obsolete HTTP/1 line folding is disabled.",
                0);
        }

        if (!HeaderStore.IsValidValue(input))
        {
            return CreateRejectedField(
                "invalid-http1-field-value",
                "An obsolete folded line contains a prohibited control octet.",
                FindInvalidValue(input, 0, input.Length));
        }

        int start = 0;
        while (start < input.Length &&
            (input[start] == 0x20 || input[start] == 0x09))
        {
            start = start + 1;
        }

        int end = input.Length;
        while (end > start &&
            (input[end - 1] == 0x20 || input[end - 1] == 0x09))
        {
            end = end - 1;
        }

        Http1FieldLineResult result = new Http1FieldLineResult();
        result.Status = HttpOperationStatus.Done;
        result.IsContinuation = true;
        result.Continuation = CopyRange(input, start, end - start);
        return result;
    }

    static Http1RequestLineResult ApplyRequestTarget(
        HttpRequest request,
        int targetPosition)
    {
        if (request.Target == "*")
        {
            if (request.Method != HttpMethods.Options)
            {
                return CreateRejected(
                    "invalid-http1-request-target-form",
                    "Only OPTIONS can use the asterisk-form request target.",
                    targetPosition);
            }

            request.TargetForm = HttpRequestTargetForm.Asterisk;
            return CreateDone();
        }

        if (request.Method == HttpMethods.Connect)
        {
            UriValidationResult authority = UriCodec.ValidateAuthority(
                request.Target,
                int.MaxValue);
            if (authority.Status != HttpOperationStatus.Done ||
                authority.HasUserInfo || authority.Host.Length == 0 ||
                !authority.HasPort || !authority.HasPortValue ||
                !authority.IsPortInRange)
            {
                return CreateRejected(
                    "invalid-http1-authority-target",
                    "CONNECT requires an authority-form target with a host and port.",
                    targetPosition + authority.ErrorPosition);
            }

            request.TargetForm = HttpRequestTargetForm.Authority;
            request.Authority = request.Target;
            return CreateDone();
        }

        if (request.Target.Length > 0 && request.Target[0] == '/')
        {
            return ApplyOriginTarget(request, targetPosition);
        }

        UriValidationResult absolute = UriCodec.ValidateUriReference(
            request.Target,
            int.MaxValue);
        if (absolute.Status != HttpOperationStatus.Done || !absolute.IsAbsolute ||
            absolute.HasFragment || absolute.HasUserInfo ||
            absolute.HasPort && absolute.HasPortValue && !absolute.IsPortInRange ||
            IsHttpScheme(absolute.Scheme) &&
            (!absolute.HasAuthority || absolute.Host.Length == 0))
        {
            return CreateRejected(
                "invalid-http1-absolute-target",
                "The absolute-form request target is malformed or unsafe.",
                targetPosition + absolute.ErrorPosition);
        }

        request.TargetForm = HttpRequestTargetForm.Absolute;
        request.Scheme = absolute.Scheme;
        request.Authority = absolute.HasAuthority ? absolute.Authority : null;
        request.Path = absolute.Path;
        request.HasQuery = absolute.HasQuery;
        request.Query = absolute.HasQuery ? absolute.Query : null;
        return CreateDone();
    }

    static Http1RequestLineResult ApplyOriginTarget(
        HttpRequest request,
        int targetPosition)
    {
        int fragment = request.Target.IndexOf('#');
        int query = request.Target.IndexOf('?');
        int pathEnd = query < 0 ? request.Target.Length : query;
        string path = request.Target.Substring(0, pathEnd);
        UriValidationResult pathResult = UriCodec.ValidatePath(path, int.MaxValue);
        if (fragment >= 0 || pathResult.Status != HttpOperationStatus.Done ||
            path.Length == 0 || path[0] != '/')
        {
            int error = fragment >= 0 ? fragment : pathResult.ErrorPosition;
            return CreateRejected(
                "invalid-http1-origin-target",
                "The origin-form request target is malformed.",
                targetPosition + error);
        }

        string? queryValue = null;
        if (query >= 0)
        {
            queryValue = request.Target.Substring(query + 1);
            UriValidationResult queryResult = UriCodec.ValidateQuery(
                queryValue,
                int.MaxValue);
            if (queryResult.Status != HttpOperationStatus.Done)
            {
                return CreateRejected(
                    "invalid-http1-origin-target",
                    "The origin-form request target has a malformed query.",
                    targetPosition + query + 1 + queryResult.ErrorPosition);
            }
        }

        request.TargetForm = HttpRequestTargetForm.Origin;
        request.Path = path;
        request.HasQuery = query >= 0;
        request.Query = queryValue;
        return CreateDone();
    }

    static bool ReadVersion(
        byte[] value,
        int start,
        int count,
        HttpProtocolVersion version)
    {
        string expected = version == HttpProtocolVersion.Http10 ?
            HttpProtocolNames.Http10 : HttpProtocolNames.Http11;
        if (count != expected.Length)
        {
            return false;
        }

        int index = 0;
        while (index < count)
        {
            if (value[start + index] != (byte)expected[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static bool IsHttpScheme(string value)
    {
        return Ascii.EqualsStringsIgnoreCase(value, "http") ||
            Ascii.EqualsStringsIgnoreCase(value, "https");
    }

    static int FindReasonError(byte[] value, int start)
    {
        int index = start;
        while (index < value.Length)
        {
            byte character = value[index];
            if (character != 0x09 && character < 0x20 || character == 0x7f)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    static int FindProhibitedControl(byte[] value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] < 0x20 || value[index] == 0x7f)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    static bool IsAsciiRange(byte[] value, int start, int count)
    {
        int index = 0;
        while (index < count)
        {
            if (value[start + index] > 0x7f)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static int FindNonAscii(byte[] value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] > 0x7f)
            {
                return index;
            }

            index = index + 1;
        }

        return 0;
    }

    static int FindInvalidName(byte[] value, int end)
    {
        int index = 0;
        while (index < end)
        {
            if (value[index] > 0x7f ||
                !HttpGrammar.IsTokenCharacter(value[index]))
            {
                return index;
            }

            index = index + 1;
        }

        return 0;
    }

    static int FindInvalidValue(byte[] value, int start, int end)
    {
        int index = start;
        while (index < end)
        {
            byte character = value[index];
            if (character != 0x09 && character < 0x20 || character == 0x7f)
            {
                return index;
            }

            index = index + 1;
        }

        return start;
    }

    static int FindWhitespaceError(
        byte[] value,
        int firstSpace,
        int secondSpace)
    {
        if (firstSpace < 0)
        {
            return 0;
        }

        if (secondSpace < 0)
        {
            return firstSpace;
        }

        if (secondSpace == firstSpace + 1)
        {
            return secondSpace;
        }

        if (secondSpace == value.Length - 1)
        {
            return secondSpace;
        }

        int extra = FindByte(value, 0x20, secondSpace + 1);
        return extra < 0 ? secondSpace : extra;
    }

    static int FindByte(byte[] value, byte searched, int start)
    {
        int index = start;
        while (index < value.Length)
        {
            if (value[index] == searched)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    static bool IsDigit(byte value)
    {
        return value >= 0x30 && value <= 0x39;
    }

    static byte[] CopyRange(byte[] value, int start, int count)
    {
        byte[] result = new byte[count];
        int index = 0;
        while (index < count)
        {
            result[index] = value[start + index];
            index = index + 1;
        }

        return result;
    }

    static Http1RequestLineResult CreateDone()
    {
        Http1RequestLineResult result = new Http1RequestLineResult();
        result.Status = HttpOperationStatus.Done;
        return result;
    }

    static Http1RequestLineResult CreateRejected(
        string code,
        string message,
        int position)
    {
        Http1RequestLineResult result = new Http1RequestLineResult();
        result.Status = HttpOperationStatus.Rejected;
        result.HasErrorPosition = true;
        result.ErrorPosition = position < 0 ? 0 : position;
        result.ErrorCode = code;
        result.ErrorMessage = message;
        return result;
    }

    static Http1StatusLineResult CreateRejectedStatus(
        string code,
        string message,
        int position)
    {
        Http1StatusLineResult result = new Http1StatusLineResult();
        result.Status = HttpOperationStatus.Rejected;
        result.HasErrorPosition = true;
        result.ErrorPosition = position < 0 ? 0 : position;
        result.ErrorCode = code;
        result.ErrorMessage = message;
        return result;
    }

    static Http1FieldLineResult CreateRejectedField(
        string code,
        string message,
        int position)
    {
        Http1FieldLineResult result = new Http1FieldLineResult();
        result.Status = HttpOperationStatus.Rejected;
        result.HasErrorPosition = true;
        result.ErrorPosition = position < 0 ? 0 : position;
        result.ErrorCode = code;
        result.ErrorMessage = message;
        return result;
    }
}
