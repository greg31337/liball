namespace dns.Tests.Internal.Messages;

public class MessageCoreExhaustiveTests
{
    static readonly int[] NonReservedFlags =
    [
        (int)DnsHeaderFlag.Response,
        (int)DnsHeaderFlag.AuthoritativeAnswer,
        (int)DnsHeaderFlag.Truncated,
        (int)DnsHeaderFlag.RecursionDesired,
        (int)DnsHeaderFlag.RecursionAvailable,
        (int)DnsHeaderFlag.AuthenticData,
        (int)DnsHeaderFlag.CheckingDisabled
    ];

    [Test]
    public void EveryHeaderCodeAndNonReservedFlagCombinationRoundTrips()
    {
        int flagCombination = 0;
        while (flagCombination < 128)
        {
            int flags = FlagCombination(flagCombination);
            int opcode = 0;
            while (opcode < 16)
            {
                int responseCode = 0;
                while (responseCode < 16)
                {
                    DnsMessage message = new DnsMessage();
                    message.Header.Identifier = 0xa55a;
                    message.Header.OpcodeValue = opcode;
                    message.Header.ResponseCodeValue = responseCode;
                    message.Header.Flags = (DnsHeaderFlag)flags;

                    DnsEncodeResult encoded = DnsHeaderCodec.EncodeHeader(
                        message, new DnsEncodeOptions());
                    DnsDecodeResult decoded = DnsHeaderCodec.DecodeHeader(
                        encoded.Bytes, new DnsDecodeOptions());

                    DnsMessageHeader header = decoded.Message!.Header;
                    if (!encoded.Success || !decoded.Success ||
                        header.Identifier != 0xa55a ||
                        header.OpcodeValue != opcode ||
                        header.ResponseCodeValue != responseCode ||
                        (int)header.Flags != flags)
                    {
                        Assert.Fail(
                            $"Header mismatch for flags 0x{flags:x4}, " +
                            $"opcode {opcode}, response code {responseCode}.");
                    }

                    responseCode = responseCode + 1;
                }

                opcode = opcode + 1;
            }

            flagCombination = flagCombination + 1;
        }
    }

