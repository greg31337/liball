namespace dns.Tests.Internal.Records;

public class TypedEdnsOptionCodecTests
{
    [Test]
    public void UpdateLeaseRoundTripsFourAndEightOctetVariants()
    {
        DnsEdnsOption four = Decode([0, 2, 0, 4, 1, 2, 3, 4]);
        DnsEdnsOption eight = Option(DnsEdnsOptionCode.UpdateLease);
        eight.HasUpdateLease = true;
        eight.UpdateLeaseSeconds = 0x01020304;
        eight.HasUpdateKeyLease = true;
        eight.UpdateKeyLeaseSeconds = 0xA0B0C0D0;

        DnsEdnsOptionResult encoded = Encode(eight);
        DnsEdnsOption decoded = Decode(encoded.Bytes);

        Assert.Multiple(() =>
        {
            Assert.That(four.HasUpdateLease, Is.True);
            Assert.That(four.UpdateLeaseSeconds, Is.EqualTo(0x01020304));
            Assert.That(four.HasUpdateKeyLease, Is.False);
            Assert.That(encoded.Bytes, Is.EqualTo(new byte[]
            {
                0, 2, 0, 8, 1, 2, 3, 4, 160, 176, 192, 208
            }));
            Assert.That(decoded.HasUpdateLease, Is.True);
            Assert.That(decoded.UpdateLeaseSeconds, Is.EqualTo(0x01020304));
            Assert.That(decoded.HasUpdateKeyLease, Is.True);
            Assert.That(decoded.UpdateKeyLeaseSeconds,
                Is.EqualTo(0xA0B0C0D0));
            Assert.That(decoded.UseRawData, Is.False);
        });
    }

    [Test]
    public void NsidAndAlgorithmListsRoundTripExactOctets()
    {
        DnsEdnsOption nsid = Option(DnsEdnsOptionCode.NameServerIdentifier);
        nsid.NameServerIdentifier = [0, 255, 65];
        DnsEdnsOption dau = Option(
            DnsEdnsOptionCode.DnssecAlgorithmUnderstood);
        dau.DnssecAlgorithmValues.Add(8);
        dau.DnssecAlgorithmValues.Add(253);
        DnsEdnsOption dhu = Option(DnsEdnsOptionCode.DsHashUnderstood);
        dhu.DigestAlgorithmValues.Add(2);
        DnsEdnsOption n3u = Option(
            DnsEdnsOptionCode.Nsec3HashUnderstood);
        n3u.Nsec3HashAlgorithmValues.Add(1);
        n3u.Nsec3HashAlgorithmValues.Add(255);

        DnsEdnsOption decodedNsid = Decode(Encode(nsid).Bytes);
        DnsEdnsOption decodedDau = Decode(Encode(dau).Bytes);
        DnsEdnsOption decodedDhu = Decode(Encode(dhu).Bytes);
        DnsEdnsOption decodedN3u = Decode(Encode(n3u).Bytes);

        Assert.Multiple(() =>
        {
            Assert.That(decodedNsid.NameServerIdentifier,
                Is.EqualTo(new byte[] { 0, 255, 65 }));
            Assert.That(decodedDau.DnssecAlgorithmValues,
                Is.EqualTo(new[] { 8, 253 }));
            Assert.That(decodedDhu.DigestAlgorithmValues,
                Is.EqualTo(new[] { 2 }));
            Assert.That(decodedN3u.Nsec3HashAlgorithmValues,
                Is.EqualTo(new[] { 1, 255 }));
        });
    }

    [Test]
    public void ExpireKeepaliveAndPaddingSupportRequestAndValueForms()
    {
        DnsEdnsOption expireRequest = Option(DnsEdnsOptionCode.Expire);
        DnsEdnsOption expireResponse = Option(DnsEdnsOptionCode.Expire);
        expireResponse.HasExpire = true;
        expireResponse.ExpireSeconds = 3600;
        DnsEdnsOption keepaliveRequest = Option(
            DnsEdnsOptionCode.TcpKeepalive);
        DnsEdnsOption keepaliveResponse = Option(
            DnsEdnsOptionCode.TcpKeepalive);
        keepaliveResponse.Keepalive = new DnsEdnsKeepalive();
        keepaliveResponse.Keepalive.HasTimeout = true;
        keepaliveResponse.Keepalive.TimeoutUnits = 250;
        DnsEdnsOption padding = Option(DnsEdnsOptionCode.Padding);
        padding.Padding = [0, 255, 0];

        DnsEdnsOption decodedExpireRequest =
            Decode(Encode(expireRequest).Bytes);
        DnsEdnsOption decodedExpireResponse =
            Decode(Encode(expireResponse).Bytes);
        DnsEdnsOption decodedKeepaliveRequest =
            Decode(Encode(keepaliveRequest).Bytes);
        DnsEdnsOption decodedKeepaliveResponse =
            Decode(Encode(keepaliveResponse).Bytes);
        DnsEdnsOption decodedPadding = Decode(Encode(padding).Bytes);

        Assert.Multiple(() =>
        {
            Assert.That(decodedExpireRequest.HasExpire, Is.False);
            Assert.That(decodedExpireResponse.HasExpire, Is.True);
            Assert.That(decodedExpireResponse.ExpireSeconds, Is.EqualTo(3600));
            Assert.That(decodedKeepaliveRequest.Keepalive, Is.Not.Null);
            Assert.That(decodedKeepaliveRequest.Keepalive!.HasTimeout,
                Is.False);
            Assert.That(decodedKeepaliveResponse.Keepalive!.HasTimeout,
                Is.True);
            Assert.That(decodedKeepaliveResponse.Keepalive.TimeoutUnits,
                Is.EqualTo(250));
            Assert.That(decodedPadding.Padding,
                Is.EqualTo(new byte[] { 0, 255, 0 }));
        });
    }

