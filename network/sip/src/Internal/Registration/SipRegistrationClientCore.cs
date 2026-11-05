namespace sip;

internal class SipRegistrationClientCore
{
    object gate = new object();
    System.Collections.Generic.List<SipRegistrationEvent> events =
        new System.Collections.Generic.List<SipRegistrationEvent>();
    System.Collections.Generic.List<SipRegistrationSnapshot> snapshots =
        new System.Collections.Generic.List<SipRegistrationSnapshot>();
    ISipRandomSource randomSource;
    SipProtocolTokenGenerator tokens;
    SipManualMonotonicClock flowClock;
    SipOutboundFlowManager flowManager;
    SipRegistrationSnapshot? snapshot;
    SipRegistrationRequest? operationRequest;
    SipMessage? outstandingRequest;
    ISipAuthenticationClient? authenticationClient;
    string? localTag;
    long nowMilliseconds;
    bool registrationSucceeded;
    bool flowRecoveryDue;
    SipRegistrationState pendingState;

    internal SipRegistrationClientCore(ISipRandomSource randomSource)
    {
        if (randomSource == null)
        {
            throw new System.ArgumentNullException("randomSource");
        }
        this.randomSource = randomSource;
        tokens = new SipProtocolTokenGenerator(randomSource);
        flowClock = new SipManualMonotonicClock(0);
        flowManager = new SipOutboundFlowManager(
            flowClock, randomSource, SipDefaults.MaxConnectionCount);
    }

    internal SipRegistrationSnapshot Register(SipRegistrationRequest request)
    {
        lock (gate)
        {
            if (snapshot != null && snapshot.State != SipRegistrationState.Terminated
                && snapshot.State != SipRegistrationState.Failed)
            {
                throw new System.InvalidOperationException(
                    "A registration operation is already active.");
            }

            ValidateRequest(request, false);
            registrationSucceeded = false;
            flowRecoveryDue = false;
            operationRequest = null;
            outstandingRequest = null;
            flowClock = new SipManualMonotonicClock(nowMilliseconds);
            flowManager = new SipOutboundFlowManager(
                flowClock, randomSource, SipDefaults.MaxConnectionCount);
            localTag = tokens.CreateTag();
            string callId = tokens.CreateCallId();
            return CreateOperation(request, SipRegistrationState.Registering,
                callId, 1, request.Options.ExpiresSeconds,
                "An initial REGISTER request was created.");
        }
    }

    internal SipRegistrationSnapshot Refresh(SipRegistrationRequest request)
    {
        lock (gate)
        {
            ValidateActiveRegistration();
            ValidateRequest(request, false);
            ValidateMatchingAddressOfRecord(request.AddressOfRecord);
            if (snapshot == null || snapshot.CallId == null)
            {
                throw new System.InvalidOperationException(
                    "The registration protocol identity is unavailable.");
            }
            if (snapshot.CSeq == int.MaxValue)
            {
                throw new SipRegistrationException(
                    "The registration CSeq is exhausted.", snapshot.State,
                    snapshot.AddressOfRecord);
            }

            AdoptAuthenticationAssociation(request.AuthenticationContextId);
            return CreateOperation(request, SipRegistrationState.Refreshing,
                snapshot.CallId, snapshot.CSeq + 1,
                request.Options.ExpiresSeconds,
                "A registration refresh request was created.");
        }
    }

    internal SipRegistrationSnapshot Unregister(SipRegistrationRequest request)
    {
        lock (gate)
        {
            ValidateActiveRegistration();
            ValidateRequest(request, true);
            ValidateMatchingAddressOfRecord(request.AddressOfRecord);
            ValidateUnregistrationTargets(request);
            if (snapshot == null || snapshot.CallId == null)
            {
                throw new System.InvalidOperationException(
                    "The registration protocol identity is unavailable.");
            }
            if (snapshot.CSeq == int.MaxValue)
            {
                throw new SipRegistrationException(
                    "The registration CSeq is exhausted.", snapshot.State,
                    snapshot.AddressOfRecord);
            }

            SipRegistrationRequest unregistration =
                CreateUnregistrationRequest(request);
            AdoptAuthenticationAssociation(unregistration.AuthenticationContextId);
            return CreateOperation(unregistration, SipRegistrationState.Unregistering,
                snapshot.CallId, snapshot.CSeq + 1, 0,
                "An unregistration request was created.");
        }
    }

    internal SipRegistrationSnapshot ReceiveResponse(SipMessage message)
    {
        lock (gate)
        {
            ValidateRegistrationResponse(message);
            if (snapshot == null)
            {
                throw new System.InvalidOperationException(
                    "An outstanding registration request is required.");
            }

            if (message.StatusCode < 200)
            {
                snapshot.Response = SipSnapshotCopy.CopyMessageModel(message);
                QueueEvent(message, "A provisional REGISTER response was received.");
                return CopyCurrentSnapshot();
            }
            if (message.StatusCode < 300)
            {
                CompleteSuccessfulResponse(message);
                return CopyCurrentSnapshot();
            }
            if (message.StatusCode == SipStatusCodes.Unauthorized
                || message.StatusCode == SipStatusCodes.ProxyAuthenticationRequired)
            {
                RetryAuthentication(message);
                return CopyCurrentSnapshot();
            }
            if (message.StatusCode >= 300 && message.StatusCode < 400)
            {
                FollowRedirect(message);
                return CopyCurrentSnapshot();
            }
            if (message.StatusCode == SipStatusCodes.IntervalTooBrief)
            {
                RetryMinimumExpiry(message);
                return CopyCurrentSnapshot();
            }
            if (message.StatusCode == SipStatusCodes.RequestTimeout
                || message.StatusCode >= 500 && message.StatusCode < 600)
            {
                HandleTransientFailure(message);
                return CopyCurrentSnapshot();
            }

            FailRegistration(message,
                "The registrar returned a permanent failure response.", false);
            return CopyCurrentSnapshot();
        }
    }

    internal void AdvanceTime(long nextMilliseconds)
    {
        lock (gate)
        {
            if (nextMilliseconds < nowMilliseconds)
            {
                throw new System.ArgumentOutOfRangeException("nowMilliseconds");
            }
            flowClock.AdvanceMilliseconds(nextMilliseconds - nowMilliseconds);
            nowMilliseconds = nextMilliseconds;
            if (snapshot == null || snapshot.State == SipRegistrationState.Terminated)
            {
                return;
            }
            if (snapshot.ExpiresAtMilliseconds >= 0
                && nowMilliseconds >= snapshot.ExpiresAtMilliseconds)
            {
                ExpireRegistration();
                return;
            }
            if (snapshot.RetryAtMilliseconds >= 0
                && nowMilliseconds >= snapshot.RetryAtMilliseconds)
            {
                if (flowRecoveryDue)
                {
                    snapshot.RetryAtMilliseconds = -1;
                    QueueEvent(null, "Outbound flow recovery is due.");
                    return;
                }
                SipRegistrationState retryState =
                    pendingState == SipRegistrationState.Unregistering
                    ? SipRegistrationState.Unregistering
                    : registrationSucceeded
                        ? SipRegistrationState.Refreshing
                        : SipRegistrationState.Registering;
                RebuildOutstandingRequest(retryState,
                    "A transient registration failure was retried.", false);
                return;
            }
            if (snapshot.State == SipRegistrationState.Registered
                && snapshot.RefreshAtMilliseconds >= 0
                && nowMilliseconds >= snapshot.RefreshAtMilliseconds)
            {
                RebuildOutstandingRequest(SipRegistrationState.Refreshing,
                    "The scheduled registration refresh was created.", true);
            }
        }
    }

    internal void Shutdown(SipCancellation cancellation)
    {
        lock (gate)
        {
            if (cancellation == null)
            {
                throw new System.ArgumentNullException("cancellation");
            }
            if (!cancellation.Requested)
            {
                throw new System.ArgumentException(
                    "Registration shutdown requires requested cancellation.",
                    "cancellation");
            }
            if (snapshot == null || snapshot.State == SipRegistrationState.Terminated)
            {
                return;
            }

            snapshot.State = SipRegistrationState.Terminated;
            snapshot.Bindings = new SipRegistrationBinding[0];
            snapshot.RefreshAtMilliseconds = -1;
            snapshot.ExpiresAtMilliseconds = -1;
            snapshot.RetryAtMilliseconds = -1;
            snapshot.ErrorMessage = cancellation.Reason;
            snapshot.Flows = new SipFlow[0];
            snapshot.ActiveFlow = null;
            outstandingRequest = null;
            registrationSucceeded = false;
            flowRecoveryDue = false;
            QueueEvent(null, cancellation.Reason == null
                ? "The registration client was shut down."
                : cancellation.Reason);
        }
    }

