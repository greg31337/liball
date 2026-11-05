using System.Text;

namespace dns.Tests.Api;

public class DnsLookupServiceRecordTests
{
    [Test]
    public void PointerLookupUsesPtrWithoutMutatingTheCallerRequest()
    {
        ScriptedResolver resolver = new ScriptedResolver();
        resolver.Pointer.Status = DnsResolveStatus.Success;
        resolver.Pointer.CanonicalName = Name("host", "example");
        DnsLookupService service = Service(resolver, new FixedEntropy());
        DnsResolveRequest request = Request(Name("4", "3", "2", "1", "in-addr", "arpa"));
        request.RecordType = DnsRecordType.Mx;
        request.RecordTypeValue = 15;

        DnsResolveResult result = service.LookupPointer(request);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.SameAs(resolver.Pointer));
            Assert.That(resolver.Requests, Has.Count.EqualTo(1));
            Assert.That(resolver.Requests[0].RecordType,
                Is.EqualTo(DnsRecordType.Ptr));
            Assert.That(resolver.Requests[0].RecordTypeValue, Is.EqualTo(12));
            Assert.That(request.RecordType, Is.EqualTo(DnsRecordType.Mx));
            Assert.That(request.RecordTypeValue, Is.EqualTo(15));
        });
    }

    [Test]
    public void MailLookupOrdersPreferencesStablyAndPreservesMetadata()
    {
        ScriptedResolver resolver = new ScriptedResolver();
        DnsMessage response = new DnsMessage();
        resolver.Mail = Result(
            DnsRecordType.Mx,
            75,
            DnsSecurityStatus.Secure,
            response,
            MailRecord(20, "backup"),
            MailRecord(10, "first"),
            MailRecord(10, "second"));
        DnsLookupService service = Service(resolver, new FixedEntropy());

        DnsMailResult result = service.LookupMail(Request(Name("example")));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DnsResolveStatus.Success));
            Assert.That(result.Exchanges.Select(item => item.Preference),
                Is.EqualTo(new[] { 10, 10, 20 }));
            Assert.That(result.Exchanges.Select(item => Text(item.Name)),
                Is.EqualTo(new[]
                {
                    "first.example.",
                    "second.example.",
                    "backup.example."
                }));
            Assert.That(result.Response, Is.SameAs(response));
            Assert.That(result.SecurityStatus,
                Is.EqualTo(DnsSecurityStatus.Secure));
            Assert.That(result.TimeToLive, Is.EqualTo(75));
            Assert.That(resolver.Requests[0].RecordTypeValue, Is.EqualTo(15));
        });
    }

    [Test]
    public void TextLookupFlattensCharacterStringsAndDispatchesByType()
    {
        ScriptedResolver resolver = new ScriptedResolver();
        byte[] first = Encoding.ASCII.GetBytes("first");
        resolver.Text = Result(
            DnsRecordType.Txt,
            45,
            DnsSecurityStatus.Insecure,
            new DnsMessage(),
            TextRecord(first, Encoding.ASCII.GetBytes("second")),
            TextRecord(new byte[0]));
        DnsLookupService service = Service(resolver, new FixedEntropy());
        DnsResolveRequest request = Request(Name("txt", "example"));
        request.RecordType = DnsRecordType.Txt;
        request.RecordTypeValue = 16;

        DnsServiceResult direct = service.LookupText(request);
        DnsServiceResult dispatched = service.LookupServices(request);
        first[0] = (byte)'X';

        Assert.Multiple(() =>
        {
            Assert.That(direct.Status, Is.EqualTo(DnsResolveStatus.Success));
            Assert.That(direct.TextValues.Select(Encoding.ASCII.GetString),
                Is.EqualTo(new[] { "first", "second", string.Empty }));
            Assert.That(dispatched.TextValues.Select(Encoding.ASCII.GetString),
                Is.EqualTo(new[] { "first", "second", string.Empty }));
            Assert.That(direct.SecurityStatus,
                Is.EqualTo(DnsSecurityStatus.Insecure));
            Assert.That(direct.TimeToLive, Is.EqualTo(45));
            Assert.That(resolver.Requests.Select(item => item.RecordTypeValue),
                Is.EqualTo(new[] { 16, 16 }));
        });
    }

    [Test]
    public void ServiceLookupUsesInclusiveWeightedOrderAndResolvesTargets()
    {
        ScriptedResolver resolver = new ScriptedResolver();
        resolver.Service = Result(
            DnsRecordType.Srv,
            100,
            DnsSecurityStatus.Secure,
            new DnsMessage(),
            ServiceRecord(10, 0, 8000, "zero"),
            ServiceRecord(10, 5, 8001, "five"),
            ServiceRecord(10, 10, 8002, "ten"),
            ServiceRecord(20, 0, 9000, "later"));
        FixedEntropy entropy = new FixedEntropy(0, 6, 0, 0);
        DnsLookupService service = Service(resolver, entropy);
        DnsResolveRequest request = Request(Name("_demo", "_tcp", "example"));

        DnsServiceResult result = service.LookupServiceRecords(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DnsResolveStatus.Success));
            Assert.That(result.Endpoints.Select(item => Text(item.Target)),
                Is.EqualTo(new[]
                {
                    "zero.example.",
                    "ten.example.",
                    "five.example.",
                    "later.example."
                }));
            Assert.That(result.Endpoints.Select(item => item.Priority),
                Is.EqualTo(new[] { 10, 10, 10, 20 }));
            Assert.That(result.Endpoints.Select(item => item.Port),
                Is.EqualTo(new[] { 8000, 8002, 8001, 9000 }));
            Assert.That(result.Endpoints.All(item =>
                item.Addresses.Count == 1 &&
                item.AddressFamilies.SequenceEqual(new[] { 4 })), Is.True);
            Assert.That(result.Endpoints.All(item =>
                item.SecurityStatus == DnsSecurityStatus.Insecure), Is.True);
            Assert.That(result.Endpoints.All(item =>
                item.HasTimeToLive && item.TimeToLive == 30), Is.True);
            Assert.That(result.SecurityStatus,
                Is.EqualTo(DnsSecurityStatus.Insecure));
            Assert.That(result.TimeToLive, Is.EqualTo(30));
            Assert.That(entropy.MaximumValues,
                Is.EqualTo(new[] { 16, 16, 6, 1 }));
            Assert.That(resolver.Requests.Select(item => item.RecordTypeValue),
                Is.EqualTo(new[]
                {
                    33,
                    1, 28,
                    1, 28,
                    1, 28,
                    1, 28
                }));
        });
    }

    [Test]
    public void RootServiceTargetReportsServiceUnavailable()
    {
        ScriptedResolver resolver = new ScriptedResolver();
        resolver.Service = Result(
            DnsRecordType.Srv,
            60,
            DnsSecurityStatus.Secure,
            new DnsMessage(),
            ServiceRecord(0, 0, 0));
        DnsLookupService service = Service(resolver, new FixedEntropy());

        DnsServiceResult result = service.LookupServiceRecords(
            Request(Name("_demo", "_tcp", "example")));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DnsResolveStatus.NoData));
            Assert.That(result.Endpoints, Is.Empty);
            Assert.That(resolver.Requests, Has.Count.EqualTo(1));
        });
    }

    static DnsLookupService Service(
        ScriptedResolver resolver,
        FixedEntropy entropy)
    {
        DnsLookupService result = new DnsLookupService();
        result.Resolver = resolver;
        result.Entropy = entropy;
        return result;
    }

    static DnsResolveRequest Request(DnsName name)
    {
        DnsResolveRequest result = new DnsResolveRequest();
        result.Name = name;
        result.Class = DnsClass.Internet;
        result.ClassValue = 1;
        return result;
    }

    static DnsResolveResult Result(
        DnsRecordType recordType,
        uint timeToLive,
        DnsSecurityStatus securityStatus,
        DnsMessage response,
        params DnsResourceRecord[] records)
    {
        DnsResolveResult result = new DnsResolveResult();
        result.Status = DnsResolveStatus.Success;
        result.CanonicalName = Name("canonical", "example");
        result.Response = response;
        result.SecurityStatus = securityStatus;
        result.HasTimeToLive = true;
        result.TimeToLive = timeToLive;
        DnsResourceRecordSet set = new DnsResourceRecordSet();
        set.Name = result.CanonicalName;
        set.RecordType = recordType;
        set.RecordTypeValue = (int)recordType;
        set.Class = DnsClass.Internet;
        set.ClassValue = 1;
        set.TimeToLive = timeToLive;
        set.Records.AddRange(records);
        result.RecordSets.Add(set);
        return result;
    }

    static DnsResourceRecord MailRecord(int preference, string target)
    {
        DnsResourceRecord result = Record(DnsRecordType.Mx);
        result.Data = new DnsRecordData();
        result.Data.PreferenceName = new DnsPreferenceNameData();
        result.Data.PreferenceName.Preference = preference;
        result.Data.PreferenceName.Name = Name(target, "example");
        return result;
    }

    static DnsResourceRecord TextRecord(params byte[][] strings)
    {
        DnsResourceRecord result = Record(DnsRecordType.Txt);
        result.Data = new DnsRecordData();
        result.Data.CharacterStrings = new DnsCharacterStringsData();
        result.Data.CharacterStrings.Strings.AddRange(strings);
        return result;
    }

    static DnsResourceRecord ServiceRecord(
        int priority,
        int weight,
        int port,
        string target = "")
    {
        DnsResourceRecord result = Record(DnsRecordType.Srv);
        result.Data = new DnsRecordData();
        result.Data.Srv = new DnsSrvData();
        result.Data.Srv.Priority = priority;
        result.Data.Srv.Weight = weight;
        result.Data.Srv.Port = port;
        if (target.Length == 0)
        {
            result.Data.Srv.Target = Name();
        }
        else
        {
            result.Data.Srv.Target = Name(target, "example");
        }

        return result;
    }

    static DnsResourceRecord Record(DnsRecordType recordType)
    {
        DnsResourceRecord result = new DnsResourceRecord();
        result.Name = Name("owner", "example");
        result.RecordType = recordType;
        result.RecordTypeValue = (int)recordType;
        result.Class = DnsClass.Internet;
        result.ClassValue = 1;
        result.TimeToLive = 100;
        return result;
    }

    static DnsResolveResult AddressResult(
        DnsResolveRequest request,
        bool ipv4)
    {
        if (!ipv4)
        {
            DnsResolveResult noData = new DnsResolveResult();
            noData.Status = DnsResolveStatus.NoData;
            noData.CanonicalName = request.Name;
            return noData;
        }

        byte value = request.Name.Labels.Count == 0 ?
            (byte)1 : request.Name.Labels[0][0];
        DnsResourceRecord record = Record(DnsRecordType.A);
        record.Name = request.Name;
        record.TimeToLive = 30;
        record.Data = new DnsRecordData();
        record.Data.Address = new DnsAddressData();
        record.Data.Address.Address = [192, 0, 2, value];
        DnsResolveResult result = Result(
            DnsRecordType.A,
            30,
            DnsSecurityStatus.Insecure,
            new DnsMessage(),
            record);
        result.CanonicalName = request.Name;
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

    class ScriptedResolver : IDnsResolver
    {
        internal DnsResolveResult Pointer = new DnsResolveResult();
        internal DnsResolveResult Mail = new DnsResolveResult();
        internal DnsResolveResult Text = new DnsResolveResult();
        internal DnsResolveResult Service = new DnsResolveResult();
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
            if (request.RecordTypeValue == 12)
            {
                return Pointer;
            }

            if (request.RecordTypeValue == 15)
            {
                return Mail;
            }

            if (request.RecordTypeValue == 16)
            {
                return Text;
            }

            if (request.RecordTypeValue == 33)
            {
                return Service;
            }

            return AddressResult(request, request.RecordTypeValue == 1);
        }
    }
}
