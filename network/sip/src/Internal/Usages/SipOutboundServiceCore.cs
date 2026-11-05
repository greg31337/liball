namespace sip;

internal static class SipOutboundServiceCore
{
    internal static SipOutboundResult CreateRegistrationContact(
        SipOutboundContactRequest? request)
    {
        if (request == null || !ValidSipUri(request.ContactUri)
            || request.ExpiresSeconds < -1
            || request.QualityThousandths < -1
            || request.QualityThousandths > 1000)
        {
            return Failure(SipStatusCodes.BadRequest,
                "A valid registration Contact request is required.");
        }
        if (request.UseOutbound
            && (request.InstanceId == null
                || request.RegistrationId <= 0)
            || !request.UseOutbound && request.RegistrationId > 0
            || request.RequestGruu && request.InstanceId == null)
        {
            return Failure(SipStatusCodes.BadRequest,
                "Outbound and GRUU Contacts require consistent instance and reg-id values.");
        }
        if (!ValidPushContactRequest(request))
        {
            return Failure(SipStatusCodes.BadRequest,
                "The push Contact parameters are incomplete or inconsistent.");
        }

        SipContactValue contact = new SipContactValue();
        contact.Address = new SipNameAddress();
        contact.Address.Uri = SipUriModelCopy.CreateNormalizedCopy(
            request.ContactUri);
        contact.Address.Uri.Parameters = RemoveManagedUriParameters(
            contact.Address.Uri.Parameters, false);
        contact.ExpiresSeconds = request.ExpiresSeconds;
        contact.QualityThousandths = request.QualityThousandths;
        contact.InstanceId = request.InstanceId;
        contact.RegistrationId = request.RegistrationId;
        contact.Outbound = request.UseOutbound;
        contact.PushProvider = request.PushProvider;
        contact.PushParameter = request.PushParameter;
        contact.PushResourceId = request.PushResourceId;
        if (request.PushRefreshWithoutNotification)
        {
            contact.Address.Parameters = new SipUriParameter[] {
                CreateValuelessParameter("+sip.pnsreg") };
        }
        SipContactValue? canonical = CanonicalContact(contact);
        if (canonical == null)
        {
            return Failure(SipStatusCodes.BadRequest,
                "The registration Contact cannot be formatted safely.");
        }

        SipOutboundResult result = Success();
        result.Contact = canonical;
        result.SupportedOptionTags = RegistrationOptionTags(
            request.UseOutbound, request.RequestGruu);
        return result;
    }

    internal static SipOutboundResult CreateDialogContact(
        SipContactValue? registrationContact)
    {
        SipContactValue? copied = CanonicalContact(registrationContact);
        if (copied == null || copied.Address == null
            || copied.Address.Uri == null)
        {
            return Failure(SipStatusCodes.BadRequest,
                "A valid concrete registration Contact is required.");
        }
        copied.Address.Uri.Parameters = RemoveManagedUriParameters(
            copied.Address.Uri.Parameters, true);
        copied.Address.Parameters = RemoveManagedContactParameters(
            copied.Address.Parameters);
        copied.ExpiresSeconds = -1;
        copied.QualityThousandths = -1;
        copied.InstanceId = null;
        copied.RegistrationId = -1;
        copied.Outbound = false;
        copied.PublicGruu = null;
        copied.TemporaryGruu = null;
        copied.PushProvider = null;
        copied.PushParameter = null;
        copied.PushResourceId = null;
        SipContactValue? canonical = CanonicalContact(copied);
        if (canonical == null)
        {
            return Failure(SipStatusCodes.BadRequest,
                "The dialog Contact cannot be formatted safely.");
        }
        SipOutboundResult result = Success();
        result.Contact = canonical;
        return result;
    }

    internal static SipOutboundResult SelectGruu(SipContactValue? contact,
        bool preferTemporary, bool requireSecure)
    {
        SipContactValue? canonical = CanonicalContact(contact);
        if (canonical == null || canonical.Address == null
            || canonical.Address.Uri == null)
        {
            return Failure(SipStatusCodes.BadRequest,
                "A valid concrete Contact is required.");
        }
        string? first = preferTemporary
            ? canonical.TemporaryGruu : canonical.PublicGruu;
        string? second = preferTemporary
            ? canonical.PublicGruu : canonical.TemporaryGruu;
        SipUri? selected = ParseGruu(first);
        bool usedTemporary = selected != null && preferTemporary;
        bool usedGruu = selected != null;
        if (selected == null)
        {
            selected = ParseGruu(second);
            usedTemporary = selected != null && !preferTemporary;
            usedGruu = selected != null;
        }
        if (selected == null)
        {
            selected = SipUriModelCopy.CreateNormalizedCopy(
                canonical.Address.Uri);
        }
        if (requireSecure && selected.Scheme != SipUriScheme.Sips)
        {
            return Failure(SipStatusCodes.UnsupportedUriScheme,
                "A secure operation cannot select a non-SIPS target.");
        }
        SipOutboundResult result = Success();
        result.Uri = selected;
        result.GruuSelected = usedGruu;
        result.TemporaryGruuSelected = usedTemporary;
        return result;
    }

