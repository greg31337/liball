namespace sip;

internal class SipReliableConnectionCore
{
    SipConnectionInfo connection;
    SipTransportOptions transportOptions;
    SipParseOptions parseOptions;
    ISipMonotonicClock clock;
    System.Collections.Generic.List<byte> readBuffer =
        new System.Collections.Generic.List<byte>();
    System.Collections.Generic.List<SipReliableWrite> writes =
        new System.Collections.Generic.List<SipReliableWrite>();
    System.Collections.Generic.List<SipTransportEvent> events =
        new System.Collections.Generic.List<SipTransportEvent>();
    long lastActivityMilliseconds;
    long keepAliveResponseDeadlineMilliseconds = -1;
    bool awaitingKeepAliveResponse;
    bool closed;

    internal SipReliableConnectionCore(SipConnectionInfo connectionInfo,
        SipTransportOptions options, SipParseOptions parsingOptions,
        ISipMonotonicClock monotonicClock)
    {
        if (!ValidateInputs(connectionInfo, options, parsingOptions, monotonicClock))
        {
            throw new System.ArgumentException(
                "The reliable connection core options are invalid.", "connectionInfo");
        }
        connection = CopyConnection(connectionInfo);
        connection.State = SipConnectionState.Connected;
        transportOptions = CopyTransportOptions(options);
        parseOptions = CopyParseOptions(parsingOptions);
        clock = monotonicClock;
        lastActivityMilliseconds = clock.GetMilliseconds();
        SipTransportEvent connected = CreateEvent(SipTransportEventKind.Connected, null);
        connected.Connection = CopyConnection(connection);
        AddEvent(connected);
    }

    internal SipReliableOperationResult ReceiveBytes(byte[]? source, int offset, int count)
    {
        SipReliableOperationResult result = new SipReliableOperationResult();
        if (closed || connection.State != SipConnectionState.Connected)
        {
            result.ErrorMessage = "The reliable connection is not open.";
            return result;
        }
        if (source == null || offset < 0 || count < 0 || offset > source.Length - count)
        {
            result.ErrorMessage = "The received byte range is invalid.";
            return result;
        }
        if (events.Count >= transportOptions.MaxQueueCount)
        {
            result.Backpressured = true;
            result.ErrorMessage = "The reliable connection message queue is full.";
            return result;
        }
        if (count > parseOptions.MaxMessageBytes
            || (long)readBuffer.Count + count > (long)parseOptions.MaxMessageBytes * 2)
        {
            SipParseResult parsed = CreateParseError(
                "The reliable stream receive buffer exceeds its bounded size.",
                readBuffer.Count, readBuffer.Count + count);
            QueueReceivedEvent(CopyBuffer(), parsed);
            connection.State = SipConnectionState.Failed;
            result.ErrorMessage = parsed.ErrorMessage;
            return result;
        }
        int index = 0;
        while (index < count)
        {
            readBuffer.Add(source[offset + index]);
            index = index + 1;
        }
        result.Success = true;
        result.BytesAccepted = count;
        if (count > 0)
        {
            lastActivityMilliseconds = clock.GetMilliseconds();
        }
        ParseBufferedMessages();
        if (connection.State == SipConnectionState.Failed)
        {
            result.Success = false;
            result.ErrorMessage = "The reliable stream contains an invalid SIP message.";
        }
        result.Backpressured = events.Count >= transportOptions.MaxQueueCount
            && readBuffer.Count > 0;
        return result;
    }

