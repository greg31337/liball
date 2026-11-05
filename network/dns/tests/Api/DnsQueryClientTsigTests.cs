using System.Text;

namespace dns.Tests.Api;

public class DnsQueryClientTsigTests
{
    [Test]
    public void SignsQueryAndRequiresAuthenticatedResponse()
    {
        DnsTsigKey key = Key();
        TsigResponseTransport transport = new TsigResponseTransport();
        transport.Key = key;
        DnsQueryClient client = Client(transport);
        DnsQueryRequest request = Request(key);

        DnsQueryResult result = client.Query(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Status, Is.EqualTo(DnsTransportStatus.Success));
            Assert.That(result.Tsig, Is.Not.Null);
            Assert.That(result.Tsig!.Authenticated, Is.True);
            Assert.That(result.Tsig.Error, Is.EqualTo(DnsTsigError.NoError));
            Assert.That(transport.QueryAuthentication, Is.Not.Null);
            Assert.That(transport.QueryAuthentication!.Authenticated, Is.True);
            Assert.That(transport.Request!.QueryMessage!.Additionals[^1]
                    .RecordType,
                Is.EqualTo(DnsRecordType.Tsig));
        });
    }

    [Test]
    public void RejectsUnsignedResponseToSignedQuery()
    {
        DnsTsigKey key = Key();
        TsigResponseTransport transport = new TsigResponseTransport();
        transport.Key = key;
        transport.ReturnUnsignedResponse = true;
        DnsQueryClient client = Client(transport);
        DnsQueryRequest request = Request(key);

        DnsQueryResult result = client.Query(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Status,
                Is.EqualTo(DnsTransportStatus.ProtocolError));
            Assert.That(result.Tsig, Is.Not.Null);
            Assert.That(result.Tsig!.Authenticated, Is.False);
            Assert.That(result.Diagnostics.Any(item =>
                item.Code == DnsDiagnosticCode.InvalidTsigPlacement),
                Is.True);
        });
    }

    [Test]
    public void ResignsQueryAfterAuthenticatedEdnsDowngrade()
    {
        DnsTsigKey key = Key();
        TsigResponseTransport transport = new TsigResponseTransport();
        transport.Key = key;
        transport.ReturnFormatErrorOnce = true;
        DnsQueryClient client = Client(transport);
        DnsQueryRequest request = Request(key);
        request.UseEdns = true;
        request.TransportOptions.MaximumAttempts = 2;

        DnsQueryResult result = client.Query(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.EdnsDowngraded, Is.True);
            Assert.That(result.Attempts, Has.Count.EqualTo(2));
            Assert.That(transport.Requests[0].QueryMessage!.Additionals.Any(
                item => item.RecordType == DnsRecordType.Opt), Is.True);
            Assert.That(transport.Requests[1].QueryMessage!.Additionals.Any(
                item => item.RecordType == DnsRecordType.Opt), Is.False);
            Assert.That(transport.Requests.All(item =>
                item.QueryMessage!.Additionals[^1].RecordType ==
                    DnsRecordType.Tsig), Is.True);
            Assert.That(transport.Authentications.All(item =>
                item.Authenticated), Is.True);
        });
    }

    static DnsQueryClient Client(IDnsTransport transport)
    {
        DnsQueryClient result = new DnsQueryClient();
        result.NextServerOffset = 0;
        result.UdpTransport = transport;
        return result;
    }

    static DnsQueryRequest Request(DnsTsigKey key)
    {
        DnsQueryRequest result = new DnsQueryRequest();
        result.Question.Name = Name("signed", "example");
        result.Question.RecordType = DnsRecordType.A;
        result.Question.RecordTypeValue = 1;
        result.Question.Class = DnsClass.Internet;
        result.Question.ClassValue = 1;
        result.TsigKey = key;
        result.UseEdns = false;
        result.TransportOptions.Kind = DnsTransportKind.Udp;
        result.TransportOptions.MaximumAttempts = 1;
        result.TransportOptions.AttemptTimeoutMilliseconds = 1000;
        result.TransportOptions.TotalTimeoutMilliseconds = 5000;
        DnsEndpoint endpoint = new DnsEndpoint();
        endpoint.HasAddress = true;
        endpoint.AddressFamily = 4;
        endpoint.Address = [127, 0, 0, 1];
        endpoint.Port = 5300;
        result.Servers.Add(endpoint);
        return result;
    }

    static DnsTsigKey Key()
    {
        DnsTsigKey result = new DnsTsigKey();
        result.Name = Name("query-key", "example");
        result.AlgorithmName = Name("hmac-sha256");
        result.Secret = Encoding.ASCII.GetBytes("query secret material");
        return result;
    }

    static DnsName Name(params string[] labels)
    {
        DnsName result = new DnsName();
        result.IsAbsolute = true;
        foreach (string label in labels)
        {
            result.Labels.Add(Encoding.ASCII.GetBytes(label));
        }

        return result;
    }

    class TsigResponseTransport : IDnsTransport
    {
        public DnsTsigKey Key = new DnsTsigKey();
        public bool ReturnUnsignedResponse;
        public bool ReturnFormatErrorOnce;
        public DnsTransportRequest? Request;
        public DnsTsigResult? QueryAuthentication;
        public List<DnsTransportRequest> Requests =
            new List<DnsTransportRequest>();
        public List<DnsTsigResult> Authentications =
            new List<DnsTsigResult>();

        public DnsTransportResult Exchange(DnsTransportRequest request)
        {
            this.Request = request;
            this.Requests.Add(request);
            DnsClockValue now = TransportTime.ReadClock();
            this.QueryAuthentication = TsigOperations.Verify(
                request.QueryMessage!,
                this.Key,
                [],
                now);
            Assert.That(this.QueryAuthentication.Authenticated, Is.True);
            this.Authentications.Add(this.QueryAuthentication);
            DnsMessage response = new DnsMessage();
            response.Header.Identifier =
                request.QueryMessage!.Header.Identifier;
            response.Header.Flags = DnsHeaderFlag.Response |
                DnsHeaderFlag.RecursionAvailable;
            response.Header.FlagsValue = (int)response.Header.Flags;
            if (this.ReturnFormatErrorOnce && this.Requests.Count == 1)
            {
                response.Header.ResponseCode = DnsResponseCode.FormatError;
                response.Header.ResponseCodeValue =
                    (int)DnsResponseCode.FormatError;
            }

            response.Questions = MessageOperations.CloneMessage(
                request.QueryMessage).Questions;
            if (!this.ReturnUnsignedResponse)
            {
                DnsTsigResult signed = TsigOperations.Sign(
                    response,
                    this.Key,
                    this.QueryAuthentication.Mac,
                    now);
                response = signed.Message!;
            }

            DnsEncodeResult encoded = DnsMessageEncoder.EncodeMessage(
                response,
                new DnsEncodeOptions());
            Assert.That(encoded.Success, Is.True);
            DnsTransportResult result = new DnsTransportResult();
            result.Status = DnsTransportStatus.Success;
            result.Endpoint = request.Endpoint;
            result.Transport = request.Options.Kind;
            result.ResponseMessage = response;
            result.ResponseBytes = encoded.Bytes;
            result.StartedAt = now;
            result.CompletedAt = now;
            return result;
        }

        public void CloseTransport()
        {
        }
    }
}
