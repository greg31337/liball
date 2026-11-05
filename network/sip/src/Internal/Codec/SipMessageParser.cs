namespace sip;

internal static class SipMessageParser
{
    internal static SipParseResult ParseStreamMessageBytes(byte[]? source, SipParseOptions? options)
    {
        if (source == null) return ParseFramedMessageBytes(source, options, false);
        if (options == null) return ParseFramedMessageBytes(source, options, false);
        string? optionError = ValidateOptions(options);
        if (optionError != null) return CreateError(0, optionError);
        int prefixLength = 0;
        while (prefixLength + 1 < source.Length
            && source[prefixLength] == '\r' && source[prefixLength + 1] == '\n')
            prefixLength = prefixLength + 2;
        if (prefixLength == 0) return ParseFramedMessageBytes(source, options, false);
        byte[] messageBytes = CopyRange(source, prefixLength, source.Length - prefixLength);
        SipParseResult result = ParseFramedMessageBytes(messageBytes, options, false);
        ShiftResultOffsets(result, prefixLength);
        return result;
    }

    internal static SipParseResult ParseDatagramMessageBytes(byte[]? source, SipParseOptions? options)
    {
        return ParseFramedMessageBytes(source, options, true);
    }

    static SipParseResult ParseFramedMessageBytes(byte[]? source, SipParseOptions? options, bool datagram)
    {
        if (source == null) return CreateError(0, "Message source bytes are required.");
        if (options == null) return CreateError(0, "Message parse options are required.");
        string? optionError = ValidateOptions(options);
        if (optionError != null) return CreateError(0, optionError);

        SipMessageLineResult startLine = ScanLine(source, 0);
        if (startLine.Error) return CreateError(startLine.ErrorOffset, "A bare or malformed line ending is not permitted.");
        if (!startLine.Complete)
        {
            if (source.Length >= options.MaxStartLineBytes)
                return CreateError(options.MaxStartLineBytes, "The start line exceeds the configured size limit.");
            if (source.Length >= options.MaxMessageBytes)
                return CreateError(options.MaxMessageBytes, "The message exceeds the configured size limit.");
            return CreateIncomplete();
        }
        if (startLine.NextOffset > options.MaxStartLineBytes)
            return CreateError(options.MaxStartLineBytes, "The start line exceeds the configured size limit.");
        if (startLine.NextOffset > options.MaxMessageBytes)
            return CreateError(options.MaxMessageBytes, "The message exceeds the configured size limit.");

        SipMessage message = new SipMessage();
        SipParseResult? startError = ParseStartLine(source, startLine.LineEnd, options, message);
        if (startError != null) return startError;

        int headersStart = startLine.NextOffset;
        int position = headersStart;
        int contentLength = -1;
        int contentLengthOffset = -1;
        System.Collections.Generic.List<SipHeader> headers = new System.Collections.Generic.List<SipHeader>();
        while (true)
        {
            SipMessageLineResult line = ScanLine(source, position);
            if (line.Error)
                return CreateError(line.ErrorOffset, "A bare or malformed line ending is not permitted.");
            if (!line.Complete)
            {
                SipParseResult? partialError = CheckPartialLimits(
                    source.Length, position, headersStart, options);
                if (partialError != null) return partialError;
                return CreateIncomplete();
            }

            if (line.LineEnd == position)
            {
                int consumed = line.NextOffset;
                if (consumed - headersStart > options.MaxHeaderBytes)
                    return CreateError(headersStart + options.MaxHeaderBytes,
                        "The header section exceeds the configured size limit.");
                if (consumed > options.MaxMessageBytes)
                    return CreateError(options.MaxMessageBytes, "The message exceeds the configured size limit.");
                message.Headers = headers.ToArray();
                return CompleteFramedMessage(source, options, message, consumed,
                    contentLength, contentLengthOffset, datagram);
            }

            if (source[position] == ' ' || source[position] == '\t')
                return CreateError(position, "A header continuation requires a preceding header field.");

            int logicalEnd = line.NextOffset;
            while (logicalEnd < source.Length
                && (source[logicalEnd] == ' ' || source[logicalEnd] == '\t'))
            {
                if (!options.AllowHeaderFolding)
                    return CreateError(logicalEnd, "Obsolete header folding is not permitted.");
                SipMessageLineResult continuation = ScanLine(source, logicalEnd);
                if (continuation.Error)
                    return CreateError(continuation.ErrorOffset, "A bare or malformed line ending is not permitted.");
                if (!continuation.Complete)
                {
                    SipParseResult? foldedError = CheckPartialLimits(
                        source.Length, position, headersStart, options);
                    if (foldedError != null) return foldedError;
                    return CreateIncomplete();
                }
                logicalEnd = continuation.NextOffset;
            }

            if (logicalEnd - position > options.MaxHeaderLineBytes)
                return CreateError(position + options.MaxHeaderLineBytes,
                    "The header field exceeds the configured line size limit.");
            if (logicalEnd - headersStart > options.MaxHeaderBytes)
                return CreateError(headersStart + options.MaxHeaderBytes,
                    "The header section exceeds the configured size limit.");
            if (logicalEnd > options.MaxMessageBytes)
                return CreateError(options.MaxMessageBytes, "The message exceeds the configured size limit.");
            if (headers.Count >= options.MaxHeaderCount)
                return CreateError(position, "The message exceeds the configured header count limit.");

            byte[] headerBytes = CopyRange(source, position, logicalEnd - position);
            SipHeaderParseResult parsed = SipHeaderParser.ParseHeaderBytes(headerBytes, options);
            if (parsed.Status != SipParseStatus.Complete || parsed.Header == null)
            {
                int localOffset = parsed.ErrorOffset < 0 ? 0 : parsed.ErrorOffset;
                return CreateError(position + localOffset,
                    parsed.ErrorMessage == null ? "The header field is invalid." : parsed.ErrorMessage);
            }
            headers.Add(parsed.Header);
            if (IsContentLengthHeader(parsed.Header.Name))
            {
                int valueOffset = position + GetHeaderValueOffset(headerBytes);
                if (contentLength < 0)
                {
                    contentLength = parsed.ContentLength;
                    contentLengthOffset = valueOffset;
                }
                else if (contentLength != parsed.ContentLength)
                    return CreateError(valueOffset, "Conflicting Content-Length header field values are not permitted.");
            }
            ApplyTypedHeader(message, parsed);
            position = logicalEnd;
        }
    }

