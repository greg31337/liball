namespace dns.Tests.Internal.Wire;

public class DnsHeaderCodecTests
{
    [Test]
    public void HeaderRoundTripPreservesIdentifierFlagsCodesAndCounts()
    {
        byte[] source =
        [
            0x12, 0x34,
            0xa7, 0xb5,
            0, 1,
            0, 2,
            0, 3,
            0, 4,
            0xde, 0xad
        ];

        DnsDecodeResult decoded = DnsHeaderCodec.DecodeHeader(
            source,
            new DnsDecodeOptions());
        DnsEncodeResult encoded = DnsHeaderCodec.EncodeHeader(
            PopulateSections(decoded.Message!),
            new DnsEncodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.BytesConsumed,
                Is.EqualTo(DnsProtocolLimits.HeaderLength));
            Assert.That(decoded.TrailingBytes,
                Is.EqualTo(new byte[] { 0xde, 0xad }));
            DnsMessageHeader header = decoded.Message!.Header;
            Assert.That(header.Identifier, Is.EqualTo(0x1234));
            Assert.That(header.Opcode, Is.EqualTo(DnsOpcode.Notify));
            Assert.That(header.OpcodeValue, Is.EqualTo(4));
            Assert.That(header.ResponseCode, Is.EqualTo(DnsResponseCode.Refused));
            Assert.That(header.ResponseCodeValue, Is.EqualTo(5));
            Assert.That(header.Flags, Is.EqualTo(
                DnsHeaderFlag.Response |
                DnsHeaderFlag.AuthoritativeAnswer |
                DnsHeaderFlag.Truncated |
                DnsHeaderFlag.RecursionDesired |
                DnsHeaderFlag.RecursionAvailable |
                DnsHeaderFlag.AuthenticData |
                DnsHeaderFlag.CheckingDisabled));
            Assert.That(header.FlagsValue, Is.EqualTo(0xa7b5));
            Assert.That(header.QuestionCount, Is.EqualTo(1));
            Assert.That(header.AnswerCount, Is.EqualTo(2));
            Assert.That(header.AuthorityCount, Is.EqualTo(3));
            Assert.That(header.AdditionalCount, Is.EqualTo(4));
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Bytes, Is.EqualTo(source[..12]));
        });
    }

    [Test]
    public void UnknownOpcodeAndResponseCodeRoundTripNumerically()
    {
        byte[] source =
        [
            0xab, 0xcd,
            0xf8, 0x0f,
            0, 0, 0, 0, 0, 0, 0, 0
        ];
        DnsDecodeOptions options = new DnsDecodeOptions();

        DnsDecodeResult decoded = DnsHeaderCodec.DecodeHeader(source, options);
        DnsEncodeResult encoded = DnsHeaderCodec.EncodeHeader(
            decoded.Message!,
            new DnsEncodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.Message!.Header.OpcodeValue, Is.EqualTo(15));
            Assert.That((int)decoded.Message.Header.Opcode, Is.EqualTo(15));
            Assert.That(decoded.Message.Header.ResponseCodeValue, Is.EqualTo(15));
            Assert.That((int)decoded.Message.Header.ResponseCode, Is.EqualTo(15));
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Bytes, Is.EqualTo(source));
        });
    }

    [Test]
    public void ReservedHeaderBitFollowsDecodePolicy()
    {
        byte[] source =
        [
            0, 1, 0, 0x40,
            0, 0, 0, 0, 0, 0, 0, 0
        ];
        DnsDecodeOptions permissive = new DnsDecodeOptions();
        permissive.RejectReservedHeaderBits = false;

        DnsDecodeResult strict = DnsHeaderCodec.DecodeHeader(
            source,
            new DnsDecodeOptions());
        DnsDecodeResult allowed = DnsHeaderCodec.DecodeHeader(source, permissive);

        Assert.Multiple(() =>
        {
            AssertFailure(strict, DnsDiagnosticCode.ValueOutOfRange, 2);
            Assert.That(allowed.Success, Is.True);
            Assert.That(allowed.Message!.Header.Flags,
                Is.EqualTo(DnsHeaderFlag.Reserved));
            Assert.That(allowed.Message.Header.FlagsValue, Is.EqualTo(0x40));
        });
    }

    [Test]
    public void HeaderEncodingUsesEnumValuesWhenRawCodesAreUnset()
    {
        DnsMessage message = new DnsMessage();
        message.Header.Identifier = 0x4321;
        message.Header.Opcode = DnsOpcode.Update;
        message.Header.ResponseCode = DnsResponseCode.NotZone;
        message.Header.Flags = DnsHeaderFlag.Response |
            DnsHeaderFlag.AuthenticData;

        DnsEncodeResult result = DnsHeaderCodec.EncodeHeader(
            message,
            new DnsEncodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Bytes[0], Is.EqualTo(0x43));
            Assert.That(result.Bytes[1], Is.EqualTo(0x21));
            Assert.That(result.Bytes[2], Is.EqualTo(0xa8));
            Assert.That(result.Bytes[3], Is.EqualTo(0x2a));
        });
    }

    [Test]
    public void DerivedCountsComeFromActualSectionCollections()
    {
        DnsMessage message = new DnsMessage();
        message.Header.QuestionCount = 99;
        message.Header.AnswerCount = 99;
        message.Header.AuthorityCount = 99;
        message.Header.AdditionalCount = 99;
        message.Questions.Add(new DnsQuestion());
        message.Answers.Add(new DnsResourceRecord());
        message.Answers.Add(new DnsResourceRecord());
        message.Authorities.Add(new DnsResourceRecord());
        message.Authorities.Add(new DnsResourceRecord());
        message.Authorities.Add(new DnsResourceRecord());
        message.Additionals.Add(new DnsResourceRecord());

        DnsEncodeResult result = DnsHeaderCodec.EncodeHeader(
            message,
            new DnsEncodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Bytes[5], Is.EqualTo(1));
            Assert.That(result.Bytes[7], Is.EqualTo(2));
            Assert.That(result.Bytes[9], Is.EqualTo(3));
            Assert.That(result.Bytes[11], Is.EqualTo(1));
            Assert.That(message.Header.QuestionCount, Is.EqualTo(99));
        });
    }

    [Test]
    public void ExplicitCountsMustMatchActualSections()
    {
        DnsMessage message = new DnsMessage();
        message.Header.QuestionCount = 1;
        DnsEncodeOptions options = new DnsEncodeOptions();
        options.DeriveSectionCounts = false;

        DnsEncodeResult result = DnsHeaderCodec.EncodeHeader(message, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Bytes, Is.Empty);
            Assert.That(result.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.SectionCountMismatch));
            Assert.That(result.Diagnostics[0].Offset, Is.EqualTo(4));
        });
    }

    [Test]
    public void DecodedCountsHonorPerSectionAndTotalLimits()
    {
        byte[] source =
        [
            0, 0, 0, 0,
            0, 2,
            0, 1,
            0, 1,
            0, 1
        ];
        DnsDecodeOptions sectionLimit = new DnsDecodeOptions();
        sectionLimit.MaximumSectionEntryCount = 1;
        DnsDecodeOptions totalLimit = new DnsDecodeOptions();
        totalLimit.MaximumSectionEntryCount = 2;
        totalLimit.MaximumTotalSectionEntryCount = 4;

        DnsDecodeResult excessiveSection = DnsHeaderCodec.DecodeHeader(
            source,
            sectionLimit);
        DnsDecodeResult excessiveTotal = DnsHeaderCodec.DecodeHeader(
            source,
            totalLimit);

        Assert.Multiple(() =>
        {
            AssertFailure(
                excessiveSection,
                DnsDiagnosticCode.ResourceLimitExceeded,
                4);
            AssertFailure(
                excessiveTotal,
                DnsDiagnosticCode.ResourceLimitExceeded,
                4);
        });
    }

    [Test]
    public void TruncatedHeaderReportsTheFirstUnavailableOffset()
    {
        DnsDecodeResult result = DnsHeaderCodec.DecodeHeader(
            new byte[11],
            new DnsDecodeOptions());

        AssertFailure(result, DnsDiagnosticCode.UnexpectedEnd, 11);
    }

    [Test]
    public void InvalidHeaderValuesFailEncoding()
    {
        DnsMessage opcode = new DnsMessage();
        opcode.Header.OpcodeValue = 16;
        DnsMessage responseCode = new DnsMessage();
        responseCode.Header.ResponseCodeValue = 16;
        DnsMessage flags = new DnsMessage();
        flags.Header.Flags = (DnsHeaderFlag)1;

        DnsEncodeResult opcodeResult = DnsHeaderCodec.EncodeHeader(
            opcode,
            new DnsEncodeOptions());
        DnsEncodeResult responseCodeResult = DnsHeaderCodec.EncodeHeader(
            responseCode,
            new DnsEncodeOptions());
        DnsEncodeResult flagsResult = DnsHeaderCodec.EncodeHeader(
            flags,
            new DnsEncodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(opcodeResult.Success, Is.False);
            Assert.That(responseCodeResult.Success, Is.False);
            Assert.That(flagsResult.Success, Is.False);
            Assert.That(opcodeResult.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.ValueOutOfRange));
            Assert.That(responseCodeResult.Diagnostics[0].Offset, Is.EqualTo(3));
            Assert.That(flagsResult.Diagnostics[0].Field, Is.EqualTo("header.flags"));
        });
    }

    private static DnsMessage PopulateSections(DnsMessage message)
    {
        int index = 0;
        while (index < message.Header.QuestionCount)
        {
            message.Questions.Add(new DnsQuestion());
            index = index + 1;
        }

        index = 0;
        while (index < message.Header.AnswerCount)
        {
            message.Answers.Add(new DnsResourceRecord());
            index = index + 1;
        }

        index = 0;
        while (index < message.Header.AuthorityCount)
        {
            message.Authorities.Add(new DnsResourceRecord());
            index = index + 1;
        }

        index = 0;
        while (index < message.Header.AdditionalCount)
        {
            message.Additionals.Add(new DnsResourceRecord());
            index = index + 1;
        }

        return message;
    }

    private static void AssertFailure(
        DnsDecodeResult result,
        DnsDiagnosticCode code,
        int offset)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Message, Is.Null);
        Assert.That(result.Diagnostics[0].Code, Is.EqualTo(code));
        Assert.That(result.Diagnostics[0].HasOffset, Is.True);
        Assert.That(result.Diagnostics[0].Offset, Is.EqualTo(offset));
    }
}
