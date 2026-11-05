namespace dns.Tests.Internal.Records;

public class ModernRecordDataCodecTests
{
    [Test]
    public void AaaaRoundTripsExactlySixteenAddressOctets()
    {
        byte[] address =
        [
            32, 1, 13, 184, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 1
        ];
        DnsRecordData data = new DnsRecordData();
        data.Address = new DnsAddressData();
        data.Address.Address = address;

        byte[] encoded = Encode(28, data);
        DnsDecodeResult decoded = Decode(28, encoded);
        DnsDecodeResult shortAddress = Decode(28, new byte[15]);

        Assert.Multiple(() =>
        {
            Assert.That(encoded, Is.EqualTo(address));
            Assert.That(decoded.RecordData!.Address!.Address, Is.EqualTo(address));
            Assert.That(shortAddress.Success, Is.False);
            Assert.That(shortAddress.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.InvalidLength));
        });
    }

    [Test]
    public void LocRoundTripsVersionZeroFieldsAtTheirFullWireWidths()
    {
        DnsRecordData data = new DnsRecordData();
        data.Loc = new DnsLocData();
        data.Loc.Version = 0;
        data.Loc.Size = 0x12;
        data.Loc.HorizontalPrecision = 0x34;
        data.Loc.VerticalPrecision = 0x56;
        data.Loc.Latitude = uint.MaxValue;
        data.Loc.Longitude = 0x80000000;
        data.Loc.Altitude = 1;

        DnsDecodeResult decoded = Decode(29, Encode(29, data));

        Assert.Multiple(() =>
        {
            Assert.That(decoded.RecordData!.Loc!.Size, Is.EqualTo(0x12));
            Assert.That(decoded.RecordData!.Loc!.HorizontalPrecision,
                Is.EqualTo(0x34));
            Assert.That(decoded.RecordData!.Loc!.VerticalPrecision,
                Is.EqualTo(0x56));
            Assert.That(decoded.RecordData!.Loc!.Latitude, Is.EqualTo(uint.MaxValue));
            Assert.That(decoded.RecordData!.Loc!.Longitude, Is.EqualTo(0x80000000));
            Assert.That(decoded.RecordData!.Loc!.Altitude, Is.EqualTo(1));
        });
    }

    [Test]
    public void LocRejectsUnknownVersionsAndInvalidPrecisionDigits()
    {
        byte[] unknownVersion = new byte[16];
        unknownVersion[0] = 1;
        byte[] invalidMantissa = new byte[16];
        invalidMantissa[1] = 0xa0;
        byte[] undefinedZeroMantissa = new byte[16];
        undefinedZeroMantissa[1] = 1;

        DnsDecodeResult version = Decode(29, unknownVersion);
        DnsDecodeResult mantissa = Decode(29, invalidMantissa);
        DnsDecodeResult zeroMantissa = Decode(29, undefinedZeroMantissa);

        Assert.Multiple(() =>
        {
            Assert.That(version.Success, Is.False);
            Assert.That(version.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.UnsupportedVersion));
            Assert.That(mantissa.Success, Is.False);
            Assert.That(zeroMantissa.Success, Is.False);
        });
    }

    [Test]
    public void SrvRoundTripsUnsignedFieldsAndAnUncompressedTarget()
    {
        DnsRecordData data = new DnsRecordData();
        data.Srv = new DnsSrvData();
        data.Srv.Priority = 65535;
        data.Srv.Weight = 32768;
        data.Srv.Port = 443;
        data.Srv.Target = Parse("service.example.");

        byte[] encoded = Encode(33, data);
        DnsDecodeResult decoded = Decode(33, encoded);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.RecordData!.Srv!.Priority, Is.EqualTo(65535));
            Assert.That(decoded.RecordData!.Srv!.Weight, Is.EqualTo(32768));
            Assert.That(decoded.RecordData!.Srv!.Port, Is.EqualTo(443));
            Assert.That(Format(decoded.RecordData!.Srv!.Target),
                Is.EqualTo("service.example."));
            Assert.That(encoded[6] & 192, Is.EqualTo(0));
        });
    }

    [Test]
    public void SrvAndDnameRejectCompressedNames()
    {
        byte[] srvMessage =
        [
            1, 97, 0,
            0, 1, 0, 2, 0, 53, 192, 0
        ];
        byte[] dnameMessage = [1, 97, 0, 192, 0];

        DnsDecodeResult srv = ModernRecordDataCodec.DecodeRecordData(
            33, srvMessage, 3, 8, new DnsDecodeOptions());
        DnsDecodeResult dname = ModernRecordDataCodec.DecodeRecordData(
            39, dnameMessage, 3, 2, new DnsDecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(srv.Success, Is.False);
            Assert.That(srv.Diagnostics[0].Field, Is.EqualTo("srv.target"));
            Assert.That(dname.Success, Is.False);
            Assert.That(dname.Diagnostics[0].Field, Is.EqualTo("dname.target"));
        });
    }

    [Test]
    public void NaptrRoundTripsCharacterStringsAndRootReplacement()
    {
        DnsRecordData data = new DnsRecordData();
        data.Naptr = new DnsNaptrData();
        data.Naptr.Order = 100;
        data.Naptr.Preference = 10;
        data.Naptr.Flags = [85];
        data.Naptr.Services = [69, 50, 85, 43, 115, 105, 112];
        data.Naptr.RegularExpression = [33, 94, 46, 42, 36, 33];
        data.Naptr.Replacement = Parse(".");

        DnsDecodeResult decoded = Decode(35, Encode(35, data));

        Assert.Multiple(() =>
        {
            Assert.That(decoded.RecordData!.Naptr!.Order, Is.EqualTo(100));
            Assert.That(decoded.RecordData!.Naptr!.Preference, Is.EqualTo(10));
            Assert.That(decoded.RecordData!.Naptr!.Flags, Is.EqualTo(new byte[] { 85 }));
            Assert.That(decoded.RecordData!.Naptr!.Services,
                Is.EqualTo(new byte[] { 69, 50, 85, 43, 115, 105, 112 }));
            Assert.That(Format(decoded.RecordData!.Naptr!.Replacement), Is.EqualTo("."));
        });
    }

    [Test]
    public void NaptrRejectsConflictingSubstitutionsAndCompressedReplacement()
    {
        DnsRecordData conflict = new DnsRecordData();
        conflict.Naptr = new DnsNaptrData();
        conflict.Naptr.RegularExpression = [120];
        conflict.Naptr.Replacement = Parse("replacement.example.");
        WireWriteResult conflictingWrite = EncodeResult(35, conflict);
        byte[] compressed =
        [
            1, 97, 0,
            0, 1, 0, 2, 0, 0, 0, 192, 0
        ];
        DnsDecodeResult compressedReplacement =
            ModernRecordDataCodec.DecodeRecordData(
                35, compressed, 3, 9, new DnsDecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(conflictingWrite.Success, Is.False);
            Assert.That(compressedReplacement.Success, Is.False);
            Assert.That(compressedReplacement.Diagnostics[0].Field,
                Is.EqualTo("naptr.replacement"));
        });
    }

    [Test]
    public void CertRoundTripsUnknownAlgorithmsAndCertificateBytes()
    {
        DnsRecordData data = new DnsRecordData();
        data.Cert = new DnsCertData();
        data.Cert.CertificateType = 65535;
        data.Cert.KeyTag = 65534;
        data.Cert.AlgorithmValue = 250;
        data.Cert.Certificate = [0, 1, 2, 255];

        DnsDecodeResult decoded = Decode(37, Encode(37, data));

        Assert.Multiple(() =>
        {
            Assert.That(decoded.RecordData!.Cert!.CertificateType,
                Is.EqualTo(65535));
            Assert.That(decoded.RecordData!.Cert!.KeyTag, Is.EqualTo(65534));
            Assert.That(decoded.RecordData!.Cert!.AlgorithmValue, Is.EqualTo(250));
            Assert.That(decoded.RecordData!.Cert!.Certificate,
                Is.EqualTo(new byte[] { 0, 1, 2, 255 }));
        });
    }

    [Test]
    public void DnameRoundTripsItsUncompressedTarget()
    {
        DnsRecordData data = new DnsRecordData();
        data.Name = new DnsNameData();
        data.Name.Name = Parse("target.example.");

        byte[] encoded = Encode(39, data);
        DnsDecodeResult decoded = Decode(39, encoded);

        Assert.Multiple(() =>
        {
            Assert.That(Format(decoded.RecordData!.Name!.Name),
                Is.EqualTo("target.example."));
            Assert.That(encoded[0] & 192, Is.EqualTo(0));
        });
    }

    [Test]
    public void AplPreservesOrderedPrefixesAndNegation()
    {
        DnsRecordData data = new DnsRecordData();
        data.Apl = new DnsAplData();
        DnsAplItem ipv4 = new DnsAplItem();
        ipv4.AddressFamily = 1;
        ipv4.PrefixLength = 24;
        ipv4.Address = [192, 0, 2];
        data.Apl.Items.Add(ipv4);
        DnsAplItem ipv6 = new DnsAplItem();
        ipv6.AddressFamily = 2;
        ipv6.PrefixLength = 32;
        ipv6.Negated = true;
        ipv6.Address = [32, 1, 13, 184];
        data.Apl.Items.Add(ipv6);

        byte[] encoded = Encode(42, data);
        DnsDecodeResult decoded = Decode(42, encoded);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.RecordData!.Apl!.Items, Has.Count.EqualTo(2));
            Assert.That(decoded.RecordData!.Apl!.Items[0].AddressFamily,
                Is.EqualTo(1));
            Assert.That(decoded.RecordData!.Apl!.Items[0].Address,
                Is.EqualTo(new byte[] { 192, 0, 2 }));
            Assert.That(decoded.RecordData!.Apl!.Items[1].Negated, Is.True);
            Assert.That(encoded[10] & 128, Is.EqualTo(128));
        });
    }

    [Test]
    public void AplRejectsTrailingZeroOctetsAndNonzeroHostBits()
    {
        byte[] trailingZero = [0, 1, 24, 3, 192, 0, 0];
        byte[] hostBits = [0, 1, 17, 3, 192, 0, 127];
        byte[] unknownFamilyTrailingZero = [3, 231, 16, 2, 1, 0];

        DnsDecodeResult trailing = Decode(42, trailingZero);
        DnsDecodeResult host = Decode(42, hostBits);
        DnsDecodeResult unknown = Decode(42, unknownFamilyTrailingZero);

        Assert.Multiple(() =>
        {
            Assert.That(trailing.Success, Is.False);
            Assert.That(host.Success, Is.False);
            Assert.That(unknown.Success, Is.False);
        });
    }

    [Test]
    public void UriRoundTripsTargetsLongerThanACharacterString()
    {
        byte[] target = new byte[300];
        int index = 0;
        while (index < target.Length)
        {
            target[index] = (byte)(index % 251);
            index = index + 1;
        }

        DnsRecordData data = new DnsRecordData();
        data.Uri = new DnsUriData();
        data.Uri.Priority = 65535;
        data.Uri.Weight = 123;
        data.Uri.Target = target;

        DnsDecodeResult decoded = Decode(256, Encode(256, data));
        DnsDecodeResult empty = Decode(256, new byte[] { 0, 1, 0, 2 });

        Assert.Multiple(() =>
        {
            Assert.That(decoded.RecordData!.Uri!.Priority, Is.EqualTo(65535));
            Assert.That(decoded.RecordData!.Uri!.Weight, Is.EqualTo(123));
            Assert.That(decoded.RecordData!.Uri!.Target, Is.EqualTo(target));
            Assert.That(empty.Success, Is.False);
        });
    }

    [Test]
    public void CaaRoundTripsCriticalPropertiesAndRejectsReservedFlagsOrTags()
    {
        DnsRecordData data = new DnsRecordData();
        data.Caa = new DnsCaaData();
        data.Caa.Flags = 128;
        data.Caa.Tag = [105, 115, 115, 117, 101];
        data.Caa.Value = [99, 97, 46, 101, 120, 97, 109, 112, 108, 101];

        DnsDecodeResult decoded = Decode(257, Encode(257, data));
        DnsDecodeResult reservedFlag = Decode(257, [1, 1, 97]);
        DnsDecodeResult punctuationTag = Decode(257, [0, 3, 97, 45, 98]);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.RecordData!.Caa!.Flags, Is.EqualTo(128));
            Assert.That(decoded.RecordData!.Caa!.Tag,
                Is.EqualTo(new byte[] { 105, 115, 115, 117, 101 }));
            Assert.That(decoded.RecordData!.Caa!.Value, Is.EqualTo(data.Caa.Value));
            Assert.That(reservedFlag.Success, Is.False);
            Assert.That(punctuationTag.Success, Is.False);
        });
    }

    [TestCase(108, 6)]
    [TestCase(109, 8)]
    public void EuiRecordsRequireAndRoundTripTheirExactWidths(
        int recordTypeValue,
        int width)
    {
        byte[] address = new byte[width];
        int index = 0;
        while (index < address.Length)
        {
            address[index] = (byte)(index + 1);
            index = index + 1;
        }

        DnsRecordData data = new DnsRecordData();
        data.Eui = new DnsEuiData();
        data.Eui.Address = address;
        DnsDecodeResult decoded = Decode(recordTypeValue, Encode(recordTypeValue, data));
        DnsDecodeResult shortAddress = Decode(recordTypeValue, new byte[width - 1]);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.RecordData!.Eui!.Address, Is.EqualTo(address));
            Assert.That(shortAddress.Success, Is.False);
        });
    }

    [Test]
    public void CodecRejectsUnsupportedTypesAndMissingSchemas()
    {
        DnsDecodeResult unsupported = Decode(999, []);
        WireWriteResult unsupportedWrite = EncodeResult(999, new DnsRecordData());
        WireWriteResult missingAaaa = EncodeResult(28, new DnsRecordData());

        Assert.Multiple(() =>
        {
            Assert.That(unsupported.Success, Is.False);
            Assert.That(unsupportedWrite.Success, Is.False);
            Assert.That(missingAaaa.Success, Is.False);
            Assert.That(missingAaaa.Diagnostics[0].Field, Is.EqualTo("address"));
        });
    }

    static DnsDecodeResult Decode(int recordTypeValue, byte[] bytes)
    {
        return ModernRecordDataCodec.DecodeRecordData(
            recordTypeValue,
            bytes,
            0,
            bytes.Length,
            new DnsDecodeOptions());
    }

    static byte[] Encode(int recordTypeValue, DnsRecordData data)
    {
        WireWriter writer = new WireWriter(new byte[2048], 0, 2048);
        WireWriteResult result = ModernRecordDataCodec.EncodeRecordData(
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
        WireWriter writer = new WireWriter(new byte[2048], 0, 2048);
        return ModernRecordDataCodec.EncodeRecordData(
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
