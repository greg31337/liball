using tftp;

namespace tftp.Tests;

public class NetasciiTests
{
    [Test]
    public void OctetModePreservesEveryByteThroughOneByteBuffers()
    {
        byte[] source = Enumerable.Range(0, 256).Select(value => (byte)value).ToArray();

        byte[] encoded = Encode(source, 1, TftpTransferMode.Octet);
        byte[] decoded = Decode(encoded, 1, 1, TftpTransferMode.Octet);

        Assert.Multiple(() =>
        {
            Assert.That(encoded, Is.EqualTo(source));
            Assert.That(decoded, Is.EqualTo(source));
        });
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(7)]
    [TestCase(8)]
    [TestCase(9)]
    [TestCase(127)]
    [TestCase(128)]
    [TestCase(129)]
    [TestCase(257)]
    [TestCase(258)]
    public void EveryByteRoundTripsAcrossNetasciiBlockEdges(int blockSize)
    {
        byte[] source = Enumerable.Range(0, 256).Select(value => (byte)value).ToArray();
        byte[] expected = CreateExpectedNetascii(source);

        byte[] encoded = Encode(source, blockSize, TftpTransferMode.Netascii);
        byte[] decoded = Decode(
            encoded,
            blockSize,
            blockSize,
            TftpTransferMode.Netascii);

        Assert.Multiple(() =>
        {
            Assert.That(encoded, Is.EqualTo(expected));
            Assert.That(decoded, Is.EqualTo(source));
        });
    }

    [Test]
    public void ExhaustiveCarriageReturnAndLineFeedRunsRoundTripOneByteAtATime()
    {
        int length = 0;
        while (length <= 7)
        {
            int combinationCount = 1 << length;
            int combination = 0;
            while (combination < combinationCount)
            {
                byte[] source = new byte[length + 2];
                source[0] = (byte)'A';
                source[source.Length - 1] = (byte)'B';
                int index = 0;
                while (index < length)
                {
                    if ((combination & (1 << index)) == 0)
                    {
                        source[index + 1] = 13;
                    }
                    else
                    {
                        source[index + 1] = 10;
                    }

                    index = index + 1;
                }

                byte[] encoded = Encode(source, 1, TftpTransferMode.Netascii);
                byte[] decoded = Decode(encoded, 1, 1, TftpTransferMode.Netascii);

                Assert.That(encoded, Is.EqualTo(CreateExpectedNetascii(source)),
                    $"Encoded mismatch for length {length}, combination {combination}.");
                Assert.That(decoded, Is.EqualTo(source),
                    $"Round-trip mismatch for length {length}, combination {combination}.");
                combination = combination + 1;
            }

            length = length + 1;
        }
    }

    [Test]
    public void EveryPossibleInputSplitPreservesNetasciiState()
    {
        byte[] source =
        {
            (byte)'A', 13, (byte)'B', 10, 13, 10, (byte)'C'
        };
        byte[] encoded = CreateExpectedNetascii(source);
        int chunkSize = 1;

        while (chunkSize <= encoded.Length)
        {
            byte[] decoded = Decode(
                encoded,
                chunkSize,
                1,
                TftpTransferMode.Netascii);

            Assert.That(decoded, Is.EqualTo(source),
                $"Decode mismatch for input chunk size {chunkSize}.");
            chunkSize = chunkSize + 1;
        }
    }

    [Test]
    public void EmptyInputProducesNoContentInEitherMode()
    {
        byte[] empty = Array.Empty<byte>();

        byte[] octetEncoded = Encode(empty, 1, TftpTransferMode.Octet);
        byte[] netasciiEncoded = Encode(empty, 1, TftpTransferMode.Netascii);
        byte[] octetDecoded = Decode(empty, 1, 1, TftpTransferMode.Octet);
        byte[] netasciiDecoded = Decode(empty, 1, 1, TftpTransferMode.Netascii);

        Assert.Multiple(() =>
        {
            Assert.That(octetEncoded, Is.Empty);
            Assert.That(netasciiEncoded, Is.Empty);
            Assert.That(octetDecoded, Is.Empty);
            Assert.That(netasciiDecoded, Is.Empty);
        });
    }

    [Test]
    public void EveryInvalidCarriageReturnPairFailsWithinAndAcrossChunks()
    {
        int value = 0;
        while (value <= byte.MaxValue)
        {
            if (value != 0 && value != 10)
            {
                byte pairedValue = (byte)value;
                NetasciiDecoder sameChunk = new NetasciiDecoder(TftpTransferMode.Netascii);
                NetasciiDecodeResult sameChunkResult = sameChunk.DecodeBytes(
                    new byte[] { 13, pairedValue },
                    new byte[1]);

                NetasciiDecoder splitChunk = new NetasciiDecoder(TftpTransferMode.Netascii);
                NetasciiDecodeResult prefix = splitChunk.DecodeBytes(
                    new byte[] { 13 },
                    Span<byte>.Empty);
                NetasciiDecodeResult splitChunkResult = splitChunk.DecodeBytes(
                    new byte[] { pairedValue },
                    new byte[1]);

                Assert.Multiple(() =>
                {
                    Assert.That(sameChunkResult.Success, Is.False,
                        $"Same-chunk pair unexpectedly accepted for byte {value}.");
                    Assert.That(sameChunkResult.FailureOffset, Is.EqualTo(1));
                    Assert.That(prefix.Success, Is.True);
                    Assert.That(prefix.HasPendingCarriageReturn, Is.True);
                    Assert.That(splitChunkResult.Success, Is.False,
                        $"Split pair unexpectedly accepted for byte {value}.");
                    Assert.That(splitChunkResult.FailureOffset, Is.Zero);
                });
            }

            value = value + 1;
        }
    }

