namespace sip;

/// <summary>Composes SIP transport, transaction, dialog, DNS, codec, and extension services.</summary>
public class SipEndpoint : ISipEndpoint
{
    object gate = new object();
    SipTransportManager transports = new SipTransportManager();
    SipTransactionLayer transactions = new SipTransactionLayer();
    SipDialogLayer dialogs = new SipDialogLayer();
    SipDnsResolver dns = new SipDnsResolver();
    SipMessageCodec codec = new SipMessageCodec();
    SipExtensionRegistry extensions = new SipExtensionRegistry();
    SipAuthenticationClient? authenticationClient;
    SipAuthenticationServer? authenticationServer;
    System.Collections.Generic.List<SipStackEvent> events =
        new System.Collections.Generic.List<SipStackEvent>();
    System.Collections.Generic.List<SipEndpointTransaction> transactionIndex =
        new System.Collections.Generic.List<SipEndpointTransaction>();
    System.Collections.Generic.List<SipTransportMessage> extensionDispatches =
        new System.Collections.Generic.List<SipTransportMessage>();
    SipEndpointState state;
    SipStackOptions? options;
    int maximumEvents = SipDefaults.MaxQueueCount;

    /// <summary>Initializes an idle endpoint with composed core components.</summary>
    public SipEndpoint()
    {
    }

    /// <summary>Registers an application-provided transport before shutdown.</summary>
    /// <param name="transport">The transport implementation.</param>
    public void RegisterEndpointTransport(ISipTransport transport)
    {
        lock (gate)
        {
            EnsureRegistrationAllowed();
            transports.RegisterTransport(transport);
        }
    }

    /// <summary>Registers an application-provided transport content coding.</summary>
    /// <param name="contentCoding">The content-coding implementation.</param>
    /// <returns>True when the coding was registered.</returns>
    public bool RegisterEndpointContentCoding(
        ISipTransportContentCoding contentCoding)
    {
        lock (gate)
        {
            EnsureRegistrationAllowed();
            return transports.RegisterTransportContentCoding(contentCoding);
        }
    }

    /// <summary>Registers an application SIP extension.</summary>
    /// <param name="extension">The extension implementation.</param>
    /// <returns>True when the extension was registered.</returns>
    public bool RegisterEndpointExtension(ISipExtension extension)
    {
        lock (gate)
        {
            EnsureRegistrationAllowed();
            return extensions.RegisterExtension(extension);
        }
    }

    /// <summary>Removes an application SIP extension.</summary>
    /// <param name="extension">The extension implementation.</param>
    /// <returns>True when the extension was removed.</returns>
    public bool RemoveEndpointExtension(ISipExtension extension)
    {
        lock (gate)
        {
            EnsureRegistrationAllowed();
            return extensions.RemoveExtension(extension);
        }
    }

    /// <summary>Configures the composed authentication client and server.</summary>
    /// <param name="credentialProvider">The shared credential provider.</param>
    public void ConfigureEndpointAuthentication(
        ISipCredentialProvider credentialProvider)
    {
        lock (gate)
        {
            EnsureRegistrationAllowed();
            if (credentialProvider == null)
            {
                throw new System.ArgumentNullException("credentialProvider");
            }
            authenticationClient =
                new SipAuthenticationClient(credentialProvider);
            authenticationServer =
                new SipAuthenticationServer(credentialProvider);
        }
    }

    /// <summary>Starts configured transports and enables endpoint operations.</summary>
    /// <param name="options">The stack options.</param>
    public void StartEndpoint(SipStackOptions options)
    {
        lock (gate)
        {
            if (state != SipEndpointState.None)
            {
                throw new System.InvalidOperationException(
                    "The endpoint can only be started once.");
            }
            ValidateStackOptions(options);
            this.options = CopyStackOptions(options);
            maximumEvents = options.MaxEventCount;
            state = SipEndpointState.Starting;
            using (SipEndpointStartGuard start =
                new SipEndpointStartGuard(this))
            {
                StartConfiguredTransports(this.options);
                state = SipEndpointState.Running;
                SipStackEvent started = new SipStackEvent();
                started.Kind = SipStackEventKind.Started;
                started.State = state;
                QueueStackEvent(started);
                start.CompleteEndpointStart();
            }
        }
    }

    /// <summary>Parses and dispatches one received transport message.</summary>
    /// <param name="message">The received message envelope.</param>
    public void ReceiveEndpointMessage(SipTransportMessage message)
    {
        lock (gate)
        {
            EnsureRunning();
            ProcessEndpointMessage(message);
        }
        DrainEndpointExtensionDispatches();
    }

