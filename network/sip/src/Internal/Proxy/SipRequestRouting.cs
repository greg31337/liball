namespace sip;

internal static class SipRequestRouting
{
    internal static int PrepareMaxForwards(SipMessage? request)
    {
        ValidateBasicRequest(request);
        if (request == null)
        {
            throw new System.ArgumentNullException("request");
        }

        int headerIndex = FindSingleHeader(request.Headers, SipHeaderNames.MaxForwards, null);
        int value;
        if (headerIndex < 0)
        {
            value = SipDefaults.MaxForwards;
            SipHeader added = new SipHeader();
            added.Name = SipHeaderNames.MaxForwards;
            added.Value = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
            request.Headers = InsertAfterViaHeaders(request.Headers, added);
        }
        else
        {
            SipHeader? header = request.Headers[headerIndex];
            SipStructuredHeaderResult parsed = SipStructuredHeaderCodec.ParseStructuredHeaderValue(
                SipHeaderNames.MaxForwards, header == null ? null : header.Value,
                new SipParseOptions());
            if (!parsed.Success)
            {
                throw new SipProtocolException("The Max-Forwards header field is invalid.",
                    SipStatusCodes.BadRequest, request.MethodToken);
            }
            if (parsed.MaxForwards == 0)
            {
                throw new SipProtocolException("The request has no remaining forwarding hops.",
                    SipStatusCodes.TooManyHops, request.MethodToken);
            }
            value = parsed.MaxForwards - 1;
            if (header == null)
            {
                throw new SipProtocolException("The Max-Forwards header field is invalid.",
                    SipStatusCodes.BadRequest, request.MethodToken);
            }
            header.Name = SipHeaderNames.MaxForwards;
            header.Value = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
            header.OriginalBytes = new byte[0];
        }

        request.OriginalBytes = new byte[0];
        return value;
    }

    internal static string CreateLoopFingerprint(SipMessage? request)
    {
        ValidateRoutingRequest(request);
        if (request == null || request.RequestUri == null || request.CSeq == null
            || request.ViaValues.Length == 0)
        {
            throw new System.ArgumentException("A complete routing request is required.", "request");
        }

        System.Text.StringBuilder material = new System.Text.StringBuilder();
        AppendFingerprintValue(material, GetTag(request.To));
        AppendFingerprintValue(material, GetTag(request.From));
        AppendFingerprintValue(material, GetCallId(request));
        AppendFingerprintValue(material, FormatUri(request.RequestUri));
        AppendFingerprintValue(material, FormatTopVia(request.ViaValues[0]));
        AppendFingerprintValue(material, request.CSeq.SequenceNumber.ToString(
            System.Globalization.CultureInfo.InvariantCulture));
        AppendNamedHeaderValues(material, request.Headers, SipHeaderNames.Route);
        AppendNamedHeaderValues(material, request.Headers, SipHeaderNames.ProxyRequire);
        AppendNamedHeaderValues(material, request.Headers, SipHeaderNames.ProxyAuthorization);
        return HashFingerprint(material.ToString());
    }

    internal static string CreateSpiralFingerprint(SipMessage? request)
    {
        ValidateRoutingRequest(request);
        if (request == null || request.CSeq == null)
        {
            throw new System.ArgumentException("A complete routing request is required.", "request");
        }

        System.Text.StringBuilder material = new System.Text.StringBuilder();
        AppendFingerprintValue(material, GetTag(request.To));
        AppendFingerprintValue(material, GetTag(request.From));
        AppendFingerprintValue(material, GetCallId(request));
        AppendFingerprintValue(material, request.CSeq.SequenceNumber.ToString(
            System.Globalization.CultureInfo.InvariantCulture));
        AppendFingerprintValue(material, request.CSeq.MethodToken);
        return HashFingerprint(material.ToString());
    }

    internal static bool RequestsFormLoop(SipMessage? first, SipMessage? second)
    {
        return CreateLoopFingerprint(first) == CreateLoopFingerprint(second);
    }

    internal static bool RequestsFormSpiral(SipMessage? first, SipMessage? second)
    {
        return CreateSpiralFingerprint(first) == CreateSpiralFingerprint(second)
            && CreateLoopFingerprint(first) != CreateLoopFingerprint(second);
    }

