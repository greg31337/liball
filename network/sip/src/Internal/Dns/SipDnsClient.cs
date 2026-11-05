namespace sip;

internal static class SipDnsClient
{
    internal static SipDnsClientResult QueryRecords(string? name, SipDnsRecordKind kind,
        SipDnsOptions? options, SipCancellation? cancellation)
    {
        SipDnsClientResult invalid = ValidateInputs(name, kind, options);
        if (invalid.Status != SipResolutionStatus.None || name == null || options == null)
        {
            return invalid;
        }
        if (IsCancelled(cancellation))
        {
            return CreateClientResult(SipResolutionStatus.Cancelled,
                "The DNS query was cancelled.");
        }

        SipDnsServerEndpoint[] servers = GetNameServers(options);
        if (servers.Length == 0)
        {
            return CreateClientResult(SipResolutionStatus.Failed,
                "No usable DNS name server is configured on the system.");
        }

        string currentName = NormalizeName(name);
        System.Collections.Generic.List<string> visited = new System.Collections.Generic.List<string>();
        System.Collections.Generic.List<SipDnsWireRecord> accumulated =
            new System.Collections.Generic.List<SipDnsWireRecord>();
        int depth = 0;
        int attempts = 0;
        bool usedTcp = false;
        while (true)
        {
            if (ContainsName(visited, currentName))
            {
                SipDnsClientResult loop = CreateClientResult(SipResolutionStatus.Failed,
                    "The DNS CNAME chain contains a loop.");
                loop.Attempts = attempts;
                loop.CnameDepth = depth;
                loop.UsedTcp = usedTcp;
                return loop;
            }
            visited.Add(currentName);

            SipDnsClientResult queried = QueryOneName(currentName, kind, options,
                cancellation, servers);
            attempts = attempts + queried.Attempts;
            usedTcp = usedTcp || queried.UsedTcp;
            queried.Attempts = attempts;
            queried.CnameDepth = depth;
            queried.UsedTcp = usedTcp;
            if (queried.Status != SipResolutionStatus.Success || queried.Response == null)
            {
                return queried;
            }

            AppendRecords(accumulated, queried.Response.Records);
            if (HasAnswer(queried.Response.Records, currentName, kind))
            {
                queried.Response.Records = accumulated.ToArray();
                return queried;
            }

            string? canonical = FindCanonicalName(queried.Response.Records, currentName);
            if (canonical == null)
            {
                queried.Status = SipResolutionStatus.NoRecords;
                queried.ErrorMessage = "The DNS response contains no requested records.";
                queried.Response.Records = accumulated.ToArray();
                return queried;
            }
            depth = depth + 1;
            if (depth > options.MaxCnameDepth)
            {
                queried.Status = SipResolutionStatus.Failed;
                queried.ErrorMessage = "The DNS CNAME chain exceeds the configured depth limit.";
                queried.CnameDepth = depth;
                queried.Response.Records = accumulated.ToArray();
                return queried;
            }
            currentName = NormalizeName(canonical);
            if (HasAnswer(accumulated.ToArray(), currentName, kind))
            {
                queried.Response.Records = accumulated.ToArray();
                queried.CnameDepth = depth;
                return queried;
            }
        }
    }

