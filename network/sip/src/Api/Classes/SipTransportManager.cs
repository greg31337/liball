namespace sip;

/// <summary>Coordinates registered SIP transports and preserves connection and flow affinity.</summary>
public class SipTransportManager : ISipTransportManager
{
    object gate = new object();
    System.Collections.Generic.List<SipManagedTransport> transports =
        new System.Collections.Generic.List<SipManagedTransport>();
    System.Collections.Generic.List<SipManagedConnection> connections =
        new System.Collections.Generic.List<SipManagedConnection>();
    System.Collections.Generic.List<SipManagedFlow> flows =
        new System.Collections.Generic.List<SipManagedFlow>();
    System.Collections.Generic.List<SipTransportEvent> events =
        new System.Collections.Generic.List<SipTransportEvent>();
    System.Collections.Generic.List<ISipTransportContentCoding> contentCodings =
        new System.Collections.Generic.List<ISipTransportContentCoding>();
    int nextPollIndex;
    bool closed;

    /// <summary>Initializes an empty transport manager.</summary>
    public SipTransportManager()
    {
    }

    /// <summary>Registers one transport implementation.</summary>
    /// <param name="transport">The transport implementation.</param>
    public void RegisterTransport(ISipTransport transport)
    {
        lock (gate)
        {
            if (transport == null)
            {
                throw new System.ArgumentNullException("transport");
            }
            if (closed)
            {
                throw new SipTransportException(
                    "The transport manager is closed.",
                    SipTransportKind.Unknown, null);
            }
            if (FindRegisteredTransport(transport) != null)
            {
                throw new System.ArgumentException(
                    "The transport implementation is already registered.",
                    "transport");
            }
            if (transports.Count >= SipDefaults.MaxConnectionCount)
            {
                throw new System.InvalidOperationException(
                    "The transport manager registration limit was reached.");
            }
            SipManagedTransport registration = new SipManagedTransport();
            registration.Transport = transport;
            transports.Add(registration);
        }
    }

    /// <summary>Registers one application-provided transport content coding.</summary>
    /// <param name="contentCoding">The content-coding implementation.</param>
    /// <returns>True when the implementation was registered.</returns>
    public bool RegisterTransportContentCoding(
        ISipTransportContentCoding contentCoding)
    {
        lock (gate)
        {
            if (contentCoding == null || closed)
            {
                return false;
            }
            string? name = contentCoding.GetTransportContentCodingName();
            if (!SipAscii.IsToken(name)
                || FindManagedContentCoding(name) != null
                || contentCodings.Count >= SipDefaults.MaxListValueCount)
            {
                return false;
            }
            contentCodings.Add(contentCoding);
            return true;
        }
    }

    /// <summary>Removes one application-provided transport content coding.</summary>
    /// <param name="contentCoding">The content-coding implementation.</param>
    /// <returns>True when the implementation was removed.</returns>
    public bool RemoveTransportContentCoding(
        ISipTransportContentCoding contentCoding)
    {
        lock (gate)
        {
            if (contentCoding == null)
            {
                return false;
            }
            int index = 0;
            while (index < contentCodings.Count)
            {
                if (object.ReferenceEquals(contentCodings[index], contentCoding))
                {
                    contentCodings.RemoveAt(index);
                    return true;
                }
                index = index + 1;
            }
            return false;
        }
    }

    /// <summary>Releases one externally managed SigComp compartment.</summary>
    /// <param name="metadata">The coding and compartment metadata.</param>
    /// <returns>True when a registered implementation received the close operation.</returns>
    public bool CloseTransportContentCodingCompartment(
        SipContentCodingMetadata metadata)
    {
        lock (gate)
        {
            if (metadata == null || metadata.SigCompCompartment == null)
            {
                return false;
            }
            ISipTransportContentCoding? contentCoding =
                FindManagedContentCoding(metadata.Name);
            if (contentCoding == null)
            {
                return false;
            }
            SipSigCompCompartment compartment =
                CopyManagedCompartment(metadata.SigCompCompartment);
            compartment.Closed = true;
            contentCoding.CloseTransportContentCodingCompartment(compartment);
            return true;
        }
    }

