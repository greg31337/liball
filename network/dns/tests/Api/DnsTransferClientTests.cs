using System.Text;

namespace dns.Tests.Api;

public class DnsTransferClientTests
{
    [Test]
    public void ReceivesMultiMessageAxfrAndPublishesCompleteZone()
    {
        ScriptedAxfrProvider provider = new ScriptedAxfrProvider();
        provider.Factory = query => Exchange(
            Response(query, true, [Soa(41), NameServer()]),
            Response(query, false, [Address("www.example")]),
            Response(query, false, [Soa(41)]));
        DnsTransferClient client = Client(provider);

        DnsTransferResult result = client.ReceiveTransfer(Request());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.TransportStatus,
                Is.EqualTo(DnsTransportStatus.Success));
            Assert.That(result.Zone, Is.Not.Null);
            Assert.That(Text(result.Zone!.Apex), Is.EqualTo("example."));
            Assert.That(result.MessageCount, Is.EqualTo(3));
            Assert.That(result.RecordCount, Is.EqualTo(4));
            Assert.That(result.HasInitialSerial, Is.True);
            Assert.That(result.InitialSerial, Is.EqualTo(41));
            Assert.That(result.HasFinalSerial, Is.True);
            Assert.That(result.FinalSerial, Is.EqualTo(41));
            Assert.That(AllRecords(result.Zone), Has.Count.EqualTo(3));
            Assert.That(AllRecords(result.Zone).Count(record =>
                record.RecordType == DnsRecordType.Soa), Is.EqualTo(1));
            Assert.That(provider.Queries, Has.Count.EqualTo(1));
            Assert.That(provider.Queries[0].Questions[0].RecordType,
                Is.EqualTo(DnsRecordType.Axfr));
        });
    }

    [Test]
    public void RejectsNonTcpAndIncompleteTransfersWithoutPublishingZone()
    {
        ScriptedAxfrProvider provider = new ScriptedAxfrProvider();
        provider.Factory = query => Exchange(
            Response(query, true, [Soa(7), NameServer()]));
        DnsTransferClient client = Client(provider);
        DnsTransferRequest udp = Request();
        udp.Transport = DnsTransportKind.Udp;

        DnsTransferResult rejected = client.ReceiveTransfer(udp);
        DnsTransferResult incomplete = client.ReceiveTransfer(Request());

        Assert.Multiple(() =>
        {
            Assert.That(rejected.Success, Is.False);
            Assert.That(rejected.Zone, Is.Null);
            Assert.That(provider.Queries, Has.Count.EqualTo(1));
            Assert.That(incomplete.Success, Is.False);
            Assert.That(incomplete.Zone, Is.Null);
            Assert.That(incomplete.TransportStatus,
                Is.EqualTo(DnsTransportStatus.ProtocolError));
            Assert.That(incomplete.Diagnostics.Any(diagnostic =>
                diagnostic.Field == "transfer.soaBoundary"), Is.True);
        });
    }

    [Test]
    public void RejectsMismatchedBoundaryAndOutOfZoneRecordsAtomically()
    {
        ScriptedAxfrProvider boundaryProvider = new ScriptedAxfrProvider();
        boundaryProvider.Factory = query => Exchange(
            Response(query, true, [Soa(8), NameServer()]),
            Response(query, false, [Soa(9)]));
        ScriptedAxfrProvider ownerProvider = new ScriptedAxfrProvider();
        ownerProvider.Factory = query => Exchange(
            Response(query, true, [Soa(8), NameServer()]),
            Response(query, false, [Address("outside.test")]),
            Response(query, false, [Soa(8)]));

        DnsTransferResult boundary =
            Client(boundaryProvider).ReceiveTransfer(Request());
        DnsTransferResult owner =
            Client(ownerProvider).ReceiveTransfer(Request());

        Assert.Multiple(() =>
        {
            Assert.That(boundary.Success, Is.False);
            Assert.That(boundary.Zone, Is.Null);
            Assert.That(boundary.Diagnostics.Any(diagnostic =>
                diagnostic.Field == "transfer.closingSoa"), Is.True);
            Assert.That(owner.Success, Is.False);
            Assert.That(owner.Zone, Is.Null);
            Assert.That(owner.Diagnostics.Any(diagnostic =>
                diagnostic.Field == "transfer.record.owner"), Is.True);
        });
    }

    [Test]
    public void EnforcesMessageRecordAndByteLimits()
    {
        ScriptedAxfrProvider provider = new ScriptedAxfrProvider();
        provider.Factory = query => Exchange(
            Response(query, true, [Soa(3), NameServer()]),
            Response(query, false, [Address("www.example")]),
            Response(query, false, [Soa(3)]));
        DnsTransferRequest messageRequest = Request();
        messageRequest.MaximumMessages = 2;
        DnsTransferRequest recordRequest = Request();
        recordRequest.MaximumRecords = 3;
        DnsTransferRequest byteRequest = Request();
        byteRequest.MaximumBytes = 12;

        DnsTransferResult messages =
            Client(provider).ReceiveTransfer(messageRequest);
        DnsTransferResult records =
            Client(provider).ReceiveTransfer(recordRequest);
        DnsTransferResult bytes =
            Client(provider).ReceiveTransfer(byteRequest);

        Assert.Multiple(() =>
        {
            Assert.That(messages.Success, Is.False);
            Assert.That(messages.TransportStatus,
                Is.EqualTo(DnsTransportStatus.ResponseTooLarge));
            Assert.That(records.Success, Is.False);
            Assert.That(records.TransportStatus,
                Is.EqualTo(DnsTransportStatus.ResponseTooLarge));
            Assert.That(bytes.Success, Is.False);
            Assert.That(bytes.TransportStatus,
                Is.EqualTo(DnsTransportStatus.ResponseTooLarge));
            Assert.That(messages.Zone, Is.Null);
            Assert.That(records.Zone, Is.Null);
            Assert.That(bytes.Zone, Is.Null);
        });
    }

    [Test]
    public void VerifiesSignedFirstAndLastMessagesAcrossUnsignedMiddle()
    {
        DnsTsigKey key = Key();
        ScriptedAxfrProvider provider = new ScriptedAxfrProvider();
        provider.Factory = query => SignedExchange(query, key, false);
        DnsTransferRequest request = Request();
        request.TsigKey = key;

        DnsTransferResult result =
            Client(provider).ReceiveTransfer(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Zone, Is.Not.Null);
            Assert.That(result.Tsig, Is.Not.Null);
            Assert.That(result.Tsig!.Success, Is.True);
            Assert.That(result.Tsig.Authenticated, Is.True);
            Assert.That(provider.Queries[0].Additionals.Last().RecordType,
                Is.EqualTo(DnsRecordType.Tsig));
        });
    }

    [Test]
    public void RejectsTamperedTsigChainWithoutPublishingZone()
    {
        DnsTsigKey key = Key();
        ScriptedAxfrProvider provider = new ScriptedAxfrProvider();
        provider.Factory = query => SignedExchange(query, key, true);
        DnsTransferRequest request = Request();
        request.TsigKey = key;

        DnsTransferResult result =
            Client(provider).ReceiveTransfer(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Zone, Is.Null);
            Assert.That(result.TransportStatus,
                Is.EqualTo(DnsTransportStatus.AuthenticationFailed));
            Assert.That(result.Tsig, Is.Not.Null);
            Assert.That(result.Tsig!.Error,
                Is.EqualTo(DnsTsigError.BadSignature));
        });
    }

    static DnsTransferClient Client(ScriptedAxfrProvider provider)
    {
        DnsTransferClient result = new DnsTransferClient();
        result.ExchangeProvider = provider;
        return result;
    }

    static DnsTransferRequest Request()
    {
        DnsTransferRequest result = new DnsTransferRequest();
        result.Zone = Parse("example");
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
        DnsTsigKey key,
        bool tamper)
    {
        byte[] requestMac = query.Additionals.Last().Data!.Tsig!.Mac;
        DnsMessage first = Response(
            query,
            true,
            [Soa(55), NameServer()]);
        DnsMessage middle = Response(
            query,
            false,
            [Address("www.example")]);
        DnsMessage final = Response(query, false, [Soa(55)]);
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
        if (tamper)
        {
            signedFinal.Message!.Answers[0].Data!.Soa!.Minimum++;
        }

        return Exchange(signedFirst.Message!, middle, signedFinal.Message!);
    }

    static DnsMessage Response(
        DnsMessage query,
        bool includeQuestion,
        DnsResourceRecord[] answers)
    {
        DnsMessage result = new DnsMessage();
        result.Header.Identifier = query.Header.Identifier;
        result.Header.Opcode = DnsOpcode.Query;
        result.Header.OpcodeValue = (int)DnsOpcode.Query;
        result.Header.Flags = DnsHeaderFlag.Response |
            DnsHeaderFlag.AuthoritativeAnswer;
        result.Header.FlagsValue = (int)result.Header.Flags;
        if (includeQuestion)
        {
            result.Questions.Add(CopyQuestion(query.Questions[0]));
        }

        result.Answers.AddRange(answers);
        result.Header.QuestionCount = result.Questions.Count;
        result.Header.AnswerCount = result.Answers.Count;
        return result;
    }

    static DnsQuestion CopyQuestion(DnsQuestion source)
    {
        DnsQuestion result = new DnsQuestion();
        result.Name = Parse(Text(source.Name).TrimEnd('.'));
        result.RecordType = source.RecordType;
        result.RecordTypeValue = source.RecordTypeValue;
        result.Class = source.Class;
        result.ClassValue = source.ClassValue;
        return result;
    }

    static DnsResourceRecord Soa(uint serial)
    {
        DnsResourceRecord result = BaseRecord("example", DnsRecordType.Soa);
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
        DnsResourceRecord result = BaseRecord("example", DnsRecordType.Ns);
        result.Data = new DnsRecordData();
        result.Data.Name = new DnsNameData();
        result.Data.Name.Name = Parse("ns.example");
        return result;
    }

    static DnsResourceRecord Address(string name)
    {
        DnsResourceRecord result = BaseRecord(name, DnsRecordType.A);
        result.Data = new DnsRecordData();
        result.Data.Address = new DnsAddressData();
        result.Data.Address.Address = [192, 0, 2, 10];
        return result;
    }

    static DnsResourceRecord BaseRecord(
        string name,
        DnsRecordType type)
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
        result.Secret = Encoding.ASCII.GetBytes("axfr-test-secret");
        return result;
    }

    static List<DnsResourceRecord> AllRecords(DnsZone zone)
    {
        return zone.Nodes
            .SelectMany(node => node.RecordSets)
            .SelectMany(set => set.Records)
            .ToList();
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

    class ScriptedAxfrProvider : IAxfrExchangeProvider
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