    [Test]
    public void ChainDistinguishesDiscoveryRootAndUncompressedNames()
    {
        DnsEdnsOption discovery = Option(DnsEdnsOptionCode.Chain);
        DnsEdnsOption root = Option(DnsEdnsOptionCode.Chain);
        root.ChainName = ParseName(".");
        DnsEdnsOption named = Option(DnsEdnsOptionCode.Chain);
        named.ChainName = ParseName("com.");

        DnsEdnsOptionResult discoveryWire = Encode(discovery);
        DnsEdnsOptionResult rootWire = Encode(root);
        DnsEdnsOptionResult namedWire = Encode(named);
        DnsEdnsOption decodedDiscovery = Decode(discoveryWire.Bytes);
        DnsEdnsOption decodedRoot = Decode(rootWire.Bytes);
        DnsEdnsOption decodedName = Decode(namedWire.Bytes);

        Assert.Multiple(() =>
        {
            Assert.That(discoveryWire.Bytes,
                Is.EqualTo(new byte[] { 0, 13, 0, 0 }));
            Assert.That(rootWire.Bytes,
                Is.EqualTo(new byte[] { 0, 13, 0, 1, 0 }));
            Assert.That(namedWire.Bytes,
                Is.EqualTo(new byte[] { 0, 13, 0, 5, 3, 99, 111, 109, 0 }));
            Assert.That(decodedDiscovery.ChainName, Is.Null);
            Assert.That(FormatName(decodedRoot.ChainName!), Is.EqualTo("."));
            Assert.That(FormatName(decodedName.ChainName!), Is.EqualTo("com."));
        });
    }

