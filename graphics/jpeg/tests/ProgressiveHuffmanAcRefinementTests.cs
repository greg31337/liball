namespace jpeg.Tests;

internal class ProgressiveHuffmanAcRefinementTests
{
    [Test]
    public void Decoder_RefinesExistingCoefficientAndAddsNewPositiveCoefficient()
    {
        global::jpeg.JpegBitWriter writer = new global::jpeg.JpegBitWriter();
        writer.WriteBits(1, 1);
        writer.WriteBits(2, 2);
        writer.WriteBits(1, 1);
        writer.WriteBits(0, 1);
        writer.FlushToByteBoundary();
        global::jpeg.JpegProgressiveHuffmanEntropyDecoder decoder = new global::jpeg.JpegProgressiveHuffmanEntropyDecoder(writer.ToArray());
        int[] coefficients = CreateBlock();
        coefficients[1] = 2;

        decoder.DecodeAcRefinementBlock(coefficients, CreateNewCoefficientAcTable(), 1, 5, 0);

        Assert.That(coefficients[1], Is.EqualTo(3));
        Assert.That(coefficients[2], Is.EqualTo(1));
    }

    [Test]
    public void Decoder_RefinesNegativeCoefficient()
    {
        global::jpeg.JpegBitWriter writer = new global::jpeg.JpegBitWriter();
        writer.WriteBits(1, 1);
        writer.WriteBits(0, 1);
        writer.FlushToByteBoundary();
        global::jpeg.JpegProgressiveHuffmanEntropyDecoder decoder = new global::jpeg.JpegProgressiveHuffmanEntropyDecoder(writer.ToArray());
        int[] coefficients = CreateBlock();
        coefficients[1] = -2;

        decoder.DecodeAcRefinementBlock(coefficients, CreateEobOnlyAcTable(), 1, 3, 0);

        Assert.That(coefficients[1], Is.EqualTo(-3));
        Assert.That(decoder.EobRun, Is.EqualTo(0));
    }

    [Test]
    public void Decoder_UsesEobRunToRefineSubsequentBlocksWithoutHuffmanSymbol()
    {
        global::jpeg.JpegBitWriter writer = new global::jpeg.JpegBitWriter();
        writer.WriteBits(0, 1);
        writer.WriteBits(1, 1);
        writer.WriteBits(1, 1);
        writer.WriteBits(1, 1);
        writer.FlushToByteBoundary();
        global::jpeg.JpegProgressiveHuffmanEntropyDecoder decoder = new global::jpeg.JpegProgressiveHuffmanEntropyDecoder(writer.ToArray());
        int[] first = CreateBlock();
        int[] second = CreateBlock();
        first[2] = 2;
        second[1] = 4;

        decoder.DecodeAcRefinementBlock(first, ProgressiveHuffmanAcFirstTests.CreateEobRunAcTable(), 1, 4, 0);
        decoder.DecodeAcRefinementBlock(second, ProgressiveHuffmanAcFirstTests.CreateEobRunAcTable(), 1, 4, 0);

        Assert.That(first[2], Is.EqualTo(3));
        Assert.That(second[1], Is.EqualTo(5));
        Assert.That(decoder.EobRun, Is.EqualTo(1));
    }

    [Test]
    public void Decoder_HandlesZeroRunLengthInAcRefinementScan()
    {
        global::jpeg.JpegBitWriter writer = new global::jpeg.JpegBitWriter();
        writer.WriteBits(6, 3);
        writer.WriteBits(0, 1);
        writer.WriteBits(2, 2);
        writer.WriteBits(0, 1);
        writer.WriteBits(0, 1);
        writer.FlushToByteBoundary();
        global::jpeg.JpegProgressiveHuffmanEntropyDecoder decoder = new global::jpeg.JpegProgressiveHuffmanEntropyDecoder(writer.ToArray());
        int[] coefficients = CreateBlock();
        coefficients[2] = 2;

        decoder.DecodeAcRefinementBlock(coefficients, CreateZrlRefinementAcTable(), 1, 20, 0);

        Assert.That(coefficients[2], Is.EqualTo(2));
        Assert.That(coefficients[18], Is.EqualTo(-1));
    }

    [Test]
    public void Decoder_DecodesAcRefinementFromScanHeader()
    {
        global::jpeg.JpegBitWriter writer = new global::jpeg.JpegBitWriter();
        writer.WriteBits(2, 2);
        writer.WriteBits(1, 1);
        writer.WriteBits(0, 1);
        writer.FlushToByteBoundary();
        global::jpeg.JpegProgressiveHuffmanEntropyDecoder decoder = new global::jpeg.JpegProgressiveHuffmanEntropyDecoder(writer.ToArray());
        global::jpeg.JpegScanHeader scan = ProgressiveHuffmanDcFirstTests.CreateScan(1, 5, 1, 0);
        int[] coefficients = CreateBlock();

        decoder.DecodeAcRefinementBlockForScan(scan, CreateNewCoefficientAcTable(), coefficients);

        Assert.That(coefficients[1], Is.EqualTo(1));
    }