    static SipDnsClientResult QueryOneName(string name, SipDnsRecordKind kind,
        SipDnsOptions options, SipCancellation? cancellation,
        SipDnsServerEndpoint[] servers)
    {
        SipDnsClientResult result = new SipDnsClientResult();
        int attempt = 0;
        while (attempt < options.MaxAttempts)
        {
            if (IsCancelled(cancellation))
            {
                result.Status = SipResolutionStatus.Cancelled;
                result.ErrorMessage = "The DNS query was cancelled.";
                result.Attempts = attempt;
                return result;
            }
            SipDnsServerEndpoint server = servers[attempt % servers.Length];
            int transactionId = System.Security.Cryptography.RandomNumberGenerator.GetInt32(65536);
            byte[] query = SipDnsWireCodec.CreateQuery(name, kind, transactionId);
            long deadline = System.Environment.TickCount64 + options.TimeoutMilliseconds;
            SipDnsAttemptResult received = QueryUdp(server, query, transactionId,
                name, kind, options.MaxRecordCount, deadline, cancellation);
            attempt = attempt + 1;
            result.Attempts = attempt;
            result.NameServer = server.Text;
            result.UsedTcp = result.UsedTcp || received.UsedTcp;
            if (received.Completed && received.Response != null)
            {
                result.Response = received.Response;
                if (!received.Response.Success)
                {
                    result.Status = SipResolutionStatus.Failed;
                    result.ErrorMessage = received.Response.ErrorMessage;
                    return result;
                }
                if (received.Response.ResponseCode == 0)
                {
                    result.Status = SipResolutionStatus.Success;
                    return result;
                }
                if (received.Response.ResponseCode == 3)
                {
                    result.Status = SipResolutionStatus.NoRecords;
                    result.ErrorMessage = "The DNS name does not exist.";
                    return result;
                }
                result.Status = SipResolutionStatus.Failed;
                result.ErrorMessage = "The DNS server returned response code "
                    + received.Response.ResponseCode.ToString(
                        System.Globalization.CultureInfo.InvariantCulture) + ".";
                return result;
            }
            if (IsCancelled(cancellation))
            {
                result.Status = SipResolutionStatus.Cancelled;
                result.ErrorMessage = "The DNS query was cancelled.";
                return result;
            }
            if (!received.TimedOut && received.ErrorMessage != null)
            {
                result.ErrorMessage = received.ErrorMessage;
            }
        }
        result.Status = SipResolutionStatus.Timeout;
        if (result.ErrorMessage == null)
        {
            result.ErrorMessage = "The DNS query timed out.";
        }
        return result;
    }

    static SipDnsAttemptResult QueryUdp(SipDnsServerEndpoint server, byte[] query,
        int transactionId, string name, SipDnsRecordKind kind, int maximumRecordCount,
        long deadline, SipCancellation? cancellation)
    {
        SipDnsAttemptResult result = new SipDnsAttemptResult();
        if (server.Address == null)
        {
            result.ErrorMessage = "The DNS name-server address is unavailable.";
            return result;
        }
        System.Net.IPEndPoint endpoint = new System.Net.IPEndPoint(server.Address, server.Port);
        using (System.Net.Sockets.Socket socket = new System.Net.Sockets.Socket(
            server.Address.AddressFamily, System.Net.Sockets.SocketType.Dgram,
            System.Net.Sockets.ProtocolType.Udp))
        {
            socket.Connect(endpoint);
            socket.Send(query);
            byte[] buffer = new byte[65535];
            while (!IsCancelled(cancellation))
            {
                int remaining = RemainingMilliseconds(deadline);
                if (remaining <= 0 || !WaitForSocket(socket,
                    System.Net.Sockets.SelectMode.SelectRead, deadline, cancellation))
                {
                    result.TimedOut = true;
                    return result;
                }
                int received = socket.Receive(buffer);
                if (received < 2 || ReadTransactionId(buffer) != transactionId)
                {
                    continue;
                }
                byte[] packet = CopyPacket(buffer, received);
                SipDnsWireResult parsed = SipDnsWireCodec.ParseResponse(packet,
                    transactionId, name, kind, maximumRecordCount);
                if (parsed.Unrelated)
                {
                    continue;
                }
                if (!parsed.Success || !parsed.Truncated)
                {
                    result.Completed = true;
                    result.Response = parsed;
                    return result;
                }
                SipDnsAttemptResult tcp = QueryTcp(server, query, transactionId,
                    name, kind, maximumRecordCount, deadline, cancellation);
                tcp.UsedTcp = true;
                return tcp;
            }
        }
        result.TimedOut = true;
        return result;
    }

