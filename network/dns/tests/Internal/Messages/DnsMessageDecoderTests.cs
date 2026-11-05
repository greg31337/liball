namespace dns.Tests.Internal.Messages;

public class DnsMessageDecoderTests
{
    [Test]
    public void DecodesEveryDeclaredSectionAndTypedRecordData()
    {
        byte[] wire = Header(1, 1, 1, 1)
            .Concat(QuestionWire())
            .Concat(RecordWire(1, [192, 0, 2, 1]))
            .Concat(RecordWire(2, [0]))
            .Concat(RecordWire(65000, [9, 8, 7]))
            .ToArray();

        DnsDecodeResult result = DnsMessageDecoder.DecodeMessage(
            wire, new DnsDecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.BytesConsumed, Is.EqualTo(wire.Length));
            Assert.That(result.TrailingBytes, Is.Empty);
            Assert.That(result.Message!.Header.QuestionCount, Is.EqualTo(1));
            Assert.That(result.Message.Questions, Has.Count.EqualTo(1));
            Assert.That(result.Message.Answers, Has.Count.EqualTo(1));
            Assert.That(result.Message.Authorities, Has.Count.EqualTo(1));
            Assert.That(result.Message.Additionals, Has.Count.EqualTo(1));
            Assert.That(result.Message.Answers[0].Data!.Address!.Address,
                Is.EqualTo(new byte[] { 192, 0, 2, 1 }));
            Assert.That(result.Message.Authorities[0].Data!.Name!.Name.IsAbsolute,
                Is.True);
            Assert.That(result.Message.Additionals[0].Data!.Opaque!.Bytes,
                Is.EqualTo(new byte[] { 9, 8, 7 }));
        });
    }

    [Test]
    public void DecodesCompressionPointersAcrossMessageSections()
    {
        byte[] name = [7, 101, 120, 97, 109, 112, 108, 101, 0];
        byte[] question = name.Concat(new byte[] { 0, 1, 0, 1 }).ToArray();
        byte[] answer = new byte[] { 192, 12 }
            .Concat(RecordHeader(1, 4))
            .Concat(new byte[] { 203, 0, 113, 5 })
            .ToArray();
        byte[] wire = Header(1, 1, 0, 0)
            .Concat(question)
            .Concat(answer)
            .ToArray();

        DnsDecodeResult result = DnsMessageDecoder.DecodeMessage(
            wire, new DnsDecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(Format(result.Message!.Questions[0].Name),
                Is.EqualTo("example."));
            Assert.That(Format(result.Message.Answers[0].Name),
                Is.EqualTo("example."));
            Assert.That(result.Message.Answers[0].RawData,
                Is.EqualTo(new byte[] { 203, 0, 113, 5 }));
        });
    }

    [Test]
    public void DecodesMixedLiteralAndCompressedNamesAcrossRecordData()
    {
        byte[] question =
        [
            7, 115, 101, 114, 118, 105, 99, 101,
            7, 101, 120, 97, 109, 112, 108, 101,
            0, 0, 1, 0, 1
        ];
        byte[] answer = RecordWireWithOwner(
            [192, 12], 5,
            [5, 97, 108, 105, 97, 115, 192, 20]);
        byte[] authority = RecordWireWithOwner(
            [192, 20], 2,
            [2, 110, 115, 192, 20]);
        byte[] additional = RecordWireWithOwner(
            [2, 110, 115, 192, 20], 1,
            [192, 0, 2, 1]);
        byte[] wire = Header(1, 1, 1, 1)
            .Concat(question)
            .Concat(answer)
            .Concat(authority)
            .Concat(additional)
            .ToArray();

        DnsDecodeResult result = DnsMessageDecoder.DecodeMessage(
            wire, new DnsDecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(Format(result.Message!.Questions[0].Name),
                Is.EqualTo("service.example."));
            Assert.That(Format(result.Message.Answers[0].Name),
                Is.EqualTo("service.example."));
            Assert.That(Format(result.Message.Answers[0].Data!.Name!.Name),
                Is.EqualTo("alias.example."));
            Assert.That(Format(result.Message.Authorities[0].Name),
                Is.EqualTo("example."));
            Assert.That(Format(result.Message.Authorities[0].Data!.Name!.Name),
                Is.EqualTo("ns.example."));
            Assert.That(Format(result.Message.Additionals[0].Name),
                Is.EqualTo("ns.example."));
        });
    }

    [Test]
    public void StrictModeRejectsTrailingDataAndPermissiveModeReturnsIt()
    {
        byte[] wire = Header(0, 0, 0, 0)
            .Concat(new byte[] { 222, 173 })
            .ToArray();
        DnsDecodeOptions permissive = new DnsDecodeOptions();
        permissive.RequireCompleteInput = false;

        DnsDecodeResult strictResult = DnsMessageDecoder.DecodeMessage(
            wire, new DnsDecodeOptions());
        DnsDecodeResult permissiveResult = DnsMessageDecoder.DecodeMessage(
            wire, permissive);

        Assert.Multiple(() =>
        {
            Assert.That(strictResult.Success, Is.False);
            Assert.That(strictResult.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.TrailingData));
            Assert.That(strictResult.Diagnostics[0].Offset,
                Is.EqualTo(DnsProtocolLimits.HeaderLength));
            Assert.That(permissiveResult.Success, Is.True);
            Assert.That(permissiveResult.BytesConsumed,
                Is.EqualTo(DnsProtocolLimits.HeaderLength));
            Assert.That(permissiveResult.TrailingBytes,
                Is.EqualTo(new byte[] { 222, 173 }));
        });
    }

    [TestCase(DnsMessageSection.Question)]
    [TestCase(DnsMessageSection.Answer)]
    [TestCase(DnsMessageSection.Authority)]
    [TestCase(DnsMessageSection.Additional)]
    public void TruncatedEntriesReportTheirSectionAndIndex(
        DnsMessageSection section)
    {
        byte[] wire = TruncatedSecondEntry(section);

        DnsDecodeResult result = DnsMessageDecoder.DecodeMessage(
            wire, new DnsDecodeOptions());
        DnsDiagnostic diagnostic = result.Diagnostics[0];

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(diagnostic.Code,
                Is.EqualTo(DnsDiagnosticCode.UnexpectedEnd));
            Assert.That(diagnostic.HasOffset, Is.True);
            Assert.That(diagnostic.Offset, Is.EqualTo(wire.Length));
            Assert.That(diagnostic.HasSection, Is.True);
            Assert.That(diagnostic.Section, Is.EqualTo(section));
            Assert.That(diagnostic.HasRecordIndex, Is.True);
            Assert.That(diagnostic.RecordIndex, Is.EqualTo(1));
        });
    }

    [TestCase(DnsMessageSection.Question)]
    [TestCase(DnsMessageSection.Answer)]
    [TestCase(DnsMessageSection.Authority)]
    [TestCase(DnsMessageSection.Additional)]
    public void MissingDeclaredEntryReportsSectionCountMismatch(
        DnsMessageSection section)
    {
        byte[] wire = Header(
            section == DnsMessageSection.Question ? 1 : 0,
            section == DnsMessageSection.Answer ? 1 : 0,
            section == DnsMessageSection.Authority ? 1 : 0,
            section == DnsMessageSection.Additional ? 1 : 0);

        DnsDecodeResult result = DnsMessageDecoder.DecodeMessage(
            wire, new DnsDecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.SectionCountMismatch));
            Assert.That(result.Diagnostics[0].Section,
                Is.EqualTo(section));
            Assert.That(result.Diagnostics[0].RecordIndex, Is.EqualTo(0));
            Assert.That(result.Diagnostics[0].Offset, Is.EqualTo(wire.Length));
        });
    }

    [Test]
    public void RecordDataLengthFailuresReportTheirSectionAndRecordIndex()
    {
        byte[] truncated = Header(0, 1, 0, 0)
            .Concat(new byte[] { 0 })
            .Concat(RecordHeader(1, 4))
            .Concat(new byte[] { 192, 0, 2 })
            .ToArray();
        byte[] schemaMismatch = Header(0, 1, 0, 0)
            .Concat(RecordWire(1, [192, 0, 2, 1, 5]))
            .ToArray();

        DnsDecodeResult truncatedResult = DnsMessageDecoder.DecodeMessage(
            truncated, new DnsDecodeOptions());
        DnsDecodeResult mismatchResult = DnsMessageDecoder.DecodeMessage(
            schemaMismatch, new DnsDecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(truncatedResult.Success, Is.False);
            Assert.That(truncatedResult.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.RecordDataLengthMismatch));
            Assert.That(truncatedResult.Diagnostics[0].Field,
                Is.EqualTo("resourceRecord.recordDataLength"));
            Assert.That(truncatedResult.Diagnostics[0].Section,
                Is.EqualTo(DnsMessageSection.Answer));
            Assert.That(truncatedResult.Diagnostics[0].RecordIndex,
                Is.EqualTo(0));
            Assert.That(mismatchResult.Success, Is.False);
            Assert.That(mismatchResult.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.InvalidLength));
            Assert.That(mismatchResult.Diagnostics[0].Field,
                Is.EqualTo("address"));
            Assert.That(mismatchResult.Diagnostics[0].Section,
                Is.EqualTo(DnsMessageSection.Answer));
            Assert.That(mismatchResult.Diagnostics[0].RecordIndex,
                Is.EqualTo(0));
        });
    }

    [Test]
    public void EnforcesSectionTotalMessageRecordDataAndOptionLimits()
    {
        DnsDecodeOptions sectionLimit = new DnsDecodeOptions();
        sectionLimit.MaximumSectionEntryCount = 1;
        DnsDecodeOptions totalLimit = new DnsDecodeOptions();
        totalLimit.MaximumSectionEntryCount = 2;
        totalLimit.MaximumTotalSectionEntryCount = 1;
        byte[] answerWire = Header(0, 1, 0, 0)
            .Concat(RecordWire(1, [192, 0, 2, 1]))
            .ToArray();
        DnsDecodeOptions messageLimit = new DnsDecodeOptions();
        messageLimit.MaximumMessageLength = answerWire.Length - 1;
        DnsDecodeOptions dataLimit = new DnsDecodeOptions();
        dataLimit.MaximumRecordDataLength = 3;
        DnsDecodeOptions invalidOptions = new DnsDecodeOptions();
        invalidOptions.MaximumCompressionPointerHops = -1;

        DnsDecodeResult section = DnsMessageDecoder.DecodeMessage(
            Header(0, 2, 0, 0), sectionLimit);
        DnsDecodeResult total = DnsMessageDecoder.DecodeMessage(
            Header(1, 1, 0, 0), totalLimit);
        DnsDecodeResult message = DnsMessageDecoder.DecodeMessage(
            answerWire, messageLimit);
        DnsDecodeResult data = DnsMessageDecoder.DecodeMessage(
            answerWire, dataLimit);
        DnsDecodeResult options = DnsMessageDecoder.DecodeMessage(
            Header(0, 0, 0, 0), invalidOptions);

        Assert.Multiple(() =>
        {
            Assert.That(section.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.ResourceLimitExceeded));
            Assert.That(section.Diagnostics[0].Offset, Is.EqualTo(6));
            Assert.That(total.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.ResourceLimitExceeded));
            Assert.That(message.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.ResourceLimitExceeded));
            Assert.That(data.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.ResourceLimitExceeded));
            Assert.That(data.Diagnostics[0].Section,
                Is.EqualTo(DnsMessageSection.Answer));
            Assert.That(data.Diagnostics[0].RecordIndex, Is.EqualTo(0));
            Assert.That(options.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.ValueOutOfRange));
        });
    }

    [Test]
    public void TypedRecordFailuresCanFallBackToExactOpaqueData()
    {
        byte[] wire = Header(0, 1, 0, 0)
            .Concat(RecordWire(1, [192, 0, 2]))
            .ToArray();
        DnsDecodeOptions fallback = new DnsDecodeOptions();
        fallback.FallBackToOpaqueRecordData = true;

        DnsDecodeResult strict = DnsMessageDecoder.DecodeMessage(
            wire, new DnsDecodeOptions());
        DnsDecodeResult preserved = DnsMessageDecoder.DecodeMessage(
            wire, fallback);

        Assert.Multiple(() =>
        {
            Assert.That(strict.Success, Is.False);
            Assert.That(strict.Diagnostics[0].Section,
                Is.EqualTo(DnsMessageSection.Answer));
            Assert.That(strict.Diagnostics[0].RecordIndex, Is.EqualTo(0));
            Assert.That(preserved.Success, Is.True);
            Assert.That(preserved.Message!.Answers[0].RawData,
                Is.EqualTo(new byte[] { 192, 0, 2 }));
            Assert.That(preserved.Message.Answers[0].Data!.Opaque!.Bytes,
                Is.EqualTo(new byte[] { 192, 0, 2 }));
        });
    }

    [Test]
    public void RejectsTsigUnlessItIsTheFinalAdditionalRecord()
    {
        byte[] invalid = Header(0, 0, 0, 2)
            .Concat(TsigRecordWire())
            .Concat(RecordWire(1, [192, 0, 2, 1]))
            .ToArray();
        byte[] valid = Header(0, 0, 0, 2)
            .Concat(RecordWire(1, [192, 0, 2, 1]))
            .Concat(TsigRecordWire())
            .ToArray();

        DnsDecodeResult rejected = DnsMessageDecoder.DecodeMessage(
            invalid, new DnsDecodeOptions());
        DnsDecodeResult accepted = DnsMessageDecoder.DecodeMessage(
            valid, new DnsDecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(rejected.Success, Is.False);
            Assert.That(rejected.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.InvalidTsigPlacement));
            Assert.That(rejected.Diagnostics[0].Section,
                Is.EqualTo(DnsMessageSection.Additional));
            Assert.That(rejected.Diagnostics[0].RecordIndex, Is.EqualTo(0));
            Assert.That(accepted.Success, Is.True);
            Assert.That(accepted.Message!.Additionals[1].Data!.Tsig,
                Is.Not.Null);
        });
    }

    static byte[] TruncatedSecondEntry(DnsMessageSection section)
    {
        int questions = section == DnsMessageSection.Question ? 2 : 0;
        int answers = section == DnsMessageSection.Answer ? 2 : 0;
        int authorities = section == DnsMessageSection.Authority ? 2 : 0;
        int additionals = section == DnsMessageSection.Additional ? 2 : 0;
        byte[] first = section == DnsMessageSection.Question ?
            QuestionWire() : RecordWire(1, [192, 0, 2, 1]);
        return Header(questions, answers, authorities, additionals)
            .Concat(first)
            .Concat(new byte[] { 0 })
            .ToArray();
    }

    static byte[] Header(
        int questionCount,
        int answerCount,
        int authorityCount,
        int additionalCount)
    {
        byte[] bytes = new byte[DnsProtocolLimits.HeaderLength];
        bytes[0] = 18;
        bytes[1] = 52;
        bytes[2] = 129;
        bytes[3] = 128;
        DnsHeaderCodec.WriteUnsigned16(bytes, 4, questionCount);
        DnsHeaderCodec.WriteUnsigned16(bytes, 6, answerCount);
        DnsHeaderCodec.WriteUnsigned16(bytes, 8, authorityCount);
        DnsHeaderCodec.WriteUnsigned16(bytes, 10, additionalCount);
        return bytes;
    }

    static byte[] QuestionWire()
    {
        return [0, 0, 1, 0, 1];
    }

    static byte[] RecordWire(int recordType, byte[] data)
    {
        return new byte[] { 0 }
            .Concat(RecordHeader(recordType, data.Length))
            .Concat(data)
            .ToArray();
    }

    static byte[] RecordWireWithOwner(
        byte[] owner,
        int recordType,
        byte[] data)
    {
        return owner
            .Concat(RecordHeader(recordType, data.Length))
            .Concat(data)
            .ToArray();
    }

    static byte[] RecordHeader(int recordType, int dataLength)
    {
        return
        [
            (byte)(recordType >> 8),
            (byte)(recordType & 255),
            0,
            1,
            0,
            0,
            1,
            44,
            (byte)(dataLength >> 8),
            (byte)(dataLength & 255)
        ];
    }

    static byte[] TsigRecordWire()
    {
        byte[] data =
        [
            0,
            0, 0, 0, 0, 0, 0,
            0, 0,
            0, 0,
            18, 52,
            0, 0,
            0, 0
        ];
        byte[] header = RecordHeader(250, data.Length);
        header[2] = 0;
        header[3] = 255;
        header[4] = 0;
        header[5] = 0;
        header[6] = 0;
        header[7] = 0;
        return new byte[] { 0 }.Concat(header).Concat(data).ToArray();
    }

    static string Format(DnsName name)
    {
        return PresentationDnsName.FormatName(name).Text!;
    }
}
