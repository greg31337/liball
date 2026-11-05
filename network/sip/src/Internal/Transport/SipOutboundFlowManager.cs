namespace sip;

internal class SipOutboundFlowManager
{
    ISipMonotonicClock clock;
    ISipRandomSource random;
    SipFlowTokenCodec tokenCodec;
    int maximumFlows;
    long nextSequence;
    System.Collections.Generic.List<SipOutboundFlowEntry> flows =
        new System.Collections.Generic.List<SipOutboundFlowEntry>();

    internal SipOutboundFlowManager(ISipMonotonicClock monotonicClock,
        ISipRandomSource randomSource, int maximumFlowCount)
    {
        if (monotonicClock == null || randomSource == null
            || maximumFlowCount <= 0)
        {
            throw new System.ArgumentException(
                "The outbound flow manager options are invalid.",
                "maximumFlowCount");
        }
        byte[] secret = new byte[32];
        randomSource.FillBytes(secret);
        clock = monotonicClock;
        random = randomSource;
        tokenCodec = new SipFlowTokenCodec(secret);
        System.Security.Cryptography.CryptographicOperations.ZeroMemory(secret);
        maximumFlows = maximumFlowCount;
    }

    internal SipFlow RegisterFlow(SipContactValue? contact,
        SipConnectionInfo? connection, SipRouteValue[]? path,
        int flowTimerSeconds, long expiresAtMilliseconds, bool primary)
    {
        if (!ValidRegistration(contact, connection, path, flowTimerSeconds,
            expiresAtMilliseconds) || contact == null || connection == null
            || contact.InstanceId == null || path == null)
        {
            throw new System.ArgumentException(
                "A valid outbound registration flow is required.", "contact");
        }
        int replacement = FindRegistration(contact.InstanceId,
            contact.RegistrationId);
        if (replacement < 0 && flows.Count >= maximumFlows)
        {
            throw new System.InvalidOperationException(
                "The outbound flow registry is full.");
        }
        bool selectedPrimary = primary
            || !HasActiveFlow(contact.InstanceId, replacement);
        if (selectedPrimary)
        {
            DemoteFlows(contact.InstanceId, replacement);
        }
        SipFlow flow = new SipFlow();
        flow.InstanceId = contact.InstanceId;
        flow.RegistrationId = contact.RegistrationId;
        flow.Connection = CopyConnection(connection);
        flow.Path = CopyRoutes(path);
        flow.Primary = selectedPrimary;
        flow.Failed = false;
        flow.FlowTimerSeconds = flowTimerSeconds;
        flow.ExpiresAtMilliseconds = expiresAtMilliseconds;
        flow.Id = tokenCodec.CreateToken(connection, contact.InstanceId,
            contact.RegistrationId);
        ScheduleKeepAlive(flow);
        SipOutboundFlowEntry entry = new SipOutboundFlowEntry();
        entry.Flow = flow;
        if (replacement >= 0)
        {
            entry.Sequence = flows[replacement].Sequence;
            flows[replacement] = entry;
        }
        else
        {
            entry.Sequence = nextSequence;
            nextSequence = nextSequence + 1;
            flows.Add(entry);
        }
        return CopyFlow(flow);
    }

    internal SipFlow? FindFlow(string? flowId)
    {
        SipOutboundFlowEntry? entry = FindEntry(flowId);
        return entry == null ? null : CopyFlow(entry.Flow);
    }

    internal SipFlow? SelectFlow(string? instanceId)
    {
        SipOutboundFlowEntry? selected = SelectEntry(instanceId, null);
        return selected == null ? null : CopyFlow(selected.Flow);
    }

    internal SipFlow? SelectAlternateFlow(string? instanceId,
        string? excludedFlowId)
    {
        SipOutboundFlowEntry? selected = SelectEntry(instanceId,
            excludedFlowId);
        return selected == null ? null : CopyFlow(selected.Flow);
    }

