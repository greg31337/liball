namespace sip;

internal static class SipGenericValueAccess
{
    internal static SipHeader[] FindMessageHeaders(SipMessage? message, string? name)
    {
        System.Collections.Generic.List<SipHeader> matches = new System.Collections.Generic.List<SipHeader>();
        if (message == null || message.Headers == null || !SipAscii.IsToken(name)) return matches.ToArray();
        int index = 0;
        while (index < message.Headers.Length)
        {
            SipHeader? header = message.Headers[index];
            if (header != null && HeaderNamesMatch(header.Name, name)) matches.Add(header);
            index = index + 1;
        }
        return matches.ToArray();
    }

    internal static SipUriParameter[] FindNamedParameters(SipUriParameter[]? parameters, string? name)
    {
        System.Collections.Generic.List<SipUriParameter> matches =
            new System.Collections.Generic.List<SipUriParameter>();
        if (parameters == null || !SipAscii.IsToken(name)) return matches.ToArray();
        int index = 0;
        while (index < parameters.Length)
        {
            SipUriParameter? parameter = parameters[index];
            if (parameter != null && SipAscii.EqualsIgnoreCase(parameter.Name, name)) matches.Add(parameter);
            index = index + 1;
        }
        return matches.ToArray();
    }

    internal static bool HeaderNamesMatch(string? left, string? right)
    {
        if (SipAscii.EqualsIgnoreCase(left, right)) return true;
        return IsHeaderAliasPair(left, right, SipHeaderNames.AcceptContact, SipHeaderNames.CompactAcceptContact)
            || IsHeaderAliasPair(left, right, SipHeaderNames.AllowEvents, SipHeaderNames.CompactAllowEvents)
            || IsHeaderAliasPair(left, right, SipHeaderNames.CallId, SipHeaderNames.CompactCallId)
            || IsHeaderAliasPair(left, right, SipHeaderNames.Contact, SipHeaderNames.CompactContact)
            || IsHeaderAliasPair(left, right, SipHeaderNames.ContentEncoding, SipHeaderNames.CompactContentEncoding)
            || IsHeaderAliasPair(left, right, SipHeaderNames.ContentLength, SipHeaderNames.CompactContentLength)
            || IsHeaderAliasPair(left, right, SipHeaderNames.ContentType, SipHeaderNames.CompactContentType)
            || IsHeaderAliasPair(left, right, SipHeaderNames.Event, SipHeaderNames.CompactEvent)
            || IsHeaderAliasPair(left, right, SipHeaderNames.From, SipHeaderNames.CompactFrom)
            || IsHeaderAliasPair(left, right, SipHeaderNames.Identity, SipHeaderNames.CompactIdentity)
            || IsHeaderAliasPair(left, right, SipHeaderNames.ReferTo, SipHeaderNames.CompactReferTo)
            || IsHeaderAliasPair(left, right, SipHeaderNames.ReferredBy, SipHeaderNames.CompactReferredBy)
            || IsHeaderAliasPair(left, right, SipHeaderNames.RejectContact, SipHeaderNames.CompactRejectContact)
            || IsHeaderAliasPair(left, right, SipHeaderNames.RequestDisposition,
                SipHeaderNames.CompactRequestDisposition)
            || IsHeaderAliasPair(left, right, SipHeaderNames.SessionExpires, SipHeaderNames.CompactSessionExpires)
            || IsHeaderAliasPair(left, right, SipHeaderNames.Subject, SipHeaderNames.CompactSubject)
            || IsHeaderAliasPair(left, right, SipHeaderNames.Supported, SipHeaderNames.CompactSupported)
            || IsHeaderAliasPair(left, right, SipHeaderNames.To, SipHeaderNames.CompactTo)
            || IsHeaderAliasPair(left, right, SipHeaderNames.Via, SipHeaderNames.CompactVia);
    }

    static bool IsHeaderAliasPair(string? left, string? right, string longName, string compactName)
    {
        return SipAscii.EqualsIgnoreCase(left, longName) && SipAscii.EqualsIgnoreCase(right, compactName)
            || SipAscii.EqualsIgnoreCase(left, compactName) && SipAscii.EqualsIgnoreCase(right, longName);
    }
}