    /// <summary>Sends one request or response through coordinated transaction and transport layers.</summary>
    /// <param name="request">The send request.</param>
    /// <returns>The detached send result.</returns>
    public SipSendResult SendEndpointMessage(SipSendRequest request)
    {
        lock (gate)
        {
            if (state != SipEndpointState.Running)
            {
                return CreateEndpointSendFailure(request,
                    "The endpoint is not running.");
            }
            if (request == null || request.Bytes == null)
            {
                return CreateEndpointSendFailure(request,
                    "A valid endpoint send request is required.");
            }
            if (request.Cancellation != null
                && request.Cancellation.Requested)
            {
                return CreateEndpointSendFailure(request,
                    "The endpoint send was cancelled.");
            }
            if (request.DeadlineMilliseconds >= 0
                && System.Environment.TickCount64
                    >= request.DeadlineMilliseconds)
            {
                return CreateEndpointSendFailure(request,
                    "The endpoint send deadline has expired.");
            }

            SipEndpointTransaction? indexed = null;
            SipMessage? sipMessage = request.Message;
            if (sipMessage != null
                && sipMessage.Kind == SipMessageKind.Request
                && sipMessage.Method != SipMethod.Ack)
            {
                SipTransactionOptions transactionOptions =
                    CopyTransactionOptions(RequiredOptions().TransactionOptions);
                transactionOptions.CorrelationId = request.CorrelationId;
                transactionOptions.ReliableTransport =
                    IsReliableEndpointSend(request);
                SipTransactionSnapshot created = transactions.CreateTransaction(
                    sipMessage, transactionOptions);
                indexed = IndexTransaction(created);
                indexed.SendRequest = CopySendRequest(request);
                DrainTransactionEvents();
            }
            else if (sipMessage != null
                && sipMessage.Kind == SipMessageKind.Response)
            {
                indexed = FindIndexedTransaction(
                    sipMessage, SipTransactionRole.Server);
                if (indexed == null || indexed.Id == null)
                {
                    return CreateEndpointSendFailure(request,
                        "The response does not match an endpoint server transaction.");
                }
                SipTransactionSnapshot sent =
                    transactions.SendTransactionResponse(
                        indexed.Id, sipMessage);
                IndexTransaction(sent);
                DrainTransactionEvents();
            }

            SipSendResult result = transports.SendManaged(
                CopySendRequest(request));
            if (!result.Success && indexed != null && indexed.Id != null)
            {
                transactions.ReportTransactionTransportError(
                    indexed.Id, result.ErrorMessage == null
                        ? "The endpoint transport send failed."
                        : result.ErrorMessage);
                DrainTransactionEvents();
            }
            if (sipMessage != null
                && sipMessage.Kind == SipMessageKind.Response)
            {
                SipStackEvent response = new SipStackEvent();
                response.Kind = result.Success
                    ? SipStackEventKind.OutgoingResponse
                    : SipStackEventKind.Error;
                response.Message = SipSnapshotCopy.CopyMessageModel(sipMessage);
                response.SendResult = CopySendResult(result);
                response.ErrorMessage = result.ErrorMessage;
                response.CorrelationId = request.CorrelationId;
                QueueStackEvent(response);
            }
            else if (!result.Success)
            {
                QueueEndpointError(result.ErrorMessage,
                    request.CorrelationId, sipMessage);
            }
            return CopySendResult(result);
        }
    }

    /// <summary>Advances transaction time and publishes resulting component events.</summary>
    /// <param name="nowMilliseconds">The monotonic timestamp.</param>
    public void AdvanceEndpointTime(long nowMilliseconds)
    {
        lock (gate)
        {
            EnsureRunning();
            transactions.AdvanceTransactionTime(nowMilliseconds);
            DrainTransactionEvents();
            DrainDialogEvents();
        }
    }

    /// <summary>Polls endpoint and component events in stable queue order.</summary>
    /// <returns>The next detached event.</returns>
    public SipStackEvent PollStackEvent()
    {
        DrainEndpointExtensionDispatches();
        lock (gate)
        {
            if (events.Count > 0)
            {
                return RemoveStackEvent();
            }
            SipTransportEvent transport =
                transports.PollManagedTransportEvent();
            if (transport.Kind != SipTransportEventKind.None)
            {
                SipStackEvent wrapped = new SipStackEvent();
                wrapped.Kind = SipStackEventKind.Transport;
                wrapped.TransportEvent = CopyTransportEvent(transport);
                wrapped.Message = transport.Message == null ? null
                    : SipSnapshotCopy.CopyMessageModel(
                        transport.Message.Message);
                wrapped.ErrorMessage = transport.ErrorMessage;
                wrapped.CorrelationId = transport.CorrelationId;
                wrapped.State = state;
                if (transport.Kind == SipTransportEventKind.MessageReceived
                    && transport.Message != null
                    && state == SipEndpointState.Running)
                {
                    ProcessEndpointMessage(transport.Message);
                }
                return CopyStackEvent(wrapped);
            }
            SipTransactionEvent transaction =
                transactions.PollTransactionEvent();
            if (transaction.Kind != SipTransactionEventKind.None)
            {
                return CreateTransactionStackEvent(transaction);
            }
            SipDialogEvent dialog = dialogs.PollDialogEvent();
            if (dialog.Kind != null && dialog.Kind.Length > 0)
            {
                return CreateDialogStackEvent(dialog);
            }
            SipStackEvent empty = new SipStackEvent();
            empty.State = state;
            return empty;
        }
    }

