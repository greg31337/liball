namespace dns;

/// <summary>Provides common DNS lookup operations with resolver metadata.</summary>
public class DnsLookupService : IDnsLookupService
{
    internal IDnsResolver Resolver = new DnsStubResolver();
    internal IDnsLookupEntropy Entropy = new DnsLookupEntropy();

    /// <summary>Creates a DNS lookup service.</summary>
    public DnsLookupService()
    {
    }

    /// <summary>Resolves IPv4 and IPv6 addresses.</summary>
    /// <param name="request">The name and resolver policy.</param>
    /// <returns>The addresses and their DNS metadata.</returns>
    public DnsAddressResult LookupAddresses(DnsResolveRequest request)
    {
        return AddressLookupOperations.LookupAddresses(request, this);
    }

    /// <summary>Resolves mail exchangers.</summary>
    /// <param name="request">The mail domain and resolver policy.</param>
    /// <returns>The mail exchangers and their DNS metadata.</returns>
    public DnsMailResult LookupMail(DnsResolveRequest request)
    {
        return LookupRecordOperations.LookupMail(request, this);
    }

    /// <summary>Resolves a reverse-map pointer record.</summary>
    /// <param name="request">The reverse-map name and resolver policy.</param>
    /// <returns>The pointer answer, aliases, and DNS metadata.</returns>
    public DnsResolveResult LookupPointer(DnsResolveRequest request)
    {
        return LookupRecordOperations.LookupPointer(request, this);
    }

    /// <summary>Resolves character-string values from TXT records.</summary>
    /// <param name="request">The text-record name and resolver policy.</param>
    /// <returns>The character strings and their DNS metadata.</returns>
    public DnsServiceResult LookupText(DnsResolveRequest request)
    {
        return LookupRecordOperations.LookupText(request, this);
    }

    /// <summary>Resolves SRV targets, ports, priorities, weights, and addresses.</summary>
    /// <param name="request">The service name and resolver policy.</param>
    /// <returns>The ordered service endpoints and DNS metadata.</returns>
    public DnsServiceResult LookupServiceRecords(DnsResolveRequest request)
    {
        return ServiceRecordLookupOperations.LookupServiceRecords(
            request,
            this);
    }

    /// <summary>Resolves SVCB or HTTPS service bindings and target addresses.</summary>
    /// <param name="request">The binding name and resolver policy.</param>
    /// <returns>The validated service endpoints and DNS metadata.</returns>
    public DnsServiceResult LookupServiceBindings(DnsResolveRequest request)
    {
        return ServiceBindingLookupOperations.LookupServiceBindings(
            request,
            this);
    }

    /// <summary>Resolves service endpoints or text values.</summary>
    /// <param name="request">The service or text name and resolver policy.</param>
    /// <returns>The endpoints or text values and their DNS metadata.</returns>
    public DnsServiceResult LookupServices(DnsResolveRequest request)
    {
        return LookupRecordOperations.LookupServices(request, this);
    }
}
