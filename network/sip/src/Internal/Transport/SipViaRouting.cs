namespace sip;

internal static class SipViaRouting
{
    internal static bool AddRequestViaAlias(SipMessage? request)
    {
        if (request == null || request.Kind != SipMessageKind.Request
            || request.Headers == null || request.ViaValues == null
            || request.ViaValues.Length == 0
            || !SipAscii.EqualsIgnoreCase(
                request.ViaValues[0].Transport, SipTransportNames.Tls))
        {
            return false;
        }
        SipViaValue updated = CopyViaValue(request.ViaValues[0]);
        int existingIndex = FindParameterIndex(updated.Parameters, "alias");
        if (existingIndex >= 0)
        {
            return !updated.Parameters[existingIndex].HasValue;
        }
        SipUriParameter alias = new SipUriParameter();
        alias.Name = "alias";
        updated.Parameters = InsertParameterAt(
            updated.Parameters, alias, updated.Parameters.Length);
        if (!RewriteFirstViaHeader(request.Headers, updated))
        {
            return false;
        }
        request.ViaValues[0] = updated;
        request.OriginalBytes = new byte[0];
        return true;
    }

    internal static SipViaValue InsertRequestVia(SipMessage? request,
        SipEndpointAddress? sentBy, string? branch, bool requestRPort)
    {
        if (request == null || request.Kind != SipMessageKind.Request)
        {
            throw new System.ArgumentException("A SIP request is required.", "request");
        }
        if (request.Headers == null || request.ViaValues == null)
        {
            throw new System.ArgumentException("The request header and Via collections are required.", "request");
        }
        if (sentBy == null)
        {
            throw new System.ArgumentNullException("sentBy");
        }
        if (!SipAscii.IsToken(branch) || branch == null
            || !branch.StartsWith("z9hG4bK", System.StringComparison.Ordinal))
        {
            throw new System.ArgumentException(
                "The branch must be a SIP token beginning with the magic cookie.", "branch");
        }

        SipViaValue via = CreateRequestViaValue(sentBy, branch, requestRPort);
        string value = FormatViaValueArray(new SipViaValue[] { via });

        SipHeader header = new SipHeader();
        header.Name = SipHeaderNames.Via;
        header.Value = value;
        request.Headers = InsertHeaderBeforeFirstVia(request.Headers, header);
        request.ViaValues = PrependViaValue(request.ViaValues, via);
        request.OriginalBytes = new byte[0];
        return via;
    }

    internal static SipViaValue ReplaceRequestVia(SipMessage? request,
        SipEndpointAddress? sentBy, string? branch, bool requestRPort)
    {
        if (request == null || request.Kind != SipMessageKind.Request)
        {
            throw new System.ArgumentException("A SIP request is required.", "request");
        }
        if (request.Headers == null || request.ViaValues == null)
        {
            throw new System.ArgumentException("The request header and Via collections are required.", "request");
        }
        if (sentBy == null)
        {
            throw new System.ArgumentNullException("sentBy");
        }
        if (!SipAscii.IsToken(branch) || branch == null
            || !branch.StartsWith("z9hG4bK", System.StringComparison.Ordinal))
        {
            throw new System.ArgumentException(
                "The branch must be a SIP token beginning with the magic cookie.", "branch");
        }
        if (request.ViaValues.Length == 0)
        {
            return InsertRequestVia(request, sentBy, branch, requestRPort);
        }

        SipViaValue via = CreateRequestViaValue(sentBy, branch, requestRPort);
        if (!RewriteFirstViaHeader(request.Headers, via))
        {
            throw new System.ArgumentException(
                "The request top Via header cannot be replaced.", "request");
        }
        request.ViaValues[0] = via;
        request.OriginalBytes = new byte[0];
        return via;
    }

    internal static bool TopViaMatches(SipMessage? message, string? sentByHost,
        int sentByPort, string? branch)
    {
        if (message == null || message.ViaValues == null || message.ViaValues.Length == 0)
        {
            return false;
        }

        return ViaMatches(message.ViaValues[0], sentByHost, sentByPort, branch);
    }

