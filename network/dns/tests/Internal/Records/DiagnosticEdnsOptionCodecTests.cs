namespace dns.Tests.Internal.Records;

public class DiagnosticEdnsOptionCodecTests
{
    [Test]
    public void ExtendedErrorsRoundTripKnownUnknownAndUtf8Text()
    {
        DnsEdnsOption known = Decode(
            [0, 15, 0, 7, 0, 15, 99, 97, 102, 195, 169]);
        DnsEdnsOption unknown = Option(DnsEdnsOptionCode.ExtendedDnsError);
        unknown.ExtendedError = new DnsEdnsExtendedError();
        unknown.ExtendedError.Code = (DnsExtendedErrorCode)65000;
        unknown.ExtendedError.CodeValue = 65000;
        unknown.ExtendedError.ExtraText = "failure \ud83d\udea7";

        DnsEdnsOptionResult encoded = Encode(unknown);
        DnsEdnsOption decoded = Decode(encoded.Bytes);

        Assert.Multiple(() =>
        {
            Assert.That(known.ExtendedError!.Code,
                Is.EqualTo(DnsExtendedErrorCode.Blocked));
            Assert.That(known.ExtendedError.CodeValue, Is.EqualTo(15));
            Assert.That(known.ExtendedError.ExtraText, Is.EqualTo("caf\u00e9"));
            Assert.That(encoded.Success, Is.True);
            Assert.That((int)decoded.ExtendedError!.Code, Is.EqualTo(65000));
            Assert.That(decoded.ExtendedError.CodeValue, Is.EqualTo(65000));
            Assert.That(decoded.ExtendedError.ExtraText,
                Is.EqualTo("failure \ud83d\udea7"));
        });
    }

    [Test]
    public void ExtendedErrorRejectsMissingCodeInvalidUtf8AndInvalidValues()
    {
        byte[][] malformed =
        [
            [0, 15, 0, 0],
            [0, 15, 0, 3, 0, 1, 192],
            [0, 15, 0, 4, 0, 1, 237, 160]
        ];
        DnsEdnsOption invalidCode = Option(
            DnsEdnsOptionCode.ExtendedDnsError);
        invalidCode.ExtendedError = new DnsEdnsExtendedError();
        invalidCode.ExtendedError.CodeValue = 65536;
        DnsEdnsOption invalidText = Option(
            DnsEdnsOptionCode.ExtendedDnsError);
        invalidText.ExtendedError = new DnsEdnsExtendedError();
        invalidText.ExtendedError.ExtraText = "\ud800";

        Assert.Multiple(() =>
        {
            AssertMalformedWithFallback(malformed);
            Assert.That(Encode(invalidCode).Diagnostics[0].Field,
                Is.EqualTo("ednsOption.extendedError.code"));
            Assert.That(Encode(invalidText).Diagnostics[0].Field,
                Is.EqualTo("ednsOption.extendedError.extraText"));
        });
    }

