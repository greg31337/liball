using whois;

namespace whois.Tests;

internal class FakeConnectionFactory : IWhoisConnectionFactory
{
    public Stream? Stream = null;
    public WhoisExchangeStatus Status = WhoisExchangeStatus.Complete;
    public TimeSpan Duration = TimeSpan.Zero;
    public Exception? Error = null;
    public Func<WhoisEndpoint, System.Net.IPAddress, WhoisTimeoutOptions, CancellationToken, WhoisConnection>? OpenHandler = null;
    public List<WhoisEndpoint> Endpoints = new List<WhoisEndpoint>();
    public List<System.Net.IPAddress> ApprovedAddresses = new List<System.Net.IPAddress>();
    public List<WhoisTimeoutOptions> TimeoutOptions = new List<WhoisTimeoutOptions>();
    public List<CancellationToken> CancellationTokens = new List<CancellationToken>();

    public WhoisConnection OpenConnection(WhoisEndpoint endpoint, System.Net.IPAddress approvedAddress, WhoisTimeoutOptions timeouts, CancellationToken cancellationToken)
    {
        Endpoints.Add(endpoint);
        ApprovedAddresses.Add(approvedAddress);
        TimeoutOptions.Add(timeouts);
        CancellationTokens.Add(cancellationToken);
        if (OpenHandler != null)
        {
            return OpenHandler(endpoint, approvedAddress, timeouts, cancellationToken);
        }

        WhoisConnection result = new WhoisConnection();
        result.ApprovedAddress = approvedAddress;
        result.Stream = Stream;
        result.Status = Status;
        result.Duration = Duration;
        result.Error = Error;
        return result;
    }
}
