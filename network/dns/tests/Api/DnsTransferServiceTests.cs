using System.Text;

namespace dns.Tests.Api;

public class DnsTransferServiceTests
{
    [Test]
    public void StreamsSoaBoundedZoneAcrossSizeLimitedMessages()
    {
        MemoryDnsZoneStore store = Store(12);
        DnsTransferRequest request = Request();
        request.TransportOptions.MaximumResponseLength = 150;

        DnsTransferResult result =
            new DnsTransferService(store).ServeTransfer(request);
        List<DnsResourceRecord> records = result.Messages
            .SelectMany(message => message.Answers)
            .ToList();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.TransportStatus,
                Is.EqualTo(DnsTransportStatus.Success));
            Assert.That(result.Messages.Count, Is.GreaterThan(1));
            Assert.That(result.MessageCount, Is.EqualTo(result.Messages.Count));
            Assert.That(result.RecordCount, Is.EqualTo(records.Count));
            Assert.That(records.First().RecordType,
                Is.EqualTo(DnsRecordType.Soa));
            Assert.That(records.Last().RecordType,
                Is.EqualTo(DnsRecordType.Soa));
            Assert.That(records.First().Data!.Soa!.Serial, Is.EqualTo(12));
            Assert.That(records.Last().Data!.Soa!.Serial, Is.EqualTo(12));
            Assert.That(result.Messages[0].Questions, Has.Count.EqualTo(1));
            Assert.That(result.Messages.Skip(1).All(message =>
                message.Questions.Count == 0), Is.True);
            Assert.That(result.Messages.All(message =>
                EncodedLength(message) <= 150), Is.True);
            Assert.That(result.Zone, Is.Not.Null);
            Assert.That(Text(result.ZoneName!), Is.EqualTo("example."));
            Assert.That(result.HasInitialSerial, Is.True);
            Assert.That(result.HasFinalSerial, Is.True);
        });
    }

    [Test]
    public void HandlesEmptyStoreMinimalZoneAndCancellationWithoutPartialData()
    {
        DnsTransferResult unhosted = new DnsTransferService(
            new MemoryDnsZoneStore()).ServeTransfer(Request());
        DnsTransferResult minimal = new DnsTransferService(
            Store(15, 0)).ServeTransfer(Request());
        DnsTransferRequest cancelledRequest = Request();
        cancelledRequest.CancellationRequested = true;
        DnsTransferResult cancelled = new DnsTransferService(
            Store(16)).ServeTransfer(cancelledRequest);
        List<DnsResourceRecord> minimalRecords = minimal.Messages
            .SelectMany(message => message.Answers)
            .ToList();

        Assert.Multiple(() =>
        {
            Assert.That(unhosted.Success, Is.False);
            Assert.That(unhosted.ResponseCode,
                Is.EqualTo(DnsResponseCode.NotAuthoritative));
            Assert.That(unhosted.Messages, Has.Count.EqualTo(1));
            Assert.That(unhosted.Messages[0].Answers, Is.Empty);
            Assert.That(unhosted.Zone, Is.Null);
            Assert.That(minimal.Success, Is.True);
            Assert.That(minimal.Messages, Has.Count.EqualTo(1));
            Assert.That(minimalRecords, Has.Count.EqualTo(4));
            Assert.That(minimalRecords.First().RecordType,
                Is.EqualTo(DnsRecordType.Soa));
            Assert.That(minimalRecords.Last().RecordType,
                Is.EqualTo(DnsRecordType.Soa));
            Assert.That(cancelled.Success, Is.False);
            Assert.That(cancelled.TransportStatus,
                Is.EqualTo(DnsTransportStatus.Cancelled));
            Assert.That(cancelled.Messages, Is.Empty);
            Assert.That(cancelled.Zone, Is.Null);
        });
    }

    [Test]
    public void RefusesDisabledTransferWithoutExposingZoneRecords()
    {
        MemoryDnsZoneStore store = Store(20);
        DnsTransferRequest request = Request();
        request.Authorization.AllowTransfers = false;

        DnsTransferResult result =
            new DnsTransferService(store).ServeTransfer(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ResponseCode,
                Is.EqualTo(DnsResponseCode.Refused));
            Assert.That(result.Messages, Has.Count.EqualTo(1));
            Assert.That(result.Messages[0].Answers, Is.Empty);
            Assert.That(result.Messages[0].Header.ResponseCode,
                Is.EqualTo(DnsResponseCode.Refused));
            Assert.That(result.Zone, Is.Null);
        });
    }

    [Test]
    public void SignsEveryPartitionInOneVerifiedTsigChain()
    {
        MemoryDnsZoneStore store = Store(30);
        DnsTsigKey key = Key();
        DnsMessage query = Query(0x4242);
        DnsTsigResult signedRequest = new DnsTsigService().SignTsig(
            query,
            key,
            [],
            TransportTime.ReadClock());
        DnsTransferRequest request = Request();
        request.Message = signedRequest.Message;
        request.TsigKey = key;
        request.Authorization.RequireTsigForTransfers = true;
        request.Authorization.AllowedTsigKeyNames.Add(Parse("transfer-key"));
        request.TransportOptions.MaximumResponseLength = 250;

        DnsTransferResult result =
            new DnsTransferService(store).ServeTransfer(request);
        DnsTsigStreamState state = TsigStreamOperations.CreateState(
            key,
            signedRequest.Mac);
        List<DnsTsigResult> verified = result.Messages.Select(message =>
            TsigStreamOperations.VerifyNext(
                state,
                message,
                TransportTime.ReadClock())).ToList();
        DnsTsigResult completed = TsigStreamOperations.Complete(state);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Messages.Count, Is.GreaterThan(1));
            Assert.That(result.Messages.All(message =>
                message.Additionals.Last().RecordType == DnsRecordType.Tsig),
                Is.True);
            Assert.That(verified.All(item => item.Success &&
                item.Authenticated), Is.True);
            Assert.That(completed.Success, Is.True);
            Assert.That(completed.Authenticated, Is.True);
            Assert.That(result.Tsig, Is.Not.Null);
            Assert.That(result.Tsig!.Authenticated, Is.True);
        });
    }

    [Test]
    public void RejectsTamperedSignedRequestBeforeReadingZoneData()
    {
        MemoryDnsZoneStore store = Store(40);
        DnsTsigKey key = Key();
        DnsTsigResult signed = new DnsTsigService().SignTsig(
            Query(9),
            key,
            [],
            TransportTime.ReadClock());
        signed.Message!.Questions[0].Name = Parse("other.example");
        DnsTransferRequest request = Request();
        request.Message = signed.Message;
        request.TsigKey = key;
        request.Authorization.RequireTsigForTransfers = true;

        DnsTransferResult result =
            new DnsTransferService(store).ServeTransfer(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.TransportStatus,
                Is.EqualTo(DnsTransportStatus.AuthenticationFailed));
            Assert.That(result.ResponseCode,
                Is.EqualTo(DnsResponseCode.NotAuthoritative));
            Assert.That(result.Messages[0].Answers, Is.Empty);
            Assert.That(result.Zone, Is.Null);
            Assert.That(result.Tsig, Is.Not.Null);
            Assert.That(result.Tsig!.Error,
                Is.EqualTo(DnsTsigError.BadSignature));
        });
    }

    [Test]
    public void RejectsTransferLimitsWithoutReturningPartialStream()
    {
        MemoryDnsZoneStore store = Store(50);
        DnsTransferService service = new DnsTransferService(store);
        DnsTransferRequest recordsRequest = Request();
        recordsRequest.MaximumRecords = 3;
        DnsTransferRequest messagesRequest = Request();
        messagesRequest.TransportOptions.MaximumResponseLength = 150;
        messagesRequest.MaximumMessages = 1;
        DnsTransferRequest bytesRequest = Request();
        bytesRequest.MaximumBytes = 60;

        DnsTransferResult records = service.ServeTransfer(recordsRequest);
        DnsTransferResult messages = service.ServeTransfer(messagesRequest);
        DnsTransferResult bytes = service.ServeTransfer(bytesRequest);

        Assert.Multiple(() =>
        {
            Assert.That(records.Success, Is.False);
            Assert.That(records.Messages, Is.Empty);
            Assert.That(records.TransportStatus,
                Is.EqualTo(DnsTransportStatus.ResponseTooLarge));
            Assert.That(messages.Success, Is.False);
            Assert.That(messages.Messages, Is.Empty);
            Assert.That(messages.TransportStatus,
                Is.EqualTo(DnsTransportStatus.ResponseTooLarge));
            Assert.That(bytes.Success, Is.False);
            Assert.That(bytes.Messages, Is.Empty);
            Assert.That(bytes.TransportStatus,
                Is.EqualTo(DnsTransportStatus.ResponseTooLarge));
        });
    }

    static DnsTransferRequest Request()
    {
        DnsTransferRequest result = new DnsTransferRequest();
        result.Zone = Parse("example");
        result.Endpoint.HasAddress = true;
        result.Endpoint.AddressFamily = 4;
        result.Endpoint.Address = [192, 0, 2, 25];
        result.Authorization.AllowTransfers = true;
        result.Authorization.RequireTsigForTransfers = false;
        return result;
    }

    static DnsMessage Query(ushort identifier)
    {
        DnsMessage result = new DnsMessage();
        result.Header.Identifier = identifier;
        DnsQuestion question = new DnsQuestion();
        question.Name = Parse("example");
        question.RecordType = DnsRecordType.Axfr;
        question.RecordTypeValue = (int)DnsRecordType.Axfr;
        question.Class = DnsClass.Internet;
        question.ClassValue = 1;
        result.Questions.Add(question);
        result.Header.QuestionCount = 1;
        return result;
    }

    static MemoryDnsZoneStore Store(uint serial)
    {
        return Store(serial, 8);
    }

    static MemoryDnsZoneStore Store(uint serial, int hostCount)
    {
        MemoryDnsZoneStore result = new MemoryDnsZoneStore();
        DnsZone zone = new DnsZone();
        zone.Apex = Parse("example");
        zone.Class = DnsClass.Internet;
        zone.ClassValue = 1;
        DnsZoneNode apex = Node("example");
        apex.RecordSets.Add(Set(Soa(serial)));
        apex.RecordSets.Add(Set(NameServer()));
        zone.Nodes.Add(apex);
        DnsZoneNode nameServer = Node("ns.example");
        nameServer.RecordSets.Add(Set(Address("ns.example", 1)));
        zone.Nodes.Add(nameServer);
        int index = 0;
        while (index < hostCount)
        {
            string name = $"host-{index}.example";
            DnsZoneNode node = Node(name);
            node.RecordSets.Add(Set(Address(name, index + 10)));
            zone.Nodes.Add(node);
            index = index + 1;
        }

        Assert.That(result.AddZone(zone).Valid, Is.True);
        return result;
    }

    static DnsZoneNode Node(string name)
    {
        DnsZoneNode result = new DnsZoneNode();
        result.Name = Parse(name);
        return result;
    }

    static DnsResourceRecordSet Set(DnsResourceRecord record)
    {
        DnsResourceRecordSet result = new DnsResourceRecordSet();
        result.Name = record.Name;
        result.RecordType = record.RecordType;
        result.RecordTypeValue = record.RecordTypeValue;
        result.Class = record.Class;
        result.ClassValue = record.ClassValue;
        result.TimeToLive = record.TimeToLive;
        result.Records.Add(record);
        return result;
    }

    static DnsResourceRecord Soa(uint serial)
    {
        DnsResourceRecord result = Record("example", DnsRecordType.Soa);
        result.Data = new DnsRecordData();
        result.Data.Soa = new DnsSoaData();
        result.Data.Soa.PrimaryNameServer = Parse("ns.example");
        result.Data.Soa.ResponsibleMailbox = Parse("hostmaster.example");
        result.Data.Soa.Serial = serial;
        result.Data.Soa.Refresh = 3600;
        result.Data.Soa.Retry = 600;
        result.Data.Soa.Expire = 86400;
        result.Data.Soa.Minimum = 300;
        return result;
    }

    static DnsResourceRecord NameServer()
    {
        DnsResourceRecord result = Record("example", DnsRecordType.Ns);
        result.Data = new DnsRecordData();
        result.Data.Name = new DnsNameData();
        result.Data.Name.Name = Parse("ns.example");
        return result;
    }

    static DnsResourceRecord Address(string name, int lastOctet)
    {
        DnsResourceRecord result = Record(name, DnsRecordType.A);
        result.Data = new DnsRecordData();
        result.Data.Address = new DnsAddressData();
        result.Data.Address.Address = [192, 0, 2, (byte)lastOctet];
        return result;
    }

    static DnsResourceRecord Record(string name, DnsRecordType type)
    {
        DnsResourceRecord result = new DnsResourceRecord();
        result.Name = Parse(name);
        result.RecordType = type;
        result.RecordTypeValue = (int)type;
        result.Class = DnsClass.Internet;
        result.ClassValue = 1;
        result.TimeToLive = 300;
        return result;
    }

    static DnsTsigKey Key()
    {
        DnsTsigKey result = new DnsTsigKey();
        result.Name = Parse("transfer-key");
        result.AlgorithmName = Parse("hmac-sha256");
        result.Secret = Encoding.ASCII.GetBytes("server-axfr-secret");
        return result;
    }

    static int EncodedLength(DnsMessage message)
    {
        DnsEncodeResult encoded = new DnsMessageCodec().EncodeMessage(
            message,
            new DnsEncodeOptions());
        Assert.That(encoded.Success, Is.True);
        return encoded.BytesWritten;
    }

    static DnsName Parse(string text)
    {
        DnsName result = new DnsName();
        result.IsAbsolute = true;
        foreach (string label in text.Split('.'))
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
        return string.Join(
            ".",
            name.Labels.Select(Encoding.ASCII.GetString)) + ".";
    }
}
