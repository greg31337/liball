namespace sip;

internal class SipRegistrarCore
{
    object sync;
    ISipLocationService locationService;
    ISipAuthenticationServer? authenticationServer;
    SipNonceOptions? nonceOptions;
    long nowMilliseconds;
    SipProtocolTokenGenerator tokens;
    System.Collections.Generic.List<SipRegistrationEvent> events;
    long eventVersion;
    bool hasRegistrationEvent;

    internal SipRegistrarCore()
    {
        sync = new object();
        locationService = new SipLocationService();
        tokens = new SipProtocolTokenGenerator(new SipCryptographicRandomSource());
        events = new System.Collections.Generic.List<SipRegistrationEvent>();
    }

    internal void ConfigureAuthenticationServer(
        ISipAuthenticationServer configuredAuthenticationServer,
        SipNonceOptions configuredNonceOptions)
    {
        if (configuredAuthenticationServer == null)
        {
            throw new System.ArgumentNullException("authenticationServer");
        }
        if (configuredNonceOptions == null)
        {
            throw new System.ArgumentNullException("nonceOptions");
        }
        lock (sync)
        {
            authenticationServer = configuredAuthenticationServer;
            nonceOptions = CopyNonceOptions(configuredNonceOptions);
        }
    }

    internal void ConfigureLocationService(
        ISipLocationService configuredLocationService)
    {
        if (configuredLocationService == null)
        {
            throw new System.ArgumentNullException("locationService");
        }
        lock (sync)
        {
            locationService = configuredLocationService;
        }
    }

    internal SipRegistrationSnapshot Process(SipMessage? message,
        SipRegistrarOptions? options)
    {
        lock (sync)
        {
            string? optionError = ValidateOptions(options);
            if (optionError != null)
            {
                return Failure(message, null, null, -1, new SipRegistrationBinding[0],
                    optionError);
            }
            if (options == null)
            {
                return Failure(message, null, null, -1, new SipRegistrationBinding[0],
                    "Registrar policy is required.");
            }
            if (message == null || message.Kind != SipMessageKind.Request
                || message.Method != SipMethod.Register
                || !SipAscii.EqualsIgnoreCase(message.MethodToken,
                    SipMethods.Register))
            {
                return Failure(message, null, null, -1, new SipRegistrationBinding[0],
                    "A REGISTER request is required.");
            }
            SipUri? addressOfRecord = message.To == null ? null : message.To.Uri;
            string? requestError = ValidateRequestAddresses(
                message, addressOfRecord, options);
            string? callId = ReadCallId(message);
            int cseq = message.CSeq == null ? -1 : message.CSeq.SequenceNumber;
            if (requestError != null)
            {
                return Failure(message, addressOfRecord, callId, cseq,
                    new SipRegistrationBinding[0], requestError);
            }
            if (callId == null)
            {
                return Failure(message, addressOfRecord, null, cseq,
                    new SipRegistrationBinding[0],
                    "A REGISTER request requires exactly one valid Call-ID.");
            }
            if (message.CSeq == null || message.CSeq.SequenceNumber < 0
                || message.CSeq.Method != SipMethod.Register
                || !SipAscii.EqualsIgnoreCase(message.CSeq.MethodToken,
                    SipMethods.Register))
            {
                return Failure(message, addressOfRecord, callId, cseq,
                    new SipRegistrationBinding[0],
                    "A REGISTER request requires a matching REGISTER CSeq.");
            }
            if (addressOfRecord == null)
            {
                return Failure(message, null, callId, cseq,
                    new SipRegistrationBinding[0],
                    "A REGISTER address of record is required.");
            }

            string? unsupported = ReadUnsupportedOptions(message);
            if (unsupported != null)
            {
                SipRegistrationSnapshot unsupportedFailure = Failure(
                    message, addressOfRecord, callId, cseq,
                    new SipRegistrationBinding[0], unsupported.Length == 0
                        ? "The REGISTER extension requirements are malformed."
                        : "The REGISTER request requires unsupported extensions: "
                            + unsupported);
                if (unsupported.Length > 0 && unsupportedFailure.Response != null)
                {
                    AppendHeader(unsupportedFailure.Response,
                        SipHeaderNames.Unsupported, unsupported);
                }
                return unsupportedFailure;
            }
            SipRouteValue[]? path = ReadPathRoutes(message);
            if (path == null)
            {
                return Failure(message, addressOfRecord, callId, cseq,
                    new SipRegistrationBinding[0],
                    "The REGISTER request contains a malformed Path header.");
            }
            bool supportsGruu = HasOptionTag(
                message, SipHeaderNames.Supported, SipOptionTags.Gruu);

            SipDigestAuthorization? authenticationInformation = null;
            if (options.RequireAuthentication)
            {
                if (authenticationServer == null || nonceOptions == null)
                {
                    return Failure(message, addressOfRecord, callId, cseq,
                        new SipRegistrationBinding[0],
                        "Registrar authentication is required but is not configured.");
                }
                SipAuthenticationRequest authenticationRequest =
                    CreateAuthenticationRequest(message);
                SipAuthenticationResult authentication =
                    authenticationServer.VerifyAuthentication(
                        authenticationRequest);
                if (authentication.Status != SipAuthenticationStatus.Success)
                {
                    if (authentication.Message == null)
                    {
                        authentication = authenticationServer
                            .CreateAuthenticationChallenge(
                                authenticationRequest, nonceOptions);
                    }
                    SipRegistrationSnapshot challenged = Failure(
                        message, addressOfRecord, callId, cseq,
                        new SipRegistrationBinding[0],
                        authentication.ErrorMessage == null
                            ? "Registration authentication is required."
                            : authentication.ErrorMessage);
                    challenged.Response = AddCommonResponseHeaders(
                        authentication.Message);
                    return challenged;
                }
                authenticationInformation = authentication.DigestAuthorization;
            }

            SipLocationQuery query = new SipLocationQuery();
            query.AddressOfRecord = addressOfRecord;
            query.NowMilliseconds = nowMilliseconds;
            SipLocationResult current = locationService.QueryLocation(query);
            if (!current.Success)
            {
                return Failure(message, addressOfRecord, callId, cseq,
                    new SipRegistrationBinding[0], current.ErrorMessage == null
                        ? "The registrar location lookup failed."
                        : "The registrar location lookup failed: "
                            + current.ErrorMessage);
            }

            SipContactValue[] contacts = message.ContactValues == null
                ? new SipContactValue[0] : message.ContactValues;
            if (contacts.Length == 0)
            {
                return Success(message, addressOfRecord, callId, cseq,
                    current.Bindings, authenticationInformation);
            }

            int expiresHeader = ReadExpiresHeader(message);
            if (expiresHeader < -1)
            {
                return Failure(message, addressOfRecord, callId, cseq, current.Bindings,
                    "The REGISTER request contains an invalid or duplicate Expires header.");
            }
            string? contactError = ValidateContacts(
                contacts, expiresHeader, options);
            if (contactError != null)
            {
                SipRegistrationSnapshot contactFailure = Failure(
                    message, addressOfRecord, callId, cseq, current.Bindings,
                    contactError);
                if (contactError.IndexOf("registrar minimum",
                        System.StringComparison.Ordinal) >= 0
                    && contactFailure.Response != null)
                {
                    AppendHeader(contactFailure.Response,
                        SipHeaderNames.MinExpires,
                        options.MinExpiresSeconds.ToString(
                            System.Globalization.CultureInfo.InvariantCulture));
                }
                return contactFailure;
            }

            int previousCSeq = FindCSeq(current.Bindings, callId);
            if (previousCSeq > cseq)
            {
                return Failure(message, addressOfRecord, callId, cseq, current.Bindings,
                    "The REGISTER CSeq is older than the stored Call-ID sequence.");
            }
            if (previousCSeq == cseq)
            {
                return Success(message, addressOfRecord, callId, cseq,
                    current.Bindings, authenticationInformation);
            }

            SipRegistrationBinding[] replacements = BuildReplacements(
                current.Bindings, addressOfRecord, contacts, callId, cseq,
                expiresHeader, options, nowMilliseconds, path, supportsGruu);
            if (replacements.Length > options.MaxBindingCount)
            {
                return Failure(message, addressOfRecord, callId, cseq, current.Bindings,
                    "The REGISTER request exceeds the binding capacity policy.");
            }

            SipLocationUpdate update = new SipLocationUpdate();
            update.AddressOfRecord = addressOfRecord;
            update.Bindings = replacements;
            SipLocationResult committed = locationService.ReplaceLocations(update);
            if (!committed.Success)
            {
                return Failure(message, addressOfRecord, callId, cseq, current.Bindings,
                    committed.ErrorMessage == null
                        ? "The registrar location update failed."
                        : "The registrar location update failed: "
                            + committed.ErrorMessage);
            }
            SipRegistrationSnapshot result = Success(
                message, addressOfRecord, callId, cseq,
                committed.Bindings, authenticationInformation);
            QueueRegistrationChanges(
                current.Bindings, committed.Bindings, result);
            return result;
        }
    }

