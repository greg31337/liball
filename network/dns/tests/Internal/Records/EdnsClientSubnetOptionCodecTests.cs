namespace dns.Tests.Internal.Records;

public class EdnsClientSubnetOptionCodecTests
{
    [Test]
    public void DecodesCanonicalIpv4AndIpv6Prefixes()
    {
        DnsEdnsOption ipv4 = Decode(
            [0, 8, 0, 7, 0, 1, 24, 0, 192, 0, 2]);
        DnsEdnsOption ipv6 = Decode(
            [0, 8, 0, 11, 0, 2, 56, 64,
                32, 1, 13, 184, 171, 205, 238]);

        Assert.Multiple(() =>
        {
            Assert.That(ipv4.UseRawData, Is.False);
            Assert.That(ipv4.ClientSubnet, Is.Not.Null);
            Assert.That(ipv4.ClientSubnet!.AddressFamily, Is.EqualTo(1));
            Assert.That(ipv4.ClientSubnet.SourcePrefixLength,
                Is.EqualTo(24));
            Assert.That(ipv4.ClientSubnet.ScopePrefixLength, Is.Zero);
            Assert.That(ipv4.ClientSubnet.Address,
                Is.EqualTo(new byte[] { 192, 0, 2 }));
            Assert.That(ipv6.ClientSubnet, Is.Not.Null);
            Assert.That(ipv6.ClientSubnet!.AddressFamily, Is.EqualTo(2));
            Assert.That(ipv6.ClientSubnet.SourcePrefixLength,
                Is.EqualTo(56));
            Assert.That(ipv6.ClientSubnet.ScopePrefixLength,
                Is.EqualTo(64));
            Assert.That(ipv6.ClientSubnet.Address,
                Is.EqualTo(new byte[] { 32, 1, 13, 184, 171, 205, 238 }));
        });
    }

    [Test]
    public void EncodingCanonicalizesCompleteAddressesToMaskedPrefixes()
    {
        DnsEdnsOption ipv4 = SubnetOption(
            1, 25, 0, [192, 0, 2, 255]);
        DnsEdnsOption ipv6 = SubnetOption(
            2,
            57,
            64,
            [32, 1, 13, 184, 171, 205, 238, 255,
                1, 2, 3, 4, 5, 6, 7, 8]);

        DnsEdnsOptionResult encodedIpv4 = Encode(ipv4);
        DnsEdnsOptionResult encodedIpv6 = Encode(ipv6);

        Assert.Multiple(() =>
        {
            Assert.That(encodedIpv4.Success, Is.True);
            Assert.That(encodedIpv4.Bytes,
                Is.EqualTo(new byte[]
                {
                    0, 8, 0, 8, 0, 1, 25, 0, 192, 0, 2, 128
                }));
            Assert.That(ipv4.ClientSubnet!.Address[3], Is.EqualTo(255));
            Assert.That(encodedIpv6.Success, Is.True);
            Assert.That(encodedIpv6.Bytes,
                Is.EqualTo(new byte[]
                {
                    0, 8, 0, 12, 0, 2, 57, 64,
                    32, 1, 13, 184, 171, 205, 238, 128
                }));
        });
    }