    static SipParseResult CompleteFramedMessage(byte[] source, SipParseOptions options,
        SipMessage message, int headerEnd, int contentLength, int contentLengthOffset, bool datagram)
    {
        if (contentLength < 0 && !datagram)
            return CreateError(headerEnd, "A stream message requires a Content-Length header field.");
        int bodyLength = contentLength;
        if (bodyLength < 0) bodyLength = source.Length - headerEnd;
        if (bodyLength > options.MaxBodyBytes)
        {
            int offset = contentLengthOffset < 0 ? headerEnd + options.MaxBodyBytes : contentLengthOffset;
            return CreateError(offset, "The message body exceeds the configured size limit.");
        }
        if (bodyLength > options.MaxMessageBytes - headerEnd)
            return CreateError(options.MaxMessageBytes, "The message exceeds the configured size limit.");
        int required = headerEnd + bodyLength;
        if (source.Length < required)
        {
            if (datagram)
            {
                SipParseResult truncated = CreateError(source.Length,
                    "The datagram ends before the declared message body is complete.");
                truncated.RequiredBytes = required;
                return truncated;
            }
            SipParseResult pending = CreateIncomplete();
            pending.RequiredBytes = required;
            return pending;
        }

        message.Body = CopyRange(source, headerEnd, bodyLength);
        if (options.PreserveOriginal) message.OriginalBytes = CopyRange(source, 0, required);
        SipValidationIssue[] semanticIssues = SipMessageValidator.ValidateMessageModel(message, options.Strict);
        if (SipMessageValidator.HasErrorIssues(semanticIssues))
        {
            string? issueMessage = semanticIssues[0].Message;
            string error = issueMessage == null ? "The SIP message is semantically invalid." : issueMessage;
            SipParseResult invalid = CreateError(headerEnd, error);
            invalid.RequiredBytes = required;
            invalid.ValidationIssues = semanticIssues;
            return invalid;
        }
        SipParseResult complete = new SipParseResult();
        complete.Status = SipParseStatus.Complete;
        complete.Message = message;
        complete.Consumed = required;
        complete.RequiredBytes = required;
        complete.ValidationIssues = semanticIssues;
        return complete;
    }