    internal SipRouteValue CreatePathRoute(string? flowId, SipUri? edgeUri)
    {
        SipOutboundFlowEntry? entry = FindEntry(flowId);
        if (entry == null || edgeUri == null || edgeUri.Host == null
            || edgeUri.Host.Length == 0
            || edgeUri.Scheme != SipUriScheme.Sip
                && edgeUri.Scheme != SipUriScheme.Sips
            || SipUriValidator.ValidateUriModel(edgeUri).Length > 0
            || !UriTransportMatches(edgeUri, entry.Flow.Connection))
        {
            throw new System.ArgumentException(
                "The flow and edge URI are not transport-compatible.", "edgeUri");
        }
        SipRouteValue route = new SipRouteValue();
        route.Address = new SipNameAddress();
        route.Address.Uri = CopyUri(edgeUri);
        route.Address.Uri.User = entry.Flow.Id;
        route.Address.Uri.Parameters = AddValuelessParameter(
            route.Address.Uri.Parameters, "lr");
        route.Address.Uri.Parameters = AddValuelessParameter(
            route.Address.Uri.Parameters, "ob");
        route.LooseRouting = true;
        return route;
    }

    internal SipFlow? ResolvePathFlow(SipRouteValue? route)
    {
        SipUri? uri = route == null || route.Address == null
            ? null : route.Address.Uri;
        if (route == null || !route.LooseRouting || uri == null
            || SipUriValidator.ValidateUriModel(uri).Length > 0
            || !HasSingleValuelessParameter(uri.Parameters, "lr")
            || !HasSingleValuelessParameter(uri.Parameters, "ob"))
        {
            return null;
        }
        SipOutboundFlowEntry? entry = FindEntry(uri.User);
        if (entry == null || !UriTransportMatches(uri, entry.Flow.Connection))
        {
            return null;
        }
        return CopyFlow(entry.Flow);
    }

    internal int ReadFlowTimer(SipMessage? response)
    {
        if (response == null || response.Headers == null)
        {
            throw new System.ArgumentNullException("response");
        }
        int value = -1;
        int index = 0;
        while (index < response.Headers.Length)
        {
            SipHeader? header = response.Headers[index];
            if (header != null && SipAscii.EqualsIgnoreCase(
                header.Name, SipHeaderNames.FlowTimer))
            {
                SipNumericParseResult parsed =
                    SipNumericParser.ParseNonNegativeInt32Text(
                        header.Value, int.MaxValue);
                if (value >= 0 || !parsed.Success)
                {
                    throw new SipProtocolException(
                        "Flow-Timer must be one non-negative decimal value.",
                        400, null);
                }
                value = parsed.Value;
            }
            index = index + 1;
        }
        return value;
    }

    internal SipFlow? UpdateFlowTimer(string? flowId, int flowTimerSeconds)
    {
        SipOutboundFlowEntry? entry = FindEntry(flowId);
        if (entry == null || flowTimerSeconds < -1)
        {
            return null;
        }
        entry.Flow.FlowTimerSeconds = flowTimerSeconds;
        entry.Flow.KeepAliveResponseDeadlineMilliseconds = -1;
        ScheduleKeepAlive(entry.Flow);
        return CopyFlow(entry.Flow);
    }