    /// <summary>Closes transports and transitions to the terminal stopped state.</summary>
    /// <param name="cancellation">The shutdown request.</param>
    public void StopEndpoint(SipCancellation cancellation)
    {
        lock (gate)
        {
            if (cancellation == null)
            {
                throw new System.ArgumentException(
                    "A shutdown cancellation request is required.",
                    "cancellation");
            }
            if (!cancellation.Requested)
            {
                return;
            }
            if (state == SipEndpointState.Stopped)
            {
                return;
            }
            if (state != SipEndpointState.Running)
            {
                throw new System.InvalidOperationException(
                    "Only a running endpoint can be stopped.");
            }
            state = SipEndpointState.Stopping;
            extensionDispatches.Clear();
            int transactionIndexValue = 0;
            while (transactionIndexValue < transactionIndex.Count)
            {
                SipEndpointTransaction indexed =
                    transactionIndex[transactionIndexValue];
                if (indexed.Id != null
                    && indexed.State != SipTransactionState.Terminated)
                {
                    transactions.TerminateTransaction(
                        indexed.Id, cancellation);
                }
                transactionIndexValue = transactionIndexValue + 1;
            }
            DrainTransactionEvents();
            transports.CloseManagedTransports();
            transactionIndex.Clear();
            state = SipEndpointState.Stopped;
            SipStackEvent stopped = new SipStackEvent();
            stopped.Kind = SipStackEventKind.Stopped;
            stopped.State = state;
            stopped.ErrorMessage = cancellation.Reason;
            stopped.CorrelationId = cancellation.Id;
            QueueStackEvent(stopped);
        }
    }

    /// <summary>Gets the current endpoint lifecycle state.</summary>
    /// <returns>The current state.</returns>
    public SipEndpointState GetEndpointState()
    {
        lock (gate)
        {
            return state;
        }
    }

    /// <summary>Gets the composed transport manager.</summary>
    /// <returns>The endpoint transport manager.</returns>
    public ISipTransportManager GetEndpointTransportManager()
    {
        return transports;
    }

    /// <summary>Gets the composed transaction layer.</summary>
    /// <returns>The endpoint transaction layer.</returns>
    public ISipTransactionLayer GetEndpointTransactionLayer()
    {
        return transactions;
    }

    /// <summary>Gets the composed dialog layer.</summary>
    /// <returns>The endpoint dialog layer.</returns>
    public ISipDialogLayer GetEndpointDialogLayer()
    {
        return dialogs;
    }

    /// <summary>Gets the composed DNS resolver.</summary>
    /// <returns>The endpoint DNS resolver.</returns>
    public ISipDnsResolver GetEndpointDnsResolver()
    {
        return dns;
    }

    /// <summary>Gets the composed message codec.</summary>
    /// <returns>The endpoint message codec.</returns>
    public ISipMessageCodec GetEndpointMessageCodec()
    {
        return codec;
    }

    /// <summary>Gets the composed extension registry.</summary>
    /// <returns>The endpoint extension registry.</returns>
    public ISipExtensionRegistry GetEndpointExtensionRegistry()
    {
        return extensions;
    }

    /// <summary>Gets the configured authentication client.</summary>
    /// <returns>The endpoint authentication client.</returns>
    public ISipAuthenticationClient GetEndpointAuthenticationClient()
    {
        lock (gate)
        {
            if (authenticationClient == null)
            {
                throw new System.InvalidOperationException(
                    "Endpoint authentication has not been configured.");
            }
            return authenticationClient;
        }
    }

    /// <summary>Gets the configured authentication server.</summary>
    /// <returns>The endpoint authentication server.</returns>
    public ISipAuthenticationServer GetEndpointAuthenticationServer()
    {
        lock (gate)
        {
            if (authenticationServer == null)
            {
                throw new System.InvalidOperationException(
                    "Endpoint authentication has not been configured.");
            }
            return authenticationServer;
        }
    }

