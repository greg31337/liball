using System.Collections.Generic;
using System.Text;

namespace http;

internal class Http1FramingResult
{
    internal Http1FramingDecision Decision = new Http1FramingDecision();
    internal string? ErrorCode;
    internal string? ErrorMessage;
}

internal class Http1TransferEncodingResult
{
    internal bool IsValid = true;
    internal bool IsPresent;
    internal List<string> Codings = new List<string>();
    internal List<bool> HasParameters = new List<bool>();
    internal string? ErrorCode;
    internal string? ErrorMessage;
}

internal class Http1TransferMemberResult
{
    internal bool IsValid;
    internal int Consumed;
    internal string Coding = "";
    internal bool HasParameters;
}

internal class Http1ContentLengthResult
{
    internal bool IsValid = true;
    internal bool IsPresent;
    internal long Value;
    internal string? ErrorCode;
    internal string? ErrorMessage;
}

internal static class Http1Framing
{
    internal static Http1FramingResult DetermineRequest(
        HttpRequest request,
        Http1ParserOptions options)
    {
        return DetermineFromHeaders(
            request.Headers,
            request.ProtocolVersion,
            true,
            options);
    }

    internal static Http1FramingResult DetermineResponse(
        HttpResponse response,
        Http1ParserOptions options)
    {
        if (options.ResponseRequestMethod == HttpMethods.Head ||
            response.StatusCode >= 100 && response.StatusCode <= 199 ||
            response.StatusCode == HttpStatusCodes.NoContent ||
            response.StatusCode == HttpStatusCodes.NotModified)
        {
            return CreateNoBody(
                "The response status or associated request method prohibits a message body.");
        }

        if (options.ResponseRequestMethod == HttpMethods.Connect &&
            response.StatusCode >= 200 && response.StatusCode <= 299)
        {
            return CreateTunnel();
        }

        return DetermineFromHeaders(
            response.Headers,
            response.ProtocolVersion,
            false,
            options);
    }

    static Http1FramingResult DetermineFromHeaders(
        HttpHeaders headers,
        HttpProtocolVersion protocolVersion,
        bool isRequest,
        Http1ParserOptions options)
    {
        Http1FramingResult result = new Http1FramingResult();
        Http1TransferEncodingResult transfer = ParseTransferEncoding(headers);
        if (!transfer.IsValid)
        {
            return Reject(
                result,
                transfer.ErrorCode ?? "invalid-http1-transfer-encoding",
                transfer.ErrorMessage ?? "The Transfer-Encoding field is malformed.");
        }

        if (transfer.IsPresent && protocolVersion == HttpProtocolVersion.Http10)
        {
            return Reject(
                result,
                "http1-transfer-encoding-on-http10",
                "Transfer-Encoding cannot frame an HTTP/1.0 message.");
        }

        bool hasContentLength = HasField(headers, HttpHeaderNames.ContentLength);
        if (transfer.IsPresent && hasContentLength &&
            !options.AllowTransferEncodingWithContentLength)
        {
            return Reject(
                result,
                "http1-transfer-encoding-content-length-conflict",
                "Transfer-Encoding and Content-Length cannot both frame a message.");
        }

        if (transfer.IsPresent)
        {
            int chunkedIndex = FindChunkedIndex(transfer);
            if (chunkedIndex == -2)
            {
                return Reject(
                    result,
                    "repeated-http1-chunked-transfer-coding",
                    "The chunked transfer coding cannot be applied more than once.");
            }

            if (chunkedIndex >= 0 && transfer.HasParameters[chunkedIndex])
            {
                return Reject(
                    result,
                    "invalid-http1-chunked-transfer-coding",
                    "The chunked transfer coding cannot have parameters.");
            }

            if (chunkedIndex >= 0 && chunkedIndex != transfer.Codings.Count - 1 &&
                !options.AllowChunkedNotFinal)
            {
                return Reject(
                    result,
                    "http1-chunked-transfer-coding-not-final",
                    "The chunked transfer coding must be the final transfer coding.");
            }

            CopyTransferCodings(transfer, result.Decision);
            string finalCoding = transfer.Codings[transfer.Codings.Count - 1];
            if (Ascii.EqualsStringsIgnoreCase(finalCoding, HttpTransferCodings.Chunked))
            {
                if (isRequest && HasUnsupportedRequestCoding(transfer))
                {
                    return Reject(
                        result,
                        "unsupported-http1-request-transfer-coding",
                        "The request uses a transfer coding that this parser cannot decode.");
                }

                return Accept(
                    result,
                    Http1BodyFraming.Chunked,
                    true,
                    false,
                    "The final transfer coding is chunked.");
            }

            if (isRequest)
            {
                return Reject(
                    result,
                    "http1-request-transfer-coding-not-final",
                    "A request Transfer-Encoding must end in chunked.");
            }

            return Accept(
                result,
                Http1BodyFraming.CloseDelimited,
                false,
                false,
                "A response whose final transfer coding is not chunked is delimited by closure.");
        }

        Http1ContentLengthResult contentLength = ParseContentLength(
            headers,
            options.AllowMismatchedContentLengths);
        if (!contentLength.IsValid)
        {
            return Reject(
                result,
                contentLength.ErrorCode ?? "invalid-http1-content-length",
                contentLength.ErrorMessage ??
                    "The Content-Length field is malformed or inconsistent.");
        }

        if (contentLength.IsPresent)
        {
            result.Decision.HasDeclaredLength = true;
            result.Decision.DeclaredLength = contentLength.Value;
            return Accept(
                result,
                Http1BodyFraming.ContentLength,
                true,
                false,
                "Content-Length declares the message body length.");
        }

        if (isRequest)
        {
            return CreateNoBody(
                "A request without Transfer-Encoding or Content-Length has no message body.");
        }

        return Accept(
            result,
            Http1BodyFraming.CloseDelimited,
            false,
            false,
            "A response without a declared length is delimited by connection closure.");
    }

