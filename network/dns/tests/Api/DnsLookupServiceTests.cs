using System.Text;

namespace dns.Tests.Api;

public class DnsLookupServiceTests
{
    [Test]
    public void LocalhostLookupCombinesBuiltInLoopbackFamiliesWithoutNetwork()
    {
        DnsLookupService service = new DnsLookupService();
        DnsResolveRequest request = Request();
        request.Name = Name("localhost");

        DnsAddressResult result = service.LookupAddresses(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DnsResolveStatus.Success));
            Assert.That(result.AddressFamilies, Is.EqualTo(new[] { 6, 4 }));
            Assert.That(result.Addresses[0], Is.EqualTo(new byte[]
            {
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 1
            }));
            Assert.That(result.Addresses[1],
                Is.EqualTo(new byte[] { 127, 0, 0, 1 }));
            Assert.That(result.SecurityStatus,
                Is.EqualTo(DnsSecurityStatus.Insecure));
            Assert.That(result.HasTimeToLive, Is.True);
            Assert.That(result.TimeToLive, Is.Zero);
        });
    }

    [Test]
    public void AggregatesFamiliesOrdersAddressesAndPreservesDnsMetadata()
    {
        DnsMessage ipv4Response = new DnsMessage();
        DnsMessage ipv6Response = new DnsMessage();
        DnsAliasStep ipv6Alias = new DnsAliasStep();
        ScriptedResolver resolver = new ScriptedResolver();
        resolver.Ipv4 = AddressResult(
            DnsRecordType.A,
            100,
            DnsSecurityStatus.Secure,
            Name("ipv4", "example"),
            ipv4Response,
            [192, 0, 2, 10],
            [127, 0, 0, 1],
            [192, 0, 2, 10]);
        resolver.Ipv6 = AddressResult(
            DnsRecordType.Aaaa,
            60,
            DnsSecurityStatus.Insecure,
            Name("ipv6", "example"),
            ipv6Response,
            [0x20, 0x01, 0x0d, 0xb8, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1],
            [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1],
            [0x20, 0x02, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1],
            [0xfc, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1]);
        resolver.Ipv6.AliasSteps.Add(ipv6Alias);
        DnsLookupService service = Service(resolver);
        DnsResolveRequest request = Request();
        request.RecordType = DnsRecordType.Mx;
        request.RecordTypeValue = 15;

        DnsAddressResult result = service.LookupAddresses(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DnsResolveStatus.Success));
            Assert.That(resolver.Requests.Select(item => item.RecordTypeValue),
                Is.EqualTo(new[] { 1, 28 }));
            Assert.That(request.RecordType, Is.EqualTo(DnsRecordType.Mx));
            Assert.That(result.AddressFamilies,
                Is.EqualTo(new[] { 6, 6, 4, 4, 6, 6 }));
            Assert.That(result.Addresses[0],
                Is.EqualTo(new byte[]
                {
                    0, 0, 0, 0, 0, 0, 0, 0,
                    0, 0, 0, 0, 0, 0, 0, 1
                }));
            Assert.That(result.Addresses[1][0..4],
                Is.EqualTo(new byte[] { 0x20, 0x01, 0x0d, 0xb8 }));
            Assert.That(result.Addresses[2],
                Is.EqualTo(new byte[] { 127, 0, 0, 1 }));
            Assert.That(result.Addresses[3],
                Is.EqualTo(new byte[] { 192, 0, 2, 10 }));
            Assert.That(result.Addresses[4][0..2],
                Is.EqualTo(new byte[] { 0x20, 0x02 }));
            Assert.That(result.Addresses[5][0], Is.EqualTo(0xfc));
            Assert.That(result.Addresses, Has.Count.EqualTo(6));
            Assert.That(result.Response, Is.SameAs(ipv6Response));
            Assert.That(result.CanonicalName,
                Is.SameAs(resolver.Ipv6.CanonicalName));
            Assert.That(result.AliasSteps,
                Is.EqualTo(new[] { ipv6Alias }));
            Assert.That(result.SecurityStatus,
                Is.EqualTo(DnsSecurityStatus.Insecure));
            Assert.That(result.HasTimeToLive, Is.True);
            Assert.That(result.TimeToLive, Is.EqualTo(60));
        });
    }

    [Test]
    public void ReturnsSuccessfulFamilyWhenTheOtherFamilyFails()
    {
        ScriptedResolver resolver = new ScriptedResolver();
        resolver.Ipv4.Status = DnsResolveStatus.ServerFailure;
        resolver.Ipv6 = AddressResult(
            DnsRecordType.Aaaa,
            30,
            DnsSecurityStatus.Secure,
            Name("host", "example"),
            new DnsMessage(),
            [0x20, 0x01, 0x0d, 0xb8, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2]);
        DnsLookupService service = Service(resolver);

        DnsAddressResult result = service.LookupAddresses(Request());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DnsResolveStatus.Success));
            Assert.That(result.Addresses, Has.Count.EqualTo(1));
            Assert.That(result.AddressFamilies, Is.EqualTo(new[] { 6 }));
            Assert.That(result.SecurityStatus,
                Is.EqualTo(DnsSecurityStatus.Secure));
            Assert.That(result.TimeToLive, Is.EqualTo(30));
        });
    }

    [Test]
    public void EmptyFamiliesUseNegativeStatusAndMetadataPrecedence()
    {
        ScriptedResolver resolver = new ScriptedResolver();
        resolver.Ipv4.Status = DnsResolveStatus.NameError;
        resolver.Ipv4.CanonicalName = Name("missing", "example");
        resolver.Ipv6.Status = DnsResolveStatus.NoData;
        resolver.Ipv6.CanonicalName = Name("existing", "example");
        resolver.Ipv6.Response = new DnsMessage();
        resolver.Ipv6.SecurityStatus = DnsSecurityStatus.Insecure;
        resolver.Ipv6.HasTimeToLive = true;
        resolver.Ipv6.TimeToLive = 15;
        DnsLookupService service = Service(resolver);

        DnsAddressResult result = service.LookupAddresses(Request());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DnsResolveStatus.NoData));
            Assert.That(result.Addresses, Is.Empty);
            Assert.That(Text(result.CanonicalName!),
                Is.EqualTo("existing.example."));
            Assert.That(result.Response, Is.SameAs(resolver.Ipv6.Response));
            Assert.That(result.SecurityStatus,
                Is.EqualTo(DnsSecurityStatus.Insecure));
            Assert.That(result.TimeToLive, Is.EqualTo(15));
        });
    }

    [Test]
    public void MalformedSuccessfulFamilyProducesProtocolErrorWithoutAddresses()
    {
        ScriptedResolver resolver = new ScriptedResolver();
        resolver.Ipv4 = AddressResult(
            DnsRecordType.A,
            20,
            DnsSecurityStatus.Indeterminate,
            Name("bad", "example"),
            new DnsMessage(),
            [192, 0, 2]);
        resolver.Ipv6.Status = DnsResolveStatus.NoData;
        DnsLookupService service = Service(resolver);

        DnsAddressResult result = service.LookupAddresses(Request());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(DnsResolveStatus.ProtocolError));
            Assert.That(result.Addresses, Is.Empty);
            Assert.That(result.AddressFamilies, Is.Empty);
            Assert.That(result.CanonicalName,
                Is.SameAs(resolver.Ipv4.CanonicalName));
        });
    }

    static DnsLookupService Service(ScriptedResolver resolver)
    {
        DnsLookupService result = new DnsLookupService();
        result.Resolver = resolver;
        return result;
    }

    static DnsResolveRequest Request()
    {
        DnsResolveRequest result = new DnsResolveRequest();
        result.Name = Name("host", "example");
        result.Class = DnsClass.Internet;
        result.ClassValue = 1;
        return result;
    }

    static DnsResolveResult AddressResult(
        DnsRecordType recordType,
        uint timeToLive,
        DnsSecurityStatus securityStatus,
        DnsName canonicalName,
        DnsMessage response,
        params byte[][] addresses)
    {
        DnsResolveResult result = new DnsResolveResult();
        result.Status = DnsResolveStatus.Success;
        result.CanonicalName = canonicalName;
        result.Response = response;
        result.SecurityStatus = securityStatus;
        result.HasTimeToLive = true;
        result.TimeToLive = timeToLive;
        DnsResourceRecordSet set = new DnsResourceRecordSet();
        set.Name = canonicalName;
        set.RecordType = recordType;
        set.RecordTypeValue = (int)recordType;
        set.Class = DnsClass.Internet;
        set.ClassValue = 1;
        set.TimeToLive = timeToLive;
        int index = 0;
        while (index < addresses.Length)
        {
            DnsResourceRecord record = new DnsResourceRecord();
            record.Name = canonicalName;
            record.RecordType = recordType;
            record.RecordTypeValue = (int)recordType;
            record.Class = DnsClass.Internet;
            record.ClassValue = 1;
            record.TimeToLive = timeToLive;
            record.Data = new DnsRecordData();
            record.Data.Address = new DnsAddressData();
            record.Data.Address.Address = addresses[index];
            set.Records.Add(record);
            index = index + 1;
        }

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

        return result + ".";
    }

    class ScriptedResolver : IDnsResolver
    {
        internal DnsResolveResult Ipv4 = new DnsResolveResult();
        internal DnsResolveResult Ipv6 = new DnsResolveResult();
        internal List<DnsResolveRequest> Requests = new();

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
            if (request.RecordTypeValue == 1)
            {
                return Ipv4;
            }

            return Ipv6;
        }
    }
}