    [Test]
    public void ReportChannelRoundTripsUncompressedAbsoluteAgentDomain()
    {
        DnsEdnsOption option = Option(DnsEdnsOptionCode.ReportChannel);
        option.ReportChannel = Parse("a01.agent-domain.example.");

        DnsEdnsOptionResult encoded = Encode(option);
        DnsEdnsOption decoded = Decode(encoded.Bytes);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Bytes[4], Is.EqualTo(3));
            Assert.That(encoded.Bytes[^1], Is.Zero);
            Assert.That(Format(decoded.ReportChannel!),
                Is.EqualTo("a01.agent-domain.example."));
            Assert.That(decoded.UseRawData, Is.False);
        });
    }

    [Test]
    public void ReportChannelRejectsMissingRootCompressedAndRootNames()
    {
        byte[][] malformed =
        [
            [0, 18, 0, 0],
            [0, 18, 0, 1, 0],
            [0, 18, 0, 2, 192, 0],
            [0, 18, 0, 4, 3, 99, 111, 109]
        ];
        DnsEdnsOption root = Option(DnsEdnsOptionCode.ReportChannel);
        root.ReportChannel = Parse(".");
        DnsEdnsOption relative = Option(DnsEdnsOptionCode.ReportChannel);
        relative.ReportChannel = Parse("agent.example");

        Assert.Multiple(() =>
        {
            AssertMalformedWithFallback(malformed);
            Assert.That(Encode(root).Success, Is.False);
            Assert.That(Encode(relative).Success, Is.False);
        });
    }

    [Test]
    public void ZoneVersionRoundTripsRequestSoaSerialAndUnknownType()
    {
        DnsEdnsOption request = ZoneVersion(false, 0, 0, []);
        DnsEdnsOption soa = ZoneVersion(
            true, 2, 0, [120, 149, 164, 233]);
        DnsEdnsOption unknown = ZoneVersion(true, 1, 246, []);

        DnsEdnsOptionResult requestWire = Encode(request);
        DnsEdnsOptionResult soaWire = Encode(soa);
        DnsEdnsOption decodedRequest = Decode(requestWire.Bytes);
        DnsEdnsOption decodedSoa = Decode(soaWire.Bytes);
        DnsEdnsOption decodedUnknown = Decode(Encode(unknown).Bytes);

        Assert.Multiple(() =>
        {
            Assert.That(requestWire.Bytes,
                Is.EqualTo(new byte[] { 0, 19, 0, 0 }));
            Assert.That(decodedRequest.ZoneVersion!.HasVersion, Is.False);
            Assert.That(soaWire.Bytes,
                Is.EqualTo(new byte[]
                {
                    0, 19, 0, 6, 2, 0, 120, 149, 164, 233
                }));
            Assert.That(decodedSoa.ZoneVersion!.LabelCount, Is.EqualTo(2));
            Assert.That(decodedSoa.ZoneVersion.Type, Is.Zero);
            Assert.That(decodedSoa.ZoneVersion.Version,
                Is.EqualTo(new byte[] { 120, 149, 164, 233 }));
            Assert.That(decodedUnknown.ZoneVersion!.Type, Is.EqualTo(246));
            Assert.That(decodedUnknown.ZoneVersion.Version, Is.Empty);
        });
    }

    [Test]
    public void ZoneVersionRejectsTruncatedSoaAndOutOfRangeFields()
    {
        byte[][] malformed =
        [
            [0, 19, 0, 1, 2],
            [0, 19, 0, 5, 2, 0, 1, 2, 3],
            [0, 19, 0, 7, 2, 0, 1, 2, 3, 4, 5]
        ];
        DnsEdnsOption labels = ZoneVersion(
            true, 256, 1, []);
        DnsEdnsOption type = ZoneVersion(
            true, 1, 256, []);
        DnsEdnsOption soa = ZoneVersion(
            true, 1, 0, new byte[3]);

        Assert.Multiple(() =>
        {
            AssertMalformedWithFallback(malformed);
            Assert.That(Encode(labels).Diagnostics[0].Field,
                Is.EqualTo("ednsOption.zoneVersion.labelCount"));
            Assert.That(Encode(type).Diagnostics[0].Field,
                Is.EqualTo("ednsOption.zoneVersion.type"));
            Assert.That(Encode(soa).Diagnostics[0].Field,
                Is.EqualTo("ednsOption.zoneVersion.version"));
        });
    }

    static void AssertMalformedWithFallback(byte[][] malformed)
    {
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
            Assert.That(rejected.Success, Is.False);
            Assert.That(preserved.Success, Is.True);
            Assert.That(preserved.Option!.UseRawData, Is.True);
            index = index + 1;
        }
    }

    static DnsEdnsOption ZoneVersion(
        bool hasVersion,
        int labelCount,
        int type,
        byte[] versionBytes)
    {
        DnsEdnsOption option = Option(DnsEdnsOptionCode.ZoneVersion);
        option.ZoneVersion = new DnsEdnsZoneVersion();
        option.ZoneVersion.HasVersion = hasVersion;
        option.ZoneVersion.LabelCount = labelCount;
        option.ZoneVersion.Type = type;
        option.ZoneVersion.Version = versionBytes;
        return option;
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

    static DnsName Parse(string text)
    {
        return new DnsNameService().ParseName(text, null);
    }

    static string Format(DnsName name)
    {
        return new DnsNameService().FormatName(
            name, new DnsTextOptions());
    }
}
