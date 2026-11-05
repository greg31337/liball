namespace sip;

internal static class SipMimeParser
{
    internal static SipParseResult ParseEntityBytes(byte[]? source, SipParseOptions? options)
    {
        if (source == null) return CreateError(0, "MIME source bytes are required.");
        if (options == null) return CreateError(0, "MIME parse options are required.");
        string? optionError = ValidateOptions(options);
        if (optionError != null) return CreateError(0, optionError);
        if (source.Length > options.MaxMessageBytes)
            return CreateError(options.MaxMessageBytes, "The MIME entity exceeds the configured message limit.");

        SipMimeParseState parsed = ParseEntityRange(source, options, 0, false);
        if (!parsed.Success || parsed.Entity == null)
            return CreateError(parsed.ErrorOffset, parsed.ErrorMessage == null
                ? "The MIME entity is invalid." : parsed.ErrorMessage);
        SipValidationIssue[] issues = SipMimeValidator.ValidateEntityWithLimit(
            parsed.Entity, options.Strict, options.MaxMimeDepth);
        if (options.Strict && HasErrorIssues(issues))
        {
            string? issueMessage = issues[0].Message;
            SipParseResult invalid = CreateError(0,
                issueMessage == null ? "The MIME entity is invalid." : issueMessage);
            invalid.RequiredBytes = source.Length;
            invalid.ValidationIssues = issues;
            return invalid;
        }
        SipParseResult result = new SipParseResult();
        result.Status = SipParseStatus.Complete;
        result.MimeEntity = parsed.Entity;
        result.Consumed = source.Length;
        result.RequiredBytes = source.Length;
        result.ValidationIssues = issues;
        return result;
    }

    internal static SipMessage? ParseSipFragmentBody(byte[] source, SipParseOptions options)
    {
        if (source.Length == 0)
        {
            SipMessage empty = new SipMessage();
            empty.Kind = SipMessageKind.Unknown;
            return empty;
        }
        bool hasStartLine = HasSipStartLine(source);
        byte[] terminated = AddFragmentTerminator(source);
        byte[] input;
        int prefixLength = 0;
        if (hasStartLine)
        {
            input = terminated;
        }
        else
        {
            byte[] prefix = System.Text.Encoding.ASCII.GetBytes("OPTIONS sip:fragment.invalid SIP/2.0\r\n");
            prefixLength = prefix.Length;
            input = new byte[prefix.Length + terminated.Length];
            System.Array.Copy(prefix, input, prefix.Length);
            System.Array.Copy(terminated, 0, input, prefix.Length, terminated.Length);
        }
        SipParseOptions compatible = CopyParseOptions(options);
        compatible.Strict = false;
        compatible.PreserveOriginal = false;
        compatible.MaxMessageBytes = input.Length > options.MaxMessageBytes ? input.Length : options.MaxMessageBytes;
        SipParseResult parsed = SipMessageParser.ParseDatagramMessageBytes(input, compatible);
        if (parsed.Status != SipParseStatus.Complete || parsed.Message == null) return null;
        SipMessage fragment = parsed.Message;
        if (!hasStartLine)
        {
            fragment.Kind = SipMessageKind.Unknown;
            fragment.Method = SipMethod.Unknown;
            fragment.MethodToken = null;
            fragment.RequestUri = null;
            fragment.StatusClass = SipStatusClass.Unknown;
            fragment.StatusCode = 0;
            fragment.ReasonPhrase = null;
        }
        fragment.OriginalBytes = SipMimeSyntax.CopyByteRange(source, 0, source.Length);
        if (prefixLength > 0 && fragment.Headers.Length == 0 && source.Length > 0) return null;
        return fragment;
    }

