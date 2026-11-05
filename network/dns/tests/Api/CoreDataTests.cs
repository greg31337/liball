namespace dns.Tests.Api;

public class CoreDataTests
{
    [Test]
    public void NameRetainsCasePreservingLabelOctets()
    {
        DnsName name = new DnsName();
        byte[] label = [0x57, 0x57, 0x77];
        name.Labels.Add(label);
        name.IsAbsolute = true;

        Assert.Multiple(() =>
        {
            Assert.That(name.Labels, Has.Count.EqualTo(1));
            Assert.That(name.Labels[0], Is.EqualTo(label));
            Assert.That(name.IsAbsolute, Is.True);
        });
    }

    [Test]
    public void MessageOwnsIndependentHeaderAndSectionCollections()
    {
        DnsMessage first = new DnsMessage();
        DnsMessage second = new DnsMessage();
        first.Header.Identifier = 0x1234;
        first.Questions.Add(new DnsQuestion());
        first.Answers.Add(new DnsResourceRecord());
        first.Authorities.Add(new DnsResourceRecord());
        first.Additionals.Add(new DnsResourceRecord());

        Assert.Multiple(() =>
        {
            Assert.That(first.Header.Identifier, Is.EqualTo(0x1234));
            Assert.That(first.Questions, Has.Count.EqualTo(1));
            Assert.That(first.Answers, Has.Count.EqualTo(1));
            Assert.That(first.Authorities, Has.Count.EqualTo(1));
            Assert.That(first.Additionals, Has.Count.EqualTo(1));
            Assert.That(second.Header.Identifier, Is.Zero);
            Assert.That(second.Questions, Is.Empty);
            Assert.That(second.Answers, Is.Empty);
            Assert.That(second.Authorities, Is.Empty);
            Assert.That(second.Additionals, Is.Empty);
            Assert.That(second.Header, Is.Not.SameAs(first.Header));
        });
    }

    [Test]
    public void HeaderRetainsRawAndRecognizedProtocolValues()
    {
        DnsMessageHeader header = new DnsMessageHeader();
        header.Opcode = DnsOpcode.StatefulOperations;
        header.OpcodeValue = 14;
        header.ResponseCode = DnsResponseCode.BadCookie;
        header.ResponseCodeValue = 4095;
        header.Flags = DnsHeaderFlag.Response | DnsHeaderFlag.AuthenticData;
        header.FlagsValue = 0x802f;
        header.QuestionCount = 1;
        header.AnswerCount = 2;
        header.AuthorityCount = 3;
        header.AdditionalCount = 4;

        Assert.Multiple(() =>
        {
            Assert.That(header.Opcode, Is.EqualTo(DnsOpcode.StatefulOperations));
            Assert.That(header.OpcodeValue, Is.EqualTo(14));
            Assert.That(header.ResponseCode, Is.EqualTo(DnsResponseCode.BadCookie));
            Assert.That(header.ResponseCodeValue, Is.EqualTo(4095));
            Assert.That(header.Flags, Is.EqualTo(
                DnsHeaderFlag.Response | DnsHeaderFlag.AuthenticData));
            Assert.That(header.FlagsValue, Is.EqualTo(0x802f));
            Assert.That(header.QuestionCount, Is.EqualTo(1));
            Assert.That(header.AnswerCount, Is.EqualTo(2));
            Assert.That(header.AuthorityCount, Is.EqualTo(3));
            Assert.That(header.AdditionalCount, Is.EqualTo(4));
        });
    }

    [Test]
    public void QuestionAndRecordRetainUnknownCodesAndOpaqueData()
    {
        DnsQuestion question = new DnsQuestion();
        question.RecordType = (DnsRecordType)65000;
        question.RecordTypeValue = 65000;
        question.Class = (DnsClass)65001;
        question.ClassValue = 65001;

        DnsResourceRecord record = new DnsResourceRecord();
        record.RecordType = question.RecordType;
        record.RecordTypeValue = question.RecordTypeValue;
        record.Class = question.Class;
        record.ClassValue = question.ClassValue;
        record.TimeToLive = uint.MaxValue;
        record.RawData = [0xde, 0xad, 0xbe, 0xef];
        record.UseRawData = true;

        Assert.Multiple(() =>
        {
            Assert.That(question.RecordTypeValue, Is.EqualTo(65000));
            Assert.That((int)question.RecordType, Is.EqualTo(65000));
            Assert.That(question.ClassValue, Is.EqualTo(65001));
            Assert.That((int)question.Class, Is.EqualTo(65001));
            Assert.That(record.TimeToLive, Is.EqualTo(uint.MaxValue));
            Assert.That(record.RawData, Is.EqualTo(new byte[] { 0xde, 0xad, 0xbe, 0xef }));
            Assert.That(record.UseRawData, Is.True);
        });
    }