    internal SipReliableOperationResult ReceiveMessage(byte[]? source)
    {
        SipReliableOperationResult result = new SipReliableOperationResult();
        if (closed || connection.State != SipConnectionState.Connected)
        {
            result.ErrorMessage = "The reliable connection is not open.";
            return result;
        }
        if (source == null || source.Length == 0)
        {
            result.ErrorMessage = "A non-empty framed SIP message is required.";
            return result;
        }
        if (events.Count >= transportOptions.MaxQueueCount)
        {
            result.Backpressured = true;
            result.ErrorMessage = "The reliable connection message queue is full.";
            return result;
        }
        SipParseResult parsed = source.Length > parseOptions.MaxMessageBytes
            ? CreateParseError("The framed SIP message exceeds its bounded size.",
                parseOptions.MaxMessageBytes, source.Length)
            : SipMessageParser.ParseDatagramMessageBytes(source, parseOptions);
        if (parsed.Status == SipParseStatus.Incomplete)
        {
            parsed = CreateParseError(
                "A framed transport message must be one complete SIP message.",
                parsed.ErrorOffset, parsed.RequiredBytes);
        }
        if (parsed.Status == SipParseStatus.Complete
            && parsed.Consumed != source.Length)
        {
            parsed = CreateParseError(
                "A framed transport message must contain exactly one SIP message.",
                parsed.Consumed, source.Length);
        }
        QueueReceivedEvent(CopyBytes(source, 0, source.Length), parsed);
        lastActivityMilliseconds = clock.GetMilliseconds();
        result.BytesAccepted = source.Length;
        result.Success = parsed.Status == SipParseStatus.Complete;
        if (!result.Success)
        {
            connection.State = SipConnectionState.Failed;
            result.ErrorMessage = parsed.ErrorMessage == null
                ? "The framed SIP message is invalid." : parsed.ErrorMessage;
        }
        return result;
    }

    internal SipReliableOperationResult QueueWrite(SipSendRequest? request, byte[]? bytes)
    {
        SipReliableOperationResult result = new SipReliableOperationResult();
        if (closed || connection.State != SipConnectionState.Connected)
        {
            result.ErrorMessage = "The reliable connection is not open.";
            return result;
        }
        if (request == null || bytes == null || bytes.Length == 0)
        {
            result.ErrorMessage = "A non-empty reliable transport write is required.";
            return result;
        }
        if (bytes.Length > parseOptions.MaxMessageBytes)
        {
            result.ErrorMessage = "The reliable transport write exceeds the message size limit.";
            return result;
        }
        if (!RequestMatchesConnection(request))
        {
            result.ErrorMessage =
                "The send request does not match the reliable connection identity.";
            return result;
        }
        if (IsCancelled(request.Cancellation))
        {
            result.ErrorMessage = "The reliable transport write was cancelled.";
            QueueSendFailure(request.CorrelationId, result.ErrorMessage);
            return result;
        }
        if (DeadlineExpired(request.DeadlineMilliseconds))
        {
            result.ErrorMessage = "The reliable transport write deadline has expired.";
            QueueSendFailure(request.CorrelationId, result.ErrorMessage);
            return result;
        }
        if (writes.Count >= transportOptions.MaxQueueCount)
        {
            result.Backpressured = true;
            result.ErrorMessage = "The reliable connection write queue is full.";
            return result;
        }
        SipReliableWrite write = new SipReliableWrite();
        write.Bytes = CopyBytes(bytes, 0, bytes.Length);
        write.DeadlineMilliseconds = request.DeadlineMilliseconds;
        write.Cancellation = request.Cancellation;
        write.CorrelationId = request.CorrelationId;
        write.Envelope = CreateSentEnvelope(request, write.Bytes);
        writes.Add(write);
        result.Success = true;
        result.BytesAccepted = bytes.Length;
        return result;
    }

