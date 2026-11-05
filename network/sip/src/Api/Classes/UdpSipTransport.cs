namespace sip;

/// <summary>Provides a polled SIP transport over IPv4 or IPv6 UDP datagrams.</summary>
public class UdpSipTransport : ISipTransport
{
    object gate = new object();
    System.Collections.Generic.List<SipTransportEvent> events =
        new System.Collections.Generic.List<SipTransportEvent>();
    System.Net.Sockets.Socket? socket;
    SipEndpointAddress? localAddress;
    int maximumDatagramBytes;
    int maximumQueueCount;
    bool bound;
    bool closed;

    /// <summary>Initializes an unbound UDP SIP transport.</summary>
    public UdpSipTransport()
    {
    }

    /// <summary>Binds an IPv4 or IPv6 UDP endpoint.</summary>
    /// <param name="options">The common transport options.</param>
    /// <param name="tlsOptions">Unused TLS options supplied by the common contract.</param>
    /// <param name="webSocketOptions">Unused WebSocket options supplied by the common contract.</param>
    /// <returns>The actual bound endpoint, including an assigned ephemeral port.</returns>
    public SipEndpointAddress BindTransport(SipTransportOptions options,
        SipTlsOptions tlsOptions, SipWebSocketOptions webSocketOptions)
    {
        lock (gate)
        {
            if (bound || closed)
            {
                throw new SipTransportException(
                    "The UDP transport has already been bound or closed.",
                    SipTransportKind.Udp, null);
            }
            SipEndpointAddress? requested = ValidateBindOptions(options);
            if (requested == null || requested.Host == null)
            {
                throw new SipTransportException(
                    "The UDP bind options are invalid.", SipTransportKind.Udp, null);
            }
            System.Net.IPAddress address = System.Net.IPAddress.Parse(requested.Host);
            System.Net.Sockets.Socket created = new System.Net.Sockets.Socket(
                address.AddressFamily, System.Net.Sockets.SocketType.Dgram,
                System.Net.Sockets.ProtocolType.Udp);
            created.Blocking = false;
            created.Bind(new System.Net.IPEndPoint(address, requested.Port));
            System.Net.IPEndPoint? actual =
                (System.Net.IPEndPoint?)created.LocalEndPoint;
            if (actual == null)
            {
                created.Dispose();
                throw new SipTransportException(
                    "The UDP socket did not report its bound endpoint.",
                    SipTransportKind.Udp, null);
            }
            socket = created;
            localAddress = CreateEndpoint(actual);
            maximumDatagramBytes = options.MaxDatagramBytes;
            maximumQueueCount = options.MaxQueueCount;
            bound = true;
            SipTransportEvent boundEvent = CreateEvent(SipTransportEventKind.Bound, null);
            AddEvent(boundEvent);
            return CopyEndpoint(localAddress);
        }
    }

    /// <summary>Reports that UDP has no reliable connection operation.</summary>
    /// <param name="remoteAddress">The remote endpoint.</param>
    /// <returns>No connection is returned because the operation throws.</returns>
    /// <exception cref="SipTransportException">Always thrown because UDP is connectionless.</exception>
    public SipConnectionInfo ConnectTransport(SipEndpointAddress remoteAddress)
    {
        throw new SipTransportException(
            "UDP is connectionless and does not create transport connections.",
            SipTransportKind.Udp, null);
    }