    internal static bool ApplyReceivedAndRPort(SipMessage? request,
        SipEndpointAddress? remoteAddress)
    {
        if (request == null || request.Kind != SipMessageKind.Request
            || request.ViaValues == null || request.ViaValues.Length == 0
            || request.Headers == null || remoteAddress == null
            || remoteAddress.Host == null || remoteAddress.Port <= 0
            || remoteAddress.Port > 65535
            || !SipUriSyntax.IsIpv4Address(remoteAddress.Host)
                && !SipUriSyntax.IsIpv6Address(remoteAddress.Host))
        {
            return false;
        }

        SipViaValue updated = CopyViaValue(request.ViaValues[0]);
        bool requestedRPort = updated.HasRPort && updated.RPort < 0;
        bool sentByIsAddress = updated.Host != null
            && (SipUriSyntax.IsIpv4Address(updated.Host) || SipUriSyntax.IsIpv6Address(updated.Host));
        bool needsReceived = requestedRPort || updated.Received != null || !sentByIsAddress
            || !SentByHostsMatch(updated.Host, remoteAddress.Host);
        if (needsReceived)
        {
            updated.Received = remoteAddress.Host;
            SetReceivedParameter(updated, remoteAddress.Host);
        }
        if (requestedRPort)
        {
            updated.RPort = remoteAddress.Port;
            SetRPortParameter(updated, remoteAddress.Port);
        }

        if (!RewriteFirstViaHeader(request.Headers, updated))
        {
            return false;
        }

        request.ViaValues[0] = updated;
        request.OriginalBytes = new byte[0];
        return true;
    }

    internal static bool RemoveResponseVia(SipMessage? response, string? sentByHost,
        int sentByPort, string? branch)
    {
        if (response == null || response.Kind != SipMessageKind.Response
            || response.Headers == null || response.ViaValues == null
            || response.ViaValues.Length == 0
            || !ViaMatches(response.ViaValues[0], sentByHost, sentByPort, branch))
        {
            return false;
        }

        int headerIndex = FindFirstViaHeaderIndex(response.Headers);
        if (headerIndex < 0)
        {
            return false;
        }
        SipHeader? header = response.Headers[headerIndex];
        if (header == null)
        {
            return false;
        }
        SipViaParseResult parsed = SipViaCodec.ParseViaHeaderValue(header.Value, new SipParseOptions());
        if (!parsed.Success || parsed.Values.Length == 0
            || !ViaMatches(parsed.Values[0], sentByHost, sentByPort, branch))
        {
            return false;
        }

        if (parsed.Values.Length == 1)
        {
            response.Headers = RemoveHeaderAt(response.Headers, headerIndex);
        }
        else
        {
            SipViaValue[] remainingHeaderValues = RemoveFirstViaValue(parsed.Values);
            header.Value = FormatViaValueArray(remainingHeaderValues);
            header.OriginalBytes = new byte[0];
        }
        response.ViaValues = RemoveFirstViaValue(response.ViaValues);
        response.OriginalBytes = new byte[0];
        return true;
    }

    internal static SipSendRequest CreateResponseSendRequest(SipMessage? response,
        SipTransportMessage? receivedRequest)
    {
        if (response == null || response.Kind != SipMessageKind.Response
            || response.ViaValues == null || response.ViaValues.Length == 0)
        {
            throw new System.ArgumentException("A SIP response with a top Via is required.", "response");
        }
        if (receivedRequest == null)
        {
            throw new System.ArgumentNullException("receivedRequest");
        }

        SipViaValue via = response.ViaValues[0];
        SipTransportKind transport = GetTransportKind(via.Transport);
        SipSendRequest send = new SipSendRequest();
        send.Message = response;
        send.Transport = transport;

        SipConnectionInfo? connection = receivedRequest.Connection;
        if (IsReliableTransport(transport) && connection != null
            && connection.State == SipConnectionState.Connected
            && connection.Id != null && connection.Id.Length > 0
            && connection.Transport == transport)
        {
            send.ConnectionId = connection.Id;
            send.RemoteAddress = CopyEndpointAddress(connection.RemoteAddress == null
                ? receivedRequest.RemoteAddress : connection.RemoteAddress);
            send.LocalAddress = CopyEndpointAddress(connection.LocalAddress == null
                ? receivedRequest.LocalAddress : connection.LocalAddress);
            return send;
        }

        string? host = via.Host;
        int port = via.Port < 0 ? GetDefaultPort(transport) : via.Port;
        if (!IsReliableTransport(transport))
        {
            string? multicastAddress = FindMAddr(via.Parameters);
            if (multicastAddress != null)
            {
                host = RemoveIpv6Brackets(multicastAddress);
            }
            else if (via.Received != null && via.HasRPort && via.RPort >= 0)
            {
                host = via.Received;
                port = via.RPort;
                send.LocalAddress = CopyEndpointAddress(receivedRequest.LocalAddress);
            }
            else if (via.Received != null)
            {
                host = via.Received;
            }
        }
        else if (via.Received != null)
        {
            host = via.Received;
        }

        if (host == null || host.Length == 0)
        {
            throw new SipProtocolException("The top Via does not identify a response destination.",
                response.StatusCode, null);
        }

        SipEndpointAddress destination = new SipEndpointAddress();
        destination.Host = host;
        destination.Port = port;
        destination.Transport = transport;
        destination.Secure = IsSecureTransport(transport);
        send.RemoteAddress = destination;
        return send;
    }

