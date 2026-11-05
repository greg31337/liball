using System.Text;

namespace dns.Tests.Internal.Resolver;

public class StubAliasOperationsTests
{
    [Test]
    public void ContinuesCnameAcrossQueriesAndReusesIntermediateCacheEntries()
    {
        ScriptedQueryClient client = new ScriptedQueryClient();
        client.Enqueue(query => AliasAnswer(
            query,
            DnsRecordType.Cname,
            Name(true, "target", "example"),
            40));
        client.Enqueue(query => PositiveAnswer(query, 90));
        DnsStubResolver resolver = Resolver(client);
        DnsResolveRequest request = Request(Name(true, "alias", "example"));

        DnsResolveResult first = resolver.Resolve(request);
        DnsResolveResult second = resolver.Resolve(request);

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(DnsResolveStatus.Success));
            Assert.That(first.Queries, Has.Count.EqualTo(2));
            Assert.That(first.AliasSteps, Has.Count.EqualTo(1));
            Assert.That(first.AliasSteps[0].Synthesized, Is.False);
            Assert.That(Text(first.CanonicalName!),
                Is.EqualTo("target.example."));
            Assert.That(first.TimeToLive, Is.EqualTo(40));
            Assert.That(client.Requests.Select(item => Text(item.Question.Name)),
                Is.EqualTo(new[]
                {
                    "alias.example.",
                    "target.example."
                }));
            Assert.That(second.Status, Is.EqualTo(DnsResolveStatus.Success));
            Assert.That(second.FromCache, Is.True);
            Assert.That(second.Queries, Is.Empty);
            Assert.That(second.AliasSteps, Has.Count.EqualTo(1));
            Assert.That(Text(second.CanonicalName!),
                Is.EqualTo("target.example."));
            Assert.That(client.Requests, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void SynthesizesDnameTargetBeforeContinuingTheQuery()
    {
        ScriptedQueryClient client = new ScriptedQueryClient();
        client.Enqueue(query => AliasAnswer(
            query,
            DnsRecordType.Dname,
            Name(true, "example", "net"),
            50,
            Name(true, "example")));
        client.Enqueue(query => PositiveAnswer(query, 30));
        DnsStubResolver resolver = Resolver(client);
        DnsResolveRequest request = Request(
            Name(true, "www", "sub", "example"));
        request.Options.UseCache = false;

        DnsResolveResult result = resolver.Resolve(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DnsResolveStatus.Success));
            Assert.That(result.AliasSteps, Has.Count.EqualTo(1));
            Assert.That(result.AliasSteps[0].RecordType,
                Is.EqualTo(DnsRecordType.Dname));
            Assert.That(result.AliasSteps[0].Synthesized, Is.True);
            Assert.That(Text(result.AliasSteps[0].Target),
                Is.EqualTo("www.sub.example.net."));
            Assert.That(Text(result.CanonicalName!),
                Is.EqualTo("www.sub.example.net."));
            Assert.That(Text(client.Requests[1].Question.Name),
                Is.EqualTo("www.sub.example.net."));
            Assert.That(result.TimeToLive, Is.EqualTo(30));
        });
    }

    [Test]
    public void AliasOnlyResponsePreservesAnExistingTargetAnswer()
    {
        ScriptedQueryClient client = new ScriptedQueryClient();
        client.Enqueue(query => PositiveAnswer(query, 120));
        client.Enqueue(query => AliasAnswer(
            query,
            DnsRecordType.Cname,
            Name(true, "target", "example"),
            45));
        DnsStubResolver resolver = Resolver(client);
        DnsResolveRequest target = Request(
            Name(true, "target", "example"));
        DnsResolveRequest alias = Request(
            Name(true, "alias", "example"));

        DnsResolveResult seeded = resolver.Resolve(target);
        DnsResolveResult result = resolver.Resolve(alias);

        Assert.Multiple(() =>
        {
            Assert.That(seeded.Status, Is.EqualTo(DnsResolveStatus.Success));
            Assert.That(result.Status, Is.EqualTo(DnsResolveStatus.Success));
            Assert.That(result.Queries, Has.Count.EqualTo(1));
            Assert.That(result.AliasSteps, Has.Count.EqualTo(1));
            Assert.That(result.RecordSets, Has.Count.EqualTo(1));
            Assert.That(Text(result.CanonicalName!),
                Is.EqualTo("target.example."));
            Assert.That(client.Requests, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void DetectsAliasLoopsAcrossSeparateResponses()
    {
        ScriptedQueryClient client = new ScriptedQueryClient();
        client.Enqueue(query => AliasAnswer(
            query,
            DnsRecordType.Cname,
            Name(true, "b", "example"),
            60));
        client.Enqueue(query => AliasAnswer(
            query,
            DnsRecordType.Cname,
            Name(true, "a", "example"),
            60));
        DnsStubResolver resolver = Resolver(client);
        DnsResolveRequest request = Request(Name(true, "a", "example"));
        request.Options.UseCache = false;

        DnsResolveResult result = resolver.Resolve(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DnsResolveStatus.Loop));
            Assert.That(result.Queries, Has.Count.EqualTo(2));
            Assert.That(result.AliasSteps, Has.Count.EqualTo(2));
            Assert.That(Text(result.CanonicalName!),
                Is.EqualTo("a.example."));
            Assert.That(result.Diagnostics.Any(item =>
                item.Field == "answerChain"), Is.True);
        });
    }

    [Test]
    public void EnforcesCumulativeDepthAcrossSeparateResponses()
    {
        ScriptedQueryClient client = new ScriptedQueryClient();
        client.Enqueue(query => AliasAnswer(
            query,
            DnsRecordType.Cname,
            Name(true, "b", "example"),
            60));
        client.Enqueue(query => AliasAnswer(
            query,
            DnsRecordType.Cname,
            Name(true, "c", "example"),
            60));
        DnsStubResolver resolver = Resolver(client);
        DnsResolveRequest request = Request(Name(true, "a", "example"));
        request.Options.UseCache = false;
        request.Options.MaximumAliasDepth = 1;

        DnsResolveResult result = resolver.Resolve(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(DnsResolveStatus.DepthLimitExceeded));
            Assert.That(result.Queries, Has.Count.EqualTo(2));
            Assert.That(result.AliasSteps, Has.Count.EqualTo(1));
            Assert.That(Text(result.CanonicalName!),
                Is.EqualTo("b.example."));
        });
    }

    [Test]
    public void FollowAliasesFalseStopsAtTheFirstReportedAlias()
    {
        ScriptedQueryClient client = new ScriptedQueryClient();
        client.Enqueue(query =>
        {
            DnsQueryResult result = AliasAnswer(
                query,
                DnsRecordType.Cname,
                Name(true, "b", "example"),
                25);
            result.Response!.Answers.Add(AddressRecord(
                Name(true, "b", "example"),
                80));
            return result;
        });
        DnsStubResolver resolver = Resolver(client);
        DnsResolveRequest request = Request(Name(true, "a", "example"));
        request.Options.UseCache = false;
        request.Options.FollowAliases = false;

        DnsResolveResult result = resolver.Resolve(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DnsResolveStatus.NoData));
            Assert.That(result.Queries, Has.Count.EqualTo(1));
            Assert.That(result.AliasSteps, Has.Count.EqualTo(1));
            Assert.That(result.RecordSets, Is.Empty);
            Assert.That(Text(result.CanonicalName!),
                Is.EqualTo("b.example."));
            Assert.That(result.TimeToLive, Is.EqualTo(25));
        });
    }

    static DnsQueryResult AliasAnswer(
        DnsQueryRequest query,
        DnsRecordType recordType,
        DnsName target,
        uint timeToLive,
        DnsName? owner = null)
    {
        DnsResourceRecord record = new DnsResourceRecord();
        record.Name = owner ?? query.Question.Name;
        record.RecordType = recordType;
        record.RecordTypeValue = (int)recordType;
        record.Class = DnsClass.Internet;
        record.ClassValue = 1;
        record.TimeToLive = timeToLive;
        record.Data = new DnsRecordData();
        record.Data.Name = new DnsNameData();
        record.Data.Name.Name = target;
        DnsMessage response = Response(query);
        response.Answers.Add(record);
        DnsQueryResult result = new DnsQueryResult();
        result.Success = true;
        result.Status = DnsTransportStatus.Success;
        result.Response = response;
        return result;
    }

    static DnsQueryResult PositiveAnswer(
        DnsQueryRequest query,
        uint timeToLive)
    {
        DnsMessage response = Response(query);
        response.Answers.Add(AddressRecord(query.Question.Name, timeToLive));
        DnsQueryResult result = new DnsQueryResult();
        result.Success = true;
        result.Status = DnsTransportStatus.Success;
        result.Response = response;
        return result;
    }

    static DnsResourceRecord AddressRecord(
        DnsName owner,
        uint timeToLive)
    {
        DnsResourceRecord result = new DnsResourceRecord();
        result.Name = owner;
        result.RecordType = DnsRecordType.A;
        result.RecordTypeValue = 1;
        result.Class = DnsClass.Internet;
        result.ClassValue = 1;
        result.TimeToLive = timeToLive;
        result.Data = new DnsRecordData();
        result.Data.Address = new DnsAddressData();
        result.Data.Address.Address = [192, 0, 2, 44];
        return result;
    }

    static DnsMessage Response(DnsQueryRequest query)
    {
        DnsMessage result = new DnsMessage();
        result.Header.Flags = DnsHeaderFlag.Response |
            DnsHeaderFlag.RecursionAvailable;
        result.Header.FlagsValue = (int)result.Header.Flags;
        result.Header.ResponseCode = DnsResponseCode.NoError;
        result.Questions.Add(query.Question);
        return result;
    }

    static DnsStubResolver Resolver(ScriptedQueryClient client)
    {
        DnsStubResolver result = new DnsStubResolver();
        result.QueryClient = client;
        result.Cache = new MemoryDnsCache();
        return result;
    }

    static DnsResolveRequest Request(DnsName name)
    {
        DnsResolveRequest result = new DnsResolveRequest();
        result.Name = name;
        result.RecordType = DnsRecordType.A;
        result.RecordTypeValue = 1;
        result.Class = DnsClass.Internet;
        result.ClassValue = 1;
        DnsResolverServer server = new DnsResolverServer();
        server.Endpoint.HasAddress = true;
        server.Endpoint.AddressFamily = 4;
        server.Endpoint.Address = [192, 0, 2, 1];
        server.Endpoint.Port = 53;
        result.Options.Configuration.Servers.Add(server);
        return result;
    }

    static DnsName Name(bool absolute, params string[] labels)
    {
        DnsName result = new DnsName();
        result.IsAbsolute = absolute;
        int index = 0;
        while (index < labels.Length)
        {
            result.Labels.Add(Encoding.ASCII.GetBytes(labels[index]));
            index = index + 1;
        }

        return result;
    }

    static string Text(DnsName name)
    {
        string result = string.Empty;
        int index = 0;
        while (index < name.Labels.Count)
        {
            if (index > 0)
            {
                result = result + ".";
            }

            result = result + Encoding.ASCII.GetString(name.Labels[index]);
            index = index + 1;
        }

        if (name.IsAbsolute)
        {
            result = result + ".";
        }

        return result;
    }

    class ScriptedQueryClient : IDnsQueryClient
    {
        readonly Queue<Func<DnsQueryRequest, DnsQueryResult>> responses = new();

        internal List<DnsQueryRequest> Requests = new();

        internal void Enqueue(Func<DnsQueryRequest, DnsQueryResult> response)
        {
            responses.Enqueue(response);
        }

        public DnsQueryResult Query(DnsQueryRequest request)
        {
            Requests.Add(request);
            return responses.Dequeue()(request);
        }
    }
}