    /// <summary>Sends using flow, connection, endpoint, and transport selection policy.</summary>
    /// <param name="request">The send request.</param>
    /// <returns>The send result.</returns>
    public SipSendResult SendManaged(SipSendRequest request)
    {
        lock (gate)
        {
            DiscoverManagedTransportKinds();
            SipTransportKind requestedKind = FindRequestedTransportKind(request);
            if (events.Count >= SipDefaults.MaxQueueCount)
            {
                return CreateManagedSendFailure(request, requestedKind,
                    "The managed transport event queue is full.", null, null, false);
            }
            if (request == null)
            {
                return CreateManagedSendFailure(null, SipTransportKind.Unknown,
                    "A managed send request is required.", null, null, true);
            }
            if (closed)
            {
                return CreateManagedSendFailure(request, requestedKind,
                    "The transport manager is closed.", null, null, true);
            }
            if (request.Bytes == null)
            {
                return CreateManagedSendFailure(request, requestedKind,
                    "The managed send byte collection cannot be null.",
                    null, null, true);
            }
            if (request.Cancellation != null && request.Cancellation.Requested)
            {
                return CreateManagedSendFailure(request, requestedKind,
                    "The managed send was cancelled.", null, null, true);
            }
            if (request.DeadlineMilliseconds >= 0
                && System.Environment.TickCount64 >= request.DeadlineMilliseconds)
            {
                return CreateManagedSendFailure(request, requestedKind,
                    "The managed send deadline has expired.", null, null, true);
            }

            SipManagedFlow? flow = FindManagedFlow(request.FlowId);
            if (flow != null && flow.Flow != null && flow.Flow.Failed)
            {
                return CreateManagedSendFailure(request,
                    FindFlowTransportKind(flow.Flow),
                    "The requested outbound flow has failed.",
                    flow.Flow.Connection, flow.Flow, true);
            }
            SipManagedConnection? connection = flow == null
                ? FindManagedConnection(request.ConnectionId) : null;
            SipTransportKind selectedKind = SelectTransportKind(
                request, flow, connection);
            if (ManagedSendRequiresSecurity(request)
                && !IsManagedSecureTransport(selectedKind))
            {
                return CreateManagedSendFailure(request, selectedKind,
                    "A secure SIP send cannot downgrade to an insecure transport.",
                    connection == null ? null : connection.Connection,
                    flow == null ? null : flow.Flow, true);
            }
            if (flow == null && connection == null
                && (request.ConnectionId == null
                    || request.ConnectionId.Length == 0))
            {
                connection = FindReusableManagedConnection(
                    selectedKind, request.RemoteAddress);
            }

            SipManagedTransport? affinity = flow != null
                ? flow.Transport : connection == null ? null : connection.Transport;
            if (affinity != null)
            {
                return SendUsingManagedTransport(affinity, request,
                    selectedKind, connection, flow);
            }
            return SendUsingSelectedTransports(request, selectedKind);
        }
    }

    /// <summary>Polls the next manager or registered transport event.</summary>
    /// <returns>The next event, or an event whose kind is <see cref="SipTransportEventKind.None"/>.</returns>
    public SipTransportEvent PollManagedTransportEvent()
    {
        lock (gate)
        {
            if (events.Count > 0)
            {
                return RemoveManagedEvent();
            }
            int checkedCount = 0;
            while (checkedCount < transports.Count)
            {
                if (nextPollIndex >= transports.Count)
                {
                    nextPollIndex = 0;
                }
                SipManagedTransport registration = transports[nextPollIndex];
                nextPollIndex = nextPollIndex + 1;
                checkedCount = checkedCount + 1;
                if (registration.Transport != null)
                {
                    SipTransportEvent value =
                        registration.Transport.PollTransportEvent();
                    if (value != null
                        && value.Kind != SipTransportEventKind.None)
                    {
                        ProcessManagedEvent(registration, value);
                        return value;
                    }
                }
            }
            return CreateEmptyManagedEvent();
        }
    }

    /// <summary>Closes every registered transport.</summary>
    public void CloseManagedTransports()
    {
        lock (gate)
        {
            if (closed)
            {
                return;
            }
            closed = true;
            int index = 0;
            while (index < transports.Count)
            {
                SipManagedTransport registration = transports[index];
                if (!registration.Closed && registration.Transport != null)
                {
                    registration.Transport.CloseTransport();
                }
                index = index + 1;
            }
        }
    }