    internal SipReliableWriteChunk PollWrite(int maximumBytes)
    {
        SipReliableWriteChunk result = new SipReliableWriteChunk();
        if (maximumBytes <= 0 || closed
            || connection.State != SipConnectionState.Connected)
        {
            return result;
        }
        RemoveUnavailableWrites();
        if (writes.Count == 0 && KeepAliveDue()
            && !awaitingKeepAliveResponse)
        {
            SipReliableWrite keepAlive = new SipReliableWrite();
            keepAlive.Bytes = new byte[] { 13, 10, 13, 10 };
            keepAlive.KeepAlive = true;
            writes.Add(keepAlive);
        }
        if (writes.Count == 0)
        {
            return result;
        }
        SipReliableWrite write = writes[0];
        int remaining = write.Bytes.Length - write.Offset;
        int count = remaining < maximumBytes ? remaining : maximumBytes;
        write.OfferedCount = count;
        result.Available = true;
        result.Bytes = write.Bytes;
        result.Offset = write.Offset;
        result.Count = count;
        result.KeepAlive = write.KeepAlive;
        result.CorrelationId = write.CorrelationId;
        return result;
    }

    internal SipReliableOperationResult CompleteWrite(int count)
    {
        SipReliableOperationResult result = new SipReliableOperationResult();
        if (closed || writes.Count == 0 || count <= 0
            || count > writes[0].OfferedCount)
        {
            result.ErrorMessage = "The completed reliable write count is invalid.";
            return result;
        }
        SipReliableWrite write = writes[0];
        write.Offset = write.Offset + count;
        write.OfferedCount = 0;
        lastActivityMilliseconds = clock.GetMilliseconds();
        result.Success = true;
        result.BytesAccepted = count;
        if (write.Offset == write.Bytes.Length)
        {
            writes.RemoveAt(0);
            if (write.KeepAlive && !write.KeepAlivePong)
            {
                awaitingKeepAliveResponse = true;
                keepAliveResponseDeadlineMilliseconds =
                    AddMilliseconds(lastActivityMilliseconds,
                        SipDefaults.OutboundPongTimeoutMilliseconds);
            }
            else if (!write.KeepAlive)
            {
                if (write.Envelope != null)
                {
                    write.Envelope.TimestampMilliseconds = lastActivityMilliseconds;
                }
                SipTransportEvent sent = CreateEvent(
                    SipTransportEventKind.MessageSent, write.CorrelationId);
                sent.Connection = CopyConnection(connection);
                sent.Message = write.Envelope;
                AddEvent(sent);
            }
        }
        return result;
    }

    internal SipTransportEvent PollEvent()
    {
        if (events.Count == 0)
        {
            return CreateEvent(SipTransportEventKind.None, null);
        }
        SipTransportEvent result = events[0];
        events.RemoveAt(0);
        if (!closed && connection.State == SipConnectionState.Connected)
        {
            ParseBufferedMessages();
        }
        return result;
    }

    internal bool IsIdleExpired()
    {
        if (closed || transportOptions.IdleTimeoutMilliseconds <= 0)
        {
            return false;
        }
        long elapsed = clock.GetMilliseconds() - lastActivityMilliseconds;
        return elapsed >= transportOptions.IdleTimeoutMilliseconds;
    }

    internal bool IsKeepAliveFailed()
    {
        return !closed && awaitingKeepAliveResponse
            && keepAliveResponseDeadlineMilliseconds >= 0
            && clock.GetMilliseconds()
                >= keepAliveResponseDeadlineMilliseconds;
    }

    internal void Close(string? reason)
    {
        if (closed)
        {
            return;
        }
        closed = true;
        bool failed = connection.State == SipConnectionState.Failed;
        connection.State = failed ? SipConnectionState.Failed : SipConnectionState.Closed;
        int index = 0;
        while (index < writes.Count)
        {
            SipReliableWrite write = writes[index];
            if (!write.KeepAlive)
            {
                QueueSendFailure(write.CorrelationId, reason == null
                    ? "The reliable connection closed before the write completed." : reason);
            }
            index = index + 1;
        }
        writes.Clear();
        readBuffer.Clear();
        awaitingKeepAliveResponse = false;
        keepAliveResponseDeadlineMilliseconds = -1;
        SipTransportEvent disconnected = CreateEvent(
            SipTransportEventKind.Disconnected, null);
        disconnected.Connection = CopyConnection(connection);
        disconnected.ErrorMessage = reason;
        if (events.Count >= transportOptions.MaxQueueCount)
        {
            events.RemoveAt(0);
        }
        AddEvent(disconnected);
    }

