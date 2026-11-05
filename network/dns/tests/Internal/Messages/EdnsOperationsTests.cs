namespace dns.Tests.Internal.Messages;

public class EdnsOperationsTests
{
    [Test]
    public void PreparesIndependentRequestWithClampedPayloadAndDnssecFlag()
    {
        DnsMessage source = Message(false);

        DnsMessage prepared = EdnsOperations.PrepareRequest(
            source, 128, true);
        DnsEdnsData edns = Edns(prepared);

        Assert.Multiple(() =>
        {
            Assert.That(source.Additionals, Is.Empty);
            Assert.That(prepared.Additionals, Has.Count.EqualTo(1));
            Assert.That(edns.UdpPayloadSize,
                Is.EqualTo(DnsProtocolLimits.ClassicUdpPayloadSize));
            Assert.That(edns.Version, Is.Zero);
            Assert.That(edns.ExtendedResponseCode, Is.Zero);
            Assert.That(edns.Flags,
                Is.EqualTo(DnsEdnsFlag.DnssecAnswerOk));
            Assert.That(prepared.Additionals[0].ClassValue,
                Is.EqualTo(DnsProtocolLimits.ClassicUdpPayloadSize));
            Assert.That(prepared.Additionals[0].TimeToLive,
                Is.EqualTo(0x00008000));
        });
    }

    [Test]
    public void RequestPreparationAppliesRequestOptionPolicy()
    {
        DnsMessage request = Message(false);
        DnsResourceRecord opt = Opt(2000, 0);
        DnsEdnsOption nsid = Option(DnsEdnsOptionCode.NameServerIdentifier);
        nsid.NameServerIdentifier = [1, 2, 3];
        DnsEdnsOption subnet = Subnet(2, 24, 20, [192, 0, 2]);
        DnsEdnsOption expire = Option(DnsEdnsOptionCode.Expire);
        expire.HasExpire = true;
        expire.ExpireSeconds = 99;
        DnsEdnsOption keepalive = Option(DnsEdnsOptionCode.TcpKeepalive);
        keepalive.Keepalive = new DnsEdnsKeepalive();
        keepalive.Keepalive.HasTimeout = true;
        keepalive.Keepalive.TimeoutUnits = 30;
        DnsEdnsOption zone = ZoneVersion(true);
        DnsEdnsOption error = Option(DnsEdnsOptionCode.ExtendedDnsError);
        error.ExtendedError = new DnsEdnsExtendedError();
        DnsEdnsOption report = Option(DnsEdnsOptionCode.ReportChannel);
        report.ReportChannel = Parse("agent.example.");
        opt.Data!.Edns!.Options.AddRange(
            [nsid, subnet, expire, keepalive, zone, error, report]);
        request.Additionals.Add(opt);

        DnsMessage prepared = EdnsOperations.PrepareRequest(
            request, 70000, false);
        DnsEdnsData edns = Edns(prepared);

        Assert.Multiple(() =>
        {
            Assert.That(edns.UdpPayloadSize, Is.EqualTo(65535));
            Assert.That(edns.Options, Has.Count.EqualTo(5));
            Assert.That(edns.Options[0].NameServerIdentifier, Is.Empty);
            Assert.That(edns.Options[1].ClientSubnet!.ScopePrefixLength,
                Is.Zero);
            Assert.That(edns.Options[2].HasExpire, Is.False);
            Assert.That(edns.Options[3].Keepalive!.HasTimeout, Is.False);
            Assert.That(edns.Options[4].ZoneVersion!.HasVersion, Is.False);
            Assert.That(request.Additionals[0].Data!.Edns!.Options,
                Has.Count.EqualTo(7));
        });
    }

