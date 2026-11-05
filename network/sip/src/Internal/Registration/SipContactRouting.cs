namespace sip;

internal static class SipContactRouting
{
    internal static SipContactValue[] NormalizeRegisterContacts(SipMessage? request,
        int expiresHeader, int defaultExpires)
    {
        if (request == null || request.Kind != SipMessageKind.Request
            || !SipAscii.EqualsIgnoreCase(request.MethodToken, SipMethods.Register)
            || request.ContactValues == null)
        {
            throw new System.ArgumentException("A REGISTER request with Contact values is required.", "request");
        }
        ValidateExpirationInput(expiresHeader, "expiresHeader");
        ValidateExpirationInput(defaultExpires, "defaultExpires");

        if (request.ContactValues.Length == 1 && request.ContactValues[0] != null
            && request.ContactValues[0].Wildcard)
        {
            if (expiresHeader != 0)
            {
                throw new SipProtocolException(
                    "A wildcard Contact requires an Expires header value of zero.",
                    400, request.MethodToken);
            }
            return new SipContactValue[] { CopyContact(request.ContactValues[0]) };
        }

        SipContactValue[] normalized = new SipContactValue[request.ContactValues.Length];
        int activeOutboundCount = 0;
        int index = 0;
        while (index < request.ContactValues.Length)
        {
            SipContactValue? source = request.ContactValues[index];
            if (source == null || source.Wildcard)
            {
                throw new SipProtocolException(
                    "A wildcard Contact cannot be combined with other Contact values.",
                    400, request.MethodToken);
            }
            ValidateContact(source, "contact");
            SipContactValue contact = CopyContact(source);
            if (contact.RegistrationId > 0 && contact.InstanceId == null)
            {
                contact.RegistrationId = -1;
                if (contact.Address != null)
                {
                    contact.Address.Parameters = RemoveNamedParameter(
                        contact.Address.Parameters, "reg-id");
                }
            }
            if (contact.RegistrationId > 0
                && GetEffectiveExpires(contact, expiresHeader, defaultExpires) > 0)
            {
                activeOutboundCount = activeOutboundCount + 1;
            }
            normalized[index] = contact;
            index = index + 1;
        }

        if (activeOutboundCount > 1)
        {
            throw new SipProtocolException(
                "A REGISTER request cannot contain multiple active Contacts with reg-id values.",
                400, request.MethodToken);
        }
        return normalized;
    }

    internal static bool ContactsMatch(SipContactValue? left, SipContactValue? right)
    {
        if (left == null || right == null)
        {
            return false;
        }
        if (left.Wildcard || right.Wildcard)
        {
            return left.Wildcard && right.Wildcard;
        }
        if (left.InstanceId != null && right.InstanceId != null
            && left.RegistrationId > 0 && right.RegistrationId > 0)
        {
            return left.RegistrationId == right.RegistrationId
                && left.InstanceId.Equals(right.InstanceId,
                    System.StringComparison.OrdinalIgnoreCase);
        }

        SipUri? leftUri = left.Address == null ? null : left.Address.Uri;
        SipUri? rightUri = right.Address == null ? null : right.Address.Uri;
        return leftUri != null && rightUri != null
            && SipUriComparison.CompareUriModels(leftUri, rightUri);
    }

    internal static SipContactValue[] OrderContacts(SipContactValue[]? contacts,
        int expiresHeader, int defaultExpires)
    {
        if (contacts == null)
        {
            throw new System.ArgumentNullException("contacts");
        }
        ValidateExpirationInput(expiresHeader, "expiresHeader");
        ValidateExpirationInput(defaultExpires, "defaultExpires");
        SipContactValue[] result = new SipContactValue[contacts.Length];
        int index = 0;
        while (index < contacts.Length)
        {
            SipContactValue? source = contacts[index];
            if (source == null || source.Wildcard)
            {
                throw new System.ArgumentException(
                    "Only concrete Contact values can be ordered.", "contacts");
            }
            ValidateContact(source, "contacts");
            SipContactValue current = CopyContact(source);
            int insertion = index;
            while (insertion > 0 && ContactComesBefore(current, result[insertion - 1],
                expiresHeader, defaultExpires))
            {
                result[insertion] = result[insertion - 1];
                insertion = insertion - 1;
            }
            result[insertion] = current;
            index = index + 1;
        }
        return result;
    }

