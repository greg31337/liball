namespace dns.Tests.Internal.Records;

public class ExtendedRecordDataCodecTests
{
    [TestCase(104, 8)]
    [TestCase(105, 4)]
    [TestCase(106, 8)]
    public void IlNpFamiliesRoundTripExactNetworkOrderValues(
        int recordTypeValue,
        int valueLength)
    {
        DnsRecordData data = new DnsRecordData();
        data.IlNp = new DnsIlNpData();
        data.IlNp.Preference = 65535;
        data.IlNp.Value = Sequence(valueLength);

        byte[] encoded = Encode(recordTypeValue, data);
        DnsDecodeResult decoded = Decode(recordTypeValue, encoded);

        Assert.Multiple(() =>
        {
            Assert.That(encoded[0], Is.EqualTo(255));
            Assert.That(encoded[1], Is.EqualTo(255));
            Assert.That(decoded.RecordData!.IlNp!.Preference, Is.EqualTo(65535));
            Assert.That(decoded.RecordData!.IlNp!.Value,
                Is.EqualTo(Sequence(valueLength)));
        });
    }

    [Test]
    public void IlNpFamiliesRejectWrongWidthsAndPreferenceOverflow()
    {
        DnsDecodeResult shortNid = Decode(104, new byte[9]);
        DnsDecodeResult longL32 = Decode(105, new byte[7]);
        DnsRecordData data = new DnsRecordData();
        data.IlNp = new DnsIlNpData();
        data.IlNp.Preference = 65536;
        data.IlNp.Value = new byte[8];
        WireWriteResult preference = EncodeResult(106, data);

        Assert.Multiple(() =>
        {
            Assert.That(shortNid.Success, Is.False);
            Assert.That(longL32.Success, Is.False);
            Assert.That(preference.Success, Is.False);
        });
    }

    [Test]
    public void LpRoundTripsAnUncompressedTarget()
    {
        DnsRecordData data = new DnsRecordData();
        data.PreferenceName = new DnsPreferenceNameData();
        data.PreferenceName.Preference = 65535;
        data.PreferenceName.Name = Parse("locator.example.");

        byte[] encoded = Encode(107, data);
        DnsDecodeResult decoded = Decode(107, encoded);

        Assert.Multiple(() =>
        {
            Assert.That(encoded[2] & 192, Is.EqualTo(0));
            Assert.That(decoded.RecordData!.PreferenceName!.Preference,
                Is.EqualTo(65535));
            Assert.That(Format(decoded.RecordData!.PreferenceName!.Name),
                Is.EqualTo("locator.example."));
        });
    }

    [Test]
    public void LpRejectsCompressedTargetsAndInvalidPreferences()
    {
        byte[] compressed = [1, 97, 0, 0, 10, 192, 0];
        DnsDecodeResult compressedTarget =
            ExtendedRecordDataCodec.DecodeRecordData(
                107, compressed, 3, 4, new DnsDecodeOptions());
        DnsRecordData data = new DnsRecordData();
        data.PreferenceName = new DnsPreferenceNameData();
        data.PreferenceName.Preference = -1;
        data.PreferenceName.Name = Parse("locator.example.");
        WireWriteResult preference = EncodeResult(107, data);

        Assert.Multiple(() =>
        {
            Assert.That(compressedTarget.Success, Is.False);
            Assert.That(compressedTarget.Diagnostics[0].Field,
                Is.EqualTo("lp.target"));
            Assert.That(preference.Success, Is.False);
        });
    }

