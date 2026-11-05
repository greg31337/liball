namespace dns;

/// <summary>Receives and validates AXFR or IXFR zone transfers.</summary>
public interface IDnsTransferClient
{
    /// <summary>Receives one bounded full or incremental zone transfer.</summary>
    /// <param name="request">The zone, server, transfer kind, limits, and authentication.</param>
    /// <returns>The transferred zone or journal entries and protocol metadata.</returns>
    DnsTransferResult ReceiveTransfer(DnsTransferRequest request);
}
