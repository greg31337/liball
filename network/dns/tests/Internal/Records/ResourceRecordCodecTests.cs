namespace dns.Tests.Internal.Records;

public class ResourceRecordCodecTests
{
    [Test]
    public void DispatcherRoundTripsRepresentativeCodecFamilies()
    {
        DnsRecordData address = new DnsRecordData();
        address.Address = new DnsAddressData();
        address.Address.Address = [192, 0, 2, 1];

        DnsRecordData responsiblePerson = new DnsRecordData();
        responsiblePerson.TwoName = new DnsTwoNameData();
        responsiblePerson.TwoName.FirstName = Parse("mailbox.example.");
        responsiblePerson.TwoName.SecondName = Parse("text.example.");

        DnsRecordData sshfp = new DnsRecordData();
        sshfp.Sshfp = new DnsSshfpData();
        sshfp.Sshfp.Algorithm = 1;
        sshfp.Sshfp.FingerprintType = 1;
        sshfp.Sshfp.Fingerprint = Sequence(20);

        DnsRecordData nsec = new DnsRecordData();
        nsec.Nsec = new DnsNsecData();
        nsec.Nsec.NextDomainName = Parse("next.example.");
        nsec.Nsec.RecordTypeValues.Add(1);

        DnsRecordData address6 = new DnsRecordData();
        address6.Address = new DnsAddressData();
        address6.Address.Address = Sequence(16);

        DnsRecordData zonemd = new DnsRecordData();
        zonemd.Zonemd = new DnsZonemdData();
        zonemd.Zonemd.Scheme = 250;
        zonemd.Zonemd.HashAlgorithm = 250;
        zonemd.Zonemd.Digest = Sequence(12);

        DnsDecodeResult decodedAddress = RoundTripData(1, address);
        DnsDecodeResult decodedPerson = RoundTripData(17, responsiblePerson);
        DnsDecodeResult decodedSshfp = RoundTripData(44, sshfp);
        DnsDecodeResult decodedNsec = RoundTripData(47, nsec);
        DnsDecodeResult decodedAddress6 = RoundTripData(28, address6);
        DnsDecodeResult decodedZonemd = RoundTripData(63, zonemd);

        Assert.Multiple(() =>
        {
            Assert.That(decodedAddress.RecordData!.Address!.Address,
                Is.EqualTo(new byte[] { 192, 0, 2, 1 }));
            Assert.That(Format(decodedPerson.RecordData!.TwoName!.FirstName),
                Is.EqualTo("mailbox.example."));
            Assert.That(decodedSshfp.RecordData!.Sshfp!.Fingerprint,
                Is.EqualTo(Sequence(20)));
            Assert.That(decodedNsec.RecordData!.Nsec!.RecordTypeValues,
                Is.EqualTo(new[] { 1 }));
            Assert.That(decodedAddress6.RecordData!.Address!.Address,
                Is.EqualTo(Sequence(16)));
            Assert.That(decodedZonemd.RecordData!.Zonemd!.Digest,
                Is.EqualTo(Sequence(12)));
        });
    }

    [Test]
    public void A6RoundTripsAllPrefixBoundaryShapes()
    {
        DnsRecordData complete = A6Data(0, Sequence(16), null);
        DnsRecordData partial = A6Data(
            65, Sequence(8), Parse("prefix.example."));
        DnsRecordData prefixOnly = A6Data(
            128, [], Parse("prefix.example."));

        DnsA6Data first = RoundTripData(38, complete).RecordData!.A6!;
        DnsA6Data second = RoundTripData(38, partial).RecordData!.A6!;
        DnsA6Data third = RoundTripData(38, prefixOnly).RecordData!.A6!;

        Assert.Multiple(() =>
        {
            Assert.That(first.PrefixLength, Is.EqualTo(0));
            Assert.That(first.AddressSuffix, Is.EqualTo(Sequence(16)));
            Assert.That(first.PrefixName, Is.Null);
            Assert.That(second.PrefixLength, Is.EqualTo(65));
            Assert.That(second.AddressSuffix, Is.EqualTo(Sequence(8)));
            Assert.That(Format(second.PrefixName!), Is.EqualTo("prefix.example."));
            Assert.That(third.PrefixLength, Is.EqualTo(128));
            Assert.That(third.AddressSuffix, Is.Empty);
            Assert.That(Format(third.PrefixName!), Is.EqualTo("prefix.example."));
        });
    }

