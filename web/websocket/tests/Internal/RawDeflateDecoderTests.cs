using websocket;

namespace websocket.Tests;

public class RawDeflateDecoderTests
{
    [Test]
    public void DecodesStoredBlockAcrossSingleByteInputAndSmallOutput()
    {
        byte[] compressed = Convert.FromHexString(
            "011200edff68656c6c6f2073746f72656420626c6f636b");
        byte[] expected = "hello stored block"u8.ToArray();

        RawDeflateDecoderRun run = DecodeAll(
            compressed,
            1,
            2,
            8,
            expected.Length);

        Assert.Multiple(() =>
        {
            Assert.That(run.Result.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(run.Result.StreamComplete, Is.True);
            Assert.That(run.Result.SyncFlushComplete, Is.False);
            Assert.That(run.ConsumedCount, Is.EqualTo(compressed.Length));
            Assert.That(run.Output, Is.EqualTo(expected));
        });
    }

    [Test]
    public void DecodesFixedBlockWithOverlappingMatchesIncrementally()
    {
        byte[] compressed = Convert.FromHexString(
            "4bcbac484d514843908a00");
        byte[] expected = "fixed fixed fixed!"u8.ToArray();

        RawDeflateDecoderRun run = DecodeAll(
            compressed,
            2,
            1,
            8,
            100);

        Assert.Multiple(() =>
        {
            Assert.That(run.Result.StreamComplete, Is.True);
            Assert.That(run.Output, Is.EqualTo(expected));
            Assert.That(run.Result.TotalOutputCount,
                Is.EqualTo(expected.Length));
        });
    }

    [Test]
    public void FinalStreamLeavesTrailingCallerBytesUnconsumed()
    {
        byte[] compressed = Convert.FromHexString(
            "4bcbac484d514843908a00");
        byte[] source = new byte[compressed.Length + 2];
        compressed.CopyTo(source, 0);
        source[source.Length - 2] = 0xaa;
        source[source.Length - 1] = 0xbb;
        var decoder = new RawDeflateDecoder(8, 100);
        var output = new byte[100];

        RawDeflateDecodeResult result = decoder.DecodeRawDeflate(
            source,
            output);

        Assert.Multiple(() =>
        {
            Assert.That(result.StreamComplete, Is.True);
            Assert.That(result.ConsumedCount, Is.EqualTo(compressed.Length));
            Assert.That(result.ProducedCount, Is.EqualTo(18));
            Assert.That(output.AsSpan(0, result.ProducedCount).ToArray(),
                Is.EqualTo("fixed fixed fixed!"u8.ToArray()));
        });
    }

    [Test]
    public void ZeroOutputCapacityRetainsDecodedLiteralForResume()
    {
        byte[] compressed = Convert.FromHexString(
            "4bcbac484d514843908a00");
        var decoder = new RawDeflateDecoder(8, 100);

        RawDeflateDecodeResult blocked = decoder.DecodeRawDeflate(
            compressed,
            []);
        var output = new byte[18];
        RawDeflateDecodeResult resumed = decoder.DecodeRawDeflate(
            compressed.AsSpan(blocked.ConsumedCount),
            output);

        Assert.Multiple(() =>
        {
            Assert.That(blocked.Status,
                Is.EqualTo(WebSocketParseStatus.NeedOutputCapacity));
            Assert.That(blocked.ProducedCount, Is.Zero);
            Assert.That(blocked.ConsumedCount, Is.GreaterThan(0));
            Assert.That(resumed.StreamComplete, Is.True);
            Assert.That(resumed.ProducedCount, Is.EqualTo(18));
            Assert.That(output, Is.EqualTo("fixed fixed fixed!"u8.ToArray()));
        });
    }

    [Test]
    public void DecodesDynamicBlockWithLongMatchesAndRingWrap()
    {
        byte[] compressed = Convert.FromHexString(
            "edc3870900200c00b05bebecff17788548258144000000000000000000407d0d" +
            "00000000b8ae030050ce00f8d65c3b555555d5b70f");
        var expected = new List<byte>();
        AppendRepeated(expected, (byte)'a', 10000);
        AppendRepeated(expected, (byte)'b', 5000);
        AppendRepeated(expected, (byte)'c', 2500);
        AppendRepeated(expected, (byte)'d', 1250);
        int suffixCount = 0;
        while (suffixCount < 1000)
        {
            expected.Add((byte)'e');
            expected.Add((byte)'f');
            expected.Add((byte)'g');
            expected.Add((byte)'h');
            suffixCount++;
        }

        RawDeflateDecoderRun run = DecodeAll(
            compressed,
            1,
            31,
            15,
            expected.Count);

        Assert.Multiple(() =>
        {
            Assert.That((compressed[0] >> 1) & 3, Is.EqualTo(2));
            Assert.That(run.Result.StreamComplete, Is.True);
            Assert.That(run.Output, Is.EqualTo(expected));
            Assert.That(run.Result.TotalOutputCount,
                Is.EqualTo(expected.Count));
        });
    }

    [TestCase("stored")]
    [TestCase("fixed")]
    [TestCase("dynamic")]
    public void DecodesEveryPossibleInputSplit(string blockKind)
    {
        byte[] compressed = CompressedVector(blockKind);
        byte[] expected = ExpectedVector(blockKind);

        for (var split = 0; split <= compressed.Length; split++)
        {
            var decoder = new RawDeflateDecoder(15, expected.Length);
            var output = new byte[expected.Length];
            RawDeflateDecodeResult first = decoder.DecodeRawDeflate(
                compressed.AsSpan(0, split),
                output);
            int consumed = first.ConsumedCount;
            int produced = first.ProducedCount;
            RawDeflateDecodeResult completed = first;
            if (!first.StreamComplete)
            {
                completed = decoder.DecodeRawDeflate(
                    compressed.AsSpan(consumed),
                    output.AsSpan(produced));
                consumed += completed.ConsumedCount;
                produced += completed.ProducedCount;
            }

            Assert.Multiple(() =>
            {
                Assert.That(completed.Status,
                    Is.EqualTo(WebSocketParseStatus.Complete),
                    blockKind + " input split " + split);
                Assert.That(completed.StreamComplete, Is.True,
                    blockKind + " input split " + split);
                Assert.That(consumed, Is.EqualTo(compressed.Length),
                    blockKind + " input split " + split);
                Assert.That(produced, Is.EqualTo(expected.Length),
                    blockKind + " input split " + split);
                Assert.That(output, Is.EqualTo(expected),
                    blockKind + " input split " + split);
            });
        }
    }

    [TestCase("stored")]
    [TestCase("fixed")]
    [TestCase("dynamic")]
    public void DecodesEveryPossibleOutputSplit(string blockKind)
    {
        byte[] compressed = CompressedVector(blockKind);
        byte[] expected = ExpectedVector(blockKind);

        for (var split = 0; split <= expected.Length; split++)
        {
            var decoder = new RawDeflateDecoder(15, expected.Length);
            var output = new byte[expected.Length];
            RawDeflateDecodeResult first = decoder.DecodeRawDeflate(
                compressed,
                output.AsSpan(0, split));
            int consumed = first.ConsumedCount;
            int produced = first.ProducedCount;
            RawDeflateDecodeResult completed = first;
            if (!first.StreamComplete)
            {
                completed = decoder.DecodeRawDeflate(
                    compressed.AsSpan(consumed),
                    output.AsSpan(produced));
                consumed += completed.ConsumedCount;
                produced += completed.ProducedCount;
            }

            Assert.Multiple(() =>
            {
                Assert.That(completed.Status,
                    Is.EqualTo(WebSocketParseStatus.Complete),
                    blockKind + " output split " + split);
                Assert.That(completed.StreamComplete, Is.True,
                    blockKind + " output split " + split);
                Assert.That(consumed, Is.EqualTo(compressed.Length),
                    blockKind + " output split " + split);
                Assert.That(produced, Is.EqualTo(expected.Length),
                    blockKind + " output split " + split);
                Assert.That(output, Is.EqualTo(expected),
                    blockKind + " output split " + split);
            });
        }
    }

    [TestCase(8)]
    [TestCase(9)]
    [TestCase(10)]
    [TestCase(11)]
    [TestCase(12)]
    [TestCase(13)]
    [TestCase(14)]
    [TestCase(15)]
    public void DecodesDynamicVectorAtEveryNegotiatedWindowSize(int windowBits)
    {
        byte[] compressed = CompressedVector("dynamic");
        byte[] expected = ExpectedVector("dynamic");

        RawDeflateDecoderRun run = DecodeAll(
            compressed,
            1,
            1,
            windowBits,
            expected.Length);

        Assert.Multiple(() =>
        {
            Assert.That(run.Result.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(run.Result.StreamComplete, Is.True);
            Assert.That(run.Output, Is.EqualTo(expected));
        });
    }

    [TestCase("stored")]
    [TestCase("fixed")]
    [TestCase("dynamic")]
    public void EnforcesExpansionLimitAtExactBoundaryForEveryBlockKind(
        string blockKind)
    {
        byte[] compressed = CompressedVector(blockKind);
        byte[] expected = ExpectedVector(blockKind);

        RawDeflateDecoderRun exact = DecodeAll(
            compressed,
            1,
            1,
            15,
            expected.Length);
        RawDeflateDecoderRun exceeded = DecodeAll(
            compressed,
            1,
            1,
            15,
            expected.Length - 1);

        Assert.Multiple(() =>
        {
            Assert.That(exact.Result.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(exact.Output, Is.EqualTo(expected));
            Assert.That(exceeded.Result.Status,
                Is.EqualTo(WebSocketParseStatus.LimitRejected));
            Assert.That(exceeded.Result.ErrorIdentifier,
                Is.EqualTo("deflate-output-limit-exceeded"));
            Assert.That(exceeded.Output.Count,
                Is.LessThanOrEqualTo(expected.Length - 1));
        });
    }

    [Test]
    public void HistoryCrossesFromStoredBlockIntoFixedMatchBlock()
    {
        byte[] stored =
        [
            0x00, 0x03, 0x00, 0xfc, 0xff,
            (byte)'A', (byte)'B', (byte)'C'
        ];
        var bits = new TestDeflateBitPacker();
        bits.WriteBits(1, 1);
        bits.WriteBits(1, 2);
        DeflateHuffmanTable literals =
            DeflateHuffmanTables.CreateFixedLiteralLengthTable();
        DeflateHuffmanTable distances =
            DeflateHuffmanTables.CreateFixedDistanceTable();
        bits.WriteHuffman(
            literals.Codes[257],
            literals.CodeLengths[257]);
        bits.WriteHuffman(
            distances.Codes[2],
            distances.CodeLengths[2]);
        bits.WriteHuffman(
            literals.Codes[256],
            literals.CodeLengths[256]);
        byte[] fixedBlock = bits.Finish();
        byte[] compressed = new byte[stored.Length + fixedBlock.Length];
        stored.CopyTo(compressed, 0);
        fixedBlock.CopyTo(compressed, stored.Length);

        RawDeflateDecoderRun run = DecodeAll(
            compressed,
            1,
            1,
            8,
            6);

        Assert.Multiple(() =>
        {
            Assert.That(run.Result.StreamComplete, Is.True);
            Assert.That(run.Output, Is.EqualTo("ABCABC"u8.ToArray()));
        });
    }

    [TestCase("0300")]
    [TestCase("010000ffff")]
    public void EmptyFinalStreamCompletesWithoutOutputCapacity(string hex)
    {
        var decoder = new RawDeflateDecoder(8, 0);

        RawDeflateDecodeResult result = decoder.DecodeRawDeflate(
            Convert.FromHexString(hex),
            []);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(result.StreamComplete, Is.True);
            Assert.That(result.ProducedCount, Is.Zero);
            Assert.That(result.TotalOutputCount, Is.Zero);
        });
    }

    [Test]
    public void NegativeOutputLimitIsRejectedAtConstruction()
    {
        Assert.Throws<WebSocketStateException>(
            () => new RawDeflateDecoder(8, -1));
    }

    [Test]
    public void RecognizesSyncFlushTailWithoutClaimingFinalStream()
    {
        byte[] compressed = Convert.FromHexString(
            "4acecf2b49ad28514846a501000000ffff");
        byte[] expected = "context context context"u8.ToArray();

        RawDeflateDecoderRun run = DecodeAll(
            compressed,
            3,
            4,
            15,
            100);
        RawDeflateDecodeResult repeated = run.Decoder.DecodeRawDeflate([], []);
        RawDeflateDecodeResult finished = run.Decoder.FinishRawDeflateInput();

        Assert.Multiple(() =>
        {
            Assert.That(run.Result.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(run.Result.StreamComplete, Is.False);
            Assert.That(run.Result.SyncFlushComplete, Is.True);
            Assert.That(run.Output, Is.EqualTo(expected));
            Assert.That(repeated.SyncFlushComplete, Is.True);
            Assert.That(finished.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(finished.SyncFlushComplete, Is.True);
        });
    }

    [Test]
    public void ResetCanRetainHistoryNeededByNextSyncFlushedMessage()
    {
        byte[] firstCompressed = Convert.FromHexString(
            "4a4c4a4e494d4bcfc8cc4a84b300000000ffff");
        byte[] secondCompressed = Convert.FromHexString(
            "c2c602000000ffff");
        byte[] expected = "abcdefghijabcdefghij"u8.ToArray();
        RawDeflateDecoder decoder = new RawDeflateDecoder(15, 100);
        RawDeflateDecoderRun first = DecodeWithDecoder(
            decoder,
            firstCompressed,
            2,
            3);

        decoder.ResetRawDeflateDecoder(true);
        RawDeflateDecoderRun second = DecodeWithDecoder(
            decoder,
            secondCompressed,
            1,
            2);

        Assert.Multiple(() =>
        {
            Assert.That(first.Output, Is.EqualTo(expected));
            Assert.That(first.Result.SyncFlushComplete, Is.True);
            Assert.That(second.Output, Is.EqualTo(expected));
            Assert.That(second.Result.SyncFlushComplete, Is.True);
            Assert.That(decoder.GetRawDeflateHistoryCount(),
                Is.EqualTo(expected.Length * 2));
        });
    }

    [Test]
    public void DiscardedHistoryRejectsAContextTakeoverDistance()
    {
        byte[] firstCompressed = Convert.FromHexString(
            "4a4c4a4e494d4bcfc8cc4a84b300000000ffff");
        byte[] secondCompressed = Convert.FromHexString(
            "c2c602000000ffff");
        RawDeflateDecoder decoder = new RawDeflateDecoder(15, 100);
        DecodeWithDecoder(decoder, firstCompressed, 20, 100);
        decoder.ResetRawDeflateDecoder(false);

        RawDeflateDecoderRun second = DecodeWithDecoder(
            decoder,
            secondCompressed,
            8,
            100);

        Assert.Multiple(() =>
        {
            Assert.That(second.Result.Status,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(second.Result.ErrorIdentifier,
                Is.EqualTo("deflate-distance-invalid"));
            Assert.That(decoder.GetRawDeflateHistoryCount(), Is.Zero);
        });
    }

    [Test]
    public void StoredBlockLimitIsRejectedBeforePayloadIsEmitted()
    {
        byte[] compressed = Convert.FromHexString(
            "011200edff68656c6c6f2073746f72656420626c6f636b");
        var decoder = new RawDeflateDecoder(8, 17);
        var output = new byte[18];

        RawDeflateDecodeResult rejected = decoder.DecodeRawDeflate(
            compressed,
            output);
        RawDeflateDecodeResult repeated = decoder.DecodeRawDeflate([], output);

        Assert.Multiple(() =>
        {
            Assert.That(rejected.Status,
                Is.EqualTo(WebSocketParseStatus.LimitRejected));
            Assert.That(rejected.ErrorIdentifier,
                Is.EqualTo("deflate-output-limit-exceeded"));
            Assert.That(rejected.ConsumedCount, Is.EqualTo(5));
            Assert.That(rejected.ProducedCount, Is.Zero);
            Assert.That(repeated.Status,
                Is.EqualTo(WebSocketParseStatus.LimitRejected));
            Assert.That(repeated.ErrorIdentifier,
                Is.EqualTo(rejected.ErrorIdentifier));
        });
    }

    [Test]
    public void CompressedMatchCannotCrossConfiguredOutputLimit()
    {
        byte[] compressed = Convert.FromHexString(
            "4bcbac484d514843908a00");
        RawDeflateDecoderRun run = DecodeAll(
            compressed,
            compressed.Length,
            20,
            8,
            17);

        Assert.Multiple(() =>
        {
            Assert.That(run.Result.Status,
                Is.EqualTo(WebSocketParseStatus.LimitRejected));
            Assert.That(run.Result.ErrorIdentifier,
                Is.EqualTo("deflate-output-limit-exceeded"));
            Assert.That(run.Output.Count, Is.LessThanOrEqualTo(17));
        });
    }

    [TestCase("reserved-block", "deflate-block-type-invalid")]
    [TestCase("stored-complement", "deflate-stored-length-invalid")]
    [TestCase("missing-distance", "deflate-distance-invalid")]
    [TestCase("reserved-literal", "deflate-literal-length-symbol-invalid")]
    [TestCase("reserved-distance", "deflate-distance-symbol-invalid")]
    [TestCase("malformed-tree", "deflate-huffman-tree-invalid")]
    public void MalformedStreamsHaveDeterministicStickyErrors(
        string mutation,
        string expectedIdentifier)
    {
        byte[] compressed = MalformedStream(mutation);
        var decoder = new RawDeflateDecoder(15, 1000);
        var output = new byte[1000];

        RawDeflateDecodeResult failure = decoder.DecodeRawDeflate(
            compressed,
            output);
        RawDeflateDecodeResult repeated = decoder.DecodeRawDeflate([], []);

        Assert.Multiple(() =>
        {
            Assert.That(failure.Status,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(failure.ErrorIdentifier,
                Is.EqualTo(expectedIdentifier));
            Assert.That(repeated.Status, Is.EqualTo(failure.Status));
            Assert.That(repeated.ErrorIdentifier,
                Is.EqualTo(expectedIdentifier));
        });
    }

    [Test]
    public void ExplicitInputFinishRejectsTruncatedStreamAndResetRecovers()
    {
        var decoder = new RawDeflateDecoder(8, 100);
        RawDeflateDecodeResult partial = decoder.DecodeRawDeflate(
            new byte[] { 0x03 },
            new byte[10]);
        RawDeflateDecodeResult truncated = decoder.FinishRawDeflateInput();
        decoder.ResetRawDeflateDecoder(false);
        RawDeflateDecoderRun recovered = DecodeWithDecoder(
            decoder,
            Convert.FromHexString("4bcbac484d514843908a00"),
            2,
            2);

        Assert.Multiple(() =>
        {
            Assert.That(partial.Status,
                Is.EqualTo(WebSocketParseStatus.NeedInput));
            Assert.That(truncated.Status,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(truncated.ErrorIdentifier,
                Is.EqualTo("deflate-input-truncated"));
            Assert.That(recovered.Result.StreamComplete, Is.True);
            Assert.That(recovered.Output,
                Is.EqualTo("fixed fixed fixed!"u8.ToArray()));
        });
    }

    static RawDeflateDecoderRun DecodeAll(
        byte[] compressed,
        int inputChunkSize,
        int outputChunkSize,
        int windowBits,
        int maximumOutputLength)
    {
        var decoder = new RawDeflateDecoder(
            windowBits,
            maximumOutputLength);
        return DecodeWithDecoder(
            decoder,
            compressed,
            inputChunkSize,
            outputChunkSize);
    }

    static RawDeflateDecoderRun DecodeWithDecoder(
        RawDeflateDecoder decoder,
        byte[] compressed,
        int inputChunkSize,
        int outputChunkSize)
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
            int sourceCount = compressed.Length - sourceIndex;
            if (sourceCount > inputChunkSize)
            {
                sourceCount = inputChunkSize;
            }

            var destination = new byte[outputChunkSize];
            result = decoder.DecodeRawDeflate(
                compressed.AsSpan(sourceIndex, sourceCount),
                destination);
            sourceIndex = sourceIndex + result.ConsumedCount;
            int outputIndex = 0;
            while (outputIndex < result.ProducedCount)
            {
                output.Add(destination[outputIndex]);
                outputIndex++;
            }

            if (result.Status == WebSocketParseStatus.NeedInput &&
                sourceIndex == compressed.Length)
            {
                break;
            }

            iteration++;
        }

        Assert.That(iteration, Is.LessThan(100000));
        var run = new RawDeflateDecoderRun();
        run.Decoder = decoder;
        run.Result = result;
        run.Output = output;
        run.ConsumedCount = sourceIndex;
        return run;
    }

    static void AppendRepeated(List<byte> destination, byte value, int count)
    {
        int index = 0;
        while (index < count)
        {
            destination.Add(value);
            index++;
        }
    }

    static byte[] CompressedVector(string blockKind)
    {
        if (blockKind == "stored")
        {
            return Convert.FromHexString(
                "011200edff68656c6c6f2073746f72656420626c6f636b");
        }

        if (blockKind == "fixed")
        {
            return Convert.FromHexString("4bcbac484d514843908a00");
        }

        return Convert.FromHexString(
            "0dcad10e84200c45c16f2de134162235705df1efd7c749e6b2fb6728d2da" +
            "f92a6df44d226e966bc7f4d13f55fa752803338db5f97a09e12d1ad4d7e" +
            "6731e8b914f91aa668116e07824e80f");
    }

    static byte[] ExpectedVector(string blockKind)
    {
        if (blockKind == "stored")
        {
            return "hello stored block"u8.ToArray();
        }

        if (blockKind == "fixed")
        {
            return "fixed fixed fixed!"u8.ToArray();
        }

        return Convert.FromHexString(
            "70617576616574696f616a6d79746f616e6b78656f65746575657366747869" +
            "72666e6b65746564656b7068746f69656161746e737865696f616269746566" +
            "6a696a656564796172776d6873656e6f776274746474726265656a69656566" +
            "6566696f656574");
    }

    static byte[] MalformedStream(string mutation)
    {
        if (mutation == "reserved-block")
        {
            return [0x07];
        }

        if (mutation == "stored-complement")
        {
            return [0x01, 0x00, 0x00, 0x00, 0x00];
        }

        var bits = new TestDeflateBitPacker();
        bits.WriteBits(1, 1);
        if (mutation == "malformed-tree")
        {
            bits.WriteBits(2, 2);
            bits.WriteBits(0, 5);
            bits.WriteBits(0, 5);
            bits.WriteBits(0, 4);
            bits.WriteBits(1, 3);
            bits.WriteBits(0, 3);
            bits.WriteBits(0, 3);
            bits.WriteBits(1, 3);
            bits.WriteHuffman(1, 1);
            return bits.Finish();
        }

        bits.WriteBits(1, 2);
        DeflateHuffmanTable literals =
            DeflateHuffmanTables.CreateFixedLiteralLengthTable();
        DeflateHuffmanTable distances =
            DeflateHuffmanTables.CreateFixedDistanceTable();
        if (mutation == "reserved-literal")
        {
            bits.WriteHuffman(
                literals.Codes[286],
                literals.CodeLengths[286]);
            return bits.Finish();
        }

        if (mutation == "reserved-distance")
        {
            bits.WriteHuffman(
                literals.Codes[65],
                literals.CodeLengths[65]);
            bits.WriteHuffman(
                literals.Codes[257],
                literals.CodeLengths[257]);
            bits.WriteHuffman(
                distances.Codes[30],
                distances.CodeLengths[30]);
            return bits.Finish();
        }

        bits.WriteHuffman(
            literals.Codes[257],
            literals.CodeLengths[257]);
        bits.WriteHuffman(
            distances.Codes[0],
            distances.CodeLengths[0]);
        return bits.Finish();
    }
}

internal class RawDeflateDecoderRun
{
    internal RawDeflateDecoder Decoder = new RawDeflateDecoder(15, 1);
    internal RawDeflateDecodeResult Result = new RawDeflateDecodeResult();
    internal List<byte> Output = [];
    internal int ConsumedCount;
}