    internal SipRegistrationSnapshot ReportTransportFailure(
        SipTransportEvent transportEvent)
    {
        lock (gate)
        {
            ValidateActiveRegistration();
            if (transportEvent == null
                || transportEvent.Kind != SipTransportEventKind.Disconnected
                    && transportEvent.Kind != SipTransportEventKind.FlowFailed
                    && transportEvent.Kind != SipTransportEventKind.SendFailed)
            {
                throw new System.ArgumentException(
                    "A transport disconnect, flow failure, or send failure is required.",
                    "transportEvent");
            }
            if (snapshot == null || operationRequest == null)
            {
                return new SipRegistrationSnapshot();
            }

            SipFlow? failed = null;
            if (transportEvent.Flow != null && transportEvent.Flow.Id != null)
            {
                failed = flowManager.MarkFlowFailed(transportEvent.Flow.Id);
            }
            if (failed == null && transportEvent.Connection != null)
            {
                SipFlow[] affected = flowManager.ApplyTransportEvent(transportEvent);
                if (affected.Length > 0)
                {
                    failed = affected[0];
                }
            }
            if (failed == null && snapshot.ActiveFlow != null)
            {
                failed = flowManager.MarkFlowFailed(snapshot.ActiveFlow.Id);
            }
            if (failed == null)
            {
                HandleUnassociatedTransportFailure(transportEvent);
                return CopyCurrentSnapshot();
            }

            snapshot.Flows = flowManager.CopyFlows();
            SipFlow? alternate = flowManager.SelectAlternateFlow(
                failed.InstanceId, failed.Id);
            UpdateBindingFlows();
            if (alternate != null)
            {
                snapshot.ActiveFlow = alternate;
                flowRecoveryDue = false;
                RebuildOutstandingRequest(
                    registrationSucceeded
                        ? SipRegistrationState.Refreshing
                        : SipRegistrationState.Registering,
                    "The REGISTER request failed over to a backup Outbound flow.",
                    true);
            }
            else
            {
                snapshot.ActiveFlow = null;
                snapshot.State = SipRegistrationState.Failed;
                snapshot.RefreshAtMilliseconds = -1;
                snapshot.ErrorMessage = transportEvent.ErrorMessage == null
                    ? "All Outbound registration flows failed."
                    : transportEvent.ErrorMessage;
                snapshot.RetryAtMilliseconds = failed.RetryAtMilliseconds;
                outstandingRequest = null;
                flowRecoveryDue = true;
                QueueEvent(null, snapshot.ErrorMessage);
            }
            return CopyCurrentSnapshot();
        }
    }

    internal SipRegistrationSnapshot RecoverFlow(SipFlow flow)
    {
        lock (gate)
        {
            if (snapshot == null || operationRequest == null
                || !operationRequest.Options.UseOutbound)
            {
                throw new System.InvalidOperationException(
                    "An Outbound registration is required for flow recovery.");
            }
            SipContactValue? contact = FindFlowContact(
                flow == null ? null : flow.InstanceId,
                flow == null ? -1 : flow.RegistrationId);
            if (flow == null || contact == null || flow.Connection == null)
            {
                throw new System.ArgumentException(
                    "A replacement flow matching a registration Contact is required.",
                    "flow");
            }
            long expiry = FindFlowExpiry(contact);
            SipFlow recovered = flowManager.RegisterFlow(contact, flow.Connection,
                flow.Path, flow.FlowTimerSeconds, expiry, flow.Primary);
            snapshot.Flows = flowManager.CopyFlows();
            SipFlow? active = flowManager.SelectFlow(contact.InstanceId);
            snapshot.ActiveFlow = active == null ? recovered : active;
            snapshot.RetryAtMilliseconds = -1;
            snapshot.ErrorMessage = null;
            flowRecoveryDue = false;
            UpdateBindingFlows();
            RebuildOutstandingRequest(
                registrationSucceeded
                    ? SipRegistrationState.Refreshing
                    : SipRegistrationState.Registering,
                "An Outbound flow recovered and a REGISTER request was created.",
                true);
            return CopyCurrentSnapshot();
        }
    }

    internal void AssociateAuthentication(string authenticationContextId)
    {
        lock (gate)
        {
            ValidateActiveRegistration();
            ValidateIdentifier(authenticationContextId, "authenticationContextId");
            if (snapshot == null)
            {
                return;
            }
            if (snapshot.AuthenticationContextId == authenticationContextId)
            {
                return;
            }

            snapshot.AuthenticationContextId = authenticationContextId;
            QueueEvent(null, "An authentication context was associated with the registration.");
        }
    }

    internal void ConfigureAuthentication(
        ISipAuthenticationClient configuredAuthenticationClient)
    {
        lock (gate)
        {
            if (configuredAuthenticationClient == null)
            {
                throw new System.ArgumentNullException("authenticationClient");
            }
            authenticationClient = configuredAuthenticationClient;
        }
    }

    internal SipRegistrationEvent PollEvent()
    {
        lock (gate)
        {
            if (events.Count == 0)
            {
                return new SipRegistrationEvent();
            }
            SipRegistrationEvent value = events[0];
            events.RemoveAt(0);
            SipRegistrationEvent? copy = SipSnapshotCopy.CopyRegistrationEvent(value);
            return copy == null ? new SipRegistrationEvent() : copy;
        }
    }

    internal SipRegistrationSnapshot PollSnapshot()
    {
        lock (gate)
        {
            if (snapshots.Count == 0)
            {
                return new SipRegistrationSnapshot();
            }
            SipRegistrationSnapshot value = snapshots[0];
            snapshots.RemoveAt(0);
            SipRegistrationSnapshot? copy =
                SipSnapshotCopy.CopyRegistrationSnapshot(value);
            return copy == null ? new SipRegistrationSnapshot() : copy;
        }
    }

    internal SipRegistrationSnapshot GetSnapshot()
    {
        lock (gate)
        {
            return CopyCurrentSnapshot();
        }
    }

    SipRegistrationSnapshot CreateOperation(
        SipRegistrationRequest request, SipRegistrationState state,
        string callId, int cseq, int expiresSeconds, string reason)
    {
        SipUri target = SelectRegistrarTarget(request);
        SipRouteValue[] serviceRoute = SipRouteRouting.CopyRouteSet(
            request.ServiceRoute);
        SipUri? outboundProxy = CopyOptionalSipUri(
            request.OutboundProxy, "outboundProxy");
        SipRouteValue[] routes = BuildRequestRoutes(outboundProxy, serviceRoute);
        SipMessage message = BuildRegisterMessage(request, target, routes,
            callId, cseq, expiresSeconds);

        SipRegistrationSnapshot next = new SipRegistrationSnapshot();
        next.State = state;
        next.AddressOfRecord = SipUriModelCopy.CreateNormalizedCopy(
            request.AddressOfRecord);
        next.Bindings = registrationSucceeded && snapshot != null
            ? CopyBindingArray(snapshot.Bindings)
            : BuildBindings(message, next.AddressOfRecord, serviceRoute, callId, cseq);
        next.Target = SipUriModelCopy.CreateNormalizedCopy(target);
        next.OutboundProxy = outboundProxy == null
            ? null : SipUriModelCopy.CreateNormalizedCopy(outboundProxy);
        next.ServiceRoute = SipRouteRouting.CopyRouteSet(serviceRoute);
        next.Flows = registrationSucceeded && snapshot != null
            ? SipSnapshotCopy.CopyFlowArray(snapshot.Flows)
            : SipSnapshotCopy.CopyFlowArray(request.Flows);
        next.ActiveFlow = registrationSucceeded && snapshot != null
            ? SipSnapshotCopy.CopyFlowModel(snapshot.ActiveFlow)
            : SelectConfiguredPrimaryFlow(next.Flows);
        next.CallId = callId;
        next.CSeq = cseq;
        next.AuthenticationContextId = request.AuthenticationContextId == null
            && snapshot != null ? snapshot.AuthenticationContextId
            : request.AuthenticationContextId;
        next.CorrelationId = request.CorrelationId;
        next.RefreshAtMilliseconds = registrationSucceeded && snapshot != null
            ? snapshot.RefreshAtMilliseconds : -1;
        next.ExpiresAtMilliseconds = registrationSucceeded && snapshot != null
            ? snapshot.ExpiresAtMilliseconds : -1;
        next.AuthenticationAttemptCount = 0;
        next.RedirectCount = 0;
        operationRequest = CopyOperationRequest(request);
        outstandingRequest = SipSnapshotCopy.CopyMessageModel(message);
        pendingState = state;
        snapshot = next;
        QueueEvent(message, reason);
        SipRegistrationSnapshot? copy = SipSnapshotCopy.CopyRegistrationSnapshot(next);
        return copy == null ? new SipRegistrationSnapshot() : copy;
    }

