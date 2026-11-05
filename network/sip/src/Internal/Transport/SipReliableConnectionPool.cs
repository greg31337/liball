namespace sip;

internal class SipReliableConnectionPool
{
    ISipMonotonicClock clock;
    int maximumConnections;
    SipConnectionAliasTable aliasTable;
    System.Collections.Generic.List<SipReliableConnectionCore> connections =
        new System.Collections.Generic.List<SipReliableConnectionCore>();

    internal SipReliableConnectionPool(ISipMonotonicClock monotonicClock,
        int maximumConnectionCount)
    {
        if (monotonicClock == null || maximumConnectionCount <= 0)
        {
            throw new System.ArgumentException(
                "The reliable connection pool options are invalid.",
                "maximumConnectionCount");
        }
        clock = monotonicClock;
        maximumConnections = maximumConnectionCount;
        aliasTable = new SipConnectionAliasTable(maximumConnectionCount);
    }

    internal bool AddConnection(SipReliableConnectionCore? connection)
    {
        if (connection == null || connections.Count >= maximumConnections)
        {
            return false;
        }
        SipConnectionInfo info = connection.GetConnectionInfo();
        if (info.Id == null || info.State != SipConnectionState.Connected
            || FindById(info.Id) != null)
        {
            return false;
        }
        connections.Add(connection);
        return true;
    }

    internal SipReliableConnectionCore? FindConnection(string? connectionId,
        SipEndpointAddress? remoteAddress, SipTransportKind transport,
        bool allowEndpointReuse)
    {
        if (connectionId != null && connectionId.Length > 0)
        {
            SipReliableConnectionCore? identified = FindById(connectionId);
            if (identified != null)
            {
                SipConnectionInfo info = identified.GetConnectionInfo();
                if (info.State == SipConnectionState.Connected
                    && info.Transport == transport)
                {
                    return identified;
                }
            }
            return null;
        }
        if (!allowEndpointReuse || remoteAddress == null)
        {
            return null;
        }
        int index = 0;
        while (index < connections.Count)
        {
            SipReliableConnectionCore candidate = connections[index];
            SipConnectionInfo info = candidate.GetConnectionInfo();
            if (info.State == SipConnectionState.Connected
                && info.Transport == transport
                && EndpointsMatch(info.RemoteAddress, remoteAddress))
            {
                return candidate;
            }
            index = index + 1;
        }
        return null;
    }

    internal SipReliableConnectionCore? RemoveConnection(string? connectionId)
    {
        int index = FindIndex(connectionId);
        if (index < 0)
        {
            return null;
        }
        SipReliableConnectionCore result = connections[index];
        connections.RemoveAt(index);
        aliasTable.RemoveAliases(connectionId);
        return result;
    }

    internal SipReliableConnectionCore[] ExpireIdleConnections()
    {
        System.Collections.Generic.List<SipReliableConnectionCore> expired =
            new System.Collections.Generic.List<SipReliableConnectionCore>();
        int index = connections.Count - 1;
        while (index >= 0)
        {
            SipReliableConnectionCore connection = connections[index];
            if (connection.IsIdleExpired())
            {
                SipConnectionInfo info = connection.GetConnectionInfo();
                connection.Close("The reliable connection idle timeout expired.");
                expired.Add(connection);
                connections.RemoveAt(index);
                aliasTable.RemoveAliases(info.Id);
            }
            index = index - 1;
        }
        ReverseConnections(expired);
        return expired.ToArray();
    }

    internal int CountConnections()
    {
        return connections.Count;
    }

    internal long GetTimestampMilliseconds()
    {
        return clock.GetMilliseconds();
    }

    internal bool AddClientAlias(SipReliableConnectionCore? connection,
        SipEndpointAddress? resolvedAddress, string? destinationDomain)
    {
        return aliasTable.AddClientAlias(
            connection, resolvedAddress, destinationDomain);
    }

    internal bool AddServerAlias(SipReliableConnectionCore? connection,
        SipViaValue? via)
    {
        return aliasTable.AddServerAlias(connection, via);
    }

    internal SipReliableConnectionCore? FindAliasedConnection(
        SipEndpointAddress? resolvedAddress, string? destinationDomain)
    {
        return aliasTable.FindAlias(resolvedAddress, destinationDomain);
    }

    internal int CountAliases()
    {
        return aliasTable.CountAliases();
    }

    SipReliableConnectionCore? FindById(string connectionId)
    {
        int index = FindIndex(connectionId);
        return index < 0 ? null : connections[index];
    }

    int FindIndex(string? connectionId)
    {
        if (connectionId == null)
        {
            return -1;
        }
        int index = 0;
        while (index < connections.Count)
        {
            SipConnectionInfo info = connections[index].GetConnectionInfo();
            if (info.Id == connectionId)
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    static bool EndpointsMatch(SipEndpointAddress? left, SipEndpointAddress? right)
    {
        return left != null && right != null && left.Host != null && right.Host != null
            && SipAscii.EqualsIgnoreCase(left.Host, right.Host)
            && left.Port == right.Port && left.Secure == right.Secure;
    }

    static void ReverseConnections(
        System.Collections.Generic.List<SipReliableConnectionCore> values)
    {
        int left = 0;
        int right = values.Count - 1;
        while (left < right)
        {
            SipReliableConnectionCore temporary = values[left];
            values[left] = values[right];
            values[right] = temporary;
            left = left + 1;
            right = right - 1;
        }
    }
}