    SipSendResult SendUsingSelectedTransports(SipSendRequest request,
        SipTransportKind selectedKind)
    {
        SipSendResult? lastFailure = null;
        bool attempted = false;
        int pass = 0;
        while (pass < 2)
        {
            int index = 0;
            while (index < transports.Count)
            {
                SipManagedTransport registration = transports[index];
                bool matches = selectedKind == SipTransportKind.Unknown
                    ? pass == 0
                    : pass == 0
                        ? registration.Kind == selectedKind
                        : registration.Kind == SipTransportKind.Unknown;
                if (!registration.Closed && matches
                    && registration.Transport != null)
                {
                    attempted = true;
                    SipSendResult result = SendUsingManagedTransport(
                        registration, request, selectedKind, null, null);
                    if (result.Success)
                    {
                        return result;
                    }
                    lastFailure = result;
                }
                index = index + 1;
            }
            pass = pass + 1;
        }
        if (attempted && lastFailure != null)
        {
            return lastFailure;
        }
        return CreateManagedSendFailure(request, selectedKind,
            "No registered SIP transport can satisfy the send request.",
            null, null, true);
    }

    void DiscoverManagedTransportKinds()
    {
        int index = 0;
        while (index < transports.Count
            && events.Count < SipDefaults.MaxQueueCount)
        {
            SipManagedTransport registration = transports[index];
            if (!registration.Closed
                && registration.Kind == SipTransportKind.Unknown
                && registration.Transport != null)
            {
                SipTransportEvent value =
                    registration.Transport.PollTransportEvent();
                if (value != null
                    && value.Kind != SipTransportEventKind.None)
                {
                    ProcessManagedEvent(registration, value);
                    AddManagedEvent(value);
                }
            }
            index = index + 1;
        }
    }

    SipSendResult SendUsingManagedTransport(SipManagedTransport registration,
        SipSendRequest request, SipTransportKind selectedKind,
        SipManagedConnection? managedConnection, SipManagedFlow? managedFlow)
    {
        if (registration.Transport == null)
        {
            return CreateManagedSendFailure(request, selectedKind,
                "The selected SIP transport is unavailable.", null, null, true);
        }
        SipSendRequest normalized = CopyManagedSendRequest(request);
        ApplyManagedAffinity(normalized, selectedKind,
            managedConnection, managedFlow);
        string? codingError = ApplyManagedContentEncoding(normalized);
        if (codingError != null)
        {
            return CreateManagedSendFailure(request, selectedKind,
                codingError, null, null, true);
        }
        SipSendResult result = registration.Transport.SendTransport(normalized);
        if (result == null)
        {
            return CreateManagedSendFailure(request, selectedKind,
                "The selected SIP transport returned no send result.",
                null, null, true);
        }
        LearnManagedTransportKind(registration, selectedKind,
            result.Connection, result.Flow);
        TrackManagedConnection(registration, result.Connection);
        TrackManagedFlow(registration, result.Flow);
        if (result.Success && managedFlow != null && managedFlow.Flow != null
            && result.Flow == null)
        {
            result.Flow = CopyManagedFlow(managedFlow.Flow);
        }
        if (result.Success && managedFlow != null && managedFlow.Flow != null
            && result.Connection == null)
        {
            result.Connection = CopyManagedConnection(
                managedFlow.Flow.Connection);
        }
        if (result.Success && managedConnection != null
            && managedConnection.Connection != null && result.Connection == null)
        {
            result.Connection = CopyManagedConnection(
                managedConnection.Connection);
        }
        if (result.CorrelationId == null)
        {
            result.CorrelationId = request.CorrelationId;
        }
        if (result.ContentCoding == null)
        {
            result.ContentCoding = CopyManagedContentCoding(
                normalized.ContentCoding);
        }
        return result;
    }

    string? ApplyManagedContentEncoding(SipSendRequest request)
    {
        if (request.ContentCoding == null)
        {
            return null;
        }
        ISipTransportContentCoding? contentCoding =
            FindManagedContentCoding(request.ContentCoding.Name);
        if (contentCoding == null)
        {
            return "No registered transport content coding matches the send request.";
        }
        byte[] input = request.Bytes;
        if (input.Length == 0 && request.Message != null)
        {
            SipWriteOptions options = new SipWriteOptions();
            options.Canonical = true;
            options.PreserveOriginal = false;
            options.ReconcileContentLength = true;
            SipWriteResult written = SipMessageFormatter.FormatMessageModel(
                request.Message, options);
            if (!written.Success)
            {
                return written.ErrorMessage == null
                    ? "The SIP message cannot be serialized for content coding."
                    : written.ErrorMessage;
            }
            input = written.Bytes;
        }
        SipContentCodingRequest codingRequest =
            CreateManagedCodingRequest(input, request.ContentCoding,
                request.CorrelationId);
        SipContentCodingResult? coded =
            contentCoding.EncodeTransportContent(codingRequest);
        string? error = ValidateManagedCodingResult(coded);
        if (error != null)
        {
            return error;
        }
        request.Bytes = CopyManagedBytes(coded == null ? null : coded.Bytes);
        request.Message = null;
        request.ContentCoding = CopyManagedContentCoding(
            coded == null || coded.Metadata == null
                ? request.ContentCoding : coded.Metadata);
        return null;
    }

