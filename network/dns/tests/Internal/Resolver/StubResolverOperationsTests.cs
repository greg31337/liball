using System.Text;

namespace dns.Tests.Internal.Resolver;

public class StubResolverOperationsTests
{
    [Test]
    public void PropagatesConfigurationFlagsServersAndRetryBudget()
    {
        ScriptedQueryClient client = new ScriptedQueryClient();
        client.Enqueue(query => PositiveAnswer(query, 90));
        DnsStubResolver resolver = Resolver(client);
        DnsResolveRequest request = Request(Name(true, "www", "example"));
        request.Options.UseCache = false;
        request.Options.RequestDnssec = true;
        request.Options.CheckingDisabled = true;
        request.Options.TotalTimeoutMilliseconds = 10000;
        request.Options.Configuration.Attempts = 3;
        request.Options.Configuration.TimeoutMilliseconds = 1250;
        request.Options.Configuration.UseEdns = false;
        request.Options.Configuration.UseTcp = true;
        request.Options.Configuration.Servers.Clear();
        request.Options.Configuration.Servers.Add(Server(1053, 20));
        request.Options.Configuration.Servers.Add(Server(2053, 10));

        DnsResolveResult result = resolver.Resolve(request);

        DnsQueryRequest sent = client.Requests.Single();
        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DnsResolveStatus.Success));
            Assert.That(result.Queries, Has.Count.EqualTo(1));
            Assert.That(sent.Servers.Select(server => server.Port),
                Is.EqualTo(new[] { 2053, 1053 }));
            Assert.That(sent.TransportOptions.Kind,
                Is.EqualTo(DnsTransportKind.Tcp));
            Assert.That(sent.TransportOptions.AttemptTimeoutMilliseconds,
                Is.EqualTo(1250));
            Assert.That(sent.TransportOptions.TotalTimeoutMilliseconds,
                Is.InRange(1, 10000));
            Assert.That(sent.TransportOptions.MaximumAttempts, Is.EqualTo(6));
            Assert.That(sent.RecursionDesired, Is.True);
            Assert.That(sent.UseEdns, Is.False);
            Assert.That(sent.RequestDnssec, Is.True);
            Assert.That(sent.CheckingDisabled, Is.True);
            Assert.That(sent.AllowTcpFallback, Is.False);
            Assert.That(Text(sent.Question.Name), Is.EqualTo("www.example."));
        });
    }

    [Test]
    public void RetryBudgetUsesEnabledServersAndPreservesEqualPriorityOrder()
    {
        ScriptedQueryClient client = new ScriptedQueryClient();
        client.Enqueue(query => PositiveAnswer(query, 60));
        DnsStubResolver resolver = Resolver(client);
        DnsResolveRequest request = Request(Name(true, "retry", "example"));
        request.Options.UseCache = false;
        request.Options.Configuration.Attempts = 2;
        request.Options.Configuration.Servers.Clear();
        DnsResolverServer disabled = Server(1053, 0);
        disabled.Enabled = false;
        request.Options.Configuration.Servers.Add(disabled);
        request.Options.Configuration.Servers.Add(Server(2053, 5));
        request.Options.Configuration.Servers.Add(Server(3053, 5));

        DnsResolveResult result = resolver.Resolve(request);

        DnsQueryRequest sent = client.Requests.Single();
        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DnsResolveStatus.Success));
            Assert.That(sent.Servers.Select(server => server.Port),
                Is.EqualTo(new[] { 2053, 3053 }));
            Assert.That(sent.TransportOptions.MaximumAttempts, Is.EqualTo(4));
        });
    }

    [Test]
    public void StoresPositiveAnswersAndReusesThemFromCache()
    {
        ScriptedQueryClient client = new ScriptedQueryClient();
        client.Enqueue(query => PositiveAnswer(query, 60));
        DnsStubResolver resolver = Resolver(client);
        DnsResolveRequest request = Request(Name(true, "cached", "example"));

        DnsResolveResult first = resolver.Resolve(request);
        DnsResolveResult second = resolver.Resolve(request);

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(DnsResolveStatus.Success));
            Assert.That(first.FromCache, Is.False);
            Assert.That(second.Status, Is.EqualTo(DnsResolveStatus.Success));
            Assert.That(second.FromCache, Is.True);
            Assert.That(second.Stale, Is.False);
            Assert.That(second.RecordSets, Has.Count.EqualTo(1));
            Assert.That(second.HasTimeToLive, Is.True);
            Assert.That(second.TimeToLive, Is.InRange(1U, 60U));
            Assert.That(second.Queries, Is.Empty);
            Assert.That(client.Requests, Has.Count.EqualTo(1));
        });
    }

    [TestCase(DnsResponseCode.NoError, DnsResolveStatus.NoData)]
    [TestCase(DnsResponseCode.NameError, DnsResolveStatus.NameError)]
    public void StoresAndReusesNegativeAnswersWithSoaProof(
        DnsResponseCode responseCode,
        DnsResolveStatus expectedStatus)
    {
        ScriptedQueryClient client = new ScriptedQueryClient();
        client.Enqueue(query => NegativeAnswerWithSoa(query, responseCode));
        DnsStubResolver resolver = Resolver(client);
        DnsResolveRequest request = Request(
            Name(true, "negative", "example"));

        DnsResolveResult first = resolver.Resolve(request);
        DnsResolveResult second = resolver.Resolve(request);

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(expectedStatus));
            Assert.That(first.FromCache, Is.False);
            Assert.That(first.Response, Is.Not.Null);
            Assert.That(second.Status, Is.EqualTo(expectedStatus));
            Assert.That(second.FromCache, Is.True);
            Assert.That(second.HasTimeToLive, Is.True);
            Assert.That(second.TimeToLive, Is.InRange(1U, 30U));
            Assert.That(second.Queries, Is.Empty);
            Assert.That(client.Requests, Has.Count.EqualTo(1));
        });
    }

    [TestCase(DnsResponseCode.NoError, DnsResolveStatus.NoData)]
    [TestCase(DnsResponseCode.NameError, DnsResolveStatus.NameError)]
    [TestCase(DnsResponseCode.ServerFailure, DnsResolveStatus.ServerFailure)]
    [TestCase(DnsResponseCode.Refused, DnsResolveStatus.Refused)]
    [TestCase(DnsResponseCode.FormatError, DnsResolveStatus.ProtocolError)]
    [TestCase(DnsResponseCode.NotImplemented, DnsResolveStatus.Unsupported)]
    public void MapsTerminalDnsResponseCodes(
        DnsResponseCode responseCode,
        DnsResolveStatus expectedStatus)
    {
        ScriptedQueryClient client = new ScriptedQueryClient();
        client.Enqueue(query => ResponseResult(query, responseCode));
        DnsStubResolver resolver = Resolver(client);
        DnsResolveRequest request = Request(Name(true, "rcode", "example"));
        request.Options.UseCache = false;

        DnsResolveResult result = resolver.Resolve(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(expectedStatus));
            Assert.That(result.Response, Is.Not.Null);
            Assert.That(result.Queries, Has.Count.EqualTo(1));
        });
    }

    [TestCase(DnsTransportStatus.TimedOut, DnsResolveStatus.TimedOut)]
    [TestCase(DnsTransportStatus.Cancelled, DnsResolveStatus.Cancelled)]
    [TestCase(DnsTransportStatus.NetworkError, DnsResolveStatus.ServerFailure)]
    [TestCase(DnsTransportStatus.AuthenticationFailed, DnsResolveStatus.ServerFailure)]
    [TestCase(DnsTransportStatus.Closed, DnsResolveStatus.ServerFailure)]
    [TestCase(DnsTransportStatus.ProtocolError, DnsResolveStatus.ProtocolError)]
    [TestCase(DnsTransportStatus.ResponseTooLarge, DnsResolveStatus.ProtocolError)]
    [TestCase(DnsTransportStatus.Unsupported, DnsResolveStatus.Unsupported)]
    public void MapsTerminalTransportOutcomes(
        DnsTransportStatus transportStatus,
        DnsResolveStatus expectedStatus)
    {
        ScriptedQueryClient client = new ScriptedQueryClient();
        client.Enqueue(query => TransportFailure(transportStatus));
        DnsStubResolver resolver = Resolver(client);
        DnsResolveRequest request = Request(
            Name(true, "transport", "example"));
        request.Options.UseCache = false;

        DnsResolveResult result = resolver.Resolve(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(expectedStatus));
            Assert.That(result.Response, Is.Null);
            Assert.That(result.Queries, Has.Count.EqualTo(1));
            Assert.That(result.Queries[0].Status, Is.EqualTo(transportStatus));
        });
    }

    [Test]
    public void TrustAuthenticDataControlsReportedSecurityStatus()
    {
        ScriptedQueryClient client = new ScriptedQueryClient();
        client.Enqueue(query => AuthenticPositiveAnswer(query));
        client.Enqueue(query => AuthenticPositiveAnswer(query));
        DnsStubResolver resolver = Resolver(client);
        DnsResolveRequest untrustedRequest = Request(
            Name(true, "untrusted-ad", "example"));
        untrustedRequest.Options.UseCache = false;
        DnsResolveRequest trustedRequest = Request(
            Name(true, "trusted-ad", "example"));
        trustedRequest.Options.UseCache = false;
        trustedRequest.Options.Configuration.TrustAuthenticData = true;

        DnsResolveResult untrusted = resolver.Resolve(untrustedRequest);
        DnsResolveResult trusted = resolver.Resolve(trustedRequest);

        Assert.Multiple(() =>
        {
            Assert.That(untrusted.Status, Is.EqualTo(DnsResolveStatus.Success));
            Assert.That(untrusted.SecurityStatus,
                Is.EqualTo(DnsSecurityStatus.Indeterminate));
            Assert.That(trusted.Status, Is.EqualTo(DnsResolveStatus.Success));
            Assert.That(trusted.SecurityStatus,
                Is.EqualTo(DnsSecurityStatus.Secure));
            Assert.That(client.Requests, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void ContinuesSearchAfterNameErrorAndReturnsLaterAnswer()
    {
        ScriptedQueryClient client = new ScriptedQueryClient();
        client.Enqueue(NegativeNameAnswer);
        client.Enqueue(query => PositiveAnswer(query, 30));
        DnsStubResolver resolver = Resolver(client);
        DnsResolveRequest request = Request(Name(false, "printer"));
        request.Options.UseCache = false;
        request.Options.Configuration.SearchDomains.Add(
            Name(true, "office", "example"));

        DnsResolveResult result = resolver.Resolve(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DnsResolveStatus.Success));
            Assert.That(result.Queries, Has.Count.EqualTo(2));
            Assert.That(client.Requests.Select(item => Text(item.Question.Name)),
                Is.EqualTo(new[]
                {
                    "printer.office.example.",
                    "printer."
                }));
            Assert.That(Text(result.CanonicalName!), Is.EqualTo("printer."));
        });
    }

    [Test]
    public void RejectsSuccessfulQueryResultWithMismatchedQuestion()
    {
        ScriptedQueryClient client = new ScriptedQueryClient();
        client.Enqueue(query =>
        {
            DnsQueryResult result = PositiveAnswer(query, 45);
            DnsQuestion mismatched = new DnsQuestion();
            mismatched.Name = Name(true, "different", "example");
            mismatched.RecordType = DnsRecordType.A;
            mismatched.RecordTypeValue = 1;
            mismatched.Class = DnsClass.Internet;
            mismatched.ClassValue = 1;
            result.Response!.Questions[0] = mismatched;
            return result;
        });
        DnsStubResolver resolver = Resolver(client);
        DnsResolveRequest request = Request(Name(true, "www", "example"));
        request.Options.UseCache = false;

        DnsResolveResult result = resolver.Resolve(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(DnsResolveStatus.ProtocolError));
            Assert.That(result.Response, Is.Not.Null);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.ResponseMismatch));
            Assert.That(result.Diagnostics[0].Field,
                Is.EqualTo("response.questions"));
        });
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
        result.Options.Configuration.Servers.Add(Server(53, 0));
        return result;
    }

    static DnsResolverServer Server(int port, int priority)
    {
        DnsResolverServer result = new DnsResolverServer();
        result.Endpoint.HasAddress = true;
        result.Endpoint.AddressFamily = 4;
        result.Endpoint.Address = [192, 0, 2, 1];
        result.Endpoint.Port = port;
        result.Transport = DnsTransportKind.Udp;
        result.Priority = priority;
        return result;
    }

    static DnsQueryResult PositiveAnswer(
        DnsQueryRequest query,
        uint timeToLive)
    {
        DnsMessage response = Response(query, DnsResponseCode.NoError);
        DnsResourceRecord record = new DnsResourceRecord();
        record.Name = query.Question.Name;
        record.RecordType = DnsRecordType.A;
        record.RecordTypeValue = 1;
        record.Class = DnsClass.Internet;
        record.ClassValue = 1;
        record.TimeToLive = timeToLive;
        record.Data = new DnsRecordData();
        record.Data.Address = new DnsAddressData();
        record.Data.Address.Address = [192, 0, 2, 25];
        response.Answers.Add(record);
        DnsQueryResult result = new DnsQueryResult();
        result.Success = true;
        result.Status = DnsTransportStatus.Success;
        result.Response = response;
        return result;
    }

    static DnsQueryResult AuthenticPositiveAnswer(DnsQueryRequest query)
    {
        DnsQueryResult result = PositiveAnswer(query, 60);
        result.Response!.Header.Flags = result.Response.Header.Flags |
            DnsHeaderFlag.AuthenticData;
        result.Response.Header.FlagsValue = (int)result.Response.Header.Flags;
        return result;
    }

    static DnsQueryResult NegativeAnswerWithSoa(
        DnsQueryRequest query,
        DnsResponseCode responseCode)
    {
        DnsQueryResult result = ResponseResult(query, responseCode);
        DnsResourceRecord soa = new DnsResourceRecord();
        soa.Name = Name(true, "example");
        soa.RecordType = DnsRecordType.Soa;
        soa.RecordTypeValue = 6;
        soa.Class = DnsClass.Internet;
        soa.ClassValue = 1;
        soa.TimeToLive = 60;
        soa.Data = new DnsRecordData();
        soa.Data.Soa = new DnsSoaData();
        soa.Data.Soa.Minimum = 30;
        result.Response!.Authorities.Add(soa);
        return result;
    }

    static DnsQueryResult ResponseResult(
        DnsQueryRequest query,
        DnsResponseCode responseCode)
    {
        DnsQueryResult result = new DnsQueryResult();
        result.Success = true;
        result.Status = DnsTransportStatus.Success;
        result.Response = Response(query, responseCode);
        return result;
    }

    static DnsQueryResult TransportFailure(DnsTransportStatus status)
    {
        DnsQueryResult result = new DnsQueryResult();
        result.Status = status;
        result.Success = false;
        return result;
    }

    static DnsQueryResult NegativeNameAnswer(DnsQueryRequest query)
    {
        DnsQueryResult result = new DnsQueryResult();
        result.Success = true;
        result.Status = DnsTransportStatus.Success;
        result.Response = Response(query, DnsResponseCode.NameError);
        return result;
    }

    static DnsMessage Response(
        DnsQueryRequest query,
        DnsResponseCode responseCode)
    {
        DnsMessage result = new DnsMessage();
        result.Header.Flags = DnsHeaderFlag.Response |
            DnsHeaderFlag.RecursionAvailable;
        result.Header.FlagsValue = (int)result.Header.Flags;
        result.Header.Opcode = DnsOpcode.Query;
        result.Header.OpcodeValue = 0;
        result.Header.ResponseCode = responseCode;
        result.Header.ResponseCodeValue = (int)responseCode;
        result.Questions.Add(query.Question);
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
