namespace dns.Tests.Internal.Messages;

public class DnsMessageTruncatorTests
{
    [Test]
    public void FittingMessageIsReturnedWithoutTruncation()
    {
        DnsMessage message = new DnsMessage();
        message.Header.Flags = DnsHeaderFlag.Response;
        message.Questions.Add(Question("example."));
        DnsEncodeOptions options = TruncationOptions(512);

        DnsEncodeResult result = DnsMessageEncoder.EncodeMessage(
            message, options);
        DnsDecodeResult decoded = DnsMessageDecoder.DecodeMessage(
            result.Bytes, new DnsDecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Truncated, Is.False);
            Assert.That(decoded.Message!.Header.Flags & DnsHeaderFlag.Truncated,
                Is.EqualTo(DnsHeaderFlag.None));
            Assert.That(decoded.Message.Questions, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void RemovesAnEntireTailRecordSetWhenOneRecordWouldFit()
    {
        DnsResourceRecord keep = OpaqueRecord(
            "keep.example.", 65000, Sequence(20, 1));
        DnsResourceRecord dropFirst = OpaqueRecord(
            "drop.example.", 65001, Sequence(30, 31));
        DnsResourceRecord dropSecond = OpaqueRecord(
            "DROP.EXAMPLE.", 65001, Sequence(30, 61));
        DnsMessage oneDropRecord = MessageWithQuestion();
        oneDropRecord.Answers.Add(keep);
        oneDropRecord.Answers.Add(dropFirst);
        int oneRecordLength = EncodeLength(oneDropRecord);
        DnsMessage message = MessageWithQuestion();
        message.Answers.Add(keep);
        message.Answers.Add(dropFirst);
        message.Answers.Add(dropSecond);

        DnsEncodeResult result = DnsMessageEncoder.EncodeMessage(
            message, TruncationOptions(oneRecordLength));
        DnsDecodeResult decoded = DnsMessageDecoder.DecodeMessage(
            result.Bytes, new DnsDecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Truncated, Is.True);
            Assert.That(result.BytesWritten, Is.LessThanOrEqualTo(oneRecordLength));
            Assert.That(decoded.Message!.Answers, Has.Count.EqualTo(1));
            Assert.That(Format(decoded.Message.Answers[0].Name),
                Is.EqualTo("keep.example."));
            Assert.That(decoded.Message.Header.Flags & DnsHeaderFlag.Truncated,
                Is.EqualTo(DnsHeaderFlag.Truncated));
            Assert.That(message.Answers, Has.Count.EqualTo(3));
        });
    }

