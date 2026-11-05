namespace dns.Tests.Internal.Records;

public class LegacyRecordDataCodecTests
{
    [Test]
    public void RpRoundTripsMailboxAndTextNames()
    {
        DnsRecordData data = new DnsRecordData();
        data.TwoName = new DnsTwoNameData();
        data.TwoName.FirstName = Parse("hostmaster.example.");
        data.TwoName.SecondName = Parse("contact.example.");

        DnsDecodeResult decoded = Decode(17, Encode(17, data));

        Assert.Multiple(() =>
        {
            Assert.That(Format(decoded.RecordData!.TwoName!.FirstName),
                Is.EqualTo("hostmaster.example."));
            Assert.That(Format(decoded.RecordData!.TwoName!.SecondName),
                Is.EqualTo("contact.example."));
        });
    }

    [TestCase(18)]
    [TestCase(21)]
    [TestCase(36)]
    public void PreferenceNameFamiliesRoundTripFullUnsignedValues(int recordTypeValue)
    {
        DnsRecordData data = new DnsRecordData();
        data.PreferenceName = new DnsPreferenceNameData();
        data.PreferenceName.Preference = 65535;
        data.PreferenceName.Name = Parse("relay.example.");

        DnsDecodeResult decoded = Decode(
            recordTypeValue,
            Encode(recordTypeValue, data));

        Assert.Multiple(() =>
        {
            Assert.That(decoded.RecordData!.PreferenceName!.Preference,
                Is.EqualTo(65535));
            Assert.That(Format(decoded.RecordData!.PreferenceName!.Name),
                Is.EqualTo("relay.example."));
        });
    }