    SipMessage BuildRegisterMessage(
        SipRegistrationRequest request, SipUri target, SipRouteValue[] routes,
        string callId, int cseq, int expiresSeconds)
    {
        SipWriteOptions writeOptions = new SipWriteOptions();
        writeOptions.PreserveOriginal = false;
        string targetText = FormatUri(target, writeOptions, "target");
        SipNameAddress from = CreateAddress(request.AddressOfRecord, localTag);
        SipNameAddress to = CreateAddress(request.AddressOfRecord, null);
        string fromText = SipHeaderPrimitives.FormatNameAddressValue(from, writeOptions);
        string toText = SipHeaderPrimitives.FormatNameAddressValue(to, writeOptions);
        string contactLine = FormatContactLine(request.Contacts, writeOptions);
        string branch = tokens.CreateBranch();
        string transport = target.Scheme == SipUriScheme.Sips ? "TLS" : "UDP";

        System.Text.StringBuilder text = new System.Text.StringBuilder();
        text.Append("REGISTER ");
        text.Append(targetText);
        text.Append(" SIP/2.0\r\nVia: SIP/2.0/");
        text.Append(transport);
        text.Append(" registration.invalid;branch=");
        text.Append(branch);
        text.Append("\r\nMax-Forwards: 70\r\nFrom: ");
        text.Append(fromText);
        text.Append("\r\nTo: ");
        text.Append(toText);
        text.Append("\r\nCall-ID: ");
        text.Append(callId);
        text.Append("\r\nCSeq: ");
        text.Append(cseq.ToString(System.Globalization.CultureInfo.InvariantCulture));
        text.Append(" REGISTER\r\n");
        text.Append(contactLine);
        text.Append("Expires: ");
        text.Append(expiresSeconds.ToString(
            System.Globalization.CultureInfo.InvariantCulture));
        text.Append("\r\n");
        AppendRegistrationSupported(text, request.Options);
        text.Append("Content-Length: 0\r\n\r\n");

        SipParseResult parsed = SipMessageParser.ParseDatagramMessageBytes(
            System.Text.Encoding.UTF8.GetBytes(text.ToString()),
            new SipParseOptions());
        if (parsed.Status != SipParseStatus.Complete || parsed.Message == null)
        {
            throw new SipRegistrationException(
                parsed.ErrorMessage == null
                    ? "The REGISTER request could not be constructed."
                    : parsed.ErrorMessage,
                snapshot == null ? SipRegistrationState.None : snapshot.State,
                request.AddressOfRecord);
        }

        SipRouteRouting.ApplyRouteSet(parsed.Message, target, routes);
        return parsed.Message;
    }

    void AppendRegistrationSupported(System.Text.StringBuilder text,
        SipRegistrationOptions options)
    {
        if (options.UseOutbound && options.UseGruu)
        {
            text.Append("Supported: path, outbound, gruu\r\n");
        }
        else if (options.UseOutbound)
        {
            text.Append("Supported: path, outbound\r\n");
        }
        else if (options.UseGruu)
        {
            text.Append("Supported: gruu\r\n");
        }
    }

    string FormatContactLine(
        SipContactValue[] contacts, SipWriteOptions writeOptions)
    {
        SipStructuredHeaderResult structured = new SipStructuredHeaderResult();
        structured.Success = true;
        structured.ContactValues = contacts;
        SipWriteResult formatted = SipStructuredHeaderCodec.FormatStructuredHeaderValue(
            SipHeaderNames.Contact, structured, writeOptions);
        if (!formatted.Success)
        {
            throw new System.ArgumentException(
                formatted.ErrorMessage == null
                    ? "The registration Contacts are invalid."
                    : formatted.ErrorMessage,
                "request");
        }
        return System.Text.Encoding.UTF8.GetString(formatted.Bytes);
    }

    string FormatUri(SipUri uri, SipWriteOptions options, string parameterName)
    {
        SipWriteResult result = SipUriFormatter.FormatUriModel(uri, options);
        if (!result.Success)
        {
            throw new System.ArgumentException(
                result.ErrorMessage == null ? "The URI is invalid." : result.ErrorMessage,
                parameterName);
        }
        return System.Text.Encoding.ASCII.GetString(result.Bytes);
    }

    SipUri SelectRegistrarTarget(SipRegistrationRequest request)
    {
        if (request.Target != null)
        {
            SipUri? target = CopyOptionalSipUri(request.Target, "target");
            if (target != null)
            {
                return target;
            }
        }

        SipUri targetFromAor = SipUriModelCopy.CreateNormalizedCopy(
            request.AddressOfRecord);
        targetFromAor.User = null;
        targetFromAor.Password = null;
        targetFromAor.Parameters = new SipUriParameter[0];
        targetFromAor.Headers = new SipUriHeader[0];
        targetFromAor.OriginalText = null;
        return targetFromAor;
    }

    SipUri? CopyOptionalSipUri(SipUri? source, string parameterName)
    {
        if (source == null)
        {
            return null;
        }
        if (source.Scheme != SipUriScheme.Sip
            && source.Scheme != SipUriScheme.Sips
            || SipUriValidator.ValidateUriModel(source).Length > 0)
        {
            throw new System.ArgumentException(
                "A valid SIP or SIPS URI is required.", parameterName);
        }
        return SipUriModelCopy.CreateNormalizedCopy(source);
    }

    SipRouteValue[] BuildRequestRoutes(
        SipUri? outboundProxy, SipRouteValue[] serviceRoute)
    {
        int offset = outboundProxy == null ? 0 : 1;
        SipRouteValue[] routes = new SipRouteValue[serviceRoute.Length + offset];
        if (outboundProxy != null)
        {
            SipRouteValue proxyRoute = new SipRouteValue();
            SipNameAddress address = new SipNameAddress();
            address.Uri = SipUriModelCopy.CreateNormalizedCopy(outboundProxy);
            EnsureLooseRouting(address.Uri);
            proxyRoute.Address = address;
            proxyRoute.LooseRouting = true;
            routes[0] = proxyRoute;
        }
        int index = 0;
        while (index < serviceRoute.Length)
        {
            routes[index + offset] = serviceRoute[index];
            index = index + 1;
        }
        return routes;
    }

    void EnsureLooseRouting(SipUri uri)
    {
        int index = 0;
        while (index < uri.Parameters.Length)
        {
            SipUriParameter parameter = uri.Parameters[index];
            if (parameter != null && SipAscii.EqualsIgnoreCase(parameter.Name, "lr"))
            {
                parameter.Value = null;
                parameter.HasValue = false;
                parameter.OriginalText = null;
                return;
            }
            index = index + 1;
        }

        SipUriParameter added = new SipUriParameter();
        added.Name = "lr";
        SipUriParameter[] parameters = new SipUriParameter[uri.Parameters.Length + 1];
        index = 0;
        while (index < uri.Parameters.Length)
        {
            parameters[index] = uri.Parameters[index];
            index = index + 1;
        }
        parameters[parameters.Length - 1] = added;
        uri.Parameters = parameters;
    }

    SipNameAddress CreateAddress(SipUri? uri, string? tag)
    {
        SipNameAddress address = new SipNameAddress();
        address.Uri = SipUriModelCopy.CreateNormalizedCopy(uri);
        if (tag != null)
        {
            SipUriParameter parameter = new SipUriParameter();
            parameter.Name = "tag";
            address.Parameters = new SipUriParameter[] { parameter };
            parameter.Value = tag;
            parameter.HasValue = true;
        }
        return address;
    }

    SipRegistrationBinding[] BuildBindings(
        SipMessage message, SipUri addressOfRecord, SipRouteValue[] serviceRoute,
        string callId, int cseq)
    {
        SipRegistrationBinding[] bindings =
            new SipRegistrationBinding[message.ContactValues.Length];
        int index = 0;
        while (index < bindings.Length)
        {
            SipRegistrationBinding binding = new SipRegistrationBinding();
            binding.AddressOfRecord = SipUriModelCopy.CreateNormalizedCopy(
                addressOfRecord);
            binding.Contact = message.ContactValues[index];
            binding.CallId = callId;
            binding.CSeq = cseq;
            binding.QualityThousandths = message.ContactValues[index].QualityThousandths < 0
                ? 1000 : message.ContactValues[index].QualityThousandths;
            binding.ServiceRoute = SipRouteRouting.CopyRouteSet(serviceRoute);
            binding.InstanceId = message.ContactValues[index].InstanceId;
            binding.RegistrationId = message.ContactValues[index].RegistrationId;
            bindings[index] = binding;
            index = index + 1;
        }
        return bindings;
    }

