using whois;

namespace whois.Tests;

internal class FakeTransport : IWhoisTransport
{
    public WhoisRawResponse Result = new WhoisRawResponse();
    public Func<WhoisRequest, WhoisRawResponse>? ExchangeHandler = null;
    public List<WhoisRequest> Requests = new List<WhoisRequest>();

    public WhoisRawResponse Exchange(WhoisRequest request)
    {
        Requests.Add(request);
        if (ExchangeHandler != null)
        {
            return ExchangeHandler(request);
        }
        return Result;
    }
}
