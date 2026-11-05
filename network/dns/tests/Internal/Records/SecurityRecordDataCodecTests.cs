namespace dns.Tests.Internal.Records;

public class SecurityRecordDataCodecTests
{
    [Test]
    public void SigRoundTripsAndMayCompressItsSignerName()
    {
        DnsName signerName = Parse("signer.example.");
        DnsRecordData data = SignatureData(signerName);
        WireWriter writer = new WireWriter(new byte[2048], 0, 2048);
        CompressionTable table = new CompressionTable(0);
        DnsEncodeOptions options = new DnsEncodeOptions();
        WireWriteResult seed = table.WriteName(writer, signerName, options, true);
        int start = writer.WriteOffset();

        WireWriteResult encoded = SecurityRecordDataCodec.EncodeRecordData(
            writer, table, 24, data, options);
        byte[] message = writer.CopyWrittenBytes();
        DnsDecodeResult decoded = SecurityRecordDataCodec.DecodeRecordData(
            24, message, start, encoded.BytesWritten, new DnsDecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(seed.Success, Is.True);
            Assert.That(encoded.Success, Is.True);
            Assert.That(message[start + 18] & 192, Is.EqualTo(192));
            Assert.That(decoded.RecordData!.Signature!.TypeCoveredValue,
                Is.EqualTo(257));
            Assert.That(decoded.RecordData!.Signature!.AlgorithmValue,
                Is.EqualTo(250));
            Assert.That(Format(decoded.RecordData!.Signature!.SignerName),
                Is.EqualTo("signer.example."));
            Assert.That(decoded.RecordData!.Signature!.Signature,
                Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
        });
    }

    [Test]
    public void RrsigRoundTripsFullWidthFieldsWithoutNameCompression()
    {
        DnsRecordData data = SignatureData(Parse("zone.example."));
        data.Signature!.Labels = 255;
        data.Signature.OriginalTimeToLive = uint.MaxValue;
        data.Signature.SignatureExpiration = 0xffffffff;
        data.Signature.SignatureInception = 0x80000000;
        data.Signature.KeyTag = 65535;

        byte[] encoded = Encode(46, data);
        DnsDecodeResult decoded = Decode(46, encoded);

        Assert.Multiple(() =>
        {
            Assert.That(encoded[18] & 192, Is.EqualTo(0));
            Assert.That(decoded.RecordData!.Signature!.Labels, Is.EqualTo(255));
            Assert.That(decoded.RecordData!.Signature!.OriginalTimeToLive,
                Is.EqualTo(uint.MaxValue));
            Assert.That(decoded.RecordData!.Signature!.SignatureExpiration,
                Is.EqualTo(0xffffffff));
            Assert.That(decoded.RecordData!.Signature!.SignatureInception,
                Is.EqualTo(0x80000000));
            Assert.That(decoded.RecordData!.Signature!.KeyTag, Is.EqualTo(65535));
        });
    }

