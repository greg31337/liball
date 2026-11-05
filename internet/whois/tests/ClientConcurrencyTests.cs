using whois;

namespace whois.Tests;

public class ClientConcurrencyTests
{
    [Test]
    public async Task ConcurrentLookupsKeepPerCallTraversalOptionsAndResponseGraphsIsolated()
    {
        int lookupCount = 30;
        ConcurrentSelector selector = new ConcurrentSelector();
        ConcurrentFunctionalTransport transport = new ConcurrentFunctionalTransport();
        WhoisClient client = CreateClient(selector, transport);
        WhoisLookupRequest[] requests = new WhoisLookupRequest[lookupCount];
        Task<WhoisLookupResult>[] tasks = new Task<WhoisLookupResult>[lookupCount];

        int index = 0;
        while (index < lookupCount)
        {
            WhoisLookupRequest request = new WhoisLookupRequest();
            request.Text = "name" + index + ".example";
            int mode = index % 3;
            if (mode == 1)
            {
                request.Options.FollowReferrals = false;
            }
            if (mode == 2)
            {
                request.Options.RetainDiscoveryResponses = false;
            }
            requests[index] = request;
            bool useThrowingApi = index % 2 == 0;
            tasks[index] = Task.Run(() =>
            {
                if (useThrowingApi)
                {
                    return client.Lookup(request);
                }
                return client.TryLookup(request);
            });
            index = index + 1;
        }

        WhoisLookupResult[] results = await Task.WhenAll(tasks);

        Assert.That(selector.Calls, Is.EqualTo(lookupCount));
        Assert.That(transport.Requests.Count, Is.EqualTo(50));
        index = 0;
        while (index < lookupCount)
        {
            WhoisLookupResult result = results[index];
            string normalizedQuery = "name" + index + ".example";
            string registryHost = "registry-name" + index + "-example.test";
            int mode = index % 3;
            Assert.That(result.IsComplete, Is.True);
            Assert.That(result.FailedStage, Is.EqualTo(WhoisOperationStage.None));
            Assert.That(result.Error, Is.Null);
            Assert.That(result.Query.OriginalText, Is.EqualTo(normalizedQuery));
            Assert.That(result.Query.NormalizedText, Is.EqualTo(normalizedQuery));
            Assert.That(result.Query.Kind, Is.EqualTo(WhoisQueryKind.Domain));

            if (mode == 0)
            {
                Assert.That(result.Hops, Has.Length.EqualTo(2));
                AssertDiscoveryHop(result.Hops[0], normalizedQuery, registryHost, true);
                AssertRegistryHop(result.Hops[1], normalizedQuery, registryHost);
            }
            else if (mode == 1)
            {
                Assert.That(result.Hops, Has.Length.EqualTo(1));
                AssertDiscoveryHop(result.Hops[0], normalizedQuery, registryHost, false);
            }
            else
            {
                Assert.That(result.Hops, Has.Length.EqualTo(1));
                AssertRegistryHop(result.Hops[0], normalizedQuery, registryHost);
            }

            Assert.That(requests[index].Text, Is.EqualTo(normalizedQuery));
            Assert.That(requests[index].Options.FollowReferrals, Is.EqualTo(mode != 1));
            Assert.That(requests[index].Options.RetainDiscoveryResponses, Is.EqualTo(mode != 2));
            index = index + 1;
        }

        int left = 0;
        while (left < results.Length)
        {
            int right = left + 1;
            while (right < results.Length)
            {
                Assert.That(results[left], Is.Not.SameAs(results[right]));
                Assert.That(results[left].Query, Is.Not.SameAs(results[right].Query));
                Assert.That(results[left].Hops, Is.Not.SameAs(results[right].Hops));
                Assert.That(results[left].Hops[0], Is.Not.SameAs(results[right].Hops[0]));
                Assert.That(results[left].Hops[0].RawResponse, Is.Not.SameAs(results[right].Hops[0].RawResponse));
                Assert.That(results[left].Hops[0].TextResponse, Is.Not.SameAs(results[right].Hops[0].TextResponse));
                Assert.That(results[left].Hops[0].ParsedResponse, Is.Not.SameAs(results[right].Hops[0].ParsedResponse));
                right = right + 1;
            }
            left = left + 1;
        }
    }

    [Test]
    public async Task ConcurrentDirectOperationsReturnIndependentDeterministicResponses()
    {
        int queryCount = 24;
        ConcurrentSelector selector = new ConcurrentSelector();
        ConcurrentFunctionalTransport transport = new ConcurrentFunctionalTransport();
        WhoisClient client = CreateClient(selector, transport);
        WhoisRequest[] requests = new WhoisRequest[queryCount];
        Task<WhoisRawResponse>[] tasks = new Task<WhoisRawResponse>[queryCount];

        int index = 0;
        while (index < queryCount)
        {
            WhoisRequest request = new WhoisRequest();
            request.Endpoint.Host = "whois.direct" + index + ".example";
            request.Endpoint.Port = 1043 + index;
            request.QueryText = "direct query " + index;
            requests[index] = request;
            bool useThrowingApi = index % 2 == 0;
            tasks[index] = Task.Run(() =>
            {
                if (useThrowingApi)
                {
                    return client.Query(request);
                }
                return client.TryQuery(request);
            });
            index = index + 1;
        }

        WhoisRawResponse[] responses = await Task.WhenAll(tasks);

        Assert.That(selector.Calls, Is.Zero);
        Assert.That(transport.Requests.Count, Is.EqualTo(queryCount));
        index = 0;
        while (index < queryCount)
        {
            WhoisRawResponse response = responses[index];
            Assert.That(response.Status, Is.EqualTo(WhoisExchangeStatus.Complete));
            Assert.That(response.Request, Is.Not.SameAs(requests[index]));
            Assert.That(response.Request.Source, Is.EqualTo(WhoisEndpointSource.Direct));
            Assert.That(response.Request.Endpoint.Host, Is.EqualTo("whois.direct" + index + ".example"));
            Assert.That(response.Request.Endpoint.Port, Is.EqualTo(1043 + index));
            Assert.That(response.Request.QueryText, Is.EqualTo("direct query " + index));
            Assert.That(response.Body, Is.EqualTo(System.Text.Encoding.UTF8.GetBytes("direct response: direct query " + index)));
            index = index + 1;
        }

        int left = 0;
        while (left < responses.Length)
        {
            int right = left + 1;
            while (right < responses.Length)
            {
                Assert.That(responses[left], Is.Not.SameAs(responses[right]));
                Assert.That(responses[left].Request, Is.Not.SameAs(responses[right].Request));
                Assert.That(responses[left].Body, Is.Not.SameAs(responses[right].Body));
                right = right + 1;
            }
            left = left + 1;
        }
    }