    ISipTransportContentCoding? FindManagedContentCoding(string? name)
    {
        if (!SipAscii.IsToken(name))
        {
            return null;
        }
        int index = 0;
        while (index < contentCodings.Count)
        {
            ISipTransportContentCoding candidate = contentCodings[index];
            if (SipAscii.EqualsIgnoreCase(
                candidate.GetTransportContentCodingName(), name))
            {
                return candidate;
            }
            index = index + 1;
        }
        return null;
    }

    void ApplyManagedAffinity(SipSendRequest request,
        SipTransportKind selectedKind,
        SipManagedConnection? managedConnection, SipManagedFlow? managedFlow)
    {
        SipConnectionInfo? connection = managedFlow == null
            || managedFlow.Flow == null ? null : managedFlow.Flow.Connection;
        if (connection == null && managedConnection != null)
        {
            connection = managedConnection.Connection;
        }
        if (connection != null)
        {
            request.ConnectionId = connection.Id;
            request.LocalAddress = null;
            request.RemoteAddress = CopyManagedEndpoint(connection.RemoteAddress);
            if (connection.Transport != SipTransportKind.Unknown)
            {
                request.Transport = connection.Transport;
                return;
            }
        }
        bool oversizedUdp = IsManagedOversizedUdp(request);
        request.Transport = oversizedUdp ? SipTransportKind.Udp : selectedKind;
        if (request.RemoteAddress != null
            && request.RemoteAddress.Transport == SipTransportKind.Unknown)
        {
            request.RemoteAddress.Transport = selectedKind;
            request.RemoteAddress.Secure = IsManagedSecureTransport(selectedKind);
        }
    }

    SipManagedTransport? FindRegisteredTransport(ISipTransport transport)
    {
        int index = 0;
        while (index < transports.Count)
        {
            SipManagedTransport candidate = transports[index];
            if (candidate.Transport != null
                && object.ReferenceEquals(candidate.Transport, transport))
            {
                return candidate;
            }
            index = index + 1;
        }
        return null;
    }

    SipManagedConnection? FindManagedConnection(string? connectionId)
    {
        if (connectionId == null || connectionId.Length == 0)
        {
            return null;
        }
        int index = 0;
        while (index < connections.Count)
        {
            SipManagedConnection candidate = connections[index];
            if (candidate.Connection != null
                && candidate.Connection.Id == connectionId)
            {
                return candidate;
            }
            index = index + 1;
        }
        return null;
    }

    SipManagedConnection? FindReusableManagedConnection(
        SipTransportKind transport, SipEndpointAddress? remote)
    {
        if (remote == null || remote.Host == null
            || remote.Host.Length == 0 || remote.Port <= 0)
        {
            return null;
        }
        int index = 0;
        while (index < connections.Count)
        {
            SipManagedConnection candidate = connections[index];
            SipConnectionInfo? connection = candidate.Connection;
            if (connection != null
                && connection.State == SipConnectionState.Connected
                && connection.Transport == transport
                && ManagedEndpointsMatch(connection.RemoteAddress, remote))
            {
                return candidate;
            }
            index = index + 1;
        }
        return null;
    }

    SipManagedFlow? FindManagedFlow(string? flowId)
    {
        if (flowId == null || flowId.Length == 0)
        {
            return null;
        }
        int index = 0;
        while (index < flows.Count)
        {
            SipManagedFlow candidate = flows[index];
            if (candidate.Flow != null && candidate.Flow.Id == flowId)
            {
                return candidate;
            }
            index = index + 1;
        }
        return null;
    }

