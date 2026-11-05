using System;
using System.Collections.Generic;

namespace websocket;

internal class Http1ResponseParser
{
    int maximumHeaderBytes;
    int maximumHeaderCount;
    int maximumLineLength;
    byte[] lineBytes;
    int lineByteCount;
    bool hasCarriageReturn;
    int totalByteCount;
    bool statusLineComplete;
    int statusCode;
    bool hasReasonPhrase;
    string reasonPhrase = "";
    List<WebSocketHeaderField> fields = new List<WebSocketHeaderField>();
    bool responseAvailable;
    bool finalResponse;
    WebSocketHttpResponse response = new WebSocketHttpResponse();
    bool failed;
    WebSocketParseStatus failureStatus = WebSocketParseStatus.ProtocolFailure;
    WebSocketError failureError = new WebSocketError();

    internal Http1ResponseParser(
        int maximumHeaderBytes,
        int maximumHeaderCount,
        int maximumLineLength)
    {
        if (maximumHeaderBytes <= 0 ||
            maximumHeaderCount <= 0 ||
            maximumLineLength <= 0 ||
            maximumLineLength > maximumHeaderBytes)
        {
            throw new WebSocketLimitException();
        }

        this.maximumHeaderBytes = maximumHeaderBytes;
        this.maximumHeaderCount = maximumHeaderCount;
        this.maximumLineLength = maximumLineLength;
        this.lineBytes = new byte[maximumLineLength];
    }

    internal WebSocketHttp1ReadResult ReadResponse(ReadOnlySpan<byte> source)
    {
        if (this.failed)
        {
            return CreateFailureResult(0);
        }

        if (this.responseAvailable)
        {
            if (this.finalResponse)
            {
                return CreateCompleteResult(0);
            }

            PrepareNextResponse();
        }

        int sourceIndex = 0;
        while (sourceIndex < source.Length && !this.failed && !this.responseAvailable)
        {
            byte current = source[sourceIndex];
            sourceIndex = sourceIndex + 1;
            this.totalByteCount = this.totalByteCount + 1;
            if (this.totalByteCount > this.maximumHeaderBytes)
            {
                SetLimitFailure(
                    "http-header-bytes-too-large",
                    "The HTTP response header exceeds the configured byte limit.");
            }
            else if (this.hasCarriageReturn)
            {
                if (current != 10)
                {
                    SetProtocolFailure(
                        "http-line-ending-invalid",
                        "HTTP response lines must end with CRLF.");
                }
                else
                {
                    this.hasCarriageReturn = false;
                    ProcessCompletedLine();
                }
            }
            else if (current == 13)
            {
                this.hasCarriageReturn = true;
            }
            else if (current == 10)
            {
                SetProtocolFailure(
                    "http-line-ending-invalid",
                    "HTTP response lines must end with CRLF.");
            }
            else if (this.lineByteCount == this.maximumLineLength)
            {
                SetLimitFailure(
                    "http-header-line-too-long",
                    "An HTTP response line exceeds the configured line limit.");
            }
            else
            {
                this.lineBytes[this.lineByteCount] = current;
                this.lineByteCount = this.lineByteCount + 1;
            }

            if (!this.failed &&
                !this.responseAvailable &&
                this.totalByteCount == this.maximumHeaderBytes)
            {
                SetLimitFailure(
                    "http-header-bytes-too-large",
                    "The HTTP response header exceeds the configured byte limit.");
            }
        }

        if (this.failed)
        {
            return CreateFailureResult(sourceIndex);
        }

        if (this.responseAvailable)
        {
            return CreateCompleteResult(sourceIndex);
        }

        WebSocketHttp1ReadResult result = new WebSocketHttp1ReadResult();
        result.Status = WebSocketParseStatus.NeedInput;
        result.ConsumedCount = sourceIndex;
        return result;
    }

    internal void ResetResponse()
    {
        ClearPartialLine();
        this.hasCarriageReturn = false;
        this.totalByteCount = 0;
        this.statusLineComplete = false;
        this.statusCode = 0;
        this.hasReasonPhrase = false;
        this.reasonPhrase = "";
        this.fields.Clear();
        this.responseAvailable = false;
        this.finalResponse = false;
        this.response = new WebSocketHttpResponse();
        this.failed = false;
        this.failureStatus = WebSocketParseStatus.ProtocolFailure;
        this.failureError = new WebSocketError();
    }

    void PrepareNextResponse()
    {
        this.totalByteCount = 0;
        this.statusLineComplete = false;
        this.statusCode = 0;
        this.hasReasonPhrase = false;
        this.reasonPhrase = "";
        this.fields.Clear();
        this.responseAvailable = false;
        this.finalResponse = false;
        this.response = new WebSocketHttpResponse();
    }