    static string? ValidateOptions(SipParseOptions options)
    {
        if (options.MaxMessageBytes < 0 || options.MaxBodyBytes < 0
            || options.MaxHeaderBytes < 0 || options.MaxStartLineBytes < 0
            || options.MaxHeaderLineBytes < 0 || options.MaxHeaderCount < 0
            || options.MaxUriBytes < 0 || options.MaxParameterCount < 0
            || options.MaxListValueCount < 0)
            return "Message parse limits cannot be negative.";
        return null;
    }

    static SipMessageLineResult ScanLine(byte[] source, int start)
    {
        SipMessageLineResult result = new SipMessageLineResult();
        int index = start;
        while (index < source.Length)
        {
            if (source[index] == '\n')
            {
                result.Error = true;
                result.ErrorOffset = index;
                return result;
            }
            if (source[index] == '\r')
            {
                if (index + 1 == source.Length) return result;
                if (source[index + 1] != '\n')
                {
                    result.Error = true;
                    result.ErrorOffset = index;
                    return result;
                }
                result.Complete = true;
                result.LineEnd = index;
                result.NextOffset = index + 2;
                return result;
            }
            index = index + 1;
        }
        return result;
    }

    static SipParseResult? CheckPartialLimits(int available, int lineStart, int headersStart,
        SipParseOptions options)
    {
        if (available - lineStart >= options.MaxHeaderLineBytes)
            return CreateError(lineStart + options.MaxHeaderLineBytes,
                "The header field exceeds the configured line size limit.");
        if (available - headersStart >= options.MaxHeaderBytes)
            return CreateError(headersStart + options.MaxHeaderBytes,
                "The header section exceeds the configured size limit.");
        if (available >= options.MaxMessageBytes)
            return CreateError(options.MaxMessageBytes, "The message exceeds the configured size limit.");
        return null;
    }

    static SipParseResult? ParseStartLine(byte[] source, int lineEnd, SipParseOptions options,
        SipMessage message)
    {
        byte[] lineBytes = CopyRange(source, 0, lineEnd);
        int invalidUtf8 = SipUtf8Validator.GetInvalidUtf8Offset(lineBytes);
        if (invalidUtf8 >= 0) return CreateError(invalidUtf8, "The start line is not valid UTF-8.");
        string text = System.Text.Encoding.UTF8.GetString(lineBytes);
        if (text.Length == 0) return CreateError(0, "The start line is empty.");
        if (text.Length >= 4 && SipAscii.EqualsIgnoreCase(text.Substring(0, 4), "SIP/"))
            return ParseStatusLine(text, options, message);
        return ParseRequestLine(text, options, message);
    }

