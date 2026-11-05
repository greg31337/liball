using System.Text;

namespace dns.Tests.Api;

public class DnsLookupServiceBindingTests
{
    [Test]
    public void MultipleAliasesUseInjectedEntropyAndAggregateMetadata()
    {
        BindingResolver resolver = new BindingResolver();
        DnsResolveResult aliases = BindingResult(
            DnsRecordType.Svcb,
            Name("origin", "example"),
            80,
            new DnsMessage(),
            BindingRecord(
                DnsRecordType.Svcb,
                0,
                Name("first", "example")),
            BindingRecord(
                DnsRecordType.Svcb,
                0,
                Name("second", "example")));
        resolver.BindingResults.Enqueue(aliases);
        DnsResolveResult serviceBinding = BindingResult(
            DnsRecordType.Svcb,
            Name("second", "example"),
            25,
            new DnsMessage(),
            BindingRecord(
                DnsRecordType.Svcb,
                1,
                Name("target", "example")));
        serviceBinding.SecurityStatus = DnsSecurityStatus.Insecure;
        resolver.BindingResults.Enqueue(serviceBinding);
        FixedEntropy entropy = new FixedEntropy(1);
        DnsLookupService service = Service(resolver);
        service.Entropy = entropy;

        DnsServiceResult result = service.LookupServiceBindings(Request(
            Name("origin", "example"),
            DnsRecordType.Svcb));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DnsResolveStatus.Success));
            Assert.That(result.Endpoints, Has.Count.EqualTo(1));
            Assert.That(Text(result.CanonicalName),
                Is.EqualTo("second.example."));
            Assert.That(Text(result.Endpoints[0].Target),
                Is.EqualTo("target.example."));
            Assert.That(result.SecurityStatus,
                Is.EqualTo(DnsSecurityStatus.Insecure));
            Assert.That(result.Endpoints[0].SecurityStatus,
                Is.EqualTo(DnsSecurityStatus.Insecure));
            Assert.That(result.HasTimeToLive, Is.True);
            Assert.That(result.TimeToLive, Is.EqualTo(25));
            Assert.That(result.Endpoints[0].TimeToLive, Is.EqualTo(25));
            Assert.That(entropy.MaximumValues, Is.EqualTo(new[] { 2 }));
            Assert.That(resolver.Requests.Take(2).Select(item => Text(item.Name)),
                Is.EqualTo(new[]
                {
                    "origin.example.",
                    "second.example."
                }));
        });
    }

    [Test]
    public void HttpsAliasModeFollowsSameTypeAndBuildsOrderedEndpoints()
    {
        BindingResolver resolver = new BindingResolver();
        DnsMessage aliasResponse = new DnsMessage();
        DnsMessage serviceResponse = new DnsMessage();
        DnsSvcbParameter ignored = Parameter(2);
        ignored.NoDefaultAlpn = true;
        resolver.BindingResults.Enqueue(BindingResult(
            DnsRecordType.Https,
            Name("origin", "example"),
            120,
            aliasResponse,
            BindingRecord(
                DnsRecordType.Https,
                0,
                Name("alias", "example"),
                ignored),
            BindingRecord(
                DnsRecordType.Https,
                5,
                Name("ignored", "example"),
                ignored)));
        DnsSvcbParameter port = Parameter(3);
        port.HasPort = true;
        port.Port = 8443;
        DnsResourceRecord first = BindingRecord(
            DnsRecordType.Https,
            20,
            Name(),
            port);
        DnsResourceRecord second = BindingRecord(
            DnsRecordType.Https,
            10,
            Name("edge", "example"));
        resolver.BindingResults.Enqueue(BindingResult(
            DnsRecordType.Https,
            Name("alias", "example"),
            90,
            serviceResponse,
            first,
            second));
        resolver.ReturnAddresses = true;
        DnsLookupService service = Service(resolver);
        DnsResolveRequest request = Request(
            Name("origin", "example"),
            DnsRecordType.Https);

        DnsServiceResult result = service.LookupServices(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DnsResolveStatus.Success));
            Assert.That(Text(result.CanonicalName),
                Is.EqualTo("alias.example."));
            Assert.That(result.Response, Is.SameAs(serviceResponse));
            Assert.That(result.Endpoints.Select(item => item.Priority),
                Is.EqualTo(new[] { 10, 20 }));
            Assert.That(result.Endpoints.Select(item => item.Port),
                Is.EqualTo(new[] { 443, 8443 }));
            Assert.That(result.Endpoints.Select(item => Text(item.Target)),
                Is.EqualTo(new[] { "edge.example.", "alias.example." }));
            Assert.That(result.Endpoints[0].ServiceBinding,
                Is.SameAs(second.Data!.Svcb));
            Assert.That(result.Endpoints[1].ServiceBinding,
                Is.SameAs(first.Data!.Svcb));
            Assert.That(result.Endpoints.All(item =>
                item.Addresses.Count == 1 &&
                item.AddressFamilies.SequenceEqual(new[] { 4 })), Is.True);
            Assert.That(result.SecurityStatus,
                Is.EqualTo(DnsSecurityStatus.Insecure));
            Assert.That(result.TimeToLive, Is.EqualTo(30));
            Assert.That(result.Endpoints.All(item =>
                item.HasTimeToLive && item.TimeToLive == 30), Is.True);
            Assert.That(resolver.Requests.Select(item => item.RecordTypeValue),
                Is.EqualTo(new[] { 65, 65, 1, 28, 1, 28 }));
            Assert.That(resolver.Requests.Take(2).Select(item => Text(item.Name)),
                Is.EqualTo(new[] { "origin.example.", "alias.example." }));
        });
    }

    [Test]
    public void UnsupportedMandatoryRecordIsSkippedAndHintsAreFallbackAddresses()
    {
        BindingResolver resolver = new BindingResolver();
        DnsSvcbParameter mandatoryUnknown = Parameter(0);
        mandatoryUnknown.MandatoryKeyValues.Add(65400);
        DnsSvcbParameter unknown = Parameter(65400);
        unknown.RawValue = [1, 2, 3];
        DnsResourceRecord incompatible = BindingRecord(
            DnsRecordType.Svcb,
            1,
            Name("unsupported", "example"),
            mandatoryUnknown,
            unknown);
        DnsSvcbParameter port = Parameter(3);
        port.HasPort = true;
        port.Port = 853;
        DnsSvcbParameter ipv4 = Parameter(4);
        ipv4.Ipv4Hints.Add([192, 0, 2, 44]);
        DnsSvcbParameter ipv6 = Parameter(6);
        ipv6.Ipv6Hints.Add(
            [32, 1, 13, 184, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 44]);
        DnsResourceRecord compatible = BindingRecord(
            DnsRecordType.Svcb,
            2,
            Name(),
            port,
            ipv4,
            ipv6);
        resolver.BindingResults.Enqueue(BindingResult(
            DnsRecordType.Svcb,
            Name("service", "example"),
            50,
            new DnsMessage(),
            incompatible,
            compatible));
        DnsLookupService service = Service(resolver);

        DnsServiceResult result = service.LookupServiceBindings(Request(
            Name("service", "example"),
            DnsRecordType.Svcb));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DnsResolveStatus.Success));
            Assert.That(result.Endpoints, Has.Count.EqualTo(1));
            Assert.That(result.Endpoints[0].Priority, Is.EqualTo(2));
            Assert.That(result.Endpoints[0].Port, Is.EqualTo(853));
            Assert.That(Text(result.Endpoints[0].Target),
                Is.EqualTo("service.example."));
            Assert.That(result.Endpoints[0].Addresses,
                Is.EqualTo(new byte[][]
                {
                    [192, 0, 2, 44],
                    [32, 1, 13, 184, 0, 0, 0, 0,
                        0, 0, 0, 0, 0, 0, 0, 44]
                }));
            Assert.That(result.Endpoints[0].AddressFamilies,
                Is.EqualTo(new[] { 4, 6 }));
            Assert.That(resolver.Requests.Select(item => item.RecordTypeValue),
                Is.EqualTo(new[] { 64, 1, 28 }));
        });
    }

    [Test]
    public void MissingMandatoryParameterRejectsTheServiceRecordSet()
    {
        BindingResolver resolver = new BindingResolver();
        DnsSvcbParameter mandatory = Parameter(0);
        mandatory.MandatoryKeyValues.Add(3);
        resolver.BindingResults.Enqueue(BindingResult(
            DnsRecordType.Svcb,
            Name("service", "example"),
            60,
            new DnsMessage(),
            BindingRecord(
                DnsRecordType.Svcb,
                1,
                Name("target", "example"),
                mandatory)));
        DnsLookupService service = Service(resolver);

        DnsServiceResult result = service.LookupServiceBindings(Request(
            Name("service", "example"),
            DnsRecordType.Svcb));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(DnsResolveStatus.ProtocolError));
            Assert.That(result.Endpoints, Is.Empty);
            Assert.That(resolver.Requests, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void AliasLoopAndRootTargetTerminateWithoutAddressQueries()
    {
        BindingResolver loopResolver = new BindingResolver();
        loopResolver.BindingResults.Enqueue(BindingResult(
            DnsRecordType.Svcb,
            Name("origin", "example"),
            60,
            new DnsMessage(),
            BindingRecord(
                DnsRecordType.Svcb,
                0,
                Name("next", "example"))));
        loopResolver.BindingResults.Enqueue(BindingResult(
            DnsRecordType.Svcb,
            Name("next", "example"),
            40,
            new DnsMessage(),
            BindingRecord(
                DnsRecordType.Svcb,
                0,
                Name("ORIGIN", "EXAMPLE"))));
        DnsServiceResult loop = Service(loopResolver).LookupServiceBindings(
            Request(Name("origin", "example"), DnsRecordType.Svcb));

        BindingResolver rootResolver = new BindingResolver();
        rootResolver.BindingResults.Enqueue(BindingResult(
            DnsRecordType.Https,
            Name("origin", "example"),
            30,
            new DnsMessage(),
            BindingRecord(DnsRecordType.Https, 0, Name())));
        DnsServiceResult root = Service(rootResolver).LookupServiceBindings(
            Request(Name("origin", "example"), DnsRecordType.Https));

        Assert.Multiple(() =>
        {
            Assert.That(loop.Status, Is.EqualTo(DnsResolveStatus.Loop));
            Assert.That(loop.Endpoints, Is.Empty);
            Assert.That(loop.TimeToLive, Is.EqualTo(40));
            Assert.That(loopResolver.Requests, Has.Count.EqualTo(2));
            Assert.That(root.Status, Is.EqualTo(DnsResolveStatus.NoData));
            Assert.That(root.Endpoints, Is.Empty);
            Assert.That(rootResolver.Requests, Has.Count.EqualTo(1));
        });
    }

    static DnsLookupService Service(BindingResolver resolver)
    {
        DnsLookupService result = new DnsLookupService();
        result.Resolver = resolver;
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

    static DnsResolveResult BindingResult(
        DnsRecordType recordType,
        DnsName owner,
        uint timeToLive,
        DnsMessage response,
        params DnsResourceRecord[] records)
    {
        DnsResolveResult result = new DnsResolveResult();
        result.Status = DnsResolveStatus.Success;
        result.CanonicalName = owner;
        result.Response = response;
        result.SecurityStatus = DnsSecurityStatus.Secure;
        result.HasTimeToLive = true;
        result.TimeToLive = timeToLive;
        DnsResourceRecordSet set = new DnsResourceRecordSet();
        set.Name = owner;
        set.RecordType = recordType;
        set.RecordTypeValue = (int)recordType;
        set.Class = DnsClass.Internet;
        set.ClassValue = 1;
        set.TimeToLive = timeToLive;
        int index = 0;
        while (index < records.Length)
        {
            records[index].Name = owner;
            records[index].TimeToLive = timeToLive;
            set.Records.Add(records[index]);
            index = index + 1;
        }

        result.RecordSets.Add(set);
        return result;
    }

    static DnsResourceRecord BindingRecord(
        DnsRecordType recordType,
        int priority,
        DnsName target,
        params DnsSvcbParameter[] parameters)
    {
        DnsResourceRecord result = new DnsResourceRecord();
        result.RecordType = recordType;
        result.RecordTypeValue = (int)recordType;
        result.Class = DnsClass.Internet;
        result.ClassValue = 1;
        result.Data = new DnsRecordData();
        result.Data.Svcb = new DnsSvcbData();
        result.Data.Svcb.Priority = priority;
        result.Data.Svcb.TargetName = target;
        result.Data.Svcb.Parameters.AddRange(parameters);
        return result;
    }

    static DnsSvcbParameter Parameter(int key)
    {
        DnsSvcbParameter result = new DnsSvcbParameter();
        result.Key = (DnsSvcbParameterKey)key;
        result.KeyValue = key;
        return result;
    }

    static DnsResolveResult AddressResult(
        DnsResolveRequest request,
        bool returnAddress)
    {
        if (!returnAddress)
        {
            DnsResolveResult noData = new DnsResolveResult();
            noData.Status = DnsResolveStatus.NoData;
            noData.CanonicalName = request.Name;
            return noData;
        }

        DnsResourceRecord address = new DnsResourceRecord();
        address.Name = request.Name;
        address.RecordType = DnsRecordType.A;
        address.RecordTypeValue = 1;
        address.Class = DnsClass.Internet;
        address.ClassValue = 1;
        address.TimeToLive = 30;
        address.Data = new DnsRecordData();
        address.Data.Address = new DnsAddressData();
        address.Data.Address.Address = [192, 0, 2, 10];
        DnsResolveResult result = new DnsResolveResult();
        result.Status = DnsResolveStatus.Success;
        result.CanonicalName = request.Name;
        result.SecurityStatus = DnsSecurityStatus.Insecure;
        result.HasTimeToLive = true;
        result.TimeToLive = 30;
        DnsResourceRecordSet set = new DnsResourceRecordSet();
        set.Name = request.Name;
        set.RecordType = DnsRecordType.A;
        set.RecordTypeValue = 1;
        set.Class = DnsClass.Internet;
        set.ClassValue = 1;
        set.TimeToLive = 30;
        set.Records.Add(address);
        result.RecordSets.Add(set);
        return result;
    }

    static DnsName Name(params string[] labels)
    {
        DnsName result = new DnsName();
        result.IsAbsolute = true;
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

        return result + ".";
    }

    class FixedEntropy : IDnsLookupEntropy
    {
        readonly Queue<int> values = new();
        internal List<int> MaximumValues = new();

        internal FixedEntropy(params int[] values)
        {
            int index = 0;
            while (index < values.Length)
            {
                this.values.Enqueue(values[index]);
                index = index + 1;
            }
        }

        public int NextIndex(int maximumExclusive)
        {
            MaximumValues.Add(maximumExclusive);
            return values.Count == 0 ? 0 : values.Dequeue();
        }
    }

    class BindingResolver : IDnsResolver
    {
        internal Queue<DnsResolveResult> BindingResults = new();
        internal List<DnsResolveRequest> Requests = new();
        internal bool ReturnAddresses;

        public DnsResolverConfiguration ParseResolverConfiguration(
            string source)
        {
            return new DnsResolverConfiguration();
        }

        public DnsResolverConfiguration ValidateResolverConfiguration(
            DnsResolverConfiguration configuration)
        {
            return configuration;
        }

        public DnsResolveResult Resolve(DnsResolveRequest request)
        {
            Requests.Add(request);
            if (request.RecordTypeValue == 64 ||
                request.RecordTypeValue == 65)
            {
                return BindingResults.Dequeue();
            }

            return AddressResult(
                request,
                ReturnAddresses && request.RecordTypeValue == 1);
        }
    }
}
