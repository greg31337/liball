using System.Text;

namespace dns.Tests.Api;

public class DnsIxfrClientTests
{
    [Test]
    public void RecognizesUpToDateResponseAndBuildsIxfrAuthoritySoa()
    {
        ScriptedIxfrProvider provider = new ScriptedIxfrProvider();
        provider.Factory = query => Exchange(
            Response(query, true, [Soa(10)]));

        DnsTransferResult result =
            Client(provider).ReceiveTransfer(Request(10));
        DnsMessage query = provider.Queries.Single();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.UpToDate, Is.True);
            Assert.That(result.FellBackToAxfr, Is.False);
            Assert.That(result.JournalEntries, Is.Empty);
            Assert.That(result.Zone, Is.Null);
            Assert.That(result.InitialSerial, Is.EqualTo(10));
            Assert.That(result.FinalSerial, Is.EqualTo(10));
            Assert.That(query.Questions[0].RecordType,
                Is.EqualTo(DnsRecordType.Ixfr));
            Assert.That(query.Authorities, Has.Count.EqualTo(1));
            Assert.That(query.Authorities[0].Data!.Soa!.Serial,
                Is.EqualTo(10));
        });
    }

    [Test]
    public void ProducesAtomicDeleteAndAddJournalDelta()
    {
        ScriptedIxfrProvider provider = new ScriptedIxfrProvider();
        provider.Factory = query => Exchange(
            Response(query, true, [
                Soa(12),
                Soa(10),
                Address("old.example", 1)]),
            Response(query, false, [
                Soa(12),
                Address("new.example", 2),
                Soa(12)]));

        DnsTransferResult result =
            Client(provider).ReceiveTransfer(Request(10));
        DnsZoneJournalEntry entry = result.JournalEntries.Single();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.UpToDate, Is.False);
            Assert.That(result.FellBackToAxfr, Is.False);
            Assert.That(result.Zone, Is.Null);
            Assert.That(result.InitialSerial, Is.EqualTo(10));
            Assert.That(result.FinalSerial, Is.EqualTo(12));
            Assert.That(entry.FromSerial, Is.EqualTo(10));
            Assert.That(entry.ToSerial, Is.EqualTo(12));
            Assert.That(entry.Transaction.Atomic, Is.True);
            Assert.That(entry.Transaction.Committed, Is.True);
            Assert.That(entry.Transaction.ExpectedSerial, Is.EqualTo(10));
            Assert.That(entry.Transaction.ResultingSerial, Is.EqualTo(12));
            Assert.That(entry.Transaction.Changes.Select(change => change.Kind),
                Is.EqualTo(new[] {
                    DnsZoneChangeKind.DeleteRecord,
                    DnsZoneChangeKind.Add
                }));
            Assert.That(Text(entry.Transaction.Changes[0].Record!.Name),
                Is.EqualTo("old.example."));
            Assert.That(Text(entry.Transaction.Changes[1].Record!.Name),
                Is.EqualTo("new.example."));
        });
    }

    [Test]
    public void AcceptsContinuousMultiDeltaSerialChain()
    {
        ScriptedIxfrProvider provider = new ScriptedIxfrProvider();
        provider.Factory = query => Exchange(Response(query, true, [
            Soa(14),
            Soa(10),
            Address("old-a.example", 1),
            Soa(12),
            Address("new-a.example", 2),
            Soa(12),
            Address("old-b.example", 3),
            Soa(14),
            Address("new-b.example", 4),
            Soa(14)]));

        DnsTransferResult result =
            Client(provider).ReceiveTransfer(Request(10));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.JournalEntries, Has.Count.EqualTo(2));
            Assert.That(result.JournalEntries[0].FromSerial, Is.EqualTo(10));
            Assert.That(result.JournalEntries[0].ToSerial, Is.EqualTo(12));
            Assert.That(result.JournalEntries[1].FromSerial, Is.EqualTo(12));
            Assert.That(result.JournalEntries[1].ToSerial, Is.EqualTo(14));
            Assert.That(result.JournalEntries.All(entry =>
                entry.Transaction.Committed), Is.True);
            Assert.That(result.FinalSerial, Is.EqualTo(14));
        });
    }

    [Test]
    public void RejectsBrokenSerialChainWithoutPublishingJournalEntries()
    {
        ScriptedIxfrProvider provider = new ScriptedIxfrProvider();
        provider.Factory = query => Exchange(Response(query, true, [
            Soa(14),
            Soa(11),
            Address("old.example", 1),
            Soa(14),
            Address("new.example", 2),
            Soa(14)]));

        DnsTransferResult result =
            Client(provider).ReceiveTransfer(Request(10));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.JournalEntries, Is.Empty);
            Assert.That(result.Zone, Is.Null);
            Assert.That(result.TransportStatus,
                Is.EqualTo(DnsTransportStatus.ProtocolError));
            Assert.That(result.Diagnostics.Any(diagnostic =>
                diagnostic.Field == "transfer.delta.oldSoa"), Is.True);
        });
    }

    [Test]
    public void AcceptsValidatedAxfrFallbackAtomically()
    {
        ScriptedIxfrProvider provider = new ScriptedIxfrProvider();
        provider.Factory = query => Exchange(
            Response(query, true, [Soa(20), NameServer()]),
            Response(query, false, [
                Address("ns.example", 53),
                Address("www.example", 80),
                Soa(20)]));

        DnsTransferResult result =
            Client(provider).ReceiveTransfer(Request(10));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.FellBackToAxfr, Is.True);
            Assert.That(result.UpToDate, Is.False);
            Assert.That(result.Zone, Is.Not.Null);
            Assert.That(result.JournalEntries, Is.Empty);
            Assert.That(result.InitialSerial, Is.EqualTo(10));
            Assert.That(result.FinalSerial, Is.EqualTo(20));
            Assert.That(result.Zone!.Nodes.SelectMany(node => node.RecordSets)
                .SelectMany(set => set.Records)
                .Count(record => record.RecordType == DnsRecordType.Soa),
                Is.EqualTo(1));
        });
    }

    [Test]
    public void VerifiesTsigAcrossIxfrDeltaMessages()
    {
        DnsTsigKey key = Key();
        ScriptedIxfrProvider provider = new ScriptedIxfrProvider();
        provider.Factory = query => SignedExchange(query, key);
        DnsTransferRequest request = Request(10);
        request.TsigKey = key;

        DnsTransferResult result =
            Client(provider).ReceiveTransfer(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.JournalEntries, Has.Count.EqualTo(1));
            Assert.That(result.Tsig, Is.Not.Null);
            Assert.That(result.Tsig!.Success, Is.True);
            Assert.That(result.Tsig.Authenticated, Is.True);
            Assert.That(provider.Queries[0].Additionals.Last().RecordType,
                Is.EqualTo(DnsRecordType.Tsig));
        });
    }

    static DnsTransferClient Client(ScriptedIxfrProvider provider)
    {
        DnsTransferClient result = new DnsTransferClient();
        result.ExchangeProvider = provider;
        return result;
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
        result.Endpoint.Address = [192, 0, 2, 53];
        result.TransportOptions.Kind = DnsTransportKind.Tcp;
        return result;
    }

    static DnsAxfrExchangeResult Exchange(params DnsMessage[] messages)
    {
        DnsAxfrExchangeResult result = new DnsAxfrExchangeResult();
        result.Status = DnsTransportStatus.Success;
        foreach (DnsMessage message in messages)
        {
            DnsEncodeResult encoded = new DnsMessageCodec().EncodeMessage(
                message,
                new DnsEncodeOptions());
            Assert.That(encoded.Success, Is.True);
            result.Messages.Add(message);
            result.ByteCount += encoded.BytesWritten;
        }

        return result;
    }

    static DnsAxfrExchangeResult SignedExchange(
        DnsMessage query,
        DnsTsigKey key)
    {
        byte[] requestMac = query.Additionals.Last().Data!.Tsig!.Mac;
        DnsMessage first = Response(query, true, [
            Soa(12),
            Soa(10),
            Address("old.example", 1)]);
        DnsMessage middle = Response(query, false, [
            Soa(12),
            Address("new.example", 2)]);
        DnsMessage final = Response(query, false, [Soa(12)]);
        DnsClockValue now = TransportTime.ReadClock();
        DnsTsigResult signedFirst = TsigOperations.Sign(
            first,
            key,
            requestMac,
            now);
        DnsEncodeResult encodedMiddle = new DnsMessageCodec().EncodeMessage(
            middle,
            new DnsEncodeOptions());
        DnsTsigResult signedFinal = TsigOperations.SignChained(
            final,
            key,
            signedFirst.Mac,
            [encodedMiddle.Bytes],
            now);
        Assert.That(signedFirst.Success, Is.True);
        Assert.That(signedFinal.Success, Is.True);
        return Exchange(signedFirst.Message!, middle, signedFinal.Message!);
    }

    static DnsMessage Response(
        DnsMessage query,
        bool includeQuestion,
        DnsResourceRecord[] answers)
    {
        DnsMessage result = new DnsMessage();
        result.Header.Identifier = query.Header.Identifier;
        result.Header.Flags = DnsHeaderFlag.Response |
            DnsHeaderFlag.AuthoritativeAnswer;
        result.Header.FlagsValue = (int)result.Header.Flags;
        if (includeQuestion)
        {
            DnsQuestion source = query.Questions[0];
            DnsQuestion question = new DnsQuestion();
            question.Name = Parse(Text(source.Name).TrimEnd('.'));
            question.RecordType = source.RecordType;
            question.RecordTypeValue = source.RecordTypeValue;
            question.Class = source.Class;
            question.ClassValue = source.ClassValue;
            result.Questions.Add(question);
        }

        result.Answers.AddRange(answers);
        result.Header.QuestionCount = result.Questions.Count;
        result.Header.AnswerCount = result.Answers.Count;
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
        result.Secret = Encoding.ASCII.GetBytes("ixfr-test-secret");
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

    class ScriptedIxfrProvider : IAxfrExchangeProvider
    {
        public Func<DnsMessage, DnsAxfrExchangeResult> Factory =
            query => new DnsAxfrExchangeResult();
        public List<DnsMessage> Queries = new List<DnsMessage>();

        public DnsAxfrExchangeResult ReceiveMessages(
            DnsTransferRequest request,
            DnsMessage query,
            byte[] queryBytes,
            DnsClockValue deadline)
        {
            this.Queries.Add(query);
            return this.Factory(query);
        }
    }
}