    [Test]
    public void ReservedHeaderFlagIsEncodedButRequiresPermissiveDecoding()
    {
        DnsMessage message = new DnsMessage();
        message.Header.Flags = DnsHeaderFlag.Reserved |
            DnsHeaderFlag.Response;
        DnsEncodeResult encoded = DnsHeaderCodec.EncodeHeader(
            message, new DnsEncodeOptions());
        DnsDecodeOptions permissive = new DnsDecodeOptions();
        permissive.RejectReservedHeaderBits = false;

        DnsDecodeResult strict = DnsHeaderCodec.DecodeHeader(
            encoded.Bytes, new DnsDecodeOptions());
        DnsDecodeResult allowed = DnsHeaderCodec.DecodeHeader(
            encoded.Bytes, permissive);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Bytes[2] & 0x80, Is.EqualTo(0x80));
            Assert.That(encoded.Bytes[3] & 0x40, Is.EqualTo(0x40));
            Assert.That(strict.Success, Is.False);
            Assert.That(strict.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.ValueOutOfRange));
            Assert.That(strict.Diagnostics[0].Offset, Is.EqualTo(2));
            Assert.That(allowed.Success, Is.True);
            Assert.That(allowed.Message!.Header.Flags,
                Is.EqualTo(DnsHeaderFlag.Reserved | DnsHeaderFlag.Response));
        });
    }

    [Test]
    public void QuestionTypeAndClassNumericBoundariesRoundTrip()
    {
        int[] values = [0, 1, 255, 256, 32767, 32768, 65534, 65535];
        int typeIndex = 0;
        while (typeIndex < values.Length)
        {
            int classIndex = 0;
            while (classIndex < values.Length)
            {
                DnsQuestion question = new DnsQuestion();
                question.Name = Parse("boundary.example.");
                question.RecordTypeValue = values[typeIndex];
                question.ClassValue = values[classIndex];

                DnsEncodeResult encoded = DnsQuestionCodec.EncodeQuestion(
                    question, new DnsEncodeOptions());
                DnsDecodeResult decoded = DnsQuestionCodec.DecodeQuestion(
                    encoded.Bytes,
                    0,
                    new DnsDecodeOptions(),
                    true);

                Assert.Multiple(() =>
                {
                    Assert.That(encoded.Success, Is.True);
                    Assert.That(decoded.Success, Is.True);
                    Assert.That(decoded.Question!.RecordTypeValue,
                        Is.EqualTo(values[typeIndex]));
                    Assert.That(decoded.Question.ClassValue,
                        Is.EqualTo(values[classIndex]));
                    Assert.That((int)decoded.Question.RecordType,
                        Is.EqualTo(values[typeIndex]));
                    Assert.That((int)decoded.Question.Class,
                        Is.EqualTo(values[classIndex]));
                });

                classIndex = classIndex + 1;
            }

            typeIndex = typeIndex + 1;
        }
    }

    [Test]
    public void EverySectionCountBoundaryUsesItsOwnWireOffset()
    {
        int[] offsets = [4, 6, 8, 10];
        int section = 0;
        while (section < offsets.Length)
        {
            int[] accepted = [0, 0, 0, 0];
            accepted[section] = 65535;
            DnsEncodeResult acceptedResult = DnsHeaderCodec.ValidateEncodedCounts(
                accepted[0], accepted[1], accepted[2], accepted[3]);

            int[] negative = [0, 0, 0, 0];
            negative[section] = -1;
            DnsEncodeResult negativeResult = DnsHeaderCodec.ValidateEncodedCounts(
                negative[0], negative[1], negative[2], negative[3]);

            int[] overflow = [0, 0, 0, 0];
            overflow[section] = 65536;
            DnsEncodeResult overflowResult = DnsHeaderCodec.ValidateEncodedCounts(
                overflow[0], overflow[1], overflow[2], overflow[3]);

            byte[] header = new byte[DnsProtocolLimits.HeaderLength];
            header[offsets[section]] = 0;
            header[offsets[section] + 1] = 2;
            DnsDecodeOptions limited = new DnsDecodeOptions();
            limited.MaximumSectionEntryCount = 1;
            limited.MaximumTotalSectionEntryCount = 65535;
            DnsDecodeResult decoded = DnsHeaderCodec.DecodeHeader(
                header, limited);

            Assert.Multiple(() =>
            {
                Assert.That(acceptedResult.Success, Is.True);
                Assert.That(negativeResult.Success, Is.False);
                Assert.That(negativeResult.Diagnostics[0].Offset,
                    Is.EqualTo(offsets[section]));
                Assert.That(overflowResult.Success, Is.False);
                Assert.That(overflowResult.Diagnostics[0].Offset,
                    Is.EqualTo(offsets[section]));
                Assert.That(decoded.Success, Is.False);
                Assert.That(decoded.Diagnostics[0].Code,
                    Is.EqualTo(DnsDiagnosticCode.ResourceLimitExceeded));
                Assert.That(decoded.Diagnostics[0].Offset,
                    Is.EqualTo(offsets[section]));
            });

            section = section + 1;
        }
    }

    [Test]
    public void CloneMessageDeepCopiesEverySectionAndHeader()
    {
        DnsMessage source = CompleteMessage();

        DnsMessage clone = MessageOperations.CloneMessage(source);
        source.Header.Identifier = 99;
        source.Questions[0].Name.Labels[0][0] = 0x78;
        source.Answers[0].RawData[0] = 99;
        source.Authorities[0].Name.Labels[0][0] = 0x78;
        source.Additionals[0].RawData[0] = 99;

        Assert.Multiple(() =>
        {
            Assert.That(clone, Is.Not.SameAs(source));
            Assert.That(clone.Header, Is.Not.SameAs(source.Header));
            Assert.That(clone.Header.Identifier, Is.EqualTo(0x1234));
            Assert.That(Format(clone.Questions[0].Name),
                Is.EqualTo("question.example."));
            Assert.That(clone.Answers[0].RawData, Is.EqualTo(new byte[] { 1 }));
            Assert.That(Format(clone.Authorities[0].Name),
                Is.EqualTo("authority.example."));
            Assert.That(clone.Additionals[0].RawData,
                Is.EqualTo(new byte[] { 3 }));
            Assert.That(clone.Answers, Is.Not.SameAs(source.Answers));
            Assert.That(clone.Authorities, Is.Not.SameAs(source.Authorities));
            Assert.That(clone.Additionals, Is.Not.SameAs(source.Additionals));
        });
    }

    [Test]
    public void NormalizedComparisonAppliesIndependentlyToEveryRecordSection()
    {
        DnsMessage left = ComparableMessage(false);
        DnsMessage right = ComparableMessage(true);
        DnsCanonicalOrderOptions normalized = new DnsCanonicalOrderOptions();
        normalized.NormalizeRecordTtl = true;

        bool equal = MessageOperations.MessagesEqual(left, right, normalized);
        right.Authorities[0].RawData[0] = 99;
        bool unequalAuthority = MessageOperations.MessagesEqual(
            left, right, normalized);
        right.Authorities[0].RawData[0] = 2;
        DnsQuestion firstQuestion = right.Questions[0];
        right.Questions[0] = right.Questions[1];
        right.Questions[1] = firstQuestion;
        bool unequalQuestionOrder = MessageOperations.MessagesEqual(
            left, right, normalized);

        Assert.Multiple(() =>
        {
            Assert.That(equal, Is.True);
            Assert.That(unequalAuthority, Is.False);
            Assert.That(unequalQuestionOrder, Is.False);
        });
    }

    [TestCase(true)]
    [TestCase(false)]
    public void EstimatedSizeMatchesEncodingAcrossAllSections(bool compression)
    {
        DnsMessage message = CompleteMessage();
        DnsEncodeOptions options = new DnsEncodeOptions();
        options.EnableCompression = compression;

        int estimated = MessageOperations.EstimateMessageSize(message, options);
        DnsEncodeResult encoded = DnsMessageEncoder.EncodeMessage(
            message, options);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(estimated, Is.EqualTo(encoded.BytesWritten));
            Assert.That(estimated, Is.EqualTo(encoded.Bytes.Length));
        });
    }

    [Test]
    public void EveryEffectiveResponseCodeRoundTripsAndPreservesOtherFlagBits()
    {
        int responseCode = 0;
        while (responseCode <= 4095)
        {
            DnsMessage message = new DnsMessage();
            message.Header.FlagsValue = 0xabc0;

            MessageOperations.WriteEffectiveResponseCode(message, responseCode);
            int actual = MessageOperations.ReadEffectiveResponseCode(message);

            if (actual != responseCode ||
                message.Header.ResponseCodeValue != (responseCode & 15) ||
                (message.Header.FlagsValue & ~15) != 0xabc0 ||
                (message.Header.FlagsValue & 15) != (responseCode & 15) ||
                (responseCode < 16 && message.Additionals.Count != 0) ||
                (responseCode >= 16 && message.Additionals.Count != 1))
            {
                Assert.Fail($"Effective response code mismatch for {responseCode}.");
            }

            if (responseCode >= 16)
            {
                DnsResourceRecord opt = message.Additionals[0];
                int extended = responseCode >> 4;
                if (opt.Data!.Edns!.ExtendedResponseCode != extended ||
                    (int)(opt.TimeToLive >> 24) != extended)
                {
                    Assert.Fail(
                        $"Extended response code mismatch for {responseCode}.");
                }
            }

            responseCode = responseCode + 1;
        }
    }

    static int FlagCombination(int combination)
    {
        int flags = 0;
        int index = 0;
        while (index < NonReservedFlags.Length)
        {
            if ((combination & (1 << index)) != 0)
            {
                flags = flags | NonReservedFlags[index];
            }

            index = index + 1;
        }

        return flags;
    }

    static DnsMessage CompleteMessage()
    {
        DnsMessage message = new DnsMessage();
        message.Header.Identifier = 0x1234;
        message.Questions.Add(Question("question.example.", 1));
        message.Answers.Add(Record("answer.example.", 65000, [1], 10));
        message.Authorities.Add(Record("authority.example.", 65001, [2], 20));
        message.Additionals.Add(Record("additional.example.", 65002, [3], 30));
        return message;
    }

    static DnsMessage ComparableMessage(bool reverseRecords)
    {
        DnsMessage message = new DnsMessage();
        message.Header.Identifier = 7;
        message.Header.Flags = DnsHeaderFlag.Response;
        message.Header.AnswerCount = reverseRecords ? 1 : 99;
        message.Questions.Add(Question("first.example.", 65000));
        message.Questions.Add(Question("second.example.", 65001));
        AddComparableRecords(message.Answers, "answer.example.", 65010,
            reverseRecords);
        AddComparableRecords(message.Authorities, "authority.example.", 65011,
            reverseRecords);
        AddComparableRecords(message.Additionals, "additional.example.", 65012,
            reverseRecords);
        return message;
    }

    static void AddComparableRecords(
        List<DnsResourceRecord> records,
        string name,
        int type,
        bool reverse)
    {
        if (reverse)
        {
            records.Add(Record(name, type, [2], 100));
            records.Add(Record(name, type, [1], 200));
            return;
        }

        records.Add(Record(name, type, [1], 100));
        records.Add(Record(name, type, [2], 200));
        records.Add(Record(name, type, [1], 100));
    }

    static DnsQuestion Question(string name, int type)
    {
        DnsQuestion question = new DnsQuestion();
        question.Name = Parse(name);
        question.RecordType = (DnsRecordType)type;
        question.RecordTypeValue = type;
        question.Class = DnsClass.Internet;
        question.ClassValue = 1;
        return question;
    }

    static DnsResourceRecord Record(
        string name,
        int type,
        byte[] data,
        uint ttl)
    {
        DnsResourceRecord record = new DnsResourceRecord();
        record.Name = Parse(name);
        record.RecordType = (DnsRecordType)type;
        record.RecordTypeValue = type;
        record.Class = DnsClass.Internet;
        record.ClassValue = 1;
        record.TimeToLive = ttl;
        record.RawData = data;
        record.UseRawData = true;
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