    internal static SipOutboundResult SelectBindingRoute(
        SipOutboundRouteRequest? request,
        ISipPushNotificationPolicy? pushPolicy)
    {
        if (request == null || request.Bindings == null
            || request.NowMilliseconds < 0
            || request.Destination != null
                && !ValidSipUri(request.Destination))
        {
            return Failure(SipStatusCodes.BadRequest,
                "A valid Outbound routing request is required.");
        }
        SipRegistrationBinding? selected = null;
        int index = 0;
        while (index < request.Bindings.Length)
        {
            SipRegistrationBinding? candidate = request.Bindings[index];
            if (BindingMatches(candidate, request)
                && (selected == null
                    || BindingComesBefore(candidate, selected,
                        request, pushPolicy != null)))
            {
                selected = candidate;
            }
            index = index + 1;
        }
        if (selected == null || selected.Contact == null
            || selected.Contact.Address == null
            || selected.Contact.Address.Uri == null)
        {
            return Failure(SipStatusCodes.NotFound,
                "No active registration binding matches the destination.");
        }
        SipRegistrationBinding copied = CopyBinding(selected);
        SipContactValue contact = copied.Contact == null
            ? new SipContactValue() : copied.Contact;
        SipUri? contactUri = contact.Address == null
            ? null : contact.Address.Uri;
        if (contactUri == null)
        {
            return Failure(SipStatusCodes.BadRequest,
                "The selected registration Contact has no routing URI.");
        }
        bool outbound = contact.Outbound
            || copied.RegistrationId > 0
            || contact.RegistrationId > 0;
        bool activeFlow = FlowIsActive(copied.Flow,
            request.NowMilliseconds);
        if (outbound && !activeFlow)
        {
            return RequestPushForBinding(
                request, copied, pushPolicy);
        }
        SipUri targetUri = SipUriModelCopy.CreateNormalizedCopy(
            contactUri);
        if (request.RequireSecure && targetUri.Scheme != SipUriScheme.Sips)
        {
            return Failure(SipStatusCodes.UnsupportedUriScheme,
                "A secure route cannot use a non-SIPS registration Contact.");
        }
        SipProxyTarget target = new SipProxyTarget();
        target.Uri = targetUri;
        target.QualityThousandths = copied.QualityThousandths;
        target.ExpiresSeconds = RemainingSeconds(
            copied.ExpiresAtMilliseconds, request.NowMilliseconds);
        target.Flow = SipSnapshotCopy.CopyFlowModel(copied.Flow);
        if (copied.Path.Length > 0)
            target.RouteSet = copied.Path;
        else if (copied.Flow != null && copied.Flow.Path.Length > 0)
            target.RouteSet = copied.Flow.Path;
        else
            target.RouteSet = copied.ServiceRoute;
        SipOutboundResult result = Success();
        result.Target = target;
        result.Uri = SipUriModelCopy.CreateNormalizedCopy(targetUri);
        result.Binding = copied;
        return result;
    }

    internal static SipOutboundResult UpdateDialogTarget(
        SipDialogSnapshot? dialog, SipMessage? message)
    {
        SipDialogSnapshot? copy = SipSnapshotCopy.CopyDialogSnapshot(dialog);
        if (copy == null || message == null)
        {
            return Failure(SipStatusCodes.BadRequest,
                "A dialog and target-refresh message are required.");
        }
        SipOutboundResult result = Success();
        result.Dialog = copy;
        if (!IsSuccessfulTargetRefresh(message)) return result;
        if (message.ContactValues == null
            || message.ContactValues.Length != 1
            || message.ContactValues[0] == null
            || message.ContactValues[0].Wildcard)
        {
            return Failure(SipStatusCodes.BadRequest,
                "A target refresh requires exactly one concrete Contact.");
        }
        SipContactValue? contact = CanonicalContact(message.ContactValues[0]);
        SipUri? uri = contact == null || contact.Address == null
            ? null : contact.Address.Uri;
        if (!ValidSipUri(uri))
        {
            return Failure(SipStatusCodes.BadRequest,
                "The target-refresh Contact URI is invalid.");
        }
        if (copy.Secure && uri != null
            && uri.Scheme != SipUriScheme.Sips)
        {
            return Failure(SipStatusCodes.UnsupportedUriScheme,
                "A secure dialog cannot use a non-SIPS remote target.");
        }
        copy.RemoteTarget = uri == null ? null
            : SipUriModelCopy.CreateNormalizedCopy(uri);
        result.Dialog = copy;
        result.Uri = uri == null ? null
            : SipUriModelCopy.CreateNormalizedCopy(uri);
        result.TargetUpdated = true;
        return result;
    }