    internal static string CreateMergedRequestKey(SipMessage? request)
    {
        ValidateRoutingRequest(request);
        if (request == null || request.CSeq == null)
        {
            throw new System.ArgumentException("A complete routing request is required.", "request");
        }
        if (GetTag(request.To) != null)
        {
            throw new System.ArgumentException(
                "Merged-request detection applies only to requests outside a dialog.", "request");
        }

        System.Text.StringBuilder material = new System.Text.StringBuilder();
        AppendFingerprintValue(material, GetTag(request.From));
        AppendFingerprintValue(material, GetCallId(request));
        AppendFingerprintValue(material, request.CSeq.SequenceNumber.ToString(
            System.Globalization.CultureInfo.InvariantCulture));
        AppendFingerprintValue(material, request.CSeq.MethodToken);
        return HashFingerprint(material.ToString());
    }

    internal static bool IsMergedRequest(SipMessage? existing, SipMessage? candidate)
    {
        if (existing == null || candidate == null || existing.Method == SipMethod.Ack
            || candidate.Method == SipMethod.Ack)
        {
            return false;
        }
        ValidateRoutingRequest(existing);
        ValidateRoutingRequest(candidate);
        if (GetTag(existing.To) != null || GetTag(candidate.To) != null)
        {
            return false;
        }
        if (CreateMergedRequestKey(existing) != CreateMergedRequestKey(candidate))
        {
            return false;
        }
        return !TopViaTransactionMatches(existing, candidate);
    }

    internal static void RewriteRequestTarget(SipMessage? request, SipUri? target)
    {
        ValidateBasicRequest(request);
        ValidateTarget(target);
        if (request == null || request.RequestUri == null || target == null)
        {
            throw new System.ArgumentException("A request and target URI are required.", "request");
        }
        if (request.RequestUri.Scheme == SipUriScheme.Sips && target.Scheme != SipUriScheme.Sips)
        {
            throw new SipProtocolException("Request-target rewriting cannot downgrade a SIPS URI.",
                SipStatusCodes.UnsupportedUriScheme, request.MethodToken);
        }

        SipUri rewritten = CopyUri(target);
        rewritten.Headers = new SipUriHeader[0];
        rewritten.OriginalText = null;
        request.RequestUri = rewritten;
        request.OriginalBytes = new byte[0];
    }

    internal static void ValidateRoutingRequest(SipMessage? request)
    {
        ValidateBasicRequest(request);
        if (request == null || request.CSeq == null || request.From == null || request.To == null
            || request.ViaValues == null || request.ViaValues.Length == 0)
        {
            throw new System.ArgumentException(
                "A routing request requires Via, From, To, Call-ID, and CSeq values.", "request");
        }
        SipValidationIssue[] issues = SipMessageValidator.ValidateMessageModel(request, true);
        if (SipMessageValidator.HasErrorIssues(issues))
        {
            throw new System.ArgumentException("The routing request model is invalid.", "request");
        }
        if (!SipAscii.IsToken(request.CSeq.MethodToken)
            || request.MethodToken != request.CSeq.MethodToken
            || request.CSeq.SequenceNumber < 0)
        {
            throw new System.ArgumentException("The request CSeq value is invalid.", "request");
        }
        if (GetTag(request.From) == null)
        {
            throw new System.ArgumentException("The request From value requires a tag.", "request");
        }
        GetCallId(request);
        ValidateTopVia(request.ViaValues[0]);
        int maxForwardsIndex = FindSingleHeader(request.Headers, SipHeaderNames.MaxForwards, null);
        if (maxForwardsIndex >= 0)
        {
            SipHeader? maxForwards = request.Headers[maxForwardsIndex];
            SipStructuredHeaderResult parsed = SipStructuredHeaderCodec.ParseStructuredHeaderValue(
                SipHeaderNames.MaxForwards, maxForwards == null ? null : maxForwards.Value,
                new SipParseOptions());
            if (!parsed.Success)
            {
                throw new System.ArgumentException("The Max-Forwards header field is invalid.", "request");
            }
        }
    }