    [Test]
    public void ResponseToClassicRequestOmitsEdns()
    {
        DnsMessage request = Message(false);
        DnsMessage response = Message(true);
        response.Additionals.Add(Opt(1232, 0));

        EdnsResponsePreparation prepared = EdnsOperations.PrepareResponse(
            request, response, 4096);

        Assert.Multiple(() =>
        {
            Assert.That(prepared.Message.Additionals, Is.Empty);
            Assert.That(prepared.PayloadSizeLimit,
                Is.EqualTo(DnsProtocolLimits.ClassicUdpPayloadSize));
            Assert.That(response.Additionals, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void NegotiatesPayloadAndEnforcesResponseOptionPolicy()
    {
        DnsMessage request = Message(false);
        DnsResourceRecord requestOpt = Opt(900, 0);
        DnsEdnsOption requestNsid = Option(
            DnsEdnsOptionCode.NameServerIdentifier);
        DnsEdnsOption requestSubnet = Subnet(1, 24, 0, [192, 0, 2]);
        DnsEdnsOption requestCookie = Cookie([0, 1, 2, 3, 4, 5, 6, 7], []);
        DnsEdnsOption requestZone = ZoneVersion(false);
        DnsEdnsOption requestPrivate = Opaque(65000, [1]);
        requestOpt.Data!.Edns!.Options.AddRange(
            [requestNsid, requestSubnet, requestCookie, requestZone, requestPrivate]);
        request.Additionals.Add(requestOpt);

        DnsMessage response = Message(true);
        DnsResourceRecord responseOpt = Opt(4096, 0);
        DnsEdnsOption responseNsid = Option(
            DnsEdnsOptionCode.NameServerIdentifier);
        responseNsid.NameServerIdentifier = [9, 8];
        DnsEdnsOption responseSubnet = Subnet(2, 56, 20,
            [32, 1, 13, 184, 0, 0, 0]);
        DnsEdnsOption responseCookie = Cookie(
            [9, 9, 9, 9, 9, 9, 9, 9], [10, 11, 12, 13, 14, 15, 16, 17]);
        DnsEdnsOption error = Option(DnsEdnsOptionCode.ExtendedDnsError);
        error.ExtendedError = new DnsEdnsExtendedError();
        error.ExtendedError.Code = DnsExtendedErrorCode.StaleAnswer;
        error.ExtendedError.CodeValue = 3;
        DnsEdnsOption report = Option(DnsEdnsOptionCode.ReportChannel);
        report.ReportChannel = Parse("agent.example.");
        DnsEdnsOption responseZone = ZoneVersion(true);
        DnsEdnsOption queryOnly = Option(
            DnsEdnsOptionCode.DnssecAlgorithmUnderstood);
        queryOnly.DnssecAlgorithmValues.Add(15);
        DnsEdnsOption responsePrivate = Opaque(65000, [2]);
        DnsEdnsOption unsolicited = Opaque(65001, [3]);
        responseOpt.Data!.Edns!.Options.AddRange(
            [responseNsid, responseSubnet, responseCookie, error, report,
                responseZone, queryOnly, responsePrivate, unsolicited]);
        response.Additionals.Add(responseOpt);

        EdnsResponsePreparation prepared = EdnsOperations.PrepareResponse(
            request, response, 70000);
        DnsEdnsData edns = Edns(prepared.Message);
        DnsEncodeResult encoded = new DnsMessageCodec().TryEncodeMessage(
            prepared.Message, new DnsEncodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(prepared.PayloadSizeLimit, Is.EqualTo(900));
            Assert.That(edns.UdpPayloadSize, Is.EqualTo(65535));
            Assert.That(edns.Options.Select(Value),
                Is.EqualTo(new[] { 3, 8, 10, 15, 18, 19, 65000 }));
            Assert.That(edns.Options[1].ClientSubnet!.AddressFamily,
                Is.EqualTo(1));
            Assert.That(edns.Options[1].ClientSubnet!.SourcePrefixLength,
                Is.EqualTo(24));
            Assert.That(edns.Options[1].ClientSubnet!.ScopePrefixLength,
                Is.EqualTo(20));
            Assert.That(edns.Options[1].ClientSubnet!.Address,
                Is.EqualTo(new byte[] { 192, 0, 2 }));
            Assert.That(edns.Options[2].Cookie!.ClientCookie,
                Is.EqualTo(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 }));
            Assert.That(encoded.Success, Is.True);
        });
    }

    [Test]
    public void UnsupportedRequestVersionProducesMinimalBadVersionResponse()
    {
        DnsMessage request = Message(false);
        request.Additionals.Add(Opt(600, 1));
        DnsMessage response = Message(true);
        response.Answers.Add(AddressRecord());
        response.Authorities.Add(AddressRecord());
        response.Additionals.Add(AddressRecord());
        DnsResourceRecord responseOpt = Opt(4096, 0);
        responseOpt.Data!.Edns!.Options.Add(
            Option(DnsEdnsOptionCode.Padding));
        response.Additionals.Add(responseOpt);

        EdnsResponsePreparation prepared = EdnsOperations.PrepareResponse(
            request, response, 1232);
        DnsEdnsData edns = Edns(prepared.Message);

        Assert.Multiple(() =>
        {
            Assert.That(prepared.BadVersion, Is.True);
            Assert.That(prepared.PayloadSizeLimit, Is.EqualTo(600));
            Assert.That(prepared.Message.Answers, Is.Empty);
            Assert.That(prepared.Message.Authorities, Is.Empty);
            Assert.That(prepared.Message.Additionals, Has.Count.EqualTo(1));
            Assert.That(edns.Version, Is.Zero);
            Assert.That(edns.Options, Is.Empty);
            Assert.That(new DnsMessageCodec().ReadEffectiveResponseCode(
                prepared.Message), Is.EqualTo((int)DnsResponseCode.BadVersion));
        });
    }

    [Test]
    public void InspectsEdnsSupportAndSafeNoEdnsFallback()
    {
        DnsMessage request = EdnsOperations.PrepareRequest(
            Message(false), 1232, false);
        DnsMessage formError = Message(true);
        formError.Header.ResponseCode = DnsResponseCode.FormatError;
        formError.Header.ResponseCodeValue = 1;
        DnsMessage supported = Message(true);
        supported.Additionals.Add(Opt(400, 0));

        EdnsResponseMetadata retry = EdnsOperations.InspectResponse(
            request, formError, true, false);
        EdnsResponseMetadata required = EdnsOperations.InspectResponse(
            request, formError, true, true);
        EdnsResponseMetadata capability = EdnsOperations.InspectResponse(
            request, supported, true, false);

        Assert.Multiple(() =>
        {
            Assert.That(retry.RequestUsedEdns, Is.True);
            Assert.That(retry.HasEdnsCapability, Is.True);
            Assert.That(retry.SupportsEdns, Is.False);
            Assert.That(retry.ShouldRetryWithoutEdns, Is.True);
            Assert.That(required.ShouldRetryWithoutEdns, Is.False);
            Assert.That(capability.SupportsEdns, Is.True);
            Assert.That(capability.ResponseUsedEdns, Is.True);
            Assert.That(capability.UdpPayloadSize, Is.EqualTo(512));
        });
    }

    [Test]
    public void AppliesCapabilityAndDowngradeMetadataToQueryState()
    {
        EdnsResponseMetadata metadata = new EdnsResponseMetadata();
        metadata.HasEdnsCapability = true;
        metadata.SupportsEdns = true;
        metadata.UdpPayloadSize = 1232;
        DnsQueryResult queryResult = new DnsQueryResult();
        DnsServerState serverState = new DnsServerState();

        EdnsOperations.ApplyResponseMetadata(
            metadata, queryResult, serverState, false);
        Assert.Multiple(() =>
        {
            Assert.That(queryResult.EdnsDowngraded, Is.False);
            Assert.That(serverState.HasEdnsCapability, Is.True);
            Assert.That(serverState.SupportsEdns, Is.True);
            Assert.That(serverState.EdnsUdpPayloadSize, Is.EqualTo(1232));
        });

        EdnsOperations.ApplyResponseMetadata(
            metadata, queryResult, serverState, true);
        Assert.Multiple(() =>
        {
            Assert.That(queryResult.EdnsDowngraded, Is.True);
            Assert.That(serverState.HasEdnsCapability, Is.True);
            Assert.That(serverState.SupportsEdns, Is.False);
            Assert.That(serverState.EdnsUdpPayloadSize, Is.EqualTo(512));
        });
    }

    static DnsMessage Message(bool response)
    {
        DnsMessage message = new DnsMessage();
        if (response)
        {
            message.Header.Flags = DnsHeaderFlag.Response;
            message.Header.FlagsValue = (int)DnsHeaderFlag.Response;
        }

        DnsQuestion question = new DnsQuestion();
        question.Name = Parse("a.b.example.");
        question.RecordType = DnsRecordType.A;
        question.RecordTypeValue = 1;
        question.Class = DnsClass.Internet;
        question.ClassValue = 1;
        message.Questions.Add(question);
        return message;
    }

    static DnsResourceRecord Opt(int payloadSize, int version)
    {
        DnsResourceRecord opt = new DnsResourceRecord();
        opt.Name = Parse(".");
        opt.RecordType = DnsRecordType.Opt;
        opt.RecordTypeValue = 41;
        opt.Class = (DnsClass)payloadSize;
        opt.ClassValue = payloadSize;
        opt.Data = new DnsRecordData();
        opt.Data.Edns = new DnsEdnsData();
        opt.Data.Edns.UdpPayloadSize = payloadSize;
        opt.Data.Edns.Version = version;
        opt.TimeToLive = (uint)version << 16;
        return opt;
    }

    static DnsEdnsData Edns(DnsMessage message)
    {
        return message.Additionals.Single(
            record => Value(record) == 41).Data!.Edns!;
    }

    static DnsEdnsOption Option(DnsEdnsOptionCode code)
    {
        DnsEdnsOption option = new DnsEdnsOption();
        option.Code = code;
        option.CodeValue = (int)code;
        return option;
    }

    static DnsEdnsOption Opaque(int code, byte[] data)
    {
        DnsEdnsOption option = new DnsEdnsOption();
        option.Code = (DnsEdnsOptionCode)code;
        option.CodeValue = code;
        option.RawData = data;
        option.UseRawData = true;
        return option;
    }

    static DnsEdnsOption Subnet(
        int family,
        int sourcePrefixLength,
        int scopePrefixLength,
        byte[] address)
    {
        DnsEdnsOption option = Option(DnsEdnsOptionCode.ClientSubnet);
        option.ClientSubnet = new DnsEdnsClientSubnet();
        option.ClientSubnet.AddressFamily = family;
        option.ClientSubnet.SourcePrefixLength = sourcePrefixLength;
        option.ClientSubnet.ScopePrefixLength = scopePrefixLength;
        option.ClientSubnet.Address = address;
        return option;
    }

    static DnsEdnsOption Cookie(byte[] client, byte[] server)
    {
        DnsEdnsOption option = Option(DnsEdnsOptionCode.Cookie);
        option.Cookie = new DnsEdnsCookie();
        option.Cookie.ClientCookie = client;
        option.Cookie.ServerCookie = server;
        return option;
    }

    static DnsEdnsOption ZoneVersion(bool populated)
    {
        DnsEdnsOption option = Option(DnsEdnsOptionCode.ZoneVersion);
        option.ZoneVersion = new DnsEdnsZoneVersion();
        option.ZoneVersion.HasVersion = populated;
        if (populated)
        {
            option.ZoneVersion.LabelCount = 2;
            option.ZoneVersion.Type = 0;
            option.ZoneVersion.Version = [0, 0, 0, 1];
        }

        return option;
    }

    static DnsResourceRecord AddressRecord()
    {
        DnsResourceRecord record = new DnsResourceRecord();
        record.Name = Parse("a.example.");
        record.RecordType = DnsRecordType.A;
        record.RecordTypeValue = 1;
        record.Class = DnsClass.Internet;
        record.ClassValue = 1;
        record.TimeToLive = 60;
        record.UseRawData = true;
        record.RawData = [192, 0, 2, 1];
        return record;
    }

    static int Value(DnsEdnsOption option)
    {
        if (option.CodeValue != 0)
        {
            return option.CodeValue;
        }

        return (int)option.Code;
    }

    static int Value(DnsResourceRecord record)
    {
        if (record.RecordTypeValue != 0)
        {
            return record.RecordTypeValue;
        }

        return (int)record.RecordType;
    }

    static DnsName Parse(string text)
    {
        return new DnsNameService().ParseName(text, null);
    }
}
