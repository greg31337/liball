namespace dns.Tests.Internal.Names;

public class CompressedDnsNameDecoderTests
{
    [Test]
    public void UncompressedNameDecodesFromOffsetAndOwnsItsLabels()
    {
        byte[] message =
        [
            0xaa,
            3, 0x57, 0x77, 0x57,
            7, 0x45, 0x78, 0x61, 0x6d, 0x70, 0x6c, 0x65,
            0,
            0xde, 0xad
        ];
        DnsDecodeResult result = CompressedDnsNameDecoder.DecodeName(
            message,
            1,
            new DnsDecodeOptions());
        message[2] = 0x78;

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Name, Is.Not.Null);
            Assert.That(result.Name!.IsAbsolute, Is.True);
            Assert.That(Format(result.Name), Is.EqualTo("WwW.Example."));
            Assert.That(result.BytesConsumed, Is.EqualTo(13));
            Assert.That(result.TrailingBytes,
                Is.EqualTo(new byte[] { 0xde, 0xad }));
            Assert.That(result.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void RootConsumesOneOctetAndLeavesFollowingMessageData()
    {
        byte[] message = [0, 0x12, 0x34];

        DnsDecodeResult result = CompressedDnsNameDecoder.DecodeName(
            message,
            0,
            new DnsDecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(Format(result.Name!), Is.EqualTo("."));
            Assert.That(result.BytesConsumed, Is.EqualTo(1));
            Assert.That(result.TrailingBytes,
                Is.EqualTo(new byte[] { 0x12, 0x34 }));
        });
    }