    [Test]
    public void FinalizationRejectsDanglingCarriageReturnAtEveryChunkBoundary()
    {
        byte[] prefix = { (byte)'A', (byte)'B', 13 };
        int chunkSize = 1;

        while (chunkSize <= prefix.Length)
        {
            NetasciiDecoder decoder = new NetasciiDecoder(TftpTransferMode.Netascii);
            DecodeWithoutFinishing(decoder, prefix, chunkSize, 1);

            NetasciiDecodeResult result = decoder.FinishDecoding();

            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.False,
                    $"Dangling CR accepted for chunk size {chunkSize}.");
                Assert.That(result.HasPendingCarriageReturn, Is.True);
            });
            chunkSize = chunkSize + 1;
        }
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(6)]
    public void ExactEncodedBlockMultiplesLeaveNoHiddenOutput(int blockSize)
    {
        byte[] source = { (byte)'A', 10, 13, (byte)'B' };
        NetasciiEncoder encoder = new NetasciiEncoder(TftpTransferMode.Netascii);
        List<byte> encoded = new List<byte>();
        List<int> blockLengths = new List<int>();
        int sourceOffset = 0;
        bool needsMoreOutput = true;

        while (needsMoreOutput)
        {
            byte[] block = new byte[blockSize];
            NetasciiEncodeResult result = encoder.EncodeBytes(
                source.AsSpan(sourceOffset),
                block);
            sourceOffset = sourceOffset + result.BytesConsumed;
            encoded.AddRange(block.AsSpan(0, result.BytesWritten).ToArray());
            blockLengths.Add(result.BytesWritten);
            needsMoreOutput = result.NeedsMoreOutput;
        }

        byte[] emptyBlock = new byte[blockSize];
        NetasciiEncodeResult final = encoder.EncodeBytes(
            ReadOnlySpan<byte>.Empty,
            emptyBlock);

        Assert.Multiple(() =>
        {
            Assert.That(encoded, Is.EqualTo(CreateExpectedNetascii(source)));
            Assert.That(encoded.Count % blockSize, Is.Zero);
            Assert.That(blockLengths, Has.All.EqualTo(blockSize));
            Assert.That(final.BytesConsumed, Is.Zero);
            Assert.That(final.BytesWritten, Is.Zero);
            Assert.That(final.HasPendingOutput, Is.False);
            Assert.That(final.NeedsMoreOutput, Is.False);
        });
    }

    [Test]
    public void EncoderRejectsInvalidTransferMode()
    {
        TftpProtocolException exception = Assert.Throws<TftpProtocolException>(
            () => new NetasciiEncoder((TftpTransferMode)99));

        Assert.That(exception.Message, Does.Contain("transfer mode is invalid"));
    }

    private static byte[] CreateExpectedNetascii(byte[] source)
    {
        List<byte> expected = new List<byte>();
        foreach (byte value in source)
        {
            if (value == 10)
            {
                expected.Add(13);
                expected.Add(10);
            }
            else if (value == 13)
            {
                expected.Add(13);
                expected.Add(0);
            }
            else
            {
                expected.Add(value);
            }
        }

        return expected.ToArray();
    }

    private static byte[] Encode(
        byte[] source,
        int outputBlockSize,
        TftpTransferMode mode)
    {
        NetasciiEncoder encoder = new NetasciiEncoder(mode);
        List<byte> encoded = new List<byte>();
        int sourceOffset = 0;
        bool needsMoreOutput = true;
        while (needsMoreOutput)
        {
            byte[] destination = new byte[outputBlockSize];
            NetasciiEncodeResult result = encoder.EncodeBytes(
                source.AsSpan(sourceOffset),
                destination);
            if (source.Length != 0)
            {
                Assert.That(result.BytesConsumed + result.BytesWritten, Is.GreaterThan(0));
            }

            sourceOffset = sourceOffset + result.BytesConsumed;
            encoded.AddRange(destination.AsSpan(0, result.BytesWritten).ToArray());
            needsMoreOutput = result.NeedsMoreOutput;
        }

        Assert.That(sourceOffset, Is.EqualTo(source.Length));
        return encoded.ToArray();
    }

    private static byte[] Decode(
        byte[] source,
        int inputChunkSize,
        int outputBlockSize,
        TftpTransferMode mode)
    {
        NetasciiDecoder decoder = new NetasciiDecoder(mode);
        byte[] decoded = DecodeWithoutFinishing(
            decoder,
            source,
            inputChunkSize,
            outputBlockSize);
        NetasciiDecodeResult finished = decoder.FinishDecoding();
        Assert.That(finished.Success, Is.True);
        return decoded;
    }

    private static byte[] DecodeWithoutFinishing(
        NetasciiDecoder decoder,
        byte[] source,
        int inputChunkSize,
        int outputBlockSize)
    {
        List<byte> decoded = new List<byte>();
        int sourceOffset = 0;
        while (sourceOffset < source.Length)
        {
            int chunkLength = inputChunkSize;
            if (chunkLength > source.Length - sourceOffset)
            {
                chunkLength = source.Length - sourceOffset;
            }

            int chunkEnd = sourceOffset + chunkLength;
            while (sourceOffset < chunkEnd)
            {
                byte[] destination = new byte[outputBlockSize];
                NetasciiDecodeResult result = decoder.DecodeBytes(
                    source.AsSpan(sourceOffset, chunkEnd - sourceOffset),
                    destination);
                Assert.That(result.Success, Is.True);
                Assert.That(result.BytesConsumed + result.BytesWritten, Is.GreaterThan(0));
                sourceOffset = sourceOffset + result.BytesConsumed;
                decoded.AddRange(destination.AsSpan(0, result.BytesWritten).ToArray());
            }
        }

        return decoded.ToArray();
    }
}
