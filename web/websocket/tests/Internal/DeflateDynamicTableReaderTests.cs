using websocket;

namespace websocket.Tests;

public class DeflateDynamicTableReaderTests
{
    [Test]
    public void ReadsMinimalLiteralOnlyTablesOneByteAtATimeAndLeavesEndCode()
    {
        byte[] source = MinimalLiteralOnlyBlock();
        var bitReader = new DeflateBitReader();
        var tableReader = new DeflateDynamicTableReader(bitReader);
        DeflateDynamicTableResult result = new DeflateDynamicTableResult();
        int sourceOffset = 0;
        while (result.Status == WebSocketParseStatus.NeedInput &&
            sourceOffset < source.Length)
        {
            result = tableReader.ReadDynamicTables(
                source.AsSpan(sourceOffset, 1));
            sourceOffset = sourceOffset + result.ConsumedCount;
        }

        var decoder = new DeflateHuffmanDecoder(result.LiteralLengthTable);
        var end = decoder.ReadSymbol(bitReader, []);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(result.LiteralLengthTable.CodeLengths,
                Has.Length.EqualTo(257));
            Assert.That(result.LiteralLengthTable.CodeLengths[256], Is.EqualTo(1));
            Assert.That(result.DistanceTable.Valid, Is.True);
            Assert.That(result.DistanceTable.MaximumCodeLength, Is.Zero);
            Assert.That(end.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(end.Symbol, Is.EqualTo(256));
        });
    }

    [TestCase("first-repeat")]
    [TestCase("repeat-overflow")]
    [TestCase("missing-end")]
    [TestCase("incomplete-code-tree")]
    public void RejectsMalformedDynamicTrees(string mutation)
    {
        byte[] source;
        if (mutation == "first-repeat")
        {
            source = FirstRepeatBlock();
        }
        else if (mutation == "repeat-overflow")
        {
            source = RepeatOverflowBlock();
        }
        else if (mutation == "missing-end")
        {
            source = MissingEndBlock();
        }
        else
        {
            source = IncompleteCodeLengthTreeBlock();
        }

        var reader = new DeflateDynamicTableReader(new DeflateBitReader());

        var result = reader.ReadDynamicTables(source);
        var repeated = reader.ReadDynamicTables([]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(repeated.Status,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
        });
    }

    [Test]
    public void ResetAllowsAnotherDynamicTableAfterFailure()
    {
        var bitReader = new DeflateBitReader();
        var reader = new DeflateDynamicTableReader(bitReader);
        reader.ReadDynamicTables(FirstRepeatBlock());
        bitReader.ResetBitReader();
        reader.ResetDynamicTableReader();

        var result = reader.ReadDynamicTables(MinimalLiteralOnlyBlock());

        Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.Complete));
    }

    static byte[] MinimalLiteralOnlyBlock()
    {
        var bits = DynamicHeader(14);
        int[] codeLengthLengths = new int[18];
        codeLengthLengths[2] = 2;
        codeLengthLengths[3] = 2;
        codeLengthLengths[17] = 1;
        WriteCodeLengthLengths(bits, codeLengthLengths);
        bits.WriteHuffman(3, 2);
        bits.WriteBits(127, 7);
        bits.WriteHuffman(3, 2);
        bits.WriteBits(107, 7);
        bits.WriteHuffman(0, 1);
        bits.WriteHuffman(2, 2);
        bits.WriteHuffman(0, 1);
        return bits.Finish();
    }

    static byte[] FirstRepeatBlock()
    {
        var bits = DynamicHeader(0);
        WriteCodeLengthLengths(bits, [1, 0, 0, 1]);
        bits.WriteHuffman(1, 1);
        return bits.Finish();
    }

    static byte[] RepeatOverflowBlock()
    {
        var bits = DynamicHeader(0);
        WriteCodeLengthLengths(bits, [0, 0, 1, 1]);
        bits.WriteHuffman(1, 1);
        bits.WriteBits(127, 7);
        bits.WriteHuffman(1, 1);
        bits.WriteBits(110, 7);
        return bits.Finish();
    }

    static byte[] MissingEndBlock()
    {
        var bits = DynamicHeader(0);
        WriteCodeLengthLengths(bits, [0, 0, 1, 1]);
        bits.WriteHuffman(1, 1);
        bits.WriteBits(127, 7);
        bits.WriteHuffman(1, 1);
        bits.WriteBits(109, 7);
        return bits.Finish();
    }

    static byte[] IncompleteCodeLengthTreeBlock()
    {
        var bits = DynamicHeader(0);
        WriteCodeLengthLengths(bits, [0, 0, 1, 0]);
        return bits.Finish();
    }

    static TestDeflateBitPacker DynamicHeader(int hclen)
    {
        var bits = new TestDeflateBitPacker();
        bits.WriteBits(0, 5);
        bits.WriteBits(0, 5);
        bits.WriteBits(hclen, 4);
        return bits;
    }

    static void WriteCodeLengthLengths(
        TestDeflateBitPacker bits,
        int[] lengths)
    {
        foreach (int length in lengths)
        {
            bits.WriteBits(length, 3);
        }
    }
}

internal class TestDeflateBitPacker
{
    internal List<int> Bits = [];

    internal void WriteBits(int value, int count)
    {
        int index = 0;
        while (index < count)
        {
            Bits.Add((value >> index) & 1);
            index++;
        }
    }

    internal void WriteHuffman(int canonicalCode, int count)
    {
        int index = count - 1;
        while (index >= 0)
        {
            Bits.Add((canonicalCode >> index) & 1);
            index--;
        }
    }

    internal byte[] Finish()
    {
        byte[] output = new byte[(Bits.Count + 7) / 8];
        for (var index = 0; index < Bits.Count; index++)
        {
            output[index / 8] |= (byte)(Bits[index] << (index % 8));
        }

        return output;
    }
}