    internal static int GetEffectiveExpires(SipContactValue? contact,
        int expiresHeader, int defaultExpires)
    {
        if (contact == null)
        {
            throw new System.ArgumentNullException("contact");
        }
        ValidateExpirationInput(expiresHeader, "expiresHeader");
        ValidateExpirationInput(defaultExpires, "defaultExpires");
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

    internal static bool ApplyTargetRefresh(SipDialogSnapshot? dialog, SipMessage? message)
    {
        if (dialog == null || message == null || message.ContactValues == null)
        {
            throw new System.ArgumentException(
                "A dialog and target-refresh message are required.", "message");
        }
        if (!IsSuccessfulTargetRefresh(message) || message.ContactValues.Length == 0)
        {
            return false;
        }
        if (message.ContactValues.Length != 1 || message.ContactValues[0] == null
            || message.ContactValues[0].Wildcard)
        {
            throw new SipProtocolException(
                "A target refresh requires exactly one concrete Contact value.",
                400, message.MethodToken);
        }
        SipContactValue contact = message.ContactValues[0];
        ValidateContact(contact, "contact");
        SipUri? target = contact.Address == null ? null : contact.Address.Uri;
        if (target == null)
        {
            throw new SipProtocolException("A target refresh Contact URI is required.",
                400, message.MethodToken);
        }
        if (dialog.Secure && target.Scheme != SipUriScheme.Sips)
        {
            throw new SipProtocolException(
                "A secure dialog cannot be refreshed with a non-SIPS Contact.",
                416, message.MethodToken);
        }
        dialog.RemoteTarget = CopyUri(target);
        return true;
    }

    internal static SipUri SelectGruu(SipContactValue? contact, bool preferTemporary,
        bool requireSecure)
    {
        if (contact == null || contact.Wildcard)
        {
            throw new System.ArgumentException("A concrete Contact is required.", "contact");
        }
        ValidateContact(contact, "contact");
        string? first = preferTemporary ? contact.TemporaryGruu : contact.PublicGruu;
        string? second = preferTemporary ? contact.PublicGruu : contact.TemporaryGruu;
        SipUri? selected = ParseGruu(first);
        if (selected == null)
        {
            selected = ParseGruu(second);
        }
        if (selected == null && first == null && second == null
            && contact.Address != null && contact.Address.Uri != null)
        {
            selected = CopyUri(contact.Address.Uri);
        }
        if (selected == null)
        {
            throw new SipProtocolException("The Contact does not contain a valid routing URI.",
                400, null);
        }
        if (requireSecure && selected.Scheme != SipUriScheme.Sips)
        {
            throw new SipProtocolException("A secure operation cannot select a non-SIPS Contact URI.",
                416, null);
        }
        return selected;
    }

    internal static SipRegistrationBinding CreateRegistrationBinding(SipUri? addressOfRecord,
        SipContactValue? contact, string? callId, int cseq, int expiresHeader,
        int defaultExpires, long nowMilliseconds, SipEndpointAddress? observedAddress,
        SipFlow? flow)
    {
        ValidateSipUri(addressOfRecord, "addressOfRecord");
        ValidateContact(contact, "contact");
        if (contact == null || callId == null || callId.Length == 0 || cseq < 0
            || nowMilliseconds < 0 || observedAddress == null
            || observedAddress.Host == null || observedAddress.Host.Length == 0
            || observedAddress.Port <= 0 || observedAddress.Port > 65535)
        {
            throw new System.ArgumentException(
                "Complete registration and observed-address metadata are required.", "contact");
        }

        int expires = GetEffectiveExpires(contact, expiresHeader, defaultExpires);
        SipContactValue normalized = CopyContact(contact);
        if (normalized.RegistrationId > 0 && normalized.InstanceId == null)
        {
            normalized.RegistrationId = -1;
            if (normalized.Address != null)
            {
                normalized.Address.Parameters = RemoveNamedParameter(
                    normalized.Address.Parameters, "reg-id");
            }
        }
        if (flow != null && normalized.RegistrationId > 0
            && (flow.RegistrationId != normalized.RegistrationId
                || flow.InstanceId == null || normalized.InstanceId == null
                || !flow.InstanceId.Equals(normalized.InstanceId,
                    System.StringComparison.OrdinalIgnoreCase)))
        {
            throw new System.ArgumentException(
                "The outbound flow identity must match the Contact instance and reg-id.", "flow");
        }

        long duration = (long)expires * 1000;
        long expiry = nowMilliseconds > long.MaxValue - duration
            ? long.MaxValue : nowMilliseconds + duration;
        SipRegistrationBinding binding = new SipRegistrationBinding();
        binding.AddressOfRecord = addressOfRecord == null ? null : CopyUri(addressOfRecord);
        binding.Contact = normalized;
        binding.CallId = callId;
        binding.CSeq = cseq;
        binding.ExpiresAtMilliseconds = expiry;
        binding.QualityThousandths = normalized.QualityThousandths < 0
            ? 1000 : normalized.QualityThousandths;
        binding.ReceivedAddress = CopyEndpointAddress(observedAddress);
        binding.Flow = CopyFlow(flow);
        binding.InstanceId = normalized.InstanceId;
        binding.RegistrationId = normalized.RegistrationId;
        return binding;
    }

    static bool IsSuccessfulTargetRefresh(SipMessage message)
    {
        string? method = null;
        if (message.Kind == SipMessageKind.Request)
        {
            method = message.MethodToken;
        }
        else if (message.Kind == SipMessageKind.Response
            && message.StatusCode >= 200 && message.StatusCode < 300
            && message.CSeq != null)
        {
            method = message.CSeq.MethodToken;
        }
        return IsTargetRefreshMethod(method);
    }

    static bool IsTargetRefreshMethod(string? method)
    {
        return SipAscii.EqualsIgnoreCase(method, SipMethods.Invite)
            || SipAscii.EqualsIgnoreCase(method, SipMethods.Update)
            || SipAscii.EqualsIgnoreCase(method, SipMethods.Subscribe)
            || SipAscii.EqualsIgnoreCase(method, SipMethods.Notify);
    }

    static bool ContactComesBefore(SipContactValue left, SipContactValue right,
        int expiresHeader, int defaultExpires)
    {
        int leftQuality = left.QualityThousandths < 0 ? 1000 : left.QualityThousandths;
        int rightQuality = right.QualityThousandths < 0 ? 1000 : right.QualityThousandths;
        if (leftQuality != rightQuality)
        {
            return leftQuality > rightQuality;
        }
        return GetEffectiveExpires(left, expiresHeader, defaultExpires)
            > GetEffectiveExpires(right, expiresHeader, defaultExpires);
    }

    static void ValidateExpirationInput(int value, string parameterName)
    {
        if (value < -1)
        {
            throw new System.ArgumentOutOfRangeException(parameterName);
        }
    }

    static void ValidateContact(SipContactValue? contact, string parameterName)
    {
        SipUri? uri = contact == null || contact.Address == null ? null : contact.Address.Uri;
        if (contact == null || contact.Wildcard || uri == null
            || uri.Scheme != SipUriScheme.Sip && uri.Scheme != SipUriScheme.Sips
            || SipUriValidator.ValidateUriModel(uri).Length > 0
            || contact.QualityThousandths < -1 || contact.QualityThousandths > 1000
            || contact.ExpiresSeconds < -1 || contact.RegistrationId == 0)
        {
            throw new System.ArgumentException("A valid concrete Contact is required.", parameterName);
        }
        if (contact.PublicGruu != null && ParseGruu(contact.PublicGruu) == null
            || contact.TemporaryGruu != null && ParseGruu(contact.TemporaryGruu) == null)
        {
            throw new System.ArgumentException("Contact GRUU values must be SIP URIs with a gr parameter.",
                parameterName);
        }
    }

    static void ValidateSipUri(SipUri? uri, string parameterName)
    {
        if (uri == null || uri.Scheme != SipUriScheme.Sip && uri.Scheme != SipUriScheme.Sips
            || SipUriValidator.ValidateUriModel(uri).Length > 0)
        {
            throw new System.ArgumentException("A valid SIP or SIPS URI is required.", parameterName);
        }
    }

    static SipUri? ParseGruu(string? text)
    {
        if (text == null)
        {
            return null;
        }
        SipParseOptions options = new SipParseOptions();
        options.PreserveOriginal = false;
        SipParseResult parsed = SipUriParser.ParseUriBytes(
            System.Text.Encoding.ASCII.GetBytes(text), options);
        if (parsed.Status != SipParseStatus.Complete || parsed.Uri == null
            || parsed.Uri.Scheme != SipUriScheme.Sip && parsed.Uri.Scheme != SipUriScheme.Sips
            || !ContainsParameter(parsed.Uri.Parameters, "gr"))
        {
            return null;
        }
        return CopyUri(parsed.Uri);
    }

    static bool ContainsParameter(SipUriParameter[] parameters, string name)
    {
        int index = 0;
        while (index < parameters.Length)
        {
            SipUriParameter? parameter = parameters[index];
            if (parameter != null && SipAscii.EqualsIgnoreCase(parameter.Name, name))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static SipUriParameter[] RemoveNamedParameter(SipUriParameter[] parameters, string name)
    {
        int count = 0;
        int index = 0;
        while (index < parameters.Length)
        {
            SipUriParameter? parameter = parameters[index];
            if (parameter != null && SipAscii.EqualsIgnoreCase(parameter.Name, name))
            {
                count = count + 1;
            }
            index = index + 1;
        }
        SipUriParameter[] result = new SipUriParameter[parameters.Length - count];
        int destination = 0;
        index = 0;
        while (index < parameters.Length)
        {
            SipUriParameter? parameter = parameters[index];
            if (parameter == null || !SipAscii.EqualsIgnoreCase(parameter.Name, name))
            {
                result[destination] = parameters[index];
                destination = destination + 1;
            }
            index = index + 1;
        }
        return result;
    }

    static SipContactValue CopyContact(SipContactValue source)
    {
        SipContactValue copy = new SipContactValue();
        copy.Wildcard = source.Wildcard;
        copy.Address = CopyNameAddress(source.Address);
        copy.ExpiresSeconds = source.ExpiresSeconds;
        copy.QualityThousandths = source.QualityThousandths;
        copy.InstanceId = source.InstanceId;
        copy.RegistrationId = source.RegistrationId;
        copy.Outbound = source.Outbound;
        copy.PublicGruu = source.PublicGruu;
        copy.TemporaryGruu = source.TemporaryGruu;
        copy.PushProvider = source.PushProvider;
        copy.PushParameter = source.PushParameter;
        copy.PushResourceId = source.PushResourceId;
        copy.PushRegistrationReference = source.PushRegistrationReference;
        return copy;
    }

    static SipNameAddress? CopyNameAddress(SipNameAddress? source)
    {
        if (source == null)
        {
            return null;
        }
        SipNameAddress copy = new SipNameAddress();
        copy.DisplayName = source.DisplayName;
        copy.Uri = source.Uri == null ? null : CopyUri(source.Uri);
        copy.Parameters = CopyParameterArray(source.Parameters);
        copy.OriginalText = source.OriginalText;
        return copy;
    }

    static SipUri CopyUri(SipUri source)
    {
        SipUri copy = new SipUri();
        copy.Scheme = source.Scheme;
        copy.SchemeToken = source.SchemeToken;
        copy.User = source.User;
        copy.Password = source.Password;
        copy.Host = source.Host;
        copy.Port = source.Port;
        copy.Parameters = CopyParameterArray(source.Parameters);
        copy.Headers = CopyUriHeaderArray(source.Headers);
        copy.OpaquePart = source.OpaquePart;
        copy.OriginalText = source.OriginalText;
        return copy;
    }

    static SipUriParameter[] CopyParameterArray(SipUriParameter[]? source)
    {
        if (source == null)
        {
            return new SipUriParameter[0];
        }
        SipUriParameter[] result = new SipUriParameter[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            SipUriParameter item = new SipUriParameter();
            SipUriParameter? original = source[index];
            if (original != null)
            {
                item.Name = original.Name;
                item.Value = original.Value;
                item.HasValue = original.HasValue;
                item.OriginalText = original.OriginalText;
                item.Quoted = original.Quoted;
            }
            result[index] = item;
            index = index + 1;
        }
        return result;
    }

    static SipUriHeader[] CopyUriHeaderArray(SipUriHeader[]? source)
    {
        if (source == null)
        {
            return new SipUriHeader[0];
        }
        SipUriHeader[] result = new SipUriHeader[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            SipUriHeader item = new SipUriHeader();
            SipUriHeader? original = source[index];
            if (original != null)
            {
                item.Name = original.Name;
                item.Value = original.Value;
            }
            result[index] = item;
            index = index + 1;
        }
        return result;
    }

    static SipEndpointAddress CopyEndpointAddress(SipEndpointAddress source)
    {
        SipEndpointAddress copy = new SipEndpointAddress();
        copy.Host = source.Host;
        copy.Port = source.Port;
        copy.Transport = source.Transport;
        copy.Secure = source.Secure;
        return copy;
    }

    static SipFlow? CopyFlow(SipFlow? source)
    {
        return SipSnapshotCopy.CopyFlowModel(source);
    }

    static SipConnectionInfo? CopyConnection(SipConnectionInfo? source)
    {
        if (source == null)
        {
            return null;
        }
        SipConnectionInfo copy = new SipConnectionInfo();
        copy.Id = source.Id;
        copy.State = source.State;
        copy.Transport = source.Transport;
        copy.LocalAddress = source.LocalAddress == null ? null : CopyEndpointAddress(source.LocalAddress);
        copy.RemoteAddress = source.RemoteAddress == null ? null : CopyEndpointAddress(source.RemoteAddress);
        copy.Secure = source.Secure;
        copy.PeerIdentity = source.PeerIdentity;
        copy.CertificateIdentity =
            SipCertificateIdentityCopy.Copy(source.CertificateIdentity);
        return copy;
    }
}