    void ProcessManagedEvent(SipManagedTransport registration,
        SipTransportEvent value)
    {
        if (value.Transport != SipTransportKind.Unknown)
        {
            registration.Kind = value.Transport;
        }
        else if (registration.Kind != SipTransportKind.Unknown)
        {
            value.Transport = registration.Kind;
        }
        if (value.Connection == null && value.Message != null)
        {
            value.Connection = value.Message.Connection;
        }
        if (value.Flow == null && value.Message != null)
        {
            value.Flow = value.Message.Flow;
        }
        LearnManagedTransportKind(registration, value.Transport,
            value.Connection, value.Flow);
        TrackManagedConnection(registration, value.Connection);
        TrackManagedFlow(registration, value.Flow);
        if (value.Message != null
            && value.Message.Transport == SipTransportKind.Unknown)
        {
            value.Message.Transport = registration.Kind;
        }
        ApplyManagedContentDecoding(value);
        if (value.Kind == SipTransportEventKind.Disconnected)
        {
            RemoveManagedConnection(value.Connection);
            FailManagedFlowsForConnection(registration, value.Connection,
                value.ErrorMessage, value.CorrelationId);
        }
        if (value.Kind == SipTransportEventKind.FlowFailed
            && value.Flow != null)
        {
            MarkManagedFlowFailed(value.Flow.Id);
        }
        if (value.Kind == SipTransportEventKind.Closed)
        {
            registration.Closed = true;
            RemoveManagedConnectionsForTransport(registration);
            FailManagedFlowsForTransport(registration, value.ErrorMessage);
        }
    }

    void ApplyManagedContentDecoding(SipTransportEvent value)
    {
        SipTransportMessage? message = value.Message;
        if (value.Kind != SipTransportEventKind.MessageReceived
            || message == null || message.ContentCoding == null)
        {
            return;
        }
        ISipTransportContentCoding? contentCoding =
            FindManagedContentCoding(message.ContentCoding.Name);
        if (contentCoding == null)
        {
            value.Kind = SipTransportEventKind.ContentCodingFailed;
            value.ErrorMessage =
                "No registered transport content coding matches the received message.";
            return;
        }
        SipContentCodingRequest request = CreateManagedCodingRequest(
            message.Bytes, message.ContentCoding, message.CorrelationId);
        SipContentCodingResult? decoded =
            contentCoding.DecodeTransportContent(request);
        string? error = ValidateManagedCodingResult(decoded);
        if (error != null)
        {
            value.Kind = SipTransportEventKind.ContentCodingFailed;
            value.ErrorMessage = error;
            return;
        }
        message.Bytes = CopyManagedBytes(
            decoded == null ? null : decoded.Bytes);
        message.ContentCoding = CopyManagedContentCoding(
            decoded == null || decoded.Metadata == null
                ? message.ContentCoding : decoded.Metadata);
        SipParseOptions options = new SipParseOptions();
        SipParseResult parsed = SipMessageParser.ParseDatagramMessageBytes(
            message.Bytes, options);
        message.ParseResult = parsed;
        message.Message = parsed.Message;
    }

    static SipContentCodingRequest CreateManagedCodingRequest(
        byte[]? bytes, SipContentCodingMetadata? metadata,
        string? correlationId)
    {
        SipContentCodingRequest request = new SipContentCodingRequest();
        request.Bytes = CopyManagedBytes(bytes);
        request.Metadata = CopyManagedContentCoding(metadata);
        request.MaxOutputBytes = SipDefaults.MaxMessageBytes;
        request.CorrelationId = correlationId;
        return request;
    }

    static string? ValidateManagedCodingResult(
        SipContentCodingResult? result)
    {
        if (result == null)
        {
            return "The transport content coding returned no result.";
        }
        if (!result.Success)
        {
            return result.ErrorMessage == null
                ? "The transport content coding failed." : result.ErrorMessage;
        }
        if (result.Bytes == null)
        {
            return "The transport content coding returned no bytes.";
        }
        if (result.Bytes.Length > SipDefaults.MaxMessageBytes)
        {
            return "The transport content coding output exceeds the message limit.";
        }
        return null;
    }

    void LearnManagedTransportKind(SipManagedTransport registration,
        SipTransportKind selectedKind, SipConnectionInfo? connection,
        SipFlow? flow)
    {
        SipTransportKind learned = selectedKind;
        if (connection != null
            && connection.Transport != SipTransportKind.Unknown)
        {
            learned = connection.Transport;
        }
        if (flow != null && flow.Connection != null
            && flow.Connection.Transport != SipTransportKind.Unknown)
        {
            learned = flow.Connection.Transport;
        }
        if (learned != SipTransportKind.Unknown)
        {
            registration.Kind = learned;
        }
    }

    void TrackManagedConnection(SipManagedTransport registration,
        SipConnectionInfo? connection)
    {
        if (connection == null || connection.Id == null
            || connection.Id.Length == 0)
        {
            return;
        }
        SipManagedConnection? existing = FindManagedConnection(connection.Id);
        if (connection.State == SipConnectionState.Closed
            || connection.State == SipConnectionState.Failed)
        {
            RemoveManagedConnection(connection);
            return;
        }
        if (existing != null)
        {
            existing.Transport = registration;
            existing.Connection = CopyManagedConnection(connection);
            return;
        }
        if (connections.Count >= SipDefaults.MaxConnectionCount)
        {
            return;
        }
        SipManagedConnection added = new SipManagedConnection();
        added.Transport = registration;
        added.Connection = CopyManagedConnection(connection);
        connections.Add(added);
    }