    internal void Expire(long currentMilliseconds)
    {
        if (currentMilliseconds < 0)
        {
            throw new System.ArgumentOutOfRangeException("nowMilliseconds");
        }
        lock (sync)
        {
            if (currentMilliseconds < nowMilliseconds)
            {
                throw new System.ArgumentOutOfRangeException("nowMilliseconds");
            }
            nowMilliseconds = currentMilliseconds;
            SipLocationResult expired =
                locationService.ExpireLocations(currentMilliseconds);
            if (expired.Success)
            {
                int index = 0;
                while (index < expired.ExpiredBindings.Length)
                {
                    SipRegistrationBinding binding =
                        expired.ExpiredBindings[index];
                    SipRegistrationBinding[] remaining = SelectAddressBindings(
                        expired.Bindings, binding.AddressOfRecord);
                    SipRegistrationSnapshot snapshot = CreateSnapshot(
                        binding.AddressOfRecord, binding.CallId,
                        binding.CSeq, remaining);
                    snapshot.State = remaining.Length == 0
                        ? SipRegistrationState.Terminated
                        : SipRegistrationState.Registered;
                    QueueContactEvent(snapshot, binding, "expired",
                        false, HasPushData(binding.Contact));
                    index = index + 1;
                }
            }
        }
    }

    internal SipRegistrationSnapshot UpdateBindingStatus(
        SipRegistrationBinding? requested)
    {
        lock (sync)
        {
            if (requested == null || requested.AddressOfRecord == null
                || requested.Contact == null)
            {
                return Failure(null, null, null, -1,
                    new SipRegistrationBinding[0],
                    "A binding identity and status metadata are required.");
            }
            SipLocationQuery query = new SipLocationQuery();
            query.AddressOfRecord = requested.AddressOfRecord;
            query.NowMilliseconds = nowMilliseconds;
            SipLocationResult current = locationService.QueryLocation(query);
            if (!current.Success)
            {
                return Failure(null, requested.AddressOfRecord,
                    requested.CallId, requested.CSeq,
                    new SipRegistrationBinding[0],
                    current.ErrorMessage == null
                        ? "The registrar location lookup failed."
                        : current.ErrorMessage);
            }
            int match = FindBinding(current.Bindings, requested);
            if (match < 0)
            {
                return Failure(null, requested.AddressOfRecord,
                    requested.CallId, requested.CSeq, current.Bindings,
                    "The registrar binding status target was not found.");
            }
            SipRegistrationBinding stored =
                SipSnapshotCopy.CopyRegistrationBindingArray(
                    new SipRegistrationBinding[] {
                        current.Bindings[match] })[0];
            bool flowChanged = !FlowStatusMatches(
                stored.Flow, requested.Flow);
            bool pushChanged = !PushDataMatches(
                stored.Contact, requested.Contact);
            if (!flowChanged && !pushChanged)
            {
                return CreateActiveSnapshot(current.Bindings, stored);
            }
            stored.Flow = SipSnapshotCopy.CopyFlowModel(requested.Flow);
            CopyPushData(stored.Contact, requested.Contact);
            SipLocationResult committed = locationService.UpdateLocation(stored);
            if (!committed.Success)
            {
                return Failure(null, requested.AddressOfRecord,
                    stored.CallId, stored.CSeq, current.Bindings,
                    committed.ErrorMessage == null
                        ? "The registrar binding status update failed."
                        : committed.ErrorMessage);
            }
            SipRegistrationSnapshot snapshot =
                CreateActiveSnapshot(committed.Bindings, stored);
            SipRegistrationBinding eventBinding = FindEventBinding(
                committed.Bindings, stored);
            QueueContactEvent(snapshot, eventBinding, "refreshed",
                flowChanged, pushChanged);
            return snapshot;
        }
    }

