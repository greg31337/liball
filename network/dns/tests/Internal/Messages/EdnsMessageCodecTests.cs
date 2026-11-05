namespace dns.Tests.Internal.Messages;

public class EdnsMessageCodecTests
{
    [Test]
    public void DecodesOptHeaderFlagsAndOrderedOptionFrames()
    {
        byte[] options =
        [
            253, 232, 0, 2, 170, 187,
            0, 3, 0, 0
        ];
        byte[] wire = Header(0, 0, 1)
            .Concat(OptWire([0], 4096, 0x1207C001, options))
            .ToArray();

        DnsDecodeResult result = new DnsMessageCodec().TryDecodeMessage(
            wire, new DnsDecodeOptions());
        DnsDecodeOptions opaqueOptions = new DnsDecodeOptions();
        opaqueOptions.DecodeTypedRecordData = false;
        DnsDecodeResult recognized =
            new DnsMessageCodec().TryDecodeMessage(wire, opaqueOptions);
        DnsResourceRecord opt = result.Message!.Additionals[0];
        DnsEdnsData edns = opt.Data!.Edns!;

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(opt.RecordType, Is.EqualTo(DnsRecordType.Opt));
            Assert.That(opt.ClassValue, Is.EqualTo(4096));
            Assert.That(edns.UdpPayloadSize, Is.EqualTo(4096));
            Assert.That(edns.ExtendedResponseCode, Is.EqualTo(18));
            Assert.That(edns.Version, Is.EqualTo(7));
            Assert.That(edns.Flags,
                Is.EqualTo(DnsEdnsFlag.DnssecAnswerOk |
                    DnsEdnsFlag.CompactAnswersOk));
            Assert.That(edns.FlagsValue, Is.EqualTo(0xC001));
            Assert.That(edns.Options, Has.Count.EqualTo(2));
            Assert.That(edns.Options[0].CodeValue, Is.EqualTo(65000));
            Assert.That(edns.Options[0].RawData,
                Is.EqualTo(new byte[] { 170, 187 }));
            Assert.That(edns.Options[0].UseRawData, Is.True);
            Assert.That(edns.Options[1].CodeValue, Is.EqualTo(3));
            Assert.That(edns.Options[1].RawData, Is.Empty);
            Assert.That(recognized.Success, Is.True);
            Assert.That(recognized.Message!.Additionals[0].Data!.Edns,
                Is.Not.Null);
        });
    }

    [Test]
    public void EncodesTypedOptAndPreservesEffectiveResponseAndUnknownFlags()
    {
        DnsMessage message = new DnsMessage();
        message.Header.ResponseCode = (DnsResponseCode)3;
        message.Header.ResponseCodeValue = 3;
        DnsResourceRecord opt = CreateOptRecord();
        DnsEdnsData edns = opt.Data!.Edns!;
        edns.UdpPayloadSize = 1400;
        edns.ExtendedResponseCode = 18;
        edns.Version = 1;
        edns.Flags = DnsEdnsFlag.DnssecAnswerOk |
            DnsEdnsFlag.CompactAnswersOk;
        edns.FlagsValue = 1;
        DnsEdnsOption option = new DnsEdnsOption();
        option.Code = (DnsEdnsOptionCode)65000;
        option.CodeValue = 65000;
        option.RawData = [9, 8, 7];
        option.UseRawData = true;
        edns.Options.Add(option);
        message.Additionals.Add(opt);

        DnsEncodeResult encoded = new DnsMessageCodec().TryEncodeMessage(
            message, new DnsEncodeOptions());
        DnsDecodeResult decoded = new DnsMessageCodec().TryDecodeMessage(
            encoded.Bytes, new DnsDecodeOptions());
        DnsResourceRecord decodedOpt = decoded.Message!.Additionals[0];
        DnsEdnsData decodedEdns = decodedOpt.Data!.Edns!;
        DnsTextResult text = new DnsTextCodec().FormatMessageText(
            message, new DnsTextOptions());

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(decoded.Success, Is.True);
            Assert.That(decodedOpt.ClassValue, Is.EqualTo(1400));
            Assert.That(decodedOpt.TimeToLive, Is.EqualTo(0x1201C001));
            Assert.That(decodedEdns.FlagsValue, Is.EqualTo(0xC001));
            Assert.That(decodedEdns.Options[0].RawData,
                Is.EqualTo(new byte[] { 9, 8, 7 }));
            Assert.That(new DnsMessageCodec().ReadEffectiveResponseCode(
                    decoded.Message),
                Is.EqualTo(291));
            Assert.That(text.Text, Does.Contain("flags: do co 0x0001"));
        });
    }

    [Test]
    public void RejectsMalformedOptionFramingWithAdditionalRecordLocation()
    {
        byte[] malformedOptions = [0, 1, 0, 3, 9, 8];
        byte[] wire = Header(0, 0, 1)
            .Concat(OptWire([0], 1232, 0, malformedOptions))
            .ToArray();

        DnsDecodeResult result = new DnsMessageCodec().TryDecodeMessage(
            wire, new DnsDecodeOptions());
        DnsDecodeOptions fallbackOptions = new DnsDecodeOptions();
        fallbackOptions.FallBackToOpaqueRecordData = true;
        DnsDecodeResult fallback =
            new DnsMessageCodec().TryDecodeMessage(wire, fallbackOptions);
        DnsDiagnostic diagnostic = result.Diagnostics[0];

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(diagnostic.Code,
                Is.EqualTo(DnsDiagnosticCode.InvalidEdns));
            Assert.That(diagnostic.Field,
                Is.EqualTo("edns.option.length"));
            Assert.That(diagnostic.HasSection, Is.True);
            Assert.That(diagnostic.Section,
                Is.EqualTo(DnsMessageSection.Additional));
            Assert.That(diagnostic.RecordIndex, Is.EqualTo(0));
            Assert.That(fallback.Success, Is.False);
            Assert.That(fallback.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.InvalidEdns));
        });
    }

    [Test]
    public void RejectsOptOutsideAdditionalAndDuplicateOptRecords()
    {
        byte[] misplacedWire = Header(1, 0, 0)
            .Concat(OptWire([0], 1232, 0, []))
            .ToArray();
        byte[] duplicateWire = Header(0, 0, 2)
            .Concat(OptWire([0], 1232, 0, []))
            .Concat(OptWire([0], 1232, 0, []))
            .ToArray();

        DnsDecodeResult misplaced =
            new DnsMessageCodec().TryDecodeMessage(
                misplacedWire, new DnsDecodeOptions());
        DnsDecodeResult duplicate =
            new DnsMessageCodec().TryDecodeMessage(
                duplicateWire, new DnsDecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(misplaced.Success, Is.False);
            Assert.That(misplaced.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.InvalidEdns));
            Assert.That(misplaced.Diagnostics[0].Section,
                Is.EqualTo(DnsMessageSection.Answer));
            Assert.That(misplaced.Diagnostics[0].RecordIndex, Is.EqualTo(0));
            Assert.That(duplicate.Success, Is.False);
            Assert.That(duplicate.Diagnostics[0].Section,
                Is.EqualTo(DnsMessageSection.Additional));
            Assert.That(duplicate.Diagnostics[0].RecordIndex, Is.EqualTo(1));
            Assert.That(duplicate.Diagnostics[0].Reason,
                Does.Contain("at most one OPT"));
        });
    }

    [Test]
    public void RejectsNonRootOptOwnerOnDecodeAndEncode()
    {
        byte[] wire = Header(0, 0, 1)
            .Concat(OptWire([1, 120, 0], 1232, 0, []))
            .ToArray();
        DnsDecodeResult decoded = new DnsMessageCodec().TryDecodeMessage(
            wire, new DnsDecodeOptions());
        DnsMessage message = new DnsMessage();
        DnsResourceRecord opt = CreateOptRecord();
        opt.Name = ParseName("not-root.example.");
        message.Additionals.Add(opt);
        DnsEncodeResult encoded = new DnsMessageCodec().TryEncodeMessage(
            message, new DnsEncodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.False);
            Assert.That(decoded.Diagnostics[0].Field,
                Is.EqualTo("edns.owner"));
            Assert.That(decoded.Diagnostics[0].Section,
                Is.EqualTo(DnsMessageSection.Additional));
            Assert.That(encoded.Success, Is.False);
            Assert.That(encoded.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.InvalidEdns));
            Assert.That(encoded.Diagnostics[0].Field,
                Is.EqualTo("edns.owner"));
            Assert.That(encoded.Diagnostics[0].Section,
                Is.EqualTo(DnsMessageSection.Additional));
        });
    }

    [Test]
    public void RejectsOptFixedFieldsOutsideTheirWireRanges()
    {
        DnsMessage message = new DnsMessage();
        DnsResourceRecord opt = CreateOptRecord();
        message.Additionals.Add(opt);
        DnsEdnsData edns = opt.Data!.Edns!;
        edns.UdpPayloadSize = 65536;
        DnsEncodeResult payload = Encode(message);
        edns.UdpPayloadSize = 1232;
        edns.ExtendedResponseCode = 256;
        DnsEncodeResult responseCode = Encode(message);
        edns.ExtendedResponseCode = 0;
        edns.Version = 256;
        DnsEncodeResult version = Encode(message);
        edns.Version = 0;
        edns.FlagsValue = 65536;
        DnsEncodeResult flags = Encode(message);

        Assert.Multiple(() =>
        {
            Assert.That(payload.Diagnostics[0].Field,
                Is.EqualTo("edns.udpPayloadSize"));
            Assert.That(responseCode.Diagnostics[0].Field,
                Is.EqualTo("edns.extendedResponseCode"));
            Assert.That(version.Diagnostics[0].Field,
                Is.EqualTo("edns.version"));
            Assert.That(flags.Diagnostics[0].Field,
                Is.EqualTo("edns.flags"));
        });
    }

    [Test]
    public void RejectsOptDuringEncodingWhenMisplacedOrDuplicated()
    {
        DnsMessage misplacedMessage = new DnsMessage();
        misplacedMessage.Authorities.Add(CreateOptRecord());
        DnsMessage duplicateMessage = new DnsMessage();
        duplicateMessage.Additionals.Add(CreateOptRecord());
        duplicateMessage.Additionals.Add(CreateOptRecord());

        DnsEncodeResult misplaced = Encode(misplacedMessage);
        DnsEncodeResult duplicate = Encode(duplicateMessage);

        Assert.Multiple(() =>
        {
            Assert.That(misplaced.Success, Is.False);
            Assert.That(misplaced.Diagnostics[0].Section,
                Is.EqualTo(DnsMessageSection.Authority));
            Assert.That(duplicate.Success, Is.False);
            Assert.That(duplicate.Diagnostics[0].RecordIndex, Is.EqualTo(1));
        });
    }

    static DnsEncodeResult Encode(DnsMessage message)
    {
        return new DnsMessageCodec().TryEncodeMessage(
            message, new DnsEncodeOptions());
    }

    static DnsResourceRecord CreateOptRecord()
    {
        DnsResourceRecord record = new DnsResourceRecord();
        record.Name = ParseName(".");
        record.RecordType = DnsRecordType.Opt;
        record.RecordTypeValue = 41;
        record.Data = new DnsRecordData();
        record.Data.Edns = new DnsEdnsData();
        return record;
    }

    static DnsName ParseName(string text)
    {
        return new DnsNameService().ParseName(text, null);
    }

    static byte[] Header(int answers, int authorities, int additionals)
    {
        byte[] result = new byte[12];
        result[6] = (byte)(answers >> 8);
        result[7] = (byte)answers;
        result[8] = (byte)(authorities >> 8);
        result[9] = (byte)authorities;
        result[10] = (byte)(additionals >> 8);
        result[11] = (byte)additionals;
        return result;
    }

    static byte[] OptWire(
        byte[] owner,
        int udpPayloadSize,
        uint timeToLive,
        byte[] data)
    {
        byte[] fixedFields =
        [
            0, 41,
            (byte)(udpPayloadSize >> 8), (byte)udpPayloadSize,
            (byte)(timeToLive >> 24), (byte)(timeToLive >> 16),
            (byte)(timeToLive >> 8), (byte)timeToLive,
            (byte)(data.Length >> 8), (byte)data.Length
        ];
        return owner.Concat(fixedFields).Concat(data).ToArray();
    }
}