    void TrackManagedFlow(SipManagedTransport registration, SipFlow? flow)
    {
        if (flow == null || flow.Id == null || flow.Id.Length == 0)
        {
            return;
        }
        TrackManagedConnection(registration, flow.Connection);
        SipManagedFlow? existing = FindManagedFlow(flow.Id);
        if (existing != null)
        {
            existing.Transport = registration;
            existing.Flow = CopyManagedFlow(flow);
            return;
        }
        if (flows.Count >= SipDefaults.MaxConnectionCount)
        {
            return;
        }
        SipManagedFlow added = new SipManagedFlow();
        added.Transport = registration;
        added.Flow = CopyManagedFlow(flow);
        flows.Add(added);
    }

    void RemoveManagedConnection(SipConnectionInfo? connection)
    {
        string? connectionId = connection == null ? null : connection.Id;
        if (connectionId == null || connectionId.Length == 0)
        {
            return;
        }
        int index = connections.Count - 1;
        while (index >= 0)
        {
            SipManagedConnection candidate = connections[index];
            if (candidate.Connection != null
                && candidate.Connection.Id == connectionId)
            {
                connections.RemoveAt(index);
            }
            index = index - 1;
        }
    }

    void RemoveManagedConnectionsForTransport(
        SipManagedTransport registration)
    {
        int index = connections.Count - 1;
        while (index >= 0)
        {
            if (object.ReferenceEquals(
                connections[index].Transport, registration))
            {
                connections.RemoveAt(index);
            }
            index = index - 1;
        }
    }

    void FailManagedFlowsForConnection(SipManagedTransport registration,
        SipConnectionInfo? connection, string? message, string? correlationId)
    {
        string? connectionId = connection == null ? null : connection.Id;
        if (connectionId == null || connectionId.Length == 0)
        {
            return;
        }
        int index = 0;
        while (index < flows.Count)
        {
            SipManagedFlow candidate = flows[index];
            SipConnectionInfo? associated = candidate.Flow == null
                ? null : candidate.Flow.Connection;
            if (candidate.Flow != null && associated != null
                && associated.Id == connectionId && !candidate.Flow.Failed)
            {
                candidate.Flow.Failed = true;
                AddManagedFlowFailure(registration, candidate.Flow,
                    message, correlationId);
            }
            index = index + 1;
        }
    }

    void FailManagedFlowsForTransport(SipManagedTransport registration,
        string? message)
    {
        int index = 0;
        while (index < flows.Count)
        {
            SipManagedFlow candidate = flows[index];
            if (candidate.Flow != null && !candidate.Flow.Failed
                && object.ReferenceEquals(candidate.Transport, registration))
            {
                candidate.Flow.Failed = true;
                AddManagedFlowFailure(registration, candidate.Flow,
                    message, null);
            }
            index = index + 1;
        }
    }

    void MarkManagedFlowFailed(string? flowId)
    {
        SipManagedFlow? managed = FindManagedFlow(flowId);
        if (managed != null && managed.Flow != null)
        {
            managed.Flow.Failed = true;
        }
    }

    void AddManagedFlowFailure(SipManagedTransport registration,
        SipFlow flow, string? message, string? correlationId)
    {
        SipTransportEvent failure = new SipTransportEvent();
        failure.Kind = SipTransportEventKind.FlowFailed;
        failure.Transport = registration.Kind;
        failure.Connection = CopyManagedConnection(flow.Connection);
        failure.Flow = CopyManagedFlow(flow);
        failure.ErrorMessage = message == null
            ? "The outbound flow transport failed." : message;
        failure.CorrelationId = correlationId;
        AddManagedEvent(failure);
    }

