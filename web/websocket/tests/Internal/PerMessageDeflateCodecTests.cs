using websocket;

namespace websocket.Tests;

public class PerMessageDeflateCodecTests
{
    [TestCase("f248cdc9c90700")]
    [TestCase("000500faff48656c6c6f00")]
    [TestCase("f348cdc9c9070000")]
    [TestCase("f24805000000ffffcac9c90700")]
    public void DecodesPublishedRfc7692HelloVectorsOneByteAtATime(
        string payloadHex)
    {
        PerMessageDeflateCodec codec = CreateCodec(
            true,
            true,
            15,
            15,
            100);

        PerMessageDeflateTestRun decoded = DecompressMessage(
            codec,
            Convert.FromHexString(payloadHex),
            1,
            1);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Result.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(decoded.Result.MessageComplete, Is.True);
            Assert.That(decoded.Output, Is.EqualTo("Hello"u8.ToArray()));
        });
    }

    [Test]
    public void EncoderOutputMatchesPublishedRfc7692HelloVector()
    {
        PerMessageDeflateCodec codec = CreateCodec(
            false,
            false,
            15,
            15,
            100);

        PerMessageDeflateTestRun compressed = CompressMessage(
            codec,
            "Hello"u8.ToArray(),
            1,
            1);

        Assert.Multiple(() =>
        {
            Assert.That(compressed.Result.MessageComplete, Is.True);
            Assert.That(compressed.Output,
                Is.EqualTo(Convert.FromHexString("f248cdc9c90700")));
        });
    }

    [Test]
    public void DecodesPublishedRfc7692ContextTakeoverSequence()
    {
        PerMessageDeflateCodec codec = CreateCodec(
            true,
            true,
            15,
            15,
            100);

        PerMessageDeflateTestRun first = DecompressMessage(
            codec,
            Convert.FromHexString("f248cdc9c90700"),
            1,
            1);
        PerMessageDeflateTestRun second = DecompressMessage(
            codec,
            Convert.FromHexString("f200110000"),
            1,
            1);

        Assert.Multiple(() =>
        {
            Assert.That(first.Result.MessageComplete, Is.True);
            Assert.That(first.Output, Is.EqualTo("Hello"u8.ToArray()));
            Assert.That(second.Result.MessageComplete, Is.True);
            Assert.That(second.Output, Is.EqualTo("Hello"u8.ToArray()));
            Assert.That(codec.GetIncomingCompressionHistoryCount(),
                Is.EqualTo(10));
        });
    }

    [Test]
    public void FinalBlockRequiresExactlyOneZeroPaddingByte()
    {
        PerMessageDeflateCodec missing = CreateCodec(
            true,
            true,
            15,
            15,
            100);
        PerMessageDeflateCodec nonzero = CreateCodec(
            true,
            true,
            15,
            15,
            100);
        PerMessageDeflateCodec extra = CreateCodec(
            true,
            true,
            15,
            15,
            100);
        byte[] finalBlock = Convert.FromHexString("f348cdc9c90700");
        missing.DecodeCompressedBytes(finalBlock, new byte[100]);

        PerMessageDeflateCodecResult missingResult =
            missing.FinishDecompressedBytes(new byte[100]);
        PerMessageDeflateCodecResult nonzeroResult =
            nonzero.DecodeCompressedBytes(
                Convert.FromHexString("f348cdc9c9070001"),
                new byte[100]);
        PerMessageDeflateCodecResult extraResult =
            extra.DecodeCompressedBytes(
                Convert.FromHexString("f348cdc9c907000000"),
                new byte[100]);

        Assert.Multiple(() =>
        {
            Assert.That(missingResult.Status,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(nonzeroResult.Status,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(extraResult.Status,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(missingResult.ErrorIdentifier,
                Is.EqualTo("permessage-deflate-final-block-invalid"));
            Assert.That(nonzeroResult.ErrorIdentifier,
                Is.EqualTo(missingResult.ErrorIdentifier));
            Assert.That(extraResult.ErrorIdentifier,
                Is.EqualTo(missingResult.ErrorIdentifier));
        });
    }

    [Test]
    public void SplitFinalBlockRejectsLateInvalidPaddingAndTrailingBytes()
    {
        byte[] finalBlock = Convert.FromHexString("f348cdc9c90700");
        PerMessageDeflateCodec nonzeroPadding = CreateCodec(
            true,
            true,
            15,
            15,
            100);
        PerMessageDeflateCodec trailingBytes = CreateCodec(
            true,
            true,
            15,
            15,
            100);

        PerMessageDeflateCodecResult firstNonzero =
            nonzeroPadding.DecodeCompressedBytes(
                finalBlock,
                new byte[100]);
        PerMessageDeflateCodecResult invalidPadding =
            nonzeroPadding.DecodeCompressedBytes(
                [1],
                new byte[100]);
        PerMessageDeflateCodecResult firstTrailing =
            trailingBytes.DecodeCompressedBytes(
                finalBlock,
                new byte[100]);
        PerMessageDeflateCodecResult padding =
            trailingBytes.DecodeCompressedBytes(
                [0],
                new byte[100]);
        PerMessageDeflateCodecResult trailing =
            trailingBytes.DecodeCompressedBytes(
                [1],
                new byte[100]);

        Assert.Multiple(() =>
        {
            Assert.That(firstNonzero.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(firstNonzero.MessageComplete, Is.False);
            Assert.That(invalidPadding.Status,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(invalidPadding.ErrorIdentifier,
                Is.EqualTo("permessage-deflate-final-block-invalid"));
            Assert.That(firstTrailing.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(padding.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(padding.ConsumedCount, Is.EqualTo(1));
            Assert.That(trailing.Status,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(trailing.ErrorIdentifier,
                Is.EqualTo("permessage-deflate-final-block-invalid"));
        });
    }

    [Test]
    public void CompressionRemovesTailAndDecompressionRestoresIt()
    {
        byte[] source = "tail removal across tiny buffers"u8.ToArray();
        PerMessageDeflateCodec codec = CreateCodec(true, true, 15, 15, 1000);

        PerMessageDeflateTestRun compressed = CompressMessage(
            codec,
            source,
            2,
            1);
        PerMessageDeflateTestRun decompressed = DecompressMessage(
            codec,
            compressed.Output.ToArray(),
            1,
            2);
        var restored = new List<byte>(compressed.Output);
        restored.AddRange(new byte[] { 0x00, 0x00, 0xff, 0xff });
        RawDeflateDecodeResult rawResult = DecodeRestoredTail(
            restored.ToArray(),
            source.Length);

        Assert.Multiple(() =>
        {
            Assert.That(compressed.Result.MessageComplete, Is.True);
            Assert.That(compressed.Output.TakeLast(4),
                Is.Not.EqualTo(new byte[] { 0x00, 0x00, 0xff, 0xff }));
            Assert.That(decompressed.Result.MessageComplete, Is.True);
            Assert.That(decompressed.Output, Is.EqualTo(source));
            Assert.That(rawResult.SyncFlushComplete, Is.True);
        });
    }

    [Test]
    public void EmptyMessageUsesOneBytePayloadAndRoundTrips()
    {
        PerMessageDeflateCodec codec = CreateCodec(true, true, 8, 8, 0);

        PerMessageDeflateTestRun compressed = CompressMessage(
            codec,
            [],
            1,
            1);
        PerMessageDeflateTestRun decompressed = DecompressMessage(
            codec,
            compressed.Output.ToArray(),
            1,
            1);

        Assert.Multiple(() =>
        {
            Assert.That(compressed.Output, Is.EqualTo(new byte[] { 0x00 }));
            Assert.That(compressed.Result.MessageComplete, Is.True);
            Assert.That(decompressed.Output, Is.Empty);
            Assert.That(decompressed.Result.MessageComplete, Is.True);
        });
    }

    [Test]
    public void DirectionSettingsResetOnlyConfiguredHistory()
    {
        byte[] source = "direction specific history"u8.ToArray();
        PerMessageDeflateCodec codec = CreateCodec(
            false,
            true,
            15,
            15,
            1000);

        PerMessageDeflateTestRun compressed = CompressMessage(
            codec,
            source,
            source.Length,
            100);
        PerMessageDeflateTestRun decompressed = DecompressMessage(
            codec,
            compressed.Output.ToArray(),
            compressed.Output.Count,
            100);

        Assert.Multiple(() =>
        {
            Assert.That(decompressed.Output, Is.EqualTo(source));
            Assert.That(codec.GetOutgoingCompressionHistoryCount(), Is.Zero);
            Assert.That(codec.GetIncomingCompressionHistoryCount(),
                Is.EqualTo(source.Length));
        });
    }

    [Test]
    public void TakeoverHistorySurvivesInterveningUncompressedMessages()
    {
        byte[] compressedMessage =
            "abcdefghijabcdefghijabcdefghijabcdefghij"u8.ToArray();
        byte[] uncompressedMessage =
            "this message must not enter deflate history"u8.ToArray();
        PerMessageDeflateCodec codec = CreateCodec(
            true,
            true,
            15,
            15,
            1000);

        PerMessageDeflateTestRun first = CompressMessage(
            codec,
            compressedMessage,
            3,
            2);
        WebSocketParseStatus outgoingUncompressed =
            codec.CompleteOutgoingUncompressedMessage();
        PerMessageDeflateTestRun second = CompressMessage(
            codec,
            compressedMessage,
            4,
            1);
        PerMessageDeflateTestRun firstDecoded = DecompressMessage(
            codec,
            first.Output.ToArray(),
            2,
            3);
        WebSocketParseStatus incomingUncompressed =
            codec.CompleteIncomingUncompressedMessage();
        PerMessageDeflateTestRun secondDecoded = DecompressMessage(
            codec,
            second.Output.ToArray(),
            1,
            2);

        Assert.Multiple(() =>
        {
            Assert.That(outgoingUncompressed,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(incomingUncompressed,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(second.Output.Count, Is.LessThan(first.Output.Count));
            Assert.That(firstDecoded.Output, Is.EqualTo(compressedMessage));
            Assert.That(secondDecoded.Output, Is.EqualTo(compressedMessage));
            Assert.That(codec.GetOutgoingCompressionHistoryCount(),
                Is.EqualTo(compressedMessage.Length * 2));
            Assert.That(codec.GetIncomingCompressionHistoryCount(),
                Is.EqualTo(compressedMessage.Length * 2));
            Assert.That(uncompressedMessage.Length, Is.GreaterThan(0));
        });
    }

    [Test]
    public void NoTakeoverProducesIndependentlyDecodableMessages()
    {
        byte[] source =
            "independent independent independent"u8.ToArray();
        PerMessageDeflateCodec sender = CreateCodec(
            false,
            false,
            15,
            15,
            1000);
        PerMessageDeflateTestRun first = CompressMessage(
            sender,
            source,
            10,
            10);
        PerMessageDeflateTestRun second = CompressMessage(
            sender,
            source,
            10,
            10);
        PerMessageDeflateCodec freshReceiver = CreateCodec(
            false,
            false,
            15,
            15,
            1000);

        PerMessageDeflateTestRun decoded = DecompressMessage(
            freshReceiver,
            second.Output.ToArray(),
            10,
            10);

        Assert.Multiple(() =>
        {
            Assert.That(first.Output, Is.EqualTo(second.Output));
            Assert.That(decoded.Output, Is.EqualTo(source));
            Assert.That(sender.GetOutgoingCompressionHistoryCount(), Is.Zero);
            Assert.That(freshReceiver.GetIncomingCompressionHistoryCount(),
                Is.Zero);
        });
    }

    [Test]
    public void UnremovedTailIsRejectedWithStickyDirectionLocalFailure()
    {
        byte[] source = "tail must be stripped"u8.ToArray();
        PerMessageDeflateCodec sender = CreateCodec(
            true,
            true,
            15,
            15,
            1000);
        PerMessageDeflateTestRun compressed = CompressMessage(
            sender,
            source,
            source.Length,
            100);
        var invalid = new List<byte>(compressed.Output);
        invalid.AddRange(new byte[] { 0x00, 0x00, 0xff, 0xff });
        PerMessageDeflateCodec receiver = CreateCodec(
            true,
            true,
            15,
            15,
            1000);

        PerMessageDeflateCodecResult pending =
            receiver.DecodeCompressedBytes(
                invalid.ToArray(),
                new byte[100]);
        PerMessageDeflateCodecResult failure =
            receiver.FinishDecompressedBytes(new byte[100]);
        PerMessageDeflateCodecResult repeated =
            receiver.FinishDecompressedBytes([]);
        PerMessageDeflateTestRun outgoingStillWorks = CompressMessage(
            receiver,
            "other direction"u8.ToArray(),
            5,
            5);

        Assert.Multiple(() =>
        {
            Assert.That(pending.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(failure.Status,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(failure.ErrorIdentifier,
                Is.EqualTo("permessage-deflate-tail-not-removed"));
            Assert.That(repeated.ErrorIdentifier,
                Is.EqualTo(failure.ErrorIdentifier));
            Assert.That(outgoingStillWorks.Result.MessageComplete, Is.True);
        });
    }

    [Test]
    public void MissingCompressedEmptyHeaderIsRejectedAtFinish()
    {
        PerMessageDeflateCodec codec = CreateCodec(
            true,
            true,
            8,
            8,
            100);
        codec.DecodeCompressedBytes([], []);

        PerMessageDeflateCodecResult failure =
            codec.FinishDecompressedBytes(new byte[10]);

        Assert.Multiple(() =>
        {
            Assert.That(failure.Status,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(failure.ErrorIdentifier,
                Is.EqualTo("deflate-input-truncated"));
        });
    }

    [Test]
    public void DecompressedLimitFailureIsPreserved()
    {
        byte[] source = "too long"u8.ToArray();
        PerMessageDeflateCodec sender = CreateCodec(
            true,
            true,
            8,
            8,
            100);
        PerMessageDeflateTestRun compressed = CompressMessage(
            sender,
            source,
            source.Length,
            100);
        PerMessageDeflateCodec receiver = CreateCodec(
            true,
            true,
            8,
            8,
            source.Length - 1);

        PerMessageDeflateTestRun decoded = DecompressMessage(
            receiver,
            compressed.Output.ToArray(),
            compressed.Output.Count,
            100);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Result.Status,
                Is.EqualTo(WebSocketParseStatus.LimitRejected));
            Assert.That(decoded.Result.ErrorIdentifier,
                Is.EqualTo("deflate-output-limit-exceeded"));
            Assert.That(decoded.Output.Count,
                Is.LessThanOrEqualTo(source.Length - 1));
        });
    }

    [Test]
    public void HighlyExpandingPayloadStopsAtConfiguredLimit()
    {
        var source = new byte[100000];
        source.AsSpan().Fill((byte)'A');
        PerMessageDeflateCodec sender = CreateCodec(
            true,
            true,
            15,
            15,
            source.Length);
        PerMessageDeflateTestRun compressed = CompressMessage(
            sender,
            source,
            997,
            7);
        PerMessageDeflateCodec receiver = CreateCodec(
            true,
            true,
            15,
            15,
            1024);

        PerMessageDeflateTestRun decoded = DecompressMessage(
            receiver,
            compressed.Output.ToArray(),
            1,
            31);

        Assert.Multiple(() =>
        {
            Assert.That(compressed.Output.Count, Is.LessThan(1000));
            Assert.That(decoded.Result.Status,
                Is.EqualTo(WebSocketParseStatus.LimitRejected));
            Assert.That(decoded.Result.ErrorIdentifier,
                Is.EqualTo("deflate-output-limit-exceeded"));
            Assert.That(decoded.Output.Count, Is.LessThanOrEqualTo(1024));
        });
    }

    static PerMessageDeflateCodec CreateCodec(
        bool outgoingTakeover,
        bool incomingTakeover,
        int outgoingWindowBits,
        int incomingWindowBits,
        int maximumDecompressedLength)
    {
        var settings = new PerMessageDeflateDirectionSettings();
        settings.Valid = true;
        settings.OutgoingContextTakeover = outgoingTakeover;
        settings.IncomingContextTakeover = incomingTakeover;
        settings.OutgoingWindowBits = outgoingWindowBits;
        settings.IncomingWindowBits = incomingWindowBits;
        return new PerMessageDeflateCodec(
            settings,
            maximumDecompressedLength);
    }

    static PerMessageDeflateTestRun CompressMessage(
        PerMessageDeflateCodec codec,
        byte[] source,
        int inputChunkSize,
        int outputChunkSize)
    {
        var output = new List<byte>();
        PerMessageDeflateCodecResult result =
            new PerMessageDeflateCodecResult();
        int sourceIndex = 0;
        int iteration = 0;
        while ((sourceIndex < source.Length ||
                result.Status == WebSocketParseStatus.NeedOutputCapacity) &&
            iteration < 100000)
        {
            int sourceCount = source.Length - sourceIndex;
            if (sourceCount > inputChunkSize)
            {
                sourceCount = inputChunkSize;
            }

            var destination = new byte[outputChunkSize];
            result = codec.EncodeCompressedBytes(
                source.AsSpan(sourceIndex, sourceCount),
                destination);
            sourceIndex = sourceIndex + result.ConsumedCount;
            AppendProduced(output, destination, result.ProducedCount);
            iteration++;
        }

        result = new PerMessageDeflateCodecResult();
        result.Status = WebSocketParseStatus.NeedOutputCapacity;
        while (result.Status == WebSocketParseStatus.NeedOutputCapacity &&
            iteration < 100000)
        {
            var destination = new byte[outputChunkSize];
            result = codec.FinishCompressedBytes(destination);
            AppendProduced(output, destination, result.ProducedCount);
            iteration++;
        }

        Assert.That(iteration, Is.LessThan(100000));
        var run = new PerMessageDeflateTestRun();
        run.Result = result;
        run.Output = output;
        return run;
    }

    static PerMessageDeflateTestRun DecompressMessage(
        PerMessageDeflateCodec codec,
        byte[] source,
        int inputChunkSize,
        int outputChunkSize)
    {
        var output = new List<byte>();
        PerMessageDeflateCodecResult result =
            new PerMessageDeflateCodecResult();
        int sourceIndex = 0;
        int iteration = 0;
        while ((sourceIndex < source.Length ||
                result.Status == WebSocketParseStatus.NeedOutputCapacity) &&
            result.Status != WebSocketParseStatus.ProtocolFailure &&
            result.Status != WebSocketParseStatus.LimitRejected &&
            iteration < 100000)
        {
            int sourceCount = source.Length - sourceIndex;
            if (sourceCount > inputChunkSize)
            {
                sourceCount = inputChunkSize;
            }

            var destination = new byte[outputChunkSize];
            result = codec.DecodeCompressedBytes(
                source.AsSpan(sourceIndex, sourceCount),
                destination);
            sourceIndex = sourceIndex + result.ConsumedCount;
            AppendProduced(output, destination, result.ProducedCount);
            iteration++;
        }

        if (result.Status != WebSocketParseStatus.ProtocolFailure &&
            result.Status != WebSocketParseStatus.LimitRejected)
        {
            result = new PerMessageDeflateCodecResult();
            result.Status = WebSocketParseStatus.NeedOutputCapacity;
            while (result.Status == WebSocketParseStatus.NeedOutputCapacity &&
                iteration < 100000)
            {
                var destination = new byte[outputChunkSize];
                result = codec.FinishDecompressedBytes(destination);
                AppendProduced(output, destination, result.ProducedCount);
                iteration++;
            }
        }

        Assert.That(iteration, Is.LessThan(100000));
        var run = new PerMessageDeflateTestRun();
        run.Result = result;
        run.Output = output;
        return run;
    }

    static RawDeflateDecodeResult DecodeRestoredTail(
        byte[] source,
        int outputLength)
    {
        var decoder = new RawDeflateDecoder(15, outputLength);
        return decoder.DecodeRawDeflate(source, new byte[outputLength]);
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
}

internal class PerMessageDeflateTestRun
{
    internal PerMessageDeflateCodecResult Result =
        new PerMessageDeflateCodecResult();
    internal List<byte> Output = [];
}