    static SipMimeParseState ParseEntityRange(byte[] source, SipParseOptions options, int depth,
        bool digestDefault)
    {
        SipMimeParseState headerState = ParseEntityHeaders(source, options);
        if (!headerState.Success || headerState.Entity == null) return headerState;
        SipMimeEntity entity = headerState.Entity;
        int bodyStart = headerState.ErrorOffset;
        int bodyLength = source.Length - bodyStart;
        if (bodyLength > options.MaxBodyBytes)
            return CreateStateError(bodyStart + options.MaxBodyBytes,
                "The MIME body exceeds the configured size limit.");
        if (entity.ContentType == null) entity.ContentType = CreateDefaultMediaType(digestDefault);
        if (options.PreserveOriginal)
            entity.OriginalBytes = SipMimeSyntax.CopyByteRange(source, 0, source.Length);

        if (SipMimeSyntax.IsMultipartType(entity.ContentType))
        {
            if (depth >= options.MaxMimeDepth)
                return CreateStateError(bodyStart, "The multipart nesting depth exceeds the configured limit.");
            string? boundary = SipMimeSyntax.GetBoundaryValue(entity.ContentType);
            if (!SipMimeSyntax.IsBoundaryValue(boundary))
                return CreateStateError(0, "A multipart entity requires one valid boundary parameter.");
            string boundaryValue = boundary == null ? "" : boundary;
            SipMimeParseState multipart = ParseMultipartBody(
                source, bodyStart, boundaryValue, entity, options, depth);
            if (!multipart.Success) return multipart;
        }
        else
        {
            entity.Body = SipMimeSyntax.CopyByteRange(source, bodyStart, bodyLength);
            if (SipMimeSyntax.IsSipFragmentType(entity.ContentType))
            {
                entity.SipFragment = ParseSipFragmentBody(entity.Body, options);
                if (entity.SipFragment == null)
                    return CreateStateError(bodyStart, "The message/sipfrag body is invalid.");
                if (!SipFragmentHasRequiredBodyHeaders(entity.SipFragment))
                    return CreateStateError(bodyStart,
                        "A SIP fragment body requires Content-Type and Content-Length fields.");
            }
        }
        SipMimeParseState result = new SipMimeParseState();
        result.Success = true;
        result.Entity = entity;
        return result;
    }

    static SipMimeParseState ParseEntityHeaders(byte[] source, SipParseOptions options)
    {
        SipMimeEntity entity = new SipMimeEntity();
        System.Collections.Generic.List<SipHeader> headers = new System.Collections.Generic.List<SipHeader>();
        int position = 0;
        while (true)
        {
            int lineEnd = FindCrlf(source, position);
            if (lineEnd < 0) return CreateStateError(position, "The MIME header section is not terminated.");
            int physicalLength = lineEnd + 2 - position;
            if (physicalLength > options.MaxHeaderLineBytes)
                return CreateStateError(position + options.MaxHeaderLineBytes,
                    "A MIME header line exceeds the configured size limit.");
            if (lineEnd == position)
            {
                position = lineEnd + 2;
                break;
            }
            int fieldStart = position;
            int fieldEnd = lineEnd + 2;
            while (fieldEnd < source.Length && (source[fieldEnd] == ' ' || source[fieldEnd] == '\t'))
            {
                int continuationEnd = FindCrlf(source, fieldEnd);
                if (continuationEnd < 0)
                    return CreateStateError(fieldEnd, "A folded MIME header line is not terminated.");
                if (continuationEnd + 2 - fieldEnd > options.MaxHeaderLineBytes)
                    return CreateStateError(fieldEnd + options.MaxHeaderLineBytes,
                        "A MIME header line exceeds the configured size limit.");
                fieldEnd = continuationEnd + 2;
            }
            if (fieldEnd - fieldStart > options.MaxHeaderBytes)
                return CreateStateError(fieldStart + options.MaxHeaderBytes,
                    "A MIME header field exceeds the configured size limit.");
            if (headers.Count >= options.MaxHeaderCount)
                return CreateStateError(fieldStart, "The MIME entity exceeds the configured header count.");
            byte[] fieldBytes = SipMimeSyntax.CopyByteRange(source, fieldStart, fieldEnd - fieldStart);
            SipHeaderParseResult parsed = SipHeaderParser.ParseHeaderBytes(fieldBytes, options);
            if (parsed.Status != SipParseStatus.Complete || parsed.Header == null)
                return CreateStateError(fieldStart + parsed.ErrorOffset,
                    parsed.ErrorMessage == null ? "The MIME header field is invalid." : parsed.ErrorMessage);
            headers.Add(parsed.Header);
            ApplyTypedHeader(entity, parsed);
            position = fieldEnd;
            if (position > options.MaxHeaderBytes)
                return CreateStateError(options.MaxHeaderBytes,
                    "The MIME header section exceeds the configured size limit.");
        }
        if (position > options.MaxHeaderBytes)
            return CreateStateError(options.MaxHeaderBytes,
                "The MIME header section exceeds the configured size limit.");
        entity.Headers = headers.ToArray();
        SipMimeParseState result = new SipMimeParseState();
        result.Success = true;
        result.Entity = entity;
        result.ErrorOffset = position;
        return result;
    }