    internal SipConnectionInfo GetConnectionInfo()
    {
        return CopyConnection(connection);
    }

    internal int CountQueuedWrites()
    {
        return writes.Count;
    }

    internal int CountBufferedBytes()
    {
        return readBuffer.Count;
    }

    internal bool CanReceiveBytes()
    {
        return !closed && connection.State == SipConnectionState.Connected
            && events.Count < transportOptions.MaxQueueCount;
    }

    internal bool CanWriteBytes()
    {
        if (closed || connection.State != SipConnectionState.Connected)
        {
            return false;
        }
        return writes.Count == 0 || writes[0].KeepAlive
            || events.Count < transportOptions.MaxQueueCount;
    }

    void ParseBufferedMessages()
    {
        while (readBuffer.Count > 0 && events.Count < transportOptions.MaxQueueCount)
        {
            if (ConsumeKeepAlive())
            {
                continue;
            }
            if (PendingKeepAlivePrefix())
            {
                return;
            }
            byte[] source = CopyBuffer();
            SipParseResult parsed = SipMessageParser.ParseStreamMessageBytes(
                source, parseOptions);
            if (parsed.Status == SipParseStatus.Incomplete)
            {
                return;
            }
            if (parsed.Status == SipParseStatus.Error)
            {
                QueueReceivedEvent(source, parsed);
                readBuffer.Clear();
                connection.State = SipConnectionState.Failed;
                return;
            }
            if (parsed.Consumed <= 0 || parsed.Consumed > readBuffer.Count)
            {
                SipParseResult failure = CreateParseError(
                    "The reliable stream parser returned an invalid consumed length.",
                    0, readBuffer.Count);
                QueueReceivedEvent(source, failure);
                readBuffer.Clear();
                connection.State = SipConnectionState.Failed;
                return;
            }
            byte[] messageBytes = CopyBytes(source, 0, parsed.Consumed);
            QueueReceivedEvent(messageBytes, parsed);
            readBuffer.RemoveRange(0, parsed.Consumed);
        }
    }

    bool ConsumeKeepAlive()
    {
        int count = 0;
        if (readBuffer.Count >= 4 && readBuffer[0] == 13 && readBuffer[1] == 10
            && readBuffer[2] == 13 && readBuffer[3] == 10)
        {
            count = 4;
        }
        else if (readBuffer.Count >= 2 && readBuffer[0] == 13
            && readBuffer[1] == 10
            && (readBuffer.Count > 2 || awaitingKeepAliveResponse))
        {
            count = 2;
        }
        if (count == 0)
        {
            return false;
        }
        readBuffer.RemoveRange(0, count);
        if (count == 4)
        {
            QueueKeepAlivePong();
        }
        else
        {
            awaitingKeepAliveResponse = false;
            keepAliveResponseDeadlineMilliseconds = -1;
        }
        SipTransportEvent keepAlive = CreateEvent(
            SipTransportEventKind.KeepAliveReceived, null);
        keepAlive.Connection = CopyConnection(connection);
        AddEvent(keepAlive);
        return true;
    }

    bool PendingKeepAlivePrefix()
    {
        return readBuffer.Count == 2 && readBuffer[0] == 13
            && readBuffer[1] == 10 && !awaitingKeepAliveResponse;
    }

    void QueueKeepAlivePong()
    {
        int index = 0;
        while (index < writes.Count)
        {
            if (writes[index].KeepAlivePong)
            {
                return;
            }
            index = index + 1;
        }
        SipReliableWrite pong = new SipReliableWrite();
        pong.Bytes = new byte[] { 13, 10 };
        pong.KeepAlive = true;
        pong.KeepAlivePong = true;
        int insertion = writes.Count > 0
            && (writes[0].Offset > 0 || writes[0].OfferedCount > 0) ? 1 : 0;
        writes.Insert(insertion, pong);
    }