    [Test]
    public void ZeroSourcePrefixOmitsAddressBytes()
    {
        DnsEdnsOption option = SubnetOption(
            1, 0, 0, [203, 0, 113, 44]);

        DnsEdnsOptionResult encoded = Encode(option);
        DnsEdnsOption decoded = Decode(encoded.Bytes);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Bytes,
                Is.EqualTo(new byte[] { 0, 8, 0, 4, 0, 1, 0, 0 }));
            Assert.That(decoded.ClientSubnet!.Address, Is.Empty);
        });
    }

    [Test]
    public void RejectsMalformedFamiliesPrefixesLengthsAndHostBits()
    {
        byte[][] malformed =
        [
            [0, 8, 0, 4, 0, 3, 0, 0],
            [0, 8, 0, 9, 0, 1, 33, 0, 192, 0, 2, 0, 0],
            [0, 8, 0, 4, 0, 1, 0, 33],
            [0, 8, 0, 3, 0, 1, 24],
            [0, 8, 0, 8, 0, 1, 24, 0, 192, 0, 2, 0],
            [0, 8, 0, 8, 0, 1, 25, 0, 192, 0, 2, 129]
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
    public void RejectsTypedValuesOutsideFamilyAndAddressRanges()
    {
        DnsEdnsOption missing = Option();
        DnsEdnsOption family = SubnetOption(3, 0, 0, []);
        DnsEdnsOption source = SubnetOption(
            1, 33, 0, [192, 0, 2, 1]);
        DnsEdnsOption scope = SubnetOption(1, 24, 33, [192, 0, 2]);
        DnsEdnsOption address = SubnetOption(1, 24, 0, [192, 0]);

        Assert.Multiple(() =>
        {
            Assert.That(Encode(missing).Success, Is.False);
            Assert.That(Encode(family).Diagnostics[0].Field,
                Is.EqualTo("ednsOption.clientSubnet.addressFamily"));
            Assert.That(Encode(source).Diagnostics[0].Field,
                Is.EqualTo("ednsOption.clientSubnet.sourcePrefixLength"));
            Assert.That(Encode(scope).Diagnostics[0].Field,
                Is.EqualTo("ednsOption.clientSubnet.scopePrefixLength"));
            Assert.That(Encode(address).Diagnostics[0].Field,
                Is.EqualTo("ednsOption.clientSubnet.address"));
        });
    }

    [Test]
    public void RequestRequiresZeroScopeOnEncodeAndDecode()
    {
        DnsMessage request = Message(
            SubnetOption(1, 24, 16, [192, 0, 2]));
        DnsEncodeResult encoded = new DnsMessageCodec().TryEncodeMessage(
            request, new DnsEncodeOptions());
        byte[] wire = Header(false)
            .Concat(OptWire(
                [0, 8, 0, 7, 0, 1, 24, 16, 192, 0, 2]))
            .ToArray();
        DnsDecodeResult decoded = new DnsMessageCodec().TryDecodeMessage(
            wire, new DnsDecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.False);
            Assert.That(encoded.Diagnostics[0].Field,
                Is.EqualTo("ednsOption.clientSubnet.scopePrefixLength"));
            Assert.That(encoded.Diagnostics[0].Section,
                Is.EqualTo(DnsMessageSection.Additional));
            Assert.That(decoded.Success, Is.False);
            Assert.That(decoded.Diagnostics[0].Field,
                Is.EqualTo("ednsOption.clientSubnet.scopePrefixLength"));
            Assert.That(decoded.Diagnostics[0].RecordIndex, Is.Zero);
        });
    }

    [Test]
    public void ResponseAllowsScopeLongerThanSourcePrefix()
    {
        DnsMessage response = Message(
            SubnetOption(1, 24, 32, [192, 0, 2]));
        response.Header.Flags = DnsHeaderFlag.Response;
        DnsEncodeResult encoded = new DnsMessageCodec().TryEncodeMessage(
            response, new DnsEncodeOptions());
        DnsDecodeResult decoded = new DnsMessageCodec().TryDecodeMessage(
            encoded.Bytes, new DnsDecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.Message!.Header.Flags,
                Is.EqualTo(DnsHeaderFlag.Response));
            Assert.That(decoded.Message.Additionals[0].Data!.Edns!
                .Options[0].ClientSubnet!.ScopePrefixLength,
                Is.EqualTo(32));
        });
    }

    static DnsEdnsOption Option()
    {
        DnsEdnsOption option = new DnsEdnsOption();
        option.Code = DnsEdnsOptionCode.ClientSubnet;
        option.CodeValue = 8;
        return option;
    }

    static DnsEdnsOption SubnetOption(
        int family,
        int sourcePrefixLength,
        int scopePrefixLength,
        byte[] address)
    {
        DnsEdnsOption option = Option();
        option.ClientSubnet = new DnsEdnsClientSubnet();
        option.ClientSubnet.AddressFamily = family;
        option.ClientSubnet.SourcePrefixLength = sourcePrefixLength;
        option.ClientSubnet.ScopePrefixLength = scopePrefixLength;
        option.ClientSubnet.Address = address;
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

    static DnsMessage Message(DnsEdnsOption option)
    {
        DnsEdnsData edns = new DnsEdnsData();
        edns.Options.Add(option);
        DnsRecordData data = new DnsRecordData();
        data.Edns = edns;
        DnsResourceRecord opt = new DnsResourceRecord();
        opt.Name = new DnsNameService().ParseName(".", null);
        opt.RecordType = DnsRecordType.Opt;
        opt.RecordTypeValue = 41;
        opt.Data = data;
        DnsMessage message = new DnsMessage();
        message.Additionals.Add(opt);
        return message;
    }

    static byte[] Header(bool response)
    {
        byte[] result = new byte[12];
        if (response)
            result[2] = 128;
        result[11] = 1;
        return result;
    }

    static byte[] OptWire(byte[] option)
    {
        byte[] fixedFields =
        [
            0,
            0, 41,
            4, 208,
            0, 0, 0, 0,
            (byte)(option.Length >> 8), (byte)option.Length
        ];
        return fixedFields.Concat(option).ToArray();
    }
}
