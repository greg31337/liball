namespace sip;

internal static class SipUasResponseCore
{
    internal static bool CanCreateResponse(SipMessage? request)
    {
        return request != null
            && request.Kind == SipMessageKind.Request
            && request.ViaValues != null
            && request.ViaValues.Length > 0
            && request.From != null
            && request.To != null
            && request.CSeq != null;
    }

    internal static SipMessage? CreateResponseMessage(
        SipMessage? request, int statusCode, string reason)
    {
        if (!CanCreateResponse(request) || request == null
            || statusCode < 100 || statusCode > 699)
        {
            return null;
        }
        return SipTransactionMessageFactory.CreateRequestResponse(
            request, statusCode,
            SanitizeReason(reason, "Response"),
            CreateResponseTag(request));
    }

    internal static SipMessage? CompleteApplicationResponse(
        SipMessage? request, SipMessage? applicationResponse)
    {
        if (applicationResponse == null
            || applicationResponse.Kind != SipMessageKind.Response
            || applicationResponse.StatusCode < 100
            || applicationResponse.StatusCode > 699)
        {
            return null;
        }
        SipMessage? response = CreateResponseMessage(request,
            applicationResponse.StatusCode,
            applicationResponse.ReasonPhrase == null
                ? "Response" : applicationResponse.ReasonPhrase);
        SipMessage? application =
            SipSnapshotCopy.CopyMessageModel(applicationResponse);
        if (response == null || application == null)
        {
            return null;
        }

        response.Body = application.Body == null
            ? new byte[0] : application.Body;
        response.ContentType = application.ContentType;
        response.ContentDisposition = application.ContentDisposition;
        response.ContactValues = application.ContactValues == null
            ? new SipContactValue[0] : application.ContactValues;
        response.Headers = MergeApplicationHeaders(
            response.Headers, application.Headers, response.Body.Length);
        AddTypedBodyHeaders(response);
        response.OriginalBytes = new byte[0];
        return response;
    }

    internal static void AppendResponseHeader(
        SipMessage response, string name, string value)
    {
        SipHeader header = new SipHeader();
        header.Name = name;
        header.Value = value;
        SipHeader[] updated =
            new SipHeader[response.Headers.Length + 1];
        int index = 0;
        while (index < response.Headers.Length)
        {
            updated[index] = response.Headers[index];
            index = index + 1;
        }
        updated[index] = header;
        response.Headers = updated;
        response.OriginalBytes = new byte[0];
    }

