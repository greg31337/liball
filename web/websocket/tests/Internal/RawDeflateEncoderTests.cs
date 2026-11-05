using System.IO.Compression;
using websocket;

namespace websocket.Tests;

public class RawDeflateEncoderTests
{
    [Test]
    public void EmptyFinalStreamProducesAValidRoundTrip()
    {
        RawDeflateEncoderRun encoded = EncodeAll(
            [],
            1,
            1,
            8,
            true);
        RawDeflateEncoderDecoded decoded = DecodeAll(
            encoded.Output.ToArray(),
            8,
            0);
        byte[] platformDecoded = InflateWithPlatform(encoded.Output.ToArray());

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Result.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(encoded.Result.StreamComplete, Is.True);
            Assert.That(encoded.Result.TotalInputCount, Is.Zero);
            Assert.That(encoded.Output, Is.Not.Empty);
            Assert.That((encoded.Output[0] & 1), Is.EqualTo(1));
            Assert.That(decoded.Result.StreamComplete, Is.True);
            Assert.That(decoded.Output, Is.Empty);
            Assert.That(platformDecoded, Is.Empty);
        });
    }

    [TestCase("stored", 0)]
    [TestCase("fixed", 1)]
    [TestCase("dynamic", 2)]
    public void PlatformInflaterAcceptsEverySelectedBlockType(
        string kind,
        int expectedBlockType)
    {
        byte[] source;
        if (kind == "stored")
        {
            source = RandomBytes(16000, 12345);
        }
        else if (kind == "fixed")
        {
            source = "small fixed block"u8.ToArray();
        }
        else
        {
            source = new byte[20000];
            source.AsSpan().Fill((byte)'A');
        }

        RawDeflateEncoderRun encoded = EncodeAll(
            source,
            503,
            17,
            15,
            true);
        byte[] decoded = InflateWithPlatform(encoded.Output.ToArray());

        Assert.Multiple(() =>
        {
            Assert.That((encoded.Output[0] >> 1) & 3,
                Is.EqualTo(expectedBlockType));
            Assert.That(decoded, Is.EqualTo(source));
        });
    }

    [Test]
    public void SelectsStoredBlockForIncompressibleInputAndRoundTrips()
    {
        byte[] source = RandomBytes(16000, 12345);

        RawDeflateEncoderRun encoded = EncodeAll(
            source,
            997,
            113,
            15,
            true);
        RawDeflateEncoderDecoded decoded = DecodeAll(
            encoded.Output.ToArray(),
            15,
            source.Length);

        Assert.Multiple(() =>
        {
            Assert.That((encoded.Output[0] >> 1) & 3, Is.Zero);
            Assert.That(encoded.Result.LastBlockType, Is.Zero);
            Assert.That(encoded.Result.StreamComplete, Is.True);
            Assert.That(decoded.Result.StreamComplete, Is.True);
            Assert.That(decoded.Output, Is.EqualTo(source));
        });
    }

    [Test]
    public void SelectsFixedBlockForSmallInputAndRoundTrips()
    {
        byte[] source = "small fixed block"u8.ToArray();

        RawDeflateEncoderRun encoded = EncodeAll(
            source,
            1,
            1,
            8,
            true);
        RawDeflateEncoderDecoded decoded = DecodeAll(
            encoded.Output.ToArray(),
            8,
            source.Length);

        Assert.Multiple(() =>
        {
            Assert.That((encoded.Output[0] >> 1) & 3, Is.EqualTo(1));
            Assert.That(encoded.Result.LastBlockType, Is.EqualTo(1));
            Assert.That(decoded.Output, Is.EqualTo(source));
        });
    }

    [Test]
    public void SelectsDynamicBlockForSkewedInputAndRoundTrips()
    {
        var source = new byte[20000];
        source.AsSpan().Fill((byte)'A');

        RawDeflateEncoderRun encoded = EncodeAll(
            source,
            137,
            2,
            15,
            true);
        RawDeflateEncoderDecoded decoded = DecodeAll(
            encoded.Output.ToArray(),
            15,
            source.Length);

        Assert.Multiple(() =>
        {
            Assert.That((encoded.Output[0] >> 1) & 3, Is.EqualTo(2));
            Assert.That(encoded.Result.LastBlockType, Is.EqualTo(2));
            Assert.That(encoded.Output.Count, Is.LessThan(200));
            Assert.That(decoded.Output, Is.EqualTo(source));
        });
    }

    [Test]
    public void IncrementalEncodingCrossesBlockBoundaryWithOneByteOutput()
    {
        var source = new byte[40000];
        int index = 0;
        while (index < source.Length)
        {
            source[index] = (byte)('a' + (index % 7));
            index++;
        }

        RawDeflateEncoderRun encoded = EncodeAll(
            source,
            211,
            1,
            15,
            true);
        RawDeflateEncoderDecoded decoded = DecodeAll(
            encoded.Output.ToArray(),
            15,
            source.Length);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Result.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(encoded.Result.TotalInputCount,
                Is.EqualTo(source.Length));
            Assert.That(decoded.Output, Is.EqualTo(source));
        });
    }

    [TestCase(8)]
    [TestCase(9)]
    [TestCase(10)]
    [TestCase(11)]
    [TestCase(12)]
    [TestCase(13)]
    [TestCase(14)]
    [TestCase(15)]
    public void IncrementalInputsRoundTripAtEveryNegotiatedWindowSize(
        int windowBits)
    {
        byte[] source = RepetitiveBytes(40001);

        RawDeflateEncoderRun encoded = EncodeAll(
            source,
            113,
            1,
            windowBits,
            true);
        RawDeflateEncoderDecoded decoded = DecodeAll(
            encoded.Output.ToArray(),
            windowBits,
            source.Length);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Result.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(encoded.Result.StreamComplete, Is.True);
            Assert.That(encoded.Result.TotalInputCount,
                Is.EqualTo(source.Length));
            Assert.That(decoded.Result.StreamComplete, Is.True);
            Assert.That(decoded.Output, Is.EqualTo(source));
        });
    }

    [Test]
    public void ExactFullBlockDrainsBeforeTerminalBlockIsGenerated()
    {
        byte[] source = RandomBytes(32768, 4567);

        RawDeflateEncoderRun encoded = EncodeAll(
            source,
            source.Length,
            1,
            15,
            true);
        RawDeflateEncoderDecoded decoded = DecodeAll(
            encoded.Output.ToArray(),
            15,
            source.Length);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Result.StreamComplete, Is.True);
            Assert.That(encoded.Result.TotalInputCount,
                Is.EqualTo(source.Length));
            Assert.That(decoded.Result.StreamComplete, Is.True);
            Assert.That(decoded.Output, Is.EqualTo(source));
        });
    }

    [Test]
    public void SyncFlushEndsAtByteBoundaryWithCanonicalTail()
    {
        byte[] source = "sync flush sync flush"u8.ToArray();

        RawDeflateEncoderRun encoded = EncodeAll(
            source,
            3,
            1,
            15,
            false);
        RawDeflateEncoderDecoded decoded = DecodeAll(
            encoded.Output.ToArray(),
            15,
            100);
        int count = encoded.Output.Count;

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Result.StreamComplete, Is.False);
            Assert.That(encoded.Result.SyncFlushComplete, Is.True);
            Assert.That(encoded.Output.GetRange(count - 4, 4),
                Is.EqualTo(new byte[] { 0x00, 0x00, 0xff, 0xff }));
            Assert.That(decoded.Result.SyncFlushComplete, Is.True);
            Assert.That(decoded.Output, Is.EqualTo(source));
        });
    }

    [Test]
    public void EmptySyncFlushProducesCanonicalEmptyStoredBlock()
    {
        RawDeflateEncoderRun encoded = EncodeAll(
            [],
            1,
            1,
            8,
            false);
        RawDeflateEncoderDecoded decoded = DecodeAll(
            encoded.Output.ToArray(),
            8,
            0);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Output,
                Is.EqualTo(new byte[] { 0x00, 0x00, 0x00, 0xff, 0xff }));
            Assert.That(decoded.Result.SyncFlushComplete, Is.True);
            Assert.That(decoded.Output, Is.Empty);
        });
    }

    [Test]
    public void RetainedHistoryCompressesAndDecodesNextMessage()
    {
        byte[] message =
            "abcdefghijabcdefghijabcdefghijabcdefghij"u8.ToArray();
        var encoder = new RawDeflateEncoder(15);
        RawDeflateEncoderRun first = EncodeWithEncoder(
            encoder,
            message,
            5,
            2,
            false);
        encoder.ResetRawDeflateEncoder(true);
        RawDeflateEncoderRun second = EncodeWithEncoder(
            encoder,
            message,
            4,
            1,
            false);
        var decoder = new RawDeflateDecoder(15, 100);
        RawDeflateEncoderDecoded firstDecoded = DecodeWithDecoder(
            decoder,
            first.Output.ToArray(),
            100);
        decoder.ResetRawDeflateDecoder(true);
        RawDeflateEncoderDecoded secondDecoded = DecodeWithDecoder(
            decoder,
            second.Output.ToArray(),
            100);

        Assert.Multiple(() =>
        {
            Assert.That(firstDecoded.Output, Is.EqualTo(message));
            Assert.That(secondDecoded.Output, Is.EqualTo(message));
            Assert.That(second.Output.Count, Is.LessThan(first.Output.Count));
            Assert.That(encoder.GetRawDeflateEncoderHistoryCount(),
                Is.EqualTo(message.Length * 2));
        });
    }

    [Test]
    public void NegotiatedWindowConstrainsEmittedMatchDistance()
    {
        byte[] prefix = RandomBytes(300, 9876);
        var source = new byte[prefix.Length * 2];
        prefix.CopyTo(source, 0);
        prefix.CopyTo(source, prefix.Length);

        RawDeflateEncoderRun windowNine = EncodeAll(
            source,
            source.Length,
            1000,
            9,
            true);
        RawDeflateEncoderRun windowEight = EncodeAll(
            source,
            source.Length,
            1000,
            8,
            true);
        RawDeflateEncoderDecoded valid = DecodeAll(
            windowNine.Output.ToArray(),
            9,
            source.Length);
        RawDeflateEncoderDecoded tooSmall = DecodeAll(
            windowNine.Output.ToArray(),
            8,
            source.Length);
        RawDeflateEncoderDecoded constrained = DecodeAll(
            windowEight.Output.ToArray(),
            8,
            source.Length);

        Assert.Multiple(() =>
        {
            Assert.That(valid.Output, Is.EqualTo(source));
            Assert.That(tooSmall.Result.Status,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(tooSmall.Result.ErrorIdentifier,
                Is.EqualTo("deflate-distance-invalid"));
            Assert.That(constrained.Output, Is.EqualTo(source));
            Assert.That(windowNine.Output.Count,
                Is.LessThan(windowEight.Output.Count));
        });
    }

    [Test]
    public void MaximumWindowEncodesMatchesBeyondTheFourteenBitWindow()
    {
        byte[] prefix = RandomBytes(17000, 24680);
        var source = new byte[prefix.Length * 2];
        prefix.CopyTo(source, 0);
        prefix.CopyTo(source, prefix.Length);

        RawDeflateEncoderRun windowFifteen = EncodeAll(
            source,
            977,
            101,
            15,
            true);
        RawDeflateEncoderRun windowFourteen = EncodeAll(
            source,
            977,
            101,
            14,
            true);
        RawDeflateEncoderDecoded valid = DecodeAll(
            windowFifteen.Output.ToArray(),
            15,
            source.Length);
        RawDeflateEncoderDecoded insufficientWindow = DecodeAll(
            windowFifteen.Output.ToArray(),
            14,
            source.Length);
        RawDeflateEncoderDecoded constrained = DecodeAll(
            windowFourteen.Output.ToArray(),
            14,
            source.Length);

        Assert.Multiple(() =>
        {
            Assert.That(valid.Output, Is.EqualTo(source));
            Assert.That(insufficientWindow.Result.Status,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(insufficientWindow.Result.ErrorIdentifier,
                Is.EqualTo("deflate-distance-invalid"));
            Assert.That(constrained.Result.StreamComplete, Is.True);
            Assert.That(constrained.Output, Is.EqualTo(source));
            Assert.That(windowFifteen.Output.Count,
                Is.LessThan(windowFourteen.Output.Count));
        });
    }

    [Test]
    public void FinalizedEncoderRejectsAdditionalInputWithStickyError()
    {
        RawDeflateEncoderRun encoded = EncodeAll(
            "done"u8.ToArray(),
            4,
            10,
            8,
            true);

        RawDeflateEncodeResult invalid = encoded.Encoder.EncodeRawDeflate(
            new byte[] { 1 },
            new byte[1]);
        RawDeflateEncodeResult repeated = encoded.Encoder.EncodeRawDeflate(
            [],
            []);

        Assert.Multiple(() =>
        {
            Assert.That(invalid.Status,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(invalid.ErrorIdentifier,
                Is.EqualTo("deflate-encoder-state-invalid"));
            Assert.That(repeated.Status, Is.EqualTo(invalid.Status));
            Assert.That(repeated.ErrorIdentifier,
                Is.EqualTo(invalid.ErrorIdentifier));
        });
    }

    static RawDeflateEncoderRun EncodeAll(
        byte[] source,
        int inputChunkSize,
        int outputChunkSize,
        int windowBits,
        bool finalStream)
    {
        var encoder = new RawDeflateEncoder(windowBits);
        return EncodeWithEncoder(
            encoder,
            source,
            inputChunkSize,
            outputChunkSize,
            finalStream);
    }

    static RawDeflateEncoderRun EncodeWithEncoder(
        RawDeflateEncoder encoder,
        byte[] source,
        int inputChunkSize,
        int outputChunkSize,
        bool finalStream)
    {
        var output = new List<byte>();
        RawDeflateEncodeResult result = new RawDeflateEncodeResult();
        int sourceIndex = 0;
        int iteration = 0;
        while (sourceIndex < source.Length && iteration < 100000)
        {
            int sourceCount = source.Length - sourceIndex;
            if (sourceCount > inputChunkSize)
            {
                sourceCount = inputChunkSize;
            }

            var destination = new byte[outputChunkSize];
            result = encoder.EncodeRawDeflate(
                source.AsSpan(sourceIndex, sourceCount),
                destination);
            sourceIndex = sourceIndex + result.ConsumedCount;
            AppendProduced(output, destination, result.ProducedCount);
            iteration++;
        }

        result = new RawDeflateEncodeResult();
        result.Status = WebSocketParseStatus.NeedOutputCapacity;
        while (result.Status != WebSocketParseStatus.Complete &&
            iteration < 200000)
        {
            var destination = new byte[outputChunkSize];
            result = encoder.FinishRawDeflate(destination, finalStream);
            AppendProduced(output, destination, result.ProducedCount);
            iteration++;
        }

        Assert.That(iteration, Is.LessThan(200000));
        var run = new RawDeflateEncoderRun();
        run.Encoder = encoder;
        run.Result = result;
        run.Output = output;
        return run;
    }

    static RawDeflateEncoderDecoded DecodeAll(
        byte[] source,
        int windowBits,
        int maximumOutputLength)
    {
        var decoder = new RawDeflateDecoder(
            windowBits,
            maximumOutputLength);
        return DecodeWithDecoder(decoder, source, maximumOutputLength + 1);
    }

    static RawDeflateEncoderDecoded DecodeWithDecoder(
        RawDeflateDecoder decoder,
        byte[] source,
        int outputCapacity)
    {
        var output = new List<byte>();
        RawDeflateDecodeResult result = new RawDeflateDecodeResult();
        int sourceIndex = 0;
        int iteration = 0;
        while (result.Status != WebSocketParseStatus.Complete &&
            result.Status != WebSocketParseStatus.ProtocolFailure &&
            result.Status != WebSocketParseStatus.LimitRejected &&
            iteration < 100000)
        {
            var destination = new byte[outputCapacity];
            result = decoder.DecodeRawDeflate(
                source.AsSpan(sourceIndex),
                destination);
            sourceIndex = sourceIndex + result.ConsumedCount;
            AppendProduced(output, destination, result.ProducedCount);
            if (result.Status == WebSocketParseStatus.NeedInput &&
                sourceIndex == source.Length)
            {
                break;
            }

            iteration++;
        }

        Assert.That(iteration, Is.LessThan(100000));
        var decoded = new RawDeflateEncoderDecoded();
        decoded.Result = result;
        decoded.Output = output;
        return decoded;
    }

    static void AppendProduced(
        List<byte> output,
        byte[] source,
        int count)
    {
        int index = 0;
        while (index < count)
        {
            output.Add(source[index]);
            index++;
        }
    }

    static byte[] RandomBytes(int count, int seed)
    {
        var bytes = new byte[count];
        var random = new Random(seed);
        random.NextBytes(bytes);
        return bytes;
    }

    static byte[] RepetitiveBytes(int count)
    {
        var bytes = new byte[count];
        int index = 0;
        while (index < bytes.Length)
        {
            bytes[index] = (byte)('a' + ((index * 7) % 23));
            index++;
        }

        return bytes;
    }

    static byte[] InflateWithPlatform(byte[] source)
    {
        using var input = new MemoryStream(source);
        using var inflater = new DeflateStream(
            input,
            CompressionMode.Decompress);
        using var output = new MemoryStream();
        inflater.CopyTo(output);
        return output.ToArray();
    }
}

internal class RawDeflateEncoderRun
{
    internal RawDeflateEncoder Encoder = new RawDeflateEncoder(15);
    internal RawDeflateEncodeResult Result = new RawDeflateEncodeResult();
    internal List<byte> Output = [];
}

internal class RawDeflateEncoderDecoded
{
    internal RawDeflateDecodeResult Result = new RawDeflateDecodeResult();
    internal List<byte> Output = [];
}
