namespace sip;

internal class SipConnectionAliasTable
{
    int maximumAliases;
    System.Collections.Generic.List<SipConnectionAlias> aliases =
        new System.Collections.Generic.List<SipConnectionAlias>();

    internal SipConnectionAliasTable(int maximumAliasCount)
    {
        if (maximumAliasCount <= 0)
        {
            throw new System.ArgumentException(
                "The maximum alias count must be positive.", "maximumAliasCount");
        }
        maximumAliases = maximumAliasCount;
    }

    internal bool AddClientAlias(SipReliableConnectionCore? connection,
        SipEndpointAddress? resolvedAddress, string? destinationDomain)
    {
        SipConnectionInfo? info = GetAuthenticatedConnection(connection, false);
        if (connection == null || resolvedAddress == null || info == null
            || info.CertificateIdentity == null
            || !ValidDestination(resolvedAddress)
            || !SipCertificateDomainIdentity.MatchesDomains(
                info.CertificateIdentity.Domains, destinationDomain))
        {
            return false;
        }
        return AddAlias(connection, resolvedAddress,
            info.CertificateIdentity.Domains, false);
    }

    internal bool AddServerAlias(SipReliableConnectionCore? connection,
        SipViaValue? via)
    {
        SipConnectionInfo? info = GetAuthenticatedConnection(connection, true);
        if (connection == null || info == null || info.RemoteAddress == null
            || info.RemoteAddress.Host == null || info.CertificateIdentity == null
            || via == null || !HasAliasParameter(via.Parameters)
            || !SipAscii.EqualsIgnoreCase(via.Transport, SipTransportNames.Tls)
            || via.Port < -1 || via.Port == 0 || via.Port > 65535)
        {
            return false;
        }
        bool numericHost = via.Host != null
            && (SipUriSyntax.IsIpv4Address(via.Host)
                || SipUriSyntax.IsIpv6Address(via.Host));
        if (!numericHost && !SipCertificateDomainIdentity.MatchesDomains(
            info.CertificateIdentity.Domains, via.Host))
        {
            return false;
        }
        SipEndpointAddress destination = new SipEndpointAddress();
        destination.Host = info.RemoteAddress.Host;
        destination.Port = via.Port < 0 ? SipTransportNames.SipsPort : via.Port;
        destination.Transport = SipTransportKind.Tls;
        destination.Secure = true;
        return AddAlias(connection, destination,
            info.CertificateIdentity.Domains, true);
    }

    internal SipReliableConnectionCore? FindAlias(
        SipEndpointAddress? resolvedAddress, string? destinationDomain)
    {
        if (!ValidDestination(resolvedAddress)
            || SipCertificateDomainIdentity.NormalizeDomain(destinationDomain) == null)
        {
            return null;
        }
        int index = 0;
        while (index < aliases.Count)
        {
            SipConnectionAlias alias = aliases[index];
            if (alias.Connection != null && alias.Destination != null
                && EndpointsMatch(alias.Destination, resolvedAddress)
                && SipCertificateDomainIdentity.MatchesDomains(
                    alias.Identities, destinationDomain))
            {
                SipConnectionInfo info = alias.Connection.GetConnectionInfo();
                if (info.State == SipConnectionState.Connected
                    && info.Transport == SipTransportKind.Tls && info.Secure
                    && info.CertificateIdentity != null
                    && info.CertificateIdentity.ChainValid
                    && (!alias.RequireMutualTls
                        || info.CertificateIdentity.MutualTls)
                    && SipCertificateDomainIdentity.MatchesDomains(
                        info.CertificateIdentity.Domains, destinationDomain))
                {
                    return alias.Connection;
                }
            }
            index = index + 1;
        }
        return null;
    }

    internal void RemoveAliases(string? connectionId)
    {
        int index = aliases.Count - 1;
        while (index >= 0)
        {
            SipConnectionAlias alias = aliases[index];
            SipConnectionInfo? info = alias.Connection == null
                ? null : alias.Connection.GetConnectionInfo();
            if (info == null || info.Id == connectionId)
            {
                aliases.RemoveAt(index);
            }
            index = index - 1;
        }
    }

    internal int CountAliases()
    {
        return aliases.Count;
    }

    bool AddAlias(SipReliableConnectionCore connection,
        SipEndpointAddress destination, string[] identities,
        bool requireMutualTls)
    {
        int index = 0;
        while (index < aliases.Count)
        {
            SipConnectionAlias existing = aliases[index];
            if (existing.Connection == connection
                && EndpointsMatch(existing.Destination, destination))
            {
                existing.Identities = CopyIdentities(identities);
                existing.RequireMutualTls = requireMutualTls;
                return true;
            }
            index = index + 1;
        }
        if (aliases.Count >= maximumAliases)
        {
            return false;
        }
        SipConnectionAlias added = new SipConnectionAlias();
        added.Connection = connection;
        added.Destination = CopyEndpoint(destination);
        added.Identities = CopyIdentities(identities);
        added.RequireMutualTls = requireMutualTls;
        aliases.Add(added);
        return true;
    }

    static SipConnectionInfo? GetAuthenticatedConnection(
        SipReliableConnectionCore? connection, bool requireMutualTls)
    {
        if (connection == null)
        {
            return null;
        }
        SipConnectionInfo info = connection.GetConnectionInfo();
        return info.State == SipConnectionState.Connected
            && info.Transport == SipTransportKind.Tls && info.Secure
            && info.Id != null && info.Id.Length > 0
            && info.RemoteAddress != null && info.CertificateIdentity != null
            && info.CertificateIdentity.ChainValid
            && (!requireMutualTls || info.CertificateIdentity.MutualTls)
            && info.CertificateIdentity.Domains.Length > 0 ? info : null;
    }

    static bool HasAliasParameter(SipUriParameter[]? parameters)
    {
        if (parameters == null)
        {
            return false;
        }
        int index = 0;
        while (index < parameters.Length)
        {
            SipUriParameter? parameter = parameters[index];
            if (parameter != null && !parameter.HasValue
                && SipAscii.EqualsIgnoreCase(parameter.Name, "alias"))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static bool ValidDestination(SipEndpointAddress? endpoint)
    {
        return endpoint != null && endpoint.Host != null
            && endpoint.Port > 0 && endpoint.Port <= 65535
            && endpoint.Transport == SipTransportKind.Tls && endpoint.Secure
            && (SipUriSyntax.IsIpv4Address(endpoint.Host)
                || SipUriSyntax.IsIpv6Address(endpoint.Host));
    }

    static bool EndpointsMatch(SipEndpointAddress? left,
        SipEndpointAddress? right)
    {
        return left != null && right != null
            && left.Host != null && right.Host != null
            && SipAscii.EqualsIgnoreCase(left.Host, right.Host)
            && left.Port == right.Port && left.Transport == right.Transport
            && left.Secure == right.Secure;
    }

    static SipEndpointAddress CopyEndpoint(SipEndpointAddress source)
    {
        SipEndpointAddress result = new SipEndpointAddress();
        result.Host = source.Host;
        result.Port = source.Port;
        result.Transport = source.Transport;
        result.Secure = source.Secure;
        return result;
    }

    static string[] CopyIdentities(string[] source)
    {
        string[] result = new string[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            result[index] = source[index];
            index = index + 1;
        }
        return result;
    }
}