    private static void AssertDiscoveryHop(WhoisLookupHop hop, string query, string registryHost, bool selected)
    {
        string expectedText = "refer: " + registryHost + "\r\n";
        Assert.That(hop.Selection.Endpoint.Host, Is.EqualTo(WhoisDefaults.IanaHost));
        Assert.That(hop.Selection.Source, Is.EqualTo(WhoisEndpointSource.InitialSelection));
        Assert.That(hop.RawResponse.Request.QueryText, Is.EqualTo(query));
        Assert.That(hop.RawResponse.Body, Is.EqualTo(System.Text.Encoding.UTF8.GetBytes(expectedText)));
        Assert.That(hop.TextResponse.RawResponse, Is.SameAs(hop.RawResponse));
        Assert.That(hop.TextResponse.Text, Is.EqualTo(expectedText));
        Assert.That(hop.ParsedResponse.TextResponse, Is.SameAs(hop.TextResponse));
        Assert.That(hop.Referrals, Has.Length.EqualTo(1));
        Assert.That(hop.Referrals[0].Endpoint!.Host, Is.EqualTo(registryHost));
        if (selected)
        {
            Assert.That(hop.SelectedReferral, Is.SameAs(hop.Referrals[0]));
        }
        else
        {
            Assert.That(hop.SelectedReferral, Is.Null);
        }
    }

    private static void AssertRegistryHop(WhoisLookupHop hop, string query, string registryHost)
    {
        string expectedText = "domain: " + query + "\r\nsource: " + registryHost + "\r\n";
        Assert.That(hop.Selection.Endpoint.Host, Is.EqualTo(registryHost));
        Assert.That(hop.Selection.Source, Is.EqualTo(WhoisEndpointSource.Referral));
        Assert.That(hop.RawResponse.Request.QueryText, Is.EqualTo(query));
        Assert.That(hop.RawResponse.Body, Is.EqualTo(System.Text.Encoding.UTF8.GetBytes(expectedText)));
        Assert.That(hop.TextResponse.RawResponse, Is.SameAs(hop.RawResponse));
        Assert.That(hop.TextResponse.Text, Is.EqualTo(expectedText));
        Assert.That(hop.ParsedResponse.TextResponse, Is.SameAs(hop.TextResponse));
        Assert.That(hop.ParsedResponse.Fields, Has.Length.EqualTo(2));
        Assert.That(hop.Referrals, Is.Empty);
        Assert.That(hop.SelectedReferral, Is.Null);
    }

    private static WhoisClient CreateClient(IWhoisServerSelector selector, IWhoisTransport transport)
    {
        return new WhoisClient(
            new WhoisQueryClassifier(),
            new WhoisQueryFormatter(),
            selector,
            transport,
            new WhoisResponseDecoder(),
            new WhoisResponseParser(),
            new WhoisReferralParser());
    }

    private class ConcurrentSelector : IWhoisServerSelector
    {
        public int Calls;

        public WhoisServerSelection SelectServer(WhoisQuery query, WhoisLookupRequest request)
        {
            System.Threading.Interlocked.Increment(ref Calls);
            WhoisServerSelection selection = new WhoisServerSelection();
            selection.Endpoint.Host = WhoisDefaults.IanaHost;
            selection.Endpoint.Port = WhoisDefaults.WhoisPort;
            selection.Source = WhoisEndpointSource.InitialSelection;
            selection.Reason = "Concurrent IANA discovery.";
            return selection;
        }
    }

    private class ConcurrentFunctionalTransport : IWhoisTransport
    {
        public System.Collections.Concurrent.ConcurrentQueue<WhoisRequest> Requests = new System.Collections.Concurrent.ConcurrentQueue<WhoisRequest>();

        public WhoisRawResponse Exchange(WhoisRequest request)
        {
            Requests.Enqueue(request);
            WhoisRawResponse response = new WhoisRawResponse();
            response.Request = request;
            response.Status = WhoisExchangeStatus.Complete;
            if (request.Source == WhoisEndpointSource.Direct)
            {
                response.Body = System.Text.Encoding.UTF8.GetBytes("direct response: " + request.QueryText);
                return response;
            }
            if (request.Endpoint.Host == WhoisDefaults.IanaHost)
            {
                string registryHost = "registry-" + request.QueryText.Replace('.', '-') + ".test";
                response.Body = System.Text.Encoding.UTF8.GetBytes("refer: " + registryHost + "\r\n");
                return response;
            }
            response.Body = System.Text.Encoding.UTF8.GetBytes(
                "domain: " + request.QueryText + "\r\n" +
                "source: " + request.Endpoint.Host + "\r\n");
            return response;
        }
    }
}