    void QueueEvent(SipMessage? message, string reason)
    {
        if (snapshot == null)
        {
            return;
        }
        SipRegistrationEvent value = new SipRegistrationEvent();
        value.State = snapshot.State;
        value.Snapshot = SipSnapshotCopy.CopyRegistrationSnapshot(snapshot);
        value.Message = SipSnapshotCopy.CopyMessageModel(message);
        value.Reason = reason;
        value.CorrelationId = snapshot.CorrelationId;
        events.Add(value);
        SipRegistrationSnapshot? queued =
            SipSnapshotCopy.CopyRegistrationSnapshot(snapshot);
        if (queued != null)
        {
            snapshots.Add(queued);
        }
    }

    void CompleteSuccessfulResponse(SipMessage response)
    {
        if (snapshot == null || operationRequest == null)
        {
            return;
        }
        snapshot.Response = SipSnapshotCopy.CopyMessageModel(response);
        snapshot.ErrorMessage = null;
        snapshot.RetryAtMilliseconds = -1;
        snapshot.AuthenticationAttemptCount = 0;
        snapshot.RedirectCount = 0;
        outstandingRequest = null;
        if (pendingState == SipRegistrationState.Unregistering)
        {
            CompleteSuccessfulUnregistration(response);
            return;
        }

        SipRouteValue[]? learnedServiceRoute =
            ReadRegistrationServiceRoute(response);
        if (learnedServiceRoute == null)
        {
            FailRegistration(response,
                "The successful REGISTER response contains a malformed Service-Route.",
                false);
            return;
        }
        if (learnedServiceRoute.Length > 0)
        {
            snapshot.ServiceRoute = SipRouteRouting.CopyRouteSet(
                learnedServiceRoute);
            operationRequest.ServiceRoute = SipRouteRouting.CopyRouteSet(
                learnedServiceRoute);
        }

        int expiresHeader = ReadSecondsHeader(response, SipHeaderNames.Expires);
        SipContactValue[] responseContacts = response.ContactValues == null
            ? new SipContactValue[0] : response.ContactValues;
        System.Collections.Generic.List<SipRegistrationBinding> bindings =
            new System.Collections.Generic.List<SipRegistrationBinding>();
        int matched = 0;
        long earliestExpiry = -1;
        int index = 0;
        while (index < responseContacts.Length)
        {
            SipContactValue contact = responseContacts[index];
            if (ContactWasRequested(contact, operationRequest.Contacts))
            {
                matched = matched + 1;
                int expires = SipContactRouting.GetEffectiveExpires(
                    contact, expiresHeader, operationRequest.Options.ExpiresSeconds);
                if (expires > 0)
                {
                    SipRegistrationBinding binding = CreateSuccessfulBinding(
                        contact, expires);
                    bindings.Add(binding);
                    if (earliestExpiry < 0
                        || binding.ExpiresAtMilliseconds < earliestExpiry)
                    {
                        earliestExpiry = binding.ExpiresAtMilliseconds;
                    }
                }
            }
            index = index + 1;
        }
        if (matched == 0)
        {
            FailRegistration(response,
                "The successful REGISTER response did not contain a matching Contact.",
                false);
            return;
        }
        if (bindings.Count == 0)
        {
            snapshot.State = SipRegistrationState.Terminated;
            snapshot.Bindings = new SipRegistrationBinding[0];
            snapshot.RefreshAtMilliseconds = -1;
            snapshot.ExpiresAtMilliseconds = -1;
            registrationSucceeded = false;
            QueueEvent(response, "The registrar returned no active matching binding.");
            return;
        }

        snapshot.State = SipRegistrationState.Registered;
        snapshot.Bindings = bindings.ToArray();
        snapshot.ExpiresAtMilliseconds = earliestExpiry;
        snapshot.RefreshAtMilliseconds = CalculateRefreshDeadline(
            earliestExpiry, operationRequest.Options);
        registrationSucceeded = true;
        InitializeOutboundFlows();
        QueueEvent(response, "The registration became active.");
    }

    static SipRouteValue[]? ReadRegistrationServiceRoute(SipMessage response)
    {
        System.Collections.Generic.List<SipRouteValue> routes =
            new System.Collections.Generic.List<SipRouteValue>();
        int headerIndex = 0;
        while (headerIndex < response.Headers.Length)
        {
            SipHeader? header = response.Headers[headerIndex];
            if (header != null && SipAscii.EqualsIgnoreCase(
                    header.Name, SipHeaderNames.ServiceRoute))
            {
                string value = header.Value == null ? "" : header.Value;
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(
                    SipHeaderNames.ServiceRoute + ": " + value + "\r\n");
                SipHeaderParseResult parsed = SipHeaderParser.ParseHeaderBytes(
                    bytes, new SipParseOptions());
                if (parsed.Status != SipParseStatus.Complete)
                {
                    return null;
                }
                int routeIndex = 0;
                while (routeIndex < parsed.RouteValues.Length)
                {
                    routes.Add(parsed.RouteValues[routeIndex]);
                    routeIndex = routeIndex + 1;
                }
            }
            headerIndex = headerIndex + 1;
        }
        return routes.ToArray();
    }

    void CompleteSuccessfulUnregistration(SipMessage response)
    {
        if (snapshot == null || operationRequest == null)
        {
            return;
        }
        if (operationRequest.UnregisterAll)
        {
            snapshot.Bindings = new SipRegistrationBinding[0];
            flowClock = new SipManualMonotonicClock(nowMilliseconds);
            flowManager = new SipOutboundFlowManager(
                flowClock, randomSource, SipDefaults.MaxConnectionCount);
        }
        else
        {
            RemoveUnregisteredFlows(operationRequest.Contacts);
            System.Collections.Generic.List<SipRegistrationBinding> remaining =
                new System.Collections.Generic.List<SipRegistrationBinding>();
            int index = 0;
            while (index < snapshot.Bindings.Length)
            {
                SipRegistrationBinding? binding = snapshot.Bindings[index];
                bool remove = binding != null && binding.Contact != null
                    && ContactWasRequested(
                        binding.Contact, operationRequest.Contacts);
                if (!remove && binding != null)
                {
                    remaining.Add(binding);
                }
                index = index + 1;
            }
            snapshot.Bindings = SipSnapshotCopy.CopyRegistrationBindingArray(
                remaining.ToArray());
        }

        if (snapshot.Bindings.Length == 0)
        {
            snapshot.State = SipRegistrationState.Terminated;
            snapshot.RefreshAtMilliseconds = -1;
            snapshot.ExpiresAtMilliseconds = -1;
            snapshot.Flows = new SipFlow[0];
            snapshot.ActiveFlow = null;
            registrationSucceeded = false;
            QueueEvent(response, "The registration was removed successfully.");
            return;
        }

        snapshot.State = SipRegistrationState.Registered;
        snapshot.Flows = flowManager.CopyFlows();
        SipContactValue? firstContact = snapshot.Bindings[0].Contact;
        string? instanceId = firstContact == null
            ? null : firstContact.InstanceId;
        snapshot.ActiveFlow = flowManager.SelectFlow(instanceId);
        snapshot.ExpiresAtMilliseconds = EarliestBindingExpiry(snapshot.Bindings);
        snapshot.RefreshAtMilliseconds = CalculateRefreshDeadline(
            snapshot.ExpiresAtMilliseconds, operationRequest.Options);
        AdoptRemainingRegistrationContacts();
        QueueEvent(response, "The selected registration Contacts were removed.");
    }

    void RemoveUnregisteredFlows(SipContactValue[] contacts)
    {
        SipFlow[] flows = flowManager.CopyFlows();
        int flowIndex = 0;
        while (flowIndex < flows.Length)
        {
            int contactIndex = 0;
            while (contactIndex < contacts.Length)
            {
                if (flows[flowIndex].RegistrationId
                        == contacts[contactIndex].RegistrationId
                    && SameText(flows[flowIndex].InstanceId,
                        contacts[contactIndex].InstanceId))
                {
                    flowManager.RemoveFlow(flows[flowIndex].Id);
                }
                contactIndex = contactIndex + 1;
            }
            flowIndex = flowIndex + 1;
        }
    }

