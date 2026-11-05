namespace dns.Tests.Internal.Records;

public class SharedRecordDataCodecTests
{
    [Test]
    public void OpaqueAndFixedByteCodecsCopyBytesAndEnforceWidths()
    {
        byte[] source = [9, 1, 2, 3, 8];
        DnsDecodeResult opaque = SharedRecordDataCodec.DecodeOpaqueData(
            source, 1, 3);
        DnsDecodeResult fixedBytes = SharedRecordDataCodec.DecodeFixedBytesData(
            source, 1, 3, 3);
        DnsDecodeResult wrongWidth = SharedRecordDataCodec.DecodeFixedBytesData(
            source, 1, 3, 4);
        source[1] = 99;

        WireWriter writer = Writer(8);
        WireWriteResult encoded = SharedRecordDataCodec.EncodeFixedBytesData(
            writer,
            fixedBytes.RecordData!.FixedBytes!,
            3);

        Assert.Multiple(() =>
        {
            Assert.That(opaque.Success, Is.True);
            Assert.That(opaque.RecordData!.Opaque!.Bytes,
                Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(fixedBytes.Success, Is.True);
            Assert.That(fixedBytes.RecordData!.FixedBytes!.Bytes,
                Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(wrongWidth.Success, Is.False);
            Assert.That(wrongWidth.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.InvalidLength));
            Assert.That(encoded.Success, Is.True);
            Assert.That(writer.CopyWrittenBytes(), Is.EqualTo(new byte[] { 1, 2, 3 }));
        });
    }

    [Test]
    public void AddressCodecSupportsOnlyExactIpv4AndIpv6Widths()
    {
        byte[] ipv4 = [192, 0, 2, 1];
        byte[] ipv6 = [32, 1, 13, 184, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1];

        DnsDecodeResult decoded4 = SharedRecordDataCodec.DecodeAddressData(
            ipv4, 0, ipv4.Length, 4);
        DnsDecodeResult decoded6 = SharedRecordDataCodec.DecodeAddressData(
            ipv6, 0, ipv6.Length, 16);
        DnsDecodeResult invalidSchema = SharedRecordDataCodec.DecodeAddressData(
            ipv4, 0, ipv4.Length, 8);
        WireWriter writer = Writer(16);
        WireWriteResult encoded = SharedRecordDataCodec.EncodeAddressData(
            writer,
            decoded6.RecordData!.Address!,
            16);

        Assert.Multiple(() =>
        {
            Assert.That(decoded4.RecordData!.Address!.Address, Is.EqualTo(ipv4));
            Assert.That(decoded6.RecordData!.Address!.Address, Is.EqualTo(ipv6));
            Assert.That(invalidSchema.Success, Is.False);
            Assert.That(encoded.Success, Is.True);
            Assert.That(writer.CopyWrittenBytes(), Is.EqualTo(ipv6));
        });
    }

    [Test]
    public void SingleNameDecoderAllowsBackwardCompressionWithinDeclaredLength()
    {
        byte[] message =
        [
            3, 119, 119, 119,
            7, 101, 120, 97, 109, 112, 108, 101,
            0,
            192, 0,
            99
        ];
        DnsDecodeOptions options = new DnsDecodeOptions();

        DnsDecodeResult decoded = SharedRecordDataCodec.DecodeNameData(
            message, 13, 2, options);
        DnsDecodeResult trailing = SharedRecordDataCodec.DecodeNameData(
            message, 13, 3, options);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True);
            Assert.That(Format(decoded.RecordData!.Name!.Name),
                Is.EqualTo("www.example."));
            Assert.That(decoded.BytesConsumed, Is.EqualTo(2));
            Assert.That(trailing.Success, Is.False);
            Assert.That(trailing.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.RecordDataLengthMismatch));
        });
    }

    [Test]
    public void TwoNameCodecRoundTripsAndCompressesTheSecondSuffix()
    {
        DnsTwoNameData data = new DnsTwoNameData();
        data.FirstName = Parse("a.example.");
        data.SecondName = Parse("b.example.");
        WireWriter writer = Writer(64);
        CompressionTable table = new CompressionTable(0);
        DnsEncodeOptions options = new DnsEncodeOptions();

        WireWriteResult encoded = SharedRecordDataCodec.EncodeTwoNameData(
            writer, table, data, options, true);
        byte[] bytes = writer.CopyWrittenBytes();
        DnsDecodeResult decoded = SharedRecordDataCodec.DecodeTwoNameData(
            bytes, 0, bytes.Length, new DnsDecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(bytes.Length, Is.EqualTo(15));
            Assert.That(bytes[13] & 192, Is.EqualTo(192));
            Assert.That(Format(decoded.RecordData!.TwoName!.FirstName),
                Is.EqualTo("a.example."));
            Assert.That(Format(decoded.RecordData!.TwoName!.SecondName),
                Is.EqualTo("b.example."));
        });
    }

    [Test]
    public void PreferenceNameCodecRoundTripsUnsignedPreference()
    {
        DnsPreferenceNameData data = new DnsPreferenceNameData();
        data.Preference = 65535;
        data.Name = Parse("mail.example.");
        WireWriter writer = Writer(64);

        WireWriteResult encoded = SharedRecordDataCodec.EncodePreferenceNameData(
            writer,
            new CompressionTable(0),
            data,
            new DnsEncodeOptions(),
            false);
        byte[] bytes = writer.CopyWrittenBytes();
        DnsDecodeResult decoded = SharedRecordDataCodec.DecodePreferenceNameData(
            bytes, 0, bytes.Length, new DnsDecodeOptions());
        data.Preference = 65536;
        WireWriteResult invalid = SharedRecordDataCodec.EncodePreferenceNameData(
            Writer(64),
            new CompressionTable(0),
            data,
            new DnsEncodeOptions(),
            false);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(decoded.RecordData!.PreferenceName!.Preference,
                Is.EqualTo(65535));
            Assert.That(Format(decoded.RecordData!.PreferenceName!.Name),
                Is.EqualTo("mail.example."));
            Assert.That(invalid.Success, Is.False);
            Assert.That(invalid.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.ValueOutOfRange));
        });
    }

    [Test]
    public void CharacterStringCodecPreservesEmptyAndMaximumLengthStrings()
    {
        DnsCharacterStringsData data = new DnsCharacterStringsData();
        data.Strings.Add([]);
        byte[] maximum = new byte[255];
        maximum[254] = 7;
        data.Strings.Add(maximum);
        WireWriter writer = Writer(300);

        WireWriteResult encoded = SharedRecordDataCodec.EncodeCharacterStringsData(
            writer, data);
        byte[] bytes = writer.CopyWrittenBytes();
        DnsDecodeResult decoded = SharedRecordDataCodec.DecodeCharacterStringsData(
            bytes, 0, bytes.Length);
        DnsDecodeResult truncated = SharedRecordDataCodec.DecodeCharacterStringsData(
            new byte[] { 3, 1, 2 }, 0, 3);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(bytes.Length, Is.EqualTo(257));
            Assert.That(decoded.RecordData!.CharacterStrings!.Strings, Has.Count.EqualTo(2));
            Assert.That(decoded.RecordData!.CharacterStrings!.Strings[0], Is.Empty);
            Assert.That(decoded.RecordData!.CharacterStrings!.Strings[1][254], Is.EqualTo(7));
            Assert.That(truncated.Success, Is.False);
            Assert.That(truncated.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.UnexpectedEnd));
        });
    }

    [Test]
    public void IntegerNameHelpersRoundTripEachSupportedIntegerWidth()
    {
        DnsName name = Parse("target.example.");
        DnsEncodeOptions encodeOptions = new DnsEncodeOptions();
        DnsDecodeOptions decodeOptions = new DnsDecodeOptions();
        WireWriter writer8 = Writer(64);
        WireWriter writer16 = Writer(64);
        WireWriter writer32 = Writer(64);
        WireWriteResult encoded8 = SharedRecordDataCodec.EncodeUnsigned8Name(
            writer8, new CompressionTable(0), 255, name, encodeOptions, false);
        WireWriteResult encoded16 = SharedRecordDataCodec.EncodeUnsigned16Name(
            writer16, new CompressionTable(0), 65535, name, encodeOptions, false);
        WireWriteResult encoded32 = SharedRecordDataCodec.EncodeUnsigned32Name(
            writer32, new CompressionTable(0), 4294967295, name, encodeOptions, false);

        IntegerNameReadResult decoded8 = SharedRecordDataCodec.DecodeUnsigned8Name(
            writer8.CopyWrittenBytes(), 0, writer8.WrittenLength(), decodeOptions);
        IntegerNameReadResult decoded16 = SharedRecordDataCodec.DecodeUnsigned16Name(
            writer16.CopyWrittenBytes(), 0, writer16.WrittenLength(), decodeOptions);
        IntegerNameReadResult decoded32 = SharedRecordDataCodec.DecodeUnsigned32Name(
            writer32.CopyWrittenBytes(), 0, writer32.WrittenLength(), decodeOptions);

        Assert.Multiple(() =>
        {
            Assert.That(encoded8.Success, Is.True);
            Assert.That(encoded16.Success, Is.True);
            Assert.That(encoded32.Success, Is.True);
            Assert.That(decoded8.IntegerValue, Is.EqualTo(255));
            Assert.That(decoded16.IntegerValue, Is.EqualTo(65535));
            Assert.That(decoded32.IntegerValue, Is.EqualTo(4294967295));
            Assert.That(Format(decoded32.Name!), Is.EqualTo("target.example."));
        });
    }

    [Test]
    public void SharedDecodersRejectRegionsOutsideTheContainingMessage()
    {
        DnsDecodeResult negative = SharedRecordDataCodec.DecodeOpaqueData(
            new byte[] { 1 }, -1, 1);
        DnsDecodeResult overrun = SharedRecordDataCodec.DecodeCharacterStringsData(
            new byte[] { 0 }, 1, 1);

        Assert.Multiple(() =>
        {
            Assert.That(negative.Success, Is.False);
            Assert.That(overrun.Success, Is.False);
            Assert.That(negative.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.InvalidLength));
            Assert.That(overrun.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.InvalidLength));
        });
    }

    static WireWriter Writer(int length)
    {
        return new WireWriter(new byte[length], 0, length);
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
