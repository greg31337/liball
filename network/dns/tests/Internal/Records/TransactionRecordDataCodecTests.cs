namespace dns.Tests.Internal.Records;

public class TransactionRecordDataCodecTests
{
    [Test]
    public void TkeyRoundTripsEveryWireFieldWithoutNameCompression()
    {
        DnsRecordData data = new DnsRecordData();
        data.Tkey = new DnsTkeyData();
        data.Tkey.AlgorithmName = Parse("gss-tsig.");
        data.Tkey.Inception = uint.MaxValue;
        data.Tkey.Expiration = 2147483648;
        data.Tkey.Mode = 65535;
        data.Tkey.Error = 65000;
        data.Tkey.Key = [0, 1, 2, 255];
        data.Tkey.OtherData = [9, 8, 7];

        byte[] encoded = Encode(249, data);
        DnsDecodeResult decoded = Decode(249, encoded);
        DnsTkeyData tkey = decoded.RecordData!.Tkey!;

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True);
            Assert.That(Format(tkey.AlgorithmName), Is.EqualTo("gss-tsig."));
            Assert.That(tkey.Inception, Is.EqualTo(uint.MaxValue));
            Assert.That(tkey.Expiration, Is.EqualTo(2147483648));
            Assert.That(tkey.Mode, Is.EqualTo(65535));
            Assert.That(tkey.Error, Is.EqualTo(65000));
            Assert.That(tkey.Key, Is.EqualTo(new byte[] { 0, 1, 2, 255 }));
            Assert.That(tkey.OtherData, Is.EqualTo(new byte[] { 9, 8, 7 }));
            Assert.That(Encode(249, decoded.RecordData), Is.EqualTo(encoded));
            Assert.That(encoded[0] & 192, Is.EqualTo(0));
        });
    }

    [Test]
    public void TsigRoundTripsUnsignedFortyEightBitTimeAndKnownError()
    {
        DnsRecordData data = BasicTsigData();
        data.Tsig!.TimeSigned = 281474976710655;
        data.Tsig.Fudge = 65535;
        data.Tsig.Mac = [0, 255, 1, 254];
        data.Tsig.OriginalIdentifier = 65535;
        data.Tsig.Error = DnsTsigError.BadTime;
        data.Tsig.OtherData = [0, 0, 0, 0, 0, 42];

        byte[] encoded = Encode(250, data);
        int timeOffset = NameWireLength(encoded);
        DnsDecodeResult decoded = Decode(250, encoded);
        DnsTsigData tsig = decoded.RecordData!.Tsig!;

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Skip(timeOffset).Take(6),
                Is.EqualTo(new byte[] { 255, 255, 255, 255, 255, 255 }));
            Assert.That(tsig.TimeSigned, Is.EqualTo(281474976710655));
            Assert.That(tsig.Fudge, Is.EqualTo(65535));
            Assert.That(tsig.Mac, Is.EqualTo(new byte[] { 0, 255, 1, 254 }));
            Assert.That(tsig.OriginalIdentifier, Is.EqualTo(65535));
            Assert.That(tsig.Error, Is.EqualTo(DnsTsigError.BadTime));
            Assert.That(tsig.ErrorValue, Is.EqualTo(18));
            Assert.That(tsig.OtherData,
                Is.EqualTo(new byte[] { 0, 0, 0, 0, 0, 42 }));
            Assert.That(Encode(250, decoded.RecordData), Is.EqualTo(encoded));
            Assert.That(encoded[0] & 192, Is.EqualTo(0));
        });
    }

    [Test]
    public void TsigPreservesUnknownErrorCodes()
    {
        DnsRecordData data = BasicTsigData();
        data.Tsig!.ErrorValue = 65000;

        DnsTsigData decoded = Decode(250, Encode(250, data)).RecordData!.Tsig!;

        Assert.Multiple(() =>
        {
            Assert.That(decoded.ErrorValue, Is.EqualTo(65000));
            Assert.That((int)decoded.Error, Is.EqualTo(65000));
        });
    }

    [TestCase(249, "tkey.algorithmName")]
    [TestCase(250, "tsig.algorithmName")]
    public void TransactionAlgorithmNamesRejectCompression(
        int recordTypeValue,
        string field)
    {
        byte[] message = new byte[32];
        message[0] = 1;
        message[1] = 97;
        message[2] = 0;
        message[3] = 192;
        message[4] = 0;
        DnsDecodeResult result = ExtendedRecordDataCodec.DecodeRecordData(
            recordTypeValue,
            message,
            3,
            recordTypeValue == 249 ? 18 : 18,
            new DnsDecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics[0].Field, Is.EqualTo(field));
        });
    }

    [Test]
    public void TkeyDecoderRejectsTruncatedKeyAndMismatchedOtherLength()
    {
        byte[] truncatedKey = Encode(249, BasicTkeyData());
        int nameLength = NameWireLength(truncatedKey);
        truncatedKey[nameLength + 12] = 255;
        truncatedKey[nameLength + 13] = 255;

        byte[] truncatedOther = Encode(249, BasicTkeyData());
        truncatedOther[truncatedOther.Length - 3] = 0;
        truncatedOther[truncatedOther.Length - 2] = 2;

        byte[] trailingOther = Encode(249, BasicTkeyData());
        trailingOther[trailingOther.Length - 2] = 0;
        trailingOther[trailingOther.Length - 1] = 0;

        Assert.Multiple(() =>
        {
            AssertDecodeField(Decode(249, truncatedKey), "tkey.key");
            AssertDecodeField(Decode(249, truncatedOther), "tkey.otherData");
            AssertDecodeField(Decode(249, trailingOther), "tkey.otherData");
        });
    }

    [Test]
    public void TsigDecoderRejectsTruncatedMacAndMismatchedOtherLength()
    {
        byte[] truncatedMac = Encode(250, BasicTsigData());
        int nameLength = NameWireLength(truncatedMac);
        truncatedMac[nameLength + 8] = 255;
        truncatedMac[nameLength + 9] = 255;

        byte[] truncatedOther = Encode(250, BasicTsigData());
        truncatedOther[truncatedOther.Length - 3] = 0;
        truncatedOther[truncatedOther.Length - 2] = 2;

        byte[] trailingOther = Encode(250, BasicTsigData());
        trailingOther[trailingOther.Length - 2] = 0;
        trailingOther[trailingOther.Length - 1] = 0;

        Assert.Multiple(() =>
        {
            AssertDecodeField(Decode(250, truncatedMac), "tsig.mac");
            AssertDecodeField(Decode(250, truncatedOther), "tsig.otherData");
            AssertDecodeField(Decode(250, trailingOther), "tsig.otherData");
        });
    }

    [Test]
    public void EncodersRejectValuesWiderThanTheirWireFields()
    {
        DnsRecordData badTkeyInteger = BasicTkeyData();
        badTkeyInteger.Tkey!.Mode = -1;
        DnsRecordData largeTkeyBytes = BasicTkeyData();
        largeTkeyBytes.Tkey!.Key = new byte[65536];
        DnsRecordData badTsigTime = BasicTsigData();
        badTsigTime.Tsig!.TimeSigned = 281474976710656;
        DnsRecordData badTsigInteger = BasicTsigData();
        badTsigInteger.Tsig!.Fudge = 65536;
        DnsRecordData largeTsigBytes = BasicTsigData();
        largeTsigBytes.Tsig!.OtherData = new byte[65536];

        Assert.Multiple(() =>
        {
            AssertWriteFails(EncodeResult(249, badTkeyInteger), "tkey");
            AssertWriteFails(EncodeResult(249, largeTkeyBytes), "tkey");
            AssertWriteFails(EncodeResult(250, badTsigTime), "tsig");
            AssertWriteFails(EncodeResult(250, badTsigInteger), "tsig");
            AssertWriteFails(EncodeResult(250, largeTsigBytes), "tsig");
        });
    }

    [Test]
    public void ExtendedCodecRequiresMatchingTransactionSchema()
    {
        WireWriteResult tkey = EncodeResult(249, new DnsRecordData());
        WireWriteResult tsig = EncodeResult(250, new DnsRecordData());

        Assert.Multiple(() =>
        {
            AssertWriteFails(tkey, "tkey");
            AssertWriteFails(tsig, "tsig");
        });
    }

    [Test]
    public void PlacementInspectionReturnsFinalAdditionalMetadata()
    {
        DnsMessage message = new DnsMessage();
        message.Additionals.Add(Record(DnsRecordType.Opt));
        DnsResourceRecord tsig = Record(DnsRecordType.Tsig);
        message.Additionals.Add(tsig);

        TsigPlacementResult result =
            TransactionRecordDataCodec.InspectTsigPlacement(message);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.HasTsig, Is.True);
            Assert.That(result.Record, Is.SameAs(tsig));
            Assert.That(result.Section, Is.EqualTo(DnsMessageSection.Additional));
            Assert.That(result.RecordIndex, Is.EqualTo(1));
            Assert.That(result.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void PlacementInspectionAllowsMessagesWithoutTsig()
    {
        DnsMessage message = new DnsMessage();
        message.Additionals.Add(Record(DnsRecordType.Opt));

        TsigPlacementResult result =
            TransactionRecordDataCodec.InspectTsigPlacement(message);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.HasTsig, Is.False);
            Assert.That(result.Record, Is.Null);
        });
    }

    [Test]
    public void PlacementInspectionRejectsTsigInAnswerWithMetadata()
    {
        DnsMessage message = new DnsMessage();
        message.Answers.Add(Record(DnsRecordType.A));
        message.Answers.Add(Record(DnsRecordType.Tsig));

        TsigPlacementResult result =
            TransactionRecordDataCodec.InspectTsigPlacement(message);

        AssertPlacementFailure(result, DnsMessageSection.Answer, 1);
    }

    [Test]
    public void PlacementInspectionRejectsNonFinalAndMultipleTsigRecords()
    {
        DnsMessage nonFinal = new DnsMessage();
        nonFinal.Additionals.Add(Record(DnsRecordType.Tsig));
        nonFinal.Additionals.Add(Record(DnsRecordType.Opt));
        DnsMessage multiple = new DnsMessage();
        multiple.Additionals.Add(Record(DnsRecordType.Tsig));
        multiple.Additionals.Add(Record(DnsRecordType.Tsig));

        TsigPlacementResult first =
            TransactionRecordDataCodec.InspectTsigPlacement(nonFinal);
        TsigPlacementResult second =
            TransactionRecordDataCodec.InspectTsigPlacement(multiple);

        Assert.Multiple(() =>
        {
            AssertPlacementFailure(first, DnsMessageSection.Additional, 0);
            AssertPlacementFailure(second, DnsMessageSection.Additional, 0);
        });
    }

    static DnsRecordData BasicTkeyData()
    {
        DnsRecordData data = new DnsRecordData();
        data.Tkey = new DnsTkeyData();
        data.Tkey.AlgorithmName = Parse("gss-tsig.");
        data.Tkey.Inception = 1;
        data.Tkey.Expiration = 2;
        data.Tkey.Mode = 3;
        data.Tkey.Error = 0;
        data.Tkey.Key = [4, 5];
        data.Tkey.OtherData = [6];
        return data;
    }

    static DnsRecordData BasicTsigData()
    {
        DnsRecordData data = new DnsRecordData();
        data.Tsig = new DnsTsigData();
        data.Tsig.AlgorithmName = Parse("hmac-sha256.");
        data.Tsig.TimeSigned = 123456;
        data.Tsig.Fudge = 300;
        data.Tsig.Mac = [1, 2, 3];
        data.Tsig.OriginalIdentifier = 4660;
        data.Tsig.OtherData = [4];
        return data;
    }

    static DnsResourceRecord Record(DnsRecordType type)
    {
        DnsResourceRecord record = new DnsResourceRecord();
        record.RecordType = type;
        record.RecordTypeValue = (int)type;
        return record;
    }

    static int NameWireLength(byte[] bytes)
    {
        int cursor = 0;
        while (bytes[cursor] != 0)
        {
            cursor = cursor + bytes[cursor] + 1;
        }

        return cursor + 1;
    }

    static DnsDecodeResult Decode(int recordTypeValue, byte[] bytes)
    {
        return ExtendedRecordDataCodec.DecodeRecordData(
            recordTypeValue, bytes, 0, bytes.Length, new DnsDecodeOptions());
    }

    static byte[] Encode(int recordTypeValue, DnsRecordData data)
    {
        WireWriter writer = new WireWriter(new byte[131072], 0, 131072);
        WireWriteResult result = ExtendedRecordDataCodec.EncodeRecordData(
            writer,
            new CompressionTable(0),
            recordTypeValue,
            data,
            new DnsEncodeOptions());
        Assert.That(result.Success, Is.True);
        return writer.CopyWrittenBytes();
    }

    static WireWriteResult EncodeResult(int recordTypeValue, DnsRecordData data)
    {
        WireWriter writer = new WireWriter(new byte[131072], 0, 131072);
        return ExtendedRecordDataCodec.EncodeRecordData(
            writer,
            new CompressionTable(0),
            recordTypeValue,
            data,
            new DnsEncodeOptions());
    }

    static void AssertDecodeField(DnsDecodeResult result, string field)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics[0].Code,
            Is.EqualTo(DnsDiagnosticCode.InvalidRecordData));
        Assert.That(result.Diagnostics[0].Field, Is.EqualTo(field));
    }

    static void AssertWriteFails(WireWriteResult result, string field)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics[0].Code,
            Is.EqualTo(DnsDiagnosticCode.InvalidRecordData));
        Assert.That(result.Diagnostics[0].Field, Is.EqualTo(field));
    }

    static void AssertPlacementFailure(
        TsigPlacementResult result,
        DnsMessageSection section,
        int index)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics[0].Code,
            Is.EqualTo(DnsDiagnosticCode.InvalidTsigPlacement));
        Assert.That(result.Diagnostics[0].HasSection, Is.True);
        Assert.That(result.Diagnostics[0].Section, Is.EqualTo(section));
        Assert.That(result.Diagnostics[0].HasRecordIndex, Is.True);
        Assert.That(result.Diagnostics[0].RecordIndex, Is.EqualTo(index));
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
