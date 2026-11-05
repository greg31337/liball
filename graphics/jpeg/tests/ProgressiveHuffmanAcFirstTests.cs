namespace jpeg.Tests;

internal class ProgressiveHuffmanAcFirstTests
{
    [Test]
    public void Decoder_DecodesAcFirstCoefficientInSpectralRange()
    {
        global::jpeg.JpegProgressiveHuffmanEntropyDecoder decoder = new global::jpeg.JpegProgressiveHuffmanEntropyDecoder(CreateSingleAcEntropy(2));
        int[] coefficients = new int[global::jpeg.JpegConstants.BlockSampleCount];

        decoder.DecodeAcFirstBlock(coefficients, BaselineHuffmanEntropyDecoderTests.CreateAcTable(), 5, 10, 1);

        Assert.That(coefficients[5], Is.EqualTo(4));
        Assert.That(coefficients[4], Is.EqualTo(0));
        Assert.That(coefficients[6], Is.EqualTo(0));
    }

    [Test]
    public void Decoder_DecodesNegativeAcFirstCoefficient()
    {
        global::jpeg.JpegProgressiveHuffmanEntropyDecoder decoder = new global::jpeg.JpegProgressiveHuffmanEntropyDecoder(CreateSingleAcEntropy(1));
        int[] coefficients = new int[global::jpeg.JpegConstants.BlockSampleCount];

        decoder.DecodeAcFirstBlock(coefficients, BaselineHuffmanEntropyDecoderTests.CreateAcTable(), 1, 8, 1);

        Assert.That(coefficients[1], Is.EqualTo(-4));
    }

    [Test]
    public void Decoder_HandlesZeroRunLengthInAcFirstScan()
    {
        global::jpeg.JpegBitWriter writer = new global::jpeg.JpegBitWriter();
        writer.WriteBits(6, 3);
        writer.WriteBits(7, 3);
        writer.WriteBits(1, 1);
        writer.WriteBits(0, 1);
        writer.FlushToByteBoundary();
        global::jpeg.JpegProgressiveHuffmanEntropyDecoder decoder = new global::jpeg.JpegProgressiveHuffmanEntropyDecoder(writer.ToArray());
        int[] coefficients = new int[global::jpeg.JpegConstants.BlockSampleCount];

        decoder.DecodeAcFirstBlock(coefficients, BaselineHuffmanEntropyDecoderTests.CreateAcTable(), 1, 20, 0);

        Assert.That(coefficients[18], Is.EqualTo(1));
        Assert.That(coefficients[17], Is.EqualTo(0));
    }

    [Test]
    public void Decoder_DecodesAcFirstBlockFromScanHeader()
    {
        global::jpeg.JpegProgressiveHuffmanEntropyDecoder decoder = new global::jpeg.JpegProgressiveHuffmanEntropyDecoder(CreateSingleAcEntropy(2));
        global::jpeg.JpegScanHeader scan = CreateAcFirstScan(2, 6, 1);
        int[] coefficients = new int[global::jpeg.JpegConstants.BlockSampleCount];

        decoder.DecodeAcFirstBlockForScan(scan, BaselineHuffmanEntropyDecoderTests.CreateAcTable(), coefficients);

        Assert.That(coefficients[2], Is.EqualTo(4));
    }

    [Test]
    public void Decoder_RejectsAcFirstRunsPastSpectralEnd()
    {
        global::jpeg.JpegBitWriter writer = new global::jpeg.JpegBitWriter();
        writer.WriteBits(7, 3);
        writer.WriteBits(1, 1);
        writer.FlushToByteBoundary();
        global::jpeg.JpegProgressiveHuffmanEntropyDecoder decoder = new global::jpeg.JpegProgressiveHuffmanEntropyDecoder(writer.ToArray());

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                decoder.DecodeAcFirstBlock(new int[global::jpeg.JpegConstants.BlockSampleCount], BaselineHuffmanEntropyDecoderTests.CreateAcTable(), 5, 5, 0);
            });
    }

    [Test]
    public void Decoder_DecodesAcFirstEobRunSymbol()
    {
        global::jpeg.JpegBitWriter writer = new global::jpeg.JpegBitWriter();
        writer.WriteBits(0, 1);
        writer.WriteBits(1, 1);
        writer.FlushToByteBoundary();
        global::jpeg.JpegProgressiveHuffmanEntropyDecoder decoder = new global::jpeg.JpegProgressiveHuffmanEntropyDecoder(writer.ToArray());
        int[] coefficients = new int[global::jpeg.JpegConstants.BlockSampleCount];

        decoder.DecodeAcFirstBlock(coefficients, CreateEobRunAcTable(), 1, 10, 0);
        decoder.DecodeAcFirstBlock(coefficients, CreateEobRunAcTable(), 1, 10, 0);

        Assert.That(decoder.EobRun, Is.EqualTo(1));
    }

    [Test]
    public void Decoder_RejectsInvalidAcFirstScanParameters()
    {
        global::jpeg.JpegProgressiveHuffmanEntropyDecoder decoder = new global::jpeg.JpegProgressiveHuffmanEntropyDecoder(new byte[0]);

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                decoder.ValidateAcFirstScan(CreateAcFirstScan(0, 5, 0));
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                decoder.ValidateAcFirstScan(CreateAcFirstScan(5, 4, 0));
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                decoder.ValidateAcFirstScan(ProgressiveHuffmanDcFirstTests.CreateScan(1, 5, 1, 0));
            });

        global::jpeg.JpegScanHeader multiComponent = CreateAcFirstScan(1, 5, 0);
        multiComponent.ComponentCount = 2;
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                decoder.ValidateAcFirstScan(multiComponent);
            });
    }

    [Test]
    public void Decoder_RejectsInvalidAcFirstInputs()
    {
        global::jpeg.JpegProgressiveHuffmanEntropyDecoder decoder = new global::jpeg.JpegProgressiveHuffmanEntropyDecoder(new byte[] { 0 });

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                decoder.ValidateAcFirstScan(null!);
            });

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                decoder.DecodeAcFirstBlock(null!, BaselineHuffmanEntropyDecoderTests.CreateAcTable(), 1, 2, 0);
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                decoder.DecodeAcFirstBlock(new int[1], BaselineHuffmanEntropyDecoderTests.CreateAcTable(), 1, 2, 0);
            });
    }

    public static byte[] CreateSingleAcEntropy(int valueBits)
    {
        global::jpeg.JpegBitWriter writer = new global::jpeg.JpegBitWriter();
        writer.WriteBits(2, 2);
        writer.WriteBits(valueBits, 2);
        writer.WriteBits(0, 1);
        writer.FlushToByteBoundary();
        return writer.ToArray();
    }

    public static global::jpeg.JpegScanHeader CreateAcFirstScan(int spectralStart, int spectralEnd, int successiveLow)
    {
        global::jpeg.JpegScanHeader scan = ProgressiveHuffmanDcFirstTests.CreateScan(spectralStart, spectralEnd, 0, successiveLow);
        return scan;
    }

    public static global::jpeg.JpegHuffmanTable CreateEobRunAcTable()
    {
        global::jpeg.JpegHuffmanTable table = new global::jpeg.JpegHuffmanTable();
        table.TableClass = 1;
        table.Identifier = 0;
        table.CodeCounts[0] = 1;
        table.SymbolCount = 1;
        table.Symbols = new int[] { 0x10 };
        table.Codes = new int[1];
        table.CodeLengths = new int[1];
        new global::jpeg.JpegHuffmanTableParser().BuildDecodeTable(table);
        return table;
    }
}
