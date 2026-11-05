using whois;

namespace whois.Tests;

internal class FakeServerSelector : IWhoisServerSelector
{
    public WhoisServerSelection Result = new WhoisServerSelection();
    public Func<WhoisQuery, WhoisLookupRequest, WhoisServerSelection>? SelectHandler = null;
    public List<WhoisQuery> Queries = new List<WhoisQuery>();
    public List<WhoisLookupRequest> Requests = new List<WhoisLookupRequest>();

    public WhoisServerSelection SelectServer(WhoisQuery query, WhoisLookupRequest request)
    {
        Queries.Add(query);
        Requests.Add(request);
        if (SelectHandler != null)
        {
            return SelectHandler(query, request);
        }
        return Result;
    }
}