    internal void FailEndpointStart()
    {
        lock (gate)
        {
            if (state == SipEndpointState.Starting)
            {
                state = SipEndpointState.Failed;
                transports.CloseManagedTransports();
                transactionIndex.Clear();
                events.Clear();
            }
        }
    }

    internal void ConfigureEndpointSources(ISipMonotonicClock clock,
        ISipRandomSource random, SipTransactionOptions transactionOptions)
    {
        lock (gate)
        {
            if (clock == null || random == null || transactionOptions == null)
            {
                throw new System.ArgumentNullException("clock");
            }
            if (state != SipEndpointState.None)
            {
                throw new System.InvalidOperationException(
                    "Endpoint sources can only be configured before startup.");
            }
            transactions.ConfigureTransactionSources(
                clock, random, CopyTransactionOptions(transactionOptions));
            dns.ConfigureResolutionSources(random, clock);
        }
    }

    void ProcessEndpointMessage(SipTransportMessage source)
    {
        if (source == null || source.Bytes == null)
        {
            QueueEndpointError(
                "A valid received transport message is required.",
                null, null);
            return;
        }
        SipTransportMessage message = CopyTransportMessage(source);
        if (message.Message == null)
        {
            SipParseResult parsed = codec.ParseDatagramMessage(
                message.Bytes, RequiredOptions().ParseOptions);
            message.ParseResult = parsed;
            message.Message = parsed.Message;
            if (parsed.Status != SipParseStatus.Complete
                || parsed.Message == null)
            {
                QueueEndpointError(parsed.ErrorMessage == null
                    ? "The received SIP message could not be parsed."
                    : parsed.ErrorMessage, message.CorrelationId, null);
                return;
            }
        }
        SipMessage? receivedValue = message.Message;
        if (receivedValue == null)
        {
            QueueEndpointError(
                "The received SIP message is unavailable.",
                message.CorrelationId, null);
            return;
        }
        SipMessage received = receivedValue;
        if (received.Kind == SipMessageKind.Request)
        {
            SipStackEvent incoming = new SipStackEvent();
            incoming.Kind = SipStackEventKind.IncomingRequest;
            incoming.Message = SipSnapshotCopy.CopyMessageModel(received);
            incoming.CorrelationId = message.CorrelationId;
            QueueStackEvent(incoming);
        }

        SipTransactionSnapshot transaction =
            transactions.ReceiveTransactionMessage(message);
        IndexTransaction(transaction);
        DrainTransactionEvents();
        if (received.Kind == SipMessageKind.Request)
        {
            SipTransportMessage dispatch = new SipTransportMessage();
            dispatch.Message = SipSnapshotCopy.CopyMessageModel(received);
            dispatch.CorrelationId = message.CorrelationId;
            extensionDispatches.Add(dispatch);
        }
    }

    void DrainEndpointExtensionDispatches()
    {
        SipTransportMessage? dispatch = null;
        lock (gate)
        {
            if (state != SipEndpointState.Running)
            {
                extensionDispatches.Clear();
                return;
            }
            if (extensionDispatches.Count > 0)
            {
                dispatch = extensionDispatches[0];
                extensionDispatches.RemoveAt(0);
            }
        }
        while (dispatch != null)
        {
            if (dispatch.Message != null)
            {
                DispatchEndpointExtension(
                    dispatch.Message, dispatch.CorrelationId);
            }
            lock (gate)
            {
                if (state != SipEndpointState.Running)
                {
                    extensionDispatches.Clear();
                    return;
                }
                dispatch = extensionDispatches.Count == 0
                    ? null : extensionDispatches[0];
                if (dispatch != null)
                {
                    extensionDispatches.RemoveAt(0);
                }
            }
        }
    }

    void DispatchEndpointExtension(SipMessage message, string? correlationId)
    {
        SipExtensionResult result =
            extensions.ProcessRegisteredRequest(message);
        if (!result.Handled && result.Response == null
            && result.ErrorMessage == null)
        {
            return;
        }
        SipStackEvent extension = new SipStackEvent();
        extension.Kind = SipStackEventKind.Extension;
        extension.Message = SipSnapshotCopy.CopyMessageModel(result.Response);
        extension.ExtensionResult = CopyExtensionResult(result);
        extension.ErrorMessage = result.ErrorMessage;
        extension.CorrelationId = correlationId;
        lock (gate)
        {
            if (state == SipEndpointState.Running)
            {
                QueueStackEvent(extension);
            }
        }
    }

    void DrainTransactionEvents()
    {
        SipTransactionEvent value = transactions.PollTransactionEvent();
        while (value.Kind != SipTransactionEventKind.None)
        {
            if (value.Kind == SipTransactionEventKind.Retransmitted
                && value.Message != null)
            {
                SendEndpointRetransmission(value);
            }
            QueueStackEvent(CreateTransactionStackEvent(value));
            value = transactions.PollTransactionEvent();
        }
    }