    static void ApplyTypedHeader(SipMimeEntity entity, SipHeaderParseResult parsed)
    {
        SipHeader header = parsed.Header == null ? new SipHeader() : parsed.Header;
        if (SipMimeSyntax.HeaderNameIs(header.Name, SipHeaderNames.ContentType,
            SipHeaderNames.CompactContentType) && entity.ContentType == null)
            entity.ContentType = parsed.MediaType;
        else if (SipMimeSyntax.HeaderNameIs(header.Name, SipHeaderNames.ContentDisposition, null)
            && entity.ContentDisposition == null)
            entity.ContentDisposition = parsed.ContentDisposition;
        else if (SipMimeSyntax.HeaderNameIs(header.Name, "Content-Transfer-Encoding", null)
            && entity.ContentTransferEncoding == null)
        {
            string value = header.Value == null ? "" : header.Value.Trim();
            entity.ContentTransferEncoding = value;
        }
    }

    static SipMimeParseState ParseMultipartBody(byte[] source, int bodyStart, string boundary,
        SipMimeEntity entity, SipParseOptions options, int depth)
    {
        SipMimeBoundaryMatch first = FindBoundaryLine(source, bodyStart, boundary);
        if (!first.Found) return CreateStateError(bodyStart, "The multipart opening boundary is missing.");
        if (first.Closing) return CreateStateError(first.Start, "A multipart entity requires at least one body part.");
        int preambleEnd = RemoveEncapsulationCrlf(source, bodyStart, first.Start);
        entity.Preamble = SipMimeSyntax.CopyByteRange(source, bodyStart, preambleEnd - bodyStart);
        System.Collections.Generic.List<SipMimeEntity> parts = new System.Collections.Generic.List<SipMimeEntity>();
        SipMimeBoundaryMatch current = first;
        while (true)
        {
            int partStart = current.AfterLine;
            SipMimeBoundaryMatch next = FindBoundaryLine(source, partStart, boundary);
            if (!next.Found) return CreateStateError(source.Length, "The multipart closing boundary is missing.");
            int partEnd = RemoveEncapsulationCrlf(source, partStart, next.Start);
            if (MultipartPartNeedsEmptyBodyTerminator(source, partStart, partEnd, next.Start))
                partEnd = next.Start;
            byte[] partBytes = SipMimeSyntax.CopyByteRange(source, partStart, partEnd - partStart);
            bool digestDefault = entity.ContentType != null
                && SipAscii.EqualsIgnoreCase(entity.ContentType.Subtype, "digest");
            SipMimeParseState part = ParseEntityRange(partBytes, options, depth + 1, digestDefault);
            if (!part.Success || part.Entity == null)
            {
                part.ErrorOffset = part.ErrorOffset < 0 ? partStart : partStart + part.ErrorOffset;
                return part;
            }
            parts.Add(part.Entity);
            if (parts.Count > options.MaxListValueCount)
                return CreateStateError(partStart, "The multipart entity exceeds the configured part count.");
            if (next.Closing)
            {
                entity.Epilogue = SipMimeSyntax.CopyByteRange(
                    source, next.AfterLine, source.Length - next.AfterLine);
                break;
            }
            current = next;
        }
        entity.Parts = parts.ToArray();
        return CreateStateSuccess(entity);
    }