    static SipUriParameter[] CreateInitialParameters(string branch, bool requestRPort)
    {
        SipUriParameter branchParameter = new SipUriParameter();
        branchParameter.Name = "branch";
        branchParameter.Value = branch;
        branchParameter.HasValue = true;
        if (!requestRPort)
        {
            return new SipUriParameter[] { branchParameter };
        }

        SipUriParameter rportParameter = new SipUriParameter();
        rportParameter.Name = "rport";
        return new SipUriParameter[] { branchParameter, rportParameter };
    }

    static SipViaValue CreateRequestViaValue(SipEndpointAddress sentBy,
        string branch, bool requestRPort)
    {
        SipViaValue via = new SipViaValue();
        via.ProtocolName = "SIP";
        via.ProtocolVersion = "2.0";
        via.Transport = GetTransportToken(sentBy.Transport);
        via.Host = sentBy.Host;
        via.Port = sentBy.Port;
        via.Branch = branch;
        via.HasRPort = requestRPort;
        via.Parameters = CreateInitialParameters(branch, requestRPort);
        return via;
    }

    static SipHeader[] InsertHeaderBeforeFirstVia(SipHeader[] headers, SipHeader added)
    {
        int insertionIndex = FindFirstViaHeaderIndex(headers);
        if (insertionIndex < 0)
        {
            insertionIndex = 0;
        }
        SipHeader[] result = new SipHeader[headers.Length + 1];
        int sourceIndex = 0;
        int destinationIndex = 0;
        while (destinationIndex < result.Length)
        {
            if (destinationIndex == insertionIndex)
            {
                result[destinationIndex] = added;
            }
            else
            {
                result[destinationIndex] = headers[sourceIndex];
                sourceIndex = sourceIndex + 1;
            }
            destinationIndex = destinationIndex + 1;
        }
        return result;
    }

    static SipViaValue[] PrependViaValue(SipViaValue[] values, SipViaValue added)
    {
        SipViaValue[] result = new SipViaValue[values.Length + 1];
        result[0] = added;
        int index = 0;
        while (index < values.Length)
        {
            result[index + 1] = values[index];
            index = index + 1;
        }
        return result;
    }

    static bool ViaMatches(SipViaValue? via, string? sentByHost, int sentByPort,
        string? branch)
    {
        if (via == null || sentByHost == null || branch == null)
        {
            return false;
        }
        int defaultPort = GetViaDefaultPort(via.Transport);
        int viaPort = via.Port < 0 ? defaultPort : via.Port;
        int comparedPort = sentByPort < 0 ? defaultPort : sentByPort;
        return SentByHostsMatch(via.Host, sentByHost)
            && viaPort == comparedPort
            && string.Equals(via.Branch, branch,
                System.StringComparison.Ordinal);
    }

    static bool SentByHostsMatch(string? left, string? right)
    {
        if (left == null || right == null)
        {
            return false;
        }
        string normalizedLeft = RemoveIpv6Brackets(left);
        string normalizedRight = RemoveIpv6Brackets(right);
        bool leftIsAddress = SipUriSyntax.IsIpv4Address(normalizedLeft)
            || SipUriSyntax.IsIpv6Address(normalizedLeft);
        bool rightIsAddress = SipUriSyntax.IsIpv4Address(normalizedRight)
            || SipUriSyntax.IsIpv6Address(normalizedRight);
        if (leftIsAddress && rightIsAddress)
        {
            return System.Net.IPAddress.Parse(normalizedLeft).Equals(
                System.Net.IPAddress.Parse(normalizedRight));
        }
        return SipAscii.EqualsIgnoreCase(left, right);
    }

    static int GetViaDefaultPort(string? transport)
    {
        return SipAscii.EqualsIgnoreCase(transport, SipTransportNames.Tls)
            || SipAscii.EqualsIgnoreCase(transport, SipTransportNames.TlsSctp)
            || SipAscii.EqualsIgnoreCase(transport,
                SipTransportNames.SecureWebSocket)
            || SipAscii.EqualsIgnoreCase(transport,
                SipTransportNames.DatagramTls)
            ? SipTransportNames.SipsPort : SipTransportNames.SipPort;
    }

