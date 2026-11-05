namespace dns.Tests.Internal.Messages;

public class DnsMessageEncoderTests
{
    [Test]
    public void EncodesAndDecodesEverySectionWithSharedCompression()
    {
        DnsMessage message = new DnsMessage();
        message.Header.Identifier = 4660;
        message.Header.Flags = DnsHeaderFlag.Response |
            DnsHeaderFlag.RecursionDesired;
        message.Questions.Add(Question("host.example."));
        message.Answers.Add(AddressRecord(
            "host.example.", [192, 0, 2, 1], 300));
        message.Authorities.Add(NameRecord(
            "example.", DnsRecordType.Ns, "ns.example."));
        message.Additionals.Add(OpaqueRecord(
            "ns.example.", 65000, [9, 8, 7]));

        DnsEncodeResult encoded = DnsMessageEncoder.EncodeMessage(
            message, new DnsEncodeOptions());
        int answerOffset = DnsProtocolLimits.HeaderLength +
            DnsQuestionCodec.EncodeQuestion(
                message.Questions[0], new DnsEncodeOptions()).Bytes.Length;
        DnsDecodeResult decoded = DnsMessageDecoder.DecodeMessage(
            encoded.Bytes, new DnsDecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.BytesWritten, Is.EqualTo(encoded.Bytes.Length));
            Assert.That(encoded.Bytes[answerOffset] & 192, Is.EqualTo(192));
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.Message!.Header.Identifier, Is.EqualTo(4660));
            Assert.That(decoded.Message.Header.QuestionCount, Is.EqualTo(1));
            Assert.That(decoded.Message.Header.AnswerCount, Is.EqualTo(1));
            Assert.That(decoded.Message.Header.AuthorityCount, Is.EqualTo(1));
            Assert.That(decoded.Message.Header.AdditionalCount, Is.EqualTo(1));
            Assert.That(decoded.Message.Answers[0].Data!.Address!.Address,
                Is.EqualTo(new byte[] { 192, 0, 2, 1 }));
            Assert.That(Format(decoded.Message.Authorities[0].Data!.Name!.Name),
                Is.EqualTo("ns.example."));
            Assert.That(decoded.Message.Additionals[0].Data!.Opaque!.Bytes,
                Is.EqualTo(new byte[] { 9, 8, 7 }));
        });
    }

    [Test]
    public void DerivedCountsReflectEncodedCollectionsWithoutMutatingSource()
    {
        DnsMessage message = new DnsMessage();
        message.Header.QuestionCount = 99;
        message.Header.AnswerCount = 98;
        message.Questions.Add(Question("example."));
        message.Answers.Add(AddressRecord(
            "example.", [192, 0, 2, 1], 60));

        DnsEncodeResult encoded = DnsMessageEncoder.EncodeMessage(
            message, new DnsEncodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(DnsHeaderCodec.ReadUnsigned16(encoded.Bytes, 4),
                Is.EqualTo(1));
            Assert.That(DnsHeaderCodec.ReadUnsigned16(encoded.Bytes, 6),
                Is.EqualTo(1));
            Assert.That(encoded.Message!.Header.QuestionCount, Is.EqualTo(1));
            Assert.That(encoded.Message.Header.AnswerCount, Is.EqualTo(1));
            Assert.That(message.Header.QuestionCount, Is.EqualTo(99));
            Assert.That(message.Header.AnswerCount, Is.EqualTo(98));
        });
    }

    [Test]
    public void ExplicitCountsMustMatchEncodedCollections()
    {
        DnsMessage message = new DnsMessage();
        message.Header.AnswerCount = 1;
        DnsEncodeOptions options = new DnsEncodeOptions();
        options.DeriveSectionCounts = false;

        DnsEncodeResult result = DnsMessageEncoder.EncodeMessage(
            message, options);

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
    public void ExactMaximumSucceedsAndOneByteLessFailsWithEntryMetadata()
    {
        DnsMessage message = new DnsMessage();
        message.Questions.Add(Question("."));
        DnsEncodeResult measured = DnsMessageEncoder.EncodeMessage(
            message, new DnsEncodeOptions());
        DnsEncodeOptions exactOptions = new DnsEncodeOptions();
        exactOptions.MaximumMessageLength = measured.BytesWritten;
        DnsEncodeOptions limitedOptions = new DnsEncodeOptions();
        limitedOptions.MaximumMessageLength = measured.BytesWritten - 1;

        DnsEncodeResult exact = DnsMessageEncoder.EncodeMessage(
            message, exactOptions);
        DnsEncodeResult limited = DnsMessageEncoder.EncodeMessage(
            message, limitedOptions);

        Assert.Multiple(() =>
        {
            Assert.That(exact.Success, Is.True);
            Assert.That(exact.Bytes.Length, Is.EqualTo(measured.BytesWritten));
            Assert.That(exact.BytesWritten, Is.EqualTo(exact.Bytes.Length));
            Assert.That(limited.Success, Is.False);
            Assert.That(limited.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.ResourceLimitExceeded));
            Assert.That(limited.Diagnostics[0].Section,
                Is.EqualTo(DnsMessageSection.Question));
            Assert.That(limited.Diagnostics[0].RecordIndex, Is.EqualTo(0));
        });
    }

    [Test]
    public void CanonicalPoliciesProduceDeterministicSortedNormalizedOutput()
    {
        DnsMessage first = CanonicalMessage(false);
        DnsMessage second = CanonicalMessage(true);
        DnsEncodeOptions options = new DnsEncodeOptions();
        options.Canonical = true;
        options.CanonicalOrder.NormalizeRecordTtl = true;

        DnsEncodeResult firstEncoded = DnsMessageEncoder.EncodeMessage(
            first, options);
        DnsEncodeResult secondEncoded = DnsMessageEncoder.EncodeMessage(
            second, options);
        DnsDecodeResult decoded = DnsMessageDecoder.DecodeMessage(
            firstEncoded.Bytes, new DnsDecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(firstEncoded.Success, Is.True);
            Assert.That(secondEncoded.Success, Is.True);
            Assert.That(firstEncoded.Bytes, Is.EqualTo(secondEncoded.Bytes));
            Assert.That(first.Answers, Has.Count.EqualTo(4));
            Assert.That(firstEncoded.Message!.Answers, Has.Count.EqualTo(3));
            Assert.That(decoded.Message!.Answers, Has.Count.EqualTo(3));
            Assert.That(Format(decoded.Message.Answers[0].Name),
                Is.EqualTo("a.example."));
            Assert.That(decoded.Message.Answers[0].RawData,
                Is.EqualTo(new byte[] { 1, 1, 1, 1 }));
            Assert.That(decoded.Message.Answers[1].RawData,
                Is.EqualTo(new byte[] { 3, 3, 3, 3 }));
            Assert.That(Format(decoded.Message.Answers[2].Name),
                Is.EqualTo("b.example."));
            Assert.That(decoded.Message.Answers[0].TimeToLive, Is.EqualTo(100));
            Assert.That(decoded.Message.Answers[1].TimeToLive, Is.EqualTo(100));
        });
    }

    [Test]
    public void DisabledCanonicalSortingPreservesSectionOrder()
    {
        DnsMessage message = new DnsMessage();
        message.Answers.Add(AddressRecord(
            "B.Example.", [2, 2, 2, 2], 60));
        message.Answers.Add(AddressRecord(
            "A.Example.", [1, 1, 1, 1], 60));
        DnsEncodeOptions options = new DnsEncodeOptions();
        options.Canonical = true;
        options.CanonicalOrder.SortRecords = false;
        options.CanonicalOrder.SuppressDuplicateRecords = false;

        DnsEncodeResult encoded = DnsMessageEncoder.EncodeMessage(
            message, options);
        DnsDecodeResult decoded = DnsMessageDecoder.DecodeMessage(
            encoded.Bytes, new DnsDecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(Format(decoded.Message!.Answers[0].Name),
                Is.EqualTo("b.example."));
            Assert.That(Format(decoded.Message.Answers[1].Name),
                Is.EqualTo("a.example."));
        });
    }

    [Test]
    public void CanonicalSortingKeepsTsigFinalAndRejectsInvalidPlacement()
    {
        DnsResourceRecord tsig = TsigRecord("A.Example.");
        DnsResourceRecord address = AddressRecord(
            "Z.Example.", [192, 0, 2, 1], 60);
        DnsMessage valid = new DnsMessage();
        valid.Additionals.Add(address);
        valid.Additionals.Add(tsig);
        DnsMessage invalid = new DnsMessage();
        invalid.Additionals.Add(tsig);
        invalid.Additionals.Add(address);
        DnsEncodeOptions options = new DnsEncodeOptions();
        options.Canonical = true;

        DnsEncodeResult accepted = DnsMessageEncoder.EncodeMessage(
            valid, options);
        DnsEncodeResult rejected = DnsMessageEncoder.EncodeMessage(
            invalid, options);
        DnsDecodeResult decoded = DnsMessageDecoder.DecodeMessage(
            accepted.Bytes, new DnsDecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(accepted.Success, Is.True);
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.Message!.Additionals[1].RecordTypeValue,
                Is.EqualTo(250));
            Assert.That(rejected.Success, Is.False);
            Assert.That(rejected.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.InvalidTsigPlacement));
            Assert.That(rejected.Diagnostics[0].RecordIndex, Is.EqualTo(0));
        });
    }

    [Test]
    public void OptMayPrecedeOnlyTheFinalTsigRecord()
    {
        DnsMessage valid = new DnsMessage();
        valid.Additionals.Add(AddressRecord(
            "host.example.", [192, 0, 2, 1], 60));
        valid.Additionals.Add(OptRecord());
        valid.Additionals.Add(TsigRecord("key.example."));
        DnsMessage invalid = new DnsMessage();
        invalid.Additionals.Add(TsigRecord("key.example."));
        invalid.Additionals.Add(OptRecord());

        DnsEncodeResult accepted = DnsMessageEncoder.EncodeMessage(
            valid, new DnsEncodeOptions());
        DnsEncodeResult rejected = DnsMessageEncoder.EncodeMessage(
            invalid, new DnsEncodeOptions());
        DnsDecodeResult decoded = DnsMessageDecoder.DecodeMessage(
            accepted.Bytes, new DnsDecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(accepted.Success, Is.True);
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.Message!.Additionals
                .Select(record => record.RecordTypeValue),
                Is.EqualTo(new[] { 1, 41, 250 }));
            Assert.That(rejected.Success, Is.False);
            Assert.That(rejected.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.InvalidTsigPlacement));
            Assert.That(rejected.Diagnostics[0].RecordIndex, Is.EqualTo(0));
        });
    }

    [Test]
    public void InvalidRecordFailuresIncludeSectionAndRecordIndex()
    {
        DnsMessage message = new DnsMessage();
        DnsResourceRecord queryOnly = OpaqueRecord(
            "example.", 255, [1]);
        message.Authorities.Add(AddressRecord(
            "example.", [192, 0, 2, 1], 60));
        message.Authorities.Add(queryOnly);

        DnsEncodeResult result = DnsMessageEncoder.EncodeMessage(
            message, new DnsEncodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.InvalidRecordData));
            Assert.That(result.Diagnostics[0].Section,
                Is.EqualTo(DnsMessageSection.Authority));
            Assert.That(result.Diagnostics[0].RecordIndex, Is.EqualTo(1));
        });
    }

    static DnsMessage CanonicalMessage(bool reverse)
    {
        DnsResourceRecord b = AddressRecord(
            "B.Example.", [2, 2, 2, 2], 200);
        DnsResourceRecord a3 = AddressRecord(
            "A.Example.", [3, 3, 3, 3], 200);
        DnsResourceRecord a1High = AddressRecord(
            "a.example.", [1, 1, 1, 1], 300);
        DnsResourceRecord a1Low = AddressRecord(
            "A.EXAMPLE.", [1, 1, 1, 1], 100);
        DnsMessage message = new DnsMessage();
        if (reverse)
        {
            message.Answers.Add(a1Low);
            message.Answers.Add(b);
            message.Answers.Add(a1High);
            message.Answers.Add(a3);
        }
        else
        {
            message.Answers.Add(b);
            message.Answers.Add(a3);
            message.Answers.Add(a1High);
            message.Answers.Add(a1Low);
        }

        return message;
    }

    static DnsQuestion Question(string name)
    {
        DnsQuestion question = new DnsQuestion();
        question.Name = Parse(name);
        question.RecordType = DnsRecordType.A;
        question.RecordTypeValue = 1;
        question.Class = DnsClass.Internet;
        question.ClassValue = 1;
        return question;
    }

    static DnsResourceRecord AddressRecord(
        string owner,
        byte[] address,
        uint timeToLive)
    {
        DnsRecordData data = new DnsRecordData();
        data.Address = new DnsAddressData();
        data.Address.Address = address;
        return Record(owner, DnsRecordType.A, 1, data, timeToLive);
    }

    static DnsResourceRecord NameRecord(
        string owner,
        DnsRecordType type,
        string target)
    {
        DnsRecordData data = new DnsRecordData();
        data.Name = new DnsNameData();
        data.Name.Name = Parse(target);
        return Record(owner, type, (int)type, data, 300);
    }

    static DnsResourceRecord OpaqueRecord(
        string owner,
        int recordType,
        byte[] bytes)
    {
        DnsRecordData data = new DnsRecordData();
        data.Opaque = new DnsOpaqueData();
        data.Opaque.Bytes = bytes;
        return Record(
            owner, (DnsRecordType)recordType, recordType, data, 60);
    }

    static DnsResourceRecord TsigRecord(string owner)
    {
        DnsRecordData data = new DnsRecordData();
        data.Tsig = new DnsTsigData();
        data.Tsig.AlgorithmName = Parse("HMAC-SHA256.");
        data.Tsig.TimeSigned = 123456;
        data.Tsig.Fudge = 300;
        data.Tsig.Mac = [1, 2, 3];
        data.Tsig.OriginalIdentifier = 4660;
        DnsResourceRecord result = Record(
            owner, DnsRecordType.Tsig, 250, data, 0);
        result.Class = DnsClass.Any;
        result.ClassValue = 255;
        return result;
    }

    static DnsResourceRecord OptRecord()
    {
        DnsRecordData data = new DnsRecordData();
        data.Edns = new DnsEdnsData();
        data.Edns.UdpPayloadSize = 1232;
        return Record(".", DnsRecordType.Opt, 41, data, 0);
    }

    static DnsResourceRecord Record(
        string owner,
        DnsRecordType type,
        int recordType,
        DnsRecordData data,
        uint timeToLive)
    {
        DnsResourceRecord record = new DnsResourceRecord();
        record.Name = Parse(owner);
        record.RecordType = type;
        record.RecordTypeValue = recordType;
        record.Class = DnsClass.Internet;
        record.ClassValue = 1;
        record.TimeToLive = timeToLive;
        record.Data = data;
        return record;
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