    static bool MultipartPartNeedsEmptyBodyTerminator(byte[] source, int start, int end,
        int boundaryStart)
    {
        if (boundaryStart - end != 2 || source[end] != '\r' || source[end + 1] != '\n')
            return false;
        if (end == start) return true;
        if (end - start >= 2 && source[start] == '\r' && source[start + 1] == '\n')
            return false;
        if (end - start < 2 || source[end - 2] != '\r' || source[end - 1] != '\n')
            return false;
        int index = start;
        while (index + 3 < end)
        {
            if (source[index] == '\r' && source[index + 1] == '\n'
                && source[index + 2] == '\r' && source[index + 3] == '\n') return false;
            index = index + 1;
        }
        return true;
    }

    static SipMimeBoundaryMatch FindBoundaryLine(byte[] source, int start, string boundary)
    {
        byte[] marker = System.Text.Encoding.ASCII.GetBytes("--" + boundary);
        int position = start;
        while (position <= source.Length - marker.Length)
        {
            bool lineStart = position == start || position >= 2
                && source[position - 2] == '\r' && source[position - 1] == '\n';
            if (lineStart && BytesMatch(source, position, marker))
            {
                int cursor = position + marker.Length;
                bool closing = cursor + 1 < source.Length
                    && source[cursor] == '-' && source[cursor + 1] == '-';
                if (closing) cursor = cursor + 2;
                while (cursor < source.Length && (source[cursor] == ' ' || source[cursor] == '\t'))
                    cursor = cursor + 1;
                if (cursor == source.Length || cursor + 1 < source.Length
                    && source[cursor] == '\r' && source[cursor + 1] == '\n')
                {
                    SipMimeBoundaryMatch match = new SipMimeBoundaryMatch();
                    match.Found = true;
                    match.Closing = closing;
                    match.Start = position;
                    match.AfterLine = cursor == source.Length ? cursor : cursor + 2;
                    return match;
                }
            }
            position = position + 1;
        }
        return new SipMimeBoundaryMatch();
    }

    static int RemoveEncapsulationCrlf(byte[] source, int minimum, int end)
    {
        return end - 2 >= minimum && source[end - 2] == '\r' && source[end - 1] == '\n' ? end - 2 : end;
    }

    static bool BytesMatch(byte[] source, int start, byte[] expected)
    {
        if (start < 0 || start + expected.Length > source.Length) return false;
        int index = 0;
        while (index < expected.Length)
        {
            if (source[start + index] != expected[index]) return false;
            index = index + 1;
        }
        return true;
    }

    static int FindCrlf(byte[] source, int start)
    {
        int index = start;
        while (index + 1 < source.Length)
        {
            if (source[index] == '\r' && source[index + 1] == '\n') return index;
            if (source[index] == '\n' || source[index] == '\r') return -1;
            index = index + 1;
        }
        return -1;
    }

    static bool HasSipStartLine(byte[] source)
    {
        int end = FindCrlf(source, 0);
        if (end < 0) end = source.Length;
        if (end == 0 || SipUtf8Validator.GetInvalidUtf8Offset(
            SipMimeSyntax.CopyByteRange(source, 0, end)) >= 0) return false;
        string line = System.Text.Encoding.UTF8.GetString(source, 0, end);
        if (line.Length >= 4 && SipAscii.EqualsIgnoreCase(line.Substring(0, 4), "SIP/")) return true;
        return line.EndsWith(" SIP/2.0", System.StringComparison.OrdinalIgnoreCase);
    }

    static byte[] AddFragmentTerminator(byte[] source)
    {
        if (ContainsHeaderSeparator(source)) return SipMimeSyntax.CopyByteRange(source, 0, source.Length);
        bool endsCrlf = source.Length >= 2 && source[source.Length - 2] == '\r'
            && source[source.Length - 1] == '\n';
        int added = endsCrlf ? 2 : 4;
        byte[] result = new byte[source.Length + added];
        System.Array.Copy(source, result, source.Length);
        int position = source.Length;
        if (!endsCrlf)
        {
            result[position] = (byte)'\r';
            result[position + 1] = (byte)'\n';
            position = position + 2;
        }
        result[position] = (byte)'\r';
        result[position + 1] = (byte)'\n';
        return result;
    }