    static SipParseResult? ParseRequestLine(string text, SipParseOptions options, SipMessage message)
    {
        int firstSpace = FindWhitespace(text, 0);
        if (firstSpace <= 0)
            return CreateError(firstSpace < 0 ? text.Length : firstSpace,
                "A request line requires a method, request URI, and protocol version.");
        int uriStart = SkipWhitespace(text, firstSpace);
        if (!options.AllowCompatibleWhitespace
            && (text[firstSpace] != ' ' || uriStart != firstSpace + 1))
            return CreateError(firstSpace, "The request line requires single-space separators.");
        int secondSpace = FindWhitespace(text, uriStart);
        if (secondSpace <= uriStart)
            return CreateError(secondSpace < 0 ? text.Length : secondSpace,
                "A request line requires a request URI and protocol version.");
        int versionStart = SkipWhitespace(text, secondSpace);
        if (!options.AllowCompatibleWhitespace
            && (text[secondSpace] != ' ' || versionStart != secondSpace + 1))
            return CreateError(secondSpace, "The request line requires single-space separators.");
        int extraSpace = FindWhitespace(text, versionStart);
        if (extraSpace >= 0) return CreateError(extraSpace, "The protocol version cannot contain whitespace.");

        string method = text.Substring(0, firstSpace);
        int methodError = GetTokenErrorOffset(method);
        if (methodError >= 0) return CreateError(methodError, "The request method must be a token.");
        string uriText = text.Substring(uriStart, secondSpace - uriStart);
        SipParseResult uriResult = SipUriParser.ParseUriBytes(System.Text.Encoding.UTF8.GetBytes(uriText), options);
        if (uriResult.Status != SipParseStatus.Complete || uriResult.Uri == null)
        {
            int uriError = uriResult.ErrorOffset < 0 ? 0 : uriResult.ErrorOffset;
            return CreateError(uriStart + uriError,
                uriResult.ErrorMessage == null ? "The request URI is invalid." : uriResult.ErrorMessage);
        }
        int versionError = GetProtocolVersionErrorOffset(text, versionStart, text.Length);
        if (versionError >= 0) return CreateError(versionError, "The SIP protocol version is invalid.");

        message.Kind = SipMessageKind.Request;
        message.Method = GetMethod(method);
        message.MethodToken = method;
        message.RequestUri = uriResult.Uri;
        message.ProtocolVersion = text.Substring(versionStart);
        return null;
    }

    static SipParseResult? ParseStatusLine(string text, SipParseOptions options, SipMessage message)
    {
        int firstSpace = FindWhitespace(text, 0);
        if (firstSpace <= 0)
            return CreateError(firstSpace < 0 ? text.Length : firstSpace,
                "A status line requires a protocol version, status code, and reason phrase.");
        int codeStart = SkipWhitespace(text, firstSpace);
        if (!options.AllowCompatibleWhitespace
            && (text[firstSpace] != ' ' || codeStart != firstSpace + 1))
            return CreateError(firstSpace, "The status line requires single-space separators.");
        int secondSpace = FindWhitespace(text, codeStart);
        if (secondSpace < 0)
            return CreateError(text.Length, "A status line requires a reason-phrase separator.");
        int reasonStart = SkipWhitespace(text, secondSpace);
        if (!options.AllowCompatibleWhitespace
            && (text[secondSpace] != ' ' || reasonStart != secondSpace + 1))
            return CreateError(secondSpace, "The status line requires single-space separators.");

        int versionError = GetProtocolVersionErrorOffset(text, 0, firstSpace);
        if (versionError >= 0) return CreateError(versionError, "The SIP protocol version is invalid.");
        if (secondSpace - codeStart != 3)
            return CreateError(codeStart, "The response status code must contain exactly three digits.");
        int statusCode = 0;
        int codeIndex = codeStart;
        while (codeIndex < secondSpace)
        {
            if (text[codeIndex] < '0' || text[codeIndex] > '9')
                return CreateError(codeIndex, "The response status code must contain exactly three digits.");
            statusCode = statusCode * 10 + text[codeIndex] - '0';
            codeIndex = codeIndex + 1;
        }
        int reasonIndex = reasonStart;
        while (reasonIndex < text.Length)
        {
            int character = text[reasonIndex];
            if (character < 32 && character != '\t' || character == 127)
                return CreateError(GetUtf8ByteOffset(text, reasonIndex),
                    "The reason phrase contains an invalid control character.");
            reasonIndex = reasonIndex + 1;
        }

        message.Kind = SipMessageKind.Response;
        message.ProtocolVersion = text.Substring(0, firstSpace);
        message.StatusCode = statusCode;
        message.StatusClass = GetStatusClass(statusCode);
        message.ReasonPhrase = text.Substring(reasonStart);
        return null;
    }