    SipSendResult CreateManagedSendFailure(SipSendRequest? request,
        SipTransportKind transport, string message,
        SipConnectionInfo? connection, SipFlow? flow, bool queueEvent)
    {
        SipSendResult result = new SipSendResult();
        result.Connection = CopyManagedConnection(connection);
        result.Flow = CopyManagedFlow(flow);
        result.ErrorMessage = message;
        result.CorrelationId = request == null ? null : request.CorrelationId;
        result.ContentCoding = request == null ? null
            : CopyManagedContentCoding(request.ContentCoding);
        if (queueEvent)
        {
            SipTransportEvent failure = new SipTransportEvent();
            failure.Kind = SipTransportEventKind.SendFailed;
            failure.Transport = transport;
            failure.Connection = CopyManagedConnection(connection);
            failure.Flow = CopyManagedFlow(flow);
            failure.ErrorMessage = message;
            failure.CorrelationId = result.CorrelationId;
            AddManagedEvent(failure);
        }
        return result;
    }

    bool AddManagedEvent(SipTransportEvent value)
    {
        if (events.Count >= SipDefaults.MaxQueueCount)
        {
            return false;
        }
        events.Add(value);
        return true;
    }

    SipTransportEvent RemoveManagedEvent()
    {
        SipTransportEvent result = events[0];
        events.RemoveAt(0);
        return result;
    }

    static SipTransportEvent CreateEmptyManagedEvent()
    {
        return new SipTransportEvent();
    }

    static SipTransportKind FindRequestedTransportKind(
        SipSendRequest? request)
    {
        if (request == null)
        {
            return SipTransportKind.Unknown;
        }
        if (request.Transport != SipTransportKind.Unknown)
        {
            return request.Transport;
        }
        if (request.RemoteAddress != null
            && request.RemoteAddress.Transport != SipTransportKind.Unknown)
        {
            return request.RemoteAddress.Transport;
        }
        return request.RemoteAddress != null && request.RemoteAddress.Secure
            ? SipTransportKind.Tls : SipTransportKind.Unknown;
    }

    static SipTransportKind SelectTransportKind(SipSendRequest request,
        SipManagedFlow? managedFlow,
        SipManagedConnection? managedConnection)
    {
        if (managedFlow != null && managedFlow.Flow != null)
        {
            SipTransportKind flowKind =
                FindFlowTransportKind(managedFlow.Flow);
            if (flowKind != SipTransportKind.Unknown)
            {
                return flowKind;
            }
        }
        if (managedConnection != null
            && managedConnection.Connection != null
            && managedConnection.Connection.Transport
                != SipTransportKind.Unknown)
        {
            return managedConnection.Connection.Transport;
        }
        SipTransportKind requested = FindRequestedTransportKind(request);
        if (requested == SipTransportKind.Unknown
            && (request.ConnectionId != null
                    && request.ConnectionId.Length > 0
                || request.FlowId != null && request.FlowId.Length > 0)
            && request.RemoteAddress == null)
        {
            return SipTransportKind.Unknown;
        }
        if (requested == SipTransportKind.Unknown)
        {
            requested = ManagedRequestByteCount(request)
                > SipDefaults.UdpSwitchThresholdBytes
                ? SipTransportKind.Tcp : SipTransportKind.Udp;
        }
        if (requested == SipTransportKind.Udp
            && IsManagedOversizedUdp(request))
        {
            return SipTransportKind.Tcp;
        }
        return requested;
    }

    static SipTransportKind FindFlowTransportKind(SipFlow flow)
    {
        return flow.Connection == null
            ? SipTransportKind.Unknown : flow.Connection.Transport;
    }

    static int ManagedRequestByteCount(SipSendRequest request)
    {
        if (request.Bytes != null && request.Bytes.Length > 0)
        {
            return request.Bytes.Length;
        }
        if (request.Message == null)
        {
            return 0;
        }
        SipWriteOptions options = new SipWriteOptions();
        options.Canonical = true;
        options.PreserveOriginal = false;
        options.ReconcileContentLength = true;
        SipWriteResult written = SipMessageFormatter.FormatMessageModel(
            request.Message, options);
        return written.Success ? written.RequiredBytes : 0;
    }

    static bool IsManagedOversizedUdp(SipSendRequest request)
    {
        SipTransportKind requested = FindRequestedTransportKind(request);
        return requested == SipTransportKind.Udp
            && ManagedRequestByteCount(request)
                > SipDefaults.UdpSwitchThresholdBytes;
    }

    static bool IsManagedSecureTransport(SipTransportKind transport)
    {
        return transport == SipTransportKind.Tls
            || transport == SipTransportKind.SecureWebSocket
            || transport == SipTransportKind.Dtls;
    }