    [Test]
    public void PointerOnlyNameConsumesThePointerInsteadOfItsTargetBytes()
    {
        byte[] message =
        [
            7, 0x65, 0x78, 0x61, 0x6d, 0x70, 0x6c, 0x65, 0,
            0xc0, 0x00,
            0xbe, 0xef
        ];

        DnsDecodeResult result = CompressedDnsNameDecoder.DecodeName(
            message,
            9,
            new DnsDecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(Format(result.Name!), Is.EqualTo("example."));
            Assert.That(result.BytesConsumed, Is.EqualTo(2));
            Assert.That(result.TrailingBytes,
                Is.EqualTo(new byte[] { 0xbe, 0xef }));
        });
    }

    [Test]
    public void MixedLabelAndPointerChainsDecodeEverySuffix()
    {
        byte[] message =
        [
            7, 0x45, 0x78, 0x61, 0x6d, 0x70, 0x6c, 0x65, 0,
            3, 0x57, 0x57, 0x57, 0xc0, 0x00,
            3, 0x41, 0x50, 0x49, 0xc0, 0x09
        ];

        DnsDecodeResult result = CompressedDnsNameDecoder.DecodeName(
            message,
            15,
            new DnsDecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(Format(result.Name!), Is.EqualTo("API.WWW.Example."));
            Assert.That(result.BytesConsumed, Is.EqualTo(6));
            Assert.That(result.TrailingBytes, Is.Empty);
        });
    }

    [Test]
    public void PointerTargetOutsideMessageReportsPointerLocation()
    {
        byte[] message = [0, 0, 0xc0, 0x10];

        DnsDecodeResult result = CompressedDnsNameDecoder.DecodeName(
            message,
            2,
            new DnsDecodeOptions());

        AssertFailure(
            result,
            DnsDiagnosticCode.CompressionPointerOutOfRange,
            2);
    }

    [Test]
    public void ForwardPointersAreRejectedAtTheirWireLocation()
    {
        byte[] message = [0xc0, 0x02, 0];

        DnsDecodeResult result = CompressedDnsNameDecoder.DecodeName(
            message,
            0,
            new DnsDecodeOptions());

        AssertFailure(result, DnsDiagnosticCode.CompressionPointerForward, 0);
    }

    [Test]
    public void SelfAndMultiPointerCyclesHaveSpecificLoopDiagnostics()
    {
        DnsDecodeResult self = CompressedDnsNameDecoder.DecodeName(
            new byte[] { 0xc0, 0x00 },
            0,
            new DnsDecodeOptions());
        DnsDecodeResult multiPointer = CompressedDnsNameDecoder.DecodeName(
            new byte[] { 0xc0, 0x02, 0xc0, 0x00 },
            2,
            new DnsDecodeOptions());

        Assert.Multiple(() =>
        {
            AssertFailure(self, DnsDiagnosticCode.CompressionLoop, 0);
            AssertFailure(multiPointer, DnsDiagnosticCode.CompressionLoop, 0);
        });
    }

    [Test]
    public void PointerHopLimitIsAppliedToDereferencedChains()
    {
        byte[] message = [0, 0xc0, 0x00, 0xc0, 0x01];
        DnsDecodeOptions oneHop = new DnsDecodeOptions();
        oneHop.MaximumCompressionPointerHops = 1;

        DnsDecodeResult result = CompressedDnsNameDecoder.DecodeName(
            message,
            3,
            oneHop);

        AssertFailure(
            result,
            DnsDiagnosticCode.CompressionHopLimitExceeded,
            1);
    }

    [Test]
    public void ZeroHopLimitStillAllowsUncompressedNames()
    {
        DnsDecodeOptions options = new DnsDecodeOptions();
        options.MaximumCompressionPointerHops = 0;

        DnsDecodeResult direct = CompressedDnsNameDecoder.DecodeName(
            new byte[] { 1, 0x61, 0 },
            0,
            options);
        DnsDecodeResult pointer = CompressedDnsNameDecoder.DecodeName(
            new byte[] { 0, 0xc0, 0x00 },
            1,
            options);

        Assert.Multiple(() =>
        {
            Assert.That(direct.Success, Is.True);
            Assert.That(Format(direct.Name!), Is.EqualTo("a."));
            AssertFailure(
                pointer,
                DnsDiagnosticCode.CompressionHopLimitExceeded,
                1);
        });
    }

    [Test]
    public void TruncatedPointersAndLabelsReportTheMissingPayloadLocation()
    {
        DnsDecodeResult pointer = CompressedDnsNameDecoder.DecodeName(
            new byte[] { 0, 0xc0 },
            1,
            new DnsDecodeOptions());
        DnsDecodeResult label = CompressedDnsNameDecoder.DecodeName(
            new byte[] { 3, 0x61 },
            0,
            new DnsDecodeOptions());
        DnsDecodeResult missingRoot = CompressedDnsNameDecoder.DecodeName(
            new byte[] { 1, 0x61 },
            0,
            new DnsDecodeOptions());

        Assert.Multiple(() =>
        {
            AssertFailure(pointer, DnsDiagnosticCode.UnexpectedEnd, 2);
            AssertFailure(label, DnsDiagnosticCode.UnexpectedEnd, 1);
            AssertFailure(missingRoot, DnsDiagnosticCode.UnexpectedEnd, 2);
        });
    }

    [Test]
    public void ReservedLabelTagsAreRejectedWithoutBeingTreatedAsLengths()
    {
        DnsDecodeResult tagOne = CompressedDnsNameDecoder.DecodeName(
            new byte[] { 0x40, 0 },
            0,
            new DnsDecodeOptions());
        DnsDecodeResult tagTwo = CompressedDnsNameDecoder.DecodeName(
            new byte[] { 0x80, 0 },
            0,
            new DnsDecodeOptions());

        Assert.Multiple(() =>
        {
            AssertFailure(tagOne, DnsDiagnosticCode.InvalidLabel, 0);
            AssertFailure(tagTwo, DnsDiagnosticCode.InvalidLabel, 0);
        });
    }

    [Test]
    public void ExactMaximumExpandedLengthIsAccepted()
    {
        byte[] message = CreateUncompressedName([63, 63, 63, 61]);

        DnsDecodeResult result = CompressedDnsNameDecoder.DecodeName(
            message,
            0,
            new DnsDecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.BytesConsumed,
                Is.EqualTo(DnsProtocolLimits.MaximumNameLength));
            Assert.That(result.Name!.Labels, Has.Count.EqualTo(4));
        });
    }

    [Test]
    public void DirectExpandedLengthOverflowReportsTheOverflowingLabel()
    {
        byte[] message = CreateUncompressedName([63, 63, 63, 62]);

        DnsDecodeResult result = CompressedDnsNameDecoder.DecodeName(
            message,
            0,
            new DnsDecodeOptions());

        AssertFailure(result, DnsDiagnosticCode.NameTooLong, 192);
    }

    [Test]
    public void PointerExpansionAlsoEnforcesMaximumNameLength()
    {
        byte[] suffix = CreateUncompressedName([63, 63, 63, 61]);
        byte[] message = new byte[suffix.Length + 4];
        Array.Copy(suffix, 0, message, 0, suffix.Length);
        int nameOffset = suffix.Length;
        message[nameOffset] = 1;
        message[nameOffset + 1] = 0x78;
        message[nameOffset + 2] = 0xc0;
        message[nameOffset + 3] = 0;

        DnsDecodeResult result = CompressedDnsNameDecoder.DecodeName(
            message,
            nameOffset,
            new DnsDecodeOptions());

        AssertFailure(result, DnsDiagnosticCode.NameTooLong, 192);
    }

    [Test]
    public void InvalidOffsetsAndDecodeLimitsReturnDiagnostics()
    {
        DnsDecodeOptions shortMessageLimit = new DnsDecodeOptions();
        shortMessageLimit.MaximumMessageLength = 1;
        DnsDecodeOptions invalidHopLimit = new DnsDecodeOptions();
        invalidHopLimit.MaximumCompressionPointerHops = -1;

        DnsDecodeResult negativeOffset = CompressedDnsNameDecoder.DecodeName(
            new byte[] { 0 },
            -1,
            new DnsDecodeOptions());
        DnsDecodeResult endOffset = CompressedDnsNameDecoder.DecodeName(
            new byte[] { 0 },
            1,
            new DnsDecodeOptions());
        DnsDecodeResult excessiveMessage = CompressedDnsNameDecoder.DecodeName(
            new byte[] { 0, 0 },
            0,
            shortMessageLimit);
        DnsDecodeResult invalidHops = CompressedDnsNameDecoder.DecodeName(
            new byte[] { 0 },
            0,
            invalidHopLimit);
        DnsDecodeResult missingOptions = CompressedDnsNameDecoder.DecodeName(
            new byte[] { 0 },
            0,
            null!);

        Assert.Multiple(() =>
        {
            AssertFailure(negativeOffset, DnsDiagnosticCode.ValueOutOfRange, -1);
            AssertFailure(endOffset, DnsDiagnosticCode.UnexpectedEnd, 1);
            AssertFailure(excessiveMessage,
                DnsDiagnosticCode.ResourceLimitExceeded,
                0);
            AssertFailure(invalidHops, DnsDiagnosticCode.ValueOutOfRange, 0);
            AssertFailure(missingOptions, DnsDiagnosticCode.ValueOutOfRange, 0);
        });
    }

    private static byte[] CreateUncompressedName(int[] labelLengths)
    {
        List<byte> bytes = new List<byte>();
        int labelIndex = 0;
        while (labelIndex < labelLengths.Length)
        {
            int labelLength = labelLengths[labelIndex];
            bytes.Add((byte)labelLength);
            int byteIndex = 0;
            while (byteIndex < labelLength)
            {
                bytes.Add((byte)(0x61 + labelIndex));
                byteIndex = byteIndex + 1;
            }

            labelIndex = labelIndex + 1;
        }

        bytes.Add(0);
        return bytes.ToArray();
    }

    private static string Format(DnsName name)
    {
        return PresentationDnsName.FormatName(name).Text!;
    }

    private static void AssertFailure(
        DnsDecodeResult result,
        DnsDiagnosticCode code,
        int offset)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Name, Is.Null);
        Assert.That(result.BytesConsumed, Is.Zero);
        Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(result.Diagnostics[0].Code, Is.EqualTo(code));
        Assert.That(result.Diagnostics[0].HasOffset, Is.True);
        Assert.That(result.Diagnostics[0].Offset, Is.EqualTo(offset));
    }
}
