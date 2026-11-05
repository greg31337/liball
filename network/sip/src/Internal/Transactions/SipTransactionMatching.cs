namespace sip;

internal static class SipTransactionMatching
{
    internal const string MagicCookie = "z9hG4bK";

    internal static SipTransactionKey CreateKey(SipMessage? request, SipTransactionRole role)
    {
        if (role != SipTransactionRole.Client && role != SipTransactionRole.Server)
        {
            throw new System.ArgumentException("A client or server transaction role is required.", "role");
        }
        if (!IsBoundedRequest(request) || request == null || request.CSeq == null
            || request.RequestUri == null || request.From == null || request.To == null
            || request.ViaValues.Length == 0)
        {
            throw new System.ArgumentException("A complete bounded SIP request is required.", "request");
        }

        string? callId = ReadCallId(request.Headers);
        if (callId == null)
        {
            throw new System.ArgumentException("The request requires one valid Call-ID header field.",
                "request");
        }

        SipViaValue topVia = request.ViaValues[0];
        SipTransactionKey key = new SipTransactionKey();
        key.Branch = topVia.Branch;
        key.SentBy = CreateSentBy(topVia);
        key.Method = request.Method;
        key.MethodToken = request.MethodToken;
        key.CallId = callId;
        key.CSeq = request.CSeq.SequenceNumber;
        key.Role = role;
        key.LegacyRequestUri = SipUriModelCopy.CreateNormalizedCopy(request.RequestUri);
        key.LegacyFrom = CopyAddress(request.From);
        key.LegacyTo = CopyAddress(request.To);
        key.LegacyTopVia = CopyVia(topVia);
        return key;
    }

    internal static bool HasMagicCookie(string? branch)
    {
        return branch != null && branch.StartsWith(MagicCookie,
            System.StringComparison.Ordinal);
    }

    internal static bool MatchResponse(SipTransactionKey? key, SipMessage? response,
        bool allowLegacy)
    {
        if (key == null || key.Role != SipTransactionRole.Client
            || !IsBoundedResponse(response) || response == null || response.CSeq == null
            || response.ViaValues.Length == 0 || !KeyMethodIsValid(key))
        {
            return false;
        }

        SipViaValue topVia = response.ViaValues[0];
        if (HasMagicCookie(topVia.Branch))
        {
            return HasMagicCookie(key.Branch) && topVia.Branch == key.Branch
                && MethodMatches(key.Method, key.MethodToken, response.CSeq.Method,
                    response.CSeq.MethodToken);
        }
        if (!allowLegacy || key.LegacyTopVia == null || key.LegacyFrom == null
            || key.LegacyTo == null)
        {
            return false;
        }

        string? callId = ReadCallId(response.Headers);
        return callId != null && callId == key.CallId && response.CSeq.SequenceNumber == key.CSeq
            && MethodMatches(key.Method, key.MethodToken, response.CSeq.Method,
                response.CSeq.MethodToken)
            && LegacyResponseViaMatches(key.LegacyTopVia, topVia)
            && ResponseAddressMatches(key.LegacyFrom, response.From, false)
            && ResponseAddressMatches(key.LegacyTo, response.To, true);
    }

    internal static bool MatchRequest(SipTransactionKey? key, SipMessage? request,
        string? responseToTag, bool allowLegacy)
    {
        if (key == null || key.Role != SipTransactionRole.Server
            || !IsBoundedRequest(request) || request == null || request.CSeq == null
            || request.ViaValues.Length == 0 || !KeyMethodIsValid(key))
        {
            return false;
        }

        SipViaValue topVia = request.ViaValues[0];
        if (HasMagicCookie(topVia.Branch))
        {
            return topVia.Branch == key.Branch && CreateSentBy(topVia) == key.SentBy
                && RequestMethodMatches(key, request);
        }
        if (!allowLegacy)
        {
            return false;
        }
        return LegacyRequestMatches(key, request, responseToTag, false);
    }

