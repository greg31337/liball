using System.Text;

namespace dns.Tests.Api;

public class DnsIxfrServiceTests
{
    [Test]
    public void StreamsCoveredJournalAsIxfrDeleteAndAddSequence()
    {
        MemoryDnsZoneStore store = Store(
            12,
            [Entry(10, 12, "old.example", "new.example")]);
        DnsTransferRequest request = Request(10);

        DnsTransferResult result =
            new DnsTransferService(store).ServeTransfer(request);
        List<DnsResourceRecord> records = result.Messages
            .SelectMany(message => message.Answers)
            .ToList();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.UpToDate, Is.False);
            Assert.That(result.FellBackToAxfr, Is.False);
            Assert.That(result.JournalEntries, Has.Count.EqualTo(1));
            Assert.That(result.JournalEntries[0].FromSerial, Is.EqualTo(10));
            Assert.That(result.JournalEntries[0].ToSerial, Is.EqualTo(12));
            Assert.That(records.Select(record =>
                record.RecordType == DnsRecordType.Soa ?
                    record.Data!.Soa!.Serial : uint.MaxValue),
                Is.EqualTo(new uint[] {
                    12,
                    10,
                    uint.MaxValue,
                    12,
                    uint.MaxValue,
                    12
                }));
            Assert.That(Text(records[2].Name), Is.EqualTo("old.example."));
            Assert.That(Text(records[4].Name), Is.EqualTo("new.example."));
            Assert.That(result.Zone, Is.Null);
        });
    }

    [Test]
    public void CoveredJournalRoundTripsThroughIxfrClient()
    {
        MemoryDnsZoneStore store = Store(
            12,
            [Entry(10, 12, "old.example", "new.example")]);
        DnsTransferService service = new DnsTransferService(store);
        DnsTransferClient client = new DnsTransferClient();
        client.ExchangeProvider = new ServiceIxfrProvider(service);

        DnsTransferResult result = client.ReceiveTransfer(Request(10));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.FellBackToAxfr, Is.False);
            Assert.That(result.JournalEntries, Has.Count.EqualTo(1));
            Assert.That(result.JournalEntries[0].FromSerial, Is.EqualTo(10));
            Assert.That(result.JournalEntries[0].ToSerial, Is.EqualTo(12));
            Assert.That(result.JournalEntries[0].Transaction.Changes
                .Select(change => change.Kind), Is.EqualTo(new[] {
                    DnsZoneChangeKind.DeleteRecord,
                    DnsZoneChangeKind.Add
                }));
        });
    }

    [Test]
    public void RoundTripsJournalDeltaAcrossUint32SerialWraparound()
    {
        MemoryDnsZoneStore store = Store(
            0,
            [Entry(
                uint.MaxValue,
                0,
                "old.example",
                "new.example")]);
        DnsTransferService service = new DnsTransferService(store);
        DnsTransferClient client = new DnsTransferClient();
        client.ExchangeProvider = new ServiceIxfrProvider(service);

        DnsTransferResult result = client.ReceiveTransfer(
            Request(uint.MaxValue));
        DnsZoneJournalEntry entry = result.JournalEntries.Single();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.FellBackToAxfr, Is.False);
            Assert.That(result.InitialSerial, Is.EqualTo(uint.MaxValue));
            Assert.That(result.FinalSerial, Is.Zero);
            Assert.That(entry.FromSerial, Is.EqualTo(uint.MaxValue));
            Assert.That(entry.ToSerial, Is.Zero);
            Assert.That(entry.Transaction.Atomic, Is.True);
            Assert.That(entry.Transaction.Committed, Is.True);
            Assert.That(entry.Transaction.Changes.Select(
                change => change.Kind), Is.EqualTo(new[]
                {
                    DnsZoneChangeKind.DeleteRecord,
                    DnsZoneChangeKind.Add
                }));
        });
    }

    [Test]
    public void ReturnsSingleCurrentSoaWhenRequesterIsUpToDate()
    {
        MemoryDnsZoneStore store = Store(
            12,
            [Entry(10, 12, "old.example", "new.example")]);

        DnsTransferResult result =
            new DnsTransferService(store).ServeTransfer(Request(12));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.UpToDate, Is.True);
            Assert.That(result.FellBackToAxfr, Is.False);
            Assert.That(result.RecordCount, Is.EqualTo(1));
            Assert.That(result.Messages.SelectMany(message => message.Answers)
                .ToList(),
                Has.Count.EqualTo(1));
            Assert.That(result.Messages[0].Answers[0].Data!.Soa!.Serial,
                Is.EqualTo(12));
            Assert.That(result.JournalEntries, Is.Empty);
        });
    }

    [Test]
    public void SelectsJournalSuffixBeginningAtRequestedSerial()
    {
        MemoryDnsZoneStore store = Store(
            14,
            [
                Entry(10, 12, "old-a.example", "new-a.example"),
                Entry(12, 14, "old-b.example", "new-b.example")
            ]);

        DnsTransferResult result =
            new DnsTransferService(store).ServeTransfer(Request(12));
        List<DnsResourceRecord> records = result.Messages
            .SelectMany(message => message.Answers)
            .ToList();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.JournalEntries, Has.Count.EqualTo(1));
            Assert.That(result.JournalEntries[0].FromSerial, Is.EqualTo(12));
            Assert.That(result.JournalEntries[0].ToSerial, Is.EqualTo(14));
            Assert.That(records.Any(record =>
                Text(record.Name) == "old-a.example."), Is.False);
            Assert.That(records.Any(record =>
                Text(record.Name) == "old-b.example."), Is.True);
            Assert.That(result.InitialSerial, Is.EqualTo(12));
            Assert.That(result.FinalSerial, Is.EqualTo(14));
        });
    }

    [Test]
    public void FallsBackToAxfrWhenJournalCoverageIsUnavailable()
    {
        MemoryDnsZoneStore store = Store(20, []);
        DnsTransferService service = new DnsTransferService(store);
        DnsTransferClient client = new DnsTransferClient();
        client.ExchangeProvider = new ServiceIxfrProvider(service);

        DnsTransferResult result = client.ReceiveTransfer(Request(10));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.FellBackToAxfr, Is.True);
            Assert.That(result.Zone, Is.Not.Null);
            Assert.That(result.JournalEntries, Is.Empty);
            Assert.That(result.InitialSerial, Is.EqualTo(10));
            Assert.That(result.FinalSerial, Is.EqualTo(20));
            Assert.That(result.Zone!.Nodes.SelectMany(node => node.RecordSets)
                .SelectMany(set => set.Records)
                .Any(record => record.RecordType == DnsRecordType.Ns),
                Is.True);
        });
    }

    [Test]
    public void FallsBackWhenJournalDoesNotRetainDeletedSetContents()
    {
        DnsZoneJournalEntry entry = Entry(
            10,
            12,
            "old.example",
            "new.example");
        entry.Transaction.Changes.Clear();
        DnsZoneChange deleteSet = new DnsZoneChange();
        deleteSet.Kind = DnsZoneChangeKind.DeleteRecordSet;
        deleteSet.Name = Parse("old.example");
        deleteSet.RecordType = DnsRecordType.A;
        deleteSet.RecordTypeValue = (int)DnsRecordType.A;
        deleteSet.Class = DnsClass.Internet;
        deleteSet.ClassValue = 1;
        entry.Transaction.Changes.Add(deleteSet);
        MemoryDnsZoneStore store = Store(12, [entry]);

        DnsTransferResult result =
            new DnsTransferService(store).ServeTransfer(Request(10));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.FellBackToAxfr, Is.True);
            Assert.That(result.Zone, Is.Not.Null);
            Assert.That(result.JournalEntries, Is.Empty);
        });
    }

    [Test]
    public void AuthenticatesRequestAndSignsPartitionedIxfrChain()
    {
        MemoryDnsZoneStore store = Store(
            12,
            [Entry(10, 12, "old.example", "new.example")]);
        DnsTsigKey key = Key();
        DnsMessage query = Query(10, 0x5151);
        DnsTsigResult signedRequest = new DnsTsigService().SignTsig(
            query,
            key,
            [],
            TransportTime.ReadClock());
        DnsTransferRequest request = Request(10);
        request.Message = signedRequest.Message;
        request.TsigKey = key;
        request.Authorization.RequireTsigForTransfers = true;
        request.Authorization.AllowedTsigKeyNames.Add(Parse("transfer-key"));
        request.TransportOptions.MaximumResponseLength = 220;

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
            Assert.That(verified.All(item => item.Success &&
                item.Authenticated), Is.True);
            Assert.That(completed.Success, Is.True);
            Assert.That(completed.Authenticated, Is.True);
            Assert.That(result.Tsig, Is.Not.Null);
            Assert.That(result.Tsig!.Authenticated, Is.True);
        });
    }

    [Test]
    public void RejectsDisabledAndCancelledIxfrWithoutPartialStream()
    {
        MemoryDnsZoneStore store = Store(
            12,
            [Entry(10, 12, "old.example", "new.example")]);
        DnsTransferService service = new DnsTransferService(store);
        DnsTransferRequest disabledRequest = Request(10);
        disabledRequest.Authorization.AllowTransfers = false;
        DnsTransferRequest cancelledRequest = Request(10);
        cancelledRequest.CancellationRequested = true;

        DnsTransferResult disabled = service.ServeTransfer(disabledRequest);
        DnsTransferResult cancelled = service.ServeTransfer(cancelledRequest);

        Assert.Multiple(() =>
        {
            Assert.That(disabled.Success, Is.False);
            Assert.That(disabled.ResponseCode,
                Is.EqualTo(DnsResponseCode.Refused));
            Assert.That(disabled.Messages, Has.Count.EqualTo(1));
            Assert.That(disabled.Messages[0].Answers, Is.Empty);
            Assert.That(disabled.JournalEntries, Is.Empty);
            Assert.That(cancelled.Success, Is.False);
            Assert.That(cancelled.TransportStatus,
                Is.EqualTo(DnsTransportStatus.Cancelled));
            Assert.That(cancelled.Messages, Is.Empty);
            Assert.That(cancelled.JournalEntries, Is.Empty);
            Assert.That(cancelled.Zone, Is.Null);
        });
    }

    static DnsTransferRequest Request(uint serial)
    {
        DnsTransferRequest result = new DnsTransferRequest();
        result.Kind = DnsTransferKind.Ixfr;
        result.Zone = Parse("example");
        result.HasCurrentSerial = true;
        result.CurrentSerial = serial;
        result.Endpoint.HasAddress = true;
        result.Endpoint.AddressFamily = 4;
        result.Endpoint.Address = [192, 0, 2, 60];
        result.Authorization.AllowTransfers = true;
        result.Authorization.RequireTsigForTransfers = false;
        return result;
    }

    static DnsMessage Query(uint serial, ushort identifier)
    {
        DnsMessage result = new DnsMessage();
        result.Header.Identifier = identifier;
        DnsQuestion question = new DnsQuestion();
        question.Name = Parse("example");
        question.RecordType = DnsRecordType.Ixfr;
        question.RecordTypeValue = (int)DnsRecordType.Ixfr;
        question.Class = DnsClass.Internet;
        question.ClassValue = 1;
        result.Questions.Add(question);
        result.Authorities.Add(Soa(serial));
        result.Header.QuestionCount = 1;
        result.Header.AuthorityCount = 1;
        return result;
    }

    static MemoryDnsZoneStore Store(
        uint serial,
        DnsZoneJournalEntry[] journal)
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
        nameServer.RecordSets.Add(Set(Address("ns.example", 53)));
        zone.Nodes.Add(nameServer);
        DnsZoneNode current = Node("new.example");
        current.RecordSets.Add(Set(Address("new.example", 2)));
        zone.Nodes.Add(current);
        foreach (DnsZoneJournalEntry entry in journal)
        {
            zone.JournalEntries.Add(entry);
        }

        Assert.That(result.AddZone(zone).Valid, Is.True);
        return result;
    }

    static DnsZoneJournalEntry Entry(
        uint fromSerial,
        uint toSerial,
        string deletedName,
        string addedName)
    {
        DnsZoneJournalEntry result = new DnsZoneJournalEntry();
        result.Sequence = toSerial;
        result.FromSerial = fromSerial;
        result.ToSerial = toSerial;
        result.Transaction.Atomic = true;
        result.Transaction.Committed = true;
        result.Transaction.HasExpectedSerial = true;
        result.Transaction.ExpectedSerial = fromSerial;
        result.Transaction.HasResultingSerial = true;
        result.Transaction.ResultingSerial = toSerial;
        result.Transaction.Changes.Add(Change(
            DnsZoneChangeKind.DeleteRecord,
            Address(deletedName, 1)));
        result.Transaction.Changes.Add(Change(
            DnsZoneChangeKind.Add,
            Address(addedName, 2)));
        return result;
    }

    static DnsZoneChange Change(
        DnsZoneChangeKind kind,
        DnsResourceRecord record)
    {
        DnsZoneChange result = new DnsZoneChange();
        result.Kind = kind;
        result.Name = record.Name;
        result.RecordType = record.RecordType;
        result.RecordTypeValue = record.RecordTypeValue;
        result.Class = record.Class;
        result.ClassValue = record.ClassValue;
        result.Record = record;
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

    static DnsResourceRecord Address(string name, int value)
    {
        DnsResourceRecord result = Record(name, DnsRecordType.A);
        result.Data = new DnsRecordData();
        result.Data.Address = new DnsAddressData();
        result.Data.Address.Address = [192, 0, 2, (byte)value];
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
        result.Secret = Encoding.ASCII.GetBytes("ixfr-server-secret");
        return result;
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

    class ServiceIxfrProvider : IAxfrExchangeProvider
    {
        public DnsTransferService Service;

        public ServiceIxfrProvider(DnsTransferService service)
        {
            this.Service = service;
        }

        public DnsAxfrExchangeResult ReceiveMessages(
            DnsTransferRequest clientRequest,
            DnsMessage query,
            byte[] queryBytes,
            DnsClockValue deadline)
        {
            DnsTransferRequest serverRequest = Request(
                clientRequest.CurrentSerial);
            serverRequest.Message = query;
            serverRequest.TsigKey = clientRequest.TsigKey;
            if (clientRequest.TsigKey != null)
            {
                serverRequest.Authorization.RequireTsigForTransfers = true;
            }

            DnsTransferResult served = this.Service.ServeTransfer(
                serverRequest);
            DnsAxfrExchangeResult result = new DnsAxfrExchangeResult();
            result.Status = served.Success ?
                DnsTransportStatus.Success :
                DnsTransportStatus.ProtocolError;
            foreach (DnsMessage message in served.Messages)
            {
                result.Messages.Add(message);
            }

            result.ByteCount = served.ByteCount;
            return result;
        }
    }
}
