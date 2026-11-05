using System.Text;

namespace dns.Tests.Api;

public class DnsAuthoritativeServiceTests
{
    [Test]
    public void BuildsEncodedAuthoritativeBaseResponseAndEchoesQuestionPolicy()
    {
        MemoryDnsZoneStore store = new MemoryDnsZoneStore();
        Assert.That(store.AddZone(Zone(
            "example", DnsClass.Internet, 1)).Valid, Is.True);
        DnsAuthoritativeService service = new DnsAuthoritativeService(store);
        DnsAuthoritativeRequest request = Request(
            "www.example", DnsRecordType.A, 1);
        request.Message.Header.Identifier = 0x1234;
        request.Message.Header.Flags = DnsHeaderFlag.RecursionDesired;
        request.Message.Header.FlagsValue =
            (int)DnsHeaderFlag.RecursionDesired;
        request.Options.RecursionAvailable = true;

        DnsAuthoritativeResult result =
            service.AnswerAuthoritatively(request);
        request.Message.Questions[0].Name.Labels[0][0] = (byte)'x';

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Kind,
                Is.EqualTo(DnsAuthoritativeAnswerKind.NameError));
            Assert.That(result.ResponseCode,
                Is.EqualTo(DnsResponseCode.NameError));
            Assert.That(result.Authoritative, Is.True);
            Assert.That(Text(result.Zone!), Is.EqualTo("example."));
            Assert.That(result.ResponseBytes, Is.Not.Empty);
            Assert.That(result.Response, Is.Not.Null);
            Assert.That(result.Response!.Header.Identifier, Is.EqualTo(0x1234));
            Assert.That(result.Response.Header.OpcodeValue, Is.EqualTo(0));
            Assert.That(result.Response.Header.QuestionCount, Is.EqualTo(1));
            Assert.That(result.Response.Header.AnswerCount, Is.EqualTo(0));
            Assert.That(result.Response.Header.AuthorityCount, Is.EqualTo(1));
            Assert.That(Text(result.Response.Questions[0].Name),
                Is.EqualTo("www.example."));
            Assert.That(result.Response.Header.FlagsValue &
                (int)DnsHeaderFlag.Response, Is.Not.Zero);
            Assert.That(result.Response.Header.FlagsValue &
                (int)DnsHeaderFlag.AuthoritativeAnswer, Is.Not.Zero);
            Assert.That(result.Response.Header.FlagsValue &
                (int)DnsHeaderFlag.RecursionDesired, Is.Not.Zero);
            Assert.That(result.Response.Header.FlagsValue &
                (int)DnsHeaderFlag.RecursionAvailable, Is.Not.Zero);
        });
    }

    [Test]
    public void SelectsLongestZoneByClassAndPreservesUnknownQuestionType()
    {
        MemoryDnsZoneStore store = new MemoryDnsZoneStore();
        Assert.That(store.AddZone(Zone(
            "example", DnsClass.Internet, 1)).Valid, Is.True);
        Assert.That(store.AddZone(Zone(
            "sub.example", DnsClass.Chaos, 3)).Valid, Is.True);
        DnsAuthoritativeService service = new DnsAuthoritativeService(store);
        DnsAuthoritativeRequest request = Request(
            "host.sub.example", (DnsRecordType)65400, 3);
        request.Message.Questions[0].RecordTypeValue = 65400;

        DnsAuthoritativeResult result =
            service.AnswerAuthoritatively(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Authoritative, Is.True);
            Assert.That(Text(result.Zone!), Is.EqualTo("sub.example."));
            Assert.That(result.Response!.Questions[0].RecordTypeValue,
                Is.EqualTo(65400));
            Assert.That(result.Response.Questions[0].ClassValue,
                Is.EqualTo(3));
        });
    }

    [Test]
    public void ReturnsFormatErrorForMalformedQuestionSectionsAndHeaderBits()
    {
        DnsAuthoritativeService service = ServiceWithExampleZone();
        DnsAuthoritativeRequest multiple = Request(
            "www.example", DnsRecordType.A, 1);
        multiple.Message.Questions.Add(Question(
            "other.example", DnsRecordType.A, 1));
        multiple.Message.Header.QuestionCount = 2;
        DnsAuthoritativeRequest responseInput = Request(
            "www.example", DnsRecordType.A, 1);
        responseInput.Message.Header.Flags = DnsHeaderFlag.Response;
        responseInput.Message.Header.FlagsValue =
            (int)DnsHeaderFlag.Response;
        DnsAuthoritativeRequest reservedType = Request(
            "www.example", DnsRecordType.Reserved, 1);
        DnsAuthoritativeRequest relativeName = Request(
            "www.example", DnsRecordType.A, 1);
        relativeName.Message.Questions[0].Name.IsAbsolute = false;
        DnsAuthoritativeRequest invalidClass = Request(
            "www.example", DnsRecordType.A, 1);
        invalidClass.Message.Questions[0].Class = (DnsClass)70000;
        invalidClass.Message.Questions[0].ClassValue = 70000;
        DnsAuthoritativeRequest invalidOpcode = Request(
            "www.example", DnsRecordType.A, 1);
        invalidOpcode.Message.Header.Opcode = (DnsOpcode)(-1);
        invalidOpcode.Message.Header.OpcodeValue = -1;

        DnsAuthoritativeResult multipleResult =
            service.AnswerAuthoritatively(multiple);
        DnsAuthoritativeResult responseResult =
            service.AnswerAuthoritatively(responseInput);
        DnsAuthoritativeResult typeResult =
            service.AnswerAuthoritatively(reservedType);
        DnsAuthoritativeResult nameResult =
            service.AnswerAuthoritatively(relativeName);
        DnsAuthoritativeResult classResult =
            service.AnswerAuthoritatively(invalidClass);
        DnsAuthoritativeResult opcodeResult =
            service.AnswerAuthoritatively(invalidOpcode);

        Assert.Multiple(() =>
        {
            AssertProtocolResult(
                multipleResult, DnsResponseCode.FormatError);
            AssertProtocolResult(
                responseResult, DnsResponseCode.FormatError);
            AssertProtocolResult(
                typeResult, DnsResponseCode.FormatError);
            AssertProtocolResult(
                nameResult, DnsResponseCode.FormatError);
            AssertProtocolResult(
                classResult, DnsResponseCode.FormatError);
            AssertProtocolResult(
                opcodeResult, DnsResponseCode.FormatError);
            Assert.That(nameResult.Response!.Questions, Is.Empty);
            Assert.That(classResult.Response!.Questions, Is.Empty);
            Assert.That(opcodeResult.Response!.Header.OpcodeValue,
                Is.EqualTo(0));
            Assert.That(multipleResult.Authoritative, Is.False);
            Assert.That(multipleResult.Response!.Header.FlagsValue &
                (int)DnsHeaderFlag.AuthoritativeAnswer, Is.Zero);
            Assert.That(multipleResult.Diagnostics, Is.Not.Empty);
        });
    }

    [Test]
    public void ReturnsNotImplementedForOtherOpcodesTransfersAndClassAny()
    {
        DnsAuthoritativeService service = ServiceWithExampleZone();
        DnsAuthoritativeRequest notify = Request(
            "example", DnsRecordType.Soa, 1);
        notify.Message.Header.Opcode = DnsOpcode.Notify;
        notify.Message.Header.OpcodeValue = 4;
        DnsAuthoritativeRequest transfer = Request(
            "example", DnsRecordType.Axfr, 1);
        DnsAuthoritativeRequest classAny = Request(
            "example", DnsRecordType.A, 255);

        DnsAuthoritativeResult notifyResult =
            service.AnswerAuthoritatively(notify);
        DnsAuthoritativeResult transferResult =
            service.AnswerAuthoritatively(transfer);
        DnsAuthoritativeResult classResult =
            service.AnswerAuthoritatively(classAny);

        Assert.Multiple(() =>
        {
            AssertProtocolResult(
                notifyResult, DnsResponseCode.NotImplemented);
            AssertProtocolResult(
                transferResult, DnsResponseCode.NotImplemented);
            AssertProtocolResult(
                classResult, DnsResponseCode.NotImplemented);
            Assert.That(notifyResult.Kind,
                Is.EqualTo(DnsAuthoritativeAnswerKind.NotImplemented));
            Assert.That(notifyResult.Response!.Header.OpcodeValue,
                Is.EqualTo(4));
            Assert.That(notifyResult.Response.Questions, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void RefusesNamesAndClassesOutsideHostedZones()
    {
        DnsAuthoritativeService service = ServiceWithExampleZone();
        DnsAuthoritativeRequest outside = Request(
            "www.other", DnsRecordType.A, 1);
        DnsAuthoritativeRequest wrongClass = Request(
            "www.example", DnsRecordType.A, 3);

        DnsAuthoritativeResult outsideResult =
            service.AnswerAuthoritatively(outside);
        DnsAuthoritativeResult classResult =
            service.AnswerAuthoritatively(wrongClass);

        Assert.Multiple(() =>
        {
            AssertProtocolResult(outsideResult, DnsResponseCode.Refused);
            AssertProtocolResult(classResult, DnsResponseCode.Refused);
            Assert.That(outsideResult.Kind,
                Is.EqualTo(DnsAuthoritativeAnswerKind.Refused));
            Assert.That(outsideResult.Authoritative, Is.False);
            Assert.That(outsideResult.Zone, Is.Null);
        });
    }

    [Test]
    public void AppliesQueryOperationNetworkAndZoneAuthorization()
    {
        DnsAuthoritativeService service = ServiceWithExampleZone();
        DnsAuthoritativeRequest disabled = Request(
            "example", DnsRecordType.Soa, 1);
        disabled.Options.Authorization.AllowQueries = false;
        DnsAuthoritativeRequest deniedNetwork = Request(
            "example", DnsRecordType.Soa, 1);
        deniedNetwork.Options.Authorization.AllowedClientAddresses.Add(
            [192, 0, 2, 0]);
        deniedNetwork.Options.Authorization.AllowedClientAddressFamilies.Add(4);
        deniedNetwork.Options.Authorization.AllowedClientPrefixLengths.Add(24);
        deniedNetwork.ClientEndpoint = Endpoint([198, 51, 100, 1]);
        DnsAuthoritativeRequest allowedNetwork = Request(
            "example", DnsRecordType.Soa, 1);
        allowedNetwork.Options.Authorization.AllowedClientAddresses.Add(
            [192, 0, 2, 0]);
        allowedNetwork.Options.Authorization.AllowedClientAddressFamilies.Add(4);
        allowedNetwork.Options.Authorization.AllowedClientPrefixLengths.Add(24);
        allowedNetwork.ClientEndpoint = Endpoint([192, 0, 2, 1]);
        DnsAuthoritativeRequest deniedZone = Request(
            "example", DnsRecordType.Soa, 1);
        deniedZone.Options.Authorization.AllowedZones.Add(Parse("other"));

        DnsAuthoritativeResult disabledResult =
            service.AnswerAuthoritatively(disabled);
        DnsAuthoritativeResult deniedNetworkResult =
            service.AnswerAuthoritatively(deniedNetwork);
        DnsAuthoritativeResult allowedNetworkResult =
            service.AnswerAuthoritatively(allowedNetwork);
        DnsAuthoritativeResult deniedZoneResult =
            service.AnswerAuthoritatively(deniedZone);

        Assert.Multiple(() =>
        {
            Assert.That(disabledResult.Kind,
                Is.EqualTo(DnsAuthoritativeAnswerKind.Refused));
            Assert.That(deniedNetworkResult.ResponseCode,
                Is.EqualTo(DnsResponseCode.Refused));
            Assert.That(allowedNetworkResult.ResponseCode,
                Is.EqualTo(DnsResponseCode.NoError));
            Assert.That(allowedNetworkResult.AnswerRecordSets,
                Has.Count.EqualTo(1));
            Assert.That(deniedZoneResult.ResponseCode,
                Is.EqualTo(DnsResponseCode.Refused));
        });
    }

    [Test]
    public void RateLimitRefusesExcessAndSlipsTruncatedResponse()
    {
        DnsAuthoritativeService service = ServiceWithExampleZone();
        DnsAuthoritativeRequest first = RateRequest(1000);
        DnsAuthoritativeRequest limited = RateRequest(1001);
        DnsAuthoritativeRequest slipped = RateRequest(1002);

        DnsAuthoritativeResult firstResult =
            service.AnswerAuthoritatively(first);
        DnsAuthoritativeResult limitedResult =
            service.AnswerAuthoritatively(limited);
        DnsAuthoritativeResult slippedResult =
            service.AnswerAuthoritatively(slipped);

        Assert.Multiple(() =>
        {
            Assert.That(firstResult.ResponseCode,
                Is.EqualTo(DnsResponseCode.NoError));
            Assert.That(firstResult.Truncated, Is.False);
            Assert.That(limitedResult.Kind,
                Is.EqualTo(DnsAuthoritativeAnswerKind.Refused));
            Assert.That(limitedResult.ResponseCode,
                Is.EqualTo(DnsResponseCode.Refused));
            Assert.That(limitedResult.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.ResourceLimitExceeded));
            Assert.That(slippedResult.ResponseCode,
                Is.EqualTo(DnsResponseCode.NoError));
            Assert.That(slippedResult.Truncated, Is.True);
            Assert.That(slippedResult.AnswerRecordSets, Is.Empty);
            Assert.That(slippedResult.Response!.Answers, Is.Empty);
            Assert.That(slippedResult.Response.Header.Flags &
                DnsHeaderFlag.Truncated,
                Is.EqualTo(DnsHeaderFlag.Truncated));
        });
    }

    [Test]
    public void StopsBeforeResponseConstructionForCancellationOrInvalidLimits()
    {
        DnsAuthoritativeService service = ServiceWithExampleZone();
        DnsAuthoritativeRequest cancelled = Request(
            "www.example", DnsRecordType.A, 1);
        cancelled.CancellationRequested = true;
        DnsAuthoritativeRequest invalidLimit = Request(
            "www.example", DnsRecordType.A, 1);
        invalidLimit.Options.MaximumUdpResponseLength = 1;

        DnsAuthoritativeResult cancelledResult =
            service.AnswerAuthoritatively(cancelled);
        DnsAuthoritativeResult limitResult =
            service.AnswerAuthoritatively(invalidLimit);

        Assert.Multiple(() =>
        {
            Assert.That(cancelledResult.Success, Is.False);
            Assert.That(cancelledResult.Response, Is.Null);
            Assert.That(cancelledResult.Diagnostics, Is.Not.Empty);
            Assert.That(limitResult.Success, Is.False);
            Assert.That(limitResult.Response, Is.Null);
            Assert.That(limitResult.Diagnostics, Is.Not.Empty);
        });
    }

    [Test]
    public void MapsResponseEncodingFailureToServerFailure()
    {
        DnsAuthoritativeRequest request = Request(
            "example", DnsRecordType.Soa, 1);
        request.Transport = DnsTransportKind.Tcp;
        request.Options.MaximumTcpResponseLength =
            DnsProtocolLimits.HeaderLength;

        DnsAuthoritativeResult result = ServiceWithExampleZone()
            .AnswerAuthoritatively(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Kind,
                Is.EqualTo(DnsAuthoritativeAnswerKind.ServerFailure));
            Assert.That(result.ResponseCode,
                Is.EqualTo(DnsResponseCode.ServerFailure));
            Assert.That(result.Response!.Header.ResponseCode,
                Is.EqualTo(DnsResponseCode.ServerFailure));
            Assert.That(result.ResponseBytes, Is.Empty);
            Assert.That(result.Diagnostics, Is.Not.Empty);
        });
    }

    static DnsAuthoritativeService ServiceWithExampleZone()
    {
        MemoryDnsZoneStore store = new MemoryDnsZoneStore();
        Assert.That(store.AddZone(Zone(
            "example", DnsClass.Internet, 1)).Valid, Is.True);
        return new DnsAuthoritativeService(store);
    }

    static DnsAuthoritativeRequest RateRequest(long monotonicMilliseconds)
    {
        DnsAuthoritativeRequest result = Request(
            "example", DnsRecordType.Soa, 1);
        result.ClientEndpoint = Endpoint([192, 0, 2, 10]);
        result.ReceivedAt.MonotonicMilliseconds = monotonicMilliseconds;
        result.Options.Authorization.RateLimitResponses = 1;
        result.Options.Authorization.RateLimitWindowMilliseconds = 100;
        result.Options.Authorization.RateLimitSlip = 2;
        return result;
    }

    static DnsEndpoint Endpoint(byte[] address)
    {
        DnsEndpoint result = new DnsEndpoint();
        result.HasAddress = true;
        result.Address = address;
        result.AddressFamily = 4;
        return result;
    }

    static DnsAuthoritativeRequest Request(
        string name,
        DnsRecordType recordType,
        int classValue)
    {
        DnsAuthoritativeRequest result = new DnsAuthoritativeRequest();
        result.Message.Header.Opcode = DnsOpcode.Query;
        result.Message.Header.OpcodeValue = 0;
        result.Message.Questions.Add(Question(name, recordType, classValue));
        result.Message.Header.QuestionCount = 1;
        return result;
    }

    static DnsQuestion Question(
        string name,
        DnsRecordType recordType,
        int classValue)
    {
        DnsQuestion result = new DnsQuestion();
        result.Name = Parse(name);
        result.RecordType = recordType;
        result.RecordTypeValue = (int)recordType;
        result.Class = (DnsClass)classValue;
        result.ClassValue = classValue;
        return result;
    }

    static DnsZone Zone(
        string apexText,
        DnsClass recordClass,
        int classValue)
    {
        DnsZone result = new DnsZone();
        result.Apex = Parse(apexText);
        result.Class = recordClass;
        result.ClassValue = classValue;
        DnsZoneNode apex = new DnsZoneNode();
        apex.Name = Parse(apexText);
        apex.RecordSets.Add(SoaSet(
            apexText, recordClass, classValue));
        apex.RecordSets.Add(NameServerSet(
            apexText, recordClass, classValue));
        result.Nodes.Add(apex);
        return result;
    }

    static DnsResourceRecordSet SoaSet(
        string name,
        DnsClass recordClass,
        int classValue)
    {
        DnsResourceRecordSet set = Set(
            name, DnsRecordType.Soa, recordClass, classValue);
        DnsResourceRecord record = Record(
            name, DnsRecordType.Soa, recordClass, classValue);
        record.Data = new DnsRecordData();
        record.Data.Soa = new DnsSoaData();
        record.Data.Soa.PrimaryNameServer = Name("ns", "outside");
        record.Data.Soa.ResponsibleMailbox = Name("hostmaster", "outside");
        record.Data.Soa.Serial = 1;
        record.Data.Soa.Refresh = 3600;
        record.Data.Soa.Retry = 600;
        record.Data.Soa.Expire = 86400;
        record.Data.Soa.Minimum = 300;
        set.Records.Add(record);
        return set;
    }

    static DnsResourceRecordSet NameServerSet(
        string name,
        DnsClass recordClass,
        int classValue)
    {
        DnsResourceRecordSet set = Set(
            name, DnsRecordType.Ns, recordClass, classValue);
        DnsResourceRecord record = Record(
            name, DnsRecordType.Ns, recordClass, classValue);
        record.Data = new DnsRecordData();
        record.Data.Name = new DnsNameData();
        record.Data.Name.Name = Name("ns", "outside");
        set.Records.Add(record);
        return set;
    }

    static DnsResourceRecordSet Set(
        string name,
        DnsRecordType recordType,
        DnsClass recordClass,
        int classValue)
    {
        DnsResourceRecordSet result = new DnsResourceRecordSet();
        result.Name = Parse(name);
        result.RecordType = recordType;
        result.RecordTypeValue = (int)recordType;
        result.Class = recordClass;
        result.ClassValue = classValue;
        result.TimeToLive = 300;
        return result;
    }

    static DnsResourceRecord Record(
        string name,
        DnsRecordType recordType,
        DnsClass recordClass,
        int classValue)
    {
        DnsResourceRecord result = new DnsResourceRecord();
        result.Name = Parse(name);
        result.RecordType = recordType;
        result.RecordTypeValue = (int)recordType;
        result.Class = recordClass;
        result.ClassValue = classValue;
        result.TimeToLive = 300;
        return result;
    }

    static void AssertProtocolResult(
        DnsAuthoritativeResult result,
        DnsResponseCode responseCode)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.ResponseCode, Is.EqualTo(responseCode));
            Assert.That(result.Response, Is.Not.Null);
            Assert.That(result.ResponseBytes, Is.Not.Empty);
            Assert.That(result.Response!.Header.ResponseCode,
                Is.EqualTo(responseCode));
            Assert.That(result.Response.Header.FlagsValue &
                (int)DnsHeaderFlag.Response, Is.Not.Zero);
        });
    }

    static DnsName Parse(string text)
    {
        return Name(text.Split('.'));
    }

    static DnsName Name(params string[] labels)
    {
        DnsName result = new DnsName();
        result.IsAbsolute = true;
        foreach (string label in labels)
        {
            if (label.Length > 0)
            {
                result.Labels.Add(Encoding.ASCII.GetBytes(label));
            }
        }

        return result;
    }

    static string Text(DnsName name)
    {
        if (name.Labels.Count == 0)
        {
            return ".";
        }

        return string.Join(
            ".",
            name.Labels.Select(Encoding.ASCII.GetString)) + ".";
    }
}