    internal static SipOutboundResult InspectPushRegistration(
        SipMessage? response, SipContactValue? requestedContact)
    {
        SipContactValue? requested = CanonicalContact(requestedContact);
        if (response == null || response.Kind != SipMessageKind.Response
            || response.CSeq == null
            || !SipAscii.EqualsIgnoreCase(
                response.CSeq.MethodToken, SipMethods.Register)
            || requested == null || requested.PushProvider == null)
        {
            return Failure(SipStatusCodes.BadRequest,
                "A REGISTER response and push-enabled Contact are required.");
        }
        if (response.StatusCode < 200 || response.StatusCode >= 300)
        {
            return Failure(response.StatusCode,
                "The REGISTER operation did not succeed.");
        }
        bool supported = false;
        int headerIndex = 0;
        while (headerIndex < response.Headers.Length)
        {
            SipHeader header = response.Headers[headerIndex];
            if (SipAscii.EqualsIgnoreCase(
                header.Name, SipHeaderNames.FeatureCaps))
            {
                SipExtensionHeaderResult parsed =
                    SipExtensionHeaderCodec.ParseExtensionHeaderValue(
                        SipHeaderNames.FeatureCaps, header.Value,
                        new SipParseOptions());
                if (!parsed.Success)
                {
                    return Failure(SipStatusCodes.BadRequest,
                        "The REGISTER response contains malformed Feature-Caps.");
                }
                if (FeatureCapsSupportsProvider(
                    parsed.Values, requested.PushProvider)) supported = true;
            }
            headerIndex = headerIndex + 1;
        }
        if (!supported)
        {
            return Failure(
                SipStatusCodes.PushNotificationServiceNotSupported,
                "The network did not confirm the requested push provider.");
        }
        SipContactValue? matched = FindResponseContact(
            response.ContactValues, requested);
        if (matched == null)
        {
            return Failure(SipStatusCodes.BadRequest,
                "The REGISTER response has no matching Contact.");
        }
        SipOutboundResult result = Success();
        result.PushSupported = true;
        result.Contact = matched;
        return result;
    }

    static SipOutboundResult RequestPushForBinding(
        SipOutboundRouteRequest request, SipRegistrationBinding binding,
        ISipPushNotificationPolicy? policy)
    {
        SipContactValue? contact = binding.Contact;
        if (!request.AllowPush || contact == null
            || contact.PushProvider == null
            || contact.PushProvider.Length == 0
            || contact.PushResourceId == null
            || contact.PushResourceId.Length == 0)
        {
            return Failure(SipStatusCodes.FlowFailed,
                "The Outbound flow is unavailable and no push fallback is usable.");
        }
        if (policy == null)
        {
            return Failure(
                SipStatusCodes.PushNotificationServiceNotSupported,
                "A push notification policy is required.");
        }
        SipPushNotificationRequest push = new SipPushNotificationRequest();
        push.Provider = contact.PushProvider;
        push.Parameter = contact.PushParameter;
        push.ResourceId = contact.PushResourceId;
        push.RegistrationReference = contact.PushRegistrationReference;
        push.Binding = CopyBinding(binding);
        push.Destination = request.Destination == null ? null
            : SipUriModelCopy.CreateNormalizedCopy(request.Destination);
        push.CorrelationId = request.CorrelationId;
        SipPushNotificationResult? decision =
            policy.RequestPushNotification(push);
        if (decision == null)
        {
            return Failure(
                SipStatusCodes.PushNotificationServiceNotSupported,
                "The push notification policy returned no decision.");
        }
        SipOutboundResult result = new SipOutboundResult();
        result.Success = decision.Accepted;
        result.StatusCode = decision.Accepted
            ? SipStatusCodes.Ok
            : decision.StatusCode > 0 ? decision.StatusCode
                : SipStatusCodes.PushNotificationServiceNotSupported;
        result.Binding = CopyBinding(binding);
        result.PushRequested = decision.Accepted;
        result.PushResult = CopyPushResult(decision);
        result.ErrorMessage = decision.Accepted ? null
            : decision.Reason == null
                ? "The push notification request was rejected."
                : decision.Reason;
        return result;
    }