    [Test]
    public void X25RequiresExactlyOneCharacterString()
    {
        DnsRecordData data = Strings([51, 49, 49, 48, 54, 49, 55, 48, 48, 57, 53, 54]);
        byte[] bytes = Encode(19, data);
        DnsDecodeResult decoded = Decode(19, bytes);
        data.CharacterStrings!.Strings.Add([48]);
        WireWriteResult tooMany = EncodeResult(19, data);
        DnsDecodeResult empty = Decode(19, []);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.RecordData!.CharacterStrings!.Strings,
                Has.Count.EqualTo(1));
            Assert.That(tooMany.Success, Is.False);
            Assert.That(empty.Success, Is.False);
        });
    }

    [Test]
    public void IsdnAllowsAnOptionalSecondCharacterString()
    {
        DnsRecordData addressOnly = Strings([49, 53, 48, 56]);
        DnsRecordData withSubaddress = Strings(
            [49, 53, 48, 56],
            [48, 48, 52]);
        DnsRecordData tooMany = Strings([49], [50], [51]);

        DnsDecodeResult one = Decode(20, Encode(20, addressOnly));
        DnsDecodeResult two = Decode(20, Encode(20, withSubaddress));
        WireWriteResult three = EncodeResult(20, tooMany);

        Assert.Multiple(() =>
        {
            Assert.That(one.RecordData!.CharacterStrings!.Strings, Has.Count.EqualTo(1));
            Assert.That(two.RecordData!.CharacterStrings!.Strings, Has.Count.EqualTo(2));
            Assert.That(three.Success, Is.False);
        });
    }

    [Test]
    public void GposRequiresExactlyThreeCharacterStrings()
    {
        DnsRecordData data = Strings(
            [45, 51, 50, 46, 54],
            [49, 49, 54, 46, 56],
            [49, 48, 46, 48]);
        DnsDecodeResult decoded = Decode(27, Encode(27, data));
        DnsDecodeResult two = Decode(27, new byte[] { 1, 49, 1, 50 });

        Assert.Multiple(() =>
        {
            Assert.That(decoded.RecordData!.CharacterStrings!.Strings,
                Has.Count.EqualTo(3));
            Assert.That(two.Success, Is.False);
            Assert.That(two.Diagnostics[0].Field, Is.EqualTo("gpos"));
        });
    }

    [Test]
    public void NsapRoundTripsBinaryAddressesUpToTwentyOctets()
    {
        byte[] address =
        [
            57, 132, 15, 128, 0, 90, 0, 0, 0, 0,
            1, 225, 55, 8, 0, 32, 16, 114, 110, 0
        ];
        DnsRecordData data = new DnsRecordData();
        data.FixedBytes = new DnsFixedBytesData();
        data.FixedBytes.Bytes = address;

        byte[] encoded = Encode(22, data);
        DnsDecodeResult decoded = Decode(22, encoded);

        Assert.Multiple(() =>
        {
            Assert.That(encoded, Is.EqualTo(address));
            Assert.That(decoded.RecordData!.FixedBytes!.Bytes, Is.EqualTo(address));
        });
    }

    [Test]
    public void NsapRejectsEmptyAndOversizedAddresses()
    {
        DnsDecodeResult empty = Decode(22, []);
        DnsDecodeResult oversized = Decode(22, new byte[21]);
        DnsRecordData model = new DnsRecordData();
        model.FixedBytes = new DnsFixedBytesData();
        WireWriteResult emptyWrite = EncodeResult(22, model);

        Assert.Multiple(() =>
        {
            Assert.That(empty.Success, Is.False);
            Assert.That(oversized.Success, Is.False);
            Assert.That(emptyWrite.Success, Is.False);
            Assert.That(oversized.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.InvalidLength));
        });
    }

    [Test]
    public void NsapPtrUsesCompressedDomainNameSchema()
    {
        DnsRecordData data = new DnsRecordData();
        data.Name = new DnsNameData();
        data.Name.Name = Parse("host.example.");

        DnsDecodeResult decoded = Decode(23, Encode(23, data));

        Assert.That(Format(decoded.RecordData!.Name!.Name),
            Is.EqualTo("host.example."));
    }

    [Test]
    public void PxRoundTripsPreferenceAndTwoCompressedNames()
    {
        DnsRecordData data = new DnsRecordData();
        data.Px = new DnsPxData();
        data.Px.Preference = 65535;
        data.Px.Map822 = Parse("mail.example.");
        data.Px.MapX400 = Parse("x400.example.");

        byte[] bytes = Encode(26, data);
        DnsDecodeResult decoded = Decode(26, bytes);

        Assert.Multiple(() =>
        {
            Assert.That(bytes[21] & 192, Is.EqualTo(192));
            Assert.That(decoded.RecordData!.Px!.Preference, Is.EqualTo(65535));
            Assert.That(Format(decoded.RecordData!.Px!.Map822),
                Is.EqualTo("mail.example."));
            Assert.That(Format(decoded.RecordData!.Px!.MapX400),
                Is.EqualTo("x400.example."));
        });
    }

    [Test]
    public void PxRejectsOutOfRangePreferenceAndIncompleteNames()
    {
        DnsRecordData data = new DnsRecordData();
        data.Px = new DnsPxData();
        data.Px.Preference = 65536;
        data.Px.Map822 = Parse("mail.example.");
        data.Px.MapX400 = Parse("x400.example.");

        WireWriteResult invalidPreference = EncodeResult(26, data);
        DnsDecodeResult incomplete = Decode(26, new byte[] { 0, 1, 0 });

        Assert.Multiple(() =>
        {
            Assert.That(invalidPreference.Success, Is.False);
            Assert.That(invalidPreference.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.ValueOutOfRange));
            Assert.That(incomplete.Success, Is.False);
            Assert.That(incomplete.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.UnexpectedEnd));
        });
    }

    [Test]
    public void CodecRejectsUnsupportedTypesAndMissingSchemas()
    {
        DnsDecodeResult unsupported = Decode(24, [0]);
        WireWriteResult unsupportedWrite = EncodeResult(24, new DnsRecordData());
        WireWriteResult missingRp = EncodeResult(17, new DnsRecordData());

        Assert.Multiple(() =>
        {
            Assert.That(unsupported.Success, Is.False);
            Assert.That(unsupportedWrite.Success, Is.False);
            Assert.That(missingRp.Success, Is.False);
            Assert.That(missingRp.Diagnostics[0].Field, Is.EqualTo("twoNames"));
        });
    }

    static DnsRecordData Strings(byte[] first)
    {
        DnsRecordData data = new DnsRecordData();
        data.CharacterStrings = new DnsCharacterStringsData();
        data.CharacterStrings.Strings.Add(first);
        return data;
    }

    static DnsRecordData Strings(byte[] first, byte[] second)
    {
        DnsRecordData data = Strings(first);
        data.CharacterStrings!.Strings.Add(second);
        return data;
    }

    static DnsRecordData Strings(byte[] first, byte[] second, byte[] third)
    {
        DnsRecordData data = Strings(first, second);
        data.CharacterStrings!.Strings.Add(third);
        return data;
    }

    static DnsDecodeResult Decode(int recordTypeValue, byte[] bytes)
    {
        return LegacyRecordDataCodec.DecodeRecordData(
            recordTypeValue,
            bytes,
            0,
            bytes.Length,
            new DnsDecodeOptions());
    }

    static byte[] Encode(int recordTypeValue, DnsRecordData data)
    {
        WireWriter writer = new WireWriter(new byte[1024], 0, 1024);
        WireWriteResult result = LegacyRecordDataCodec.EncodeRecordData(
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
        return LegacyRecordDataCodec.EncodeRecordData(
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