    /// <summary>Sends one SIP message as one UDP datagram.</summary>
    /// <param name="request">The send request.</param>
    /// <returns>The synchronous send result.</returns>
    public SipSendResult SendTransport(SipSendRequest request)
    {
        lock (gate)
        {
            SipSendResult result = CreateSendResult(request);
            if (!bound || closed || socket == null || localAddress == null)
            {
                return FailSend(result, "The UDP transport is not bound.", request);
            }
            if (IsCancelled(request.Cancellation))
            {
                return FailSend(result, "The UDP send was cancelled.", request);
            }
            if (DeadlineExpired(request.DeadlineMilliseconds))
            {
                return FailSend(result, "The UDP send deadline has expired.", request);
            }
            if (request.Transport != SipTransportKind.Unknown
                && request.Transport != SipTransportKind.Udp)
            {
                return FailSend(result,
                    "The send request selects a transport other than UDP.", request);
            }
            if (events.Count >= maximumQueueCount)
            {
                result.ErrorMessage = "The UDP transport event queue is full.";
                return result;
            }
            SipEndpointAddress? remote = ValidateRemoteAddress(request.RemoteAddress);
            if (remote == null || remote.Host == null)
            {
                return FailSend(result, "The UDP remote endpoint is invalid.", request);
            }
            if (!MatchesPreferredLocal(request.LocalAddress))
            {
                return FailSend(result,
                    "The requested local endpoint does not match the bound UDP endpoint.",
                    request);
            }
            SipWriteResult bytesResult = CreateSendBytes(request);
            if (!bytesResult.Success)
            {
                return FailSend(result, bytesResult.ErrorMessage == null
                    ? "The UDP message cannot be serialized." : bytesResult.ErrorMessage,
                    request);
            }
            if (bytesResult.Bytes.Length == 0
                || bytesResult.Bytes.Length > maximumDatagramBytes)
            {
                return FailSend(result,
                    "The UDP datagram exceeds the configured size limit or is empty.", request);
            }
            System.Net.IPAddress address = System.Net.IPAddress.Parse(remote.Host);
            if (address.AddressFamily != socket.AddressFamily)
            {
                return FailSend(result,
                    "The UDP destination address family does not match the bound endpoint.",
                    request);
            }
            System.Net.IPEndPoint destination =
                new System.Net.IPEndPoint(address, remote.Port);
            System.Threading.Tasks.Task<int> sending = socket.SendToAsync(
                new System.ArraySegment<byte>(bytesResult.Bytes),
                System.Net.Sockets.SocketFlags.None, destination);
            if (!CompleteTask(sending, request.DeadlineMilliseconds,
                request.Cancellation) || !sending.IsCompletedSuccessfully)
            {
                return FailSend(result, IsCancelled(request.Cancellation)
                    ? "The UDP send was cancelled."
                    : "The UDP send failed or exceeded its deadline.", request);
            }
            int sent = sending.Result;
            if (sent != bytesResult.Bytes.Length)
            {
                return FailSend(result,
                    "The UDP socket did not send the complete datagram.", request);
            }

            result.Success = true;
            result.BytesSent = sent;
            SipTransportMessage envelope = new SipTransportMessage();
            envelope.Message = request.Message;
            envelope.Bytes = CopyBytes(bytesResult.Bytes, bytesResult.Bytes.Length);
            envelope.LocalAddress = CopyEndpoint(localAddress);
            envelope.RemoteAddress = CopyEndpoint(remote);
            envelope.Transport = SipTransportKind.Udp;
            envelope.TimestampMilliseconds = System.Environment.TickCount64;
            SipTransportEvent sentEvent = CreateEvent(
                SipTransportEventKind.MessageSent, request.CorrelationId);
            sentEvent.Message = envelope;
            AddEvent(sentEvent);
            return result;
        }
    }

    /// <summary>Polls one queued or newly received UDP transport event.</summary>
    /// <returns>The next event, or an event with kind <see cref="SipTransportEventKind.None"/>.</returns>
    public SipTransportEvent PollTransportEvent()
    {
        lock (gate)
        {
            if (events.Count > 0)
            {
                SipTransportEvent queued = events[0];
                events.RemoveAt(0);
                return queued;
            }
            if (!bound || closed || socket == null || localAddress == null
                || !socket.Poll(0, System.Net.Sockets.SelectMode.SelectRead))
            {
                return CreateEvent(SipTransportEventKind.None, null);
            }
            byte[] buffer = new byte[maximumDatagramBytes + 1];
            System.Net.EndPoint anyAddress = socket.AddressFamily
                == System.Net.Sockets.AddressFamily.InterNetworkV6
                ? new System.Net.IPEndPoint(System.Net.IPAddress.IPv6Any, 0)
                : new System.Net.IPEndPoint(System.Net.IPAddress.Any, 0);
            System.Threading.Tasks.Task<System.Net.Sockets.SocketReceiveFromResult> receiving =
                socket.ReceiveFromAsync(new System.ArraySegment<byte>(buffer),
                    System.Net.Sockets.SocketFlags.None, anyAddress);
            if (!CompleteTask(receiving, -1, null)
                || !receiving.IsCompletedSuccessfully)
            {
                SipTransportEvent failed = CreateEvent(
                    SipTransportEventKind.MessageReceived, null);
                failed.ErrorMessage = "The UDP receive operation failed.";
                return failed;
            }
            System.Net.Sockets.SocketReceiveFromResult received = receiving.Result;
            System.Net.IPEndPoint remote =
                (System.Net.IPEndPoint)received.RemoteEndPoint;
            byte[] bytes = CopyBytes(buffer, received.ReceivedBytes);
            SipParseResult parsed = ParseDatagram(bytes, received.ReceivedBytes
                > maximumDatagramBytes);
            SipTransportMessage envelope = new SipTransportMessage();
            envelope.Bytes = bytes;
            envelope.LocalAddress = CopyEndpoint(localAddress);
            envelope.RemoteAddress = CreateEndpoint(remote);
            envelope.Transport = SipTransportKind.Udp;
            envelope.ParseResult = parsed;
            envelope.Message = parsed.Message;
            envelope.TimestampMilliseconds = System.Environment.TickCount64;
            SipTransportEvent messageEvent = CreateEvent(
                SipTransportEventKind.MessageReceived, null);
            messageEvent.Message = envelope;
            messageEvent.ErrorMessage = parsed.Status == SipParseStatus.Error
                ? parsed.ErrorMessage : null;
            return messageEvent;
        }
    }