    static SipDnsAttemptResult QueryTcp(SipDnsServerEndpoint server, byte[] query,
        int transactionId, string name, SipDnsRecordKind kind, int maximumRecordCount,
        long deadline, SipCancellation? cancellation)
    {
        SipDnsAttemptResult result = new SipDnsAttemptResult();
        result.UsedTcp = true;
        if (server.Address == null || IsCancelled(cancellation))
        {
            result.TimedOut = true;
            return result;
        }
        System.Net.IPEndPoint endpoint = new System.Net.IPEndPoint(server.Address, server.Port);
        using (System.Net.Sockets.Socket socket = new System.Net.Sockets.Socket(
            server.Address.AddressFamily, System.Net.Sockets.SocketType.Stream,
            System.Net.Sockets.ProtocolType.Tcp))
        {
            System.Threading.Tasks.Task connection = socket.ConnectAsync(endpoint);
            if (!CompleteTaskBeforeDeadline(connection, deadline, cancellation)
                || !connection.IsCompletedSuccessfully)
            {
                result.TimedOut = RemainingMilliseconds(deadline) <= 0;
                result.ErrorMessage = result.TimedOut ? null : "The DNS TCP connection failed.";
                return result;
            }
            byte[] framedQuery = new byte[query.Length + 2];
            framedQuery[0] = (byte)((query.Length >> 8) & 255);
            framedQuery[1] = (byte)(query.Length & 255);
            System.Array.Copy(query, 0, framedQuery, 2, query.Length);
            if (!SendAll(socket, framedQuery, deadline, cancellation))
            {
                result.TimedOut = true;
                return result;
            }
            byte[] lengthBytes = new byte[2];
            if (!ReceiveAll(socket, lengthBytes, deadline, cancellation))
            {
                result.TimedOut = true;
                return result;
            }
            int responseLength = ReadTransactionId(lengthBytes);
            if (responseLength < 12)
            {
                result.Completed = true;
                result.Response = new SipDnsWireResult();
                result.Response.ErrorMessage = "The DNS TCP response length is invalid.";
                return result;
            }
            byte[] packet = new byte[responseLength];
            if (!ReceiveAll(socket, packet, deadline, cancellation))
            {
                result.TimedOut = true;
                return result;
            }
            result.Completed = true;
            result.Response = SipDnsWireCodec.ParseResponse(packet, transactionId,
                name, kind, maximumRecordCount);
            if (result.Response.Success && result.Response.Truncated)
            {
                result.Response = new SipDnsWireResult();
                result.Response.ErrorMessage = "A DNS TCP response cannot remain truncated.";
            }
            return result;
        }
    }

    static bool SendAll(System.Net.Sockets.Socket socket, byte[] bytes, long deadline,
        SipCancellation? cancellation)
    {
        int offset = 0;
        while (offset < bytes.Length && !IsCancelled(cancellation))
        {
            int remaining = RemainingMilliseconds(deadline);
            if (remaining <= 0 || !WaitForSocket(socket,
                System.Net.Sockets.SelectMode.SelectWrite, deadline, cancellation))
            {
                return false;
            }
            int sent = socket.Send(bytes, offset, bytes.Length - offset,
                System.Net.Sockets.SocketFlags.None);
            if (sent <= 0)
            {
                return false;
            }
            offset = offset + sent;
        }
        return offset == bytes.Length;
    }

    static bool ReceiveAll(System.Net.Sockets.Socket socket, byte[] bytes, long deadline,
        SipCancellation? cancellation)
    {
        int offset = 0;
        while (offset < bytes.Length && !IsCancelled(cancellation))
        {
            int remaining = RemainingMilliseconds(deadline);
            if (remaining <= 0 || !WaitForSocket(socket,
                System.Net.Sockets.SelectMode.SelectRead, deadline, cancellation))
            {
                return false;
            }
            int received = socket.Receive(bytes, offset, bytes.Length - offset,
                System.Net.Sockets.SocketFlags.None);
            if (received <= 0)
            {
                return false;
            }
            offset = offset + received;
        }
        return offset == bytes.Length;
    }