    [Test]
    public void Decoder_RejectsInvalidAcRefinementSymbolsAndRuns()
    {
        global::jpeg.JpegProgressiveHuffmanEntropyDecoder badSize = new global::jpeg.JpegProgressiveHuffmanEntropyDecoder(CreateSingleSymbolEntropy(0));
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                badSize.DecodeAcRefinementBlock(CreateBlock(), CreateBadSizeAcTable(), 1, 5, 0);
            });

        global::jpeg.JpegProgressiveHuffmanEntropyDecoder overrun = new global::jpeg.JpegProgressiveHuffmanEntropyDecoder(CreateSingleSymbolEntropy(0));
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                overrun.DecodeAcRefinementBlock(CreateBlock(), CreateRunOneNewAcTable(), 5, 5, 0);
            });
    }

    [Test]
    public void Decoder_RejectsInvalidAcRefinementScanParameters()
    {
        global::jpeg.JpegProgressiveHuffmanEntropyDecoder decoder = new global::jpeg.JpegProgressiveHuffmanEntropyDecoder(new byte[0]);

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                decoder.ValidateAcRefinementScan(ProgressiveHuffmanDcFirstTests.CreateScan(0, 5, 1, 0));
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                decoder.ValidateAcRefinementScan(ProgressiveHuffmanDcFirstTests.CreateScan(1, 5, 0, 0));
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                decoder.ValidateAcRefinementScan(ProgressiveHuffmanDcFirstTests.CreateScan(1, 5, 3, 1));
            });
    }

    public static int[] CreateBlock()
    {
        return new int[global::jpeg.JpegConstants.BlockSampleCount];
    }

    public static byte[] CreateSingleSymbolEntropy(int code)
    {
        global::jpeg.JpegBitWriter writer = new global::jpeg.JpegBitWriter();
        writer.WriteBits(code, 1);
        writer.FlushToByteBoundary();
        return writer.ToArray();
    }

    public static global::jpeg.JpegHuffmanTable CreateNewCoefficientAcTable()
    {
        global::jpeg.JpegHuffmanTable table = new global::jpeg.JpegHuffmanTable();
        table.TableClass = 1;
        table.Identifier = 0;
        table.CodeCounts[0] = 1;
        table.CodeCounts[1] = 1;
        table.SymbolCount = 2;
        table.Symbols = new int[] { 0x00, 0x01 };
        table.Codes = new int[2];
        table.CodeLengths = new int[2];
        new global::jpeg.JpegHuffmanTableParser().BuildDecodeTable(table);
        return table;
    }

    public static global::jpeg.JpegHuffmanTable CreateZrlRefinementAcTable()
    {
        global::jpeg.JpegHuffmanTable table = new global::jpeg.JpegHuffmanTable();
        table.TableClass = 1;
        table.Identifier = 0;
        table.CodeCounts[0] = 1;
        table.CodeCounts[1] = 1;
        table.CodeCounts[2] = 1;
        table.SymbolCount = 3;
        table.Symbols = new int[] { 0x00, 0x01, 0xF0 };
        table.Codes = new int[3];
        table.CodeLengths = new int[3];
        new global::jpeg.JpegHuffmanTableParser().BuildDecodeTable(table);
        return table;
    }

    public static global::jpeg.JpegHuffmanTable CreateEobOnlyAcTable()
    {
        return CreateSingleSymbolAcTable(0x00);
    }

    public static global::jpeg.JpegHuffmanTable CreateBadSizeAcTable()
    {
        return CreateSingleSymbolAcTable(0x02);
    }

    public static global::jpeg.JpegHuffmanTable CreateRunOneNewAcTable()
    {
        return CreateSingleSymbolAcTable(0x11);
    }

    public static global::jpeg.JpegHuffmanTable CreateSingleSymbolAcTable(int symbol)
    {
        global::jpeg.JpegHuffmanTable table = new global::jpeg.JpegHuffmanTable();
        table.TableClass = 1;
        table.Identifier = 0;
        table.CodeCounts[0] = 1;
        table.SymbolCount = 1;
        table.Symbols = new int[] { symbol };
        table.Codes = new int[1];
        table.CodeLengths = new int[1];
        new global::jpeg.JpegHuffmanTableParser().BuildDecodeTable(table);
        return table;
    }
}