    void SendEndpointRetransmission(SipTransactionEvent value)
    {
        SipEndpointTransaction? indexed = FindIndexedTransactionById(
            value.TransactionId);
        if (indexed == null || indexed.SendRequest == null
            || value.Message == null)
        {
            return;
        }
        SipSendRequest request = CopySendRequest(indexed.SendRequest);
        request.Message = SipSnapshotCopy.CopyMessageModel(value.Message);
        request.Bytes = new byte[0];
        request.Cancellation = null;
        request.DeadlineMilliseconds = -1;
        SipSendResult sent = transports.SendManaged(request);
        if (!sent.Success)
        {
            if (indexed.Id != null)
            {
                transactions.ReportTransactionTransportError(
                    indexed.Id, sent.ErrorMessage == null
                        ? "The endpoint retransmission failed."
                        : sent.ErrorMessage);
            }
            QueueEndpointError(sent.ErrorMessage,
                value.CorrelationId, value.Message);
        }
    }

    void DrainDialogEvents()
    {
        SipDialogEvent value = dialogs.PollDialogEvent();
        while (value.Kind != null && value.Kind.Length > 0)
        {
            QueueStackEvent(CreateDialogStackEvent(value));
            value = dialogs.PollDialogEvent();
        }
    }

    SipStackEvent CreateTransactionStackEvent(SipTransactionEvent value)
    {
        SipStackEvent result = new SipStackEvent();
        result.Kind = SipStackEventKind.Transaction;
        result.TransactionEvent = SipSnapshotCopy.CopyTransactionEvent(value);
        result.Message = SipSnapshotCopy.CopyMessageModel(value.Message);
        result.CorrelationId = value.CorrelationId;
        result.State = state;
        if (value.Kind == SipTransactionEventKind.Terminated)
        {
            RemoveIndexedTransaction(value.TransactionId);
        }
        return result;
    }

    SipStackEvent CreateDialogStackEvent(SipDialogEvent value)
    {
        SipStackEvent result = new SipStackEvent();
        result.Kind = SipStackEventKind.Dialog;
        result.DialogEvent = SipSnapshotCopy.CopyDialogEvent(value);
        result.Message = SipSnapshotCopy.CopyMessageModel(value.Message);
        result.CorrelationId = value.CorrelationId;
        result.State = state;
        return result;
    }

    SipEndpointTransaction IndexTransaction(SipTransactionSnapshot snapshot)
    {
        SipEndpointTransaction? indexed = FindIndexedTransactionById(
            snapshot.Id);
        if (indexed == null)
        {
            indexed = new SipEndpointTransaction();
            transactionIndex.Add(indexed);
        }
        indexed.Id = snapshot.Id;
        indexed.Role = snapshot.Role;
        indexed.State = snapshot.State;
        SipTransactionKey? key = snapshot.Key;
        if (key != null)
        {
            indexed.Branch = key.Branch;
            indexed.CallId = key.CallId;
            indexed.CSeq = key.CSeq;
            indexed.Method = key.Method;
        }
        return indexed;
    }

    SipEndpointTransaction? FindIndexedTransaction(
        SipMessage message, SipTransactionRole role)
    {
        string? callId = ReadCallId(message);
        int cseq = message.CSeq == null
            ? -1 : message.CSeq.SequenceNumber;
        SipMethod method = message.CSeq == null
            ? SipMethod.Unknown : message.CSeq.Method;
        string? branch = message.ViaValues.Length == 0
            ? null : message.ViaValues[0].Branch;
        int index = 0;
        while (index < transactionIndex.Count)
        {
            SipEndpointTransaction candidate = transactionIndex[index];
            if (candidate.Role == role && candidate.CallId == callId
                && candidate.CSeq == cseq && candidate.Method == method)
            {
                if (!SipTransactionMatching.HasMagicCookie(candidate.Branch)
                    || candidate.Branch == branch)
                {
                    return candidate;
                }
            }
            index = index + 1;
        }
        return null;
    }

    SipEndpointTransaction? FindIndexedTransactionById(string? id)
    {
        int index = 0;
        while (index < transactionIndex.Count)
        {
            SipEndpointTransaction candidate = transactionIndex[index];
            if (candidate.Id == id)
            {
                return candidate;
            }
            index = index + 1;
        }
        return null;
    }

    void RemoveIndexedTransaction(string? id)
    {
        int index = 0;
        while (index < transactionIndex.Count)
        {
            if (transactionIndex[index].Id == id)
            {
                transactionIndex.RemoveAt(index);
                return;
            }
            index = index + 1;
        }
    }

