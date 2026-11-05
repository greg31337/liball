namespace dns;

/// <summary>Receives bounded, authenticated full zone transfers over TCP.</summary>
public class DnsTransferClient : IDnsTransferClient
{
    internal IAxfrExchangeProvider ExchangeProvider;

    /// <summary>Creates a zone-transfer client using DNS over TCP.</summary>
    public DnsTransferClient()
    {
        this.ExchangeProvider = new TcpAxfrExchangeProvider();
    }

    /// <summary>Receives and atomically validates one full zone transfer.</summary>
    /// <param name="request">The zone, server, limits, and optional TSIG key.</param>
    /// <returns>The complete zone and transfer metadata, or diagnostics.</returns>
    public DnsTransferResult ReceiveTransfer(DnsTransferRequest request)
    {
        return TransferClientOperations.ReceiveTransfer(request, this);
    }
}