    void ProcessCompletedLine()
    {
        ReadOnlySpan<byte> line = this.lineBytes.AsSpan(0, this.lineByteCount);
        if (!this.statusLineComplete)
        {
            ParseStatusLine(line);
            if (!this.failed)
            {
                this.statusLineComplete = true;
            }
        }
        else if (line.Length == 0)
        {
            CompleteResponse();
        }
        else
        {
            ParseHeaderLine(line);
        }

        ClearPartialLine();
    }

    void ParseStatusLine(ReadOnlySpan<byte> line)
    {
        if (line.Length < 13 ||
            !IsHttpVersion(line.Slice(0, 8)) ||
            line[8] != 32 ||
            line[12] != 32 ||
            line[9] < 48 || line[9] > 57 ||
            line[10] < 48 || line[10] > 57 ||
            line[11] < 48 || line[11] > 57)
        {
            SetProtocolFailure(
                "http-response-status-line-invalid",
                "The HTTP response status line is malformed.");
            return;
        }

        int parsedStatus =
            (line[9] - 48) * 100 +
            (line[10] - 48) * 10 +
            line[11] - 48;
        if (parsedStatus < 100 || parsedStatus > 599)
        {
            SetProtocolFailure(
                "http-response-status-line-invalid",
                "The HTTP response status code is outside the valid range.");
            return;
        }

        ReadOnlySpan<byte> reasonBytes = line.Slice(13);
        if (!IsSafeResponseText(reasonBytes))
        {
            SetProtocolFailure(
                "http-response-reason-invalid",
                "The HTTP response reason phrase contains an unsafe control character.");
            return;
        }

        this.statusCode = parsedStatus;
        if (reasonBytes.Length != 0)
        {
            this.hasReasonPhrase = true;
            this.reasonPhrase = DecodeLatin1(reasonBytes);
        }
    }

    void ParseHeaderLine(ReadOnlySpan<byte> line)
    {
        if (line[0] == 32 || line[0] == 9)
        {
            SetProtocolFailure(
                "http-header-obs-fold",
                "Obsolete HTTP header line folding is not accepted.");
            return;
        }

        int colonIndex = FindByte(line, 58, 0);
        if (colonIndex <= 0 || !IsAsciiToken(line.Slice(0, colonIndex)))
        {
            SetProtocolFailure(
                "http-header-name-invalid",
                "An HTTP response field name is invalid.");
            return;
        }

        int valueStart = colonIndex + 1;
        while (valueStart < line.Length &&
               (line[valueStart] == 32 || line[valueStart] == 9))
        {
            valueStart = valueStart + 1;
        }

        int valueEnd = line.Length;
        while (valueEnd > valueStart &&
               (line[valueEnd - 1] == 32 || line[valueEnd - 1] == 9))
        {
            valueEnd = valueEnd - 1;
        }

        ReadOnlySpan<byte> valueBytes = line.Slice(valueStart, valueEnd - valueStart);
        if (!IsSafeResponseText(valueBytes))
        {
            SetProtocolFailure(
                "http-header-value-invalid",
                "An HTTP response field value contains an unsafe control character.");
            return;
        }

        if (this.fields.Count == this.maximumHeaderCount)
        {
            SetLimitFailure(
                "http-header-count-too-large",
                "The HTTP response has too many fields.");
            return;
        }

        WebSocketHeaderField field = new WebSocketHeaderField();
        field.Name = DecodeAscii(line.Slice(0, colonIndex));
        field.Value = DecodeLatin1(valueBytes);
        this.fields.Add(field);
    }

    void CompleteResponse()
    {
        WebSocketHttpResponse completed = new WebSocketHttpResponse();
        completed.Transport = WebSocketHandshakeTransport.Http1Upgrade;
        completed.StatusCode = this.statusCode;
        completed.HasReasonPhrase = this.hasReasonPhrase;
        completed.ReasonPhrase = this.reasonPhrase;
        completed.Fields = this.fields.ToArray();
        this.response = completed;
        this.responseAvailable = true;
        this.finalResponse = this.statusCode == 101 || this.statusCode >= 200;
    }

    void ClearPartialLine()
    {
        int lineIndex = 0;
        while (lineIndex < this.lineByteCount)
        {
            this.lineBytes[lineIndex] = 0;
            lineIndex = lineIndex + 1;
        }

        this.lineByteCount = 0;
    }