    internal SipRegistrationEvent PollEvent()
    {
        lock (sync)
        {
            if (events.Count == 0)
            {
                return new SipRegistrationEvent();
            }
            SipRegistrationEvent value = events[0];
            events.RemoveAt(0);
            SipRegistrationEvent? copy =
                SipSnapshotCopy.CopyRegistrationEvent(value);
            return copy == null ? new SipRegistrationEvent() : copy;
        }
    }

    void QueueRegistrationChanges(SipRegistrationBinding[] previous,
        SipRegistrationBinding[] current,
        SipRegistrationSnapshot snapshot)
    {
        int index = 0;
        while (index < previous.Length)
        {
            SipRegistrationBinding before = previous[index];
            if (FindBinding(current, before) < 0)
            {
                QueueContactEvent(snapshot, before, "unregistered",
                    before.Flow != null, HasPushData(before.Contact));
            }
            index = index + 1;
        }
        index = 0;
        while (index < current.Length)
        {
            SipRegistrationBinding after = current[index];
            int previousIndex = FindBinding(previous, after);
            if (previousIndex < 0)
            {
                QueueContactEvent(snapshot, after, "registered",
                    after.Flow != null, HasPushData(after.Contact));
            }
            else
            {
                SipRegistrationBinding before = previous[previousIndex];
                bool flowChanged = !FlowStatusMatches(
                    before.Flow, after.Flow);
                bool pushChanged = !PushDataMatches(
                    before.Contact, after.Contact);
                if (BindingUpdateChanged(before, after)
                    || flowChanged || pushChanged)
                {
                    QueueContactEvent(snapshot, after,
                        after.ExpiresAtMilliseconds
                            < before.ExpiresAtMilliseconds
                            ? "shortened" : "refreshed",
                        flowChanged, pushChanged);
                }
            }
            index = index + 1;
        }
    }

    void QueueContactEvent(SipRegistrationSnapshot snapshot,
        SipRegistrationBinding binding, string reason,
        bool flowChanged, bool pushChanged)
    {
        SipRegistrationEvent value = new SipRegistrationEvent();
        value.State = snapshot.State;
        value.Snapshot = SipSnapshotCopy.CopyRegistrationSnapshot(snapshot);
        value.Reason = reason;
        value.CorrelationId = snapshot.CorrelationId;
        value.Version = eventVersion;
        value.DocumentState = hasRegistrationEvent ? "partial" : "full";
        value.RegistrationState = snapshot.Bindings.Length == 0
            ? "terminated" : "active";
        value.ContactState = reason == "expired" || reason == "unregistered"
            || reason == "deactivated" || reason == "probation"
            || reason == "rejected" ? "terminated" : "active";
        SipRegistrationBinding[] copied =
            SipSnapshotCopy.CopyRegistrationBindingArray(
                new SipRegistrationBinding[] { binding });
        value.Binding = copied[0];
        value.FlowChanged = flowChanged;
        value.PushChanged = pushChanged;
        events.Add(value);
        hasRegistrationEvent = true;
        if (eventVersion < long.MaxValue)
        {
            eventVersion = eventVersion + 1;
        }
    }

