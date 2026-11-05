using raw;

namespace raw.Tests;

public class PackBitsTests
{
    [Test]
    public void DecoderHandlesLiteralRepeatAndNoOpPackets()
    {
        byte[] encoded = new byte[] { 2, (byte)'A', (byte)'B', (byte)'C', 0xFE, (byte)'D', 0x80, 0, (byte)'E' };
        byte[] destination = new byte[7];

        int consumed = PackBitsCodec.Decode(encoded, destination, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(consumed, Is.EqualTo(encoded.Length));
            Assert.That(destination, Is.EqualTo(new byte[] { (byte)'A', (byte)'B', (byte)'C', (byte)'D', (byte)'D', (byte)'D', (byte)'E' }));
        });
    }

    [Test]
    public void EncoderProducesDeterministicLiteralAndRepeatPackets()
    {
        byte[] source = new byte[] { (byte)'A', (byte)'A', (byte)'A', (byte)'B', (byte)'C', (byte)'D', (byte)'D', (byte)'D', (byte)'D' };
        byte[] expected = new byte[] { 0xFE, (byte)'A', 1, (byte)'B', (byte)'C', 0xFD, (byte)'D' };
        byte[] first = PackBitsCodec.Encode(source, new RawLimits(), CancellationToken.None);
        byte[] second = PackBitsCodec.Encode(source, new RawLimits(), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(expected));
            Assert.That(second, Is.EqualTo(expected));
            Assert.That(PackBitsCodec.GetEncodedLength(source, CancellationToken.None), Is.EqualTo(expected.Length));
            Assert.That(PackBitsCodec.Decode(first, source.Length, new RawLimits(), CancellationToken.None), Is.EqualTo(source));
        });
    }

    [Test]
    public void PacketLengthBoundariesRoundTripExactly()
    {
        byte[] literal128 = Enumerable.Range(0, 128).Select(value => (byte)value).ToArray();
        byte[] literal129 = Enumerable.Range(0, 129).Select(value => (byte)value).ToArray();
        byte[] repeat128 = Enumerable.Repeat((byte)0x5A, 128).ToArray();
        byte[] repeat129 = Enumerable.Repeat((byte)0xA5, 129).ToArray();

        byte[] encodedLiteral128 = Encode(literal128);
        byte[] encodedLiteral129 = Encode(literal129);
        byte[] encodedRepeat128 = Encode(repeat128);
        byte[] encodedRepeat129 = Encode(repeat129);

        Assert.Multiple(() =>
        {
            Assert.That(encodedLiteral128[0], Is.EqualTo(127));
            Assert.That(encodedLiteral128.Length, Is.EqualTo(129));
            Assert.That(encodedLiteral129.Length, Is.EqualTo(131));
            Assert.That(encodedRepeat128, Is.EqualTo(new byte[] { 0x81, 0x5A }));
            Assert.That(encodedRepeat129, Is.EqualTo(new byte[] { 0x81, 0xA5, 0, 0xA5 }));
            Assert.That(Decode(encodedLiteral128, literal128.Length), Is.EqualTo(literal128));
            Assert.That(Decode(encodedLiteral129, literal129.Length), Is.EqualTo(literal129));
            Assert.That(Decode(encodedRepeat128, repeat128.Length), Is.EqualTo(repeat128));
            Assert.That(Decode(encodedRepeat129, repeat129.Length), Is.EqualTo(repeat129));
        });
    }

    [Test]
    public void DecoderAcceptsEveryDefinedLiteralAndRepeatRunLength()
    {
        for (int literalLength = 1; literalLength <= 128; literalLength++)
        {
            byte[] encoded = new byte[literalLength + 1];
            encoded[0] = (byte)(literalLength - 1);
            for (int index = 0; index < literalLength; index++)
            {
                encoded[index + 1] = (byte)index;
            }
            Assert.That(Decode(encoded, literalLength), Is.EqualTo(encoded.Skip(1).ToArray()), $"literal length {literalLength}");
        }

        for (int repeatLength = 2; repeatLength <= 128; repeatLength++)
        {
            byte[] encoded = new byte[] { (byte)(1 - repeatLength), 0x6C };
            Assert.That(Decode(encoded, repeatLength), Is.All.EqualTo(0x6C), $"repeat length {repeatLength}");
        }
    }

    [Test]
    public void EmptyAndNoOpOnlyInputsDecodeToEmptyOutput()
    {
        byte[] empty = Decode(new byte[0], 0);
        byte[] noOps = Decode(new byte[] { 0x80, 0x80, 0x80 }, 0);

        Assert.Multiple(() =>
        {
            Assert.That(empty, Is.Empty);
            Assert.That(noOps, Is.Empty);
            Assert.That(Encode(empty), Is.Empty);
        });
    }

    [Test]
    public void RandomInputsRoundTripWithoutSourceMutation()
    {
        Random random = new Random(17719);
        int[] lengths = new int[] { 0, 1, 2, 3, 127, 128, 129, 255, 256, 1024 };
        foreach (int length in lengths)
        {
            byte[] source = new byte[length];
            random.NextBytes(source);
            for (int index = 16; index < source.Length; index += 47)
            {
                int end = Math.Min(source.Length, index + 19);
                Array.Fill(source, (byte)(index & 0xFF), index, end - index);
            }
            byte[] original = source.ToArray();

            byte[] encoded = Encode(source);
            byte[] decoded = Decode(encoded, source.Length);

            Assert.Multiple(() =>
            {
                Assert.That(decoded, Is.EqualTo(source), $"length {length}");
                Assert.That(source, Is.EqualTo(original), $"source length {length}");
            });
        }
    }

    [Test]
    public void DecoderRejectsTruncatedAndLengthMismatchedPacketsBeforeWriting()
    {
        byte[] literalOutput = Enumerable.Repeat((byte)0xCC, 3).ToArray();
        byte[] repeatOutput = Enumerable.Repeat((byte)0xCC, 3).ToArray();
        byte[] shortOutput = Enumerable.Repeat((byte)0xCC, 2).ToArray();
        byte[] longOutput = Enumerable.Repeat((byte)0xCC, 2).ToArray();
        byte[] trailingOutput = new byte[] { 0xCC };

        RawFormatException? literal = Assert.Throws<RawFormatException>(() => PackBitsCodec.Decode(new byte[] { 2, 1, 2 }, literalOutput, CancellationToken.None));
        RawFormatException? repeat = Assert.Throws<RawFormatException>(() => PackBitsCodec.Decode(new byte[] { 0xFF }, repeatOutput, CancellationToken.None));
        RawFormatException? underfill = Assert.Throws<RawFormatException>(() => PackBitsCodec.Decode(new byte[] { 0, 1 }, shortOutput, CancellationToken.None));
        RawFormatException? overflow = Assert.Throws<RawFormatException>(() => PackBitsCodec.Decode(new byte[] { 0xFE, 1 }, longOutput, CancellationToken.None));
        RawFormatException? trailing = Assert.Throws<RawFormatException>(() => PackBitsCodec.Decode(new byte[] { 0, 1, 0, 2 }, trailingOutput, CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That((literal ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.TruncatedInput));
            Assert.That(literal.Offset, Is.EqualTo(3));
            Assert.That((repeat ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.TruncatedInput));
            Assert.That(repeat.Offset, Is.EqualTo(1));
            Assert.That((underfill ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.TruncatedInput));
            Assert.That((overflow ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidCodecData));
            Assert.That((trailing ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidCodecData));
            Assert.That(literalOutput, Is.All.EqualTo(0xCC));
            Assert.That(repeatOutput, Is.All.EqualTo(0xCC));
            Assert.That(shortOutput, Is.All.EqualTo(0xCC));
            Assert.That(longOutput, Is.All.EqualTo(0xCC));
            Assert.That(trailingOutput, Is.All.EqualTo(0xCC));
        });
    }

    [Test]
    public void EncoderPreflightsShortDestinationWithoutWriting()
    {
        byte[] source = Enumerable.Range(0, 20).Select(value => (byte)value).ToArray();
        byte[] destination = Enumerable.Repeat((byte)0xCC, 20).ToArray();

        RawFormatException? exception = Assert.Throws<RawFormatException>(() => PackBitsCodec.Encode(source, destination, CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That((exception ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidLength));
            Assert.That(destination, Is.All.EqualTo(0xCC));
        });
    }

    [Test]
    public void AllocatingOperationsEnforceLimitsAndArgumentValidation()
    {
        RawLimits limits = new RawLimits();
        limits.MaxDecodedBytes = 4;
        limits.MaxAllocationBytes = 4;

        RawLimitException? decodeLimit = Assert.Throws<RawLimitException>(() => PackBitsCodec.Decode(new byte[] { 0xFD, 1 }, 5, limits, CancellationToken.None));
        RawLimitException? encodeLimit = Assert.Throws<RawLimitException>(() => PackBitsCodec.Encode(new byte[] { 1, 2, 3, 4 }, limits, CancellationToken.None));
        RawLimitException? missingLimits = Assert.Throws<RawLimitException>(() => PackBitsCodec.Decode(new byte[0], 0, null, CancellationToken.None));
        RawFormatException? negativeLength = Assert.Throws<RawFormatException>(() => PackBitsCodec.Decode(new byte[0], -1, new RawLimits(), CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That((decodeLimit ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.ResourceLimitExceeded));
            Assert.That((encodeLimit ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.ResourceLimitExceeded));
            Assert.That((missingLimits ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidArgument));
            Assert.That((negativeLength ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidLength));
        });
    }

    [Test]
    public void PreCancelledOperationsDoNotWriteDestinations()
    {
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        byte[] decodeDestination = Enumerable.Repeat((byte)0xCC, 3).ToArray();
        byte[] encodeDestination = Enumerable.Repeat((byte)0xCC, 8).ToArray();

        Assert.Throws<OperationCanceledException>(() => PackBitsCodec.Decode(new byte[] { 0xFE, 1 }, decodeDestination, cancellation.Token));
        Assert.Throws<OperationCanceledException>(() => PackBitsCodec.Encode(new byte[] { 1, 2, 3 }, encodeDestination, cancellation.Token));

        Assert.Multiple(() =>
        {
            Assert.That(decodeDestination, Is.All.EqualTo(0xCC));
            Assert.That(encodeDestination, Is.All.EqualTo(0xCC));
        });
    }

    static byte[] Encode(byte[] source)
    {
        return PackBitsCodec.Encode(source, new RawLimits(), CancellationToken.None);
    }

    static byte[] Decode(byte[] source, int decodedLength)
    {
        return PackBitsCodec.Decode(source, decodedLength, new RawLimits(), CancellationToken.None);
    }
}