    static Http1TransferEncodingResult ParseTransferEncoding(HttpHeaders headers)
    {
        Http1TransferEncodingResult result = new Http1TransferEncodingResult();
        int fieldIndex = 0;
        while (fieldIndex < headers.Fields.Count)
        {
            HttpHeaderField field = headers.Fields[fieldIndex];
            if (Ascii.EqualsStringsIgnoreCase(
                field.Name,
                HttpHeaderNames.TransferEncoding))
            {
                result.IsPresent = true;
                if (!AppendTransferCodings(field.Value, result))
                {
                    result.IsValid = false;
                    result.ErrorCode = "invalid-http1-transfer-encoding";
                    result.ErrorMessage = "The Transfer-Encoding field is malformed.";
                    return result;
                }
            }

            fieldIndex = fieldIndex + 1;
        }

        if (result.IsPresent && result.Codings.Count == 0)
        {
            result.IsValid = false;
            result.ErrorCode = "invalid-http1-transfer-encoding";
            result.ErrorMessage = "The Transfer-Encoding field is empty.";
        }

        return result;
    }

    static bool HasField(HttpHeaders headers, string name)
    {
        int index = 0;
        while (index < headers.Fields.Count)
        {
            if (Ascii.EqualsStringsIgnoreCase(headers.Fields[index].Name, name))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static bool AppendTransferCodings(
        byte[] value,
        Http1TransferEncodingResult transfer)
    {
        int position = 0;
        while (position < value.Length)
        {
            Http1TransferMemberResult member = ParseTransferMember(value, position);
            if (!member.IsValid)
            {
                return false;
            }

            transfer.Codings.Add(member.Coding);
            transfer.HasParameters.Add(member.HasParameters);
            position = member.Consumed;
            if (position < value.Length)
            {
                position = position + 1;
                if (position == value.Length)
                {
                    return false;
                }
            }
        }

        return value.Length != 0;
    }

    static Http1TransferMemberResult ParseTransferMember(byte[] value, int start)
    {
        Http1TransferMemberResult result = new Http1TransferMemberResult();
        int position = SkipOptionalWhitespace(value, start);
        int codingStart = position;
        position = ReadTokenEnd(value, position);
        if (position == codingStart)
        {
            return result;
        }

        result.Coding = NormalizeToken(value, codingStart, position - codingStart);
        while (position < value.Length)
        {
            position = SkipOptionalWhitespace(value, position);
            if (position == value.Length || value[position] == 0x2c)
            {
                result.IsValid = true;
                result.Consumed = position;
                return result;
            }

            if (value[position] != 0x3b)
            {
                return result;
            }

            result.HasParameters = true;
            position = SkipOptionalWhitespace(value, position + 1);
            int parameterStart = position;
            position = ReadTokenEnd(value, position);
            if (position == parameterStart)
            {
                return result;
            }

            position = SkipOptionalWhitespace(value, position);
            if (position == value.Length || value[position] != 0x3d)
            {
                return result;
            }

            position = SkipOptionalWhitespace(value, position + 1);
            if (position == value.Length)
            {
                return result;
            }

            if (value[position] == 0x22)
            {
                position = ReadQuotedStringEnd(value, position);
                if (position < 0)
                {
                    return result;
                }
            }
            else
            {
                int parameterValueStart = position;
                position = ReadTokenEnd(value, position);
                if (position == parameterValueStart)
                {
                    return result;
                }
            }
        }

        result.IsValid = true;
        result.Consumed = position;
        return result;
    }

    static Http1ContentLengthResult ParseContentLength(
        HttpHeaders headers,
        bool allowMismatch)
    {
        Http1ContentLengthResult result = new Http1ContentLengthResult();
        int fieldIndex = 0;
        while (fieldIndex < headers.Fields.Count)
        {
            HttpHeaderField field = headers.Fields[fieldIndex];
            if (Ascii.EqualsStringsIgnoreCase(field.Name, HttpHeaderNames.ContentLength))
            {
                if (!AppendContentLengths(field.Value, result, allowMismatch))
                {
                    result.IsValid = false;
                    if (result.ErrorCode == null)
                    {
                        result.ErrorCode = "invalid-http1-content-length";
                        result.ErrorMessage = "The Content-Length field is malformed.";
                    }

                    return result;
                }
            }

            fieldIndex = fieldIndex + 1;
        }

        return result;
    }

    static bool AppendContentLengths(
        byte[] value,
        Http1ContentLengthResult result,
        bool allowMismatch)
    {
        int position = 0;
        while (position < value.Length)
        {
            while (position < value.Length && IsOptionalWhitespace(value[position]))
            {
                position = position + 1;
            }

            int numberStart = position;
            while (position < value.Length && value[position] >= 0x30 &&
                value[position] <= 0x39)
            {
                position = position + 1;
            }

            int numberEnd = position;
            while (position < value.Length && IsOptionalWhitespace(value[position]))
            {
                position = position + 1;
            }

            if (numberEnd == numberStart ||
                position < value.Length && value[position] != 0x2c)
            {
                return false;
            }

            IntegerParseResult parsed = IntegerParser.ParseDecimal(
                new System.ReadOnlySpan<byte>(value, numberStart, numberEnd - numberStart));
            if (!parsed.IsValid || parsed.Overflow)
            {
                result.ErrorCode = "http1-content-length-overflow";
                result.ErrorMessage = "The Content-Length value exceeds the supported range.";
                return false;
            }

            if (result.IsPresent && result.Value != parsed.Value && !allowMismatch)
            {
                result.ErrorCode = "mismatched-http1-content-length";
                result.ErrorMessage = "Content-Length values do not agree.";
                return false;
            }

            if (!result.IsPresent)
            {
                result.IsPresent = true;
                result.Value = parsed.Value;
            }
            if (position < value.Length)
            {
                position = position + 1;
                if (position == value.Length)
                {
                    return false;
                }
            }
        }

        return result.IsPresent;
    }

    static int FindChunkedIndex(Http1TransferEncodingResult transfer)
    {
        int found = -1;
        int index = 0;
        while (index < transfer.Codings.Count)
        {
            if (Ascii.EqualsStringsIgnoreCase(
                transfer.Codings[index],
                HttpTransferCodings.Chunked))
            {
                if (found >= 0)
                {
                    return -2;
                }

                found = index;
            }

            index = index + 1;
        }

        return found;
    }

    static bool HasUnsupportedRequestCoding(Http1TransferEncodingResult transfer)
    {
        int index = 0;
        while (index < transfer.Codings.Count)
        {
            if (!Ascii.EqualsStringsIgnoreCase(
                transfer.Codings[index],
                HttpTransferCodings.Chunked))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static int SkipOptionalWhitespace(byte[] value, int start)
    {
        int position = start;
        while (position < value.Length && IsOptionalWhitespace(value[position]))
        {
            position = position + 1;
        }

        return position;
    }

    static int ReadTokenEnd(byte[] value, int start)
    {
        int position = start;
        while (position < value.Length && HttpGrammar.IsTokenCharacter(value[position]))
        {
            position = position + 1;
        }

        return position;
    }

    static int ReadQuotedStringEnd(byte[] value, int start)
    {
        int position = start + 1;
        while (position < value.Length)
        {
            byte current = value[position];
            if (current == 0x22)
            {
                return position + 1;
            }

            if (current == 0x5c)
            {
                position = position + 1;
                if (position == value.Length || !IsQuotedPairCharacter(value[position]))
                {
                    return -1;
                }
            }
            else if (!IsQuotedText(current))
            {
                return -1;
            }

            position = position + 1;
        }

        return -1;
    }

    static string NormalizeToken(byte[] value, int start, int count)
    {
        byte[] token = new byte[count];
        int index = 0;
        while (index < count)
        {
            token[index] = Ascii.FoldByteToLower(value[start + index]);
            index = index + 1;
        }

        return Encoding.ASCII.GetString(token);
    }

    static bool IsQuotedText(byte value)
    {
        return value == 0x09 || value == 0x20 || value == 0x21 ||
            value >= 0x23 && value <= 0x5b ||
            value >= 0x5d && value <= 0x7e || value >= 0x80;
    }

    static bool IsQuotedPairCharacter(byte value)
    {
        return value == 0x09 || value == 0x20 ||
            value >= 0x21 && value <= 0x7e || value >= 0x80;
    }

    static void CopyTransferCodings(
        Http1TransferEncodingResult source,
        Http1FramingDecision destination)
    {
        int index = 0;
        while (index < source.Codings.Count)
        {
            destination.TransferCodings.Add(source.Codings[index]);
            index = index + 1;
        }
    }

    static Http1FramingResult CreateNoBody(string reason)
    {
        Http1FramingResult result = new Http1FramingResult();
        result.Decision.HasNoBody = true;
        return Accept(result, Http1BodyFraming.None, true, false, reason);
    }

    static Http1FramingResult CreateTunnel()
    {
        Http1FramingResult result = new Http1FramingResult();
        result.Decision.IsTunnel = true;
        return Accept(
            result,
            Http1BodyFraming.Tunnel,
            false,
            true,
            "A successful CONNECT response switches the connection to a tunnel.");
    }

    static Http1FramingResult Accept(
        Http1FramingResult result,
        Http1BodyFraming framing,
        bool selfDelimited,
        bool tunnel,
        string reason)
    {
        result.Decision.BodyFraming = framing;
        result.Decision.IsValid = true;
        result.Decision.IsSelfDelimited = selfDelimited;
        result.Decision.IsTunnel = tunnel;
        result.Decision.Reason = reason;
        return result;
    }

    static Http1FramingResult Reject(
        Http1FramingResult result,
        string code,
        string message)
    {
        result.Decision.BodyFraming = Http1BodyFraming.Invalid;
        result.Decision.IsValid = false;
        result.Decision.Reason = message;
        result.ErrorCode = code;
        result.ErrorMessage = message;
        return result;
    }

    static bool IsOptionalWhitespace(byte value)
    {
        return value == 0x20 || value == 0x09;
    }
}
