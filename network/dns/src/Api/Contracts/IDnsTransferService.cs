namespace dns;

/// <summary>Produces authenticated AXFR or IXFR transfer data.</summary>
public interface IDnsTransferService
{
    /// <summary>Serves one authorized full or incremental transfer request.</summary>
    /// <param name="request">The requested zone, transfer kind, client context, and limits.</param>
    /// <returns>The ordered transfer messages and authentication state.</returns>
    DnsTransferResult ServeTransfer(DnsTransferRequest request);
}