    internal static bool HasResponseHeader(
        SipMessage response, string name)
    {
        int index = 0;
        while (index < response.Headers.Length)
        {
            SipHeader? header = response.Headers[index];
            if (header != null
                && SipGenericValueAccess.HeaderNamesMatch(
                    header.Name, name))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static SipHeader[] MergeApplicationHeaders(
        SipHeader[] required, SipHeader[]? supplied, int bodyLength)
    {
        System.Collections.Generic.List<SipHeader> merged =
            new System.Collections.Generic.List<SipHeader>();
        int index = 0;
        while (index < required.Length)
        {
            SipHeader? header = required[index];
            if (header != null && !IsContentLength(header.Name))
            {
                merged.Add(header);
            }
            index = index + 1;
        }
        index = 0;
        while (supplied != null && index < supplied.Length)
        {
            SipHeader? header = supplied[index];
            if (header != null && !IsMandatoryResponseHeader(header.Name))
            {
                merged.Add(header);
            }
            index = index + 1;
        }
        SipHeader length = new SipHeader();
        length.Name = SipHeaderNames.ContentLength;
        length.Value = bodyLength.ToString(
            System.Globalization.CultureInfo.InvariantCulture);
        merged.Add(length);
        return merged.ToArray();
    }

    static void AddTypedBodyHeaders(SipMessage response)
    {
        if (response.ContentType != null
            && !HasResponseHeader(response, SipHeaderNames.ContentType))
        {
            SipCoreHeaderResult contentType = new SipCoreHeaderResult();
            contentType.Success = true;
            contentType.MediaType = response.ContentType;
            AppendFormattedCoreHeader(
                response, SipHeaderNames.ContentType, contentType);
        }
        if (response.ContentDisposition != null
            && !HasResponseHeader(
                response, SipHeaderNames.ContentDisposition))
        {
            SipCoreHeaderResult disposition = new SipCoreHeaderResult();
            disposition.Success = true;
            disposition.ContentDisposition = response.ContentDisposition;
            AppendFormattedCoreHeader(response,
                SipHeaderNames.ContentDisposition, disposition);
        }
    }

    static void AppendFormattedCoreHeader(SipMessage response,
        string name, SipCoreHeaderResult model)
    {
        SipWriteResult formatted = SipCoreHeaderCodec
            .FormatCoreHeaderValue(name, model,
                new SipWriteOptions());
        if (formatted.Success)
        {
            SipHeaderParseResult parsed =
                SipHeaderParser.ParseHeaderBytes(
                    formatted.Bytes, new SipParseOptions());
            if (parsed.Status == SipParseStatus.Complete
                && parsed.Header != null
                && parsed.Header.Name != null
                && parsed.Header.Value != null)
            {
                AppendResponseHeader(response,
                    parsed.Header.Name, parsed.Header.Value);
            }
        }
    }

    static bool IsMandatoryResponseHeader(string? name)
    {
        return SipGenericValueAccess.HeaderNamesMatch(
                name, SipHeaderNames.Via)
            || SipGenericValueAccess.HeaderNamesMatch(
                name, SipHeaderNames.From)
            || SipGenericValueAccess.HeaderNamesMatch(
                name, SipHeaderNames.To)
            || SipGenericValueAccess.HeaderNamesMatch(
                name, SipHeaderNames.CallId)
            || SipGenericValueAccess.HeaderNamesMatch(
                name, SipHeaderNames.CSeq)
            || IsContentLength(name);
    }

    static bool IsContentLength(string? name)
    {
        return SipGenericValueAccess.HeaderNamesMatch(
            name, SipHeaderNames.ContentLength);
    }

    static string CreateResponseTag(SipMessage request)
    {
        System.Text.StringBuilder source =
            new System.Text.StringBuilder();
        if (request.ViaValues.Length > 0)
        {
            SipViaValue? via = request.ViaValues[0];
            if (via != null)
            {
                source.Append(via.Branch);
                source.Append("|");
                source.Append(via.Host);
                source.Append("|");
                source.Append(via.Port.ToString(
                    System.Globalization.CultureInfo.InvariantCulture));
            }
        }
        source.Append("|");
        source.Append(ReadFirstHeaderValue(
            request.Headers, SipHeaderNames.CallId));
        source.Append("|");
        if (request.CSeq != null)
        {
            source.Append(request.CSeq.SequenceNumber.ToString(
                System.Globalization.CultureInfo.InvariantCulture));
            source.Append("|");
            source.Append(request.CSeq.MethodToken);
        }
        byte[] digest = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(source.ToString()));
        return "uas-" + System.Convert.ToHexString(digest)
            .Substring(0, 24);
    }

    static string? ReadFirstHeaderValue(
        SipHeader[]? headers, string name)
    {
        int index = 0;
        while (headers != null && index < headers.Length)
        {
            SipHeader? header = headers[index];
            if (header != null
                && SipGenericValueAccess.HeaderNamesMatch(
                    header.Name, name))
            {
                return header.Value;
            }
            index = index + 1;
        }
        return null;
    }

    static string SanitizeReason(string source, string fallback)
    {
        System.Text.StringBuilder builder =
            new System.Text.StringBuilder();
        int index = 0;
        while (index < source.Length && builder.Length < 256)
        {
            char value = source[index];
            if (value >= ' ' && value != '\u007f')
            {
                builder.Append(value);
            }
            index = index + 1;
        }
        string result = builder.ToString().Trim();
        return result.Length == 0 ? fallback : result;
    }
}