    static void ValidateBasicRequest(SipMessage? request)
    {
        if (request == null || request.Kind != SipMessageKind.Request || request.RequestUri == null
            || request.Headers == null || request.OriginalBytes == null
            || request.RouteValues == null || request.RecordRouteValues == null)
        {
            throw new System.ArgumentException("A SIP request with routing collections is required.",
                "request");
        }
        if (!SipAscii.IsToken(request.MethodToken)
            || SipUriValidator.ValidateUriModel(request.RequestUri).Length > 0
            || request.RequestUri.Headers == null)
        {
            throw new System.ArgumentException("The SIP request start line is invalid.", "request");
        }
    }

    static void ValidateTarget(SipUri? target)
    {
        if (target == null || SipUriValidator.ValidateUriModel(target).Length > 0)
        {
            throw new System.ArgumentException("A valid request target URI is required.", "target");
        }
    }

    static void ValidateTopVia(SipViaValue? via)
    {
        if (via == null || !SipAscii.EqualsIgnoreCase(via.ProtocolName, "SIP")
            || !SipAscii.EqualsIgnoreCase(via.ProtocolVersion, "2.0")
            || !SipAscii.IsToken(via.Transport) || via.Host == null || via.Host.Length == 0
            || !SipAscii.IsToken(via.Branch))
        {
            throw new System.ArgumentException("The request top Via value is invalid.", "request");
        }
    }

    static string GetCallId(SipMessage request)
    {
        int index = FindSingleHeader(request.Headers, SipHeaderNames.CallId,
            SipHeaderNames.CompactCallId);
        if (index < 0 || request.Headers[index] == null)
        {
            throw new System.ArgumentException("The request requires one Call-ID header field.",
                "request");
        }
        SipHeader header = request.Headers[index];
        SipStructuredHeaderResult parsed = SipStructuredHeaderCodec.ParseStructuredHeaderValue(
            SipHeaderNames.CallId, header.Value, new SipParseOptions());
        if (!parsed.Success || parsed.CallId == null)
        {
            throw new System.ArgumentException("The request Call-ID value is invalid.", "request");
        }
        return parsed.CallId;
    }

    static string? GetTag(SipNameAddress? address)
    {
        if (address == null || address.Parameters == null)
        {
            return null;
        }
        string? tag = null;
        int index = 0;
        while (index < address.Parameters.Length)
        {
            SipUriParameter? parameter = address.Parameters[index];
            if (parameter != null && SipAscii.EqualsIgnoreCase(parameter.Name, "tag"))
            {
                if (tag != null || !parameter.HasValue || !SipAscii.IsToken(parameter.Value))
                {
                    throw new System.ArgumentException("A request address tag is invalid.", "request");
                }
                tag = parameter.Value;
            }
            index = index + 1;
        }
        return tag;
    }

    static int FindSingleHeader(SipHeader[] headers, string name, string? compactName)
    {
        int found = -1;
        int index = 0;
        while (index < headers.Length)
        {
            SipHeader? header = headers[index];
            if (header != null && (SipAscii.EqualsIgnoreCase(header.Name, name)
                || compactName != null && SipAscii.EqualsIgnoreCase(header.Name, compactName)))
            {
                if (found >= 0)
                {
                    throw new SipProtocolException("A routing singleton header field is repeated.",
                        SipStatusCodes.BadRequest, null);
                }
                found = index;
            }
            index = index + 1;
        }
        return found;
    }

    static SipHeader[] InsertAfterViaHeaders(SipHeader[] headers, SipHeader added)
    {
        int insertion = 0;
        while (insertion < headers.Length)
        {
            SipHeader? header = headers[insertion];
            if (header == null || !SipAscii.EqualsIgnoreCase(header.Name, SipHeaderNames.Via)
                && !SipAscii.EqualsIgnoreCase(header.Name, SipHeaderNames.CompactVia))
            {
                break;
            }
            insertion = insertion + 1;
        }
        SipHeader[] result = new SipHeader[headers.Length + 1];
        int index = 0;
        while (index < insertion)
        {
            result[index] = headers[index];
            index = index + 1;
        }
        result[insertion] = added;
        while (index < headers.Length)
        {
            result[index + 1] = headers[index];
            index = index + 1;
        }
        return result;
    }