    static int FindWhitespace(string text, int start)
    {
        int index = start;
        while (index < text.Length)
        {
            if (text[index] == ' ' || text[index] == '\t') return index;
            index = index + 1;
        }
        return -1;
    }

    static int SkipWhitespace(string text, int start)
    {
        int index = start;
        while (index < text.Length && (text[index] == ' ' || text[index] == '\t'))
            index = index + 1;
        return index;
    }

    static int GetTokenErrorOffset(string token)
    {
        if (token.Length == 0) return 0;
        int index = 0;
        while (index < token.Length)
        {
            if (!SipAscii.IsTokenCharacter(token[index])) return index;
            index = index + 1;
        }
        return -1;
    }

    static int GetProtocolVersionErrorOffset(string text, int start, int end)
    {
        string prefix = "SIP/";
        int prefixIndex = 0;
        while (prefixIndex < prefix.Length)
        {
            int offset = start + prefixIndex;
            if (offset >= end) return offset;
            string actual = text.Substring(offset, 1);
            string expected = prefix.Substring(prefixIndex, 1);
            if (!SipAscii.EqualsIgnoreCase(actual, expected)) return offset;
            prefixIndex = prefixIndex + 1;
        }
        int index = start + prefix.Length;
        int majorStart = index;
        while (index < end && text[index] >= '0' && text[index] <= '9') index = index + 1;
        if (index == majorStart) return index;
        if (index >= end || text[index] != '.') return index;
        index = index + 1;
        int minorStart = index;
        while (index < end && text[index] >= '0' && text[index] <= '9') index = index + 1;
        if (index == minorStart || index != end) return index;
        return -1;
    }

    static SipMethod GetMethod(string method)
    {
        switch (method)
        {
            case SipMethods.Ack: return SipMethod.Ack;
            case SipMethods.Bye: return SipMethod.Bye;
            case SipMethods.Cancel: return SipMethod.Cancel;
            case SipMethods.Info: return SipMethod.Info;
            case SipMethods.Invite: return SipMethod.Invite;
            case SipMethods.Message: return SipMethod.Message;
            case SipMethods.Notify: return SipMethod.Notify;
            case SipMethods.Options: return SipMethod.Options;
            case SipMethods.Prack: return SipMethod.Prack;
            case SipMethods.Publish: return SipMethod.Publish;
            case SipMethods.Refer: return SipMethod.Refer;
            case SipMethods.Register: return SipMethod.Register;
            case SipMethods.Subscribe: return SipMethod.Subscribe;
            case SipMethods.Update: return SipMethod.Update;
            default: return SipMethod.Unknown;
        }
    }

    static SipStatusClass GetStatusClass(int statusCode)
    {
        switch (statusCode / 100)
        {
            case 1: return SipStatusClass.Provisional;
            case 2: return SipStatusClass.Successful;
            case 3: return SipStatusClass.Redirection;
            case 4: return SipStatusClass.ClientError;
            case 5: return SipStatusClass.ServerError;
            case 6: return SipStatusClass.GlobalFailure;
            default: return SipStatusClass.Unknown;
        }
    }