    static string? ReadCallId(SipMessage message)
    {
        int index = 0;
        while (index < message.Headers.Length)
        {
            SipHeader? header = message.Headers[index];
            if (header != null
                && (SipAscii.EqualsIgnoreCase(
                        header.Name, SipHeaderNames.CallId)
                    || SipAscii.EqualsIgnoreCase(
                        header.Name, SipHeaderNames.CompactCallId)))
            {
                return header.Value == null ? null : header.Value.Trim();
            }
            index = index + 1;
        }
        return null;
    }

    static bool IsReliableEndpointSend(SipSendRequest request)
    {
        SipTransportKind transport = request.Transport;
        if (transport == SipTransportKind.Unknown
            && request.RemoteAddress != null)
        {
            transport = request.RemoteAddress.Transport;
        }
        return transport == SipTransportKind.Tcp
            || transport == SipTransportKind.Tls
            || transport == SipTransportKind.WebSocket
            || transport == SipTransportKind.SecureWebSocket
            || transport == SipTransportKind.Sctp;
    }

    void StartConfiguredTransports(SipStackOptions configured)
    {
        int index = 0;
        while (index < configured.Transports.Length)
        {
            SipTransportOptions transportOptions = configured.Transports[index];
            ISipTransport transport = CreateConfiguredTransport(
                transportOptions.Kind);
            transports.RegisterTransport(transport);
            transport.BindTransport(transportOptions,
                configured.TlsOptions, configured.WebSocketOptions);
            index = index + 1;
        }
    }

    static ISipTransport CreateConfiguredTransport(SipTransportKind kind)
    {
        if (kind == SipTransportKind.Udp)
        {
            return new UdpSipTransport();
        }
        if (kind == SipTransportKind.Tcp)
        {
            return new TcpSipTransport();
        }
        if (kind == SipTransportKind.Tls)
        {
            return new TlsSipTransport();
        }
        if (kind == SipTransportKind.WebSocket
            || kind == SipTransportKind.SecureWebSocket)
        {
            return new WebSocketSipTransport();
        }
        throw new System.ArgumentException(
            "Configured endpoint transports must be built-in transport kinds.",
            "options");
    }

    bool QueueStackEvent(SipStackEvent value)
    {
        value.State = state;
        if (events.Count >= maximumEvents)
        {
            events.RemoveAt(0);
            SipStackEvent overflow = new SipStackEvent();
            overflow.Kind = SipStackEventKind.Error;
            overflow.State = state;
            overflow.ErrorMessage =
                "The endpoint event queue limit was reached.";
            overflow.CorrelationId = value.CorrelationId;
            events.Add(overflow);
            return false;
        }
        events.Add(CopyStackEvent(value));
        return true;
    }

    SipStackEvent RemoveStackEvent()
    {
        SipStackEvent value = events[0];
        events.RemoveAt(0);
        return CopyStackEvent(value);
    }

    void QueueEndpointError(
        string? error, string? correlationId, SipMessage? message)
    {
        SipStackEvent value = new SipStackEvent();
        value.Kind = SipStackEventKind.Error;
        value.Message = SipSnapshotCopy.CopyMessageModel(message);
        value.ErrorMessage = error == null
            ? "The endpoint operation failed." : error;
        value.CorrelationId = correlationId;
        QueueStackEvent(value);
    }

    SipSendResult CreateEndpointSendFailure(
        SipSendRequest? request, string error)
    {
        SipSendResult result = new SipSendResult();
        result.ErrorMessage = error;
        result.CorrelationId = request == null
            ? null : request.CorrelationId;
        QueueEndpointError(error, result.CorrelationId,
            request == null ? null : request.Message);
        return result;
    }

    void EnsureRunning()
    {
        if (state != SipEndpointState.Running)
        {
            throw new System.InvalidOperationException(
                "The endpoint is not running.");
        }
    }

    void EnsureRegistrationAllowed()
    {
        if (state == SipEndpointState.Stopping
            || state == SipEndpointState.Stopped
            || state == SipEndpointState.Failed)
        {
            throw new System.InvalidOperationException(
                "Endpoint registration is closed.");
        }
    }

    SipStackOptions RequiredOptions()
    {
        if (options == null)
        {
            throw new System.InvalidOperationException(
                "The endpoint has not been configured.");
        }
        return options;
    }