    void SetProtocolFailure(string identifier, string diagnostic)
    {
        this.failed = true;
        this.failureStatus = WebSocketParseStatus.ProtocolFailure;
        this.failureError.Kind = WebSocketErrorKind.Handshake;
        this.failureError.HasHttpStatusCode = true;
        this.failureError.HttpStatusCode = 502;
        this.failureError.Identifier = identifier;
        this.failureError.Diagnostic = diagnostic;
    }

    void SetLimitFailure(string identifier, string diagnostic)
    {
        this.failed = true;
        this.failureStatus = WebSocketParseStatus.LimitRejected;
        this.failureError.Kind = WebSocketErrorKind.Limit;
        this.failureError.HasHttpStatusCode = true;
        this.failureError.HttpStatusCode = 431;
        this.failureError.Identifier = identifier;
        this.failureError.Diagnostic = diagnostic;
    }

    WebSocketHttp1ReadResult CreateCompleteResult(int consumedCount)
    {
        WebSocketHttp1ReadResult result = new WebSocketHttp1ReadResult();
        result.Status = WebSocketParseStatus.Complete;
        result.ConsumedCount = consumedCount;
        result.HasResponse = true;
        result.Response = CopyResponse();
        return result;
    }

    WebSocketHttp1ReadResult CreateFailureResult(int consumedCount)
    {
        WebSocketHttp1ReadResult result = new WebSocketHttp1ReadResult();
        result.Status = this.failureStatus;
        result.ConsumedCount = consumedCount;
        result.HasError = true;
        result.Error = CopyError();
        return result;
    }

    WebSocketHttpResponse CopyResponse()
    {
        WebSocketHttpResponse copy = new WebSocketHttpResponse();
        copy.Transport = this.response.Transport;
        copy.StatusCode = this.response.StatusCode;
        copy.HasReasonPhrase = this.response.HasReasonPhrase;
        copy.ReasonPhrase = this.response.ReasonPhrase;
        WebSocketHeaderField[] fieldsCopy = new WebSocketHeaderField[this.response.Fields.Length];
        int fieldIndex = 0;
        while (fieldIndex < fieldsCopy.Length)
        {
            WebSocketHeaderField fieldCopy = new WebSocketHeaderField();
            fieldCopy.Name = this.response.Fields[fieldIndex].Name;
            fieldCopy.Value = this.response.Fields[fieldIndex].Value;
            fieldsCopy[fieldIndex] = fieldCopy;
            fieldIndex = fieldIndex + 1;
        }

        copy.Fields = fieldsCopy;
        return copy;
    }

    WebSocketError CopyError()
    {
        WebSocketError copy = new WebSocketError();
        copy.Kind = this.failureError.Kind;
        copy.HasHttpStatusCode = this.failureError.HasHttpStatusCode;
        copy.HttpStatusCode = this.failureError.HttpStatusCode;
        copy.Identifier = this.failureError.Identifier;
        copy.Diagnostic = this.failureError.Diagnostic;
        return copy;
    }

    static int FindByte(ReadOnlySpan<byte> source, byte value, int offset)
    {
        int index = offset;
        while (index < source.Length)
        {
            if (source[index] == value)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    static bool IsAsciiToken(ReadOnlySpan<byte> value)
    {
        if (value.Length == 0)
        {
            return false;
        }

        int index = 0;
        while (index < value.Length)
        {
            if (value[index] > 127 || !AsciiToken.IsCharacter((char)value[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static bool IsHttpVersion(ReadOnlySpan<byte> value)
    {
        return value.Length == 8 &&
            value[0] == 72 &&
            value[1] == 84 &&
            value[2] == 84 &&
            value[3] == 80 &&
            value[4] == 47 &&
            value[5] >= 48 && value[5] <= 57 &&
            value[6] == 46 &&
            value[7] >= 48 && value[7] <= 57;
    }

    static bool IsSafeResponseText(ReadOnlySpan<byte> value)
    {
        int index = 0;
        while (index < value.Length)
        {
            byte current = value[index];
            if (current != 9 && (current < 32 || current == 127))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static string DecodeAscii(ReadOnlySpan<byte> value)
    {
        char[] text = new char[value.Length];
        int index = 0;
        while (index < value.Length)
        {
            text[index] = (char)value[index];
            index = index + 1;
        }

        return new string(text);
    }

    static string DecodeLatin1(ReadOnlySpan<byte> value)
    {
        char[] text = new char[value.Length];
        int index = 0;
        while (index < value.Length)
        {
            text[index] = (char)value[index];
            index = index + 1;
        }

        return new string(text);
    }
}
