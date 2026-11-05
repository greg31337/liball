using System;
using System.Collections.Generic;

namespace websocket;

internal class Http1RequestParser
{
    int maximumHeaderBytes;
    int maximumHeaderCount;
    int maximumLineLength;
    byte[] lineBytes;
    int lineByteCount;
    bool hasCarriageReturn;
    int totalByteCount;
    bool requestLineComplete;
    string method = "";
    string requestTarget = "";
    string httpVersion = "";
    List<WebSocketHeaderField> fields = new List<WebSocketHeaderField>();
    bool complete;
    WebSocketHttpRequest request = new WebSocketHttpRequest();
    bool failed;
    WebSocketParseStatus failureStatus = WebSocketParseStatus.ProtocolFailure;
    WebSocketError failureError = new WebSocketError();

    internal Http1RequestParser(
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

    internal WebSocketHttp1ReadResult ReadRequest(ReadOnlySpan<byte> source)
    {
        if (this.failed)
        {
            return CreateFailureResult(0);
        }

        if (this.complete)
        {
            return CreateCompleteResult(0);
        }

        int sourceIndex = 0;
        while (sourceIndex < source.Length && !this.failed && !this.complete)
        {
            byte current = source[sourceIndex];
            sourceIndex = sourceIndex + 1;
            this.totalByteCount = this.totalByteCount + 1;
            if (this.totalByteCount > this.maximumHeaderBytes)
            {
                SetLimitFailure(
                    "http-header-bytes-too-large",
                    "The HTTP request header exceeds the configured byte limit.");
            }
            else if (this.hasCarriageReturn)
            {
                if (current != 10)
                {
                    SetProtocolFailure(
                        "http-line-ending-invalid",
                        "HTTP request lines must end with CRLF.");
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
                    "HTTP request lines must end with CRLF.");
            }
            else if (this.lineByteCount == this.maximumLineLength)
            {
                SetLimitFailure(
                    "http-header-line-too-long",
                    "An HTTP request line exceeds the configured line limit.");
            }
            else
            {
                this.lineBytes[this.lineByteCount] = current;
                this.lineByteCount = this.lineByteCount + 1;
            }

            if (!this.failed &&
                !this.complete &&
                this.totalByteCount == this.maximumHeaderBytes)
            {
                SetLimitFailure(
                    "http-header-bytes-too-large",
                    "The HTTP request header exceeds the configured byte limit.");
            }
        }

        if (this.failed)
        {
            return CreateFailureResult(sourceIndex);
        }

        if (this.complete)
        {
            return CreateCompleteResult(sourceIndex);
        }

        WebSocketHttp1ReadResult result = new WebSocketHttp1ReadResult();
        result.Status = WebSocketParseStatus.NeedInput;
        result.ConsumedCount = sourceIndex;
        return result;
    }

    internal void ResetRequest()
    {
        int lineIndex = 0;
        while (lineIndex < this.lineByteCount)
        {
            this.lineBytes[lineIndex] = 0;
            lineIndex = lineIndex + 1;
        }

        this.lineByteCount = 0;
        this.hasCarriageReturn = false;
        this.totalByteCount = 0;
        this.requestLineComplete = false;
        this.method = "";
        this.requestTarget = "";
        this.httpVersion = "";
        this.fields.Clear();
        this.complete = false;
        this.request = new WebSocketHttpRequest();
        this.failed = false;
        this.failureStatus = WebSocketParseStatus.ProtocolFailure;
        this.failureError = new WebSocketError();
    }

    void ProcessCompletedLine()
    {
        ReadOnlySpan<byte> line = this.lineBytes.AsSpan(0, this.lineByteCount);
        if (!this.requestLineComplete)
        {
            ParseRequestLine(line);
            if (!this.failed)
            {
                this.requestLineComplete = true;
            }
        }
        else if (line.Length == 0)
        {
            CompleteRequest();
        }
        else
        {
            ParseHeaderLine(line);
        }

        ClearCompletedLine();
    }

    void ParseRequestLine(ReadOnlySpan<byte> line)
    {
        int firstSpace = FindByte(line, 32, 0);
        if (firstSpace <= 0)
        {
            SetProtocolFailure(
                "http-request-line-invalid",
                "The HTTP request line is malformed.");
            return;
        }

        int secondSpace = FindByte(line, 32, firstSpace + 1);
        if (secondSpace <= firstSpace + 1 || secondSpace == line.Length - 1)
        {
            SetProtocolFailure(
                "http-request-line-invalid",
                "The HTTP request line is malformed.");
            return;
        }

        if (FindByte(line, 32, secondSpace + 1) >= 0)
        {
            SetProtocolFailure(
                "http-request-line-invalid",
                "The HTTP request line is malformed.");
            return;
        }

        ReadOnlySpan<byte> methodBytes = line.Slice(0, firstSpace);
        ReadOnlySpan<byte> targetBytes = line.Slice(
            firstSpace + 1,
            secondSpace - firstSpace - 1);
        ReadOnlySpan<byte> versionBytes = line.Slice(secondSpace + 1);
        if (!IsAsciiToken(methodBytes) || !IsHttpVersion(versionBytes))
        {
            SetProtocolFailure(
                "http-request-line-invalid",
                "The HTTP request line is malformed.");
            return;
        }

        if (!IsOriginFormTarget(targetBytes))
        {
            SetProtocolFailure(
                "http-request-target-invalid",
                "The HTTP request target must use valid origin-form syntax.");
            return;
        }

        this.method = DecodeAscii(methodBytes);
        this.requestTarget = DecodeAscii(targetBytes);
        this.httpVersion = DecodeAscii(versionBytes);
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
                "An HTTP request field name is invalid.");
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
        if (!IsSafeFieldValue(valueBytes))
        {
            SetProtocolFailure(
                "http-header-value-invalid",
                "An HTTP request field value contains an unsafe control character.");
            return;
        }

        if (this.fields.Count == this.maximumHeaderCount)
        {
            SetLimitFailure(
                "http-header-count-too-large",
                "The HTTP request has too many fields.");
            return;
        }

        WebSocketHeaderField field = new WebSocketHeaderField();
        field.Name = DecodeAscii(line.Slice(0, colonIndex));
        field.Value = DecodeLatin1(valueBytes);
        this.fields.Add(field);
    }