    static bool RewriteFirstViaHeader(SipHeader[] headers, SipViaValue updated)
    {
        int headerIndex = FindFirstViaHeaderIndex(headers);
        if (headerIndex < 0)
        {
            return false;
        }
        SipHeader? header = headers[headerIndex];
        if (header == null)
        {
            return false;
        }
        SipViaParseResult parsed = SipViaCodec.ParseViaHeaderValue(header.Value, new SipParseOptions());
        if (!parsed.Success || parsed.Values.Length == 0)
        {
            return false;
        }
        parsed.Values[0] = updated;
        header.Value = FormatViaValueArray(parsed.Values);
        header.OriginalBytes = new byte[0];
        return true;
    }

    static void SetReceivedParameter(SipViaValue via, string address)
    {
        int existingIndex = FindParameterIndex(via.Parameters, "received");
        if (existingIndex >= 0)
        {
            SipUriParameter existing = via.Parameters[existingIndex];
            existing.Value = address;
            existing.HasValue = true;
            existing.OriginalText = null;
            existing.Quoted = false;
            return;
        }

        int insertionIndex = FindParameterIndex(via.Parameters, "rport");
        if (insertionIndex < 0)
        {
            insertionIndex = via.Parameters.Length;
        }
        SipUriParameter parameter = new SipUriParameter();
        parameter.Name = "received";
        parameter.Value = address;
        parameter.HasValue = true;
        via.Parameters = InsertParameterAt(via.Parameters, parameter, insertionIndex);
    }

    static void SetRPortParameter(SipViaValue via, int port)
    {
        int existingIndex = FindParameterIndex(via.Parameters, "rport");
        SipUriParameter parameter;
        if (existingIndex >= 0)
        {
            parameter = via.Parameters[existingIndex];
        }
        else
        {
            parameter = new SipUriParameter();
            parameter.Name = "rport";
            via.Parameters = InsertParameterAt(via.Parameters, parameter, via.Parameters.Length);
        }
        parameter.Value = port.ToString(System.Globalization.CultureInfo.InvariantCulture);
        parameter.HasValue = true;
        parameter.OriginalText = null;
        parameter.Quoted = false;
    }

    static SipUriParameter[] InsertParameterAt(SipUriParameter[] parameters,
        SipUriParameter added, int insertionIndex)
    {
        SipUriParameter[] result = new SipUriParameter[parameters.Length + 1];
        int sourceIndex = 0;
        int destinationIndex = 0;
        while (destinationIndex < result.Length)
        {
            if (destinationIndex == insertionIndex)
            {
                result[destinationIndex] = added;
            }
            else
            {
                result[destinationIndex] = parameters[sourceIndex];
                sourceIndex = sourceIndex + 1;
            }
            destinationIndex = destinationIndex + 1;
        }
        return result;
    }

