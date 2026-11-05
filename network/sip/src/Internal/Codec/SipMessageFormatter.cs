namespace sip;

internal static class SipMessageFormatter
{
    internal static SipWriteResult FormatMessageModel(SipMessage? message, SipWriteOptions? options)
    {
        if (message == null) return CreateError("A SIP message is required.");
        if (options == null) return CreateError("Message write options are required.");
        if (options.MaxHeaderLineBytes < 0)
            return CreateError("The maximum header line size cannot be negative.");
        if (message.Headers == null) return CreateError("The message header collection cannot be null.");
        if (message.Body == null) return CreateError("The message body byte collection cannot be null.");
        if (message.OriginalBytes == null) return CreateError("The original message byte collection cannot be null.");

        if (options.PreserveOriginal && message.OriginalBytes.Length > 0
            && OriginalHeaderLinesFit(message.OriginalBytes, options.MaxHeaderLineBytes)
            && (options.AllowHeaderFolding || !OriginalContainsHeaderFolding(message.OriginalBytes))
            && OriginalMatchesMessage(message, options.ReconcileContentLength))
            return CreateSuccess(CopyBytes(message.OriginalBytes));

        SipWriteResult startLine = FormatStartLine(message, options);
        if (!startLine.Success) return startLine;
        System.Collections.Generic.List<byte> output = new System.Collections.Generic.List<byte>();
        AppendBytes(output, startLine.Bytes);

        bool wroteContentLength = false;
        int index = 0;
        while (index < message.Headers.Length)
        {
            SipHeader? header = message.Headers[index];
            if (header == null) return CreateError("The message contains a null header field.");
            if (options.ReconcileContentLength && IsContentLengthHeader(header.Name))
            {
                if (!wroteContentLength)
                {
                    SipWriteResult lengthHeader = FormatContentLengthHeader(message.Body.Length, options);
                    if (!lengthHeader.Success) return lengthHeader;
                    AppendBytes(output, lengthHeader.Bytes);
                    wroteContentLength = true;
                }
            }
            else
            {
                SipWriteResult formattedHeader = FormatHeaderWithPolicy(header, options);
                if (!formattedHeader.Success) return formattedHeader;
                AppendBytes(output, formattedHeader.Bytes);
            }
            index = index + 1;
        }

        if (options.ReconcileContentLength && !wroteContentLength)
        {
            SipWriteResult lengthHeader = FormatContentLengthHeader(message.Body.Length, options);
            if (!lengthHeader.Success) return lengthHeader;
            AppendBytes(output, lengthHeader.Bytes);
        }
        output.Add((byte)'\r');
        output.Add((byte)'\n');
        AppendBytes(output, message.Body);
        return CreateSuccess(output.ToArray());
    }

    static SipWriteResult FormatStartLine(SipMessage message, SipWriteOptions options)
    {
        if (!IsProtocolVersion(message.ProtocolVersion))
            return CreateError("The message protocol version is invalid.");
        string protocol = message.ProtocolVersion == null ? "" : message.ProtocolVersion;
        if (options.Canonical) protocol = "SIP/" + protocol.Substring(4);
        string text;
        if (message.Kind == SipMessageKind.Request)
        {
            string? method = GetMethodToken(message);
            if (!SipAscii.IsToken(method)) return CreateError("The request method token is invalid.");
            if (message.RequestUri == null) return CreateError("A request URI is required for a request message.");
            SipWriteResult uri = SipUriFormatter.FormatUriModel(message.RequestUri, options);
            if (!uri.Success) return CreateError(uri.ErrorMessage == null ? "The request URI is invalid." : uri.ErrorMessage);
            string uriText = System.Text.Encoding.ASCII.GetString(uri.Bytes);
            text = method + " " + uriText + " " + protocol + "\r\n";
        }
        else if (message.Kind == SipMessageKind.Response)
        {
            if (message.StatusCode < 0 || message.StatusCode > 999)
                return CreateError("The response status code must fit exactly three digits.");
            string reason = message.ReasonPhrase == null ? "" : message.ReasonPhrase;
            int invalidReason = GetInvalidReasonOffset(reason);
            if (invalidReason >= 0) return CreateError("The response reason phrase contains an invalid control character.");
            string code = message.StatusCode.ToString("D3", System.Globalization.CultureInfo.InvariantCulture);
            text = protocol + " " + code + " " + reason + "\r\n";
        }
        else
        {
            return CreateError("The message kind must identify a request or response.");
        }
        return CreateSuccess(System.Text.Encoding.UTF8.GetBytes(text));
    }