    [Test]
    public void AmtRelayRoundTripsAllDefinedRelayVariants()
    {
        DnsDecodeResult none = Decode(260, Encode(260, AmtAddressData(0, [])));
        DnsDecodeResult ipv4 = Decode(
            260, Encode(260, AmtAddressData(1, [192, 0, 2, 1])));
        DnsDecodeResult ipv6 = Decode(
            260, Encode(260, AmtAddressData(2, Sequence(16))));
        DnsRecordData nameData = AmtAddressData(3, []);
        nameData.AmtRelay!.DiscoveryOptional = true;
        nameData.AmtRelay.RelayName = Parse("relay.example.");
        byte[] nameBytes = Encode(260, nameData);
        DnsDecodeResult name = Decode(260, nameBytes);

        Assert.Multiple(() =>
        {
            Assert.That(none.RecordData!.AmtRelay!.RelayAddress, Is.Empty);
            Assert.That(ipv4.RecordData!.AmtRelay!.RelayAddress,
                Is.EqualTo(new byte[] { 192, 0, 2, 1 }));
            Assert.That(ipv6.RecordData!.AmtRelay!.RelayAddress,
                Is.EqualTo(Sequence(16)));
            Assert.That(name.RecordData!.AmtRelay!.DiscoveryOptional, Is.True);
            Assert.That(Format(name.RecordData!.AmtRelay!.RelayName!),
                Is.EqualTo("relay.example."));
            Assert.That(nameBytes[2] & 192, Is.EqualTo(0));
        });
    }

    [Test]
    public void AmtRelayRejectsCompressionUndefinedTypesAndWrongLengths()
    {
        byte[] compressed = [1, 97, 0, 10, 131, 192, 0];
        DnsDecodeResult compressedName =
            ExtendedRecordDataCodec.DecodeRecordData(
                260, compressed, 3, 4, new DnsDecodeOptions());
        DnsDecodeResult undefined = Decode(260, new byte[] { 0, 4 });
        DnsDecodeResult shortIpv4 = Decode(260, new byte[] { 0, 1, 192 });
        DnsDecodeResult extraNone = Decode(260, new byte[] { 0, 0, 1 });

        Assert.Multiple(() =>
        {
            Assert.That(compressedName.Success, Is.False);
            Assert.That(compressedName.Diagnostics[0].Field,
                Is.EqualTo("amtRelay.relayName"));
            Assert.That(undefined.Success, Is.False);
            Assert.That(shortIpv4.Success, Is.False);
            Assert.That(extraNone.Success, Is.False);
        });
    }

    [TestCase(1, 48)]
    [TestCase(2, 64)]
    public void ZonemdRoundTripsKnownHashAlgorithms(
        int hashAlgorithm,
        int digestLength)
    {
        DnsRecordData data = new DnsRecordData();
        data.Zonemd = new DnsZonemdData();
        data.Zonemd.Serial = uint.MaxValue;
        data.Zonemd.Scheme = 1;
        data.Zonemd.HashAlgorithm = hashAlgorithm;
        data.Zonemd.Digest = Sequence(digestLength);

        DnsDecodeResult decoded = Decode(63, Encode(63, data));

        Assert.Multiple(() =>
        {
            Assert.That(decoded.RecordData!.Zonemd!.Serial,
                Is.EqualTo(uint.MaxValue));
            Assert.That(decoded.RecordData!.Zonemd!.Scheme, Is.EqualTo(1));
            Assert.That(decoded.RecordData!.Zonemd!.HashAlgorithm,
                Is.EqualTo(hashAlgorithm));
            Assert.That(decoded.RecordData!.Zonemd!.Digest,
                Is.EqualTo(Sequence(digestLength)));
        });
    }