    static void ValidateStackOptions(SipStackOptions? value)
    {
        if (value == null || value.ParseOptions == null
            || value.WriteOptions == null || value.DnsOptions == null
            || value.TransactionOptions == null
            || value.DialogOptions == null || value.TlsOptions == null
            || value.WebSocketOptions == null || value.Transports == null)
        {
            throw new System.ArgumentException(
                "Complete endpoint stack options are required.", "options");
        }
        if (value.MaxEventCount < 1
            || value.MaxEventCount > SipDefaults.MaxQueueCount)
        {
            throw new System.ArgumentOutOfRangeException("options");
        }
        int index = 0;
        while (index < value.Transports.Length)
        {
            if (value.Transports[index] == null)
            {
                throw new System.ArgumentException(
                    "Configured transport options cannot be null.", "options");
            }
            index = index + 1;
        }
    }

    static SipStackOptions CopyStackOptions(SipStackOptions source)
    {
        SipStackOptions value = new SipStackOptions();
        value.ParseOptions = CopyParseOptions(source.ParseOptions);
        value.WriteOptions = CopyWriteOptions(source.WriteOptions);
        value.DnsOptions = source.DnsOptions;
        value.TransactionOptions =
            CopyTransactionOptions(source.TransactionOptions);
        value.DialogOptions = source.DialogOptions;
        value.TlsOptions = source.TlsOptions;
        value.WebSocketOptions = source.WebSocketOptions;
        value.Transports = new SipTransportOptions[source.Transports.Length];
        int index = 0;
        while (index < source.Transports.Length)
        {
            value.Transports[index] = source.Transports[index];
            index = index + 1;
        }
        value.MaxEventCount = source.MaxEventCount;
        return value;
    }

    static SipParseOptions CopyParseOptions(SipParseOptions source)
    {
        SipParseOptions value = new SipParseOptions();
        value.Strict = source.Strict;
        value.PreserveOriginal = source.PreserveOriginal;
        value.AllowHeaderFolding = source.AllowHeaderFolding;
        value.AllowCompatibleWhitespace = source.AllowCompatibleWhitespace;
        value.MaxMessageBytes = source.MaxMessageBytes;
        value.MaxBodyBytes = source.MaxBodyBytes;
        value.MaxHeaderBytes = source.MaxHeaderBytes;
        value.MaxStartLineBytes = source.MaxStartLineBytes;
        value.MaxHeaderLineBytes = source.MaxHeaderLineBytes;
        value.MaxHeaderCount = source.MaxHeaderCount;
        value.MaxUriBytes = source.MaxUriBytes;
        value.MaxParameterCount = source.MaxParameterCount;
        value.MaxListValueCount = source.MaxListValueCount;
        value.MaxMimeDepth = source.MaxMimeDepth;
        value.MaxXmlDepth = source.MaxXmlDepth;
        return value;
    }

    static SipWriteOptions CopyWriteOptions(SipWriteOptions source)
    {
        SipWriteOptions value = new SipWriteOptions();
        value.Canonical = source.Canonical;
        value.PreserveOriginal = source.PreserveOriginal;
        value.CompactHeaderNames = source.CompactHeaderNames;
        value.AllowHeaderFolding = source.AllowHeaderFolding;
        value.ReconcileContentLength = source.ReconcileContentLength;
        value.MaxHeaderLineBytes = source.MaxHeaderLineBytes;
        return value;
    }

    static SipTransactionOptions CopyTransactionOptions(
        SipTransactionOptions source)
    {
        SipTransactionOptions value = new SipTransactionOptions();
        value.Timers = new SipTimerOptions();
        value.Timers.T1Milliseconds = source.Timers.T1Milliseconds;
        value.Timers.T2Milliseconds = source.Timers.T2Milliseconds;
        value.Timers.T4Milliseconds = source.Timers.T4Milliseconds;
        value.Timers.MaxTimerCount = source.Timers.MaxTimerCount;
        value.ReliableTransport = source.ReliableTransport;
        value.AllowLegacyMatching = source.AllowLegacyMatching;
        value.MaxTransactionCount = source.MaxTransactionCount;
        value.MaxEventCount = source.MaxEventCount;
        value.CorrelationId = source.CorrelationId;
        return value;
    }

    static SipSendRequest CopySendRequest(SipSendRequest source)
    {
        SipSendRequest value = new SipSendRequest();
        value.Message = SipSnapshotCopy.CopyMessageModel(source.Message);
        value.Bytes = CopyBytes(source.Bytes);
        value.RemoteAddress = CopyEndpointAddress(source.RemoteAddress);
        value.LocalAddress = CopyEndpointAddress(source.LocalAddress);
        value.Transport = source.Transport;
        value.ConnectionId = source.ConnectionId;
        value.FlowId = source.FlowId;
        value.DeadlineMilliseconds = source.DeadlineMilliseconds;
        value.Cancellation = source.Cancellation;
        value.CorrelationId = source.CorrelationId;
        value.ContentCoding = source.ContentCoding;
        return value;
    }