    static bool ManagedSendRequiresSecurity(SipSendRequest request)
    {
        if (request.RemoteAddress != null && request.RemoteAddress.Secure)
        {
            return true;
        }
        SipMessage? message = request.Message;
        if (message == null || message.Kind != SipMessageKind.Request)
        {
            return false;
        }
        if (message.RequestUri != null
            && message.RequestUri.Scheme == SipUriScheme.Sips)
        {
            return true;
        }
        if (message.RouteValues.Length > 0)
        {
            SipNameAddress? address = message.RouteValues[0].Address;
            SipUri? route = address == null ? null : address.Uri;
            if (route != null && route.Scheme == SipUriScheme.Sips)
            {
                return true;
            }
        }
        return false;
    }

    static bool ManagedEndpointsMatch(SipEndpointAddress? left,
        SipEndpointAddress? right)
    {
        return left != null && right != null
            && left.Host != null && right.Host != null
            && SipAscii.EqualsIgnoreCase(left.Host, right.Host)
            && left.Port == right.Port
            && left.Secure == right.Secure;
    }

    static SipSendRequest CopyManagedSendRequest(SipSendRequest source)
    {
        SipSendRequest result = new SipSendRequest();
        result.Message = source.Message;
        result.Bytes = CopyManagedBytes(source.Bytes);
        result.RemoteAddress = CopyManagedEndpoint(source.RemoteAddress);
        result.LocalAddress = CopyManagedEndpoint(source.LocalAddress);
        result.Transport = source.Transport;
        result.ConnectionId = source.ConnectionId;
        result.FlowId = source.FlowId;
        result.DeadlineMilliseconds = source.DeadlineMilliseconds;
        result.Cancellation = source.Cancellation;
        result.CorrelationId = source.CorrelationId;
        result.ContentCoding = CopyManagedContentCoding(source.ContentCoding);
        return result;
    }

    static SipContentCodingMetadata? CopyManagedContentCoding(
        SipContentCodingMetadata? source)
    {
        if (source == null)
        {
            return null;
        }
        SipContentCodingMetadata result = new SipContentCodingMetadata();
        result.Name = source.Name;
        result.SigCompCompartment = CopyManagedCompartmentOrNull(
            source.SigCompCompartment);
        return result;
    }

    static SipSigCompCompartment? CopyManagedCompartmentOrNull(
        SipSigCompCompartment? source)
    {
        return source == null ? null : CopyManagedCompartment(source);
    }

    static SipSigCompCompartment CopyManagedCompartment(
        SipSigCompCompartment source)
    {
        SipSigCompCompartment result = new SipSigCompCompartment();
        result.Id = source.Id;
        result.RemoteCapability = CopyManagedSigCompCapability(
            source.RemoteCapability);
        result.RemoteAddress = CopyManagedEndpoint(source.RemoteAddress);
        result.Transport = source.Transport;
        result.ConnectionId = source.ConnectionId;
        result.FlowId = source.FlowId;
        result.Closed = source.Closed;
        return result;
    }

    static SipSigCompCapability? CopyManagedSigCompCapability(
        SipSigCompCapability? source)
    {
        if (source == null)
        {
            return null;
        }
        SipSigCompCapability result = new SipSigCompCapability();
        result.Version = source.Version;
        result.NegativeAcknowledgements = source.NegativeAcknowledgements;
        result.StateMemorySize = source.StateMemorySize;
        result.CyclesPerBit = source.CyclesPerBit;
        result.DecompressionMemorySize = source.DecompressionMemorySize;
        byte[][] sourceIdentifiers = source.StateIdentifiers == null
            ? new byte[0][] : source.StateIdentifiers;
        result.StateIdentifiers = new byte[sourceIdentifiers.Length][];
        int index = 0;
        while (index < sourceIdentifiers.Length)
        {
            result.StateIdentifiers[index] =
                CopyManagedBytes(sourceIdentifiers[index]);
            index = index + 1;
        }
        return result;
    }

    static byte[] CopyManagedBytes(byte[]? source)
    {
        if (source == null)
        {
            return new byte[0];
        }
        byte[] result = new byte[source.Length];
        System.Array.Copy(source, result, source.Length);
        return result;
    }

    static SipEndpointAddress? CopyManagedEndpoint(
        SipEndpointAddress? source)
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

    static SipConnectionInfo? CopyManagedConnection(
        SipConnectionInfo? source)
    {
        if (source == null)
        {
            return null;
        }
        SipFlow holder = new SipFlow();
        holder.Connection = source;
        SipFlow? copied = SipSnapshotCopy.CopyFlowModel(holder);
        return copied == null ? null : copied.Connection;
    }

    static SipFlow? CopyManagedFlow(SipFlow? source)
    {
        return SipSnapshotCopy.CopyFlowModel(source);
    }
}