    static SipWriteResult FormatHeaderWithPolicy(SipHeader header, SipWriteOptions options)
    {
        SipWriteOptions unlimited = CopyWriteOptions(options);
        unlimited.MaxHeaderLineBytes = int.MaxValue;
        SipWriteResult formatted = SipHeaderFormatter.FormatHeaderModel(header, unlimited);
        if (!formatted.Success) return formatted;
        if (ContainsInternalLineBreak(formatted.Bytes) && !options.AllowHeaderFolding)
        {
            unlimited.PreserveOriginal = false;
            formatted = SipHeaderFormatter.FormatHeaderModel(header, unlimited);
            if (!formatted.Success) return formatted;
        }
        if (HeaderLinesFit(formatted.Bytes, options.MaxHeaderLineBytes)) return formatted;
        if (!options.AllowHeaderFolding)
        {
            SipWriteResult limited = CreateError("A formatted header field exceeds the configured output limit.");
            limited.RequiredBytes = formatted.Bytes.Length;
            return limited;
        }

        if (ContainsInternalLineBreak(formatted.Bytes))
        {
            unlimited.PreserveOriginal = false;
            formatted = SipHeaderFormatter.FormatHeaderModel(header, unlimited);
            if (!formatted.Success) return formatted;
        }
        return FoldHeaderBytes(formatted.Bytes, options.MaxHeaderLineBytes);
    }

    static SipWriteResult FormatContentLengthHeader(int bodyLength, SipWriteOptions options)
    {
        SipHeader header = new SipHeader();
        header.Name = SipHeaderNames.ContentLength;
        header.Value = bodyLength.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return FormatHeaderWithPolicy(header, options);
    }

    static SipWriteResult FoldHeaderBytes(byte[] bytes, int maximumLineBytes)
    {
        if (bytes.Length < 2 || bytes[bytes.Length - 2] != '\r' || bytes[bytes.Length - 1] != '\n')
            return CreateError("A formatted header field has an invalid line ending.");
        int contentEnd = bytes.Length - 2;
        int position = 0;
        bool continuation = false;
        System.Collections.Generic.List<byte> output = new System.Collections.Generic.List<byte>();
        while (position < contentEnd)
        {
            int prefixLength = continuation ? 1 : 0;
            int remaining = contentEnd - position;
            if (prefixLength + remaining + 2 <= maximumLineBytes)
            {
                if (continuation) output.Add((byte)' ');
                AppendByteRange(output, bytes, position, remaining);
                output.Add((byte)'\r');
                output.Add((byte)'\n');
                return CreateSuccess(output.ToArray());
            }

            int contentLimit = maximumLineBytes - prefixLength - 2;
            if (contentLimit <= 0) return CreateFoldError(bytes.Length);
            int search = position + contentLimit;
            if (search >= contentEnd) search = contentEnd - 1;
            while (search > position && bytes[search] != ' ' && bytes[search] != '\t') search = search - 1;
            if (search <= position) return CreateFoldError(bytes.Length);
            if (continuation) output.Add((byte)' ');
            AppendByteRange(output, bytes, position, search - position);
            output.Add((byte)'\r');
            output.Add((byte)'\n');
            position = search + 1;
            while (position < contentEnd && (bytes[position] == ' ' || bytes[position] == '\t'))
                position = position + 1;
            continuation = true;
        }
        return CreateFoldError(bytes.Length);
    }

    static SipWriteResult CreateFoldError(int requiredBytes)
    {
        SipWriteResult result = CreateError(
            "The header field cannot be folded within the configured output limit without changing its value.");
        result.RequiredBytes = requiredBytes;
        return result;
    }