    static bool TopViaTransactionMatches(SipMessage first, SipMessage second)
    {
        if (first.ViaValues == null || second.ViaValues == null
            || first.ViaValues.Length == 0 || second.ViaValues.Length == 0)
        {
            return false;
        }
        SipViaValue firstVia = first.ViaValues[0];
        SipViaValue secondVia = second.ViaValues[0];
        return firstVia != null && secondVia != null
            && firstVia.Branch == secondVia.Branch
            && SipAscii.EqualsIgnoreCase(firstVia.Host, secondVia.Host)
            && EffectiveViaPort(firstVia) == EffectiveViaPort(secondVia)
            && SipAscii.EqualsIgnoreCase(firstVia.Transport, secondVia.Transport);
    }

    static int EffectiveViaPort(SipViaValue via)
    {
        if (via.Port >= 0)
        {
            return via.Port;
        }
        return SipAscii.EqualsIgnoreCase(via.Transport, SipTransportNames.Tls) ? 5061 : 5060;
    }

    static string FormatUri(SipUri uri)
    {
        SipWriteOptions options = new SipWriteOptions();
        options.Canonical = true;
        options.PreserveOriginal = false;
        options.MaxHeaderLineBytes = int.MaxValue;
        SipUri normalized = SipUriModelCopy.CreateNormalizedCopy(uri);
        SipWriteResult formatted = SipUriFormatter.FormatUriModel(normalized, options);
        if (!formatted.Success)
        {
            throw new System.ArgumentException("The request URI cannot be formatted.", "request");
        }
        return System.Text.Encoding.UTF8.GetString(formatted.Bytes);
    }

    static string FormatTopVia(SipViaValue via)
    {
        SipWriteOptions options = new SipWriteOptions();
        options.Canonical = true;
        options.PreserveOriginal = false;
        options.MaxHeaderLineBytes = int.MaxValue;
        SipWriteResult formatted = SipViaCodec.FormatViaHeaderValues(
            new SipViaValue[] { via }, options);
        if (!formatted.Success)
        {
            throw new System.ArgumentException("The request top Via cannot be formatted.", "request");
        }
        return System.Text.Encoding.UTF8.GetString(formatted.Bytes);
    }

    static void AppendNamedHeaderValues(System.Text.StringBuilder material,
        SipHeader[] headers, string name)
    {
        int index = 0;
        while (index < headers.Length)
        {
            SipHeader? header = headers[index];
            if (header != null && SipAscii.EqualsIgnoreCase(header.Name, name))
            {
                AppendFingerprintValue(material, header.Value == null ? null : header.Value.Trim());
            }
            index = index + 1;
        }
    }

    static void AppendFingerprintValue(System.Text.StringBuilder material, string? value)
    {
        if (value == null)
        {
            material.Append("-1:");
        }
        else
        {
            material.Append(value.Length.ToString(System.Globalization.CultureInfo.InvariantCulture));
            material.Append(':');
            material.Append(value);
        }
        material.Append(';');
    }

    static string HashFingerprint(string material)
    {
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(material);
        byte[] hash = System.Security.Cryptography.SHA256.HashData(bytes);
        return System.Convert.ToHexString(hash).ToLowerInvariant();
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
        copy.Parameters = CopyParameters(source.Parameters);
        copy.Headers = CopyHeaders(source.Headers);
        copy.OpaquePart = source.OpaquePart;
        copy.OriginalText = source.OriginalText;
        return copy;
    }

    static SipUriParameter[] CopyParameters(SipUriParameter[]? source)
    {
        if (source == null)
        {
            return new SipUriParameter[0];
        }
        SipUriParameter[] result = new SipUriParameter[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            SipUriParameter copy = new SipUriParameter();
            SipUriParameter? item = source[index];
            if (item != null)
            {
                copy.Name = item.Name;
                copy.Value = item.Value;
                copy.HasValue = item.HasValue;
                copy.OriginalText = item.OriginalText;
                copy.Quoted = item.Quoted;
            }
            result[index] = copy;
            index = index + 1;
        }
        return result;
    }

    static SipUriHeader[] CopyHeaders(SipUriHeader[]? source)
    {
        if (source == null)
        {
            return new SipUriHeader[0];
        }
        SipUriHeader[] result = new SipUriHeader[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            SipUriHeader copy = new SipUriHeader();
            SipUriHeader? item = source[index];
            if (item != null)
            {
                copy.Name = item.Name;
                copy.Value = item.Value;
            }
            result[index] = copy;
            index = index + 1;
        }
        return result;
    }
}
