using raw;

namespace raw.Tests;

public class HuffmanTests
{
    [Test]
    public void CompleteTableBuildsExpectedCanonicalCodesAndRoundTrips()
    {
        byte[] counts = new byte[16];
        counts[0] = 1;
        counts[1] = 1;
        counts[2] = 2;
        byte[] symbols = new byte[] { 10, 20, 30, 40 };
        HuffmanTable table = new HuffmanTable(counts, symbols, 3);

        Assert.Multiple(() =>
        {
            Assert.That(table.IsComplete(), Is.True);
            Assert.That(table.GetSymbolCount(), Is.EqualTo(4));
            Assert.That(table.GetMaxCodeLength(), Is.EqualTo(3));
            Assert.That(table.GetEncodeCode(10), Is.EqualTo(0));
            Assert.That(table.GetEncodeCodeLength(10), Is.EqualTo(1));
            Assert.That(table.GetEncodeCode(20), Is.EqualTo(2));
            Assert.That(table.GetEncodeCodeLength(20), Is.EqualTo(2));
            Assert.That(table.GetEncodeCode(30), Is.EqualTo(6));
            Assert.That(table.GetEncodeCodeLength(30), Is.EqualTo(3));
            Assert.That(table.GetEncodeCode(40), Is.EqualTo(7));
            Assert.That(table.GetEncodeCodeLength(40), Is.EqualTo(3));
        });

        byte[] encoded = new byte[2];
        MsbBitWriter writer = new MsbBitWriter(encoded);
        table.EncodeSymbol(writer, 10);
        table.EncodeSymbol(writer, 20);
        table.EncodeSymbol(writer, 30);
        table.EncodeSymbol(writer, 40);
        writer.Finish(0);

        MsbBitReader reader = CreateReader(encoded);

        Assert.Multiple(() =>
        {
            Assert.That(encoded, Is.EqualTo(new byte[] { 0x5B, 0x80 }));
            Assert.That(table.DecodeSymbol(reader), Is.EqualTo(10));
            Assert.That(table.DecodeSymbol(reader), Is.EqualTo(20));
            Assert.That(table.DecodeSymbol(reader), Is.EqualTo(30));
            Assert.That(table.DecodeSymbol(reader), Is.EqualTo(40));
        });
        reader.AlignToByteWithExpectedPadding(0);
        Assert.That(reader.IsAtEnd(), Is.True);
    }

    [Test]
    public void IncompleteTableUsesSlowFallbackForLongCodes()
    {
        byte[] counts = new byte[16];
        counts[0] = 1;
        counts[15] = 1;
        HuffmanTable table = new HuffmanTable(counts, new byte[] { 1, 2 }, 4);

        Assert.Multiple(() =>
        {
            Assert.That(table.IsComplete(), Is.False);
            Assert.That(table.GetEncodeCode(1), Is.EqualTo(0));
            Assert.That(table.GetEncodeCodeLength(1), Is.EqualTo(1));
            Assert.That(table.GetEncodeCode(2), Is.EqualTo(0x8000));
            Assert.That(table.GetEncodeCodeLength(2), Is.EqualTo(16));
        });

        byte[] encoded = new byte[3];
        MsbBitWriter writer = new MsbBitWriter(encoded);
        table.EncodeSymbol(writer, 2);
        table.EncodeSymbol(writer, 1);
        writer.Finish(0);
        MsbBitReader reader = CreateReader(encoded);

        Assert.Multiple(() =>
        {
            Assert.That(encoded, Is.EqualTo(new byte[] { 0x80, 0x00, 0x00 }));
            Assert.That(table.DecodeSymbol(reader), Is.EqualTo(2));
            Assert.That(table.DecodeSymbol(reader), Is.EqualTo(1));
            Assert.That(reader.GetBitPosition(), Is.EqualTo(17));
        });
        reader.ValidatePaddingToEnd(0);
    }

    [Test]
    public void FastLookupWorksWithFewerRemainingBitsThanItsWidth()
    {
        byte[] counts = new byte[16];
        counts[0] = 1;
        HuffmanTable table = new HuffmanTable(counts, new byte[] { 77 }, 8);
        MsbBitReader reader = CreateReader(new byte[] { 0x00 });
        reader.ConsumeBits(7);

        byte symbol = table.DecodeSymbol(reader);

        Assert.Multiple(() =>
        {
            Assert.That(symbol, Is.EqualTo(77));
            Assert.That(reader.GetBitPosition(), Is.EqualTo(8));
            Assert.That(reader.IsAtEnd(), Is.True);
        });
    }

    [Test]
    public void ConstructionCopiesInputsAndRemainsDeterministic()
    {
        byte[] counts = new byte[16];
        counts[1] = 4;
        byte[] symbols = new byte[] { 4, 3, 2, 1 };
        HuffmanTable first = new HuffmanTable(counts, symbols, 4);
        HuffmanTable second = new HuffmanTable(counts, symbols, 4);

        counts[1] = 0;
        symbols[0] = 99;

        Assert.Multiple(() =>
        {
            Assert.That(first.GetEncodeCode(4), Is.EqualTo(0));
            Assert.That(first.GetEncodeCode(3), Is.EqualTo(1));
            Assert.That(first.GetEncodeCode(2), Is.EqualTo(2));
            Assert.That(first.GetEncodeCode(1), Is.EqualTo(3));
            Assert.That(first.GetEncodeCode(4), Is.EqualTo(second.GetEncodeCode(4)));
            Assert.That(first.GetEncodeCode(3), Is.EqualTo(second.GetEncodeCode(3)));
            Assert.That(first.GetEncodeCode(2), Is.EqualTo(second.GetEncodeCode(2)));
            Assert.That(first.GetEncodeCode(1), Is.EqualTo(second.GetEncodeCode(1)));
        });
    }