    static int FindBinding(SipRegistrationBinding[] bindings,
        SipRegistrationBinding target)
    {
        int index = 0;
        while (index < bindings.Length)
        {
            SipRegistrationBinding candidate = bindings[index];
            if (candidate.InstanceId != null && target.InstanceId != null
                && candidate.RegistrationId > 0
                && target.RegistrationId > 0)
            {
                if (candidate.RegistrationId == target.RegistrationId
                    && candidate.InstanceId.Equals(target.InstanceId,
                        System.StringComparison.OrdinalIgnoreCase))
                {
                    return index;
                }
            }
            else if (RegistrarContactsMatch(
                    candidate.Contact, target.Contact))
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    static bool BindingUpdateChanged(SipRegistrationBinding previous,
        SipRegistrationBinding current)
    {
        return previous.CallId != current.CallId
            || previous.CSeq != current.CSeq
            || previous.ExpiresAtMilliseconds != current.ExpiresAtMilliseconds
            || previous.QualityThousandths != current.QualityThousandths;
    }

    static bool RegistrarContactsMatch(SipContactValue? left,
        SipContactValue? right)
    {
        if (SipContactRouting.ContactsMatch(left, right))
        {
            return true;
        }
        if (left == null || right == null
            || left.Wildcard || right.Wildcard)
        {
            return false;
        }
        SipContactValue[] leftCopy = SipSnapshotCopy.CopyContactArray(
            new SipContactValue[] { left });
        SipContactValue[] rightCopy = SipSnapshotCopy.CopyContactArray(
            new SipContactValue[] { right });
        RemovePushIdentityParameters(leftCopy[0]);
        RemovePushIdentityParameters(rightCopy[0]);
        return SipContactRouting.ContactsMatch(leftCopy[0], rightCopy[0]);
    }

    static void RemovePushIdentityParameters(SipContactValue contact)
    {
        SipUri? uri = contact.Address == null
            ? null : contact.Address.Uri;
        if (uri == null)
        {
            return;
        }
        SipUriParameter[] kept =
            new SipUriParameter[uri.Parameters.Length];
        int count = 0;
        int index = 0;
        while (index < uri.Parameters.Length)
        {
            SipUriParameter parameter = uri.Parameters[index];
            if (!IsPushIdentityParameter(parameter.Name))
            {
                kept[count] = parameter;
                count = count + 1;
            }
            index = index + 1;
        }
        SipUriParameter[] result = new SipUriParameter[count];
        if (count > 0)
        {
            System.Array.Copy(kept, result, count);
        }
        uri.Parameters = result;
    }

    static bool IsPushIdentityParameter(string? name)
    {
        return SipAscii.EqualsIgnoreCase(name, "pn-provider")
            || SipAscii.EqualsIgnoreCase(name, "pn-param")
            || SipAscii.EqualsIgnoreCase(name, "pn-prid")
            || SipAscii.EqualsIgnoreCase(name, "pn-purr");
    }

    static bool FlowStatusMatches(SipFlow? left, SipFlow? right)
    {
        if (left == null || right == null)
        {
            return left == null && right == null;
        }
        return SameStatusText(left.Id, right.Id)
            && left.Failed == right.Failed
            && left.Primary == right.Primary
            && left.NextKeepAliveAtMilliseconds
                == right.NextKeepAliveAtMilliseconds
            && left.KeepAliveResponseDeadlineMilliseconds
                == right.KeepAliveResponseDeadlineMilliseconds
            && left.RetryAtMilliseconds == right.RetryAtMilliseconds
            && left.ConsecutiveFailures == right.ConsecutiveFailures
            && left.ExpiresAtMilliseconds == right.ExpiresAtMilliseconds;
    }

    static bool PushDataMatches(SipContactValue? left,
        SipContactValue? right)
    {
        if (left == null || right == null)
        {
            return left == null && right == null;
        }
        return SameStatusText(left.PushProvider, right.PushProvider)
            && SameStatusText(left.PushParameter, right.PushParameter)
            && SameStatusText(left.PushResourceId, right.PushResourceId)
            && SameStatusText(left.PushRegistrationReference,
                right.PushRegistrationReference);
    }

    static bool SameStatusText(string? left, string? right)
    {
        if (left == null || right == null)
        {
            return left == null && right == null;
        }
        return left.Equals(right, System.StringComparison.Ordinal);
    }

    static bool HasPushData(SipContactValue? contact)
    {
        return contact != null && (contact.PushProvider != null
            || contact.PushParameter != null
            || contact.PushResourceId != null
            || contact.PushRegistrationReference != null);
    }

    static void CopyPushData(SipContactValue? target,
        SipContactValue source)
    {
        if (target == null)
        {
            return;
        }
        target.PushProvider = source.PushProvider;
        target.PushParameter = source.PushParameter;
        target.PushResourceId = source.PushResourceId;
        target.PushRegistrationReference =
            source.PushRegistrationReference;
    }

    static SipRegistrationBinding[] SelectAddressBindings(
        SipRegistrationBinding[] bindings, SipUri? addressOfRecord)
    {
        SipRegistrationBinding[] selected =
            new SipRegistrationBinding[bindings.Length];
        int count = 0;
        int index = 0;
        while (index < bindings.Length)
        {
            if (SipUriComparison.CompareUriModels(
                    bindings[index].AddressOfRecord, addressOfRecord))
            {
                selected[count] = bindings[index];
                count = count + 1;
            }
            index = index + 1;
        }
        SipRegistrationBinding[] result =
            new SipRegistrationBinding[count];
        if (count > 0)
        {
            System.Array.Copy(selected, result, count);
        }
        return result;
    }

    static SipRegistrationSnapshot CreateActiveSnapshot(
        SipRegistrationBinding[] bindings,
        SipRegistrationBinding statusBinding)
    {
        SipRegistrationSnapshot snapshot = CreateSnapshot(
            statusBinding.AddressOfRecord, statusBinding.CallId,
            statusBinding.CSeq, bindings);
        snapshot.State = bindings.Length == 0
            ? SipRegistrationState.Terminated
            : SipRegistrationState.Registered;
        return snapshot;
    }

    static SipRegistrationBinding FindEventBinding(
        SipRegistrationBinding[] bindings,
        SipRegistrationBinding fallback)
    {
        int index = FindBinding(bindings, fallback);
        return index < 0 ? fallback : bindings[index];
    }

    static string? ValidateOptions(SipRegistrarOptions? options)
    {
        if (options == null || options.MinExpiresSeconds < 0
            || options.MaxExpiresSeconds < options.MinExpiresSeconds
            || options.DefaultExpiresSeconds < options.MinExpiresSeconds
            || options.DefaultExpiresSeconds > options.MaxExpiresSeconds
            || options.MaxBindingCount <= 0 || options.Domains == null)
        {
            return "Valid registrar expiry, capacity, and domain policy is required.";
        }
        int index = 0;
        while (index < options.Domains.Length)
        {
            string? domain = options.Domains[index];
            if (domain == null || domain.Length == 0
                || domain.IndexOf(' ') >= 0 || domain.IndexOf('\t') >= 0)
            {
                return "Every served registrar domain must be a nonempty host name.";
            }
            index = index + 1;
        }
        return null;
    }

    static string? ValidateRequestAddresses(SipMessage message,
        SipUri? addressOfRecord, SipRegistrarOptions options)
    {
        if (message.RequestUri != null
            && message.RequestUri.Scheme != SipUriScheme.Sip
            && message.RequestUri.Scheme != SipUriScheme.Sips)
        {
            return "The REGISTER request uses an unsupported URI scheme.";
        }
        if (!IsValidSipUri(message.RequestUri)
            || !IsValidAddressOfRecord(addressOfRecord)
            || message.From == null || !IsValidAddressOfRecord(message.From.Uri))
        {
            return "The REGISTER request target, To address, and From address must be valid SIP URIs.";
        }
        if (message.To != null && HasNamedParameter(message.To.Parameters, "tag"))
        {
            return "A REGISTER To address cannot contain a tag parameter.";
        }
        if (!IsServedDomain(addressOfRecord, options.Domains))
        {
            return "The registrar does not serve the requested address-of-record domain.";
        }
        if (!options.AllowThirdPartyRegistration
            && !SipUriComparison.CompareUriModels(addressOfRecord,
                message.From.Uri))
        {
            return "Third-party registration is not allowed by registrar policy.";
        }
        return null;
    }

    static bool IsValidSipUri(SipUri? uri)
    {
        return uri != null
            && (uri.Scheme == SipUriScheme.Sip || uri.Scheme == SipUriScheme.Sips)
            && SipUriValidator.ValidateUriModel(uri).Length == 0;
    }

    static bool IsValidAddressOfRecord(SipUri? uri)
    {
        return IsValidSipUri(uri) && uri != null
            && uri.User != null && uri.User.Length > 0
            && uri.Password == null && uri.Headers.Length == 0;
    }

    static bool IsServedDomain(SipUri? addressOfRecord, string[] domains)
    {
        if (domains.Length == 0)
        {
            return true;
        }
        int index = 0;
        while (index < domains.Length)
        {
            if (addressOfRecord != null
                && SipAscii.EqualsIgnoreCase(addressOfRecord.Host,
                    domains[index]))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static bool HasNamedParameter(SipUriParameter[]? parameters, string name)
    {
        if (parameters == null)
        {
            return false;
        }
        int index = 0;
        while (index < parameters.Length)
        {
            SipUriParameter? parameter = parameters[index];
            if (parameter != null && SipAscii.EqualsIgnoreCase(
                    parameter.Name, name))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static string? ReadCallId(SipMessage message)
    {
        string? value = null;
        int index = 0;
        while (index < message.Headers.Length)
        {
            SipHeader? header = message.Headers[index];
            if (header != null && (SipAscii.EqualsIgnoreCase(
                    header.Name, SipHeaderNames.CallId)
                || SipAscii.EqualsIgnoreCase(
                    header.Name, SipHeaderNames.CompactCallId)))
            {
                SipStructuredHeaderResult parsed =
                    SipStructuredHeaderCodec.ParseStructuredHeaderValue(
                        SipHeaderNames.CallId, header.Value,
                        new SipParseOptions());
                if (value != null || !parsed.Success || parsed.CallId == null)
                {
                    return null;
                }
                value = parsed.CallId;
            }
            index = index + 1;
        }
        return value;
    }

    static int ReadExpiresHeader(SipMessage message)
    {
        int value = -1;
        int index = 0;
        while (index < message.Headers.Length)
        {
            SipHeader? header = message.Headers[index];
            if (header != null && SipAscii.EqualsIgnoreCase(
                    header.Name, SipHeaderNames.Expires))
            {
                if (value >= 0)
                {
                    return -2;
                }
                SipCoreHeaderResult parsed = SipCoreHeaderCodec.ParseCoreHeaderValue(
                    SipHeaderNames.Expires, header.Value, new SipParseOptions());
                if (!parsed.Success || parsed.ExpiresSeconds < 0)
                {
                    return -2;
                }
                value = parsed.ExpiresSeconds;
            }
            index = index + 1;
        }
        return value;
    }

    static string? ValidateContacts(SipContactValue[] contacts,
        int expiresHeader, SipRegistrarOptions options)
    {
        int wildcardCount = 0;
        int activeOutboundCount = 0;
        int index = 0;
        while (index < contacts.Length)
        {
            SipContactValue? contact = contacts[index];
            if (contact == null)
            {
                return "A REGISTER Contact value cannot be null.";
            }
            if (contact.Wildcard)
            {
                wildcardCount = wildcardCount + 1;
            }
            else
            {
                string? error = ValidateConcreteContact(contact);
                if (error != null)
                {
                    return error;
                }
                int expires = GetRequestedExpires(
                    contact, expiresHeader, options.DefaultExpiresSeconds);
                if (expires > 0 && expires < options.MinExpiresSeconds)
                {
                    return "A REGISTER Contact expiry is below the registrar minimum.";
                }
                if (expires > 0 && contact.RegistrationId > 0)
                {
                    activeOutboundCount = activeOutboundCount + 1;
                }
                int earlier = 0;
                while (earlier < index)
                {
                    if (RegistrarContactsMatch(
                            contacts[earlier], contact))
                    {
                        return "A REGISTER request cannot contain duplicate Contact bindings.";
                    }
                    earlier = earlier + 1;
                }
            }
            index = index + 1;
        }
        if (wildcardCount > 0
            && (contacts.Length != 1 || expiresHeader != 0))
        {
            return "A wildcard Contact must be the only Contact and requires Expires zero.";
        }
        if (activeOutboundCount > 1)
        {
            return "A REGISTER request cannot contain multiple active reg-id Contacts.";
        }
        return null;
    }

    static string? ValidateConcreteContact(SipContactValue contact)
    {
        SipUri? uri = contact.Address == null ? null : contact.Address.Uri;
        if (uri != null && uri.Scheme != SipUriScheme.Sip
            && uri.Scheme != SipUriScheme.Sips)
        {
            return "A REGISTER Contact uses an unsupported URI scheme.";
        }
        if (!IsValidSipUri(uri) || contact.ExpiresSeconds < -1
            || contact.QualityThousandths < -1
            || contact.QualityThousandths > 1000
            || contact.RegistrationId < -1 || contact.RegistrationId == 0)
        {
            return "Every REGISTER Contact must be a valid concrete SIP binding.";
        }
        if (contact.RegistrationId > 0
            && (contact.InstanceId == null || contact.InstanceId.Length == 0))
        {
            return "A Contact reg-id requires an instance identifier.";
        }
        return null;
    }

    static int GetRequestedExpires(SipContactValue contact,
        int expiresHeader, int defaultExpires)
    {
        if (contact.ExpiresSeconds >= 0)
        {
            return contact.ExpiresSeconds;
        }
        if (expiresHeader >= 0)
        {
            return expiresHeader;
        }
        return defaultExpires;
    }

    static int FindCSeq(SipRegistrationBinding[] current, string callId)
    {
        int found = -1;
        int index = 0;
        while (index < current.Length)
        {
            if (current[index].CallId == callId && current[index].CSeq > found)
            {
                found = current[index].CSeq;
            }
            index = index + 1;
        }
        return found;
    }

    SipRegistrationBinding[] BuildReplacements(
        SipRegistrationBinding[] current, SipUri addressOfRecord,
        SipContactValue[] contacts, string callId, int cseq,
        int expiresHeader, SipRegistrarOptions options, long nowMilliseconds,
        SipRouteValue[] path, bool supportsGruu)
    {
        if (contacts.Length == 1 && contacts[0].Wildcard)
        {
            return new SipRegistrationBinding[0];
        }
        System.Collections.Generic.List<SipRegistrationBinding> updated =
            new System.Collections.Generic.List<SipRegistrationBinding>();
        int index = 0;
        while (index < current.Length)
        {
            updated.Add(current[index]);
            index = index + 1;
        }
        index = 0;
        while (index < contacts.Length)
        {
            SipContactValue contact = contacts[index];
            int expires = GetRequestedExpires(
                contact, expiresHeader, options.DefaultExpiresSeconds);
            if (expires > options.MaxExpiresSeconds)
            {
                expires = options.MaxExpiresSeconds;
            }
            int match = FindContact(updated, contact);
            if (expires == 0)
            {
                if (match >= 0)
                {
                    updated.RemoveAt(match);
                }
            }
            else
            {
                SipRegistrationBinding binding = CreateBinding(
                    addressOfRecord, contact, callId, cseq, expires,
                    nowMilliseconds, path, supportsGruu,
                    FindPublicGruu(updated, contact.InstanceId),
                    match < 0 ? null : updated[match]);
                if (match < 0)
                {
                    updated.Add(binding);
                }
                else
                {
                    updated[match] = binding;
                }
            }
            index = index + 1;
        }
        return updated.ToArray();
    }

    static int FindContact(
        System.Collections.Generic.List<SipRegistrationBinding> bindings,
        SipContactValue contact)
    {
        int index = 0;
        while (index < bindings.Count)
        {
            if (RegistrarContactsMatch(
                    bindings[index].Contact, contact))
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    static string? FindPublicGruu(
        System.Collections.Generic.List<SipRegistrationBinding> bindings,
        string? instanceId)
    {
        if (instanceId == null)
        {
            return null;
        }
        int index = 0;
        while (index < bindings.Count)
        {
            SipRegistrationBinding binding = bindings[index];
            if (binding.InstanceId != null
                && binding.InstanceId.Equals(instanceId,
                    System.StringComparison.OrdinalIgnoreCase)
                && binding.Contact != null
                && binding.Contact.PublicGruu != null)
            {
                return binding.Contact.PublicGruu;
            }
            index = index + 1;
        }
        return null;
    }

    SipRegistrationBinding CreateBinding(SipUri addressOfRecord,
        SipContactValue contact, string callId, int cseq, int expiresSeconds,
        long nowMilliseconds, SipRouteValue[] path, bool supportsGruu,
        string? existingPublicGruu,
        SipRegistrationBinding? existingBinding)
    {
        SipRegistrationBinding binding = new SipRegistrationBinding();
        binding.AddressOfRecord =
            SipUriModelCopy.CreateNormalizedCopy(addressOfRecord);
        SipContactValue[] copied = SipSnapshotCopy.CopyContactArray(
            new SipContactValue[] { contact });
        binding.Contact = copied[0];
        binding.CallId = callId;
        binding.CSeq = cseq;
        long duration = (long)expiresSeconds * 1000;
        binding.ExpiresAtMilliseconds = nowMilliseconds > long.MaxValue - duration
            ? long.MaxValue : nowMilliseconds + duration;
        binding.QualityThousandths = contact.QualityThousandths < 0
            ? 1000 : contact.QualityThousandths;
        binding.InstanceId = contact.InstanceId;
        binding.RegistrationId = contact.RegistrationId;
        binding.Path = CopyRoutes(path);
        if (existingBinding != null)
        {
            SipRegistrationBinding preserved =
                SipSnapshotCopy.CopyRegistrationBindingArray(
                    new SipRegistrationBinding[] { existingBinding })[0];
            binding.ReceivedAddress = preserved.ReceivedAddress;
            binding.ServiceRoute = preserved.ServiceRoute;
            binding.Flow = preserved.Flow;
        }
        if (binding.Contact != null && supportsGruu
            && binding.InstanceId != null)
        {
            binding.Contact.PublicGruu = existingPublicGruu == null
                ? CreateGruu(addressOfRecord, false) : existingPublicGruu;
            binding.Contact.TemporaryGruu = CreateGruu(addressOfRecord, true);
        }
        return binding;
    }

    string CreateGruu(SipUri addressOfRecord, bool temporary)
    {
        SipUri gruu;
        if (temporary)
        {
            gruu = new SipUri();
            gruu.Scheme = addressOfRecord.Scheme;
            gruu.SchemeToken = addressOfRecord.Scheme == SipUriScheme.Sips
                ? "sips" : "sip";
            gruu.User = tokens.CreateOpaqueToken();
            gruu.Host = addressOfRecord.Host;
            gruu.Port = addressOfRecord.Port;
        }
        else
        {
            gruu = SipUriModelCopy.CreateNormalizedCopy(addressOfRecord);
        }
        SipUriParameter parameter = new SipUriParameter();
        parameter.Name = "gr";
        parameter.HasValue = !temporary;
        parameter.Value = temporary ? null : tokens.CreateOpaqueToken();
        SipUriParameter[] parameters =
            new SipUriParameter[gruu.Parameters.Length + 1];
        System.Array.Copy(gruu.Parameters, parameters, gruu.Parameters.Length);
        parameters[gruu.Parameters.Length] = parameter;
        gruu.Parameters = parameters;
        SipWriteOptions options = new SipWriteOptions();
        options.Canonical = true;
        options.PreserveOriginal = false;
        SipWriteResult formatted = SipUriFormatter.FormatUriModel(gruu, options);
        return formatted.Success
            ? System.Text.Encoding.UTF8.GetString(formatted.Bytes) : "";
    }

    static SipRouteValue[] CopyRoutes(SipRouteValue[] source)
    {
        SipRegistrationBinding holder = new SipRegistrationBinding();
        holder.Path = source;
        return SipSnapshotCopy.CopyRegistrationBindingArray(
            new SipRegistrationBinding[] { holder })[0].Path;
    }

    static SipNonceOptions CopyNonceOptions(SipNonceOptions source)
    {
        SipNonceOptions copy = new SipNonceOptions();
        copy.LifetimeSeconds = source.LifetimeSeconds;
        copy.ClockSkewSeconds = source.ClockSkewSeconds;
        copy.Realm = source.Realm;
        copy.CurrentKey = CopyBytes(source.CurrentKey);
        copy.PreviousKey = CopyBytes(source.PreviousKey);
        copy.MaxReplayEntries = source.MaxReplayEntries;
        return copy;
    }

    static byte[] CopyBytes(byte[]? source)
    {
        if (source == null)
        {
            return new byte[0];
        }
        byte[] copy = new byte[source.Length];
        System.Array.Copy(source, copy, source.Length);
        return copy;
    }

    static SipAuthenticationRequest CreateAuthenticationRequest(
        SipMessage message)
    {
        SipAuthenticationRequest request = new SipAuthenticationRequest();
        request.Message = SipSnapshotCopy.CopyMessageModel(message);
        request.Target = message.RequestUri == null ? null
            : SipUriModelCopy.CreateNormalizedCopy(message.RequestUri);
        request.Body = CopyBytes(message.Body);
        request.Scope = SipCredentialScope.Origin;
        return request;
    }

    static string? ReadUnsupportedOptions(SipMessage message)
    {
        System.Collections.Generic.List<string> unsupported =
            new System.Collections.Generic.List<string>();
        int index = 0;
        while (index < message.Headers.Length)
        {
            SipHeader? header = message.Headers[index];
            if (header != null && (SipAscii.EqualsIgnoreCase(
                    header.Name, SipHeaderNames.Require)
                || SipAscii.EqualsIgnoreCase(
                    header.Name, SipHeaderNames.ProxyRequire)))
            {
                SipCoreHeaderResult parsed = SipCoreHeaderCodec.ParseCoreHeaderValue(
                    header.Name, header.Value, new SipParseOptions());
                if (!parsed.Success)
                {
                    return "";
                }
                int optionIndex = 0;
                while (optionIndex < parsed.OptionTags.Length)
                {
                    string option = parsed.OptionTags[optionIndex];
                    if (!IsRegistrarOption(option)
                        && !ContainsText(unsupported, option))
                    {
                        unsupported.Add(option);
                    }
                    optionIndex = optionIndex + 1;
                }
            }
            index = index + 1;
        }
        if (unsupported.Count == 0)
        {
            return null;
        }
        System.Text.StringBuilder text = new System.Text.StringBuilder();
        index = 0;
        while (index < unsupported.Count)
        {
            if (index > 0)
            {
                text.Append(", ");
            }
            text.Append(unsupported[index]);
            index = index + 1;
        }
        return text.ToString();
    }

    static bool IsRegistrarOption(string option)
    {
        return SipAscii.EqualsIgnoreCase(option, SipOptionTags.Path)
            || SipAscii.EqualsIgnoreCase(option, SipOptionTags.Outbound)
            || SipAscii.EqualsIgnoreCase(option, SipOptionTags.Gruu);
    }

    static bool ContainsText(
        System.Collections.Generic.List<string> values, string target)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (SipAscii.EqualsIgnoreCase(values[index], target))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static bool HasOptionTag(SipMessage message, string headerName,
        string target)
    {
        int index = 0;
        while (index < message.Headers.Length)
        {
            SipHeader? header = message.Headers[index];
            if (header != null && SipAscii.EqualsIgnoreCase(
                    header.Name, headerName))
            {
                SipCoreHeaderResult parsed = SipCoreHeaderCodec.ParseCoreHeaderValue(
                    headerName, header.Value, new SipParseOptions());
                int optionIndex = 0;
                while (parsed.Success
                    && optionIndex < parsed.OptionTags.Length)
                {
                    if (SipAscii.EqualsIgnoreCase(
                            parsed.OptionTags[optionIndex], target))
                    {
                        return true;
                    }
                    optionIndex = optionIndex + 1;
                }
            }
            index = index + 1;
        }
        return false;
    }

    static SipRouteValue[]? ReadPathRoutes(SipMessage message)
    {
        System.Collections.Generic.List<SipRouteValue> routes =
            new System.Collections.Generic.List<SipRouteValue>();
        int index = 0;
        while (index < message.Headers.Length)
        {
            SipHeader? header = message.Headers[index];
            if (header != null && SipAscii.EqualsIgnoreCase(
                    header.Name, SipHeaderNames.Path))
            {
                SipStructuredHeaderResult parsed = SipStructuredHeaderCodec
                    .ParseStructuredHeaderValue(SipHeaderNames.Path,
                        header.Value, new SipParseOptions());
                if (!parsed.Success)
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
            index = index + 1;
        }
        return CopyRoutes(routes.ToArray());
    }

    SipMessage? CreateSuccessfulResponse(SipMessage request,
        SipRegistrationBinding[] bindings,
        SipDigestAuthorization? authenticationInformation)
    {
        if (!CanCreateResponse(request))
        {
            return null;
        }
        SipMessage response = SipTransactionMessageFactory.CreateRequestResponse(
            request, SipStatusCodes.Ok, SipReasonPhrases.Ok,
            tokens.CreateTag());
        AddCommonResponseHeaders(response);
        SipContactValue[] contacts = CreateResponseContacts(bindings);
        response.ContactValues = contacts;
        AppendContactHeader(response, contacts);
        CopyNamedHeaders(request, response, SipHeaderNames.Path);
        if (HasOptionTag(request, SipHeaderNames.Supported,
                SipOptionTags.Outbound)
            && HasActiveOutboundBinding(bindings))
        {
            AppendHeader(response, SipHeaderNames.Require,
                SipOptionTags.Outbound);
        }
        if (authenticationInformation != null)
        {
            string? information = SipAuthenticationHeaderCodec
                .FormatAuthenticationInfoValue(authenticationInformation);
            if (information != null)
            {
                AppendHeader(response, SipHeaderNames.AuthenticationInfo,
                    information);
            }
        }
        return response;
    }

    SipMessage? CreateFailureResponse(SipMessage? request, string error)
    {
        if (!CanCreateResponse(request) || request == null)
        {
            return null;
        }
        int status = GetFailureStatus(error);
        SipMessage response = SipTransactionMessageFactory.CreateRequestResponse(
            request, status, GetReasonPhrase(status), tokens.CreateTag());
        return AddCommonResponseHeaders(response);
    }

    static int GetFailureStatus(string error)
    {
        if (error == "A REGISTER request is required.")
        {
            return SipStatusCodes.MethodNotAllowed;
        }
        if (error.IndexOf("unsupported URI scheme",
                System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return SipStatusCodes.UnsupportedUriScheme;
        }
        if (error.IndexOf("registrar minimum",
                System.StringComparison.Ordinal) >= 0)
        {
            return SipStatusCodes.IntervalTooBrief;
        }
        if (error.IndexOf("unsupported extensions",
                System.StringComparison.Ordinal) >= 0)
        {
            return SipStatusCodes.BadExtension;
        }
        if (error.IndexOf("does not serve",
                System.StringComparison.Ordinal) >= 0
            || error.IndexOf("Third-party",
                System.StringComparison.Ordinal) >= 0
            || error.IndexOf("capacity policy",
                System.StringComparison.Ordinal) >= 0)
        {
            return SipStatusCodes.Forbidden;
        }
        if (error.IndexOf("location",
                System.StringComparison.OrdinalIgnoreCase) >= 0
            || error.IndexOf("not configured",
                System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return SipStatusCodes.ServerInternalError;
        }
        return SipStatusCodes.BadRequest;
    }

    static string GetReasonPhrase(int status)
    {
        switch (status)
        {
            case SipStatusCodes.IntervalTooBrief:
                return SipReasonPhrases.IntervalTooBrief;
            case SipStatusCodes.BadExtension:
                return SipReasonPhrases.BadExtension;
            case SipStatusCodes.MethodNotAllowed:
                return SipReasonPhrases.MethodNotAllowed;
            case SipStatusCodes.UnsupportedUriScheme:
                return SipReasonPhrases.UnsupportedUriScheme;
            case SipStatusCodes.Forbidden:
                return SipReasonPhrases.Forbidden;
            case SipStatusCodes.ServerInternalError:
                return SipReasonPhrases.ServerInternalError;
            default:
                return SipReasonPhrases.BadRequest;
        }
    }

    static bool CanCreateResponse(SipMessage? request)
    {
        return request != null && request.Kind == SipMessageKind.Request
            && request.ViaValues != null && request.ViaValues.Length > 0
            && request.CSeq != null && request.From != null
            && request.To != null && ReadCallId(request) != null;
    }

    static SipMessage? AddCommonResponseHeaders(SipMessage? response)
    {
        if (response == null)
        {
            return null;
        }
        AppendHeader(response, SipHeaderNames.Date,
            SipHeaderPrimitives.FormatDateValue(System.DateTime.UtcNow));
        AppendHeader(response, SipHeaderNames.Allow, SipMethods.Register);
        AppendHeader(response, SipHeaderNames.Supported,
            SipOptionTags.Path + ", " + SipOptionTags.Outbound + ", "
                + SipOptionTags.Gruu);
        return response;
    }

    SipContactValue[] CreateResponseContacts(
        SipRegistrationBinding[] bindings)
    {
        SipContactValue[] contacts = new SipContactValue[bindings.Length];
        int index = 0;
        while (index < bindings.Length)
        {
            SipContactValue? source = bindings[index].Contact;
            SipContactValue[] copied = SipSnapshotCopy.CopyContactArray(
                source == null ? new SipContactValue[0]
                    : new SipContactValue[] { source });
            SipContactValue contact = copied.Length == 0
                ? new SipContactValue() : copied[0];
            long remaining = bindings[index].ExpiresAtMilliseconds
                - nowMilliseconds;
            if (remaining <= 0)
            {
                contact.ExpiresSeconds = 0;
            }
            else
            {
                long seconds = remaining > long.MaxValue - 999
                    ? long.MaxValue / 1000 : (remaining + 999) / 1000;
                contact.ExpiresSeconds = seconds > int.MaxValue
                    ? int.MaxValue : (int)seconds;
            }
            contacts[index] = contact;
            index = index + 1;
        }
        return contacts;
    }

    static bool HasActiveOutboundBinding(
        SipRegistrationBinding[] bindings)
    {
        int index = 0;
        while (index < bindings.Length)
        {
            SipContactValue? contact = bindings[index].Contact;
            if (contact != null && contact.Outbound
                && contact.InstanceId != null
                && contact.RegistrationId > 0)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static void AppendContactHeader(SipMessage response,
        SipContactValue[] contacts)
    {
        if (contacts.Length == 0)
        {
            return;
        }
        SipStructuredHeaderResult model = new SipStructuredHeaderResult();
        model.Success = true;
        model.ContactValues = contacts;
        SipWriteResult formatted = SipStructuredHeaderCodec
            .FormatStructuredHeaderValue(SipHeaderNames.Contact, model,
                new SipWriteOptions());
        if (!formatted.Success)
        {
            return;
        }
        SipHeaderParseResult parsed = SipHeaderParser.ParseHeaderBytes(
            formatted.Bytes, new SipParseOptions());
        if (parsed.Status == SipParseStatus.Complete && parsed.Header != null)
        {
            AppendHeader(response, parsed.Header.Name == null
                ? SipHeaderNames.Contact : parsed.Header.Name,
                parsed.Header.Value == null ? "" : parsed.Header.Value);
        }
    }

    static void CopyNamedHeaders(SipMessage source, SipMessage destination,
        string name)
    {
        int index = 0;
        while (index < source.Headers.Length)
        {
            SipHeader? header = source.Headers[index];
            if (header != null && SipAscii.EqualsIgnoreCase(header.Name, name))
            {
                AppendHeader(destination, name,
                    header.Value == null ? "" : header.Value);
            }
            index = index + 1;
        }
    }

    static void AppendHeader(SipMessage message, string name, string value)
    {
        SipHeader[] headers = new SipHeader[message.Headers.Length + 1];
        System.Array.Copy(message.Headers, headers, message.Headers.Length);
        SipHeader added = new SipHeader();
        added.Name = name;
        added.Value = value;
        headers[message.Headers.Length] = added;
        message.Headers = headers;
        message.OriginalBytes = new byte[0];
    }

    SipRegistrationSnapshot Success(SipMessage message,
        SipUri addressOfRecord, string callId, int cseq,
        SipRegistrationBinding[] bindings,
        SipDigestAuthorization? authenticationInformation)
    {
        SipRegistrationSnapshot snapshot = CreateSnapshot(
            addressOfRecord, callId, cseq, bindings);
        snapshot.State = bindings.Length == 0
            ? SipRegistrationState.Terminated : SipRegistrationState.Registered;
        snapshot.Target = message.RequestUri == null ? null
            : SipUriModelCopy.CreateNormalizedCopy(message.RequestUri);
        snapshot.Response = CreateSuccessfulResponse(
            message, bindings, authenticationInformation);
        return snapshot;
    }

    SipRegistrationSnapshot Failure(SipMessage? request,
        SipUri? addressOfRecord,
        string? callId, int cseq, SipRegistrationBinding[] bindings,
        string message)
    {
        SipRegistrationSnapshot snapshot = CreateSnapshot(
            addressOfRecord, callId, cseq, bindings);
        snapshot.State = SipRegistrationState.Failed;
        snapshot.ErrorMessage = message;
        snapshot.Response = CreateFailureResponse(request, message);
        return snapshot;
    }

    static SipRegistrationSnapshot CreateSnapshot(SipUri? addressOfRecord,
        string? callId, int cseq, SipRegistrationBinding[] bindings)
    {
        SipRegistrationSnapshot snapshot = new SipRegistrationSnapshot();
        snapshot.AddressOfRecord = addressOfRecord == null ? null
            : SipUriModelCopy.CreateNormalizedCopy(addressOfRecord);
        snapshot.Bindings =
            SipSnapshotCopy.CopyRegistrationBindingArray(bindings);
        snapshot.CallId = callId;
        snapshot.CSeq = cseq;
        long earliest = -1;
        int index = 0;
        while (index < snapshot.Bindings.Length)
        {
            long expiry = snapshot.Bindings[index].ExpiresAtMilliseconds;
            if (earliest < 0 || expiry < earliest)
            {
                earliest = expiry;
            }
            index = index + 1;
        }
        snapshot.ExpiresAtMilliseconds = earliest;
        return snapshot;
    }
}
