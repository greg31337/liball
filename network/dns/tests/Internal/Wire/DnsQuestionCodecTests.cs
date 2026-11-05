namespace dns.Tests.Internal.Wire;

public class DnsQuestionCodecTests
{
    [Test]
    public void StandaloneQuestionRoundTripsKnownTypeAndClass()
    {
        DnsQuestion question = new DnsQuestion();
        question.Name = Parse("WwW.Example.");
        question.RecordType = DnsRecordType.A;
        question.Class = DnsClass.Internet;

        DnsEncodeResult encoded = DnsQuestionCodec.EncodeQuestion(
            question,
            new DnsEncodeOptions());
        DnsDecodeResult decoded = DnsQuestionCodec.DecodeQuestion(
            encoded.Bytes,
            0,
            new DnsDecodeOptions(),
            true);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.BytesWritten, Is.EqualTo(17));
            Assert.That(encoded.Bytes, Is.EqualTo(new byte[]
            {
                3, 0x57, 0x77, 0x57,
                7, 0x45, 0x78, 0x61, 0x6d, 0x70, 0x6c, 0x65, 0,
                0, 1,
                0, 1
            }));
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.BytesConsumed, Is.EqualTo(17));
            Assert.That(Format(decoded.Question!.Name), Is.EqualTo("WwW.Example."));
            Assert.That(decoded.Question.RecordType, Is.EqualTo(DnsRecordType.A));
            Assert.That(decoded.Question.RecordTypeValue, Is.EqualTo(1));
            Assert.That(decoded.Question.Class, Is.EqualTo(DnsClass.Internet));
            Assert.That(decoded.Question.ClassValue, Is.EqualTo(1));
        });
    }

    [Test]
    public void UnknownQuestionTypeAndClassRoundTripNumerically()
    {
        DnsQuestion question = new DnsQuestion();
        question.Name = Parse("example.");
        question.RecordTypeValue = 65000;
        question.ClassValue = 65280;

        DnsEncodeResult encoded = DnsQuestionCodec.EncodeQuestion(
            question,
            new DnsEncodeOptions());
        DnsDecodeResult decoded = DnsQuestionCodec.DecodeQuestion(
            encoded.Bytes,
            0,
            new DnsDecodeOptions(),
            true);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.Question!.RecordTypeValue, Is.EqualTo(65000));
            Assert.That((int)decoded.Question.RecordType, Is.EqualTo(65000));
            Assert.That(decoded.Question.ClassValue, Is.EqualTo(65280));
            Assert.That((int)decoded.Question.Class, Is.EqualTo(65280));
        });
    }

    [Test]
    public void QuestionDecoderFollowsPointersIntoTheContainingMessage()
    {
        byte[] message =
        [
            7, 0x65, 0x78, 0x61, 0x6d, 0x70, 0x6c, 0x65, 0,
            0xc0, 0x00,
            0, 28,
            0, 1
        ];

        DnsDecodeResult result = DnsQuestionCodec.DecodeQuestion(
            message,
            9,
            new DnsDecodeOptions(),
            true);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.BytesConsumed, Is.EqualTo(6));
            Assert.That(Format(result.Question!.Name), Is.EqualTo("example."));
            Assert.That(result.Question.RecordType, Is.EqualTo(DnsRecordType.Aaaa));
            Assert.That(result.Question.Class, Is.EqualTo(DnsClass.Internet));
        });
    }

    [Test]
    public void SharedCompressionTableCompressesLaterQuestionSuffixes()
    {
        byte[] destination = new byte[40];
        WireWriter writer = new WireWriter(destination, 0, destination.Length);
        CompressionTable table = new CompressionTable(0);
        DnsEncodeOptions options = new DnsEncodeOptions();
        DnsQuestion first = CreateQuestion("example.");
        DnsQuestion second = CreateQuestion("www.example.");
        DnsQuestionCodec.WriteQuestion(writer, table, first, options);

        WireWriteResult result = DnsQuestionCodec.WriteQuestion(
            writer,
            table,
            second,
            options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.StartOffset, Is.EqualTo(13));
            Assert.That(result.BytesWritten, Is.EqualTo(10));
            Assert.That(destination[17], Is.EqualTo(0xc0));
            Assert.That(destination[18], Is.Zero);
            DnsDecodeResult decoded = DnsQuestionCodec.DecodeQuestion(
                writer.CopyWrittenBytes(),
                13,
                new DnsDecodeOptions(),
                true);
            Assert.That(Format(decoded.Question!.Name), Is.EqualTo("www.example."));
        });
    }

    [Test]
    public void CompleteInputPolicyRejectsOrRetainsTrailingBytes()
    {
        byte[] source = [0, 0, 1, 0, 1, 0xaa, 0xbb];

        DnsDecodeResult strict = DnsQuestionCodec.DecodeQuestion(
            source,
            0,
            new DnsDecodeOptions(),
            true);
        DnsDecodeResult permissive = DnsQuestionCodec.DecodeQuestion(
            source,
            0,
            new DnsDecodeOptions(),
            false);

        Assert.Multiple(() =>
        {
            AssertFailure(strict, DnsDiagnosticCode.TrailingData, 5);
            Assert.That(permissive.Success, Is.True);
            Assert.That(permissive.BytesConsumed, Is.EqualTo(5));
            Assert.That(permissive.TrailingBytes,
                Is.EqualTo(new byte[] { 0xaa, 0xbb }));
        });
    }

    [Test]
    public void TruncatedTypeAndClassReportTheirFieldOffsets()
    {
        DnsDecodeResult type = DnsQuestionCodec.DecodeQuestion(
            new byte[] { 0, 0 },
            0,
            new DnsDecodeOptions(),
            true);
        DnsDecodeResult dnsClass = DnsQuestionCodec.DecodeQuestion(
            new byte[] { 0, 0, 1, 0 },
            0,
            new DnsDecodeOptions(),
            true);

        Assert.Multiple(() =>
        {
            AssertFailure(type, DnsDiagnosticCode.UnexpectedEnd, 1);
            AssertFailure(dnsClass, DnsDiagnosticCode.UnexpectedEnd, 3);
        });
    }

    [Test]
    public void CanonicalQuestionEncodingLowercasesItsName()
    {
        DnsQuestion question = CreateQuestion("WwW.Example.");
        DnsEncodeOptions options = new DnsEncodeOptions();
        options.Canonical = true;

        DnsEncodeResult result = DnsQuestionCodec.EncodeQuestion(question, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            DnsDecodeResult decoded = DnsQuestionCodec.DecodeQuestion(
                result.Bytes,
                0,
                new DnsDecodeOptions(),
                true);
            Assert.That(Format(decoded.Question!.Name),
                Is.EqualTo("www.example."));
            Assert.That(Format(question.Name), Is.EqualTo("WwW.Example."));
        });
    }

    [Test]
    public void InvalidQuestionValuesAndRelativeNamesFailEncoding()
    {
        DnsQuestion type = CreateQuestion("example.");
        type.RecordTypeValue = 65536;
        DnsQuestion dnsClass = CreateQuestion("example.");
        dnsClass.ClassValue = -1;
        DnsQuestion relative = CreateQuestion("example");

        DnsEncodeResult typeResult = DnsQuestionCodec.EncodeQuestion(
            type,
            new DnsEncodeOptions());
        DnsEncodeResult classResult = DnsQuestionCodec.EncodeQuestion(
            dnsClass,
            new DnsEncodeOptions());
        DnsEncodeResult relativeResult = DnsQuestionCodec.EncodeQuestion(
            relative,
            new DnsEncodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(typeResult.Success, Is.False);
            Assert.That(typeResult.Diagnostics[0].Field, Is.EqualTo("question.type"));
            Assert.That(classResult.Success, Is.False);
            Assert.That(classResult.Diagnostics[0].Field,
                Is.EqualTo("question.class"));
            Assert.That(relativeResult.Success, Is.False);
            Assert.That(relativeResult.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.InvalidLabel));
        });
    }

    [Test]
    public void QuestionEncodingHonorsMessageLengthLimit()
    {
        DnsEncodeOptions options = new DnsEncodeOptions();
        options.MaximumMessageLength = 4;

        DnsEncodeResult result = DnsQuestionCodec.EncodeQuestion(
            CreateQuestion("."),
            options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Bytes, Is.Empty);
            Assert.That(result.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.ResourceLimitExceeded));
        });
    }

    private static DnsQuestion CreateQuestion(string name)
    {
        DnsQuestion question = new DnsQuestion();
        question.Name = Parse(name);
        question.RecordType = DnsRecordType.A;
        question.Class = DnsClass.Internet;
        return question;
    }

    private static DnsName Parse(string text)
    {
        return PresentationDnsName.ParseName(text, null).Name!;
    }

    private static string Format(DnsName name)
    {
        return PresentationDnsName.FormatName(name).Text!;
    }

    private static void AssertFailure(
        DnsDecodeResult result,
        DnsDiagnosticCode code,
        int offset)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Question, Is.Null);
        Assert.That(result.Diagnostics[0].Code, Is.EqualTo(code));
        Assert.That(result.Diagnostics[0].Offset, Is.EqualTo(offset));
    }
}