    void QueueReceivedEvent(byte[] bytes, SipParseResult parsed)
    {
        SipTransportMessage message = new SipTransportMessage();
        message.Bytes = bytes;
        message.Message = parsed.Message;
        message.LocalAddress = CopyEndpoint(connection.LocalAddress);
        message.RemoteAddress = CopyEndpoint(connection.RemoteAddress);
        message.Transport = connection.Transport;
        message.Connection = CopyConnection(connection);
        message.ParseResult = parsed;
        message.TimestampMilliseconds = clock.GetMilliseconds();
        SipTransportEvent received = CreateEvent(
            SipTransportEventKind.MessageReceived, null);
        received.Connection = CopyConnection(connection);
        received.Message = message;
        received.ErrorMessage = parsed.Status == SipParseStatus.Error
            ? parsed.ErrorMessage : null;
        AddEvent(received);
    }

    void QueueSendFailure(string? correlationId, string message)
    {
        SipTransportEvent failed = CreateEvent(
            SipTransportEventKind.SendFailed, correlationId);
        failed.Connection = CopyConnection(connection);
        failed.ErrorMessage = message;
        AddEvent(failed);
    }

    void RemoveUnavailableWrites()
    {
        while (writes.Count > 0 && !writes[0].KeepAlive
            && (IsCancelled(writes[0].Cancellation)
                || DeadlineExpired(writes[0].DeadlineMilliseconds)))
        {
            SipReliableWrite write = writes[0];
            writes.RemoveAt(0);
            QueueSendFailure(write.CorrelationId,
                IsCancelled(write.Cancellation)
                    ? "The reliable transport write was cancelled."
                    : "The reliable transport write deadline has expired.");
        }
    }

    bool KeepAliveDue()
    {
        return transportOptions.KeepAliveMilliseconds > 0
            && clock.GetMilliseconds() - lastActivityMilliseconds
                >= transportOptions.KeepAliveMilliseconds;
    }

    static long AddMilliseconds(long value, int elapsed)
    {
        return value > long.MaxValue - elapsed
            ? long.MaxValue : value + elapsed;
    }

    bool RequestMatchesConnection(SipSendRequest request)
    {
        if (request.ConnectionId != null && request.ConnectionId != connection.Id)
        {
            return false;
        }
        if (request.Transport != SipTransportKind.Unknown
            && request.Transport != connection.Transport)
        {
            return false;
        }
        if (request.LocalAddress != null
            && !EndpointsMatch(request.LocalAddress, connection.LocalAddress))
        {
            return false;
        }
        return request.RemoteAddress == null
            || EndpointsMatch(request.RemoteAddress, connection.RemoteAddress);
    }

    SipTransportMessage CreateSentEnvelope(SipSendRequest request, byte[] bytes)
    {
        SipTransportMessage message = new SipTransportMessage();
        message.Message = request.Message;
        message.Bytes = bytes;
        message.LocalAddress = CopyEndpoint(connection.LocalAddress);
        message.RemoteAddress = CopyEndpoint(connection.RemoteAddress);
        message.Transport = connection.Transport;
        message.Connection = CopyConnection(connection);
        message.TimestampMilliseconds = -1;
        return message;
    }

    bool AddEvent(SipTransportEvent value)
    {
        if (events.Count >= transportOptions.MaxQueueCount)
        {
            return false;
        }
        events.Add(value);
        return true;
    }

    SipTransportEvent CreateEvent(SipTransportEventKind kind,
        string? correlationId)
    {
        SipTransportEvent result = new SipTransportEvent();
        result.Kind = kind;
        result.Transport = connection.Transport;
        result.CorrelationId = correlationId;
        return result;
    }

