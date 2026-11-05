namespace dns.Tests.Api;

public class CodecEntryPointTests
{
    [Test]
    public void NameWireEntryPointsSeparateStrictAndTryFailures()
    {
        IDnsNameService service = new DnsNameService();
        DnsName name = service.ParseName("www.example.", null);

        DnsEncodeResult encoded = service.EncodeName(
            name, new DnsEncodeOptions());
        DnsDecodeResult decoded = service.DecodeName(
            encoded.Bytes, 0, new DnsDecodeOptions());
        DnsDecodeResult malformed = service.TryDecodeName(
            [192, 0], 0, new DnsDecodeOptions());
        DnsName relative = service.ParseName("relative", null);
        DnsEncodeResult invalidEncoding = service.TryEncodeName(
            relative, new DnsEncodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(Format(decoded.Name!), Is.EqualTo("www.example."));
            Assert.That(malformed.Success, Is.False);
            Assert.That(malformed.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.CompressionLoop));
            Assert.That(
                () => service.DecodeName(
                    [192, 0], 0, new DnsDecodeOptions()),
                Throws.TypeOf<DnsFormatException>()
                    .With.Message.Contains("Offset: 0"));
            Assert.That(invalidEncoding.Success, Is.False);
            Assert.That(
                () => service.EncodeName(relative, new DnsEncodeOptions()),
                Throws.TypeOf<DnsFormatException>());
        });
    }

    [Test]
    public void NameServiceExposesCompletedNameOperations()
    {
        IDnsNameService service = new DnsNameService();
        DnsName mixed = service.ParseName("WwW.Example.", null);
        DnsName canonical = service.CreateCanonicalName(mixed);
        DnsName suffix = service.ParseName("example.", null);
        DnsName prefix = service.ParseName("mail", null);
        DnsName combined = service.ConcatenateNames(prefix, suffix);
        DnsName relative = service.RelativizeName(combined, suffix);
        DnsName common = service.FindCommonAncestor(
            mixed, service.ParseName("api.example.", null));
        DnsTextOptions formatOptions = new DnsTextOptions();
        formatOptions.Canonical = true;
        DnsName idna = service.ConvertIdnaToName("bücher.example.", null);
        DnsName ipv4 = service.BuildIpv4ReverseName([192, 0, 2, 1]);
        byte[] ipv6Address = new byte[16];
        ipv6Address[15] = 1;
        DnsName ipv6 = service.BuildIpv6ReverseName(ipv6Address);
        DnsTextResult invalidText = service.TryParseName("bad..name", null);

        Assert.Multiple(() =>
        {
            Assert.That(service.NamesEqual(mixed, canonical), Is.True);
            Assert.That(service.CompareNames(canonical, mixed), Is.EqualTo(0));
            Assert.That(service.HasNameSuffix(mixed, suffix), Is.True);
            Assert.That(service.IsAncestorName(suffix, mixed), Is.True);
            Assert.That(service.CountNameLabels(mixed), Is.EqualTo(2));
            Assert.That(Format(service.GetParentName(mixed)),
                Is.EqualTo("Example."));
            Assert.That(Format(combined), Is.EqualTo("mail.example."));
            Assert.That(Format(relative), Is.EqualTo("mail"));
            Assert.That(Format(common), Is.EqualTo("Example."));
            Assert.That(service.FormatName(mixed, formatOptions),
                Is.EqualTo("www.example."));
            Assert.That(Format(idna), Is.EqualTo("xn--bcher-kva.example."));
            Assert.That(service.ConvertNameToUnicode(idna),
                Is.EqualTo("bücher.example."));
            Assert.That(service.ParseIpv4ReverseAddress(ipv4),
                Is.EqualTo(new byte[] { 192, 0, 2, 1 }));
            Assert.That(service.ParseIpv6ReverseAddress(ipv6),
                Is.EqualTo(ipv6Address));
            Assert.That(invalidText.Success, Is.False);
            Assert.That(
                () => service.ParseName("bad..name", null),
                Throws.TypeOf<DnsFormatException>());
        });
    }

    [Test]
    public void RecordDataEntryPointsSeparateStrictAndTryFailures()
    {
        IDnsRecordDataCodec codec = new DnsRecordDataCodec();
        DnsRecordData data = AddressData([192, 0, 2, 1]);

        DnsEncodeResult encoded = codec.EncodeRecordData(
            DnsRecordType.A, 0, data, new DnsEncodeOptions());
        DnsDecodeResult decoded = codec.DecodeRecordData(
            DnsRecordType.A,
            0,
            encoded.Bytes,
            0,
            encoded.Bytes.Length,
            new DnsDecodeOptions());
        DnsDecodeResult malformed = codec.TryDecodeRecordData(
            DnsRecordType.A,
            0,
            [192, 0, 2],
            0,
            3,
            new DnsDecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Bytes,
                Is.EqualTo(new byte[] { 192, 0, 2, 1 }));
            Assert.That(decoded.RecordData!.Address!.Address,
                Is.EqualTo(new byte[] { 192, 0, 2, 1 }));
            Assert.That(malformed.Success, Is.False);
            Assert.That(
                () => codec.DecodeRecordData(
                    DnsRecordType.A,
                    0,
                    [192, 0, 2],
                    0,
                    3,
                    new DnsDecodeOptions()),
                Throws.TypeOf<DnsFormatException>()
                    .With.Message.Contains("address"));
            Assert.That(
                () => codec.EncodeRecordData(
                    DnsRecordType.A,
                    0,
                    new DnsRecordData(),
                    new DnsEncodeOptions()),
                Throws.TypeOf<DnsFormatException>());
        });
    }

    [Test]
    public void ResourceRecordEntryPointsRoundTripAndReportInvalidMetaTypes()
    {
        IDnsRecordDataCodec codec = new DnsRecordDataCodec();
        DnsResourceRecord record = AddressRecord();

        DnsEncodeResult encoded = codec.TryEncodeResourceRecord(
            record, new DnsEncodeOptions());
        DnsDecodeResult decoded = codec.DecodeResourceRecord(
            encoded.Bytes, 0, new DnsDecodeOptions());
        DnsDecodeResult tried = codec.TryDecodeResourceRecord(
            encoded.Bytes, 0, new DnsDecodeOptions());
        DnsResourceRecord meta = AddressRecord();
        meta.RecordType = DnsRecordType.Any;
        meta.RecordTypeValue = 255;
        DnsEncodeResult invalid = codec.TryEncodeResourceRecord(
            meta, new DnsEncodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(decoded.Record!.RecordType, Is.EqualTo(DnsRecordType.A));
            Assert.That(decoded.Record.Data!.Address!.Address,
                Is.EqualTo(new byte[] { 192, 0, 2, 1 }));
            Assert.That(tried.Success, Is.True);
            Assert.That(tried.Record!.RecordType, Is.EqualTo(DnsRecordType.A));
            Assert.That(tried.Record.Data!.Address!.Address,
                Is.EqualTo(new byte[] { 192, 0, 2, 1 }));
            Assert.That(invalid.Success, Is.False);
            Assert.That(invalid.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.InvalidRecordData));
            Assert.That(
                () => codec.EncodeResourceRecord(
                    meta, new DnsEncodeOptions()),
                Throws.TypeOf<DnsFormatException>());
            Assert.That(
                () => codec.DecodeResourceRecord(
                    [0], 0, new DnsDecodeOptions()),
                Throws.TypeOf<DnsFormatException>());
        });
    }

    [Test]
    public void OpaqueEdnsEntryPointsRoundTripAndPreserveDiagnostics()
    {
        IDnsRecordDataCodec codec = new DnsRecordDataCodec();
        DnsEdnsOption option = new DnsEdnsOption();
        option.Code = (DnsEdnsOptionCode)65000;
        option.CodeValue = 65000;
        option.UseRawData = true;
        option.RawData = [9, 8, 7];

        DnsEdnsOptionResult encoded = codec.TryEncodeEdnsOption(option);
        DnsEdnsOption decoded = codec.DecodeEdnsOption(
            encoded.Bytes, new DnsDecodeOptions());
        DnsEdnsOptionResult malformed = codec.TryDecodeEdnsOption(
            [0, 1, 0, 3, 9], new DnsDecodeOptions());
        DnsEncodeResult strictEncoding = codec.EncodeEdnsOption(option);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Bytes,
                Is.EqualTo(new byte[] { 253, 232, 0, 3, 9, 8, 7 }));
            Assert.That(strictEncoding.Bytes, Is.EqualTo(encoded.Bytes));
            Assert.That(decoded.CodeValue, Is.EqualTo(65000));
            Assert.That(decoded.UseRawData, Is.True);
            Assert.That(decoded.RawData, Is.EqualTo(new byte[] { 9, 8, 7 }));
            Assert.That(malformed.Success, Is.False);
            Assert.That(malformed.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.InvalidEdns));
            Assert.That(
                () => codec.DecodeEdnsOption(
                    [0, 1, 0, 3, 9], new DnsDecodeOptions()),
                Throws.TypeOf<DnsFormatException>());
        });
    }

    [Test]
    public void MessageQuestionAndFrameEntryPointsRoundTrip()
    {
        IDnsMessageCodec codec = new DnsMessageCodec();
        DnsQuestion question = Question();
        DnsEncodeResult questionBytes = codec.EncodeQuestion(
            question, new DnsEncodeOptions());
        DnsEncodeResult triedQuestionBytes = codec.TryEncodeQuestion(
            question, new DnsEncodeOptions());
        DnsDecodeResult questionResult = codec.DecodeQuestion(
            questionBytes.Bytes, new DnsDecodeOptions());
        DnsMessage message = new DnsMessage();
        message.Header.Identifier = 4660;
        message.Questions.Add(question);
        DnsEncodeResult messageBytes = codec.TryEncodeMessage(
            message, new DnsEncodeOptions());
        DnsEncodeResult strictMessageBytes = codec.EncodeMessage(
            message, new DnsEncodeOptions());
        DnsDecodeResult messageResult = codec.DecodeMessage(
            messageBytes.Bytes, new DnsDecodeOptions());
        DnsTcpFrame frame = new DnsTcpFrame();
        frame.MessageBytes = messageBytes.Bytes;
        frame.Complete = true;
        DnsEncodeResult frameBytes = codec.EncodeTcpFrame(
            frame, new DnsEncodeOptions());
        DnsDecodeResult frameResult = codec.TryDecodeTcpFrame(
            frameBytes.Bytes, new DnsDecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(triedQuestionBytes.Success, Is.True);
            Assert.That(triedQuestionBytes.Bytes,
                Is.EqualTo(questionBytes.Bytes));
            Assert.That(Format(questionResult.Question!.Name),
                Is.EqualTo("example."));
            Assert.That(messageResult.Message!.Header.Identifier,
                Is.EqualTo(4660));
            Assert.That(messageResult.Message.Questions, Has.Count.EqualTo(1));
            Assert.That(strictMessageBytes.Bytes, Is.EqualTo(messageBytes.Bytes));
            Assert.That(frameResult.Success, Is.True);
            Assert.That(frameResult.TcpFrame!.MessageBytes,
                Is.EqualTo(messageBytes.Bytes));
        });
    }

    [Test]
    public void MessageQuestionAndFrameTryFailuresDoNotThrow()
    {
        IDnsMessageCodec codec = new DnsMessageCodec();

        DnsDecodeResult question = codec.TryDecodeQuestion(
            [0], new DnsDecodeOptions());
        DnsDecodeResult message = codec.TryDecodeMessage(
            [0], new DnsDecodeOptions());
        DnsDecodeResult frame = codec.TryDecodeTcpFrame(
            [0, 0], new DnsDecodeOptions());
        DnsEncodeOptions shortMessageOptions = new DnsEncodeOptions();
        shortMessageOptions.MaximumMessageLength = 11;
        DnsEncodeResult encodedMessage = codec.TryEncodeMessage(
            new DnsMessage(), shortMessageOptions);

        Assert.Multiple(() =>
        {
            Assert.That(question.Success, Is.False);
            Assert.That(message.Success, Is.False);
            Assert.That(frame.Success, Is.False);
            Assert.That(encodedMessage.Success, Is.False);
            Assert.That(
                () => codec.DecodeQuestion([0], new DnsDecodeOptions()),
                Throws.TypeOf<DnsFormatException>());
            Assert.That(
                () => codec.DecodeMessage([0], new DnsDecodeOptions()),
                Throws.TypeOf<DnsFormatException>()
                    .With.Message.Contains("Offset: 1"));
            Assert.That(
                () => codec.DecodeTcpFrame([0, 0], new DnsDecodeOptions()),
                Throws.TypeOf<DnsFormatException>());
            Assert.That(
                () => codec.EncodeQuestion(
                    new DnsQuestion(), new DnsEncodeOptions()),
                Throws.TypeOf<DnsFormatException>());
            Assert.That(
                () => codec.EncodeMessage(
                    new DnsMessage(), shortMessageOptions),
                Throws.TypeOf<DnsFormatException>());
            Assert.That(
                () => codec.EncodeTcpFrame(
                    new DnsTcpFrame(), new DnsEncodeOptions()),
                Throws.TypeOf<DnsFormatException>());
        });
    }

    [Test]
    public void MessageOperationEntryPointsDelegateCompletedBehavior()
    {
        IDnsMessageCodec codec = new DnsMessageCodec();
        DnsMessage message = new DnsMessage();
        message.Header.Identifier = 7;
        DnsMessage clone = codec.CloneMessage(message);
        int size = codec.EstimateMessageSize(
            message, new DnsEncodeOptions());
        bool equal = codec.MessagesEqual(
            message, clone, new DnsCanonicalOrderOptions());

        codec.WriteEffectiveResponseCode(message, 16);
        int responseCode = codec.ReadEffectiveResponseCode(message);

        Assert.Multiple(() =>
        {
            Assert.That(clone, Is.Not.SameAs(message));
            Assert.That(clone.Header.Identifier, Is.EqualTo(7));
            Assert.That(size, Is.EqualTo(DnsProtocolLimits.HeaderLength));
            Assert.That(equal, Is.True);
            Assert.That(responseCode, Is.EqualTo(16));
            Assert.That(message.Additionals, Has.Count.EqualTo(1));
        });
    }

    static DnsQuestion Question()
    {
        DnsQuestion question = new DnsQuestion();
        question.Name = Parse("example.");
        question.RecordType = DnsRecordType.A;
        question.RecordTypeValue = 1;
        question.Class = DnsClass.Internet;
        question.ClassValue = 1;
        return question;
    }

    static DnsResourceRecord AddressRecord()
    {
        DnsResourceRecord record = new DnsResourceRecord();
        record.Name = Parse("host.example.");
        record.RecordType = DnsRecordType.A;
        record.RecordTypeValue = 1;
        record.Class = DnsClass.Internet;
        record.ClassValue = 1;
        record.TimeToLive = 60;
        record.Data = AddressData([192, 0, 2, 1]);
        return record;
    }

    static DnsRecordData AddressData(byte[] address)
    {
        DnsRecordData data = new DnsRecordData();
        data.Address = new DnsAddressData();
        data.Address.Address = address;
        return data;
    }

    static DnsName Parse(string text)
    {
        return PresentationDnsName.ParseName(text, null).Name!;
    }

    static string Format(DnsName name)
    {
        return PresentationDnsName.FormatName(name).Text!;
    }
}