    static SipSendResult CopySendResult(SipSendResult source)
    {
        SipSendResult value = new SipSendResult();
        value.Success = source.Success;
        value.BytesSent = source.BytesSent;
        value.Connection = CopyConnection(source.Connection);
        value.Flow = SipSnapshotCopy.CopyFlowModel(source.Flow);
        value.ErrorMessage = source.ErrorMessage;
        value.CorrelationId = source.CorrelationId;
        value.ContentCoding = source.ContentCoding;
        return value;
    }

    static SipTransportMessage CopyTransportMessage(
        SipTransportMessage source)
    {
        SipTransportMessage value = new SipTransportMessage();
        value.Message = SipSnapshotCopy.CopyMessageModel(source.Message);
        value.Bytes = CopyBytes(source.Bytes);
        value.LocalAddress = CopyEndpointAddress(source.LocalAddress);
        value.RemoteAddress = CopyEndpointAddress(source.RemoteAddress);
        value.Transport = source.Transport;
        value.Connection = CopyConnection(source.Connection);
        value.Flow = SipSnapshotCopy.CopyFlowModel(source.Flow);
        value.ParseResult = source.ParseResult;
        value.TimestampMilliseconds = source.TimestampMilliseconds;
        value.CorrelationId = source.CorrelationId;
        value.ContentCoding = source.ContentCoding;
        return value;
    }

    static SipTransportEvent CopyTransportEvent(SipTransportEvent source)
    {
        SipTransportEvent value = new SipTransportEvent();
        value.Kind = source.Kind;
        value.Transport = source.Transport;
        value.Message = source.Message == null
            ? null : CopyTransportMessage(source.Message);
        value.Connection = CopyConnection(source.Connection);
        value.Flow = SipSnapshotCopy.CopyFlowModel(source.Flow);
        value.ErrorMessage = source.ErrorMessage;
        value.CorrelationId = source.CorrelationId;
        return value;
    }

    static SipConnectionInfo? CopyConnection(SipConnectionInfo? source)
    {
        if (source == null)
        {
            return null;
        }
        SipConnectionInfo value = new SipConnectionInfo();
        value.Id = source.Id;
        value.State = source.State;
        value.Transport = source.Transport;
        value.LocalAddress = CopyEndpointAddress(source.LocalAddress);
        value.RemoteAddress = CopyEndpointAddress(source.RemoteAddress);
        value.Secure = source.Secure;
        value.PeerIdentity = source.PeerIdentity;
        value.CertificateIdentity = source.CertificateIdentity;
        return value;
    }

    static SipEndpointAddress? CopyEndpointAddress(SipEndpointAddress? source)
    {
        if (source == null)
        {
            return null;
        }
        SipEndpointAddress value = new SipEndpointAddress();
        value.Host = source.Host;
        value.Port = source.Port;
        value.Transport = source.Transport;
        value.Secure = source.Secure;
        return value;
    }

    static SipExtensionResult? CopyExtensionResult(
        SipExtensionResult? source)
    {
        if (source == null)
        {
            return null;
        }
        SipExtensionResult value = new SipExtensionResult();
        value.Handled = source.Handled;
        value.Response = SipSnapshotCopy.CopyMessageModel(source.Response);
        value.Event = source.Event;
        value.ErrorMessage = source.ErrorMessage;
        return value;
    }

    static SipStackEvent CopyStackEvent(SipStackEvent source)
    {
        SipStackEvent value = new SipStackEvent();
        value.Kind = source.Kind;
        value.State = source.State;
        value.Message = SipSnapshotCopy.CopyMessageModel(source.Message);
        value.TransportEvent = source.TransportEvent == null
            ? null : CopyTransportEvent(source.TransportEvent);
        value.TransactionEvent = SipSnapshotCopy.CopyTransactionEvent(
            source.TransactionEvent);
        value.DialogEvent = SipSnapshotCopy.CopyDialogEvent(
            source.DialogEvent);
        value.RegistrationEvent = SipSnapshotCopy.CopyRegistrationEvent(
            source.RegistrationEvent);
        value.ProxyEvent = source.ProxyEvent;
        value.InviteEvent = source.InviteEvent;
        value.ExtensionResult = CopyExtensionResult(source.ExtensionResult);
        value.SendResult = source.SendResult == null
            ? null : CopySendResult(source.SendResult);
        value.ErrorMessage = source.ErrorMessage;
        value.CorrelationId = source.CorrelationId;
        return value;
    }

    static byte[] CopyBytes(byte[]? source)
    {
        if (source == null)
        {
            return new byte[0];
        }
        byte[] value = new byte[source.Length];
        System.Array.Copy(source, value, source.Length);
        return value;
    }
}