    static bool ValidateInputs(SipConnectionInfo? connectionInfo,
        SipTransportOptions? options, SipParseOptions? parsingOptions,
        ISipMonotonicClock? monotonicClock)
    {
        return connectionInfo != null && connectionInfo.Id != null
            && connectionInfo.Id.Length > 0 && connectionInfo.LocalAddress != null
            && connectionInfo.RemoteAddress != null
            && IsReliableTransport(connectionInfo.Transport)
            && options != null && options.MaxQueueCount > 0
            && options.IdleTimeoutMilliseconds >= 0
            && options.KeepAliveMilliseconds >= 0
            && parsingOptions != null && parsingOptions.MaxMessageBytes > 0
            && monotonicClock != null;
    }

    static bool IsReliableTransport(SipTransportKind transport)
    {
        return transport == SipTransportKind.Tcp || transport == SipTransportKind.Tls
            || transport == SipTransportKind.WebSocket
            || transport == SipTransportKind.SecureWebSocket
            || transport == SipTransportKind.Sctp;
    }

    static bool IsCancelled(SipCancellation? cancellation)
    {
        return cancellation != null && cancellation.Requested;
    }

    static bool EndpointsMatch(SipEndpointAddress? left, SipEndpointAddress? right)
    {
        return left != null && right != null && left.Host != null && right.Host != null
            && SipAscii.EqualsIgnoreCase(left.Host, right.Host)
            && left.Port == right.Port;
    }

    bool DeadlineExpired(long deadline)
    {
        return deadline >= 0 && clock.GetMilliseconds() >= deadline;
    }

    static byte[] CopyBytes(byte[] source, int offset, int count)
    {
        byte[] result = new byte[count];
        System.Array.Copy(source, offset, result, 0, count);
        return result;
    }

    byte[] CopyBuffer()
    {
        return readBuffer.ToArray();
    }

    static SipEndpointAddress? CopyEndpoint(SipEndpointAddress? source)
    {
        if (source == null)
        {
            return null;
        }
        SipEndpointAddress result = new SipEndpointAddress();
        result.Host = source.Host;
        result.Port = source.Port;
        result.Transport = source.Transport;
        result.Secure = source.Secure;
        return result;
    }

    static SipConnectionInfo CopyConnection(SipConnectionInfo source)
    {
        SipConnectionInfo result = new SipConnectionInfo();
        result.Id = source.Id;
        result.State = source.State;
        result.Transport = source.Transport;
        result.LocalAddress = CopyEndpoint(source.LocalAddress);
        result.RemoteAddress = CopyEndpoint(source.RemoteAddress);
        result.Secure = source.Secure;
        result.PeerIdentity = source.PeerIdentity;
        result.CertificateIdentity =
            SipCertificateIdentityCopy.Copy(source.CertificateIdentity);
        return result;
    }

    static SipTransportOptions CopyTransportOptions(SipTransportOptions source)
    {
        SipTransportOptions result = new SipTransportOptions();
        result.Kind = source.Kind;
        result.LocalAddress = CopyEndpoint(source.LocalAddress);
        result.MaxDatagramBytes = source.MaxDatagramBytes;
        result.MaxQueueCount = source.MaxQueueCount;
        result.IdleTimeoutMilliseconds = source.IdleTimeoutMilliseconds;
        result.KeepAliveMilliseconds = source.KeepAliveMilliseconds;
        result.ReuseConnections = source.ReuseConnections;
        return result;
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

    static SipParseResult CreateParseError(string message, int offset, int required)
    {
        SipParseResult result = new SipParseResult();
        result.Status = SipParseStatus.Error;
        result.ErrorMessage = message;
        result.ErrorOffset = offset;
        result.RequiredBytes = required;
        SipValidationIssue issue = new SipValidationIssue();
        issue.Severity = SipValidationSeverity.Error;
        issue.Code = "invalid-stream";
        issue.Message = message;
        issue.ByteOffset = offset;
        issue.Path = "transport.stream";
        result.ValidationIssues = new SipValidationIssue[] { issue };
        return result;
    }
}