    static bool ValidPushContactRequest(SipOutboundContactRequest request)
    {
        if (request.PushProvider == null)
        {
            return request.PushParameter == null
                && request.PushResourceId == null
                && !request.PushRefreshWithoutNotification;
        }
        if (!SafeParameterText(request.PushProvider)
            || request.PushParameter != null
                && !SafeParameterText(request.PushParameter)
            || request.PushResourceId != null
                && !SafeParameterText(request.PushResourceId)) return false;
        if (request.PushProvider.Length == 0)
        {
            return request.PushParameter == null
                && request.PushResourceId == null
                && !request.PushRefreshWithoutNotification;
        }
        if (request.PushParameter != null
            && request.PushResourceId == null) return false;
        return !request.PushRefreshWithoutNotification
            || request.PushResourceId != null
                && request.PushResourceId.Length > 0;
    }

    static bool SafeParameterText(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] == '\r' || value[index] == '\n') return false;
            index = index + 1;
        }
        return true;
    }

    static SipContactValue? CanonicalContact(SipContactValue? contact)
    {
        if (contact == null || contact.Wildcard) return null;
        SipStructuredHeaderResult model = new SipStructuredHeaderResult();
        model.Success = true;
        model.ContactValues = SipSnapshotCopy.CopyContactArray(
            new SipContactValue[] { contact });
        SipWriteResult formatted =
            SipStructuredHeaderCodec.FormatStructuredHeaderValue(
                SipHeaderNames.Contact, model, new SipWriteOptions());
        if (!formatted.Success) return null;
        SipHeaderParseResult parsed = SipHeaderParser.ParseHeaderBytes(
            formatted.Bytes, new SipParseOptions());
        if (parsed.Status != SipParseStatus.Complete
            || parsed.ContactValues.Length != 1) return null;
        return SipSnapshotCopy.CopyContactArray(parsed.ContactValues)[0];
    }

    static string[] RegistrationOptionTags(bool outbound, bool gruu)
    {
        if (outbound && gruu)
            return new string[] {
                SipOptionTags.Path, SipOptionTags.Outbound,
                SipOptionTags.Gruu };
        if (outbound)
            return new string[] {
                SipOptionTags.Path, SipOptionTags.Outbound };
        if (gruu) return new string[] { SipOptionTags.Gruu };
        return new string[0];
    }

    static bool BindingMatches(SipRegistrationBinding? binding,
        SipOutboundRouteRequest request)
    {
        SipContactValue? contact = binding == null ? null : binding.Contact;
        SipUri? contactUri = contact == null || contact.Address == null
            ? null : contact.Address.Uri;
        if (binding == null || contact == null || !ValidSipUri(contactUri)
            || request.RequireSecure && contactUri != null
                && contactUri.Scheme != SipUriScheme.Sips
            || binding.ExpiresAtMilliseconds >= 0
                && binding.ExpiresAtMilliseconds <= request.NowMilliseconds)
            return false;
        string? instance = binding.InstanceId == null
            ? contact.InstanceId : binding.InstanceId;
        if (request.InstanceId != null
            && (instance == null || !request.InstanceId.Equals(instance,
                System.StringComparison.OrdinalIgnoreCase))) return false;
        if (request.Destination == null) return true;
        if (contactUri != null && SipUriComparison.CompareUriModels(
            contactUri, request.Destination)) return true;
        if (binding.AddressOfRecord != null
            && SipUriComparison.CompareUriModels(
                binding.AddressOfRecord, request.Destination)) return true;
        return GruuMatches(contact.PublicGruu, request.Destination)
            || GruuMatches(contact.TemporaryGruu, request.Destination);
    }

    static bool BindingComesBefore(SipRegistrationBinding candidate,
        SipRegistrationBinding selected, SipOutboundRouteRequest request,
        bool pushPolicyAvailable)
    {
        bool candidateUsable = BindingIsUsable(
            candidate, request, pushPolicyAvailable);
        bool selectedUsable = BindingIsUsable(
            selected, request, pushPolicyAvailable);
        if (candidateUsable != selectedUsable) return candidateUsable;
        int candidateQuality = candidate.QualityThousandths < 0
            ? 1000 : candidate.QualityThousandths;
        int selectedQuality = selected.QualityThousandths < 0
            ? 1000 : selected.QualityThousandths;
        if (candidateQuality != selectedQuality)
            return candidateQuality > selectedQuality;
        bool candidateFlow = FlowIsActive(
            candidate.Flow, request.NowMilliseconds);
        bool selectedFlow = FlowIsActive(
            selected.Flow, request.NowMilliseconds);
        if (candidateFlow != selectedFlow) return candidateFlow;
        return candidate.ExpiresAtMilliseconds
            > selected.ExpiresAtMilliseconds;
    }

    static bool BindingIsUsable(SipRegistrationBinding binding,
        SipOutboundRouteRequest request, bool pushPolicyAvailable)
    {
        SipContactValue? contact = binding.Contact;
        if (contact == null) return false;
        bool outbound = contact.Outbound
            || binding.RegistrationId > 0
            || contact.RegistrationId > 0;
        if (!outbound || FlowIsActive(
                binding.Flow, request.NowMilliseconds)) return true;
        return request.AllowPush && pushPolicyAvailable
            && contact.PushProvider != null
            && contact.PushProvider.Length > 0
            && contact.PushResourceId != null
            && contact.PushResourceId.Length > 0;
    }

    static bool FlowIsActive(SipFlow? flow, long nowMilliseconds)
    {
        return flow != null && !flow.Failed
            && flow.Connection != null
            && flow.Connection.State == SipConnectionState.Connected
            && (flow.ExpiresAtMilliseconds < 0
                || flow.ExpiresAtMilliseconds > nowMilliseconds);
    }

    static bool GruuMatches(string? text, SipUri destination)
    {
        SipUri? gruu = ParseGruu(text);
        return gruu != null
            && SipUriComparison.CompareUriModels(gruu, destination);
    }

    static SipUri? ParseGruu(string? text)
    {
        if (text == null) return null;
        SipParseResult parsed = SipUriParser.ParseUriBytes(
            System.Text.Encoding.UTF8.GetBytes(text),
            new SipParseOptions());
        if (parsed.Status != SipParseStatus.Complete
            || !ValidSipUri(parsed.Uri)
            || parsed.Uri == null
            || !HasParameter(parsed.Uri.Parameters, "gr")) return null;
        return SipUriModelCopy.CreateNormalizedCopy(parsed.Uri);
    }

    static bool FeatureCapsSupportsProvider(
        SipExtensionHeaderValue[] values, string requestedProvider)
    {
        int valueIndex = 0;
        while (valueIndex < values.Length)
        {
            SipExtensionHeaderValue value = values[valueIndex];
            int parameterIndex = 0;
            while (parameterIndex < value.Parameters.Length)
            {
                SipUriParameter parameter = value.Parameters[parameterIndex];
                if (SipAscii.EqualsIgnoreCase(
                        parameter.Name, "+sip.pns")
                    && parameter.HasValue && parameter.Value != null
                    && (requestedProvider.Length == 0
                        || parameter.Value.Equals(requestedProvider,
                            System.StringComparison.OrdinalIgnoreCase)))
                    return true;
                parameterIndex = parameterIndex + 1;
            }
            valueIndex = valueIndex + 1;
        }
        return false;
    }

    static SipContactValue? FindResponseContact(
        SipContactValue[] contacts, SipContactValue requested)
    {
        int index = 0;
        while (index < contacts.Length)
        {
            if (SipContactRouting.ContactsMatch(contacts[index], requested))
                return SipSnapshotCopy.CopyContactArray(
                    new SipContactValue[] { contacts[index] })[0];
            index = index + 1;
        }
        return null;
    }

    static bool IsSuccessfulTargetRefresh(SipMessage message)
    {
        string? method = null;
        if (message.Kind == SipMessageKind.Request)
            method = message.MethodToken;
        else if (message.Kind == SipMessageKind.Response
            && message.StatusCode >= 200 && message.StatusCode < 300
            && message.CSeq != null)
            method = message.CSeq.MethodToken;
        return SipAscii.EqualsIgnoreCase(method, SipMethods.Invite)
            || SipAscii.EqualsIgnoreCase(method, SipMethods.Update)
            || SipAscii.EqualsIgnoreCase(method, SipMethods.Subscribe)
            || SipAscii.EqualsIgnoreCase(method, SipMethods.Notify);
    }

    static bool ValidSipUri(SipUri? uri)
    {
        return uri != null
            && (uri.Scheme == SipUriScheme.Sip
                || uri.Scheme == SipUriScheme.Sips)
            && SipUriValidator.ValidateUriModel(uri).Length == 0;
    }

    static SipRegistrationBinding CopyBinding(
        SipRegistrationBinding source)
    {
        return SipSnapshotCopy.CopyRegistrationBindingArray(
            new SipRegistrationBinding[] { source })[0];
    }

    static SipPushNotificationResult CopyPushResult(
        SipPushNotificationResult source)
    {
        SipPushNotificationResult copy = new SipPushNotificationResult();
        copy.Accepted = source.Accepted;
        copy.StatusCode = source.StatusCode;
        copy.Reason = source.Reason;
        copy.CorrelationId = source.CorrelationId;
        return copy;
    }

    static int RemainingSeconds(long expiry, long nowMilliseconds)
    {
        if (expiry < 0) return -1;
        long remaining = expiry - nowMilliseconds;
        if (remaining <= 0) return 0;
        long seconds = (remaining + 999) / 1000;
        return seconds > int.MaxValue ? int.MaxValue : (int)seconds;
    }

    static SipUriParameter CreateValuelessParameter(string name)
    {
        SipUriParameter parameter = new SipUriParameter();
        parameter.Name = name;
        return parameter;
    }

    static SipUriParameter[] RemoveManagedUriParameters(
        SipUriParameter[]? parameters, bool retainRegistrationReference)
    {
        if (parameters == null) return new SipUriParameter[0];
        System.Collections.Generic.List<SipUriParameter> retained =
            new System.Collections.Generic.List<SipUriParameter>();
        int index = 0;
        while (index < parameters.Length)
        {
            SipUriParameter? parameter = parameters[index];
            string? name = parameter == null ? null : parameter.Name;
            bool managed = SipAscii.EqualsIgnoreCase(name, "ob")
                || SipAscii.EqualsIgnoreCase(name, "pn-provider")
                || SipAscii.EqualsIgnoreCase(name, "pn-param")
                || SipAscii.EqualsIgnoreCase(name, "pn-prid")
                || !retainRegistrationReference
                    && SipAscii.EqualsIgnoreCase(name, "pn-purr");
            if (!managed && parameter != null)
                retained.Add(CopyParameter(parameter));
            index = index + 1;
        }
        return retained.ToArray();
    }

    static SipUriParameter[] RemoveManagedContactParameters(
        SipUriParameter[]? parameters)
    {
        if (parameters == null) return new SipUriParameter[0];
        System.Collections.Generic.List<SipUriParameter> retained =
            new System.Collections.Generic.List<SipUriParameter>();
        int index = 0;
        while (index < parameters.Length)
        {
            SipUriParameter? parameter = parameters[index];
            string? name = parameter == null ? null : parameter.Name;
            bool managed = SipAscii.EqualsIgnoreCase(name, "reg-id")
                || SipAscii.EqualsIgnoreCase(name, "+sip.instance")
                || SipAscii.EqualsIgnoreCase(name, "pub-gruu")
                || SipAscii.EqualsIgnoreCase(name, "temp-gruu")
                || SipAscii.EqualsIgnoreCase(name, "+sip.pnsreg");
            if (!managed && parameter != null)
                retained.Add(CopyParameter(parameter));
            index = index + 1;
        }
        return retained.ToArray();
    }

    static SipUriParameter CopyParameter(SipUriParameter source)
    {
        SipUriParameter copy = new SipUriParameter();
        copy.Name = source.Name;
        copy.Value = source.Value;
        copy.HasValue = source.HasValue;
        copy.OriginalText = source.OriginalText;
        copy.Quoted = source.Quoted;
        return copy;
    }

    static bool HasParameter(
        SipUriParameter[] parameters, string name)
    {
        int index = 0;
        while (index < parameters.Length)
        {
            SipUriParameter parameter = parameters[index];
            if (SipAscii.EqualsIgnoreCase(parameter.Name, name)) return true;
            index = index + 1;
        }
        return false;
    }

    static SipOutboundResult Success()
    {
        SipOutboundResult result = new SipOutboundResult();
        result.Success = true;
        result.StatusCode = SipStatusCodes.Ok;
        return result;
    }

    static SipOutboundResult Failure(int statusCode, string message)
    {
        SipOutboundResult result = new SipOutboundResult();
        result.StatusCode = statusCode;
        result.ErrorMessage = message;
        return result;
    }
}