    [Test]
    public void RrsigRejectsCompressedSignerNames()
    {
        byte[] message = new byte[24];
        message[0] = 1;
        message[1] = 97;
        message[2] = 0;
        message[21] = 192;
        message[22] = 0;
        message[23] = 1;

        DnsDecodeResult decoded = SecurityRecordDataCodec.DecodeRecordData(
            46, message, 3, 21, new DnsDecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.False);
            Assert.That(decoded.Diagnostics[0].Field,
                Is.EqualTo("rrsig.signerName"));
        });
    }

    [TestCase(25)]
    [TestCase(48)]
    [TestCase(60)]
    public void KeyFamiliesRoundTripUnknownAlgorithms(int recordTypeValue)
    {
        DnsRecordData data = new DnsRecordData();
        data.Key = new DnsKeyData();
        data.Key.Flags = 65535;
        data.Key.Protocol = 255;
        data.Key.AlgorithmValue = 250;
        data.Key.PublicKey = [0, 1, 2, 255];

        DnsDecodeResult decoded = Decode(recordTypeValue, Encode(recordTypeValue, data));

        Assert.Multiple(() =>
        {
            Assert.That(decoded.RecordData!.Key!.Flags, Is.EqualTo(65535));
            Assert.That(decoded.RecordData!.Key!.Protocol, Is.EqualTo(255));
            Assert.That(decoded.RecordData!.Key!.AlgorithmValue, Is.EqualTo(250));
            Assert.That(decoded.RecordData!.Key!.PublicKey,
                Is.EqualTo(new byte[] { 0, 1, 2, 255 }));
        });
    }

    [TestCase(43)]
    [TestCase(59)]
    public void DelegationDigestFamiliesRoundTripKnownDigestWidths(
        int recordTypeValue)
    {
        DnsRecordData data = new DnsRecordData();
        data.Ds = new DnsDsData();
        data.Ds.KeyTag = 65535;
        data.Ds.Algorithm = DnsDnssecAlgorithm.EcdsaP384Sha384;
        data.Ds.DigestAlgorithm = DnsDigestAlgorithm.Sha384;
        data.Ds.Digest = Sequence(48);

        DnsDecodeResult decoded = Decode(recordTypeValue, Encode(recordTypeValue, data));

        Assert.Multiple(() =>
        {
            Assert.That(decoded.RecordData!.Ds!.KeyTag, Is.EqualTo(65535));
            Assert.That(decoded.RecordData!.Ds!.Algorithm,
                Is.EqualTo(DnsDnssecAlgorithm.EcdsaP384Sha384));
            Assert.That(decoded.RecordData!.Ds!.DigestAlgorithm,
                Is.EqualTo(DnsDigestAlgorithm.Sha384));
            Assert.That(decoded.RecordData!.Ds!.Digest, Is.EqualTo(Sequence(48)));
        });
    }

    [Test]
    public void DelegationDigestRejectsWrongKnownDigestLength()
    {
        byte[] shortSha256 = new byte[4 + 31];
        shortSha256[3] = 2;
        DnsDecodeResult decoded = Decode(43, shortSha256);
        DnsRecordData data = new DnsRecordData();
        data.Ds = new DnsDsData();
        data.Ds.DigestAlgorithm = DnsDigestAlgorithm.Sha1;
        data.Ds.Digest = new byte[19];
        WireWriteResult encoded = EncodeResult(43, data);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.False);
            Assert.That(decoded.Diagnostics[0].Field, Is.EqualTo("ds.digest"));
            Assert.That(encoded.Success, Is.False);
        });
    }

    [Test]
    public void SshfpRoundTripsSha256AndRejectsWrongFingerprintLength()
    {
        DnsRecordData data = new DnsRecordData();
        data.Sshfp = new DnsSshfpData();
        data.Sshfp.Algorithm = 4;
        data.Sshfp.FingerprintType = 2;
        data.Sshfp.Fingerprint = Sequence(32);

        DnsDecodeResult decoded = Decode(44, Encode(44, data));
        DnsDecodeResult invalid = Decode(44, new byte[] { 4, 1, 1, 2 });

        Assert.Multiple(() =>
        {
            Assert.That(decoded.RecordData!.Sshfp!.Algorithm, Is.EqualTo(4));
            Assert.That(decoded.RecordData!.Sshfp!.FingerprintType, Is.EqualTo(2));
            Assert.That(decoded.RecordData!.Sshfp!.Fingerprint,
                Is.EqualTo(Sequence(32)));
            Assert.That(invalid.Success, Is.False);
        });
    }

    [Test]
    public void IpsecKeyRoundTripsEveryGatewayAddressVariant()
    {
        DnsDecodeResult none = Decode(45, Encode(45, IpsecAddressData(0, [])));
        DnsDecodeResult ipv4 = Decode(
            45, Encode(45, IpsecAddressData(1, [192, 0, 2, 1])));
        DnsDecodeResult ipv6 = Decode(
            45, Encode(45, IpsecAddressData(2, Sequence(16))));

        Assert.Multiple(() =>
        {
            Assert.That(none.RecordData!.IpsecKey!.GatewayAddress, Is.Empty);
            Assert.That(ipv4.RecordData!.IpsecKey!.GatewayAddress,
                Is.EqualTo(new byte[] { 192, 0, 2, 1 }));
            Assert.That(ipv6.RecordData!.IpsecKey!.GatewayAddress,
                Is.EqualTo(Sequence(16)));
        });
    }

    [Test]
    public void IpsecKeyRoundTripsUncompressedDomainGateway()
    {
        DnsRecordData data = new DnsRecordData();
        data.IpsecKey = new DnsIpsecKeyData();
        data.IpsecKey.Precedence = 255;
        data.IpsecKey.GatewayType = 3;
        data.IpsecKey.Algorithm = 2;
        data.IpsecKey.GatewayName = Parse("gateway.example.");
        data.IpsecKey.PublicKey = [9, 8, 7];

        byte[] encoded = Encode(45, data);
        DnsDecodeResult decoded = Decode(45, encoded);

        Assert.Multiple(() =>
        {
            Assert.That(encoded[3] & 192, Is.EqualTo(0));
            Assert.That(Format(decoded.RecordData!.IpsecKey!.GatewayName!),
                Is.EqualTo("gateway.example."));
            Assert.That(decoded.RecordData!.IpsecKey!.PublicKey,
                Is.EqualTo(new byte[] { 9, 8, 7 }));
        });
    }

    [Test]
    public void IpsecKeyRejectsCompressedGatewayAndInconsistentKeyAlgorithm()
    {
        byte[] compressed = [1, 97, 0, 1, 3, 2, 192, 0, 9];
        DnsDecodeResult compressedGateway =
            SecurityRecordDataCodec.DecodeRecordData(
                45, compressed, 3, 6, new DnsDecodeOptions());
        DnsDecodeResult missingKey = Decode(45, new byte[] { 0, 0, 2 });
        DnsDecodeResult unexpectedKey = Decode(45, new byte[] { 0, 0, 0, 1 });

        Assert.Multiple(() =>
        {
            Assert.That(compressedGateway.Success, Is.False);
            Assert.That(compressedGateway.Diagnostics[0].Field,
                Is.EqualTo("ipsecKey.gateway"));
            Assert.That(missingKey.Success, Is.False);
            Assert.That(unexpectedKey.Success, Is.False);
        });
    }

    [Test]
    public void DhcidRoundTripsSha256AndRejectsWrongDigestLength()
    {
        DnsRecordData data = new DnsRecordData();
        data.Dhcid = new DnsDhcidData();
        data.Dhcid.IdentifierType = 65535;
        data.Dhcid.DigestType = 1;
        data.Dhcid.Digest = Sequence(32);

        DnsDecodeResult decoded = Decode(49, Encode(49, data));
        DnsDecodeResult invalid = Decode(49, new byte[] { 0, 1, 1, 1 });

        Assert.Multiple(() =>
        {
            Assert.That(decoded.RecordData!.Dhcid!.IdentifierType,
                Is.EqualTo(65535));
            Assert.That(decoded.RecordData!.Dhcid!.Digest, Is.EqualTo(Sequence(32)));
            Assert.That(invalid.Success, Is.False);
        });
    }

    [TestCase(52, 1, 32)]
    [TestCase(53, 2, 64)]
    public void CertificateAssociationFamiliesRoundTripKnownMatchingTypes(
        int recordTypeValue,
        int matchingType,
        int length)
    {
        DnsRecordData data = new DnsRecordData();
        data.Tlsa = new DnsTlsaData();
        data.Tlsa.CertificateUsage = 255;
        data.Tlsa.Selector = 254;
        data.Tlsa.MatchingType = matchingType;
        data.Tlsa.AssociationData = Sequence(length);

        DnsDecodeResult decoded = Decode(recordTypeValue, Encode(recordTypeValue, data));

        Assert.Multiple(() =>
        {
            Assert.That(decoded.RecordData!.Tlsa!.CertificateUsage, Is.EqualTo(255));
            Assert.That(decoded.RecordData!.Tlsa!.Selector, Is.EqualTo(254));
            Assert.That(decoded.RecordData!.Tlsa!.MatchingType,
                Is.EqualTo(matchingType));
            Assert.That(decoded.RecordData!.Tlsa!.AssociationData,
                Is.EqualTo(Sequence(length)));
        });
    }

    [Test]
    public void CertificateAssociationRejectsEmptyOrWrongHashLengths()
    {
        DnsDecodeResult empty = Decode(52, new byte[] { 3, 1, 0 });
        byte[] shortSha512 = new byte[3 + 63];
        shortSha512[2] = 2;
        DnsDecodeResult shortHash = Decode(53, shortSha512);

        Assert.Multiple(() =>
        {
            Assert.That(empty.Success, Is.False);
            Assert.That(shortHash.Success, Is.False);
        });
    }

    [Test]
    public void OpenPgpKeyRoundTripsTransferableKeyBytesAndRejectsEmptyData()
    {
        DnsRecordData data = new DnsRecordData();
        data.FixedBytes = new DnsFixedBytesData();
        data.FixedBytes.Bytes = [153, 1, 2, 3, 4];

        DnsDecodeResult decoded = Decode(61, Encode(61, data));
        DnsDecodeResult empty = Decode(61, []);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.RecordData!.FixedBytes!.Bytes,
                Is.EqualTo(new byte[] { 153, 1, 2, 3, 4 }));
            Assert.That(empty.Success, Is.False);
        });
    }

    [Test]
    public void HipRoundTripsMultipleUncompressedRendezvousServers()
    {
        DnsRecordData data = new DnsRecordData();
        data.Hip = new DnsHipData();
        data.Hip.PublicKeyAlgorithm = 2;
        data.Hip.HostIdentityTag = Sequence(16);
        data.Hip.PublicKey = Sequence(300);
        data.Hip.RendezvousServers.Add(Parse("first.example."));
        data.Hip.RendezvousServers.Add(Parse("second.example."));

        byte[] encoded = Encode(55, data);
        DnsDecodeResult decoded = Decode(55, encoded);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.RecordData!.Hip!.HostIdentityTag,
                Is.EqualTo(Sequence(16)));
            Assert.That(decoded.RecordData!.Hip!.PublicKey,
                Is.EqualTo(Sequence(300)));
            Assert.That(decoded.RecordData!.Hip!.RendezvousServers,
                Has.Count.EqualTo(2));
            Assert.That(Format(decoded.RecordData!.Hip!.RendezvousServers[0]),
                Is.EqualTo("first.example."));
            Assert.That(Format(decoded.RecordData!.Hip!.RendezvousServers[1]),
                Is.EqualTo("second.example."));
        });
    }

    [Test]
    public void HipRejectsTruncatedFieldsAndCompressedRendezvousNames()
    {
        byte[] compressed = [1, 97, 0, 1, 1, 0, 1, 7, 8, 192, 0];
        DnsDecodeResult compressedServer =
            SecurityRecordDataCodec.DecodeRecordData(
                55, compressed, 3, 8, new DnsDecodeOptions());
        DnsDecodeResult truncated = Decode(55, new byte[] { 2, 1, 0, 3, 7 });

        Assert.Multiple(() =>
        {
            Assert.That(compressedServer.Success, Is.False);
            Assert.That(compressedServer.Diagnostics[0].Field,
                Is.EqualTo("hip.rendezvousServer"));
            Assert.That(truncated.Success, Is.False);
        });
    }

    [Test]
    public void CodecRejectsUnsupportedTypesAndMissingSchemas()
    {
        DnsDecodeResult unsupported = Decode(50, []);
        WireWriteResult unsupportedWrite = EncodeResult(50, new DnsRecordData());
        WireWriteResult missingSignature = EncodeResult(46, new DnsRecordData());

        Assert.Multiple(() =>
        {
            Assert.That(unsupported.Success, Is.False);
            Assert.That(unsupportedWrite.Success, Is.False);
            Assert.That(missingSignature.Success, Is.False);
            Assert.That(missingSignature.Diagnostics[0].Field,
                Is.EqualTo("signature"));
        });
    }

    static DnsRecordData SignatureData(DnsName signerName)
    {
        DnsRecordData data = new DnsRecordData();
        data.Signature = new DnsSignatureData();
        data.Signature.TypeCoveredValue = 257;
        data.Signature.AlgorithmValue = 250;
        data.Signature.Labels = 2;
        data.Signature.OriginalTimeToLive = 3600;
        data.Signature.SignatureExpiration = 2000000000;
        data.Signature.SignatureInception = 1000000000;
        data.Signature.KeyTag = 12345;
        data.Signature.SignerName = signerName;
        data.Signature.Signature = [1, 2, 3, 4];
        return data;
    }

    static DnsRecordData IpsecAddressData(int gatewayType, byte[] address)
    {
        DnsRecordData data = new DnsRecordData();
        data.IpsecKey = new DnsIpsecKeyData();
        data.IpsecKey.Precedence = 10;
        data.IpsecKey.GatewayType = gatewayType;
        data.IpsecKey.Algorithm = 2;
        data.IpsecKey.GatewayAddress = address;
        data.IpsecKey.PublicKey = [1];
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
        return SecurityRecordDataCodec.DecodeRecordData(
            recordTypeValue,
            bytes,
            0,
            bytes.Length,
            new DnsDecodeOptions());
    }

    static byte[] Encode(int recordTypeValue, DnsRecordData data)
    {
        WireWriter writer = new WireWriter(new byte[4096], 0, 4096);
        WireWriteResult result = SecurityRecordDataCodec.EncodeRecordData(
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
        return SecurityRecordDataCodec.EncodeRecordData(
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
