namespace dns.Tests.Internal.Records;

public class Rfc1035RecordDataCodecTests
{
    [Test]
    public void AddressRecordRoundTripsAndRequiresFourOctets()
    {
        DnsRecordData data = new DnsRecordData();
        data.Address = new DnsAddressData();
        data.Address.Address = [192, 0, 2, 7];

        byte[] bytes = Encode(1, data);
        DnsDecodeResult decoded = Decode(1, bytes);
        DnsDecodeResult shortAddress = Rfc1035RecordDataCodec.DecodeRecordData(
            1, new byte[] { 1, 2, 3 }, 0, 3, new DnsDecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(bytes, Is.EqualTo(new byte[] { 192, 0, 2, 7 }));
            Assert.That(decoded.RecordData!.Address!.Address, Is.EqualTo(bytes));
            Assert.That(shortAddress.Success, Is.False);
            Assert.That(shortAddress.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.InvalidLength));
        });
    }

    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    [TestCase(7)]
    [TestCase(8)]
    [TestCase(9)]
    [TestCase(12)]
    public void DomainNameRecordFamiliesUseTheSharedNameCodec(int recordTypeValue)
    {
        DnsRecordData data = new DnsRecordData();
        data.Name = new DnsNameData();
        data.Name.Name = Parse("target.example.");

        byte[] bytes = Encode(recordTypeValue, data);
        DnsDecodeResult decoded = Decode(recordTypeValue, bytes);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True);
            Assert.That(Format(decoded.RecordData!.Name!.Name),
                Is.EqualTo("target.example."));
            Assert.That(decoded.BytesConsumed, Is.EqualTo(bytes.Length));
        });
    }

    [Test]
    public void SoaRoundTripsNamesAndFiveUnsignedIntegersWithCompression()
    {
        DnsRecordData data = new DnsRecordData();
        data.Soa = new DnsSoaData();
        data.Soa.PrimaryNameServer = Parse("ns.example.");
        data.Soa.ResponsibleMailbox = Parse("hostmaster.example.");
        data.Soa.Serial = 4294967295;
        data.Soa.Refresh = 3600;
        data.Soa.Retry = 600;
        data.Soa.Expire = 86400;
        data.Soa.Minimum = 300;

        byte[] bytes = Encode(6, data);
        DnsDecodeResult decoded = Decode(6, bytes);

        Assert.Multiple(() =>
        {
            Assert.That(bytes.Length, Is.EqualTo(45));
            Assert.That(bytes[23] & 192, Is.EqualTo(192));
            Assert.That(Format(decoded.RecordData!.Soa!.PrimaryNameServer),
                Is.EqualTo("ns.example."));
            Assert.That(Format(decoded.RecordData!.Soa!.ResponsibleMailbox),
                Is.EqualTo("hostmaster.example."));
            Assert.That(decoded.RecordData!.Soa!.Serial, Is.EqualTo(4294967295));
            Assert.That(decoded.RecordData!.Soa!.Refresh, Is.EqualTo(3600));
            Assert.That(decoded.RecordData!.Soa!.Retry, Is.EqualTo(600));
            Assert.That(decoded.RecordData!.Soa!.Expire, Is.EqualTo(86400));
            Assert.That(decoded.RecordData!.Soa!.Minimum, Is.EqualTo(300));
        });
    }

    [Test]
    public void SoaRejectsTruncatedAndTrailingIntegerData()
    {
        DnsRecordData data = new DnsRecordData();
        data.Soa = new DnsSoaData();
        data.Soa.PrimaryNameServer = Parse(".");
        data.Soa.ResponsibleMailbox = Parse(".");
        byte[] valid = Encode(6, data);
        byte[] truncated = new byte[valid.Length - 1];
        Array.Copy(valid, truncated, truncated.Length);
        byte[] trailing = new byte[valid.Length + 1];
        Array.Copy(valid, trailing, valid.Length);

        DnsDecodeResult shortResult = Decode(6, truncated);
        DnsDecodeResult trailingResult = Decode(6, trailing);

        Assert.Multiple(() =>
        {
            Assert.That(shortResult.Success, Is.False);
            Assert.That(shortResult.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.UnexpectedEnd));
            Assert.That(trailingResult.Success, Is.False);
            Assert.That(trailingResult.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.RecordDataLengthMismatch));
        });
    }

    [Test]
    public void NullRecordPreservesArbitraryOpaqueBytes()
    {
        DnsRecordData data = new DnsRecordData();
        data.Opaque = new DnsOpaqueData();
        data.Opaque.Bytes = [0, 255, 1, 0];

        byte[] bytes = Encode(10, data);
        DnsDecodeResult decoded = Decode(10, bytes);

        Assert.That(decoded.RecordData!.Opaque!.Bytes,
            Is.EqualTo(new byte[] { 0, 255, 1, 0 }));
    }

    [Test]
    public void WksRoundTripsAddressProtocolAndPortBitmap()
    {
        DnsRecordData data = new DnsRecordData();
        data.Wks = new DnsWksData();
        data.Wks.Address = [203, 0, 113, 9];
        data.Wks.Protocol = 6;
        data.Wks.PortBitmap = [128, 0, 1];

        byte[] bytes = Encode(11, data);
        DnsDecodeResult decoded = Decode(11, bytes);

        Assert.Multiple(() =>
        {
            Assert.That(bytes,
                Is.EqualTo(new byte[] { 203, 0, 113, 9, 6, 128, 0, 1 }));
            Assert.That(decoded.RecordData!.Wks!.Address,
                Is.EqualTo(new byte[] { 203, 0, 113, 9 }));
            Assert.That(decoded.RecordData!.Wks!.Protocol, Is.EqualTo(6));
            Assert.That(decoded.RecordData!.Wks!.PortBitmap,
                Is.EqualTo(new byte[] { 128, 0, 1 }));
        });
    }

    [Test]
    public void WksRejectsShortInputAndOutOfRangeProtocol()
    {
        DnsDecodeResult shortResult = Decode(11, new byte[] { 1, 2, 3, 4 });
        DnsRecordData data = new DnsRecordData();
        data.Wks = new DnsWksData();
        data.Wks.Address = [1, 2, 3, 4];
        data.Wks.Protocol = 256;
        WireWriteResult invalidProtocol = EncodeResult(11, data);

        Assert.Multiple(() =>
        {
            Assert.That(shortResult.Success, Is.False);
            Assert.That(shortResult.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.UnexpectedEnd));
            Assert.That(invalidProtocol.Success, Is.False);
            Assert.That(invalidProtocol.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.ValueOutOfRange));
        });
    }

    [Test]
    public void HinfoRequiresExactlyCpuAndOperatingSystemStrings()
    {
        DnsRecordData data = StringData(
            new byte[] { 65, 82, 77, 54, 52 },
            new byte[] { 85, 78, 73, 88 });

        byte[] bytes = Encode(13, data);
        DnsDecodeResult decoded = Decode(13, bytes);
        DnsDecodeResult oneString = Decode(13, new byte[] { 1, 65 });
        data.CharacterStrings!.Strings.Add([88]);
        WireWriteResult threeStrings = EncodeResult(13, data);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.RecordData!.CharacterStrings!.Strings,
                Has.Count.EqualTo(2));
            Assert.That(oneString.Success, Is.False);
            Assert.That(oneString.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.InvalidRecordData));
            Assert.That(threeStrings.Success, Is.False);
        });
    }

    [Test]
    public void TxtRequiresAtLeastOneLengthPrefixedString()
    {
        DnsRecordData data = StringData([]);
        byte[] bytes = Encode(16, data);
        DnsDecodeResult decoded = Decode(16, bytes);
        DnsDecodeResult emptyRdata = Decode(16, []);
        DnsRecordData missing = new DnsRecordData();
        missing.CharacterStrings = new DnsCharacterStringsData();
        WireWriteResult emptyModel = EncodeResult(16, missing);

        Assert.Multiple(() =>
        {
            Assert.That(bytes, Is.EqualTo(new byte[] { 0 }));
            Assert.That(decoded.RecordData!.CharacterStrings!.Strings,
                Has.Count.EqualTo(1));
            Assert.That(emptyRdata.Success, Is.False);
            Assert.That(emptyModel.Success, Is.False);
        });
    }

    [Test]
    public void MinfoAndMxDispatchToTheirSharedSchemas()
    {
        DnsRecordData minfo = new DnsRecordData();
        minfo.TwoName = new DnsTwoNameData();
        minfo.TwoName.FirstName = Parse("requests.example.");
        minfo.TwoName.SecondName = Parse("errors.example.");
        DnsRecordData mx = new DnsRecordData();
        mx.PreferenceName = new DnsPreferenceNameData();
        mx.PreferenceName.Preference = 10;
        mx.PreferenceName.Name = Parse("mail.example.");

        DnsDecodeResult decodedMinfo = Decode(14, Encode(14, minfo));
        DnsDecodeResult decodedMx = Decode(15, Encode(15, mx));

        Assert.Multiple(() =>
        {
            Assert.That(Format(decodedMinfo.RecordData!.TwoName!.FirstName),
                Is.EqualTo("requests.example."));
            Assert.That(Format(decodedMinfo.RecordData!.TwoName!.SecondName),
                Is.EqualTo("errors.example."));
            Assert.That(decodedMx.RecordData!.PreferenceName!.Preference,
                Is.EqualTo(10));
            Assert.That(Format(decodedMx.RecordData!.PreferenceName!.Name),
                Is.EqualTo("mail.example."));
        });
    }

    [Test]
    public void CodecRejectsUnsupportedTypesAndMissingTypedSchemas()
    {
        DnsDecodeResult unsupported = Decode(28, new byte[16]);
        WireWriteResult unsupportedWrite = EncodeResult(28, new DnsRecordData());
        WireWriteResult missingAddress = EncodeResult(1, new DnsRecordData());

        Assert.Multiple(() =>
        {
            Assert.That(unsupported.Success, Is.False);
            Assert.That(unsupported.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.InvalidRecordData));
            Assert.That(unsupportedWrite.Success, Is.False);
            Assert.That(missingAddress.Success, Is.False);
            Assert.That(missingAddress.Diagnostics[0].Field, Is.EqualTo("address"));
        });
    }

    static DnsRecordData StringData(byte[] first)
    {
        DnsRecordData data = new DnsRecordData();
        data.CharacterStrings = new DnsCharacterStringsData();
        data.CharacterStrings.Strings.Add(first);
        return data;
    }

    static DnsRecordData StringData(byte[] first, byte[] second)
    {
        DnsRecordData data = StringData(first);
        data.CharacterStrings!.Strings.Add(second);
        return data;
    }

    static DnsDecodeResult Decode(int recordTypeValue, byte[] bytes)
    {
        return Rfc1035RecordDataCodec.DecodeRecordData(
            recordTypeValue,
            bytes,
            0,
            bytes.Length,
            new DnsDecodeOptions());
    }

    static byte[] Encode(int recordTypeValue, DnsRecordData data)
    {
        WireWriter writer = new WireWriter(new byte[1024], 0, 1024);
        WireWriteResult result = Rfc1035RecordDataCodec.EncodeRecordData(
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
        WireWriter writer = new WireWriter(new byte[1024], 0, 1024);
        return Rfc1035RecordDataCodec.EncodeRecordData(
            writer,
            new CompressionTable(0),
            recordTypeValue,
            data,
            new DnsEncodeOptions());
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