    [Test]
    public void RemovesAdditionalThenAuthorityBeforeAnswer()
    {
        DnsResourceRecord answer = OpaqueRecord(
            "answer.example.", 65000, Sequence(20, 1));
        DnsResourceRecord authority = OpaqueRecord(
            "authority.example.", 65001, Sequence(20, 21));
        DnsResourceRecord additional = OpaqueRecord(
            "additional.example.", 65002, Sequence(20, 41));
        DnsMessage retained = MessageWithQuestion();
        retained.Answers.Add(answer);
        retained.Authorities.Add(authority);
        int retainedLength = EncodeLength(retained);
        DnsMessage message = MessageWithQuestion();
        message.Answers.Add(answer);
        message.Authorities.Add(authority);
        message.Additionals.Add(additional);

        DnsEncodeResult result = DnsMessageEncoder.EncodeMessage(
            message, TruncationOptions(retainedLength));
        DnsDecodeResult decoded = DnsMessageDecoder.DecodeMessage(
            result.Bytes, new DnsDecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(decoded.Message!.Answers, Has.Count.EqualTo(1));
            Assert.That(decoded.Message.Authorities, Has.Count.EqualTo(1));
            Assert.That(decoded.Message.Additionals, Is.Empty);
            Assert.That(decoded.Message.Header.AnswerCount, Is.EqualTo(1));
            Assert.That(decoded.Message.Header.AuthorityCount, Is.EqualTo(1));
            Assert.That(decoded.Message.Header.AdditionalCount, Is.EqualTo(0));
        });
    }

    [Test]
    public void MaximumMessageLengthAlsoBoundsTruncatedUdpOutput()
    {
        DnsMessage retained = MessageWithQuestion();
        retained.Answers.Add(OpaqueRecord(
            "keep.example.", 65000, Sequence(20, 1)));
        int retainedLength = EncodeLength(retained);
        DnsMessage message = MessageWithQuestion();
        message.Answers.Add(retained.Answers[0]);
        message.Authorities.Add(OpaqueRecord(
            "drop.example.", 65001, Sequence(40, 21)));
        DnsEncodeOptions options = TruncationOptions(65535);
        options.MaximumMessageLength = retainedLength;

        DnsEncodeResult result = DnsMessageEncoder.EncodeMessage(
            message, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Truncated, Is.True);
            Assert.That(result.BytesWritten, Is.LessThanOrEqualTo(retainedLength));
            Assert.That(result.Message!.Answers, Has.Count.EqualTo(1));
            Assert.That(result.Message.Authorities, Is.Empty);
        });
    }

    [Test]
    public void ClassicAndEdnsUdpBudgetsProduceExpectedBoundaries()
    {
        DnsMessage message = MessageWithQuestion();
        message.Answers.Add(OpaqueRecord(
            "large.example.", 65000, Sequence(600, 1)));
        DnsEncodeOptions classicOptions = TruncationOptions(
            DnsProtocolLimits.ClassicUdpPayloadSize);
        DnsEncodeOptions ednsOptions = TruncationOptions(1232);

        DnsEncodeResult classic = DnsMessageEncoder.EncodeMessage(
            message, classicOptions);
        DnsEncodeResult edns = DnsMessageEncoder.EncodeMessage(
            message, ednsOptions);

        Assert.Multiple(() =>
        {
            Assert.That(classic.Success, Is.True);
            Assert.That(classic.Truncated, Is.True);
            Assert.That(classic.BytesWritten,
                Is.LessThanOrEqualTo(DnsProtocolLimits.ClassicUdpPayloadSize));
            Assert.That(classic.Message!.Answers, Is.Empty);
            Assert.That(edns.Success, Is.True);
            Assert.That(edns.Truncated, Is.False);
            Assert.That(edns.BytesWritten,
                Is.GreaterThan(DnsProtocolLimits.ClassicUdpPayloadSize));
            Assert.That(edns.BytesWritten, Is.LessThanOrEqualTo(1232));
            Assert.That(edns.Message!.Answers, Has.Count.EqualTo(1));
        });
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(11)]
    public void BudgetsSmallerThanTheHeaderFailEvenWithoutSections(int budget)
    {
        DnsEncodeOptions options = TruncationOptions(budget);
        options.PreserveQuestionOnTruncation = false;

        DnsEncodeResult result = DnsMessageEncoder.EncodeMessage(
            new DnsMessage(), options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.ResourceLimitExceeded));
            Assert.That(result.Bytes, Is.Empty);
        });
    }

    [Test]
    public void InterleavedAdditionalRecordSetIsRemovedAtomicallyAndOptRemains()
    {
        DnsResourceRecord dropFirst = OpaqueRecord(
            "drop.example.", 65000, Sequence(40, 1));
        DnsResourceRecord keep = OpaqueRecord(
            "keep.example.", 65001, Sequence(20, 41));
        DnsResourceRecord dropSecond = OpaqueRecord(
            "DROP.EXAMPLE.", 65000, Sequence(40, 61));
        DnsResourceRecord opt = OptRecord();
        DnsMessage retained = MessageWithQuestion();
        retained.Additionals.Add(keep);
        retained.Additionals.Add(opt);
        int retainedLength = EncodeLength(retained);
        DnsMessage message = MessageWithQuestion();
        message.Additionals.Add(dropFirst);
        message.Additionals.Add(keep);
        message.Additionals.Add(dropSecond);
        message.Additionals.Add(opt);

        DnsEncodeResult result = DnsMessageEncoder.EncodeMessage(
            message, TruncationOptions(retainedLength));
        DnsDecodeResult decoded = DnsMessageDecoder.DecodeMessage(
            result.Bytes, new DnsDecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Truncated, Is.True);
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.Message!.Additionals
                .Select(record => record.RecordTypeValue),
                Is.EqualTo(new[] { 65001, 41 }));
            Assert.That(message.Additionals, Has.Count.EqualTo(4));
        });
    }

    [Test]
    public void QuestionIsPreservedByDefaultAndCanBeRemovedForTinyBudget()
    {
        DnsMessage message = MessageWithQuestion();
        DnsEncodeOptions preserve = TruncationOptions(
            DnsProtocolLimits.HeaderLength);
        DnsEncodeOptions remove = TruncationOptions(
            DnsProtocolLimits.HeaderLength);
        remove.PreserveQuestionOnTruncation = false;

        DnsEncodeResult preserved = DnsMessageEncoder.EncodeMessage(
            message, preserve);
        DnsEncodeResult removed = DnsMessageEncoder.EncodeMessage(
            message, remove);
        DnsDecodeResult decoded = DnsMessageDecoder.DecodeMessage(
            removed.Bytes, new DnsDecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(preserved.Success, Is.False);
            Assert.That(preserved.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.ResourceLimitExceeded));
            Assert.That(removed.Success, Is.True);
            Assert.That(removed.Truncated, Is.True);
            Assert.That(removed.BytesWritten,
                Is.EqualTo(DnsProtocolLimits.HeaderLength));
            Assert.That(decoded.Message!.Questions, Is.Empty);
            Assert.That(decoded.Message.Header.Flags & DnsHeaderFlag.Truncated,
                Is.EqualTo(DnsHeaderFlag.Truncated));
            Assert.That(message.Questions, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void TruncatingAStampedMessageRemovesTsigBeforeOtherAdditionals()
    {
        DnsResourceRecord ordinary = OpaqueRecord(
            "extra.example.", 65000, Sequence(12, 1));
        DnsMessage unsigned = MessageWithQuestion();
        unsigned.Additionals.Add(ordinary);
        int unsignedLength = EncodeLength(unsigned);
        DnsMessage signed = MessageWithQuestion();
        signed.Additionals.Add(ordinary);
        signed.Additionals.Add(TsigRecord());

        DnsEncodeResult result = DnsMessageEncoder.EncodeMessage(
            signed, TruncationOptions(unsignedLength));
        DnsDecodeResult decoded = DnsMessageDecoder.DecodeMessage(
            result.Bytes, new DnsDecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Truncated, Is.True);
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.Message!.Additionals, Has.Count.EqualTo(1));
            Assert.That(decoded.Message.Additionals[0].RecordTypeValue,
                Is.EqualTo(65000));
        });
    }

    [Test]
    public void InvalidUdpLimitsAndInvalidRecordsReturnDiagnostics()
    {
        DnsEncodeOptions invalidLimit = TruncationOptions(65536);
        DnsMessage invalidMessage = new DnsMessage();
        invalidMessage.Answers.Add(OpaqueRecord(
            "example.", 255, [1]));

        DnsEncodeResult limit = DnsMessageEncoder.EncodeMessage(
            new DnsMessage(), invalidLimit);
        DnsEncodeResult record = DnsMessageEncoder.EncodeMessage(
            invalidMessage, TruncationOptions(12));

        Assert.Multiple(() =>
        {
            Assert.That(limit.Success, Is.False);
            Assert.That(limit.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.ValueOutOfRange));
            Assert.That(limit.Diagnostics[0].Field,
                Is.EqualTo("options.MaximumUdpPayloadSize"));
            Assert.That(record.Success, Is.False);
            Assert.That(record.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.InvalidRecordData));
        });
    }

    static DnsMessage MessageWithQuestion()
    {
        DnsMessage message = new DnsMessage();
        message.Header.Identifier = 4660;
        message.Header.Flags = DnsHeaderFlag.Response |
            DnsHeaderFlag.RecursionDesired;
        message.Questions.Add(Question("example."));
        return message;
    }

    static DnsEncodeOptions TruncationOptions(int udpPayloadSize)
    {
        DnsEncodeOptions options = new DnsEncodeOptions();
        options.TruncateToMaximumLength = true;
        options.MaximumUdpPayloadSize = udpPayloadSize;
        return options;
    }

    static int EncodeLength(DnsMessage message)
    {
        DnsEncodeResult result = DnsMessageEncoder.EncodeMessage(
            message, new DnsEncodeOptions());
        Assert.That(result.Success, Is.True);
        return result.BytesWritten;
    }

    static DnsQuestion Question(string owner)
    {
        DnsQuestion question = new DnsQuestion();
        question.Name = Parse(owner);
        question.RecordType = DnsRecordType.A;
        question.RecordTypeValue = 1;
        question.Class = DnsClass.Internet;
        question.ClassValue = 1;
        return question;
    }

    static DnsResourceRecord OpaqueRecord(
        string owner,
        int recordType,
        byte[] data)
    {
        DnsResourceRecord record = new DnsResourceRecord();
        record.Name = Parse(owner);
        record.RecordType = (DnsRecordType)recordType;
        record.RecordTypeValue = recordType;
        record.Class = DnsClass.Internet;
        record.ClassValue = 1;
        record.TimeToLive = 60;
        record.UseRawData = true;
        record.RawData = data;
        return record;
    }

    static DnsResourceRecord TsigRecord()
    {
        DnsRecordData data = new DnsRecordData();
        data.Tsig = new DnsTsigData();
        data.Tsig.AlgorithmName = Parse("hmac-sha256.");
        data.Tsig.TimeSigned = 123456;
        data.Tsig.Fudge = 300;
        data.Tsig.Mac = [1, 2, 3];
        data.Tsig.OriginalIdentifier = 4660;
        DnsResourceRecord record = new DnsResourceRecord();
        record.Name = Parse("key.example.");
        record.RecordType = DnsRecordType.Tsig;
        record.RecordTypeValue = 250;
        record.Class = DnsClass.Any;
        record.ClassValue = 255;
        record.Data = data;
        return record;
    }

    static DnsResourceRecord OptRecord()
    {
        DnsResourceRecord record = new DnsResourceRecord();
        record.Name = Parse(".");
        record.RecordType = DnsRecordType.Opt;
        record.RecordTypeValue = 41;
        record.Data = new DnsRecordData();
        record.Data.Edns = new DnsEdnsData();
        record.Data.Edns.UdpPayloadSize = 1232;
        return record;
    }

    static byte[] Sequence(int length, int start)
    {
        return Enumerable.Range(start, length)
            .Select(value => (byte)value)
            .ToArray();
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