    /// <summary>Closes the UDP socket and queues one closed event.</summary>
    public void CloseTransport()
    {
        lock (gate)
        {
            if (closed)
            {
                return;
            }
            closed = true;
            if (socket != null)
            {
                socket.Dispose();
                socket = null;
            }
            if (maximumQueueCount <= 0)
            {
                maximumQueueCount = 1;
            }
            if (events.Count >= maximumQueueCount)
            {
                events.RemoveAt(0);
            }
            AddEvent(CreateEvent(SipTransportEventKind.Closed, null));
        }
    }

    SipEndpointAddress? ValidateBindOptions(SipTransportOptions? options)
    {
        if (options == null || options.MaxDatagramBytes <= 0
            || options.MaxDatagramBytes > 65507 || options.MaxQueueCount <= 0
            || options.Kind != SipTransportKind.Unknown
                && options.Kind != SipTransportKind.Udp)
        {
            return null;
        }
        SipEndpointAddress requested = options.LocalAddress == null
            ? new SipEndpointAddress() : CopyEndpoint(options.LocalAddress);
        if (requested.Host == null || requested.Host.Length == 0)
        {
            requested.Host = "0.0.0.0";
        }
        if (requested.Port < 0)
        {
            requested.Port = SipTransportNames.SipPort;
        }
        if (requested.Port > 65535 || requested.Secure
            || requested.Transport != SipTransportKind.Unknown
                && requested.Transport != SipTransportKind.Udp
            || !IsNumericAddress(requested.Host))
        {
            return null;
        }
        requested.Transport = SipTransportKind.Udp;
        return requested;
    }

    SipEndpointAddress? ValidateRemoteAddress(SipEndpointAddress? remote)
    {
        if (remote == null || remote.Host == null || remote.Host.Length == 0
            || remote.Port <= 0 || remote.Port > 65535 || remote.Secure
            || remote.Transport != SipTransportKind.Unknown
                && remote.Transport != SipTransportKind.Udp
            || !IsNumericAddress(remote.Host))
        {
            return null;
        }
        SipEndpointAddress result = CopyEndpoint(remote);
        result.Host = System.Net.IPAddress.Parse(remote.Host).ToString();
        result.Transport = SipTransportKind.Udp;
        return result;
    }

    bool MatchesPreferredLocal(SipEndpointAddress? preferred)
    {
        if (preferred == null)
        {
            return true;
        }
        if (localAddress == null || preferred.Secure
            || preferred.Transport != SipTransportKind.Unknown
                && preferred.Transport != SipTransportKind.Udp
            || preferred.Port >= 0 && preferred.Port != localAddress.Port)
        {
            return false;
        }
        if (preferred.Host == null || preferred.Host.Length == 0)
        {
            return true;
        }
        return IsNumericAddress(preferred.Host)
            && System.Net.IPAddress.Parse(preferred.Host).Equals(
                System.Net.IPAddress.Parse(localAddress.Host == null
                    ? "0.0.0.0" : localAddress.Host));
    }

    SipWriteResult CreateSendBytes(SipSendRequest request)
    {
        if (request.Bytes == null)
        {
            return CreateWriteError("The UDP send byte collection cannot be null.");
        }
        if (request.Bytes.Length > 0)
        {
            SipWriteResult supplied = new SipWriteResult();
            supplied.Success = true;
            supplied.Bytes = CopyBytes(request.Bytes, request.Bytes.Length);
            supplied.RequiredBytes = supplied.Bytes.Length;
            return supplied;
        }
        if (request.Message == null)
        {
            return CreateWriteError("A SIP message or pre-serialized bytes are required.");
        }
        SipWriteOptions options = new SipWriteOptions();
        options.Canonical = true;
        options.PreserveOriginal = false;
        options.ReconcileContentLength = true;
        return SipMessageFormatter.FormatMessageModel(request.Message, options);
    }

