namespace dns.Tests.Internal.Records;

public class DenialRecordDataCodecTests
{
    [Test]
    public void NxtRoundTripsLegacyBitmapAndMayCompressNextName()
    {
        DnsName nextName = Parse("next.example.");
        DnsRecordData data = NsecData(nextName, 127, 30, 1, 15, 30);
        WireWriter writer = new WireWriter(new byte[1024], 0, 1024);
        CompressionTable table = new CompressionTable(0);
        DnsEncodeOptions options = new DnsEncodeOptions();
        WireWriteResult seed = table.WriteName(writer, nextName, options, true);
        int start = writer.WriteOffset();

        WireWriteResult encoded = DenialRecordDataCodec.EncodeRecordData(
            writer, table, 30, data, options);
        byte[] message = writer.CopyWrittenBytes();
        DnsDecodeResult decoded = DenialRecordDataCodec.DecodeRecordData(
            30, message, start, encoded.BytesWritten, new DnsDecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(seed.Success, Is.True);
            Assert.That(encoded.Success, Is.True);
            Assert.That(message[start] & 192, Is.EqualTo(192));
            Assert.That(encoded.BytesWritten, Is.EqualTo(18));
            Assert.That(Format(decoded.RecordData!.Nsec!.NextDomainName),
                Is.EqualTo("next.example."));
            Assert.That(decoded.RecordData!.Nsec!.RecordTypeValues,
                Is.EqualTo(new[] { 1, 15, 30, 127 }));
        });
    }

    [Test]
    public void NxtRejectsNoncanonicalOrUnsupportedLegacyBitmaps()
    {
        DnsDecodeResult extension = Decode(
            30, new byte[] { 0, 128, 0, 0, 2 });
        DnsDecodeResult trailingZero = Decode(
            30, new byte[] { 0, 0, 0, 0, 2, 0 });
        DnsDecodeResult missingNxt = Decode(
            30, new byte[] { 0, 64, 0, 0, 0 });
        DnsRecordData highType = NsecData(Parse("next.example."), 30, 128);
        WireWriteResult highTypeWrite = EncodeResult(30, highType);

        Assert.Multiple(() =>
        {
            Assert.That(extension.Success, Is.False);
            Assert.That(trailingZero.Success, Is.False);
            Assert.That(missingNxt.Success, Is.False);
            Assert.That(highTypeWrite.Success, Is.False);
        });
    }

    [Test]
    public void NsecRoundTripsSortedMinimalWindowsWithoutNameCompression()
    {
        DnsRecordData data = NsecData(
            Parse("next.example."), 65535, 47, 1, 257, 1234, 47);

        byte[] encoded = Encode(47, data);
        DnsDecodeResult decoded = Decode(47, encoded);

        Assert.Multiple(() =>
        {
            Assert.That(encoded[0] & 192, Is.EqualTo(0));
            Assert.That(Format(decoded.RecordData!.Nsec!.NextDomainName),
                Is.EqualTo("next.example."));
            Assert.That(decoded.RecordData!.Nsec!.RecordTypeValues,
                Is.EqualTo(new[] { 1, 47, 257, 1234, 65535 }));
        });
    }

    [Test]
    public void NsecRejectsCompressedNamesAndNoncanonicalWindows()
    {
        byte[] compressedMessage = [1, 97, 0, 192, 0, 0, 1, 64];
        DnsDecodeResult compressed = DenialRecordDataCodec.DecodeRecordData(
            47, compressedMessage, 3, 5, new DnsDecodeOptions());
        DnsDecodeResult descending = Decode(
            47, new byte[] { 0, 1, 1, 64, 0, 1, 64 });
        DnsDecodeResult trailingZero = Decode(
            47, new byte[] { 0, 0, 2, 64, 0 });
        DnsDecodeResult typeZero = Decode(
            47, new byte[] { 0, 0, 1, 128 });
        DnsDecodeResult empty = Decode(47, new byte[] { 0 });

        Assert.Multiple(() =>
        {
            Assert.That(compressed.Success, Is.False);
            Assert.That(compressed.Diagnostics[0].Field,
                Is.EqualTo("nsec.nextDomainName"));
            Assert.That(descending.Success, Is.False);
            Assert.That(trailingZero.Success, Is.False);
            Assert.That(typeZero.Success, Is.False);
            Assert.That(empty.Success, Is.False);
        });
    }

    [Test]
    public void NsecEncoderRejectsEmptyAndTypeZeroBitmaps()
    {
        DnsRecordData empty = NsecData(Parse("next.example."));
        DnsRecordData typeZero = NsecData(Parse("next.example."), 47, 0);

        WireWriteResult emptyResult = EncodeResult(47, empty);
        WireWriteResult zeroResult = EncodeResult(47, typeZero);

        Assert.Multiple(() =>
        {
            Assert.That(emptyResult.Success, Is.False);
            Assert.That(zeroResult.Success, Is.False);
            Assert.That(zeroResult.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.InvalidTypeBitmap));
        });
    }

    [Test]
    public void Nsec3RoundTripsSaltHashFlagsAndCanonicalWindows()
    {
        DnsRecordData data = new DnsRecordData();
        data.Nsec3 = new DnsNsec3Data();
        data.Nsec3.HashAlgorithm = DnsNsec3HashAlgorithm.Sha1;
        data.Nsec3.Flags = 1;
        data.Nsec3.Iterations = 65535;
        data.Nsec3.Salt = [170, 187, 204, 221];
        data.Nsec3.NextHashedOwnerName = Sequence(20);
        data.Nsec3.RecordTypeValues.Add(1);
        data.Nsec3.RecordTypeValues.Add(46);
        data.Nsec3.RecordTypeValues.Add(257);
        data.Nsec3.RecordTypeValues.Add(65535);

        DnsDecodeResult decoded = Decode(50, Encode(50, data));

        Assert.Multiple(() =>
        {
            Assert.That(decoded.RecordData!.Nsec3!.HashAlgorithm,
                Is.EqualTo(DnsNsec3HashAlgorithm.Sha1));
            Assert.That(decoded.RecordData!.Nsec3!.Flags, Is.EqualTo(1));
            Assert.That(decoded.RecordData!.Nsec3!.Iterations,
                Is.EqualTo(65535));
            Assert.That(decoded.RecordData!.Nsec3!.Salt,
                Is.EqualTo(new byte[] { 170, 187, 204, 221 }));
            Assert.That(decoded.RecordData!.Nsec3!.NextHashedOwnerName,
                Is.EqualTo(Sequence(20)));
            Assert.That(decoded.RecordData!.Nsec3!.RecordTypeValues,
                Is.EqualTo(new[] { 1, 46, 257, 65535 }));
        });
    }

    [Test]
    public void Nsec3AllowsAnEmptyTypeBitmap()
    {
        DnsRecordData data = new DnsRecordData();
        data.Nsec3 = new DnsNsec3Data();
        data.Nsec3.HashAlgorithm = DnsNsec3HashAlgorithm.Sha1;
        data.Nsec3.NextHashedOwnerName = Sequence(20);

        byte[] encoded = Encode(50, data);
        DnsDecodeResult decoded = Decode(50, encoded);

        Assert.Multiple(() =>
        {
            Assert.That(encoded, Has.Length.EqualTo(26));
            Assert.That(decoded.RecordData!.Nsec3!.RecordTypeValues, Is.Empty);
        });
    }

    [Test]
    public void Nsec3PreservesUnknownHashAlgorithmsAndLengths()
    {
        DnsRecordData data = new DnsRecordData();
        data.Nsec3 = new DnsNsec3Data();
        data.Nsec3.HashAlgorithmValue = 250;
        data.Nsec3.Salt = [];
        data.Nsec3.NextHashedOwnerName = [1, 2, 3];

        DnsDecodeResult decoded = Decode(50, Encode(50, data));

        Assert.Multiple(() =>
        {
            Assert.That(decoded.RecordData!.Nsec3!.HashAlgorithmValue,
                Is.EqualTo(250));
            Assert.That(decoded.RecordData!.Nsec3!.NextHashedOwnerName,
                Is.EqualTo(new byte[] { 1, 2, 3 }));
        });
    }

    [Test]
    public void Nsec3RejectsReservedFlagsTruncationAndInvalidHashOrBitmap()
    {
        byte[] reservedFlags = Nsec3Wire(1, 2, [], Sequence(20), []);
        byte[] saltTruncated = [1, 0, 0, 0, 3, 1, 2];
        byte[] zeroHash = [1, 0, 0, 0, 0, 0];
        byte[] shortSha1 = Nsec3Wire(1, 0, [], Sequence(19), []);
        byte[] badBitmap = Nsec3Wire(
            1, 0, [], Sequence(20), [0, 2, 64, 0]);

        Assert.Multiple(() =>
        {
            Assert.That(Decode(50, reservedFlags).Success, Is.False);
            Assert.That(Decode(50, saltTruncated).Success, Is.False);
            Assert.That(Decode(50, zeroHash).Success, Is.False);
            Assert.That(Decode(50, shortSha1).Success, Is.False);
            Assert.That(Decode(50, badBitmap).Success, Is.False);
        });
    }

    [Test]
    public void Nsec3ParamRoundTripsUnknownAlgorithmsAndMaximumSalt()
    {
        DnsRecordData data = new DnsRecordData();
        data.Nsec3Param = new DnsNsec3ParamData();
        data.Nsec3Param.HashAlgorithmValue = 250;
        data.Nsec3Param.Iterations = 65535;
        data.Nsec3Param.Salt = Sequence(255);

        DnsDecodeResult decoded = Decode(51, Encode(51, data));

        Assert.Multiple(() =>
        {
            Assert.That(decoded.RecordData!.Nsec3Param!.HashAlgorithmValue,
                Is.EqualTo(250));
            Assert.That(decoded.RecordData!.Nsec3Param!.Iterations,
                Is.EqualTo(65535));
            Assert.That(decoded.RecordData!.Nsec3Param!.Salt,
                Is.EqualTo(Sequence(255)));
        });
    }

    [Test]
    public void Nsec3ParamRejectsReservedFlagsAndInconsistentSaltLengths()
    {
        DnsDecodeResult flags = Decode(51, new byte[] { 1, 1, 0, 0, 0 });
        DnsDecodeResult shortSalt = Decode(51, new byte[] { 1, 0, 0, 0, 2, 1 });
        DnsDecodeResult trailing = Decode(51, new byte[] { 1, 0, 0, 0, 0, 1 });
        DnsRecordData oversized = new DnsRecordData();
        oversized.Nsec3Param = new DnsNsec3ParamData();
        oversized.Nsec3Param.Salt = new byte[256];
        WireWriteResult oversizedWrite = EncodeResult(51, oversized);

        Assert.Multiple(() =>
        {
            Assert.That(flags.Success, Is.False);
            Assert.That(shortSalt.Success, Is.False);
            Assert.That(trailing.Success, Is.False);
            Assert.That(oversizedWrite.Success, Is.False);
        });
    }

    [Test]
    public void CsyncRoundTripsSerialFlagsAndCanonicalBitmap()
    {
        DnsRecordData data = new DnsRecordData();
        data.Csync = new DnsCsyncData();
        data.Csync.Serial = uint.MaxValue;
        data.Csync.Flags = 3;
        data.Csync.RecordTypeValues.Add(28);
        data.Csync.RecordTypeValues.Add(1);
        data.Csync.RecordTypeValues.Add(2);
        data.Csync.RecordTypeValues.Add(2);

        byte[] encoded = Encode(62, data);
        DnsDecodeResult decoded = Decode(62, encoded);

        Assert.Multiple(() =>
        {
            Assert.That(encoded,
                Is.EqualTo(new byte[]
                {
                    255, 255, 255, 255, 0, 3,
                    0, 4, 96, 0, 0, 8
                }));
            Assert.That(decoded.RecordData!.Csync!.Serial,
                Is.EqualTo(uint.MaxValue));
            Assert.That(decoded.RecordData!.Csync!.Flags, Is.EqualTo(3));
            Assert.That(decoded.RecordData!.Csync!.RecordTypeValues,
                Is.EqualTo(new[] { 1, 2, 28 }));
        });
    }

    [Test]
    public void CsyncRejectsEmptyBitmapReservedFlagsAndTypeZero()
    {
        DnsRecordData emptyData = new DnsRecordData();
        emptyData.Csync = new DnsCsyncData();
        WireWriteResult emptyWrite = EncodeResult(62, emptyData);
        DnsDecodeResult empty = Decode(
            62, new byte[] { 0, 0, 0, 0, 0, 0 });
        DnsDecodeResult flags = Decode(
            62, new byte[] { 0, 0, 0, 1, 0, 4 });
        DnsDecodeResult typeZero = Decode(
            62, new byte[] { 0, 0, 0, 1, 0, 0, 0, 1, 128 });

        Assert.Multiple(() =>
        {
            Assert.That(emptyWrite.Success, Is.False);
            Assert.That(empty.Success, Is.False);
            Assert.That(flags.Success, Is.False);
            Assert.That(typeZero.Success, Is.False);
        });
    }

    [Test]
    public void CodecRejectsUnsupportedTypesAndMissingSchemas()
    {
        DnsDecodeResult unsupported = Decode(48, []);
        WireWriteResult unsupportedWrite = EncodeResult(48, new DnsRecordData());
        WireWriteResult missingNsec = EncodeResult(47, new DnsRecordData());

        Assert.Multiple(() =>
        {
            Assert.That(unsupported.Success, Is.False);
            Assert.That(unsupportedWrite.Success, Is.False);
            Assert.That(missingNsec.Success, Is.False);
            Assert.That(missingNsec.Diagnostics[0].Field, Is.EqualTo("nsec"));
        });
    }

    static DnsRecordData NsecData(DnsName nextName, params int[] recordTypes)
    {
        DnsRecordData data = new DnsRecordData();
        data.Nsec = new DnsNsecData();
        data.Nsec.NextDomainName = nextName;
        int index = 0;
        while (index < recordTypes.Length)
        {
            data.Nsec.RecordTypeValues.Add(recordTypes[index]);
            index = index + 1;
        }

        return data;
    }

    static byte[] Nsec3Wire(
        int algorithm,
        int flags,
        byte[] salt,
        byte[] nextHash,
        byte[] bitmap)
    {
        byte[] bytes = new byte[6 + salt.Length + nextHash.Length + bitmap.Length];
        bytes[0] = (byte)algorithm;
        bytes[1] = (byte)flags;
        bytes[4] = (byte)salt.Length;
        System.Array.Copy(salt, 0, bytes, 5, salt.Length);
        int hashLengthOffset = 5 + salt.Length;
        bytes[hashLengthOffset] = (byte)nextHash.Length;
        System.Array.Copy(
            nextHash, 0, bytes, hashLengthOffset + 1, nextHash.Length);
        System.Array.Copy(
            bitmap, 0, bytes, hashLengthOffset + 1 + nextHash.Length,
            bitmap.Length);
        return bytes;
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
        return DenialRecordDataCodec.DecodeRecordData(
            recordTypeValue,
            bytes,
            0,
            bytes.Length,
            new DnsDecodeOptions());
    }

    static byte[] Encode(int recordTypeValue, DnsRecordData data)
    {
        WireWriter writer = new WireWriter(new byte[4096], 0, 4096);
        WireWriteResult result = DenialRecordDataCodec.EncodeRecordData(
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
        return DenialRecordDataCodec.EncodeRecordData(
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
