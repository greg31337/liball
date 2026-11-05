namespace dns.Tests.Internal.Messages;

public class MessageOperationsTests
{
    [Test]
    public void CloneMessageCopiesNestedNamesRecordDataAndEdnsOptions()
    {
        DnsMessage source = new DnsMessage();
        source.Header.Identifier = 42;
        source.Questions.Add(Question("www.example."));
        DnsResourceRecord alias = Record(
            "www.example.",
            DnsRecordType.Cname,
            [3, 102, 111, 111, 0]);
        alias.Data = new DnsRecordData();
        alias.Data.Name = new DnsNameData();
        alias.Data.Name.Name = Parse("foo.example.");
        source.Answers.Add(alias);
        DnsResourceRecord opt = OptRecord(1);
        DnsEdnsOption option = new DnsEdnsOption();
        option.Code = DnsEdnsOptionCode.Cookie;
        option.CodeValue = 10;
        option.Cookie = new DnsEdnsCookie();
        option.Cookie.ClientCookie = [1, 2, 3, 4, 5, 6, 7, 8];
        opt.Data!.Edns!.Options.Add(option);
        source.Additionals.Add(opt);

        DnsMessage clone = MessageOperations.CloneMessage(source);
        source.Questions[0].Name.Labels[0][0] = (byte)'x';
        source.Answers[0].RawData[0] = 99;
        source.Answers[0].Data!.Name!.Name.Labels[0][0] = (byte)'x';
        source.Additionals[0].Data!.Edns!.Options[0].Cookie!.ClientCookie[0] = 99;

        Assert.Multiple(() =>
        {
            Assert.That(clone, Is.Not.SameAs(source));
            Assert.That(clone.Header.Identifier, Is.EqualTo(42));
            Assert.That(Format(clone.Questions[0].Name), Is.EqualTo("www.example."));
            Assert.That(clone.Answers[0].RawData[0], Is.EqualTo(3));
            Assert.That(
                Format(clone.Answers[0].Data!.Name!.Name),
                Is.EqualTo("foo.example."));
            Assert.That(
                clone.Additionals[0].Data!.Edns!.Options[0].Cookie!.ClientCookie[0],
                Is.EqualTo(1));
        });
    }

    [Test]
    public void MessagesEqualNormalizesNamesRecordOrderDuplicatesAndTtl()
    {
        DnsMessage left = ComparableMessage("WwW.Example.");
        left.Answers.Add(Record("www.example.", DnsRecordType.A, [2], 200));
        left.Answers.Add(Record("www.example.", DnsRecordType.A, [1], 100));
        left.Answers.Add(Record("www.example.", DnsRecordType.A, [1], 100));
        left.Header.AnswerCount = 99;

        DnsMessage right = ComparableMessage("www.eXAMPLE.");
        right.Answers.Add(Record("WWW.EXAMPLE.", DnsRecordType.A, [1], 200));
        right.Answers.Add(Record("WWW.EXAMPLE.", DnsRecordType.A, [2], 100));
        DnsCanonicalOrderOptions normalized = new DnsCanonicalOrderOptions();
        normalized.NormalizeRecordTtl = true;
        bool equal = MessageOperations.MessagesEqual(left, right, normalized);

        normalized.NormalizeRecordTtl = false;
        bool unequalTtl = MessageOperations.MessagesEqual(left, right, normalized);
        normalized.NormalizeRecordTtl = true;
        right.Answers[0].RawData[0] = 3;
        bool unequalData = MessageOperations.MessagesEqual(left, right, normalized);

        Assert.Multiple(() =>
        {
            Assert.That(equal, Is.True);
            Assert.That(unequalTtl, Is.False);
            Assert.That(unequalData, Is.False);
        });
    }

    [Test]
    public void EffectiveResponseCodeCombinesAndSplitsEdnsBits()
    {
        DnsMessage message = new DnsMessage();
        message.Header.ResponseCode = DnsResponseCode.FormatError;
        message.Additionals.Add(OptRecord(2));

        int original = MessageOperations.ReadEffectiveResponseCode(message);
        MessageOperations.WriteEffectiveResponseCode(message, 23);
        int updated = MessageOperations.ReadEffectiveResponseCode(message);

        Assert.Multiple(() =>
        {
            Assert.That(original, Is.EqualTo(33));
            Assert.That(updated, Is.EqualTo(23));
            Assert.That(message.Header.ResponseCodeValue, Is.EqualTo(7));
            Assert.That(
                message.Additionals[0].Data!.Edns!.ExtendedResponseCode,
                Is.EqualTo(1));
        });
    }

    [Test]
    public void ExtendedResponseCodeCreatesOneOptRecordWhenNeeded()
    {
        DnsMessage message = new DnsMessage();

        MessageOperations.WriteEffectiveResponseCode(message, 16);

        Assert.Multiple(() =>
        {
            Assert.That(message.Additionals, Has.Count.EqualTo(1));
            Assert.That(message.Additionals[0].RecordTypeValue, Is.EqualTo(41));
            Assert.That(message.Additionals[0].Name.IsAbsolute, Is.True);
            Assert.That(message.Additionals[0].Name.Labels, Is.Empty);
            Assert.That(message.Additionals[0].ClassValue,
                Is.EqualTo(DnsProtocolLimits.DefaultEdnsUdpPayloadSize));
            Assert.That(MessageOperations.ReadEffectiveResponseCode(message), Is.EqualTo(16));
        });
    }

