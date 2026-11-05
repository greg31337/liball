namespace dns;

/// <summary>Provides common address, mail, and service DNS lookups.</summary>
public interface IDnsLookupService
{
    /// <summary>Resolves IPv4 and IPv6 addresses.</summary>
    /// <param name="request">The name and resolver policy.</param>
    /// <returns>The addresses and their DNS metadata.</returns>
    DnsAddressResult LookupAddresses(DnsResolveRequest request);

    /// <summary>Resolves mail exchangers.</summary>
    /// <param name="request">The mail domain and resolver policy.</param>
    /// <returns>The mail exchangers and their DNS metadata.</returns>
    DnsMailResult LookupMail(DnsResolveRequest request);

    /// <summary>Resolves a reverse-map pointer record.</summary>
    /// <param name="request">The reverse-map name and resolver policy.</param>
    /// <returns>The pointer answer, aliases, and DNS metadata.</returns>
    DnsResolveResult LookupPointer(DnsResolveRequest request);

    /// <summary>Resolves character-string values from TXT records.</summary>
    /// <param name="request">The text-record name and resolver policy.</param>
    /// <returns>The character strings and their DNS metadata.</returns>
    DnsServiceResult LookupText(DnsResolveRequest request);

    /// <summary>Resolves SRV targets, ports, priorities, weights, and addresses.</summary>
    /// <param name="request">The service name and resolver policy.</param>
    /// <returns>The ordered service endpoints and DNS metadata.</returns>
    DnsServiceResult LookupServiceRecords(DnsResolveRequest request);

    /// <summary>Resolves SVCB or HTTPS service bindings and target addresses.</summary>
    /// <param name="request">The binding name and resolver policy.</param>
    /// <returns>The validated service endpoints and DNS metadata.</returns>
    DnsServiceResult LookupServiceBindings(DnsResolveRequest request);

    /// <summary>Resolves service endpoints or text values.</summary>
    /// <param name="request">The service or text name and resolver policy.</param>
    /// <returns>The endpoints or text values and their DNS metadata.</returns>
    DnsServiceResult LookupServices(DnsResolveRequest request);
}