    SipParseResult ParseDatagram(byte[] bytes, bool oversized)
    {
        if (oversized)
        {
            return CreateParseError(
                "The UDP datagram exceeds the configured size limit.",
                maximumDatagramBytes, bytes.Length);
        }
        SipParseOptions options = new SipParseOptions();
        options.Strict = false;
        options.PreserveOriginal = true;
        options.MaxMessageBytes = maximumDatagramBytes;
        options.MaxBodyBytes = maximumDatagramBytes;
        SipParseResult parsed = SipMessageParser.ParseDatagramMessageBytes(bytes, options);
        if (parsed.Status == SipParseStatus.Incomplete)
        {
            int required = parsed.RequiredBytes < 0
                ? bytes.Length + 1 : parsed.RequiredBytes;
            return CreateParseError(
                "A UDP datagram must contain one complete SIP message.",
                bytes.Length, required);
        }
        if (parsed.Status == SipParseStatus.Complete && parsed.Consumed != bytes.Length)
        {
            return CreateParseError(
                "A UDP datagram must contain exactly one SIP message.",
                parsed.Consumed, bytes.Length);
        }
        return parsed;
    }

    SipSendResult FailSend(SipSendResult result, string message,
        SipSendRequest? request)
    {
        result.ErrorMessage = message;
        SipTransportEvent failed = CreateEvent(SipTransportEventKind.SendFailed,
            request == null ? null : request.CorrelationId);
        failed.ErrorMessage = message;
        AddEvent(failed);
        return result;
    }

    bool AddEvent(SipTransportEvent value)
    {
        if (maximumQueueCount <= 0 || events.Count >= maximumQueueCount)
        {
            return false;
        }
        events.Add(value);
        return true;
    }

    static SipEndpointAddress CreateEndpoint(System.Net.IPEndPoint endpoint)
    {
        SipEndpointAddress result = new SipEndpointAddress();
        result.Host = endpoint.Address.ToString();
        result.Port = endpoint.Port;
        result.Transport = SipTransportKind.Udp;
        return result;
    }

    static SipEndpointAddress CopyEndpoint(SipEndpointAddress source)
    {
        SipEndpointAddress result = new SipEndpointAddress();
        result.Host = source.Host;
        result.Port = source.Port;
        result.Transport = source.Transport;
        result.Secure = source.Secure;
        return result;
    }

    static byte[] CopyBytes(byte[] source, int count)
    {
        byte[] result = new byte[count];
        System.Array.Copy(source, 0, result, 0, count);
        return result;
    }

    static bool IsNumericAddress(string value)
    {
        return SipUriSyntax.IsIpv4Address(value) || SipUriSyntax.IsIpv6Address(value);
    }

    static bool IsCancelled(SipCancellation? cancellation)
    {
        return cancellation != null && cancellation.Requested;
    }

    static bool DeadlineExpired(long deadline)
    {
        return deadline >= 0 && System.Environment.TickCount64 >= deadline;
    }

    static bool CompleteTask(System.Threading.Tasks.Task task, long deadline,
        SipCancellation? cancellation)
    {
        while (!task.IsCompleted && !IsCancelled(cancellation))
        {
            if (DeadlineExpired(deadline))
            {
                return false;
            }
            int interval = 10;
            if (deadline >= 0)
            {
                long remaining = deadline - System.Environment.TickCount64;
                if (remaining < interval)
                {
                    interval = remaining <= 0 ? 1 : (int)remaining;
                }
            }
            System.Threading.Tasks.Task delay = System.Threading.Tasks.Task.Delay(interval);
            System.Threading.Tasks.Task.WhenAny(
                new System.Threading.Tasks.Task[] { task, delay }).GetAwaiter().GetResult();
        }
        return task.IsCompleted;
    }

    static SipTransportEvent CreateEvent(SipTransportEventKind kind,
        string? correlationId)
    {
        SipTransportEvent result = new SipTransportEvent();
        result.Kind = kind;
        result.Transport = SipTransportKind.Udp;
        result.CorrelationId = correlationId;
        return result;
    }

    static SipSendResult CreateSendResult(SipSendRequest? request)
    {
        SipSendResult result = new SipSendResult();
        result.CorrelationId = request == null ? null : request.CorrelationId;
        return result;
    }

    static SipWriteResult CreateWriteError(string message)
    {
        SipWriteResult result = new SipWriteResult();
        result.ErrorMessage = message;
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
        issue.Code = "invalid-datagram";
        issue.Message = message;
        issue.ByteOffset = offset;
        issue.Path = "transport.datagram";
        result.ValidationIssues = new SipValidationIssue[] { issue };
        return result;
    }
}
