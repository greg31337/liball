namespace jpeg.Tests;

internal class ProgressiveHuffmanDcRefinementTests
{
    [Test]
    public void Decoder_AddsSuccessiveLowBitWhenRefinementBitIsSet()
    {
        global::jpeg.JpegProgressiveHuffmanEntropyDecoder decoder = new global::jpeg.JpegProgressiveHuffmanEntropyDecoder(CreateRefinementEntropy(1));
        int[] coefficients = CreateBlock(8);

        decoder.DecodeDcRefinementBlock(coefficients, 1);

        Assert.That(coefficients[0], Is.EqualTo(10));
    }

    [Test]
    public void Decoder_LeavesDcCoefficientWhenRefinementBitIsClear()
    {
        global::jpeg.JpegProgressiveHuffmanEntropyDecoder decoder = new global::jpeg.JpegProgressiveHuffmanEntropyDecoder(CreateRefinementEntropy(0));
        int[] coefficients = CreateBlock(8);
        decoder.DcPredictors[0] = 5;

        decoder.DecodeDcRefinementBlock(coefficients, 2);

        Assert.That(coefficients[0], Is.EqualTo(8));
        Assert.That(decoder.DcPredictors[0], Is.EqualTo(5));
    }

    [Test]
    public void Decoder_AddsRefinementBitToNegativeDcCoefficient()
    {
        global::jpeg.JpegProgressiveHuffmanEntropyDecoder decoder = new global::jpeg.JpegProgressiveHuffmanEntropyDecoder(CreateRefinementEntropy(1));
        int[] coefficients = CreateBlock(-4);

        decoder.DecodeDcRefinementBlock(coefficients, 0);

        Assert.That(coefficients[0], Is.EqualTo(-3));
    }

    [Test]
    public void Decoder_DecodesDcRefinementFromScanHeader()
    {
        global::jpeg.JpegProgressiveHuffmanEntropyDecoder decoder = new global::jpeg.JpegProgressiveHuffmanEntropyDecoder(CreateRefinementEntropy(1));
        global::jpeg.JpegScanHeader scan = ProgressiveHuffmanDcFirstTests.CreateScan(0, 0, 2, 1);
        int[] coefficients = CreateBlock(4);

        decoder.DecodeDcRefinementBlockForScan(scan, coefficients);

        Assert.That(coefficients[0], Is.EqualTo(6));
    }

    [Test]
    public void Decoder_RejectsInvalidDcRefinementScanParameters()
    {
        global::jpeg.JpegProgressiveHuffmanEntropyDecoder decoder = new global::jpeg.JpegProgressiveHuffmanEntropyDecoder(new byte[0]);

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                decoder.ValidateDcRefinementScan(ProgressiveHuffmanDcFirstTests.CreateScan(1, 0, 1, 0));
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                decoder.ValidateDcRefinementScan(ProgressiveHuffmanDcFirstTests.CreateScan(0, 1, 1, 0));
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                decoder.ValidateDcRefinementScan(ProgressiveHuffmanDcFirstTests.CreateScan(0, 0, 0, 0));
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                decoder.ValidateDcRefinementScan(ProgressiveHuffmanDcFirstTests.CreateScan(0, 0, 3, 1));
            });
    }

    [Test]
    public void Decoder_RejectsInvalidDcRefinementInputs()
    {
        global::jpeg.JpegProgressiveHuffmanEntropyDecoder decoder = new global::jpeg.JpegProgressiveHuffmanEntropyDecoder(new byte[] { 0 });

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                decoder.ValidateDcRefinementScan(null!);
            });

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                decoder.DecodeDcRefinementBlock(null!, 0);
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                decoder.DecodeDcRefinementBlock(new int[1], 0);
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                decoder.DecodeDcRefinementBlock(CreateBlock(0), 16);
            });
    }

    public static byte[] CreateRefinementEntropy(int bit)
    {
        global::jpeg.JpegBitWriter writer = new global::jpeg.JpegBitWriter();
        writer.WriteBit(bit);
        writer.FlushToByteBoundary();
        return writer.ToArray();
    }

    public static int[] CreateBlock(int dc)
    {
        int[] coefficients = new int[global::jpeg.JpegConstants.BlockSampleCount];
        coefficients[0] = dc;
        return coefficients;
    }
}