    SipRegistrationBinding CreateSuccessfulBinding(
        SipContactValue contact, int expiresSeconds)
    {
        if (snapshot == null || snapshot.AddressOfRecord == null)
        {
            return new SipRegistrationBinding();
        }
        SipContactValue[] copied = SipSnapshotCopy.CopyContactArray(
            new SipContactValue[] { contact });
        SipRegistrationBinding binding = new SipRegistrationBinding();
        binding.AddressOfRecord = SipUriModelCopy.CreateNormalizedCopy(
            snapshot.AddressOfRecord);
        binding.Contact = copied[0];
        binding.CallId = snapshot.CallId;
        binding.CSeq = snapshot.CSeq;
        binding.ExpiresAtMilliseconds = AddMilliseconds(
            nowMilliseconds, (long)expiresSeconds * 1000);
        binding.QualityThousandths = contact.QualityThousandths < 0
            ? 1000 : contact.QualityThousandths;
        binding.ServiceRoute = SipRouteRouting.CopyRouteSet(snapshot.ServiceRoute);
        binding.InstanceId = contact.InstanceId;
        binding.RegistrationId = contact.RegistrationId;
        return binding;
    }

    bool ContactWasRequested(
        SipContactValue responseContact, SipContactValue[] requestedContacts)
    {
        if (responseContact == null || requestedContacts == null)
        {
            return false;
        }
        int index = 0;
        while (index < requestedContacts.Length)
        {
            if (SipContactRouting.ContactsMatch(
                responseContact, requestedContacts[index]))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    long CalculateRefreshDeadline(
        long expiryMilliseconds, SipRegistrationOptions options)
    {
        long lead = (long)options.RefreshBeforeSeconds * 1000;
        int jitterSeconds = 0;
        if (options.RefreshJitterSeconds > 0)
        {
            byte[] bytes = new byte[4];
            randomSource.FillBytes(bytes);
            long sample = bytes[0]
                + ((long)bytes[1] << 8)
                + ((long)bytes[2] << 16)
                + ((long)bytes[3] << 24);
            jitterSeconds = (int)(sample
                % ((long)options.RefreshJitterSeconds + 1));
        }
        long advance = lead + (long)jitterSeconds * 1000;
        if (expiryMilliseconds <= nowMilliseconds
            || advance >= expiryMilliseconds - nowMilliseconds)
        {
            return nowMilliseconds;
        }
        return expiryMilliseconds - advance;
    }

    void RetryAuthentication(SipMessage response)
    {
        if (snapshot == null || operationRequest == null)
        {
            return;
        }
        if (authenticationClient == null)
        {
            FailRegistration(response,
                "The REGISTER challenge requires an authentication client.", false);
            return;
        }
        if (snapshot.AuthenticationAttemptCount
            >= operationRequest.Options.MaxAuthenticationAttempts)
        {
            FailRegistration(response,
                "The registration authentication retry limit was reached.", false);
            return;
        }

        SipCredentialScope scope = response.StatusCode == SipStatusCodes.Unauthorized
            ? SipCredentialScope.Origin : SipCredentialScope.Proxy;
        SipDigestChallenge[] challenges = ReadDigestChallenges(response, scope);
        if (challenges.Length == 0)
        {
            FailRegistration(response,
                "The REGISTER authentication challenge is missing or malformed.",
                false);
            return;
        }
        snapshot.Response = SipSnapshotCopy.CopyMessageModel(response);
        SipMessage retry = PrepareRetryMessage(true);
        SipAuthenticationRequest authentication = new SipAuthenticationRequest();
        authentication.Message = retry;
        authentication.Challenges = challenges;
        authentication.Target = scope == SipCredentialScope.Proxy
            ? SipRouteRouting.SelectNextHop(retry, snapshot.OutboundProxy)
            : snapshot.Target;
        authentication.Body = retry.Body;
        authentication.Scope = scope;
        authentication.Attempt = snapshot.AuthenticationAttemptCount;
        authentication.CorrelationId = snapshot.AuthenticationContextId == null
            ? snapshot.CorrelationId : snapshot.AuthenticationContextId;
        SipAuthenticationResult authorized =
            authenticationClient.AuthorizeRequest(authentication);
        if (authorized.Status != SipAuthenticationStatus.Success
            || !authorized.RetryAllowed || authorized.Message == null)
        {
            FailRegistration(response,
                authorized.ErrorMessage == null
                    ? "The REGISTER request could not be authorized."
                    : authorized.ErrorMessage,
                false);
            return;
        }

        snapshot.AuthenticationAttemptCount =
            snapshot.AuthenticationAttemptCount + 1;
        snapshot.State = pendingState;
        outstandingRequest = SipSnapshotCopy.CopyMessageModel(authorized.Message);
        QueueEvent(authorized.Message,
            scope == SipCredentialScope.Origin
                ? "An authenticated REGISTER retry was created."
                : "A proxy-authenticated REGISTER retry was created.");
    }

    SipDigestChallenge[] ReadDigestChallenges(
        SipMessage response, SipCredentialScope scope)
    {
        string name = scope == SipCredentialScope.Origin
            ? SipHeaderNames.WwwAuthenticate : SipHeaderNames.ProxyAuthenticate;
        System.Collections.Generic.List<SipDigestChallenge> challenges =
            new System.Collections.Generic.List<SipDigestChallenge>();
        if (response.Headers == null)
        {
            return challenges.ToArray();
        }
        int index = 0;
        while (index < response.Headers.Length)
        {
            SipHeader header = response.Headers[index];
            if (header != null && SipAscii.EqualsIgnoreCase(header.Name, name))
            {
                SipAuthenticationHeaderResult parsed =
                    SipAuthenticationHeaderCodec.ParseAuthenticationHeaderValue(
                        header.Name, header.Value, new SipParseOptions());
                if (!parsed.Success || parsed.Challenge == null)
                {
                    return new SipDigestChallenge[0];
                }
                challenges.Add(parsed.Challenge);
            }
            index = index + 1;
        }
        return challenges.ToArray();
    }

    void FollowRedirect(SipMessage response)
    {
        if (snapshot == null || operationRequest == null)
        {
            return;
        }
        if (!operationRequest.Options.FollowRedirects
            || snapshot.RedirectCount >= operationRequest.Options.MaxRedirects)
        {
            FailRegistration(response,
                "Registration redirect policy rejected the response.", false);
            return;
        }
        SipUri? target = SelectRedirectTarget(response);
        if (target == null)
        {
            FailRegistration(response,
                "The registration redirect has no usable Contact target.", false);
            return;
        }
        if (snapshot.Target != null && snapshot.Target.Scheme == SipUriScheme.Sips
            && target.Scheme != SipUriScheme.Sips)
        {
            FailRegistration(response,
                "A registration redirect cannot downgrade a SIPS target.", false);
            return;
        }

        operationRequest.Target = SipUriModelCopy.CreateNormalizedCopy(target);
        snapshot.RedirectCount = snapshot.RedirectCount + 1;
        snapshot.AuthenticationAttemptCount = 0;
        snapshot.Response = SipSnapshotCopy.CopyMessageModel(response);
        SipMessage redirected = PrepareRetryMessage(false);
        outstandingRequest = SipSnapshotCopy.CopyMessageModel(redirected);
        QueueEvent(redirected, "A redirected REGISTER request was created.");
    }

    SipUri? SelectRedirectTarget(SipMessage response)
    {
        if (response.ContactValues == null)
        {
            return null;
        }
        SipContactValue? selected = null;
        int selectedQuality = -1;
        int index = 0;
        while (index < response.ContactValues.Length)
        {
            SipContactValue contact = response.ContactValues[index];
            SipUri? uri = contact == null || contact.Wildcard
                || contact.Address == null ? null : contact.Address.Uri;
            int quality = contact == null || contact.QualityThousandths < 0
                ? 1000 : contact.QualityThousandths;
            if (uri != null && (uri.Scheme == SipUriScheme.Sip
                    || uri.Scheme == SipUriScheme.Sips)
                && SipUriValidator.ValidateUriModel(uri).Length == 0
                && quality > selectedQuality)
            {
                selected = contact;
                selectedQuality = quality;
            }
            index = index + 1;
        }
        return selected == null || selected.Address == null
            || selected.Address.Uri == null ? null
            : SipUriModelCopy.CreateNormalizedCopy(selected.Address.Uri);
    }

    void RetryMinimumExpiry(SipMessage response)
    {
        if (snapshot == null || operationRequest == null)
        {
            return;
        }
        int minimum = ReadSecondsHeader(response, SipHeaderNames.MinExpires);
        if (pendingState == SipRegistrationState.Unregistering
            || minimum <= operationRequest.Options.ExpiresSeconds
            || minimum > operationRequest.Options.MaxExpiresSeconds)
        {
            FailRegistration(response,
                "The 423 response does not contain an acceptable Min-Expires value.",
                false);
            return;
        }

        operationRequest.Options.ExpiresSeconds = minimum;
        int index = 0;
        while (index < operationRequest.Contacts.Length)
        {
            if (operationRequest.Contacts[index].ExpiresSeconds >= 0
                && operationRequest.Contacts[index].ExpiresSeconds < minimum)
            {
                operationRequest.Contacts[index].ExpiresSeconds = minimum;
            }
            index = index + 1;
        }
        snapshot.Response = SipSnapshotCopy.CopyMessageModel(response);
        SipMessage retry = PrepareRetryMessage(true);
        outstandingRequest = SipSnapshotCopy.CopyMessageModel(retry);
        QueueEvent(retry,
            "A REGISTER retry using the required minimum expiry was created.");
    }

    void HandleTransientFailure(SipMessage response)
    {
        if (snapshot == null || operationRequest == null)
        {
            return;
        }
        int retrySeconds = ReadRetryAfterSeconds(response);
        if (retrySeconds < 0)
        {
            retrySeconds = operationRequest.Options.RetryDelaySeconds;
        }
        snapshot.State = registrationSucceeded
            ? SipRegistrationState.Registered : SipRegistrationState.Failed;
        snapshot.Response = SipSnapshotCopy.CopyMessageModel(response);
        snapshot.ErrorMessage = "The registrar returned a transient failure response.";
        snapshot.RefreshAtMilliseconds = -1;
        snapshot.RetryAtMilliseconds = AddMilliseconds(
            nowMilliseconds, (long)retrySeconds * 1000);
        outstandingRequest = null;
        QueueEvent(response, snapshot.ErrorMessage);
    }

    void FailRegistration(
        SipMessage? response, string errorMessage, bool expired)
    {
        if (snapshot == null)
        {
            return;
        }
        snapshot.State = expired
            ? SipRegistrationState.Terminated : SipRegistrationState.Failed;
        snapshot.Response = SipSnapshotCopy.CopyMessageModel(response);
        snapshot.ErrorMessage = errorMessage;
        snapshot.RefreshAtMilliseconds = -1;
        snapshot.RetryAtMilliseconds = -1;
        outstandingRequest = null;
        if (!registrationSucceeded || expired)
        {
            snapshot.Bindings = new SipRegistrationBinding[0];
            snapshot.ExpiresAtMilliseconds = -1;
            snapshot.Flows = new SipFlow[0];
            snapshot.ActiveFlow = null;
        }
        if (expired)
        {
            registrationSucceeded = false;
            flowRecoveryDue = false;
        }
        QueueEvent(response, errorMessage);
    }

    void ExpireRegistration()
    {
        FailRegistration(null, "The registration expired before refresh completed.",
            true);
    }

    void RebuildOutstandingRequest(
        SipRegistrationState state, string reason, bool preserveAuthorization)
    {
        if (snapshot == null || operationRequest == null)
        {
            return;
        }
        pendingState = state;
        snapshot.State = state;
        snapshot.RetryAtMilliseconds = -1;
        snapshot.RefreshAtMilliseconds = -1;
        snapshot.ErrorMessage = null;
        SipMessage message = PrepareRetryMessage(preserveAuthorization);
        outstandingRequest = SipSnapshotCopy.CopyMessageModel(message);
        QueueEvent(message, reason);
    }

    SipMessage PrepareRetryMessage(bool preserveAuthorization)
    {
        if (snapshot == null || operationRequest == null
            || snapshot.CallId == null || snapshot.AddressOfRecord == null)
        {
            throw new System.InvalidOperationException(
                "Registration request state is unavailable.");
        }
        if (snapshot.CSeq == int.MaxValue)
        {
            throw new SipRegistrationException(
                "The registration CSeq is exhausted.", snapshot.State,
                snapshot.AddressOfRecord);
        }
        SipUri target = SelectRegistrarTarget(operationRequest);
        SipRouteValue[] serviceRoute = SipRouteRouting.CopyRouteSet(
            operationRequest.ServiceRoute);
        SipUri? outboundProxy = CopyOptionalSipUri(
            operationRequest.OutboundProxy, "outboundProxy");
        SipRouteValue[] routes = BuildRequestRoutes(outboundProxy, serviceRoute);
        int nextCSeq = snapshot.CSeq + 1;
        int expires = pendingState == SipRegistrationState.Unregistering
            ? 0 : operationRequest.Options.ExpiresSeconds;
        SipMessage message = BuildRegisterMessage(operationRequest, target, routes,
            snapshot.CallId, nextCSeq, expires);
        if (preserveAuthorization && outstandingRequest != null)
        {
            CopyAuthorizationHeaders(outstandingRequest, message);
        }
        snapshot.CSeq = nextCSeq;
        snapshot.Target = SipUriModelCopy.CreateNormalizedCopy(target);
        snapshot.OutboundProxy = outboundProxy == null
            ? null : SipUriModelCopy.CreateNormalizedCopy(outboundProxy);
        snapshot.ServiceRoute = SipRouteRouting.CopyRouteSet(serviceRoute);
        if (!registrationSucceeded)
        {
            snapshot.Bindings = BuildBindings(message, snapshot.AddressOfRecord,
                serviceRoute, snapshot.CallId, nextCSeq);
        }
        return message;
    }

    void CopyAuthorizationHeaders(SipMessage source, SipMessage destination)
    {
        System.Collections.Generic.List<SipHeader> headers =
            new System.Collections.Generic.List<SipHeader>();
        int index = 0;
        while (index < destination.Headers.Length)
        {
            headers.Add(destination.Headers[index]);
            index = index + 1;
        }
        index = 0;
        while (index < source.Headers.Length)
        {
            SipHeader header = source.Headers[index];
            if (header != null && (SipAscii.EqualsIgnoreCase(
                    header.Name, SipHeaderNames.Authorization)
                || SipAscii.EqualsIgnoreCase(
                    header.Name, SipHeaderNames.ProxyAuthorization)))
            {
                SipHeader copied = new SipHeader();
                copied.Name = header.Name;
                copied.Value = header.Value;
                headers.Add(copied);
            }
            index = index + 1;
        }
        destination.Headers = headers.ToArray();
        destination.OriginalBytes = new byte[0];
    }

    int ReadSecondsHeader(SipMessage message, string name)
    {
        int value = -1;
        int index = 0;
        while (index < message.Headers.Length)
        {
            SipHeader header = message.Headers[index];
            if (header != null && SipAscii.EqualsIgnoreCase(header.Name, name))
            {
                if (value >= 0)
                {
                    throw new SipRegistrationException(
                        "The REGISTER response contains a duplicate " + name
                            + " header.",
                        snapshot == null ? SipRegistrationState.None : snapshot.State,
                        snapshot == null ? null : snapshot.AddressOfRecord);
                }
                SipCoreHeaderResult parsed = SipCoreHeaderCodec.ParseCoreHeaderValue(
                    name, header.Value, new SipParseOptions());
                int parsedValue = SipAscii.EqualsIgnoreCase(name, SipHeaderNames.MinExpires)
                    ? parsed.MinExpiresSeconds : parsed.ExpiresSeconds;
                if (!parsed.Success || parsedValue < 0)
                {
                    throw new SipRegistrationException(
                        "The REGISTER response contains an invalid " + name
                            + " header.",
                        snapshot == null ? SipRegistrationState.None : snapshot.State,
                        snapshot == null ? null : snapshot.AddressOfRecord);
                }
                value = parsedValue;
            }
            index = index + 1;
        }
        return value;
    }

    int ReadRetryAfterSeconds(SipMessage message)
    {
        int value = -1;
        int index = 0;
        while (index < message.Headers.Length)
        {
            SipHeader header = message.Headers[index];
            if (header != null && SipAscii.EqualsIgnoreCase(
                header.Name, SipHeaderNames.RetryAfter))
            {
                if (value >= 0)
                {
                    return -1;
                }
                SipCoreHeaderResult parsed = SipCoreHeaderCodec.ParseCoreHeaderValue(
                    SipHeaderNames.RetryAfter, header.Value,
                    new SipParseOptions());
                if (!parsed.Success || parsed.RetryAfter == null)
                {
                    return -1;
                }
                value = parsed.RetryAfter.Seconds;
            }
            index = index + 1;
        }
        return value;
    }

    void ValidateRegistrationResponse(SipMessage? message)
    {
        ValidateActiveRegistration();
        if (message == null || message.Kind != SipMessageKind.Response
            || message.StatusCode < 100 || message.StatusCode > 699
            || message.CSeq == null || message.CSeq.Method != SipMethod.Register
            || snapshot == null || outstandingRequest == null
            || message.CSeq.SequenceNumber != snapshot.CSeq
            || ReadCallId(message) != snapshot.CallId)
        {
            throw new System.ArgumentException(
                "A matching response to the outstanding REGISTER request is required.",
                "message");
        }
    }

    string? ReadCallId(SipMessage message)
    {
        string? value = null;
        int index = 0;
        while (index < message.Headers.Length)
        {
            SipHeader header = message.Headers[index];
            if (header != null && (SipAscii.EqualsIgnoreCase(
                    header.Name, SipHeaderNames.CallId)
                || SipAscii.EqualsIgnoreCase(
                    header.Name, SipHeaderNames.CompactCallId)))
            {
                if (value != null)
                {
                    return null;
                }
                value = header.Value;
            }
            index = index + 1;
        }
        return value;
    }

    SipRegistrationRequest CopyOperationRequest(SipRegistrationRequest source)
    {
        SipRegistrationRequest copy = new SipRegistrationRequest();
        copy.AddressOfRecord = source.AddressOfRecord == null
            ? null : SipUriModelCopy.CreateNormalizedCopy(source.AddressOfRecord);
        copy.Contacts = SipSnapshotCopy.CopyContactArray(source.Contacts);
        copy.UnregisterAll = source.UnregisterAll;
        copy.Flows = SipSnapshotCopy.CopyFlowArray(source.Flows);
        copy.Options = CopyRegistrationOptions(source.Options);
        copy.Target = source.Target == null
            ? null : SipUriModelCopy.CreateNormalizedCopy(source.Target);
        copy.OutboundProxy = source.OutboundProxy == null
            ? null : SipUriModelCopy.CreateNormalizedCopy(source.OutboundProxy);
        copy.ServiceRoute = SipRouteRouting.CopyRouteSet(source.ServiceRoute);
        copy.Cancellation = source.Cancellation;
        copy.CorrelationId = source.CorrelationId;
        copy.AuthenticationContextId = source.AuthenticationContextId;
        return copy;
    }

    SipRegistrationOptions CopyRegistrationOptions(SipRegistrationOptions source)
    {
        SipRegistrationOptions copy = new SipRegistrationOptions();
        copy.ExpiresSeconds = source.ExpiresSeconds;
        copy.MinExpiresSeconds = source.MinExpiresSeconds;
        copy.MaxExpiresSeconds = source.MaxExpiresSeconds;
        copy.RefreshBeforeSeconds = source.RefreshBeforeSeconds;
        copy.RefreshJitterSeconds = source.RefreshJitterSeconds;
        copy.RetryDelaySeconds = source.RetryDelaySeconds;
        copy.MaxAuthenticationAttempts = source.MaxAuthenticationAttempts;
        copy.FollowRedirects = source.FollowRedirects;
        copy.MaxRedirects = source.MaxRedirects;
        copy.UseOutbound = source.UseOutbound;
        copy.UseGruu = source.UseGruu;
        return copy;
    }

    SipRegistrationBinding[] CopyBindingArray(SipRegistrationBinding[] source)
    {
        return SipSnapshotCopy.CopyRegistrationBindingArray(source);
    }

    SipRegistrationSnapshot CopyCurrentSnapshot()
    {
        SipRegistrationSnapshot? copy =
            SipSnapshotCopy.CopyRegistrationSnapshot(snapshot);
        return copy == null ? new SipRegistrationSnapshot() : copy;
    }

    long AddMilliseconds(long start, long duration)
    {
        return duration > long.MaxValue - start ? long.MaxValue : start + duration;
    }

    SipRegistrationRequest CreateUnregistrationRequest(
        SipRegistrationRequest source)
    {
        SipRegistrationRequest copy = CopyOperationRequest(source);
        if (source.UnregisterAll)
        {
            SipContactValue wildcard = new SipContactValue();
            wildcard.Wildcard = true;
            copy.Contacts = new SipContactValue[] { wildcard };
        }
        else
        {
            int index = 0;
            while (index < copy.Contacts.Length)
            {
                copy.Contacts[index].ExpiresSeconds = 0;
                index = index + 1;
            }
        }
        return copy;
    }

    void InitializeOutboundFlows()
    {
        if (snapshot == null || operationRequest == null
            || !operationRequest.Options.UseOutbound)
        {
            return;
        }
        if (flowManager.CountFlows() == 0)
        {
            int index = 0;
            while (index < operationRequest.Flows.Length)
            {
                SipFlow configured = operationRequest.Flows[index];
                SipContactValue? contact = FindFlowContact(
                    configured.InstanceId, configured.RegistrationId);
                if (contact == null || configured.Connection == null)
                {
                    throw new SipRegistrationException(
                        "An Outbound flow does not match an active Contact.",
                        snapshot.State, snapshot.AddressOfRecord);
                }
                long expiry = FindFlowExpiry(contact);
                flowManager.RegisterFlow(contact, configured.Connection,
                    configured.Path, configured.FlowTimerSeconds, expiry,
                    configured.Primary);
                index = index + 1;
            }
        }
        snapshot.Flows = flowManager.CopyFlows();
        SipContactValue? firstContact = snapshot.Bindings.Length == 0
            ? null : snapshot.Bindings[0].Contact;
        string? instanceId = firstContact == null
            ? null : firstContact.InstanceId;
        snapshot.ActiveFlow = flowManager.SelectFlow(instanceId);
        UpdateBindingFlows();
    }

    SipContactValue? FindFlowContact(string? instanceId, int registrationId)
    {
        if (snapshot != null)
        {
            int bindingIndex = 0;
            while (bindingIndex < snapshot.Bindings.Length)
            {
                SipContactValue? candidate = snapshot.Bindings[bindingIndex].Contact;
                if (candidate != null && candidate.RegistrationId == registrationId
                    && SameText(candidate.InstanceId, instanceId))
                {
                    return candidate;
                }
                bindingIndex = bindingIndex + 1;
            }
        }
        if (operationRequest != null)
        {
            int contactIndex = 0;
            while (contactIndex < operationRequest.Contacts.Length)
            {
                SipContactValue candidate = operationRequest.Contacts[contactIndex];
                if (candidate.RegistrationId == registrationId
                    && SameText(candidate.InstanceId, instanceId))
                {
                    return candidate;
                }
                contactIndex = contactIndex + 1;
            }
        }
        return null;
    }

    long FindFlowExpiry(SipContactValue contact)
    {
        if (snapshot != null)
        {
            int index = 0;
            while (index < snapshot.Bindings.Length)
            {
                SipRegistrationBinding binding = snapshot.Bindings[index];
                if (binding.Contact != null
                    && SipContactRouting.ContactsMatch(binding.Contact, contact))
                {
                    return binding.ExpiresAtMilliseconds;
                }
                index = index + 1;
            }
        }
        return AddMilliseconds(nowMilliseconds,
            operationRequest == null ? 0
                : (long)operationRequest.Options.ExpiresSeconds * 1000);
    }

    void UpdateBindingFlows()
    {
        if (snapshot == null)
        {
            return;
        }
        int index = 0;
        while (index < snapshot.Bindings.Length)
        {
            SipRegistrationBinding binding = snapshot.Bindings[index];
            SipContactValue? contact = binding.Contact;
            SipFlow? selected = contact == null ? null
                : FindManagedFlow(contact.InstanceId, contact.RegistrationId);
            binding.Flow = SipSnapshotCopy.CopyFlowModel(selected);
            index = index + 1;
        }
    }

    SipFlow? FindManagedFlow(string? instanceId, int registrationId)
    {
        SipFlow[] flows = flowManager.CopyFlows();
        int index = 0;
        while (index < flows.Length)
        {
            if (flows[index].RegistrationId == registrationId
                && SameText(flows[index].InstanceId, instanceId))
            {
                return flows[index];
            }
            index = index + 1;
        }
        return null;
    }

    void HandleUnassociatedTransportFailure(SipTransportEvent transportEvent)
    {
        if (snapshot == null || operationRequest == null)
        {
            return;
        }
        snapshot.State = registrationSucceeded
            ? SipRegistrationState.Registered : SipRegistrationState.Failed;
        snapshot.RefreshAtMilliseconds = -1;
        snapshot.RetryAtMilliseconds = AddMilliseconds(nowMilliseconds,
            (long)operationRequest.Options.RetryDelaySeconds * 1000);
        snapshot.ErrorMessage = transportEvent.ErrorMessage == null
            ? "The registration transport failed."
            : transportEvent.ErrorMessage;
        outstandingRequest = null;
        flowRecoveryDue = false;
        QueueEvent(null, snapshot.ErrorMessage);
    }

    SipFlow? SelectConfiguredPrimaryFlow(SipFlow[] flows)
    {
        SipFlow? first = null;
        int index = 0;
        while (index < flows.Length)
        {
            if (first == null)
            {
                first = flows[index];
            }
            if (flows[index].Primary && !flows[index].Failed)
            {
                return SipSnapshotCopy.CopyFlowModel(flows[index]);
            }
            index = index + 1;
        }
        return SipSnapshotCopy.CopyFlowModel(first);
    }

    long EarliestBindingExpiry(SipRegistrationBinding[] bindings)
    {
        long earliest = -1;
        int index = 0;
        while (index < bindings.Length)
        {
            if (bindings[index].ExpiresAtMilliseconds >= 0
                && (earliest < 0
                    || bindings[index].ExpiresAtMilliseconds < earliest))
            {
                earliest = bindings[index].ExpiresAtMilliseconds;
            }
            index = index + 1;
        }
        return earliest;
    }

    void AdoptRemainingRegistrationContacts()
    {
        if (snapshot == null || operationRequest == null)
        {
            return;
        }
        SipContactValue[] contacts = new SipContactValue[snapshot.Bindings.Length];
        int index = 0;
        while (index < contacts.Length)
        {
            SipContactValue? contact = snapshot.Bindings[index].Contact;
            SipContactValue[] copied = SipSnapshotCopy.CopyContactArray(
                contact == null ? new SipContactValue[0]
                    : new SipContactValue[] { contact });
            contacts[index] = copied.Length == 0
                ? new SipContactValue() : copied[0];
            index = index + 1;
        }
        operationRequest.Contacts = contacts;
        operationRequest.UnregisterAll = false;
    }

    bool SameText(string? left, string? right)
    {
        return left != null && right != null && left.Equals(
            right, System.StringComparison.OrdinalIgnoreCase);
    }

    void ValidateRequest(SipRegistrationRequest? request, bool unregistration)
    {
        if (request == null)
        {
            throw new System.ArgumentNullException("request");
        }
        if (request.AddressOfRecord == null
            || request.AddressOfRecord.Scheme != SipUriScheme.Sip
                && request.AddressOfRecord.Scheme != SipUriScheme.Sips
            || SipUriValidator.ValidateUriModel(request.AddressOfRecord).Length > 0)
        {
            throw new System.ArgumentException(
                "A valid SIP or SIPS address of record is required.", "request");
        }
        if (request.AddressOfRecord.Password != null
            || request.AddressOfRecord.Headers.Length > 0)
        {
            throw new System.ArgumentException(
                "An address of record cannot contain a password or URI headers.",
                "request");
        }
        if (request.Options == null
            || request.Options.MinExpiresSeconds < 0
            || request.Options.MaxExpiresSeconds < request.Options.MinExpiresSeconds
            || request.Options.ExpiresSeconds < request.Options.MinExpiresSeconds
            || request.Options.ExpiresSeconds > request.Options.MaxExpiresSeconds
            || request.Options.RefreshBeforeSeconds < 0
            || request.Options.RefreshJitterSeconds < 0
            || request.Options.RefreshJitterSeconds > 86400
            || request.Options.RetryDelaySeconds < 0
            || request.Options.MaxAuthenticationAttempts < 0
            || request.Options.MaxRedirects < 0)
        {
            throw new System.ArgumentException(
                "Valid registration expiry bounds are required.", "request");
        }
        if (request.Contacts == null
            || !unregistration && request.UnregisterAll
            || unregistration && request.UnregisterAll
                && request.Contacts.Length > 1
            || unregistration && request.UnregisterAll
                && request.Contacts.Length == 1
                && (request.Contacts[0] == null
                    || !request.Contacts[0].Wildcard)
            || !request.UnregisterAll && request.Contacts.Length == 0)
        {
            throw new System.ArgumentException(
                "The registration Contact selection is invalid.", "request");
        }
        if (!request.UnregisterAll)
        {
            ValidateContacts(request.Contacts, unregistration);
        }
        if (request.Options.UseGruu && !request.UnregisterAll
            && !HasInstanceContact(request.Contacts))
        {
            throw new System.ArgumentException(
                "GRUU registration requires an instance-identified Contact.",
                "request");
        }
        ValidateFlowConfiguration(request);
        CopyOptionalSipUri(request.Target, "target");
        CopyOptionalSipUri(request.OutboundProxy, "outboundProxy");
        SipRouteRouting.CopyRouteSet(request.ServiceRoute);
        if (request.CorrelationId != null)
        {
            ValidateIdentifier(request.CorrelationId, "correlationId");
        }
        if (request.AuthenticationContextId != null)
        {
            ValidateIdentifier(request.AuthenticationContextId,
                "authenticationContextId");
        }
    }

    void ValidateContacts(SipContactValue[] contacts, bool unregistration)
    {
        int index = 0;
        while (index < contacts.Length)
        {
            SipContactValue contact = contacts[index];
            SipUri? uri = contact == null || contact.Address == null
                ? null : contact.Address.Uri;
            if (contact == null || contact.Wildcard || uri == null
                || uri.Scheme != SipUriScheme.Sip && uri.Scheme != SipUriScheme.Sips
                || SipUriValidator.ValidateUriModel(uri).Length > 0
                || contact.ExpiresSeconds < -1
                || contact.QualityThousandths < -1
                || contact.QualityThousandths > 1000
                || contact.RegistrationId < -1)
            {
                throw new System.ArgumentException(
                    "Every registration Contact must contain a valid SIP or SIPS URI.",
                    "request");
            }
            if (!unregistration && contact.ExpiresSeconds == 0)
            {
                throw new System.ArgumentException(
                    "An active registration Contact cannot have zero expiry.",
                    "request");
            }
            index = index + 1;
        }
    }

    bool HasInstanceContact(SipContactValue[] contacts)
    {
        int index = 0;
        while (index < contacts.Length)
        {
            SipContactValue? contact = contacts[index];
            if (contact != null && contact.InstanceId != null) return true;
            index = index + 1;
        }
        return false;
    }

    void ValidateFlowConfiguration(SipRegistrationRequest request)
    {
        if (request.Flows == null
            || request.Options.UseOutbound && request.Flows.Length == 0
            || !request.Options.UseOutbound && request.Flows.Length > 0)
        {
            throw new System.ArgumentException(
                "Outbound flow candidates must match the Outbound option.",
                "request");
        }
        int index = 0;
        while (index < request.Flows.Length)
        {
            SipFlow flow = request.Flows[index];
            if (flow == null || flow.Connection == null
                || flow.InstanceId == null || flow.RegistrationId <= 0
                || flow.Path == null || flow.FlowTimerSeconds < -1
                || !FlowMatchesRequestContact(flow, request.Contacts))
            {
                throw new System.ArgumentException(
                    "Every Outbound flow must match one registration Contact.",
                    "request");
            }
            index = index + 1;
        }
    }

    bool FlowMatchesRequestContact(SipFlow flow, SipContactValue[] contacts)
    {
        int index = 0;
        while (index < contacts.Length)
        {
            SipContactValue contact = contacts[index];
            if (contact != null && contact.RegistrationId == flow.RegistrationId
                && SameText(contact.InstanceId, flow.InstanceId))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    void ValidateActiveRegistration()
    {
        if (snapshot == null || snapshot.State == SipRegistrationState.None
            || snapshot.State == SipRegistrationState.Terminated
            || snapshot.State == SipRegistrationState.Failed)
        {
            throw new System.InvalidOperationException(
                "An active registration operation is required.");
        }
    }

    void ValidateUnregistrationTargets(SipRegistrationRequest request)
    {
        if (request.UnregisterAll || snapshot == null || !registrationSucceeded)
        {
            return;
        }
        int requestIndex = 0;
        while (requestIndex < request.Contacts.Length)
        {
            bool found = false;
            int bindingIndex = 0;
            while (bindingIndex < snapshot.Bindings.Length)
            {
                SipRegistrationBinding binding = snapshot.Bindings[bindingIndex];
                if (binding.Contact != null && SipContactRouting.ContactsMatch(
                    request.Contacts[requestIndex], binding.Contact))
                {
                    found = true;
                }
                bindingIndex = bindingIndex + 1;
            }
            if (!found)
            {
                throw new System.ArgumentException(
                    "Every unregistered Contact must match an active binding.",
                    "request");
            }
            requestIndex = requestIndex + 1;
        }
    }

    void ValidateMatchingAddressOfRecord(SipUri? addressOfRecord)
    {
        if (snapshot == null || snapshot.AddressOfRecord == null
            || addressOfRecord == null
            || !SipUriComparison.CompareUriModels(
                snapshot.AddressOfRecord, addressOfRecord))
        {
            throw new System.ArgumentException(
                "The address of record does not match the active registration.",
                "request");
        }
    }

    void ValidateIdentifier(string? value, string parameterName)
    {
        if (value == null || value.Length == 0 || value.Length > 256)
        {
            throw new System.ArgumentException(
                "A non-empty identifier of at most 256 characters is required.",
                parameterName);
        }
    }

    void AdoptAuthenticationAssociation(string? authenticationContextId)
    {
        if (authenticationContextId != null && snapshot != null)
        {
            snapshot.AuthenticationContextId = authenticationContextId;
        }
    }
}