    internal SipFlow[] StartDueKeepAlives()
    {
        ExpireKeepAliveTimeouts();
        long now = clock.GetMilliseconds();
        System.Collections.Generic.List<SipFlow> result =
            new System.Collections.Generic.List<SipFlow>();
        int index = 0;
        while (index < flows.Count)
        {
            SipFlow flow = flows[index].Flow;
            if (IsActiveFlow(flow) && SupportsCrlfKeepAlive(flow.Connection)
                && flow.KeepAliveResponseDeadlineMilliseconds < 0
                && flow.NextKeepAliveAtMilliseconds >= 0
                && now >= flow.NextKeepAliveAtMilliseconds)
            {
                flow.NextKeepAliveAtMilliseconds = -1;
                flow.KeepAliveResponseDeadlineMilliseconds = AddMilliseconds(
                    now, SipDefaults.OutboundPongTimeoutMilliseconds);
                result.Add(CopyFlow(flow));
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    internal bool AcknowledgeKeepAlive(string? flowId)
    {
        SipOutboundFlowEntry? entry = FindEntry(flowId);
        if (entry == null || entry.Flow.Failed
            || entry.Flow.KeepAliveResponseDeadlineMilliseconds < 0)
        {
            return false;
        }
        entry.Flow.KeepAliveResponseDeadlineMilliseconds = -1;
        ScheduleKeepAlive(entry.Flow);
        return true;
    }

    internal SipFlow[] ExpireKeepAliveTimeouts()
    {
        long now = clock.GetMilliseconds();
        System.Collections.Generic.List<SipFlow> result =
            new System.Collections.Generic.List<SipFlow>();
        int index = 0;
        while (index < flows.Count)
        {
            SipFlow flow = flows[index].Flow;
            if (!flow.Failed && flow.KeepAliveResponseDeadlineMilliseconds >= 0
                && now >= flow.KeepAliveResponseDeadlineMilliseconds)
            {
                FailEntry(flows[index]);
                result.Add(CopyFlow(flow));
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    internal SipFlow? MarkFlowFailed(string? flowId)
    {
        SipOutboundFlowEntry? entry = FindEntry(flowId);
        if (entry == null)
        {
            return null;
        }
        FailEntry(entry);
        return CopyFlow(entry.Flow);
    }

    internal SipFlow? RecordRecoveryFailure(string? flowId)
    {
        SipOutboundFlowEntry? entry = FindEntry(flowId);
        if (entry == null)
        {
            return null;
        }
        if (!entry.Flow.Failed)
        {
            FailEntry(entry);
        }
        else
        {
            if (entry.Flow.ConsecutiveFailures < 31)
            {
                entry.Flow.ConsecutiveFailures =
                    entry.Flow.ConsecutiveFailures + 1;
            }
            ScheduleRecovery(entry.Flow);
        }
        return CopyFlow(entry.Flow);
    }

    internal SipFlow[] MarkConnectionFailed(string? connectionId)
    {
        System.Collections.Generic.List<SipFlow> result =
            new System.Collections.Generic.List<SipFlow>();
        int index = 0;
        while (index < flows.Count)
        {
            SipConnectionInfo? connection = flows[index].Flow.Connection;
            if (connection != null && connection.Id == connectionId
                && !flows[index].Flow.Failed)
            {
                FailEntry(flows[index]);
                result.Add(CopyFlow(flows[index].Flow));
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    internal SipFlow[] ApplyTransportEvent(SipTransportEvent? transportEvent)
    {
        if (transportEvent == null || transportEvent.Connection == null)
        {
            return new SipFlow[0];
        }
        if (transportEvent.Kind == SipTransportEventKind.Disconnected
            || transportEvent.Kind == SipTransportEventKind.FlowFailed)
        {
            return MarkConnectionFailed(transportEvent.Connection.Id);
        }
        if (transportEvent.Kind != SipTransportEventKind.KeepAliveReceived)
        {
            return new SipFlow[0];
        }
        System.Collections.Generic.List<SipFlow> result =
            new System.Collections.Generic.List<SipFlow>();
        int index = 0;
        while (index < flows.Count)
        {
            SipFlow flow = flows[index].Flow;
            if (flow.Connection != null
                && flow.Connection.Id == transportEvent.Connection.Id
                && AcknowledgeKeepAlive(flow.Id))
            {
                result.Add(CopyFlow(flow));
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    internal SipFlow[] GetRetryableFlows()
    {
        long now = clock.GetMilliseconds();
        System.Collections.Generic.List<SipFlow> result =
            new System.Collections.Generic.List<SipFlow>();
        int index = 0;
        while (index < flows.Count)
        {
            SipFlow flow = flows[index].Flow;
            if (flow.Failed && flow.RetryAtMilliseconds >= 0
                && now >= flow.RetryAtMilliseconds
                && (flow.ExpiresAtMilliseconds < 0
                    || now < flow.ExpiresAtMilliseconds))
            {
                result.Add(CopyFlow(flow));
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    internal SipFlow[] RemoveExpiredFlows()
    {
        long now = clock.GetMilliseconds();
        System.Collections.Generic.List<SipFlow> result =
            new System.Collections.Generic.List<SipFlow>();
        int index = flows.Count - 1;
        while (index >= 0)
        {
            SipFlow flow = flows[index].Flow;
            if (flow.ExpiresAtMilliseconds >= 0
                && now >= flow.ExpiresAtMilliseconds)
            {
                result.Add(CopyFlow(flow));
                flows.RemoveAt(index);
            }
            index = index - 1;
        }
        ReverseFlows(result);
        return result.ToArray();
    }

    internal SipFlow? RemoveFlow(string? flowId)
    {
        SipOutboundFlowEntry? entry = FindEntry(flowId);
        if (entry == null)
        {
            return null;
        }
        SipFlow removed = CopyFlow(entry.Flow);
        int index = 0;
        while (index < flows.Count)
        {
            if (object.ReferenceEquals(flows[index], entry))
            {
                flows.RemoveAt(index);
                return removed;
            }
            index = index + 1;
        }
        return null;
    }

    internal SipFlow[] CopyFlows()
    {
        SipFlow[] result = new SipFlow[flows.Count];
        int index = 0;
        while (index < flows.Count)
        {
            result[index] = CopyFlow(flows[index].Flow);
            index = index + 1;
        }
        return result;
    }

    internal int CountFlows()
    {
        return flows.Count;
    }

    SipOutboundFlowEntry? SelectEntry(string? instanceId,
        string? excludedFlowId)
    {
        if (!ValidInstanceId(instanceId))
        {
            return null;
        }
        SipOutboundFlowEntry? backup = null;
        int index = 0;
        while (index < flows.Count)
        {
            SipOutboundFlowEntry candidate = flows[index];
            if (SameInstance(candidate.Flow.InstanceId, instanceId)
                && candidate.Flow.Id != excludedFlowId
                && IsActiveFlow(candidate.Flow))
            {
                if (candidate.Flow.Primary)
                {
                    return candidate;
                }
                if (backup == null || candidate.Sequence < backup.Sequence)
                {
                    backup = candidate;
                }
            }
            index = index + 1;
        }
        return backup;
    }

    SipOutboundFlowEntry? FindEntry(string? flowId)
    {
        if (flowId == null || flowId.Length == 0)
        {
            return null;
        }
        int index = 0;
        while (index < flows.Count)
        {
            SipFlow flow = flows[index].Flow;
            if (flow.Id == flowId && flow.Connection != null
                && flow.InstanceId != null
                && tokenCodec.MatchesToken(flowId, flow.Connection,
                    flow.InstanceId, flow.RegistrationId))
            {
                return flows[index];
            }
            index = index + 1;
        }
        return null;
    }

    int FindRegistration(string instanceId, int registrationId)
    {
        int index = 0;
        while (index < flows.Count)
        {
            SipFlow flow = flows[index].Flow;
            if (flow.RegistrationId == registrationId
                && SameInstance(flow.InstanceId, instanceId))
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    bool HasActiveFlow(string instanceId, int excludedIndex)
    {
        int index = 0;
        while (index < flows.Count)
        {
            if (index != excludedIndex
                && SameInstance(flows[index].Flow.InstanceId, instanceId)
                && IsActiveFlow(flows[index].Flow))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    void DemoteFlows(string instanceId, int excludedIndex)
    {
        int index = 0;
        while (index < flows.Count)
        {
            if (index != excludedIndex
                && SameInstance(flows[index].Flow.InstanceId, instanceId))
            {
                flows[index].Flow.Primary = false;
            }
            index = index + 1;
        }
    }

    void ScheduleKeepAlive(SipFlow flow)
    {
        if (!SupportsCrlfKeepAlive(flow.Connection) || flow.Failed)
        {
            flow.NextKeepAliveAtMilliseconds = -1;
            return;
        }
        int seconds = flow.FlowTimerSeconds >= 0
            ? flow.FlowTimerSeconds
            : SipDefaults.OutboundReliableKeepAliveSeconds;
        int percentage = NextInclusive(80, 100);
        long interval = (long)seconds * 1000 * percentage / 100;
        flow.NextKeepAliveAtMilliseconds = AddMilliseconds(
            clock.GetMilliseconds(), interval);
    }

    void FailEntry(SipOutboundFlowEntry entry)
    {
        SipFlow flow = entry.Flow;
        if (flow.Failed)
        {
            return;
        }
        flow.Failed = true;
        flow.NextKeepAliveAtMilliseconds = -1;
        flow.KeepAliveResponseDeadlineMilliseconds = -1;
        if (flow.ConsecutiveFailures < 31)
        {
            flow.ConsecutiveFailures = flow.ConsecutiveFailures + 1;
        }
        ScheduleRecovery(flow);
    }

    void ScheduleRecovery(SipFlow flow)
    {
        bool allFailed = AllFlowsFailed(flow);
        int baseSeconds = allFailed
            ? SipDefaults.OutboundAllFailedRetrySeconds
            : SipDefaults.OutboundPartialFailureRetrySeconds;
        long upper = baseSeconds;
        int count = 0;
        while (count < flow.ConsecutiveFailures
            && upper < SipDefaults.OutboundMaximumRetrySeconds)
        {
            upper = upper > SipDefaults.OutboundMaximumRetrySeconds / 2
                ? SipDefaults.OutboundMaximumRetrySeconds : upper * 2;
            count = count + 1;
        }
        if (upper > SipDefaults.OutboundMaximumRetrySeconds)
        {
            upper = SipDefaults.OutboundMaximumRetrySeconds;
        }
        int minimum = (int)((upper + 1) / 2);
        int delaySeconds = NextInclusive(minimum, (int)upper);
        flow.RetryAtMilliseconds = AddMilliseconds(clock.GetMilliseconds(),
            (long)delaySeconds * 1000);
    }

    bool AllFlowsFailed(SipFlow failedFlow)
    {
        int index = 0;
        while (index < flows.Count)
        {
            SipFlow flow = flows[index].Flow;
            if (flow.Id != failedFlow.Id
                && SameInstance(flow.InstanceId, failedFlow.InstanceId)
                && IsActiveFlow(flow))
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    int NextInclusive(int minimum, int maximum)
    {
        if (minimum >= maximum)
        {
            return minimum;
        }
        byte[] bytes = new byte[4];
        random.FillBytes(bytes);
        uint value = (uint)bytes[0] | (uint)bytes[1] << 8
            | (uint)bytes[2] << 16 | (uint)bytes[3] << 24;
        uint range = (uint)(maximum - minimum + 1);
        return minimum + (int)(value % range);
    }

    static bool ValidRegistration(SipContactValue? contact,
        SipConnectionInfo? connection, SipRouteValue[]? path,
        int flowTimerSeconds, long expiresAtMilliseconds)
    {
        if (contact == null || contact.Wildcard
            || !ValidInstanceId(contact.InstanceId)
            || contact.RegistrationId <= 0 || contact.Address == null
            || contact.Address.Uri == null
            || contact.Address.Uri.Scheme != SipUriScheme.Sip
                && contact.Address.Uri.Scheme != SipUriScheme.Sips
            || SipUriValidator.ValidateUriModel(contact.Address.Uri).Length > 0
            || connection == null
            || connection.Id == null || connection.Id.Length == 0
            || connection.State != SipConnectionState.Connected
            || connection.Secure != SecureTransport(connection.Transport)
            || connection.LocalAddress == null || connection.RemoteAddress == null
            || !ValidEndpointAssociation(connection.LocalAddress,
                connection.Transport, connection.Secure)
            || !ValidEndpointAssociation(connection.RemoteAddress,
                connection.Transport, connection.Secure)
            || !ReliableTransport(connection.Transport)
            || flowTimerSeconds < -1 || expiresAtMilliseconds < 0
            || path == null)
        {
            return false;
        }
        int index = 0;
        while (index < path.Length)
        {
            SipRouteValue? route = path[index];
            SipUri? uri = route == null || route.Address == null
                ? null : route.Address.Uri;
            if (uri == null || uri.Scheme != SipUriScheme.Sip
                    && uri.Scheme != SipUriScheme.Sips
                || SipUriValidator.ValidateUriModel(uri).Length > 0)
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    static bool ValidEndpointAssociation(SipEndpointAddress endpoint,
        SipTransportKind transport, bool secure)
    {
        return endpoint.Host != null && endpoint.Host.Length > 0
            && endpoint.Port > 0 && endpoint.Port <= 65535
            && (endpoint.Transport == SipTransportKind.Unknown
                || endpoint.Transport == transport)
            && endpoint.Secure == secure;
    }

    static bool ValidInstanceId(string? value)
    {
        if (value == null || value.Length != 47 || value[0] != '<'
            || value[46] != '>' || !value.Substring(1, 9).Equals(
                "urn:uuid:", System.StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }
        int index = 10;
        while (index < 46)
        {
            int offset = index - 10;
            if (offset == 8 || offset == 13 || offset == 18 || offset == 23)
            {
                if (value[index] != '-')
                {
                    return false;
                }
            }
            else if (!HexCharacter(value[index]))
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    static bool HexCharacter(char value)
    {
        return value >= '0' && value <= '9'
            || value >= 'a' && value <= 'f'
            || value >= 'A' && value <= 'F';
    }

    static bool SameInstance(string? left, string? right)
    {
        return left != null && right != null && left.Equals(right,
            System.StringComparison.OrdinalIgnoreCase);
    }

    static bool ReliableTransport(SipTransportKind transport)
    {
        return transport == SipTransportKind.Tcp
            || transport == SipTransportKind.Tls
            || transport == SipTransportKind.WebSocket
            || transport == SipTransportKind.SecureWebSocket
            || transport == SipTransportKind.Sctp;
    }

    static bool SecureTransport(SipTransportKind transport)
    {
        return transport == SipTransportKind.Tls
            || transport == SipTransportKind.SecureWebSocket;
    }

    static bool SupportsCrlfKeepAlive(SipConnectionInfo? connection)
    {
        return connection != null
            && (connection.Transport == SipTransportKind.Tcp
                || connection.Transport == SipTransportKind.Tls
                || connection.Transport == SipTransportKind.Sctp);
    }

    bool IsActiveFlow(SipFlow flow)
    {
        return !flow.Failed && flow.Connection != null
            && flow.Connection.State == SipConnectionState.Connected
            && (flow.ExpiresAtMilliseconds < 0
                || clock.GetMilliseconds() < flow.ExpiresAtMilliseconds);
    }

    static bool UriTransportMatches(SipUri uri,
        SipConnectionInfo? connection)
    {
        if (connection == null
            || uri.Scheme == SipUriScheme.Sips && !connection.Secure)
        {
            return false;
        }
        string? supplied = null;
        int index = 0;
        while (index < uri.Parameters.Length)
        {
            SipUriParameter? parameter = uri.Parameters[index];
            if (parameter != null && parameter.HasValue
                && SipAscii.EqualsIgnoreCase(parameter.Name, "transport"))
            {
                if (supplied != null)
                {
                    return false;
                }
                supplied = parameter.Value;
            }
            index = index + 1;
        }
        return supplied == null || SipAscii.EqualsIgnoreCase(
            supplied, TransportToken(connection.Transport));
    }

    static bool HasSingleValuelessParameter(SipUriParameter[]? parameters,
        string name)
    {
        if (parameters == null)
        {
            return false;
        }
        int count = 0;
        int index = 0;
        while (index < parameters.Length)
        {
            SipUriParameter? parameter = parameters[index];
            if (parameter != null && SipAscii.EqualsIgnoreCase(
                parameter.Name, name))
            {
                if (parameter.HasValue)
                {
                    return false;
                }
                count = count + 1;
            }
            index = index + 1;
        }
        return count == 1;
    }

    static string? TransportToken(SipTransportKind transport)
    {
        if (transport == SipTransportKind.Tcp)
        {
            return SipTransportNames.Tcp;
        }
        if (transport == SipTransportKind.Tls)
        {
            return SipTransportNames.Tls;
        }
        if (transport == SipTransportKind.WebSocket)
        {
            return SipTransportNames.WebSocket;
        }
        if (transport == SipTransportKind.SecureWebSocket)
        {
            return SipTransportNames.SecureWebSocket;
        }
        if (transport == SipTransportKind.Sctp)
        {
            return SipTransportNames.Sctp;
        }
        return null;
    }

    static SipUriParameter[] AddValuelessParameter(
        SipUriParameter[]? source, string name)
    {
        SipUriParameter[] existing = source == null
            ? new SipUriParameter[0] : source;
        SipUriParameter[] copied = CopyParameters(existing);
        System.Collections.Generic.List<SipUriParameter> result =
            new System.Collections.Generic.List<SipUriParameter>();
        int index = 0;
        while (index < copied.Length)
        {
            SipUriParameter? parameter = copied[index];
            if (parameter != null && !SipAscii.EqualsIgnoreCase(
                parameter.Name, name))
            {
                result.Add(parameter);
            }
            index = index + 1;
        }
        SipUriParameter added = new SipUriParameter();
        added.Name = name;
        added.HasValue = false;
        result.Add(added);
        return result.ToArray();
    }

    static SipFlow CopyFlow(SipFlow source)
    {
        SipFlow? result = SipSnapshotCopy.CopyFlowModel(source);
        if (result == null)
        {
            throw new System.InvalidOperationException(
                "The outbound flow could not be copied.");
        }
        return result;
    }

    static SipConnectionInfo CopyConnection(SipConnectionInfo source)
    {
        SipFlow holder = new SipFlow();
        holder.Connection = source;
        SipFlow copied = CopyFlow(holder);
        if (copied.Connection == null)
        {
            throw new System.InvalidOperationException(
                "The outbound connection could not be copied.");
        }
        return copied.Connection;
    }

    static SipRouteValue[] CopyRoutes(SipRouteValue[] source)
    {
        SipFlow holder = new SipFlow();
        holder.Path = source;
        return CopyFlow(holder).Path;
    }

    static SipUri CopyUri(SipUri source)
    {
        SipRouteValue route = new SipRouteValue();
        route.Address = new SipNameAddress();
        route.Address.Uri = source;
        SipRouteValue[] copied = CopyRoutes(new SipRouteValue[] { route });
        SipNameAddress? address = copied.Length == 0
            ? null : copied[0].Address;
        SipUri? uri = address == null ? null : address.Uri;
        if (uri == null)
        {
            throw new System.InvalidOperationException(
                "The outbound Path URI could not be copied.");
        }
        return uri;
    }

    static SipUriParameter[] CopyParameters(SipUriParameter[] source)
    {
        SipUri uri = new SipUri();
        uri.Scheme = SipUriScheme.Sip;
        uri.SchemeToken = "sip";
        uri.Host = "copy.invalid";
        uri.Parameters = source;
        return CopyUri(uri).Parameters;
    }

    static long AddMilliseconds(long value, long elapsed)
    {
        return elapsed > long.MaxValue - value
            ? long.MaxValue : value + elapsed;
    }

    static void ReverseFlows(System.Collections.Generic.List<SipFlow> values)
    {
        int left = 0;
        int right = values.Count - 1;
        while (left < right)
        {
            SipFlow temporary = values[left];
            values[left] = values[right];
            values[right] = temporary;
            left = left + 1;
            right = right - 1;
        }
    }
}