    [Test]
    public void OversubscribedAndEmptyTreesAreRejected()
    {
        byte[] oversubscribedCounts = new byte[16];
        oversubscribedCounts[0] = 3;
        byte[] emptyCounts = new byte[16];

        RawFormatException? oversubscribed = Assert.Throws<RawFormatException>(() => new HuffmanTable(oversubscribedCounts, new byte[] { 1, 2, 3 }, 8));
        RawFormatException? empty = Assert.Throws<RawFormatException>(() => new HuffmanTable(emptyCounts, Array.Empty<byte>(), 8));

        Assert.Multiple(() =>
        {
            Assert.That((oversubscribed ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidCodecData));
            Assert.That((empty ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidCodecData));
        });
    }

    [Test]
    public void SymbolCountMismatchDuplicatesAndExcessAreRejected()
    {
        byte[] mismatchCounts = new byte[16];
        mismatchCounts[1] = 2;
        byte[] duplicateCounts = new byte[16];
        duplicateCounts[1] = 2;
        byte[] excessCounts = new byte[16];
        excessCounts[8] = 255;
        excessCounts[9] = 2;

        RawFormatException? mismatch = Assert.Throws<RawFormatException>(() => new HuffmanTable(mismatchCounts, new byte[] { 1 }, 8));
        RawFormatException? duplicate = Assert.Throws<RawFormatException>(() => new HuffmanTable(duplicateCounts, new byte[] { 1, 1 }, 8));
        RawFormatException? excess = Assert.Throws<RawFormatException>(() => new HuffmanTable(excessCounts, new byte[257], 8));

        Assert.Multiple(() =>
        {
            Assert.That((mismatch ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidCodecData));
            Assert.That((duplicate ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidCodecData));
            Assert.That((excess ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidCodecData));
        });
    }

    [Test]
    public void InvalidConstructionArgumentsAreRejected()
    {
        byte[] counts = new byte[16];
        counts[0] = 1;

        RawFormatException? nullCounts = Assert.Throws<RawFormatException>(() => new HuffmanTable(null, new byte[] { 1 }, 8));
        RawFormatException? nullSymbols = Assert.Throws<RawFormatException>(() => new HuffmanTable(counts, null, 8));
        RawFormatException? countLength = Assert.Throws<RawFormatException>(() => new HuffmanTable(new byte[15], new byte[] { 1 }, 8));
        RawFormatException? fastLow = Assert.Throws<RawFormatException>(() => new HuffmanTable(counts, new byte[] { 1 }, 0));
        RawFormatException? fastHigh = Assert.Throws<RawFormatException>(() => new HuffmanTable(counts, new byte[] { 1 }, 17));

        Assert.Multiple(() =>
        {
            Assert.That((nullCounts ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidArgument));
            Assert.That((nullSymbols ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidArgument));
            Assert.That((countLength ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidCodecData));
            Assert.That((fastLow ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidArgument));
            Assert.That((fastHigh ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidArgument));
        });
    }

    [Test]
    public void InvalidPrefixIsRejectedAfterAvailableTreeDepth()
    {
        byte[] counts = new byte[16];
        counts[0] = 1;
        HuffmanTable table = new HuffmanTable(counts, new byte[] { 5 }, 4);
        MsbBitReader reader = CreateReader(new byte[] { 0xFF });

        RawFormatException? exception = Assert.Throws<RawFormatException>(() => table.DecodeSymbol(reader));

        Assert.Multiple(() =>
        {
            Assert.That((exception ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidCodecData));
            Assert.That(reader.GetBitPosition(), Is.EqualTo(1));
        });
    }

    [Test]
    public void TruncatedLongCodeRetainsConsumedBitPosition()
    {
        byte[] counts = new byte[16];
        counts[15] = 1;
        HuffmanTable table = new HuffmanTable(counts, new byte[] { 9 }, 8);
        MsbBitReader reader = CreateReader(new byte[] { 0x00 });

        RawFormatException? exception = Assert.Throws<RawFormatException>(() => table.DecodeSymbol(reader));

        Assert.Multiple(() =>
        {
            Assert.That((exception ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.TruncatedInput));
            Assert.That(exception.Offset, Is.EqualTo(1));
            Assert.That(reader.GetBitPosition(), Is.EqualTo(8));
        });
    }

    [Test]
    public void EncodingUnknownSymbolFailsBeforeWriting()
    {
        byte[] counts = new byte[16];
        counts[0] = 1;
        HuffmanTable table = new HuffmanTable(counts, new byte[] { 1 }, 4);
        byte[] bytes = new byte[] { 0xAA };
        MsbBitWriter writer = new MsbBitWriter(bytes);

        RawFormatException? code = Assert.Throws<RawFormatException>(() => table.GetEncodeCode(2));
        RawFormatException? length = Assert.Throws<RawFormatException>(() => table.GetEncodeCodeLength(2));
        RawFormatException? encode = Assert.Throws<RawFormatException>(() => table.EncodeSymbol(writer, 2));

        Assert.Multiple(() =>
        {
            Assert.That((code ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidArgument));
            Assert.That((length ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidArgument));
            Assert.That((encode ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidArgument));
            Assert.That(writer.GetBitPosition(), Is.EqualTo(0));
            Assert.That(bytes[0], Is.EqualTo(0xAA));
        });
    }

    static MsbBitReader CreateReader(byte[] bytes)
    {
        BoundedInput input = new BoundedInput(new ReadOnlyMemory<byte>(bytes), 0, bytes.Length, new RawLimits());
        return new MsbBitReader(input);
    }
}