    static bool ContainsHeaderSeparator(byte[] source)
    {
        int index = 0;
        while (index + 3 < source.Length)
        {
            if (source[index] == '\r' && source[index + 1] == '\n'
                && source[index + 2] == '\r' && source[index + 3] == '\n') return true;
            index = index + 1;
        }
        return false;
    }

    internal static bool SipFragmentHasRequiredBodyHeaders(SipMessage fragment)
    {
        if (fragment.Body == null || fragment.Body.Length == 0) return true;
        return SipGenericValueAccess.FindMessageHeaders(fragment, SipHeaderNames.ContentType).Length > 0
            && SipGenericValueAccess.FindMessageHeaders(fragment, SipHeaderNames.ContentLength).Length > 0;
    }

    static SipMediaType CreateDefaultMediaType(bool digestDefault)
    {
        SipMediaType mediaType = new SipMediaType();
        mediaType.Type = digestDefault ? "message" : "text";
        mediaType.Subtype = digestDefault ? "rfc822" : "plain";
        if (!digestDefault)
        {
            SipUriParameter charset = new SipUriParameter();
            charset.Name = "charset";
            charset.Value = "us-ascii";
            charset.HasValue = true;
            mediaType.Parameters = new SipUriParameter[] { charset };
        }
        return mediaType;
    }

    static SipParseOptions CopyParseOptions(SipParseOptions source)
    {
        SipParseOptions result = new SipParseOptions();
        result.Strict = source.Strict;
        result.PreserveOriginal = source.PreserveOriginal;
        result.AllowHeaderFolding = source.AllowHeaderFolding;
        result.AllowCompatibleWhitespace = source.AllowCompatibleWhitespace;
        result.MaxMessageBytes = source.MaxMessageBytes;
        result.MaxBodyBytes = source.MaxBodyBytes;
        result.MaxHeaderBytes = source.MaxHeaderBytes;
        result.MaxStartLineBytes = source.MaxStartLineBytes;
        result.MaxHeaderLineBytes = source.MaxHeaderLineBytes;
        result.MaxHeaderCount = source.MaxHeaderCount;
        result.MaxUriBytes = source.MaxUriBytes;
        result.MaxParameterCount = source.MaxParameterCount;
        result.MaxListValueCount = source.MaxListValueCount;
        result.MaxMimeDepth = source.MaxMimeDepth;
        result.MaxXmlDepth = source.MaxXmlDepth;
        return result;
    }

    static string? ValidateOptions(SipParseOptions options)
    {
        if (options.MaxMessageBytes < 0 || options.MaxBodyBytes < 0 || options.MaxHeaderBytes < 0
            || options.MaxHeaderLineBytes < 0 || options.MaxHeaderCount < 0
            || options.MaxParameterCount < 0 || options.MaxListValueCount < 0
            || options.MaxMimeDepth < 0) return "MIME parse limits cannot be negative.";
        return null;
    }

    static bool HasErrorIssues(SipValidationIssue[] issues)
    {
        int index = 0;
        while (index < issues.Length)
        {
            if (issues[index].Severity == SipValidationSeverity.Error) return true;
            index = index + 1;
        }
        return false;
    }

    static SipMimeParseState CreateStateSuccess(SipMimeEntity entity)
    {
        SipMimeParseState result = new SipMimeParseState();
        result.Success = true;
        result.Entity = entity;
        return result;
    }

    static SipMimeParseState CreateStateError(int offset, string message)
    {
        SipMimeParseState result = new SipMimeParseState();
        result.ErrorOffset = offset;
        result.ErrorMessage = message;
        return result;
    }

    static SipParseResult CreateError(int offset, string message)
    {
        SipParseResult result = new SipParseResult();
        result.Status = SipParseStatus.Error;
        result.ErrorOffset = offset;
        result.ErrorMessage = message;
        return result;
    }
}