    static bool OriginalMatchesMessage(SipMessage message, bool requireSingleContentLength)
    {
        SipParseOptions parseOptions = new SipParseOptions();
        parseOptions.AllowHeaderFolding = true;
        parseOptions.AllowCompatibleWhitespace = true;
        parseOptions.PreserveOriginal = false;
        parseOptions.Strict = false;
        parseOptions.MaxMessageBytes = int.MaxValue;
        parseOptions.MaxBodyBytes = int.MaxValue;
        parseOptions.MaxHeaderBytes = int.MaxValue;
        parseOptions.MaxStartLineBytes = int.MaxValue;
        parseOptions.MaxHeaderLineBytes = int.MaxValue;
        parseOptions.MaxHeaderCount = int.MaxValue;
        parseOptions.MaxUriBytes = int.MaxValue;
        parseOptions.MaxParameterCount = int.MaxValue;
        parseOptions.MaxListValueCount = int.MaxValue;
        SipParseResult parsed = SipMessageParser.ParseDatagramMessageBytes(message.OriginalBytes, parseOptions);
        if (parsed.Status != SipParseStatus.Complete || parsed.Message == null
            || parsed.Consumed != message.OriginalBytes.Length) return false;
        if (requireSingleContentLength && CountContentLengthHeaders(parsed.Message.Headers) != 1) return false;
        return MessageValuesMatch(message, parsed.Message);
    }

    static bool MessageValuesMatch(SipMessage left, SipMessage right)
    {
        if (left.Kind != right.Kind || left.Method != right.Method || left.MethodToken != right.MethodToken
            || left.StatusCode != right.StatusCode || left.ReasonPhrase != right.ReasonPhrase
            || left.ProtocolVersion != right.ProtocolVersion) return false;
        if (left.RequestUri == null && right.RequestUri != null
            || left.RequestUri != null && right.RequestUri == null) return false;
        if (left.RequestUri != null && !SipUriComparison.CompareUriModels(left.RequestUri, right.RequestUri))
            return false;
        if (left.Headers.Length != right.Headers.Length || !ByteArraysMatch(left.Body, right.Body)) return false;
        int index = 0;
        while (index < left.Headers.Length)
        {
            SipHeader? leftHeader = left.Headers[index];
            SipHeader? rightHeader = right.Headers[index];
            if (leftHeader == null || rightHeader == null || leftHeader.Name != rightHeader.Name
                || leftHeader.Value != rightHeader.Value) return false;
            index = index + 1;
        }
        return true;
    }

    static bool ByteArraysMatch(byte[] left, byte[] right)
    {
        if (left.Length != right.Length) return false;
        int index = 0;
        while (index < left.Length)
        {
            if (left[index] != right[index]) return false;
            index = index + 1;
        }
        return true;
    }

    static int CountContentLengthHeaders(SipHeader[] headers)
    {
        int count = 0;
        int index = 0;
        while (index < headers.Length)
        {
            SipHeader? header = headers[index];
            if (header != null && IsContentLengthHeader(header.Name)) count = count + 1;
            index = index + 1;
        }
        return count;
    }

    static bool OriginalHeaderLinesFit(byte[] bytes, int maximumLineBytes)
    {
        int lineStart = 0;
        bool startLine = true;
        int index = 0;
        while (index + 1 < bytes.Length)
        {
            if (bytes[index] == '\r' && bytes[index + 1] == '\n')
            {
                if (index == lineStart) return true;
                if (!startLine && index + 2 - lineStart > maximumLineBytes) return false;
                startLine = false;
                lineStart = index + 2;
                index = index + 2;
            }
            else
            {
                index = index + 1;
            }
        }
        return false;
    }

    static bool OriginalContainsHeaderFolding(byte[] bytes)
    {
        int lineStart = 0;
        bool startLine = true;
        int index = 0;
        while (index + 1 < bytes.Length)
        {
            if (bytes[index] == '\r' && bytes[index + 1] == '\n')
            {
                if (index == lineStart) return false;
                if (!startLine && (bytes[lineStart] == ' ' || bytes[lineStart] == '\t')) return true;
                startLine = false;
                lineStart = index + 2;
                index = index + 2;
            }
            else
            {
                index = index + 1;
            }
        }
        return false;
    }

    static bool HeaderLinesFit(byte[] bytes, int maximumLineBytes)
    {
        int lineStart = 0;
        int index = 0;
        while (index + 1 < bytes.Length)
        {
            if (bytes[index] == '\r' && bytes[index + 1] == '\n')
            {
                if (index + 2 - lineStart > maximumLineBytes) return false;
                lineStart = index + 2;
                index = index + 2;
            }
            else
            {
                index = index + 1;
            }
        }
        return lineStart == bytes.Length;
    }