    internal static bool MatchCancel(SipTransactionKey? key, SipMessage? cancel,
        bool allowLegacy)
    {
        if (key == null || key.Role != SipTransactionRole.Server
            || key.Method == SipMethod.Ack || key.Method == SipMethod.Cancel
            || !IsBoundedRequest(cancel) || cancel == null || cancel.Method != SipMethod.Cancel
            || cancel.CSeq == null || cancel.CSeq.Method != SipMethod.Cancel
            || cancel.ViaValues.Length == 0 || !KeyMethodIsValid(key))
        {
            return false;
        }

        SipViaValue topVia = cancel.ViaValues[0];
        if (HasMagicCookie(topVia.Branch))
        {
            return topVia.Branch == key.Branch && CreateSentBy(topVia) == key.SentBy;
        }
        if (!allowLegacy)
        {
            return false;
        }
        return LegacyRequestMatches(key, cancel, null, true);
    }

    internal static bool MatchAcceptedAck(SipTransactionKey? key, SipMessage? ack,
        string[]? acceptedToTags)
    {
        if (key == null || key.Role != SipTransactionRole.Server
            || key.Method != SipMethod.Invite || key.LegacyFrom == null
            || !IsBoundedRequest(ack) || ack == null || ack.Method != SipMethod.Ack
            || ack.CSeq == null || ack.CSeq.Method != SipMethod.Ack
            || ack.CSeq.SequenceNumber != key.CSeq || ack.From == null || ack.To == null
            || acceptedToTags == null || acceptedToTags.Length == 0)
        {
            return false;
        }

        string? callId = ReadCallId(ack.Headers);
        string? fromTag = ReadTag(ack.From);
        string? toTag = ReadTag(ack.To);
        if (callId == null || callId != key.CallId || fromTag != ReadTag(key.LegacyFrom)
            || toTag == null)
        {
            return false;
        }

        int index = 0;
        while (index < acceptedToTags.Length)
        {
            if (acceptedToTags[index] == toTag)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static bool LegacyRequestMatches(SipTransactionKey key, SipMessage request,
        string? responseToTag, bool cancel)
    {
        if (key.LegacyRequestUri == null || key.LegacyFrom == null || key.LegacyTo == null
            || key.LegacyTopVia == null || request.RequestUri == null || request.CSeq == null
            || request.From == null || request.To == null || request.ViaValues.Length == 0)
        {
            return false;
        }

        string? callId = ReadCallId(request.Headers);
        if (callId == null || callId != key.CallId || request.CSeq.SequenceNumber != key.CSeq
            || !SipUriComparison.CompareUriModels(key.LegacyRequestUri, request.RequestUri)
            || ReadTag(key.LegacyFrom) != ReadTag(request.From)
            || !ViaMatches(key.LegacyTopVia, request.ViaValues[0]))
        {
            return false;
        }

        if (cancel)
        {
            return ReadTag(key.LegacyTo) == ReadTag(request.To);
        }
        if (request.Method == SipMethod.Ack)
        {
            return key.Method == SipMethod.Invite && ReadTag(request.To) == responseToTag;
        }
        return ReadTag(key.LegacyTo) == ReadTag(request.To) && RequestMethodMatches(key, request);
    }

    static bool RequestMethodMatches(SipTransactionKey key, SipMessage request)
    {
        if (request.Method == SipMethod.Ack)
        {
            return key.Method == SipMethod.Invite;
        }
        return MethodMatches(key.Method, key.MethodToken, request.Method, request.MethodToken);
    }

    static bool MethodMatches(SipMethod leftMethod, string? leftToken, SipMethod rightMethod,
        string? rightToken)
    {
        return leftMethod == rightMethod && leftToken != null && leftToken == rightToken;
    }

    static bool KeyMethodIsValid(SipTransactionKey key)
    {
        return SipAscii.IsToken(key.MethodToken) && key.CSeq >= 0;
    }

    static bool IsBoundedRequest(SipMessage? request)
    {
        return request != null && request.Kind == SipMessageKind.Request
            && request.RequestUri != null && UriIsBounded(request.RequestUri)
            && request.MethodToken != null && SipAscii.IsToken(request.MethodToken)
            && request.MethodToken.Length <= SipDefaults.MaxHeaderLineBytes
            && request.CSeq != null && request.CSeq.SequenceNumber >= 0
            && request.CSeq.Method == request.Method
            && request.CSeq.MethodToken == request.MethodToken
            && MessageFieldsAreBounded(request);
    }

    static bool IsBoundedResponse(SipMessage? response)
    {
        return response != null && response.Kind == SipMessageKind.Response
            && response.StatusCode >= 100 && response.StatusCode <= 699
            && response.CSeq != null && response.CSeq.SequenceNumber >= 0
            && response.CSeq.MethodToken != null && SipAscii.IsToken(response.CSeq.MethodToken)
            && response.CSeq.MethodToken.Length <= SipDefaults.MaxHeaderLineBytes
            && MessageFieldsAreBounded(response);
    }

    static bool MessageFieldsAreBounded(SipMessage message)
    {
        if (message.Headers == null || message.Headers.Length > SipDefaults.MaxHeaderCount
            || message.ViaValues == null || message.ViaValues.Length == 0
            || message.ViaValues.Length > SipDefaults.MaxListValueCount
            || message.From == null || message.To == null
            || !AddressIsBounded(message.From) || !AddressIsBounded(message.To)
            || !ViaIsBounded(message.ViaValues[0]))
        {
            return false;
        }

        int headerBytes = 0;
        int index = 0;
        while (index < message.Headers.Length)
        {
            SipHeader? header = message.Headers[index];
            if (header == null || header.Name == null || header.Value == null
                || !TextIsBounded(header.Name, SipDefaults.MaxHeaderLineBytes)
                || !TextIsBounded(header.Value, SipDefaults.MaxHeaderLineBytes))
            {
                return false;
            }
            int fieldBytes = System.Text.Encoding.UTF8.GetByteCount(header.Name)
                + System.Text.Encoding.UTF8.GetByteCount(header.Value) + 4;
            if (fieldBytes > SipDefaults.MaxHeaderBytes - headerBytes)
            {
                return false;
            }
            headerBytes = headerBytes + fieldBytes;
            index = index + 1;
        }
        return true;
    }

    static bool UriIsBounded(SipUri uri)
    {
        if (uri.Parameters == null || uri.Headers == null
            || uri.Parameters.Length > SipDefaults.MaxParameterCount
            || uri.Headers.Length > SipDefaults.MaxParameterCount
            || !OptionalTextIsBounded(uri.SchemeToken, SipDefaults.MaxUriBytes)
            || !OptionalTextIsBounded(uri.User, SipDefaults.MaxUriBytes)
            || !OptionalTextIsBounded(uri.Password, SipDefaults.MaxUriBytes)
            || !OptionalTextIsBounded(uri.Host, SipDefaults.MaxUriBytes)
            || !OptionalTextIsBounded(uri.OpaquePart, SipDefaults.MaxUriBytes)
            || SipUriValidator.ValidateUriModel(uri).Length != 0)
        {
            return false;
        }

        int index = 0;
        while (index < uri.Parameters.Length)
        {
            SipUriParameter? parameter = uri.Parameters[index];
            if (parameter == null || !TextIsBounded(parameter.Name, SipDefaults.MaxUriBytes)
                || !OptionalTextIsBounded(parameter.Value, SipDefaults.MaxUriBytes))
            {
                return false;
            }
            index = index + 1;
        }
        index = 0;
        while (index < uri.Headers.Length)
        {
            SipUriHeader? header = uri.Headers[index];
            if (header == null || !TextIsBounded(header.Name, SipDefaults.MaxUriBytes)
                || !TextIsBounded(header.Value, SipDefaults.MaxUriBytes))
            {
                return false;
            }
            index = index + 1;
        }
        SipWriteOptions options = new SipWriteOptions();
        options.Canonical = true;
        options.PreserveOriginal = false;
        options.MaxHeaderLineBytes = SipDefaults.MaxUriBytes;
        return SipUriFormatter.FormatUriModel(uri, options).Success;
    }

    static bool AddressIsBounded(SipNameAddress address)
    {
        if (address.Uri == null || !UriIsBounded(address.Uri) || address.Parameters == null
            || address.Parameters.Length > SipDefaults.MaxParameterCount
            || !OptionalTextIsBounded(address.DisplayName, SipDefaults.MaxHeaderLineBytes))
        {
            return false;
        }

        bool foundTag = false;
        int index = 0;
        while (index < address.Parameters.Length)
        {
            SipUriParameter? parameter = address.Parameters[index];
            if (parameter == null || !TextIsBounded(parameter.Name, SipDefaults.MaxHeaderLineBytes)
                || !OptionalTextIsBounded(parameter.Value, SipDefaults.MaxHeaderLineBytes))
            {
                return false;
            }
            if (SipAscii.EqualsIgnoreCase(parameter.Name, "tag"))
            {
                if (foundTag || !parameter.HasValue || !SipAscii.IsToken(parameter.Value))
                {
                    return false;
                }
                foundTag = true;
            }
            index = index + 1;
        }
        return true;
    }

    static bool ViaIsBounded(SipViaValue via)
    {
        if (!SipAscii.EqualsIgnoreCase(via.ProtocolName, "SIP")
            || !SipAscii.EqualsIgnoreCase(via.ProtocolVersion, "2.0")
            || !SipAscii.IsToken(via.Transport) || !TextIsBounded(via.Host,
                SipDefaults.MaxHeaderLineBytes)
            || via.Port < -1 || via.Port > 65535 || via.Parameters == null
            || via.Parameters.Length > SipDefaults.MaxParameterCount
            || via.Branch != null && !SipAscii.IsToken(via.Branch))
        {
            return false;
        }

        int index = 0;
        while (index < via.Parameters.Length)
        {
            SipUriParameter? parameter = via.Parameters[index];
            if (parameter == null || !TextIsBounded(parameter.Name, SipDefaults.MaxHeaderLineBytes)
                || !OptionalTextIsBounded(parameter.Value, SipDefaults.MaxHeaderLineBytes))
            {
                return false;
            }
            index = index + 1;
        }
        SipWriteOptions options = new SipWriteOptions();
        options.Canonical = true;
        options.PreserveOriginal = false;
        options.MaxHeaderLineBytes = SipDefaults.MaxHeaderLineBytes;
        return SipViaCodec.FormatViaHeaderValues(new SipViaValue[] { via }, options).Success;
    }

    static bool TextIsBounded(string? value, int maximum)
    {
        return value != null && value.Length > 0 && value.Length <= maximum
            && System.Text.Encoding.UTF8.GetByteCount(value) <= maximum;
    }

    static bool OptionalTextIsBounded(string? value, int maximum)
    {
        return value == null || value.Length <= maximum
            && System.Text.Encoding.UTF8.GetByteCount(value) <= maximum;
    }

    static string? ReadCallId(SipHeader[] headers)
    {
        string? callId = null;
        int index = 0;
        while (index < headers.Length)
        {
            SipHeader header = headers[index];
            if (SipAscii.EqualsIgnoreCase(header.Name, SipHeaderNames.CallId)
                || SipAscii.EqualsIgnoreCase(header.Name, SipHeaderNames.CompactCallId))
            {
                if (callId != null)
                {
                    return null;
                }
                SipStructuredHeaderResult parsed = SipStructuredHeaderCodec.ParseStructuredHeaderValue(
                    SipHeaderNames.CallId, header.Value, new SipParseOptions());
                if (!parsed.Success || parsed.CallId == null)
                {
                    return null;
                }
                callId = parsed.CallId;
            }
            index = index + 1;
        }
        return callId;
    }

    static string? ReadTag(SipNameAddress address)
    {
        int index = 0;
        while (index < address.Parameters.Length)
        {
            SipUriParameter parameter = address.Parameters[index];
            if (SipAscii.EqualsIgnoreCase(parameter.Name, "tag"))
            {
                return parameter.Value;
            }
            index = index + 1;
        }
        return null;
    }

    static string CreateSentBy(SipViaValue via)
    {
        string host = via.Host == null ? "" : via.Host.ToLowerInvariant();
        if (host.IndexOf(':') >= 0)
        {
            host = "[" + host + "]";
        }
        if (via.Port >= 0)
        {
            return host + ":" + via.Port.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
        return host;
    }

    static bool ResponseAddressMatches(SipNameAddress expected, SipNameAddress? actual,
        bool permitAddedTag)
    {
        if (actual == null || expected.Uri == null || actual.Uri == null
            || !SipUriComparison.CompareUriModels(expected.Uri, actual.Uri))
        {
            return false;
        }
        string? expectedTag = ReadTag(expected);
        string? actualTag = ReadTag(actual);
        return expectedTag == actualTag || permitAddedTag && expectedTag == null;
    }

    static bool LegacyResponseViaMatches(SipViaValue expected, SipViaValue actual)
    {
        return SipAscii.EqualsIgnoreCase(expected.ProtocolName, actual.ProtocolName)
            && SipAscii.EqualsIgnoreCase(expected.ProtocolVersion, actual.ProtocolVersion)
            && SipAscii.EqualsIgnoreCase(expected.Transport, actual.Transport)
            && SipAscii.EqualsIgnoreCase(expected.Host, actual.Host)
            && expected.Port == actual.Port && expected.Branch == actual.Branch;
    }

    static bool ViaMatches(SipViaValue expected, SipViaValue actual)
    {
        if (!LegacyResponseViaMatches(expected, actual)
            || expected.Parameters.Length != actual.Parameters.Length)
        {
            return false;
        }

        bool[] matched = new bool[actual.Parameters.Length];
        int expectedIndex = 0;
        while (expectedIndex < expected.Parameters.Length)
        {
            int actualIndex = 0;
            bool found = false;
            while (actualIndex < actual.Parameters.Length)
            {
                if (!matched[actualIndex]
                    && ViaParameterMatches(expected.Parameters[expectedIndex],
                        actual.Parameters[actualIndex]))
                {
                    matched[actualIndex] = true;
                    found = true;
                    break;
                }
                actualIndex = actualIndex + 1;
            }
            if (!found)
            {
                return false;
            }
            expectedIndex = expectedIndex + 1;
        }
        return true;
    }

    static bool ViaParameterMatches(SipUriParameter expected, SipUriParameter actual)
    {
        if (!SipAscii.EqualsIgnoreCase(expected.Name, actual.Name)
            || expected.HasValue != actual.HasValue)
        {
            return false;
        }
        if (SipAscii.EqualsIgnoreCase(expected.Name, "branch")
            || expected.Quoted || actual.Quoted)
        {
            return expected.Quoted == actual.Quoted && expected.Value == actual.Value;
        }
        return SipAscii.EqualsIgnoreCase(expected.Value, actual.Value);
    }

    static SipNameAddress CopyAddress(SipNameAddress source)
    {
        SipNameAddress copy = new SipNameAddress();
        copy.DisplayName = source.DisplayName;
        copy.Uri = SipUriModelCopy.CreateNormalizedCopy(source.Uri);
        copy.Parameters = CopyParameters(source.Parameters);
        copy.OriginalText = null;
        return copy;
    }

    static SipViaValue CopyVia(SipViaValue source)
    {
        SipViaValue copy = new SipViaValue();
        copy.ProtocolName = source.ProtocolName;
        copy.ProtocolVersion = source.ProtocolVersion;
        copy.Transport = source.Transport;
        copy.Host = source.Host;
        copy.Port = source.Port;
        copy.Parameters = CopyParameters(source.Parameters);
        copy.Branch = source.Branch;
        copy.Received = source.Received;
        copy.RPort = source.RPort;
        copy.HasRPort = source.HasRPort;
        return copy;
    }

    static SipUriParameter[] CopyParameters(SipUriParameter[] source)
    {
        SipUriParameter[] copy = new SipUriParameter[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            SipUriParameter item = new SipUriParameter();
            item.Name = source[index].Name;
            item.Value = source[index].Value;
            item.HasValue = source[index].HasValue;
            item.OriginalText = null;
            item.Quoted = source[index].Quoted;
            copy[index] = item;
            index = index + 1;
        }
        return copy;
    }
}