    void CompleteRequest()
    {
        WebSocketHttpRequest completed = new WebSocketHttpRequest();
        completed.Transport = WebSocketHandshakeTransport.Http1Upgrade;
        completed.Method = this.method;
        completed.HasRequestTarget = true;
        completed.RequestTarget = this.requestTarget;
        completed.HasHttpVersion = true;
        completed.HttpVersion = this.httpVersion;
        completed.Fields = this.fields.ToArray();
        this.request = completed;
        this.complete = true;
    }

    void ClearCompletedLine()
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
        this.failureError.HttpStatusCode = 400;
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
        result.HasRequest = true;
        result.Request = CopyRequest();
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

    WebSocketHttpRequest CopyRequest()
    {
        WebSocketHttpRequest copy = new WebSocketHttpRequest();
        copy.Transport = this.request.Transport;
        copy.Method = this.request.Method;
        copy.HasRequestTarget = this.request.HasRequestTarget;
        copy.RequestTarget = this.request.RequestTarget;
        copy.HasHttpVersion = this.request.HasHttpVersion;
        copy.HttpVersion = this.request.HttpVersion;
        WebSocketHeaderField[] fieldsCopy = new WebSocketHeaderField[this.request.Fields.Length];
        int fieldIndex = 0;
        while (fieldIndex < fieldsCopy.Length)
        {
            WebSocketHeaderField fieldCopy = new WebSocketHeaderField();
            fieldCopy.Name = this.request.Fields[fieldIndex].Name;
            fieldCopy.Value = this.request.Fields[fieldIndex].Value;
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

    static bool IsOriginFormTarget(ReadOnlySpan<byte> value)
    {
        if (value.Length == 0 || value[0] != 47)
        {
            return false;
        }

        int index = 1;
        while (index < value.Length)
        {
            byte current = value[index];
            if (current == 63)
            {
                index = index + 1;
            }
            else if (current == 47)
            {
                index = index + 1;
            }
            else if (current == 37)
            {
                if (index > value.Length - 3 ||
                    !IsHexDigit(value[index + 1]) ||
                    !IsHexDigit(value[index + 2]))
                {
                    return false;
                }

                index = index + 3;
            }
            else if (IsPathCharacter(current))
            {
                index = index + 1;
            }
            else
            {
                return false;
            }
        }

        return true;
    }

    static bool IsPathCharacter(byte value)
    {
        if ((value >= 48 && value <= 57) ||
            (value >= 65 && value <= 90) ||
            (value >= 97 && value <= 122))
        {
            return true;
        }

        return value == 33 ||
            value == 36 ||
            value == 38 ||
            value == 39 ||
            value == 40 ||
            value == 41 ||
            value == 42 ||
            value == 43 ||
            value == 44 ||
            value == 45 ||
            value == 46 ||
            value == 58 ||
            value == 59 ||
            value == 61 ||
            value == 64 ||
            value == 95 ||
            value == 126;
    }

    static bool IsHexDigit(byte value)
    {
        return (value >= 48 && value <= 57) ||
            (value >= 65 && value <= 70) ||
            (value >= 97 && value <= 102);
    }

    static bool IsSafeFieldValue(ReadOnlySpan<byte> value)
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