    [Test]
    public void A6RejectsInvalidPrefixSuffixNameAndCompression()
    {
        DnsDecodeResult prefix = ResourceRecordCodec.DecodeRecordData(
            38, [129], 0, 1, new DnsDecodeOptions());
        DnsDecodeResult suffix = ResourceRecordCodec.DecodeRecordData(
            38, [0, 1], 0, 2, new DnsDecodeOptions());
        DnsDecodeResult unexpectedName = ResourceRecordCodec.DecodeRecordData(
            38, CompleteA6WireWithName(), 0, 18, new DnsDecodeOptions());
        byte[] containingMessage = NameWire("prefix.example.")
            .Concat(new byte[] { 128, 192, 0 }).ToArray();
        int dataOffset = NameWireLength(containingMessage);
        DnsDecodeResult compressed = ResourceRecordCodec.DecodeRecordData(
            38, containingMessage, dataOffset, 3, new DnsDecodeOptions());
        DnsEncodeResult wrongSuffix = ResourceRecordCodec.EncodeRecordData(
            38,
            A6Data(64, new byte[7], Parse("prefix.example.")),
            new DnsEncodeOptions());
        DnsEncodeResult missingName = ResourceRecordCodec.EncodeRecordData(
            38,
            A6Data(128, [], null),
            new DnsEncodeOptions());
        DnsEncodeResult padding = ResourceRecordCodec.EncodeRecordData(
            38,
            A6Data(65, new byte[] { 128, 0, 0, 0, 0, 0, 0, 0 },
                Parse("prefix.example.")),
            new DnsEncodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(prefix.Success, Is.False);
            Assert.That(suffix.Success, Is.False);
            Assert.That(unexpectedName.Success, Is.False);
            Assert.That(compressed.Success, Is.False);
            Assert.That(compressed.Diagnostics[0].Field,
                Is.EqualTo("a6.prefixName"));
            Assert.That(wrongSuffix.Success, Is.False);
            Assert.That(missingName.Success, Is.False);
            Assert.That(padding.Success, Is.False);
        });
    }

    [Test]
    public void TypedFailuresOptionallyFallBackToExactOpaqueData()
    {
        byte[] malformedAddress = [192, 0, 2];
        DnsDecodeOptions strict = new DnsDecodeOptions();
        DnsDecodeOptions fallback = new DnsDecodeOptions();
        fallback.FallBackToOpaqueRecordData = true;

        DnsDecodeResult failed = ResourceRecordCodec.DecodeRecordData(
            1, malformedAddress, 0, malformedAddress.Length, strict);
        DnsDecodeResult preserved = ResourceRecordCodec.DecodeRecordData(
            1, malformedAddress, 0, malformedAddress.Length, fallback);

        Assert.Multiple(() =>
        {
            Assert.That(failed.Success, Is.False);
            Assert.That(preserved.Success, Is.True);
            Assert.That(preserved.RecordData!.Address, Is.Null);
            Assert.That(preserved.RecordData.Opaque!.Bytes,
                Is.EqualTo(malformedAddress));
            Assert.That(preserved.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void OpaqueSelectionBypassesValidTypedDataAndUnknownsAreAutomatic()
    {
        DnsDecodeOptions opaque = new DnsDecodeOptions();
        opaque.DecodeTypedRecordData = false;
        byte[] validAddress = [192, 0, 2, 1];

        DnsDecodeResult forced = ResourceRecordCodec.DecodeRecordData(
            1, validAddress, 0, validAddress.Length, opaque);
        DnsDecodeResult unknown = ResourceRecordCodec.DecodeRecordData(
            65000, [9, 8, 7], 0, 3, new DnsDecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(forced.RecordData!.Opaque!.Bytes,
                Is.EqualTo(validAddress));
            Assert.That(forced.RecordData.Address, Is.Null);
            Assert.That(unknown.RecordData!.Opaque!.Bytes,
                Is.EqualTo(new byte[] { 9, 8, 7 }));
        });
    }

    [Test]
    public void ResourceRecordRoundTripDerivesRdataLengthAndPreservesRawBytes()
    {
        DnsRecordData data = new DnsRecordData();
        data.Address = new DnsAddressData();
        data.Address.Address = [203, 0, 113, 9];
        DnsResourceRecord source = Record(
            "host.example.", DnsRecordType.A, data);
        source.Class = DnsClass.Internet;
        source.ClassValue = 1;
        source.TimeToLive = uint.MaxValue;

        DnsEncodeResult encoded = ResourceRecordCodec.EncodeResourceRecord(
            source, new DnsEncodeOptions());
        int ownerLength = NameWireLength(encoded.Bytes);
        DnsDecodeResult decoded = ResourceRecordCodec.DecodeResourceRecord(
            encoded.Bytes.Concat(new byte[] { 99, 98 }).ToArray(),
            0,
            new DnsDecodeOptions());
        DnsResourceRecord record = decoded.Record!;

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Bytes[ownerLength + 8], Is.EqualTo(0));
            Assert.That(encoded.Bytes[ownerLength + 9], Is.EqualTo(4));
            Assert.That(decoded.BytesConsumed, Is.EqualTo(encoded.Bytes.Length));
            Assert.That(decoded.TrailingBytes, Is.EqualTo(new byte[] { 99, 98 }));
            Assert.That(Format(record.Name), Is.EqualTo("host.example."));
            Assert.That(record.RecordTypeValue, Is.EqualTo(1));
            Assert.That(record.ClassValue, Is.EqualTo(1));
            Assert.That(record.TimeToLive, Is.EqualTo(uint.MaxValue));
            Assert.That(record.RawData,
                Is.EqualTo(new byte[] { 203, 0, 113, 9 }));
            Assert.That(record.Data!.Address!.Address,
                Is.EqualTo(record.RawData));
        });
    }

    [Test]
    public void ResourceRecordDecoderRejectsTruncatedAndLimitedRdlength()
    {
        DnsEncodeResult encoded = ResourceRecordCodec.EncodeResourceRecord(
            Record("host.example.", DnsRecordType.A, AddressData()),
            new DnsEncodeOptions());
        int ownerLength = NameWireLength(encoded.Bytes);
        byte[] truncated = encoded.Bytes.Take(encoded.Bytes.Length - 1).ToArray();
        DnsDecodeOptions limited = new DnsDecodeOptions();
        limited.MaximumRecordDataLength = 3;

        DnsDecodeResult missing = ResourceRecordCodec.DecodeResourceRecord(
            truncated, 0, new DnsDecodeOptions());
        DnsDecodeResult overLimit = ResourceRecordCodec.DecodeResourceRecord(
            encoded.Bytes, 0, limited);

        Assert.Multiple(() =>
        {
            Assert.That(missing.Success, Is.False);
            Assert.That(missing.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.RecordDataLengthMismatch));
            Assert.That(missing.Diagnostics[0].Offset,
                Is.EqualTo(ownerLength + 8));
            Assert.That(overLimit.Success, Is.False);
            Assert.That(overLimit.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.ResourceLimitExceeded));
        });
    }

    [Test]
    public void ResourceRecordFallbackConsumesExactlyDeclaredRdlength()
    {
        DnsResourceRecord source = Record(
            "host.example.", DnsRecordType.A, AddressData());
        byte[] encoded = ResourceRecordCodec.EncodeResourceRecord(
            source, new DnsEncodeOptions()).Bytes;
        int ownerLength = NameWireLength(encoded);
        encoded[ownerLength + 9] = 3;
        DnsDecodeOptions options = new DnsDecodeOptions();
        options.FallBackToOpaqueRecordData = true;

        DnsDecodeResult decoded = ResourceRecordCodec.DecodeResourceRecord(
            encoded, 0, options);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.Record!.RawData,
                Is.EqualTo(new byte[] { 192, 0, 2 }));
            Assert.That(decoded.Record.Data!.Opaque!.Bytes,
                Is.EqualTo(decoded.Record.RawData));
            Assert.That(decoded.BytesConsumed, Is.EqualTo(encoded.Length - 1));
            Assert.That(decoded.TrailingBytes,
                Is.EqualTo(new byte[] { 1 }));
        });
    }

    [Test]
    public void RawRecordEncodingUsesCallerBytesAndPatchesLength()
    {
        DnsResourceRecord record = Record(
            "raw.example.", DnsRecordType.A, AddressData());
        record.UseRawData = true;
        record.RawData = [1, 2, 3];

        DnsEncodeResult encoded = ResourceRecordCodec.EncodeResourceRecord(
            record, new DnsEncodeOptions());
        int ownerLength = NameWireLength(encoded.Bytes);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Bytes[ownerLength + 8], Is.EqualTo(0));
            Assert.That(encoded.Bytes[ownerLength + 9], Is.EqualTo(3));
            Assert.That(encoded.Bytes.TakeLast(3),
                Is.EqualTo(new byte[] { 1, 2, 3 }));
        });
    }

    [Test]
    public void SharedCompressionTableCompressesOwnersAndPermittedRdataNames()
    {
        WireWriter writer = new WireWriter(new byte[1024], 0, 1024);
        CompressionTable table = new CompressionTable(0);
        DnsEncodeOptions options = new DnsEncodeOptions();
        DnsResourceRecord address = Record(
            "target.example.", DnsRecordType.A, AddressData());
        DnsRecordData nameData = new DnsRecordData();
        nameData.Name = new DnsNameData();
        nameData.Name.Name = Parse("target.example.");
        DnsResourceRecord nameServer = Record(
            "target.example.", DnsRecordType.Ns, nameData);

        WireWriteResult first = ResourceRecordCodec.WriteResourceRecord(
            writer, table, address, options);
        int secondOffset = writer.WriteOffset();
        WireWriteResult second = ResourceRecordCodec.WriteResourceRecord(
            writer, table, nameServer, options);
        byte[] bytes = writer.CopyWrittenBytes();
        int secondDataOffset = secondOffset + 2 + 10;
        DnsDecodeResult decodedSecond = ResourceRecordCodec.DecodeResourceRecord(
            bytes, secondOffset, new DnsDecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True);
            Assert.That(second.Success, Is.True);
            Assert.That(bytes[secondOffset] & 192, Is.EqualTo(192));
            Assert.That(bytes[secondDataOffset] & 192, Is.EqualTo(192));
            Assert.That(decodedSecond.Success, Is.True);
            Assert.That(Format(decodedSecond.Record!.Name),
                Is.EqualTo("target.example."));
            Assert.That(Format(decodedSecond.Record.Data!.Name!.Name),
                Is.EqualTo("target.example."));
        });
    }

    [Test]
    public void SchemaPoliciesKeepSrvAndA6RdataNamesUncompressed()
    {
        WireWriter writer = new WireWriter(new byte[2048], 0, 2048);
        CompressionTable table = new CompressionTable(0);
        DnsEncodeOptions options = new DnsEncodeOptions();
        DnsResourceRecord address = Record(
            "target.example.", DnsRecordType.A, AddressData());
        DnsRecordData srvData = new DnsRecordData();
        srvData.Srv = new DnsSrvData();
        srvData.Srv.Target = Parse("target.example.");
        DnsResourceRecord srv = Record(
            "service.example.", DnsRecordType.Srv, srvData);
        DnsResourceRecord a6 = Record(
            "a6.example.",
            DnsRecordType.A6,
            A6Data(128, [], Parse("target.example.")));

        ResourceRecordCodec.WriteResourceRecord(writer, table, address, options);
        int srvOffset = writer.WriteOffset();
        ResourceRecordCodec.WriteResourceRecord(writer, table, srv, options);
        int a6Offset = writer.WriteOffset();
        ResourceRecordCodec.WriteResourceRecord(writer, table, a6, options);
        byte[] bytes = writer.CopyWrittenBytes();
        int srvOwnerLength = EncodedNameLength(bytes, srvOffset);
        int srvTargetOffset = srvOffset + srvOwnerLength + 10 + 6;
        int a6OwnerLength = EncodedNameLength(bytes, a6Offset);
        int a6PrefixOffset = a6Offset + a6OwnerLength + 10 + 1;

        Assert.Multiple(() =>
        {
            Assert.That(bytes[srvTargetOffset] & 192, Is.EqualTo(0));
            Assert.That(bytes[a6PrefixOffset] & 192, Is.EqualTo(0));
        });
    }

    [Test]
    public void DecoderAllowsCompressionOnlyForSchemaPermittedFields()
    {
        byte[] name = NameWire("target.example.");
        byte[] message = name.Concat(new byte[] { 192, 0 }).ToArray();
        int offset = name.Length;
        DnsDecodeResult ns = ResourceRecordCodec.DecodeRecordData(
            2, message, offset, 2, new DnsDecodeOptions());
        DnsDecodeResult dname = ResourceRecordCodec.DecodeRecordData(
            39, message, offset, 2, new DnsDecodeOptions());
        byte[] srvMessage = name.Concat(
            new byte[] { 0, 0, 0, 0, 0, 0, 192, 0 }).ToArray();
        DnsDecodeResult srv = ResourceRecordCodec.DecodeRecordData(
            33, srvMessage, offset, 8, new DnsDecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(ns.Success, Is.True);
            Assert.That(Format(ns.RecordData!.Name!.Name),
                Is.EqualTo("target.example."));
            Assert.That(dname.Success, Is.False);
            Assert.That(srv.Success, Is.False);
        });
    }

    [Test]
    public void MetaTypesAndInvalidOptionsAreRejectedAtRecordBoundary()
    {
        DnsResourceRecord meta = Record(
            "example.", DnsRecordType.Any, OpaqueData([1]));
        DnsEncodeOptions badEncode = new DnsEncodeOptions();
        badEncode.MaximumMessageLength = 65536;
        DnsDecodeOptions badDecode = new DnsDecodeOptions();
        badDecode.MaximumRecordDataLength = -1;

        DnsEncodeResult metaResult = ResourceRecordCodec.EncodeResourceRecord(
            meta, new DnsEncodeOptions());
        DnsEncodeResult optionResult = ResourceRecordCodec.EncodeRecordData(
            1, AddressData(), badEncode);
        DnsDecodeResult decodeResult = ResourceRecordCodec.DecodeRecordData(
            1, [192, 0, 2, 1], 0, 4, badDecode);

        Assert.Multiple(() =>
        {
            Assert.That(metaResult.Success, Is.False);
            Assert.That(metaResult.Diagnostics[0].Field,
                Is.EqualTo("resourceRecord.type"));
            Assert.That(optionResult.Success, Is.False);
            Assert.That(decodeResult.Success, Is.False);
        });
    }

    static DnsDecodeResult RoundTripData(int recordTypeValue, DnsRecordData data)
    {
        DnsEncodeResult encoded = ResourceRecordCodec.EncodeRecordData(
            recordTypeValue, data, new DnsEncodeOptions());
        Assert.That(encoded.Success, Is.True);
        DnsDecodeResult decoded = ResourceRecordCodec.DecodeRecordData(
            recordTypeValue,
            encoded.Bytes,
            0,
            encoded.Bytes.Length,
            new DnsDecodeOptions());
        Assert.That(decoded.Success, Is.True);
        return decoded;
    }

    static DnsResourceRecord Record(
        string owner,
        DnsRecordType type,
        DnsRecordData data)
    {
        DnsResourceRecord record = new DnsResourceRecord();
        record.Name = Parse(owner);
        record.RecordType = type;
        record.RecordTypeValue = (int)type;
        record.Class = DnsClass.Internet;
        record.ClassValue = 1;
        record.TimeToLive = 300;
        record.Data = data;
        return record;
    }

    static DnsRecordData AddressData()
    {
        DnsRecordData data = new DnsRecordData();
        data.Address = new DnsAddressData();
        data.Address.Address = [192, 0, 2, 1];
        return data;
    }

    static DnsRecordData A6Data(
        int prefixLength,
        byte[] suffix,
        DnsName? prefixName)
    {
        DnsRecordData data = new DnsRecordData();
        data.A6 = new DnsA6Data();
        data.A6.PrefixLength = prefixLength;
        data.A6.AddressSuffix = suffix;
        data.A6.PrefixName = prefixName;
        return data;
    }

    static DnsRecordData OpaqueData(byte[] bytes)
    {
        DnsRecordData data = new DnsRecordData();
        data.Opaque = new DnsOpaqueData();
        data.Opaque.Bytes = bytes;
        return data;
    }

    static byte[] CompleteA6WireWithName()
    {
        return new byte[] { 0 }.Concat(Sequence(16)).Concat(new byte[] { 0 }).ToArray();
    }

    static byte[] NameWire(string text)
    {
        DnsNameData name = new DnsNameData();
        name.Name = Parse(text);
        DnsRecordData data = new DnsRecordData();
        data.Name = name;
        return ResourceRecordCodec.EncodeRecordData(
            39, data, new DnsEncodeOptions()).Bytes;
    }

    static int NameWireLength(byte[] bytes)
    {
        return EncodedNameLength(bytes, 0);
    }

    static int EncodedNameLength(byte[] bytes, int offset)
    {
        int cursor = offset;
        while (bytes[cursor] != 0 && (bytes[cursor] & 192) != 192)
        {
            cursor = cursor + bytes[cursor] + 1;
        }

        return (bytes[cursor] & 192) == 192 ? cursor - offset + 2 :
            cursor - offset + 1;
    }

    static byte[] Sequence(int length)
    {
        return Enumerable.Range(1, length).Select(value => (byte)value).ToArray();
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
