using System.Text;

namespace dns.Tests.Api;

public class DnsNotifyServiceTests
{
    [TestCase(DnsResponseCode.NoError)]
    [TestCase(DnsResponseCode.NotImplemented)]
    public void SendsConstructedNotifyAndAcceptsTerminalAcknowledgement(
        DnsResponseCode responseCode)
    {
        MemoryDnsZoneStore store = Store(10);
        ScriptedQueryClient queries = new ScriptedQueryClient();
        queries.NotifyResponseCode = responseCode;
        RecordingTransferClient transfers = new RecordingTransferClient();
        DnsNotifyRequest request = Request();
        request.HasSerial = true;
        request.Serial = 11;

        DnsNotifyResult result = new DnsNotifyService(
            store,
            queries,
            transfers).SendNotify(request);
        DnsQueryRequest sent = queries.Requests.Single();
        DnsMessage message = sent.Message!;

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Acknowledged, Is.True);
            Assert.That(result.ResponseCode, Is.EqualTo(responseCode));
            Assert.That(result.Attempts, Has.Count.EqualTo(1));
            Assert.That(message.Header.Opcode, Is.EqualTo(DnsOpcode.Notify));
            Assert.That(message.Header.Flags,
                Is.EqualTo(DnsHeaderFlag.AuthoritativeAnswer));
            Assert.That(message.Questions, Has.Count.EqualTo(1));
            Assert.That(message.Questions[0].RecordType,
                Is.EqualTo(DnsRecordType.Soa));
            Assert.That(message.Answers, Has.Count.EqualTo(1));
            Assert.That(message.Answers[0].Data!.Soa!.Serial,
                Is.EqualTo(11));
            Assert.That(sent.RecursionDesired, Is.False);
            Assert.That(sent.UseEdns, Is.False);
            Assert.That(sent.Servers[0].Address,
                Is.EqualTo(request.Endpoint.Address));
            Assert.That(transfers.Requests, Is.Empty);
        });
    }

    [Test]
    public void PreservesConfiguredRetryAttemptsForUnacknowledgedTarget()
    {
        MemoryDnsZoneStore store = Store(15);
        ScriptedQueryClient queries = new ScriptedQueryClient();
        queries.NotifyResponseCode = DnsResponseCode.ServerFailure;
        queries.NotifyAttemptCount = 3;
        RecordingTransferClient transfers = new RecordingTransferClient();
        DnsNotifyRequest request = Request();
        request.TransportOptions.MaximumAttempts = 3;

        DnsNotifyResult result = new DnsNotifyService(
            store,
            queries,
            transfers).SendNotify(request);
        DnsQueryRequest sent = queries.Requests.Single();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Acknowledged, Is.False);
            Assert.That(result.ResponseCode,
                Is.EqualTo(DnsResponseCode.ServerFailure));
            Assert.That(result.Attempts, Has.Count.EqualTo(3));
            Assert.That(result.Attempts.Select(attempt => attempt.Status),
                Is.EqualTo(new[]
                {
                    DnsTransportStatus.TimedOut,
                    DnsTransportStatus.TimedOut,
                    DnsTransportStatus.Success
                }));
            Assert.That(sent.TransportOptions.MaximumAttempts, Is.EqualTo(3));
            Assert.That(sent.Servers.Single().Address,
                Is.EqualTo(request.Endpoint.Address));
            Assert.That(transfers.Requests, Is.Empty);
        });
    }

    [Test]
    public void ProcessesNewerAuthorizedNotifyAndTriggersIxfrFromLocalSerial()
    {
        MemoryDnsZoneStore store = Store(20);
        ScriptedQueryClient queries = new ScriptedQueryClient();
        queries.SoaSerial = 25;
        RecordingTransferClient transfers = new RecordingTransferClient();
        DnsNotifyRequest request = Request();
        request.HasSerial = true;
        request.Serial = 24;
        AddAllowedPrimary(request.Authorization, request.Endpoint);

        DnsNotifyResult result = new DnsNotifyService(
            store,
            queries,
            transfers).ProcessNotify(request);
        DnsQueryRequest soaQuery = queries.Requests.Single();
        DnsTransferRequest transfer = transfers.Requests.Single();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Acknowledged, Is.True);
            Assert.That(result.ResponseCode, Is.EqualTo(DnsResponseCode.NoError));
            Assert.That(result.Response!.Header.Opcode,
                Is.EqualTo(DnsOpcode.Notify));
            Assert.That(result.Response.Header.Flags,
                Is.EqualTo(DnsHeaderFlag.Response |
                    DnsHeaderFlag.AuthoritativeAnswer));
            Assert.That(result.TransferRequired, Is.True);
            Assert.That(result.TransferTriggered, Is.True);
            Assert.That(result.Transfer, Is.SameAs(transfers.Result));
            Assert.That(soaQuery.Question.RecordType,
                Is.EqualTo(DnsRecordType.Soa));
            Assert.That(soaQuery.RecursionDesired, Is.False);
            Assert.That(soaQuery.Servers[0].Address,
                Is.EqualTo(request.Endpoint.Address));
            Assert.That(transfer.Kind, Is.EqualTo(DnsTransferKind.Ixfr));
            Assert.That(transfer.HasCurrentSerial, Is.True);
            Assert.That(transfer.CurrentSerial, Is.EqualTo(20));
            Assert.That(Text(transfer.Zone), Is.EqualTo("example."));
        });
    }

    [Test]
    public void EqualHintSuppressesRefreshAndStalePrimarySuppressesTransfer()
    {
        MemoryDnsZoneStore equalStore = Store(30);
        ScriptedQueryClient equalQueries = new ScriptedQueryClient();
        RecordingTransferClient equalTransfers =
            new RecordingTransferClient();
        DnsNotifyRequest equal = Request();
        equal.HasSerial = true;
        equal.Serial = 30;
        DnsNotifyResult equalResult = new DnsNotifyService(
            equalStore,
            equalQueries,
            equalTransfers).ProcessNotify(equal);

        MemoryDnsZoneStore staleStore = Store(30);
        ScriptedQueryClient staleQueries = new ScriptedQueryClient();
        staleQueries.SoaSerial = 29;
        RecordingTransferClient staleTransfers =
            new RecordingTransferClient();
        DnsNotifyResult staleResult = new DnsNotifyService(
            staleStore,
            staleQueries,
            staleTransfers).ProcessNotify(Request());

        Assert.Multiple(() =>
        {
            Assert.That(equalResult.Acknowledged, Is.True);
            Assert.That(equalResult.TransferRequired, Is.False);
            Assert.That(equalQueries.Requests, Is.Empty);
            Assert.That(equalTransfers.Requests, Is.Empty);
            Assert.That(staleResult.Acknowledged, Is.True);
            Assert.That(staleResult.TransferRequired, Is.False);
            Assert.That(staleQueries.Requests, Has.Count.EqualTo(1));
            Assert.That(staleTransfers.Requests, Is.Empty);
        });
    }

    [Test]
    public void EnforcesSecondaryAndPrimaryAuthorizationBeforeNetworkWork()
    {
        MemoryDnsZoneStore store = Store(40);
        ScriptedQueryClient sendQueries = new ScriptedQueryClient();
        RecordingTransferClient sendTransfers =
            new RecordingTransferClient();
        DnsNotifyRequest disabledSend = Request();
        disabledSend.Authorization.AllowNotify = false;
        DnsNotifyResult send = new DnsNotifyService(
            store,
            sendQueries,
            sendTransfers).SendNotify(disabledSend);

        ScriptedQueryClient processQueries = new ScriptedQueryClient();
        RecordingTransferClient processTransfers =
            new RecordingTransferClient();
        DnsNotifyRequest wrongPrimary = Request();
        DnsEndpoint another = Endpoint([198, 51, 100, 1]);
        AddAllowedPrimary(wrongPrimary.Authorization, another);
        DnsNotifyResult process = new DnsNotifyService(
            store,
            processQueries,
            processTransfers).ProcessNotify(wrongPrimary);

        Assert.Multiple(() =>
        {
            Assert.That(send.Success, Is.True);
            Assert.That(send.Acknowledged, Is.False);
            Assert.That(send.ResponseCode, Is.EqualTo(DnsResponseCode.Refused));
            Assert.That(sendQueries.Requests, Is.Empty);
            Assert.That(process.Success, Is.True);
            Assert.That(process.Acknowledged, Is.False);
            Assert.That(process.ResponseCode,
                Is.EqualTo(DnsResponseCode.Refused));
            Assert.That(processQueries.Requests, Is.Empty);
            Assert.That(processTransfers.Requests, Is.Empty);
        });
    }

    [Test]
    public void AuthenticatesSignedNotifyAndSignsMatchingAcknowledgement()
    {
        MemoryDnsZoneStore store = Store(50);
        ScriptedQueryClient queries = new ScriptedQueryClient();
        RecordingTransferClient transfers = new RecordingTransferClient();
        DnsTsigKey key = Key();
        DnsClockValue now = TransportTime.ReadClock();
        DnsMessage message = NotifyMessage(0x4321, 50);
        DnsTsigResult signed = new DnsTsigService().SignTsig(
            message,
            key,
            [],
            now);
        DnsNotifyRequest request = Request();
        request.Message = signed.Message;
        request.TsigKey = key;
        request.Authorization.RequireTsigForNotify = true;
        request.Authorization.AllowedTsigKeyNames.Add(Parse("notify-key"));

        DnsNotifyResult result = new DnsNotifyService(
            store,
            queries,
            transfers).ProcessNotify(request);
        DnsTsigResult verifiedResponse = new DnsTsigService().VerifyTsig(
            result.Response!,
            key,
            signed.Mac,
            TransportTime.ReadClock());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Acknowledged, Is.True);
            Assert.That(result.Tsig!.Authenticated, Is.True);
            Assert.That(result.Response!.Header.Identifier,
                Is.EqualTo(0x4321));
            Assert.That(result.Response.Additionals.Last().RecordType,
                Is.EqualTo(DnsRecordType.Tsig));
            Assert.That(verifiedResponse.Success, Is.True);
            Assert.That(verifiedResponse.Authenticated, Is.True);
            Assert.That(queries.Requests, Is.Empty);
            Assert.That(transfers.Requests, Is.Empty);
        });
    }

    [Test]
    public void RejectsTamperedSignedNotifyBeforeRefreshOrTransfer()
    {
        MemoryDnsZoneStore store = Store(55);
        ScriptedQueryClient queries = new ScriptedQueryClient();
        RecordingTransferClient transfers = new RecordingTransferClient();
        DnsTsigKey key = Key();
        DnsTsigResult signed = new DnsTsigService().SignTsig(
            NotifyMessage(8, 56),
            key,
            [],
            TransportTime.ReadClock());
        signed.Message!.Answers[0].Data!.Soa!.Serial = 57;
        DnsNotifyRequest request = Request();
        request.Message = signed.Message;
        request.TsigKey = key;
        request.Authorization.RequireTsigForNotify = true;

        DnsNotifyResult result = new DnsNotifyService(
            store,
            queries,
            transfers).ProcessNotify(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.ResponseCode,
                Is.EqualTo(DnsResponseCode.NotAuthoritative));
            Assert.That(result.Acknowledged, Is.False);
            Assert.That(result.Tsig!.Error,
                Is.EqualTo(DnsTsigError.BadSignature));
            Assert.That(queries.Requests, Is.Empty);
            Assert.That(transfers.Requests, Is.Empty);
        });
    }

    [Test]
    public void ComparesPrimarySerialForwardAcrossUint32Wrap()
    {
        MemoryDnsZoneStore store = Store(uint.MaxValue);
        ScriptedQueryClient queries = new ScriptedQueryClient();
        queries.SoaSerial = 0;
        RecordingTransferClient transfers = new RecordingTransferClient();

        DnsNotifyResult result = new DnsNotifyService(
            store,
            queries,
            transfers).ProcessNotify(Request());

        Assert.Multiple(() =>
        {
            Assert.That(result.TransferRequired, Is.True);
            Assert.That(result.TransferTriggered, Is.True);
            Assert.That(transfers.Requests.Single().CurrentSerial,
                Is.EqualTo(uint.MaxValue));
        });
    }

    [Test]
    public void RejectsMalformedAndUnhostedNotifyMessages()
    {
        MemoryDnsZoneStore store = Store(60);
        ScriptedQueryClient queries = new ScriptedQueryClient();
        RecordingTransferClient transfers = new RecordingTransferClient();
        DnsNotifyRequest malformed = Request();
        malformed.Message = NotifyMessage(1, 60);
        malformed.Message.Header.Opcode = DnsOpcode.Query;
        malformed.Message.Header.OpcodeValue = 0;
        DnsNotifyRequest unhosted = Request();
        unhosted.Zone = Parse("other");

        DnsNotifyService service = new DnsNotifyService(
            store,
            queries,
            transfers);
        DnsNotifyResult malformedResult = service.ProcessNotify(malformed);
        DnsNotifyResult unhostedResult = service.ProcessNotify(unhosted);

        Assert.Multiple(() =>
        {
            Assert.That(malformedResult.ResponseCode,
                Is.EqualTo(DnsResponseCode.FormatError));
            Assert.That(malformedResult.Acknowledged, Is.False);
            Assert.That(unhostedResult.ResponseCode,
                Is.EqualTo(DnsResponseCode.NotAuthoritative));
            Assert.That(unhostedResult.Acknowledged, Is.False);
            Assert.That(queries.Requests, Is.Empty);
            Assert.That(transfers.Requests, Is.Empty);
        });
    }

    static DnsNotifyRequest Request()
    {
        DnsNotifyRequest result = new DnsNotifyRequest();
        result.Zone = Parse("example");
        result.Endpoint = Endpoint([192, 0, 2, 1]);
        result.Authorization.AllowNotify = true;
        result.Authorization.RequireTsigForNotify = false;
        return result;
    }

    static void AddAllowedPrimary(
        DnsAuthorizationPolicy policy,
        DnsEndpoint endpoint)
    {
        policy.AllowedClientAddresses.Add(endpoint.Address);
        policy.AllowedClientAddressFamilies.Add(endpoint.AddressFamily);
        policy.AllowedClientPrefixLengths.Add(32);
    }

    static DnsMessage NotifyMessage(ushort identifier, uint serial)
    {
        DnsMessage result = new DnsMessage();
        result.Header.Identifier = identifier;
        result.Header.Opcode = DnsOpcode.Notify;
        result.Header.OpcodeValue = (int)DnsOpcode.Notify;
        result.Header.Flags = DnsHeaderFlag.AuthoritativeAnswer;
        result.Header.FlagsValue = (int)DnsHeaderFlag.AuthoritativeAnswer;
        result.Questions.Add(Question());
        result.Answers.Add(SoaRecord(serial));
        result.Header.QuestionCount = 1;
        result.Header.AnswerCount = 1;
        return result;
    }

    static DnsMessage NotifyResponse(
        DnsQueryRequest request,
        DnsResponseCode responseCode)
    {
        DnsMessage result = new DnsMessage();
        result.Header.Identifier = request.Message!.Header.Identifier;
        result.Header.Opcode = DnsOpcode.Notify;
        result.Header.OpcodeValue = (int)DnsOpcode.Notify;
        result.Header.Flags = DnsHeaderFlag.Response |
            DnsHeaderFlag.AuthoritativeAnswer;
        result.Header.FlagsValue = (int)result.Header.Flags;
        result.Header.ResponseCode = responseCode;
        result.Header.ResponseCodeValue = (int)responseCode;
        result.Questions.Add(CopyQuestion(request.Message.Questions[0]));
        result.Header.QuestionCount = 1;
        return result;
    }

    static DnsMessage SoaResponse(DnsQueryRequest request, uint serial)
    {
        DnsMessage result = new DnsMessage();
        result.Header.Identifier = 7;
        result.Header.Flags = DnsHeaderFlag.Response |
            DnsHeaderFlag.AuthoritativeAnswer;
        result.Header.FlagsValue = (int)result.Header.Flags;
        result.Questions.Add(CopyQuestion(request.Question));
        result.Answers.Add(SoaRecord(serial));
        result.Header.QuestionCount = 1;
        result.Header.AnswerCount = 1;
        return result;
    }

    static DnsQuestion Question()
    {
        DnsQuestion result = new DnsQuestion();
        result.Name = Parse("example");
        result.RecordType = DnsRecordType.Soa;
        result.RecordTypeValue = 6;
        result.Class = DnsClass.Internet;
        result.ClassValue = 1;
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

    static MemoryDnsZoneStore Store(uint serial)
    {
        MemoryDnsZoneStore result = new MemoryDnsZoneStore();
        DnsZone zone = new DnsZone();
        zone.Apex = Parse("example");
        zone.Class = DnsClass.Internet;
        zone.ClassValue = 1;
        DnsZoneNode apex = new DnsZoneNode();
        apex.Name = Parse("example");
        DnsResourceRecordSet soa = Set(DnsRecordType.Soa);
        soa.Records.Add(SoaRecord(serial));
        DnsResourceRecordSet ns = Set(DnsRecordType.Ns);
        DnsResourceRecord nsRecord = BaseRecord(DnsRecordType.Ns);
        nsRecord.Data = new DnsRecordData();
        nsRecord.Data.Name = new DnsNameData();
        nsRecord.Data.Name.Name = Parse("ns.outside");
        ns.Records.Add(nsRecord);
        apex.RecordSets.Add(soa);
        apex.RecordSets.Add(ns);
        zone.Nodes.Add(apex);
        Assert.That(result.AddZone(zone).Valid, Is.True);
        return result;
    }

    static DnsResourceRecordSet Set(DnsRecordType recordType)
    {
        DnsResourceRecordSet result = new DnsResourceRecordSet();
        result.Name = Parse("example");
        result.RecordType = recordType;
        result.RecordTypeValue = (int)recordType;
        result.Class = DnsClass.Internet;
        result.ClassValue = 1;
        result.TimeToLive = 300;
        return result;
    }

    static DnsResourceRecord SoaRecord(uint serial)
    {
        DnsResourceRecord result = BaseRecord(DnsRecordType.Soa);
        result.Data = new DnsRecordData();
        result.Data.Soa = new DnsSoaData();
        result.Data.Soa.PrimaryNameServer = Parse("ns.outside");
        result.Data.Soa.ResponsibleMailbox = Parse("hostmaster.outside");
        result.Data.Soa.Serial = serial;
        result.Data.Soa.Refresh = 3600;
        result.Data.Soa.Retry = 600;
        result.Data.Soa.Expire = 86400;
        result.Data.Soa.Minimum = 300;
        return result;
    }

    static DnsResourceRecord BaseRecord(DnsRecordType recordType)
    {
        DnsResourceRecord result = new DnsResourceRecord();
        result.Name = Parse("example");
        result.RecordType = recordType;
        result.RecordTypeValue = (int)recordType;
        result.Class = DnsClass.Internet;
        result.ClassValue = 1;
        result.TimeToLive = 300;
        return result;
    }

    static DnsTsigKey Key()
    {
        DnsTsigKey result = new DnsTsigKey();
        result.Name = Parse("notify-key");
        result.AlgorithmName = Parse("hmac-sha256");
        result.Secret = Encoding.ASCII.GetBytes("notify-test-secret");
        return result;
    }

    static DnsEndpoint Endpoint(byte[] address)
    {
        DnsEndpoint result = new DnsEndpoint();
        result.HasAddress = true;
        result.AddressFamily = 4;
        result.Address = address;
        result.Port = DnsPorts.Dns;
        return result;
    }

    static DnsName Parse(string text)
    {
        DnsName result = new DnsName();
        result.IsAbsolute = true;
        string[] labels = text.Split('.');
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
        return string.Join(
            ".",
            name.Labels.Select(Encoding.ASCII.GetString)) + ".";
    }

    class ScriptedQueryClient : IDnsQueryClient
    {
        public List<DnsQueryRequest> Requests = new List<DnsQueryRequest>();
        public DnsResponseCode NotifyResponseCode;
        public int NotifyAttemptCount = 1;
        public uint SoaSerial;

        public DnsQueryResult Query(DnsQueryRequest request)
        {
            this.Requests.Add(request);
            DnsMessage response;
            bool notify = request.Message != null &&
                request.Message.Header.Opcode == DnsOpcode.Notify;
            if (notify)
            {
                response = NotifyResponse(request, this.NotifyResponseCode);
            }
            else
            {
                response = SoaResponse(request, this.SoaSerial);
            }

            DnsQueryResult result = new DnsQueryResult();
            result.Success = true;
            result.Status = DnsTransportStatus.Success;
            result.Response = response;
            int attemptCount = notify ? this.NotifyAttemptCount : 1;
            int attemptIndex = 0;
            while (attemptIndex < attemptCount)
            {
                DnsTransportResult attempt = new DnsTransportResult();
                attempt.Status = attemptIndex + 1 == attemptCount ?
                    DnsTransportStatus.Success :
                    DnsTransportStatus.TimedOut;
                if (attempt.Status == DnsTransportStatus.Success)
                {
                    attempt.ResponseMessage = response;
                }

                result.Attempts.Add(attempt);
                attemptIndex = attemptIndex + 1;
            }

            result.ElapsedMilliseconds = 12;
            return result;
        }
    }

    class RecordingTransferClient : IDnsTransferClient
    {
        public List<DnsTransferRequest> Requests =
            new List<DnsTransferRequest>();
        public DnsTransferResult Result = new DnsTransferResult();

        public RecordingTransferClient()
        {
            this.Result.Success = true;
            this.Result.Kind = DnsTransferKind.Ixfr;
        }

        public DnsTransferResult ReceiveTransfer(DnsTransferRequest request)
        {
            this.Requests.Add(request);
            return this.Result;
        }
    }
}