    static int FindParameterIndex(SipUriParameter[]? parameters, string name)
    {
        if (parameters == null)
        {
            return -1;
        }
        int index = 0;
        while (index < parameters.Length)
        {
            SipUriParameter? parameter = parameters[index];
            if (parameter != null && SipAscii.EqualsIgnoreCase(parameter.Name, name))
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    static int FindFirstViaHeaderIndex(SipHeader[] headers)
    {
        int index = 0;
        while (index < headers.Length)
        {
            SipHeader? header = headers[index];
            if (header != null && (SipAscii.EqualsIgnoreCase(header.Name, SipHeaderNames.Via)
                || SipAscii.EqualsIgnoreCase(header.Name, SipHeaderNames.CompactVia)))
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    static SipHeader[] RemoveHeaderAt(SipHeader[] headers, int removalIndex)
    {
        SipHeader[] result = new SipHeader[headers.Length - 1];
        int sourceIndex = 0;
        int destinationIndex = 0;
        while (sourceIndex < headers.Length)
        {
            if (sourceIndex != removalIndex)
            {
                result[destinationIndex] = headers[sourceIndex];
                destinationIndex = destinationIndex + 1;
            }
            sourceIndex = sourceIndex + 1;
        }
        return result;
    }

    static SipViaValue[] RemoveFirstViaValue(SipViaValue[] values)
    {
        SipViaValue[] result = new SipViaValue[values.Length - 1];
        int index = 1;
        while (index < values.Length)
        {
            result[index - 1] = values[index];
            index = index + 1;
        }
        return result;
    }

    static string FormatViaValueArray(SipViaValue[] values)
    {
        SipWriteResult formatted = SipViaCodec.FormatViaHeaderValues(values, new SipWriteOptions());
        if (!formatted.Success)
        {
            throw new System.ArgumentException(formatted.ErrorMessage == null
                ? "The Via value is invalid." : formatted.ErrorMessage, "values");
        }
        string line = System.Text.Encoding.UTF8.GetString(formatted.Bytes);
        int colon = line.IndexOf(':');
        int start = colon + 1;
        while (start < line.Length && (line[start] == ' ' || line[start] == '\t'))
        {
            start = start + 1;
        }
        int end = line.Length;
        if (end >= 2 && line[end - 2] == '\r' && line[end - 1] == '\n')
        {
            end = end - 2;
        }
        return line.Substring(start, end - start);
    }

    static SipViaValue CopyViaValue(SipViaValue source)
    {
        SipViaValue copy = new SipViaValue();
        copy.ProtocolName = source.ProtocolName;
        copy.ProtocolVersion = source.ProtocolVersion;
        copy.Transport = source.Transport;
        copy.Host = source.Host;
        copy.Port = source.Port;
        copy.Parameters = CopyParameterArray(source.Parameters);
        copy.Branch = source.Branch;
        copy.Received = source.Received;
        copy.RPort = source.RPort;
        copy.HasRPort = source.HasRPort;
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

    static string? FindMAddr(SipUriParameter[]? parameters)
    {
        int index = FindParameterIndex(parameters, "maddr");
        if (index < 0 || parameters == null || !parameters[index].HasValue)
        {
            return null;
        }
        return parameters[index].Value;
    }

    static string RemoveIpv6Brackets(string host)
    {
        if (host.Length > 2 && host[0] == '[' && host[host.Length - 1] == ']')
        {
            return host.Substring(1, host.Length - 2);
        }
        return host;
    }

    static SipEndpointAddress? CopyEndpointAddress(SipEndpointAddress? source)
    {
        if (source == null)
        {
            return null;
        }
        SipEndpointAddress copy = new SipEndpointAddress();
        copy.Host = source.Host;
        copy.Port = source.Port;
        copy.Transport = source.Transport;
        copy.Secure = source.Secure;
        return copy;
    }

    static string GetTransportToken(SipTransportKind transport)
    {
        switch (transport)
        {
            case SipTransportKind.Udp: return SipTransportNames.Udp;
            case SipTransportKind.Tcp: return SipTransportNames.Tcp;
            case SipTransportKind.Tls: return SipTransportNames.Tls;
            case SipTransportKind.WebSocket: return SipTransportNames.WebSocket;
            case SipTransportKind.SecureWebSocket: return SipTransportNames.SecureWebSocket;
            case SipTransportKind.Sctp: return SipTransportNames.Sctp;
            case SipTransportKind.Dtls: return SipTransportNames.DatagramTls;
            default: throw new System.ArgumentException("The endpoint transport cannot be written in a Via.",
                "transport");
        }
    }

    static SipTransportKind GetTransportKind(string? transport)
    {
        if (SipAscii.EqualsIgnoreCase(transport, SipTransportNames.Udp)) return SipTransportKind.Udp;
        if (SipAscii.EqualsIgnoreCase(transport, SipTransportNames.Tcp)) return SipTransportKind.Tcp;
        if (SipAscii.EqualsIgnoreCase(transport, SipTransportNames.Tls)
            || SipAscii.EqualsIgnoreCase(transport, SipTransportNames.TlsSctp)) return SipTransportKind.Tls;
        if (SipAscii.EqualsIgnoreCase(transport, SipTransportNames.WebSocket))
            return SipTransportKind.WebSocket;
        if (SipAscii.EqualsIgnoreCase(transport, SipTransportNames.SecureWebSocket))
            return SipTransportKind.SecureWebSocket;
        if (SipAscii.EqualsIgnoreCase(transport, SipTransportNames.Sctp)) return SipTransportKind.Sctp;
        if (SipAscii.EqualsIgnoreCase(transport, SipTransportNames.DatagramTls)) return SipTransportKind.Dtls;
        throw new SipProtocolException("The top Via uses an unsupported transport.", 0, null);
    }

    static bool IsReliableTransport(SipTransportKind transport)
    {
        return transport == SipTransportKind.Tcp || transport == SipTransportKind.Tls
            || transport == SipTransportKind.WebSocket
            || transport == SipTransportKind.SecureWebSocket
            || transport == SipTransportKind.Sctp;
    }

    static bool IsSecureTransport(SipTransportKind transport)
    {
        return transport == SipTransportKind.Tls
            || transport == SipTransportKind.SecureWebSocket
            || transport == SipTransportKind.Dtls;
    }

    static int GetDefaultPort(SipTransportKind transport)
    {
        return IsSecureTransport(transport) ? SipTransportNames.SipsPort : SipTransportNames.SipPort;
    }
}