    static bool IsContentLengthHeader(string? name)
    {
        return SipAscii.EqualsIgnoreCase(name, SipHeaderNames.ContentLength)
            || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.CompactContentLength);
    }

    static int GetHeaderValueOffset(byte[] headerBytes)
    {
        int index = 0;
        while (index < headerBytes.Length && headerBytes[index] != ':') index = index + 1;
        if (index < headerBytes.Length) index = index + 1;
        while (index < headerBytes.Length && (headerBytes[index] == ' ' || headerBytes[index] == '\t'))
            index = index + 1;
        return index;
    }

    static void ApplyTypedHeader(SipMessage message, SipHeaderParseResult parsed)
    {
        if (parsed.Header == null || parsed.Header.Name == null) return;
        string name = parsed.Header.Name;
        if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.Via)
            || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.CompactVia))
            message.ViaValues = AppendViaValues(message.ViaValues, parsed.ViaValues);
        else if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.Route))
            message.RouteValues = AppendRouteValues(message.RouteValues, parsed.RouteValues);
        else if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.RecordRoute))
            message.RecordRouteValues = AppendRouteValues(message.RecordRouteValues, parsed.RouteValues);
        else if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.Contact)
            || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.CompactContact))
            message.ContactValues = AppendContactValues(message.ContactValues, parsed.ContactValues);
        else if ((SipAscii.EqualsIgnoreCase(name, SipHeaderNames.From)
            || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.CompactFrom)) && message.From == null)
            message.From = parsed.NameAddress;
        else if ((SipAscii.EqualsIgnoreCase(name, SipHeaderNames.To)
            || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.CompactTo)) && message.To == null)
            message.To = parsed.NameAddress;
        else if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.CSeq) && message.CSeq == null)
            message.CSeq = parsed.CSeq;
        else if ((SipAscii.EqualsIgnoreCase(name, SipHeaderNames.ContentType)
            || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.CompactContentType)) && message.ContentType == null)
            message.ContentType = parsed.MediaType;
        else if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.ContentDisposition)
            && message.ContentDisposition == null)
            message.ContentDisposition = parsed.ContentDisposition;
    }

    static SipViaValue[] AppendViaValues(SipViaValue[] current, SipViaValue[] additions)
    {
        SipViaValue[] result = new SipViaValue[current.Length + additions.Length];
        System.Array.Copy(current, result, current.Length);
        System.Array.Copy(additions, 0, result, current.Length, additions.Length);
        return result;
    }

    static SipRouteValue[] AppendRouteValues(SipRouteValue[] current, SipRouteValue[] additions)
    {
        SipRouteValue[] result = new SipRouteValue[current.Length + additions.Length];
        System.Array.Copy(current, result, current.Length);
        System.Array.Copy(additions, 0, result, current.Length, additions.Length);
        return result;
    }

    static SipContactValue[] AppendContactValues(SipContactValue[] current, SipContactValue[] additions)
    {
        SipContactValue[] result = new SipContactValue[current.Length + additions.Length];
        System.Array.Copy(current, result, current.Length);
        System.Array.Copy(additions, 0, result, current.Length, additions.Length);
        return result;
    }

    static byte[] CopyRange(byte[] source, int start, int count)
    {
        byte[] result = new byte[count];
        System.Array.Copy(source, start, result, 0, count);
        return result;
    }

    static int GetUtf8ByteOffset(string text, int characterOffset)
    {
        if (characterOffset <= 0) return 0;
        if (characterOffset >= text.Length) return System.Text.Encoding.UTF8.GetByteCount(text);
        return System.Text.Encoding.UTF8.GetByteCount(text.Substring(0, characterOffset));
    }

    static void ShiftResultOffsets(SipParseResult result, int prefixLength)
    {
        result.Consumed = result.Consumed + prefixLength;
        if (result.RequiredBytes >= 0) result.RequiredBytes = result.RequiredBytes + prefixLength;
        if (result.ErrorOffset >= 0) result.ErrorOffset = result.ErrorOffset + prefixLength;
        int index = 0;
        while (index < result.ValidationIssues.Length)
        {
            if (result.ValidationIssues[index].ByteOffset >= 0)
                result.ValidationIssues[index].ByteOffset = result.ValidationIssues[index].ByteOffset + prefixLength;
            index = index + 1;
        }
    }

    static SipParseResult CreateIncomplete()
    {
        SipParseResult result = new SipParseResult();
        result.Status = SipParseStatus.Incomplete;
        return result;
    }

    static SipParseResult CreateError(int offset, string message)
    {
        SipParseResult result = new SipParseResult();
        result.Status = SipParseStatus.Error;
        result.Consumed = offset;
        result.ErrorOffset = offset;
        result.ErrorMessage = message;
        SipValidationIssue issue = new SipValidationIssue();
        issue.Severity = SipValidationSeverity.Error;
        issue.Code = "invalid-message";
        issue.Message = message;
        issue.ByteOffset = offset;
        issue.Path = "message";
        result.ValidationIssues = new SipValidationIssue[] { issue };
        return result;
    }
}