    [Test]
    public void ExtendedResponseCodePopulatesAnExistingOpaqueOptRecord()
    {
        DnsMessage message = new DnsMessage();
        DnsResourceRecord opt = new DnsResourceRecord();
        opt.Name = Parse(".");
        opt.RecordType = DnsRecordType.Opt;
        opt.ClassValue = 1400;
        message.Additionals.Add(opt);

        MessageOperations.WriteEffectiveResponseCode(message, 32);

        Assert.Multiple(() =>
        {
            Assert.That(message.Additionals, Has.Count.EqualTo(1));
            Assert.That(message.Additionals[0].Data!.Edns!.ExtendedResponseCode,
                Is.EqualTo(2));
            Assert.That(message.Additionals[0].Data!.Edns!.UdpPayloadSize,
                Is.EqualTo(1400));
            Assert.That(message.Additionals[0].TimeToLive, Is.EqualTo(33554432));
            Assert.That(MessageOperations.ReadEffectiveResponseCode(message), Is.EqualTo(32));
        });
    }

    [Test]
    public void EffectiveResponseCodeRejectsInvalidCodesAndMultipleOptRecords()
    {
        DnsMessage message = new DnsMessage();
        message.Additionals.Add(OptRecord(0));
        message.Additionals.Add(OptRecord(0));

        Assert.Multiple(() =>
        {
            Assert.That(
                () => MessageOperations.WriteEffectiveResponseCode(message, 4096),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(
                () => MessageOperations.ReadEffectiveResponseCode(message),
                Throws.TypeOf<ArgumentException>());
        });
    }

    [Test]
    public void EstimateMessageSizeAccountsForOwnerCompressionAndRawData()
    {
        DnsMessage message = new DnsMessage();
        message.Questions.Add(Question("www.example."));
        message.Answers.Add(Record(
            "www.example.",
            DnsRecordType.A,
            [192, 0, 2, 1],
            60));
        DnsEncodeOptions compressed = new DnsEncodeOptions();
        compressed.MaximumMessageLength = 20;
        DnsEncodeOptions uncompressed = new DnsEncodeOptions();
        uncompressed.EnableCompression = false;
        DnsEncodeOptions canonical = new DnsEncodeOptions();
        canonical.Canonical = true;

        int compressedSize = MessageOperations.EstimateMessageSize(message, compressed);
        int uncompressedSize = MessageOperations.EstimateMessageSize(message, uncompressed);
        int canonicalSize = MessageOperations.EstimateMessageSize(message, canonical);

        Assert.Multiple(() =>
        {
            Assert.That(compressedSize, Is.EqualTo(45));
            Assert.That(uncompressedSize, Is.EqualTo(56));
            Assert.That(canonicalSize, Is.EqualTo(56));
        });
    }

    static DnsMessage ComparableMessage(string questionName)
    {
        DnsMessage message = new DnsMessage();
        message.Header.Identifier = 12;
        message.Header.Flags = DnsHeaderFlag.Response;
        message.Header.ResponseCode = DnsResponseCode.NoError;
        message.Questions.Add(Question(questionName));
        return message;
    }

    static DnsQuestion Question(string name)
    {
        DnsQuestion question = new DnsQuestion();
        question.Name = Parse(name);
        question.RecordType = DnsRecordType.A;
        question.Class = DnsClass.Internet;
        return question;
    }

    static DnsResourceRecord Record(
        string name,
        DnsRecordType type,
        byte[] rawData)
    {
        return Record(name, type, rawData, 60);
    }

    static DnsResourceRecord Record(
        string name,
        DnsRecordType type,
        byte[] rawData,
        uint timeToLive)
    {
        DnsResourceRecord record = new DnsResourceRecord();
        record.Name = Parse(name);
        record.RecordType = type;
        record.Class = DnsClass.Internet;
        record.TimeToLive = timeToLive;
        record.RawData = rawData;
        return record;
    }

    static DnsResourceRecord OptRecord(int extendedCode)
    {
        DnsResourceRecord record = new DnsResourceRecord();
        record.Name = Parse(".");
        record.RecordType = DnsRecordType.Opt;
        record.Data = new DnsRecordData();
        record.Data.Edns = new DnsEdnsData();
        record.Data.Edns.ExtendedResponseCode = extendedCode;
        return record;
    }

    static DnsName Parse(string value)
    {
        DnsTextResult result = PresentationDnsName.ParseName(value, null);
        Assert.That(result.Success, Is.True);
        return result.Name!;
    }

    static string Format(DnsName name)
    {
        DnsTextResult result = PresentationDnsName.FormatName(name);
        Assert.That(result.Success, Is.True);
        return result.Text!;
    }
}