    static bool CompleteTaskBeforeDeadline(System.Threading.Tasks.Task task, long deadline,
        SipCancellation? cancellation)
    {
        while (!task.IsCompleted && !IsCancelled(cancellation))
        {
            int remaining = RemainingMilliseconds(deadline);
            if (remaining <= 0)
            {
                return false;
            }
            int interval = remaining > 50 ? 50 : remaining;
            System.Threading.Tasks.Task delay = System.Threading.Tasks.Task.Delay(interval);
            System.Threading.Tasks.Task.WhenAny(new System.Threading.Tasks.Task[] { task, delay })
                .GetAwaiter().GetResult();
        }
        return task.IsCompleted;
    }

    static bool WaitForSocket(System.Net.Sockets.Socket socket,
        System.Net.Sockets.SelectMode mode, long deadline, SipCancellation? cancellation)
    {
        while (!IsCancelled(cancellation))
        {
            int remaining = RemainingMilliseconds(deadline);
            if (remaining <= 0)
            {
                return false;
            }
            int interval = remaining > 50 ? 50 : remaining;
            if (socket.Poll(System.TimeSpan.FromMilliseconds(interval), mode))
            {
                return true;
            }
        }
        return false;
    }

    static SipDnsServerEndpoint[] GetNameServers(SipDnsOptions options)
    {
        if (options.NameServers.Length > 0)
        {
            return GetConfiguredNameServers(options.NameServers);
        }
        System.Collections.Generic.List<SipDnsServerEndpoint> servers =
            new System.Collections.Generic.List<SipDnsServerEndpoint>();
        System.Net.NetworkInformation.NetworkInterface[] interfaces =
            System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();
        int interfaceIndex = 0;
        while (interfaceIndex < interfaces.Length)
        {
            System.Net.NetworkInformation.IPInterfaceProperties properties =
                interfaces[interfaceIndex].GetIPProperties();
            System.Net.NetworkInformation.IPAddressCollection addresses = properties.DnsAddresses;
            int addressIndex = 0;
            while (addressIndex < addresses.Count)
            {
                System.Net.IPAddress address = addresses[addressIndex];
                if ((address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork
                    || address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                    && !ContainsAddress(servers, address, 53))
                {
                    servers.Add(CreateServer(address, 53));
                }
                addressIndex = addressIndex + 1;
            }
            interfaceIndex = interfaceIndex + 1;
        }
        return servers.ToArray();
    }

    static SipDnsServerEndpoint[] GetConfiguredNameServers(SipEndpointAddress[] configured)
    {
        System.Collections.Generic.List<SipDnsServerEndpoint> servers =
            new System.Collections.Generic.List<SipDnsServerEndpoint>();
        int index = 0;
        while (index < configured.Length)
        {
            SipEndpointAddress? value = configured[index];
            string? host = value == null ? null : value.Host;
            int port = value == null || value.Port < 0 ? 53 : value.Port;
            if (host != null && port > 0 && port <= 65535
                && (SipUriSyntax.IsIpv4Address(host) || SipUriSyntax.IsIpv6Address(host)))
            {
                System.Net.IPAddress address = System.Net.IPAddress.Parse(host);
                if (!ContainsAddress(servers, address, port))
                {
                    servers.Add(CreateServer(address, port));
                }
            }
            index = index + 1;
        }
        return servers.ToArray();
    }

    static SipDnsServerEndpoint CreateServer(System.Net.IPAddress address, int port)
    {
        SipDnsServerEndpoint server = new SipDnsServerEndpoint();
        server.Address = address;
        server.Port = port;
        server.Text = address.ToString() + ":" + port.ToString(
            System.Globalization.CultureInfo.InvariantCulture);
        return server;
    }

    static bool ContainsAddress(System.Collections.Generic.List<SipDnsServerEndpoint> servers,
        System.Net.IPAddress address, int port)
    {
        int index = 0;
        while (index < servers.Count)
        {
            SipDnsServerEndpoint server = servers[index];
            if (server.Address != null && server.Address.Equals(address) && server.Port == port)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static SipDnsClientResult ValidateInputs(string? name, SipDnsRecordKind kind,
        SipDnsOptions? options)
    {
        SipDnsClientResult result = new SipDnsClientResult();
        if (name == null || options == null || options.NameServers == null
            || options.TimeoutMilliseconds <= 0 || options.MaxAttempts <= 0
            || options.MaxRecordCount < 0 || options.MaxCnameDepth < 0
            || !IsQueryableKind(kind) || SipDnsWireCodec.NormalizeQueryName(name) == null)
        {
            result.Status = SipResolutionStatus.Failed;
            result.ErrorMessage = "The DNS query options are invalid.";
            return result;
        }
        return result;
    }

    static bool IsQueryableKind(SipDnsRecordKind kind)
    {
        return kind == SipDnsRecordKind.A || kind == SipDnsRecordKind.Aaaa
            || kind == SipDnsRecordKind.Cname || kind == SipDnsRecordKind.Srv
            || kind == SipDnsRecordKind.Naptr;
    }

    static bool HasAnswer(SipDnsWireRecord[] records, string name, SipDnsRecordKind kind)
    {
        int index = 0;
        while (index < records.Length)
        {
            SipDnsWireRecord? record = records[index];
            if (record != null && record.Section == 0 && record.Kind == kind
                && SipAscii.EqualsIgnoreCase(record.Name, name))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static string? FindCanonicalName(SipDnsWireRecord[] records, string name)
    {
        int index = 0;
        while (index < records.Length)
        {
            SipDnsWireRecord? record = records[index];
            if (record != null && record.Section == 0 && record.Kind == SipDnsRecordKind.Cname
                && SipAscii.EqualsIgnoreCase(record.Name, name))
            {
                return record.Target;
            }
            index = index + 1;
        }
        return null;
    }

    static void AppendRecords(System.Collections.Generic.List<SipDnsWireRecord> destination,
        SipDnsWireRecord[] records)
    {
        int index = 0;
        while (index < records.Length)
        {
            destination.Add(records[index]);
            index = index + 1;
        }
    }

    static bool ContainsName(System.Collections.Generic.List<string> names, string value)
    {
        int index = 0;
        while (index < names.Count)
        {
            if (SipAscii.EqualsIgnoreCase(names[index], value))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static string NormalizeName(string name)
    {
        string normalized = name;
        if (normalized.Length > 1 && normalized[normalized.Length - 1] == '.')
        {
            normalized = normalized.Substring(0, normalized.Length - 1);
        }
        return normalized.ToLowerInvariant();
    }

    static bool IsCancelled(SipCancellation? cancellation)
    {
        return cancellation != null && cancellation.Requested;
    }

    static int RemainingMilliseconds(long deadline)
    {
        long remaining = deadline - System.Environment.TickCount64;
        if (remaining <= 0)
        {
            return 0;
        }
        return remaining > int.MaxValue ? int.MaxValue : (int)remaining;
    }

    static int ReadTransactionId(byte[] packet)
    {
        return packet[0] * 256 + packet[1];
    }

    static byte[] CopyPacket(byte[] buffer, int length)
    {
        byte[] packet = new byte[length];
        System.Array.Copy(buffer, packet, length);
        return packet;
    }

    static SipDnsClientResult CreateClientResult(SipResolutionStatus status, string message)
    {
        SipDnsClientResult result = new SipDnsClientResult();
        result.Status = status;
        result.ErrorMessage = message;
        return result;
    }
}