    static bool ContainsInternalLineBreak(byte[] bytes)
    {
        int index = 0;
        while (index + 1 < bytes.Length - 2)
        {
            if (bytes[index] == '\r' && bytes[index + 1] == '\n') return true;
            index = index + 1;
        }
        return false;
    }

    static bool IsProtocolVersion(string? protocol)
    {
        if (protocol == null || protocol.Length < 7) return false;
        if (!SipAscii.EqualsIgnoreCase(protocol.Substring(0, 4), "SIP/")) return false;
        int index = 4;
        int majorStart = index;
        while (index < protocol.Length && protocol[index] >= '0' && protocol[index] <= '9') index = index + 1;
        if (index == majorStart || index >= protocol.Length || protocol[index] != '.') return false;
        index = index + 1;
        int minorStart = index;
        while (index < protocol.Length && protocol[index] >= '0' && protocol[index] <= '9') index = index + 1;
        return index > minorStart && index == protocol.Length;
    }

    static int GetInvalidReasonOffset(string reason)
    {
        int index = 0;
        while (index < reason.Length)
        {
            int character = reason[index];
            if (character < 32 && character != '\t' || character == 127) return index;
            index = index + 1;
        }
        return -1;
    }

    static string? GetMethodToken(SipMessage message)
    {
        switch (message.Method)
        {
            case SipMethod.Ack: return SipMethods.Ack;
            case SipMethod.Bye: return SipMethods.Bye;
            case SipMethod.Cancel: return SipMethods.Cancel;
            case SipMethod.Info: return SipMethods.Info;
            case SipMethod.Invite: return SipMethods.Invite;
            case SipMethod.Message: return SipMethods.Message;
            case SipMethod.Notify: return SipMethods.Notify;
            case SipMethod.Options: return SipMethods.Options;
            case SipMethod.Prack: return SipMethods.Prack;
            case SipMethod.Publish: return SipMethods.Publish;
            case SipMethod.Refer: return SipMethods.Refer;
            case SipMethod.Register: return SipMethods.Register;
            case SipMethod.Subscribe: return SipMethods.Subscribe;
            case SipMethod.Update: return SipMethods.Update;
            default: return message.MethodToken;
        }
    }

    static bool IsContentLengthHeader(string? name)
    {
        return SipAscii.EqualsIgnoreCase(name, SipHeaderNames.ContentLength)
            || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.CompactContentLength);
    }

    static SipWriteOptions CopyWriteOptions(SipWriteOptions options)
    {
        SipWriteOptions copy = new SipWriteOptions();
        copy.Canonical = options.Canonical;
        copy.PreserveOriginal = options.PreserveOriginal;
        copy.CompactHeaderNames = options.CompactHeaderNames;
        copy.AllowHeaderFolding = options.AllowHeaderFolding;
        copy.ReconcileContentLength = options.ReconcileContentLength;
        copy.MaxHeaderLineBytes = options.MaxHeaderLineBytes;
        return copy;
    }

    static void AppendBytes(System.Collections.Generic.List<byte> output, byte[] bytes)
    {
        int index = 0;
        while (index < bytes.Length)
        {
            output.Add(bytes[index]);
            index = index + 1;
        }
    }

    static void AppendByteRange(System.Collections.Generic.List<byte> output, byte[] bytes, int start, int count)
    {
        int index = 0;
        while (index < count)
        {
            output.Add(bytes[start + index]);
            index = index + 1;
        }
    }

    static byte[] CopyBytes(byte[] bytes)
    {
        byte[] copy = new byte[bytes.Length];
        System.Array.Copy(bytes, copy, bytes.Length);
        return copy;
    }

    static SipWriteResult CreateSuccess(byte[] bytes)
    {
        SipWriteResult result = new SipWriteResult();
        result.Success = true;
        result.Bytes = bytes;
        result.Written = bytes.Length;
        result.RequiredBytes = bytes.Length;
        return result;
    }

    static SipWriteResult CreateError(string message)
    {
        SipWriteResult result = new SipWriteResult();
        result.ErrorMessage = message;
        return result;
    }
}
