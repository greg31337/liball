namespace dns.Tests.Internal.Names;

public class NameCompressionAdversarialTests
{
    [TestCase(0)]
    [TestCase(63)]
    [TestCase(64)]
    [TestCase(255)]
    [TestCase(256)]
    [TestCase(4095)]
    [TestCase(4096)]
    [TestCase(16383)]
    public void EveryFourteenBitPointerEncodingBoundaryDecodes(int target)
    {
        int pointerOffset = target + 3;
        byte[] message = FilledBytes(pointerOffset + 2, 0x40);
        message[target] = 1;
        message[target + 1] = 0x78;
        message[target + 2] = 0;
        message[pointerOffset] = (byte)(0xc0 | (target >> 8));
        message[pointerOffset + 1] = (byte)target;

        DnsDecodeResult result = CompressedDnsNameDecoder.DecodeName(
            message,
            pointerOffset,
            new DnsDecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(Format(result.Name!), Is.EqualTo("x."));
            Assert.That(result.BytesConsumed, Is.EqualTo(2));
            Assert.That(result.TrailingBytes, Is.Empty);
        });
    }

    [Test]
    public void PointersCanTargetEveryLabelBoundaryIncludingRoot()
    {
        byte[] message =
        [
            3, 0x77, 0x77, 0x77,
            7, 0x65, 0x78, 0x61, 0x6d, 0x70, 0x6c, 0x65,
            0,
            0xc0, 0x00,
            0xc0, 0x04,
            0xc0, 0x0c
        ];

        DnsDecodeResult whole = Decode(message, 13);
        DnsDecodeResult suffix = Decode(message, 15);
        DnsDecodeResult root = Decode(message, 17);

        Assert.Multiple(() =>
        {
            Assert.That(whole.Success, Is.True);
            Assert.That(Format(whole.Name!), Is.EqualTo("www.example."));
            Assert.That(whole.BytesConsumed, Is.EqualTo(2));
            Assert.That(suffix.Success, Is.True);
            Assert.That(Format(suffix.Name!), Is.EqualTo("example."));
            Assert.That(suffix.BytesConsumed, Is.EqualTo(2));
            Assert.That(root.Success, Is.True);
            Assert.That(Format(root.Name!), Is.EqualTo("."));
            Assert.That(root.BytesConsumed, Is.EqualTo(2));
        });
    }

    [Test]
    public void ExactPointerHopLimitSucceedsAndOneFewerFailsAtomically()
    {
        const int hopCount = 32;
        byte[] message = new byte[1 + (hopCount * 2)];
        message[0] = 0;
        int hop = 0;
        while (hop < hopCount)
        {
            int pointerOffset = 1 + (hop * 2);
            int target = hop == 0 ? 0 : pointerOffset - 2;
            message[pointerOffset] = (byte)(0xc0 | (target >> 8));
            message[pointerOffset + 1] = (byte)target;
            hop = hop + 1;
        }

        int start = message.Length - 2;
        DnsDecodeOptions exactOptions = new DnsDecodeOptions();
        exactOptions.MaximumCompressionPointerHops = hopCount;
        DnsDecodeOptions shortOptions = new DnsDecodeOptions();
        shortOptions.MaximumCompressionPointerHops = hopCount - 1;

        DnsDecodeResult exact = CompressedDnsNameDecoder.DecodeName(
            message, start, exactOptions);
        DnsDecodeResult excessive = CompressedDnsNameDecoder.DecodeName(
            message, start, shortOptions);

        Assert.Multiple(() =>
        {
            Assert.That(exact.Success, Is.True);
            Assert.That(Format(exact.Name!), Is.EqualTo("."));
            Assert.That(exact.BytesConsumed, Is.EqualTo(2));
            AssertFailure(
                excessive,
                DnsDiagnosticCode.CompressionHopLimitExceeded,
                1);
        });
    }

    [Test]
    public void NestedForwardOutOfRangeAndThreeNodeCycleGraphsAreRejected()
    {
        DnsDecodeResult nestedForward = Decode(
            [0, 1, 0x61, 0xc0, 0x05, 0],
            1);
        DnsDecodeResult maximumOutOfRange = Decode([0xff, 0xff], 0);
        DnsDecodeResult threeNodeCycle = Decode(
            [0xc0, 0x04, 0xc0, 0x00, 0xc0, 0x02],
            4);

        Assert.Multiple(() =>
        {
            AssertFailure(
                nestedForward,
                DnsDiagnosticCode.CompressionPointerForward,
                3);
            AssertFailure(
                maximumOutOfRange,
                DnsDiagnosticCode.CompressionPointerOutOfRange,
                0);
            AssertFailure(
                threeNodeCycle,
                DnsDiagnosticCode.CompressionLoop,
                0);
        });
    }

    [Test]
    public void CompressedExpansionAcceptsExactly255OctetsBeforeOverflow()
    {
        byte[] suffix = UncompressedName([63, 63, 63]);
        byte[] exactMessage = AppendLiteralAndPointer(suffix, 61);
        byte[] overflowMessage = AppendLiteralAndPointer(suffix, 62);

        DnsDecodeResult exact = Decode(exactMessage, suffix.Length);
        DnsDecodeResult overflow = Decode(overflowMessage, suffix.Length);

        Assert.Multiple(() =>
        {
            Assert.That(exact.Success, Is.True);
            Assert.That(exact.Name!.Labels, Has.Count.EqualTo(4));
            Assert.That(exact.BytesConsumed, Is.EqualTo(64));
            AssertFailure(overflow, DnsDiagnosticCode.NameTooLong, 128);
        });
    }

    [Test]
    public void FreshCompressionEncodersEmitTheSameLongestSuffixChoices()
    {
        byte[] expected =
        [
            7, 0x45, 0x78, 0x61, 0x6d, 0x70, 0x6c, 0x65, 0,
            3, 0x57, 0x77, 0x57, 0xc0, 0x00,
            3, 0x41, 0x70, 0x69, 0xc0, 0x09,
            4, 0x4d, 0x61, 0x69, 0x6c, 0xc0, 0x00,
            0
        ];

        int iteration = 0;
        while (iteration < 20)
        {
            byte[] actual = EncodeDeterministicSequence();
            Assert.That(actual, Is.EqualTo(expected), $"iteration {iteration}");
            iteration = iteration + 1;
        }
    }

    static byte[] EncodeDeterministicSequence()
    {
        byte[] destination = new byte[64];
        WireWriter writer = new WireWriter(destination, 0, destination.Length);
        CompressionTable table = new CompressionTable(0);
        DnsEncodeOptions options = new DnsEncodeOptions();
        table.WriteName(writer, Parse("Example."), options, true);
        table.WriteName(writer, Parse("WwW.Example."), options, true);
        table.WriteName(writer, Parse("Api.www.example."), options, true);
        table.WriteName(writer, Parse("Mail.Example."), options, true);
        table.WriteName(writer, Parse("."), options, true);
        return writer.CopyWrittenBytes();
    }

    static byte[] AppendLiteralAndPointer(byte[] suffix, int labelLength)
    {
        byte[] message = new byte[suffix.Length + labelLength + 3];
        Array.Copy(suffix, 0, message, 0, suffix.Length);
        int offset = suffix.Length;
        message[offset] = (byte)labelLength;
        int index = 0;
        while (index < labelLength)
        {
            message[offset + index + 1] = 0x78;
            index = index + 1;
        }

        message[offset + labelLength + 1] = 0xc0;
        message[offset + labelLength + 2] = 0;
        return message;
    }

    static byte[] UncompressedName(int[] labelLengths)
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

    static byte[] FilledBytes(int length, byte value)
    {
        byte[] bytes = new byte[length];
        int index = 0;
        while (index < bytes.Length)
        {
            bytes[index] = value;
            index = index + 1;
        }

        return bytes;
    }

    static DnsDecodeResult Decode(byte[] message, int offset)
    {
        return CompressedDnsNameDecoder.DecodeName(
            message,
            offset,
            new DnsDecodeOptions());
    }

    static DnsName Parse(string text)
    {
        return PresentationDnsName.ParseName(text, null).Name!;
    }

    static string Format(DnsName name)
    {
        return PresentationDnsName.FormatName(name).Text!;
    }

    static void AssertFailure(
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