    [Test]
    public void ZonemdPreservesUnknownAlgorithmsWithMinimumStrengthDigest()
    {
        DnsRecordData data = new DnsRecordData();
        data.Zonemd = new DnsZonemdData();
        data.Zonemd.Scheme = 250;
        data.Zonemd.HashAlgorithm = 250;
        data.Zonemd.Digest = Sequence(12);

        DnsDecodeResult decoded = Decode(63, Encode(63, data));
        DnsDecodeResult shortUnknown = Decode(63, new byte[17]);
        byte[] shortSha384 = new byte[6 + 47];
        shortSha384[5] = 1;
        DnsDecodeResult wrongKnown = Decode(63, shortSha384);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.RecordData!.Zonemd!.Scheme, Is.EqualTo(250));
            Assert.That(decoded.RecordData!.Zonemd!.HashAlgorithm,
                Is.EqualTo(250));
            Assert.That(decoded.RecordData!.Zonemd!.Digest,
                Is.EqualTo(Sequence(12)));
            Assert.That(shortUnknown.Success, Is.False);
            Assert.That(wrongKnown.Success, Is.False);
        });
    }

    [Test]
    public void ResolverInformationRoundTripsKeyValueCharacterStrings()
    {
        byte[] maximum = new byte[255];
        byte[] prefix = [116, 101, 109, 112, 45, 120, 61];
        System.Array.Copy(prefix, maximum, prefix.Length);
        int index = prefix.Length;
        while (index < maximum.Length)
        {
            maximum[index] = (byte)(index % 251);
            index = index + 1;
        }

        DnsRecordData data = new DnsRecordData();
        data.ResolverInformation = new DnsResolverInformationData();
        data.ResolverInformation.Values.Add(
            [113, 110, 97, 109, 101, 109, 105, 110]);
        data.ResolverInformation.Values.Add(
            [101, 120, 116, 101, 114, 114, 61, 49, 53, 45, 49, 55]);
        data.ResolverInformation.Values.Add(maximum);

        DnsDecodeResult decoded = Decode(261, Encode(261, data));

        Assert.Multiple(() =>
        {
            Assert.That(decoded.RecordData!.ResolverInformation!.Values,
                Has.Count.EqualTo(3));
            Assert.That(decoded.RecordData!.ResolverInformation!.Values[0],
                Is.EqualTo(new byte[] { 113, 110, 97, 109, 101, 109, 105, 110 }));
            Assert.That(decoded.RecordData!.ResolverInformation!.Values[2],
                Is.EqualTo(maximum));
        });
    }

    [Test]
    public void ResolverInformationPreservesDuplicatesAndRejectsInvalidKeys()
    {
        DnsDecodeResult empty = Decode(261, []);
        DnsDecodeResult emptyKey = Decode(261, new byte[] { 1, 61 });
        DnsDecodeResult controlKey = Decode(261, new byte[] { 1, 1 });
        DnsDecodeResult truncated = Decode(261, new byte[] { 5, 97, 98 });
        DnsRecordData duplicate = new DnsRecordData();
        duplicate.ResolverInformation = new DnsResolverInformationData();
        duplicate.ResolverInformation.Values.Add([75, 101, 121]);
        duplicate.ResolverInformation.Values.Add([107, 101, 121, 61, 49]);
        DnsDecodeResult duplicateRoundTrip = Decode(261, Encode(261, duplicate));

        Assert.Multiple(() =>
        {
            Assert.That(empty.Success, Is.False);
            Assert.That(emptyKey.Success, Is.False);
            Assert.That(controlKey.Success, Is.False);
            Assert.That(truncated.Success, Is.False);
            Assert.That(duplicateRoundTrip.RecordData!.ResolverInformation!.Values,
                Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void DsyncRoundTripsRawCodesAndUncompressedTarget()
    {
        DnsRecordData data = new DnsRecordData();
        data.Dsync = new DnsDsyncData();
        data.Dsync.RecordTypeValue = 65000;
        data.Dsync.Scheme = 250;
        data.Dsync.Port = 65535;
        data.Dsync.Target = Parse("notify.example.");

        byte[] encoded = Encode(66, data);
        DnsDecodeResult decoded = Decode(66, encoded);

        Assert.Multiple(() =>
        {
            Assert.That(encoded[5] & 192, Is.EqualTo(0));
            Assert.That(decoded.RecordData!.Dsync!.RecordTypeValue,
                Is.EqualTo(65000));
            Assert.That(decoded.RecordData!.Dsync!.Scheme, Is.EqualTo(250));
            Assert.That(decoded.RecordData!.Dsync!.Port, Is.EqualTo(65535));
            Assert.That(Format(decoded.RecordData!.Dsync!.Target),
                Is.EqualTo("notify.example."));
        });
    }

    [Test]
    public void DsyncRejectsCompressedTargetsAndOutOfRangeFields()
    {
        byte[] compressed = [1, 97, 0, 0, 59, 1, 0, 53, 192, 0];
        DnsDecodeResult compressedTarget =
            ExtendedRecordDataCodec.DecodeRecordData(
                66, compressed, 3, 7, new DnsDecodeOptions());
        DnsRecordData data = new DnsRecordData();
        data.Dsync = new DnsDsyncData();
        data.Dsync.RecordTypeValue = 65536;
        data.Dsync.Target = Parse("notify.example.");
        WireWriteResult recordType = EncodeResult(66, data);

        Assert.Multiple(() =>
        {
            Assert.That(compressedTarget.Success, Is.False);
            Assert.That(compressedTarget.Diagnostics[0].Field,
                Is.EqualTo("dsync.target"));
            Assert.That(recordType.Success, Is.False);
        });
    }

    [TestCase(67)]
    [TestCase(68)]
    public void HhitAndBridRoundTripExactCborPayloads(int recordTypeValue)
    {
        byte[] cbor = [131, 1, 97, 97, 65, 0];
        DnsRecordData data = new DnsRecordData();
        data.Opaque = new DnsOpaqueData();
        data.Opaque.Bytes = cbor;

        byte[] encoded = Encode(recordTypeValue, data);
        DnsDecodeResult decoded = Decode(recordTypeValue, encoded);
        DnsDecodeResult empty = Decode(recordTypeValue, []);

        Assert.Multiple(() =>
        {
            Assert.That(encoded, Is.EqualTo(cbor));
            Assert.That(decoded.RecordData!.Opaque!.Bytes, Is.EqualTo(cbor));
            Assert.That(empty.Success, Is.False);
        });
    }

    [Test]
    public void NxnameIsRejectedAsStoredRecordData()
    {
        DnsDecodeResult empty = Decode(128, []);
        DnsDecodeResult bytes = Decode(128, new byte[] { 1 });
        WireWriteResult encoded = EncodeResult(128, new DnsRecordData());

        Assert.Multiple(() =>
        {
            Assert.That(empty.Success, Is.False);
            Assert.That(bytes.Success, Is.False);
            Assert.That(encoded.Success, Is.False);
            Assert.That(encoded.Diagnostics[0].Field, Is.EqualTo("nxname"));
        });
    }

    [Test]
    public void CodecRejectsUnsupportedTypesAndMissingSchemas()
    {
        DnsDecodeResult unsupported = Decode(69, []);
        WireWriteResult unsupportedWrite = EncodeResult(69, new DnsRecordData());
        WireWriteResult missingZonemd = EncodeResult(63, new DnsRecordData());

        Assert.Multiple(() =>
        {
            Assert.That(unsupported.Success, Is.False);
            Assert.That(unsupportedWrite.Success, Is.False);
            Assert.That(missingZonemd.Success, Is.False);
            Assert.That(missingZonemd.Diagnostics[0].Field, Is.EqualTo("zonemd"));
        });
    }

    static DnsRecordData AmtAddressData(int relayType, byte[] address)
    {
        DnsRecordData data = new DnsRecordData();
        data.AmtRelay = new DnsAmtRelayData();
        data.AmtRelay.Precedence = 255;
        data.AmtRelay.RelayType = relayType;
        data.AmtRelay.RelayAddress = address;
        return data;
    }

    static byte[] Sequence(int length)
    {
        byte[] bytes = new byte[length];
        int index = 0;
        while (index < bytes.Length)
        {
            bytes[index] = (byte)(index % 251 + 1);
            index = index + 1;
        }

        return bytes;
    }

    static DnsDecodeResult Decode(int recordTypeValue, byte[] bytes)
    {
        return ExtendedRecordDataCodec.DecodeRecordData(
            recordTypeValue,
            bytes,
            0,
            bytes.Length,
            new DnsDecodeOptions());
    }

    static byte[] Encode(int recordTypeValue, DnsRecordData data)
    {
        WireWriter writer = new WireWriter(new byte[4096], 0, 4096);
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
        WireWriter writer = new WireWriter(new byte[4096], 0, 4096);
        return ExtendedRecordDataCodec.EncodeRecordData(
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
