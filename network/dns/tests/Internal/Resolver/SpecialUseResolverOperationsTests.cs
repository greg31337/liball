using System.Text;

namespace dns.Tests.Internal.Resolver;

public class SpecialUseResolverOperationsTests
{
    [Test]
    public void OnionNamesAreRejectedBeforeSearchConfigurationOrNetworkUse()
    {
        RecordingQueryClient client = new RecordingQueryClient();
        DnsStubResolver resolver = Resolver(client);
        DnsResolveRequest request = Request(
            Name(false, "Hidden", "ONION"),
            DnsRecordType.A);

        DnsResolveResult result = resolver.Resolve(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DnsResolveStatus.NameError));
            Assert.That(Text(result.CanonicalName),
                Is.EqualTo("Hidden.ONION."));
            Assert.That(result.RecordSets, Is.Empty);
            Assert.That(result.Queries, Is.Empty);
            Assert.That(client.Requests, Is.Empty);
        });
    }

    [Test]
    public void LocalhostNamesReturnZeroTtlLoopbackAddressesWithoutNetworkUse()
    {
        RecordingQueryClient client = new RecordingQueryClient();
        DnsStubResolver resolver = Resolver(client);
        DnsResolveResult ipv4 = resolver.Resolve(Request(
            Name(false, "api", "LOCALHOST"),
            DnsRecordType.A));
        DnsResolveResult ipv6 = resolver.Resolve(Request(
            Name(true, "localhost"),
            DnsRecordType.Aaaa));

        Assert.Multiple(() =>
        {
            Assert.That(ipv4.Status, Is.EqualTo(DnsResolveStatus.Success));
            Assert.That(ipv4.RecordSets, Has.Count.EqualTo(1));
            Assert.That(ipv4.RecordSets[0].Records[0].Data!.Address!.Address,
                Is.EqualTo(new byte[] { 127, 0, 0, 1 }));
            Assert.That(Text(ipv4.RecordSets[0].Name),
                Is.EqualTo("api.LOCALHOST."));
            Assert.That(ipv4.HasTimeToLive, Is.True);
            Assert.That(ipv4.TimeToLive, Is.Zero);
            Assert.That(ipv4.SecurityStatus,
                Is.EqualTo(DnsSecurityStatus.Insecure));
            Assert.That(ipv6.Status, Is.EqualTo(DnsResolveStatus.Success));
            Assert.That(ipv6.RecordSets[0].RecordType,
                Is.EqualTo(DnsRecordType.Aaaa));
            Assert.That(ipv6.RecordSets[0].Records[0].Data!.Address!.Address,
                Is.EqualTo(new byte[]
                {
                    0, 0, 0, 0, 0, 0, 0, 0,
                    0, 0, 0, 0, 0, 0, 0, 1
                }));
            Assert.That(client.Requests, Is.Empty);
        });
    }

    [Test]
    public void BuiltInPoliciesCanBeDisabledForOrdinaryUpstreamResolution()
    {
        RecordingQueryClient client = new RecordingQueryClient();
        DnsStubResolver resolver = Resolver(client);
        DnsResolveRequest onion = Request(
            Name(true, "hidden", "onion"),
            DnsRecordType.A);
        onion.Options.RejectOnionNames = false;
        onion.Options.UseCache = false;
        onion.Options.Configuration.Servers.Add(Server());
        DnsResolveRequest localhost = Request(
            Name(true, "localhost"),
            DnsRecordType.A);
        localhost.Options.HandleLocalhostNames = false;
        localhost.Options.UseCache = false;
        localhost.Options.Configuration.Servers.Add(Server());

        DnsResolveResult onionResult = resolver.Resolve(onion);
        DnsResolveResult localhostResult = resolver.Resolve(localhost);

        Assert.Multiple(() =>
        {
            Assert.That(onionResult.Status,
                Is.EqualTo(DnsResolveStatus.Success));
            Assert.That(localhostResult.Status,
                Is.EqualTo(DnsResolveStatus.Success));
            Assert.That(client.Requests.Select(item => Text(item.Question.Name)),
                Is.EqualTo(new[] { "hidden.onion.", "localhost." }));
        });
    }

    [Test]
    public void CallerPolicyAnswersSearchCandidateWithTypedRecordTemplates()
    {
        RecordingQueryClient client = new RecordingQueryClient();
        DnsStubResolver resolver = Resolver(client);
        DnsResolveRequest request = Request(
            Name(false, "printer"),
            DnsRecordType.A);
        request.Options.Configuration.Servers.Add(Server());
        request.Options.Configuration.SearchDomains.Add(
            Name(true, "corp", "example"));
        DnsSpecialUsePolicy policy = new DnsSpecialUsePolicy();
        policy.Suffix = Name(true, "corp", "example");
        policy.Status = DnsResolveStatus.Success;
        policy.SecurityStatus = DnsSecurityStatus.Secure;
        DnsResourceRecordSet template = AddressTemplate(45, [192, 0, 2, 80]);
        policy.RecordSets.Add(template);
        request.Options.SpecialUsePolicies.Add(policy);

        DnsResolveResult result = resolver.Resolve(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DnsResolveStatus.Success));
            Assert.That(Text(result.CanonicalName),
                Is.EqualTo("printer.corp.example."));
            Assert.That(result.RecordSets, Has.Count.EqualTo(1));
            Assert.That(result.RecordSets[0], Is.Not.SameAs(template));
            Assert.That(Text(result.RecordSets[0].Name),
                Is.EqualTo("printer.corp.example."));
            Assert.That(Text(result.RecordSets[0].Records[0].Name),
                Is.EqualTo("printer.corp.example."));
            Assert.That(result.RecordSets[0].Records[0].Data!.Address!.Address,
                Is.EqualTo(new byte[] { 192, 0, 2, 80 }));
            Assert.That(result.SecurityStatus,
                Is.EqualTo(DnsSecurityStatus.Secure));
            Assert.That(result.TimeToLive, Is.EqualTo(45));
            Assert.That(client.Requests, Is.Empty);
        });
    }

    [Test]
    public void CallerPoliciesOverrideBuiltInsAndFilterUnavailableTypes()
    {
        RecordingQueryClient client = new RecordingQueryClient();
        DnsStubResolver resolver = Resolver(client);
        DnsResolveRequest refusedRequest = Request(
            Name(true, "localhost"),
            DnsRecordType.A);
        DnsSpecialUsePolicy refused = new DnsSpecialUsePolicy();
        refused.Suffix = Name(true, "localhost");
        refused.Status = DnsResolveStatus.Refused;
        refusedRequest.Options.SpecialUsePolicies.Add(refused);
        DnsResolveResult refusedResult = resolver.Resolve(refusedRequest);

        DnsResolveRequest typedRequest = Request(
            Name(true, "host", "internal"),
            DnsRecordType.Aaaa);
        DnsSpecialUsePolicy typed = new DnsSpecialUsePolicy();
        typed.Suffix = Name(true, "internal");
        typed.Status = DnsResolveStatus.Success;
        typed.RecordSets.Add(AddressTemplate(60, [192, 0, 2, 81]));
        typedRequest.Options.SpecialUsePolicies.Add(typed);
        DnsResolveResult typedResult = resolver.Resolve(typedRequest);

        Assert.Multiple(() =>
        {
            Assert.That(refusedResult.Status,
                Is.EqualTo(DnsResolveStatus.Refused));
            Assert.That(refusedResult.RecordSets, Is.Empty);
            Assert.That(typedResult.Status,
                Is.EqualTo(DnsResolveStatus.NoData));
            Assert.That(typedResult.RecordSets, Is.Empty);
            Assert.That(client.Requests, Is.Empty);
        });
    }

    [Test]
    public void ExactCallerPolicyDoesNotCaptureSubdomains()
    {
        RecordingQueryClient client = new RecordingQueryClient();
        DnsStubResolver resolver = Resolver(client);
        DnsResolveRequest request = Request(
            Name(true, "host", "private", "example"),
            DnsRecordType.A);
        request.Options.UseCache = false;
        request.Options.Configuration.Servers.Add(Server());
        DnsSpecialUsePolicy policy = new DnsSpecialUsePolicy();
        policy.Suffix = Name(true, "private", "example");
        policy.ApplyToSubdomains = false;
        policy.Status = DnsResolveStatus.Refused;
        request.Options.SpecialUsePolicies.Add(policy);

        DnsResolveResult result = resolver.Resolve(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DnsResolveStatus.Success));
            Assert.That(client.Requests, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void AliasTargetsAreCheckedBeforeAnotherUpstreamQuery()
    {
        AliasQueryClient client = new AliasQueryClient();
        DnsStubResolver resolver = new DnsStubResolver();
        resolver.QueryClient = client;
        resolver.Cache = new MemoryDnsCache();
        DnsResolveRequest request = Request(
            Name(true, "entry", "example"),
            DnsRecordType.A);
        request.Options.UseCache = false;
        request.Options.Configuration.Servers.Add(Server());

        DnsResolveResult result = resolver.Resolve(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DnsResolveStatus.NameError));
            Assert.That(Text(result.CanonicalName),
                Is.EqualTo("hidden.onion."));
            Assert.That(result.AliasSteps, Has.Count.EqualTo(1));
            Assert.That(result.Queries, Has.Count.EqualTo(1));
            Assert.That(result.TimeToLive, Is.EqualTo(25));
            Assert.That(client.Requests, Has.Count.EqualTo(1));
        });
    }

    static DnsStubResolver Resolver(RecordingQueryClient client)
    {
        DnsStubResolver result = new DnsStubResolver();
        result.QueryClient = client;
        result.Cache = new MemoryDnsCache();
        return result;
    }

    static DnsResolveRequest Request(DnsName name, DnsRecordType recordType)
    {
        DnsResolveRequest result = new DnsResolveRequest();
        result.Name = name;
        result.RecordType = recordType;
        result.RecordTypeValue = (int)recordType;
        result.Class = DnsClass.Internet;
        result.ClassValue = 1;
        return result;
    }

    static DnsResolverServer Server()
    {
        DnsResolverServer result = new DnsResolverServer();
        result.Endpoint.HasAddress = true;
        result.Endpoint.AddressFamily = 4;
        result.Endpoint.Address = [192, 0, 2, 1];
        result.Endpoint.Port = 53;
        result.Transport = DnsTransportKind.Udp;
        return result;
    }

    static DnsResourceRecordSet AddressTemplate(uint ttl, byte[] address)
    {
        DnsResourceRecord record = new DnsResourceRecord();
        record.RecordType = DnsRecordType.A;
        record.RecordTypeValue = 1;
        record.Class = DnsClass.Internet;
        record.ClassValue = 1;
        record.TimeToLive = ttl;
        record.Data = new DnsRecordData();
        record.Data.Address = new DnsAddressData();
        record.Data.Address.Address = address;
        DnsResourceRecordSet result = new DnsResourceRecordSet();
        result.RecordType = DnsRecordType.A;
        result.RecordTypeValue = 1;
        result.Class = DnsClass.Internet;
        result.ClassValue = 1;
        result.TimeToLive = ttl;
        result.Records.Add(record);
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

    static string Text(DnsName? name)
    {
        if (name == null)
        {
            return string.Empty;
        }

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

    class RecordingQueryClient : IDnsQueryClient
    {
        internal List<DnsQueryRequest> Requests = new();

        public DnsQueryResult Query(DnsQueryRequest request)
        {
            Requests.Add(request);
            DnsMessage response = new DnsMessage();
            response.Header.Flags = DnsHeaderFlag.Response |
                DnsHeaderFlag.RecursionAvailable;
            response.Header.FlagsValue = (int)response.Header.Flags;
            response.Header.Opcode = DnsOpcode.Query;
            response.Header.OpcodeValue = 0;
            response.Header.ResponseCode = DnsResponseCode.NoError;
            response.Header.ResponseCodeValue = 0;
            response.Questions.Add(request.Question);
            DnsResourceRecord record = new DnsResourceRecord();
            record.Name = request.Question.Name;
            record.RecordType = request.Question.RecordType;
            record.RecordTypeValue = request.Question.RecordTypeValue;
            record.Class = request.Question.Class;
            record.ClassValue = request.Question.ClassValue;
            record.TimeToLive = 30;
            record.Data = new DnsRecordData();
            record.Data.Address = new DnsAddressData();
            record.Data.Address.Address = [192, 0, 2, 90];
            response.Answers.Add(record);
            DnsQueryResult result = new DnsQueryResult();
            result.Success = true;
            result.Status = DnsTransportStatus.Success;
            result.Response = response;
            return result;
        }
    }

    class AliasQueryClient : IDnsQueryClient
    {
        internal List<DnsQueryRequest> Requests = new();

        public DnsQueryResult Query(DnsQueryRequest request)
        {
            Requests.Add(request);
            DnsMessage response = new DnsMessage();
            response.Header.Flags = DnsHeaderFlag.Response |
                DnsHeaderFlag.RecursionAvailable;
            response.Header.FlagsValue = (int)response.Header.Flags;
            response.Header.Opcode = DnsOpcode.Query;
            response.Header.OpcodeValue = 0;
            response.Header.ResponseCode = DnsResponseCode.NoError;
            response.Header.ResponseCodeValue = 0;
            response.Questions.Add(request.Question);
            DnsResourceRecord alias = new DnsResourceRecord();
            alias.Name = request.Question.Name;
            alias.RecordType = DnsRecordType.Cname;
            alias.RecordTypeValue = 5;
            alias.Class = DnsClass.Internet;
            alias.ClassValue = 1;
            alias.TimeToLive = 25;
            alias.Data = new DnsRecordData();
            alias.Data.Name = new DnsNameData();
            alias.Data.Name.Name = Name(true, "hidden", "onion");
            response.Answers.Add(alias);
            DnsQueryResult result = new DnsQueryResult();
            result.Success = true;
            result.Status = DnsTransportStatus.Success;
            result.Response = response;
            return result;
        }
    }
}