    [Test]
    public void RecordSetOwnsItsRecordCollection()
    {
        DnsResourceRecordSet first = new DnsResourceRecordSet();
        DnsResourceRecordSet second = new DnsResourceRecordSet();
        first.RecordType = DnsRecordType.Aaaa;
        first.RecordTypeValue = 28;
        first.Class = DnsClass.Internet;
        first.ClassValue = 1;
        first.TimeToLive = 300;
        first.Records.Add(new DnsResourceRecord());

        Assert.Multiple(() =>
        {
            Assert.That(first.RecordTypeValue, Is.EqualTo(28));
            Assert.That(first.ClassValue, Is.EqualTo(1));
            Assert.That(first.TimeToLive, Is.EqualTo(300));
            Assert.That(first.Records, Has.Count.EqualTo(1));
            Assert.That(second.Records, Is.Empty);
            Assert.That(second.Name, Is.Not.SameAs(first.Name));
        });
    }

    [Test]
    public void CodecOptionsStartWithStrictBoundedDefaults()
    {
        DnsDecodeOptions decode = new DnsDecodeOptions();
        DnsEncodeOptions encode = new DnsEncodeOptions();

        Assert.Multiple(() =>
        {
            Assert.That(decode.RequireCompleteInput, Is.True);
            Assert.That(decode.RejectReservedHeaderBits, Is.True);
            Assert.That(decode.DecodeTypedRecordData, Is.True);
            Assert.That(decode.FallBackToOpaqueRecordData, Is.False);
            Assert.That(decode.MaximumMessageLength,
                Is.EqualTo(DnsProtocolLimits.MaximumTcpMessageLength));
            Assert.That(decode.MaximumSectionEntryCount,
                Is.EqualTo(DnsProtocolLimits.MaximumSectionEntryCount));
            Assert.That(decode.MaximumCompressionPointerHops,
                Is.EqualTo(DnsProtocolLimits.DefaultCompressionPointerHopLimit));
            Assert.That(encode.EnableCompression, Is.True);
            Assert.That(encode.Canonical, Is.False);
            Assert.That(encode.DeriveSectionCounts, Is.True);
            Assert.That(encode.TruncateToMaximumLength, Is.False);
            Assert.That(encode.PreserveQuestionOnTruncation, Is.True);
            Assert.That(encode.MaximumUdpPayloadSize,
                Is.EqualTo(DnsProtocolLimits.ClassicUdpPayloadSize));
            Assert.That(encode.CanonicalOrder.LowercaseNames, Is.True);
            Assert.That(encode.CanonicalOrder.DisableCompression, Is.True);
            Assert.That(encode.CanonicalOrder.SortRecords, Is.True);
            Assert.That(encode.CanonicalOrder.SuppressDuplicateRecords, Is.True);
        });
    }

    [Test]
    public void CodecResultsOwnBuffersAndDiagnosticCollections()
    {
        DnsDecodeResult firstDecode = new DnsDecodeResult();
        DnsDecodeResult secondDecode = new DnsDecodeResult();
        DnsEncodeResult firstEncode = new DnsEncodeResult();
        DnsEncodeResult secondEncode = new DnsEncodeResult();
        firstDecode.TrailingBytes = [0xaa];
        firstDecode.Diagnostics.Add(new DnsDiagnostic());
        firstEncode.Bytes = [0xbb];
        firstEncode.Diagnostics.Add(new DnsDiagnostic());

        Assert.Multiple(() =>
        {
            Assert.That(firstDecode.TrailingBytes, Is.EqualTo(new byte[] { 0xaa }));
            Assert.That(firstDecode.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(secondDecode.TrailingBytes, Is.Empty);
            Assert.That(secondDecode.Diagnostics, Is.Empty);
            Assert.That(firstEncode.Bytes, Is.EqualTo(new byte[] { 0xbb }));
            Assert.That(firstEncode.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(secondEncode.Bytes, Is.Empty);
            Assert.That(secondEncode.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void DiagnosticAndTcpFrameRetainLocationAndPartialFrameState()
    {
        DnsDiagnostic diagnostic = new DnsDiagnostic();
        diagnostic.Code = DnsDiagnosticCode.RecordDataLengthMismatch;
        diagnostic.HasOffset = true;
        diagnostic.Offset = 42;
        diagnostic.HasSection = true;
        diagnostic.Section = DnsMessageSection.Answer;
        diagnostic.HasRecordIndex = true;
        diagnostic.RecordIndex = 3;
        diagnostic.Field = "RDLENGTH";
        diagnostic.Reason = "The record data ended early.";

        DnsTcpFrame frame = new DnsTcpFrame();
        frame.MessageLength = 12;
        frame.MessageBytes = [0x00, 0x01];
        frame.Complete = false;

        Assert.Multiple(() =>
        {
            Assert.That(diagnostic.Code,
                Is.EqualTo(DnsDiagnosticCode.RecordDataLengthMismatch));
            Assert.That(diagnostic.Offset, Is.EqualTo(42));
            Assert.That(diagnostic.Section, Is.EqualTo(DnsMessageSection.Answer));
            Assert.That(diagnostic.RecordIndex, Is.EqualTo(3));
            Assert.That(diagnostic.Field, Is.EqualTo("RDLENGTH"));
            Assert.That(frame.MessageLength, Is.EqualTo(12));
            Assert.That(frame.MessageBytes, Is.EqualTo(new byte[] { 0x00, 0x01 }));
            Assert.That(frame.Complete, Is.False);
        });
    }
}