    [Test]
    public void KeyTagsRoundTripOneOrMoreUnsignedValues()
    {
        DnsEdnsOption keyTags = Option(DnsEdnsOptionCode.KeyTag);
        keyTags.KeyTagValues.Add(1);
        keyTags.KeyTagValues.Add(65535);

        DnsEdnsOptionResult encoded = Encode(keyTags);
        DnsEdnsOption decoded = Decode(encoded.Bytes);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Bytes,
                Is.EqualTo(new byte[] { 0, 14, 0, 4, 0, 1, 255, 255 }));
            Assert.That(decoded.KeyTagValues,
                Is.EqualTo(new[] { 1, 65535 }));
        });
    }

    [Test]
    public void RejectsMalformedTypedLengthsAndCanFallBackToOpaque()
    {
        byte[][] malformed =
        [
            [0, 2, 0, 0],
            [0, 9, 0, 2, 0, 1],
            [0, 11, 0, 1, 0],
            [0, 13, 0, 2, 192, 0],
            [0, 14, 0, 0]
        ];
        IDnsRecordDataCodec codec = new DnsRecordDataCodec();

        int index = 0;
        while (index < malformed.Length)
        {
            DnsEdnsOptionResult rejected = codec.TryDecodeEdnsOption(
                malformed[index], new DnsDecodeOptions());
            DnsDecodeOptions fallbackOptions = new DnsDecodeOptions();
            fallbackOptions.FallBackToOpaqueRecordData = true;
            DnsEdnsOptionResult preserved = codec.TryDecodeEdnsOption(
                malformed[index], fallbackOptions);

            Assert.Multiple(() =>
            {
                Assert.That(rejected.Success, Is.False);
                Assert.That(rejected.Diagnostics[0].Code,
                    Is.EqualTo(DnsDiagnosticCode.InvalidEdns));
                Assert.That(preserved.Success, Is.True);
                Assert.That(preserved.Option!.UseRawData, Is.True);
            });
            index = index + 1;
        }
    }

    [Test]
    public void RejectsTypedValuesOutsideTheirFieldRanges()
    {
        DnsEdnsOption update = Option(DnsEdnsOptionCode.UpdateLease);
        DnsEdnsOption algorithms = Option(
            DnsEdnsOptionCode.DnssecAlgorithmUnderstood);
        algorithms.DnssecAlgorithmValues.Add(256);
        DnsEdnsOption keepalive = Option(DnsEdnsOptionCode.TcpKeepalive);
        keepalive.Keepalive = new DnsEdnsKeepalive();
        keepalive.Keepalive.HasTimeout = true;
        keepalive.Keepalive.TimeoutUnits = -1;
        DnsEdnsOption relativeChain = Option(DnsEdnsOptionCode.Chain);
        relativeChain.ChainName = ParseName("relative");
        DnsEdnsOption emptyKeyTags = Option(DnsEdnsOptionCode.KeyTag);

        Assert.Multiple(() =>
        {
            Assert.That(Encode(update).Success, Is.False);
            Assert.That(Encode(algorithms).Success, Is.False);
            Assert.That(Encode(keepalive).Success, Is.False);
            Assert.That(Encode(relativeChain).Success, Is.False);
            Assert.That(Encode(emptyKeyTags).Success, Is.False);
        });
    }

    [Test]
    public void MessageOptFramingUsesTypedOptionCodecs()
    {
        DnsEdnsOption update = Option(DnsEdnsOptionCode.UpdateLease);
        update.HasUpdateLease = true;
        update.UpdateLeaseSeconds = 7200;
        update.HasUpdateKeyLease = true;
        update.UpdateKeyLeaseSeconds = 14400;
        DnsEdnsOption chain = Option(DnsEdnsOptionCode.Chain);
        chain.ChainName = ParseName("example.");
        DnsEdnsOption tags = Option(DnsEdnsOptionCode.KeyTag);
        tags.KeyTagValues.Add(12345);
        DnsMessage message = Message(update, chain, tags);

        DnsEncodeResult encoded = new DnsMessageCodec().TryEncodeMessage(
            message, new DnsEncodeOptions());
        DnsDecodeResult decoded = new DnsMessageCodec().TryDecodeMessage(
            encoded.Bytes, new DnsDecodeOptions());
        DnsEdnsData edns =
            decoded.Message!.Additionals[0].Data!.Edns!;
        DnsTextResult text = new DnsTextCodec().FormatMessageText(
            decoded.Message, new DnsTextOptions());

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(decoded.Success, Is.True);
            Assert.That(edns.Options, Has.Count.EqualTo(3));
            Assert.That(edns.Options[0].UpdateLeaseSeconds,
                Is.EqualTo(7200));
            Assert.That(edns.Options[0].UpdateKeyLeaseSeconds,
                Is.EqualTo(14400));
            Assert.That(FormatName(edns.Options[1].ChainName!),
                Is.EqualTo("example."));
            Assert.That(edns.Options[2].KeyTagValues,
                Is.EqualTo(new[] { 12345 }));
            Assert.That(text.Text,
                Does.Contain("7200 seconds; key 14400 seconds"));
        });
    }

    static DnsEdnsOption Option(DnsEdnsOptionCode code)
    {
        DnsEdnsOption option = new DnsEdnsOption();
        option.Code = code;
        option.CodeValue = (int)code;
        return option;
    }

    static DnsEdnsOptionResult Encode(DnsEdnsOption option)
    {
        return new DnsRecordDataCodec().TryEncodeEdnsOption(option);
    }

    static DnsEdnsOption Decode(byte[] bytes)
    {
        return new DnsRecordDataCodec().DecodeEdnsOption(
            bytes, new DnsDecodeOptions());
    }

    static DnsMessage Message(
        DnsEdnsOption first,
        DnsEdnsOption second,
        DnsEdnsOption third)
    {
        DnsEdnsData edns = new DnsEdnsData();
        edns.Options.Add(first);
        edns.Options.Add(second);
        edns.Options.Add(third);
        DnsRecordData data = new DnsRecordData();
        data.Edns = edns;
        DnsResourceRecord opt = new DnsResourceRecord();
        opt.Name = ParseName(".");
        opt.RecordType = DnsRecordType.Opt;
        opt.RecordTypeValue = 41;
        opt.Data = data;
        DnsMessage message = new DnsMessage();
        message.Additionals.Add(opt);
        return message;
    }

    static DnsName ParseName(string text)
    {
        return new DnsNameService().ParseName(text, null);
    }

    static string FormatName(DnsName name)
    {
        return new DnsNameService().FormatName(
            name, new DnsTextOptions());
    }
}
