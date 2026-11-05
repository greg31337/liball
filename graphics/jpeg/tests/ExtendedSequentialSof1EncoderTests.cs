namespace jpeg.Tests;

internal class ExtendedSequentialSof1EncoderTests
{
    [Test]
    public void Encoder_EncodesTwelveBitGrayscaleSamplesThatDecodeBack()
    {
        global::jpeg.JpegFrameHeader frame = ExtendedSequentialSof1DecoderTests.CreateSof1Frame(16, 8);
        frame.Components.Add(BaselineSof0FullImageDecoderTests.CreateFrameComponent(1, 1, 1, 0));
        frame.ComponentCount = 1;
        global::jpeg.JpegScanHeader scan = BaselineSof0FullImageDecoderTests.CreateScan(frame);
        global::jpeg.JpegTableStore tables = BaselineSof0FullImageDecoderTests.CreateTableStore(40);
        global::jpeg.JpegPlanarIntImageBuffer source = CreateSource(frame);
        FillBlock(source.Components[0], 0, 0, 2058);
        FillBlock(source.Components[0], 8, 0, 2063);
        global::jpeg.JpegExtendedSequentialSof1Encoder encoder = new global::jpeg.JpegExtendedSequentialSof1Encoder();
        global::jpeg.JpegExtendedSequentialSof1Decoder decoder = new global::jpeg.JpegExtendedSequentialSof1Decoder();

        byte[] entropyData = encoder.Encode(frame, scan, tables, source);
        global::jpeg.JpegPlanarIntImageBuffer decoded = decoder.Decode(frame, scan, tables, entropyData);

        Assert.That(entropyData.Length, Is.GreaterThan(0));
        Assert.That(decoded.Components[0].Samples[0], Is.EqualTo(2058));
        Assert.That(decoded.Components[0].Samples[7], Is.EqualTo(2058));
        Assert.That(decoded.Components[0].Samples[8], Is.EqualTo(2063));
        Assert.That(decoded.Components[0].Samples[15], Is.EqualTo(2063));
    }

    [Test]
    public void Encoder_EncodesFourTwoZeroTwelveBitBlockOrder()
    {
        global::jpeg.JpegFrameHeader frame = ExtendedSequentialSof1DecoderTests.CreateSof1Frame(16, 16);
        frame.Components.Add(BaselineSof0FullImageDecoderTests.CreateFrameComponent(1, 2, 2, 0));
        frame.Components.Add(BaselineSof0FullImageDecoderTests.CreateFrameComponent(2, 1, 1, 0));
        frame.Components.Add(BaselineSof0FullImageDecoderTests.CreateFrameComponent(3, 1, 1, 0));
        frame.ComponentCount = 3;
        global::jpeg.JpegScanHeader scan = BaselineSof0FullImageDecoderTests.CreateScan(frame);
        global::jpeg.JpegTableStore tables = BaselineSof0FullImageDecoderTests.CreateTableStore(40);
        global::jpeg.JpegPlanarIntImageBuffer source = CreateSource(frame);
        FillBlock(source.Components[0], 0, 0, 2058);
        FillBlock(source.Components[0], 8, 0, 2063);
        FillBlock(source.Components[0], 0, 8, 2053);
        FillBlock(source.Components[0], 8, 8, 2048);
        FillBlock(source.Components[1], 0, 0, 2053);
        FillBlock(source.Components[2], 0, 0, 2058);
        global::jpeg.JpegExtendedSequentialSof1Encoder encoder = new global::jpeg.JpegExtendedSequentialSof1Encoder();
        global::jpeg.JpegExtendedSequentialSof1Decoder decoder = new global::jpeg.JpegExtendedSequentialSof1Decoder();

        byte[] entropyData = encoder.Encode(frame, scan, tables, source);
        global::jpeg.JpegPlanarIntImageBuffer decoded = decoder.Decode(frame, scan, tables, entropyData);

        Assert.That(decoded.Components[0].Samples[0], Is.EqualTo(2058));
        Assert.That(decoded.Components[0].Samples[8], Is.EqualTo(2063));
        Assert.That(decoded.Components[0].Samples[8 * decoded.Components[0].Stride], Is.EqualTo(2053));
        Assert.That(decoded.Components[0].Samples[8 * decoded.Components[0].Stride + 8], Is.EqualTo(2048));
        Assert.That(decoded.Components[1].Samples[0], Is.EqualTo(2053));
        Assert.That(decoded.Components[2].Samples[0], Is.EqualTo(2058));
    }

    [Test]
    public void Encoder_WritesRestartMarkersForTwelveBitScan()
    {
        global::jpeg.JpegFrameHeader frame = ExtendedSequentialSof1DecoderTests.CreateSof1Frame(16, 8);
        frame.Components.Add(BaselineSof0FullImageDecoderTests.CreateFrameComponent(1, 1, 1, 0));
        frame.ComponentCount = 1;
        global::jpeg.JpegScanHeader scan = BaselineSof0FullImageDecoderTests.CreateScan(frame);
        global::jpeg.JpegTableStore tables = BaselineSof0FullImageDecoderTests.CreateTableStore(40);
        global::jpeg.JpegPlanarIntImageBuffer source = CreateSource(frame);
        FillBlock(source.Components[0], 0, 0, 2058);
        FillBlock(source.Components[0], 8, 0, 2058);
        global::jpeg.JpegExtendedSequentialSof1Encoder encoder = new global::jpeg.JpegExtendedSequentialSof1Encoder();
        global::jpeg.JpegExtendedSequentialSof1Decoder decoder = new global::jpeg.JpegExtendedSequentialSof1Decoder();

        byte[] entropyData = encoder.EncodeWithRestartInterval(frame, scan, tables, source, 1);
        global::jpeg.JpegPlanarIntImageBuffer decoded = decoder.DecodeWithRestartInterval(frame, scan, tables, entropyData, 1);

        Assert.That(BaselineSof0EncoderTests.ContainsRestart0(entropyData), Is.True);
        Assert.That(decoded.Components[0].Samples[0], Is.EqualTo(2058));
        Assert.That(decoded.Components[0].Samples[8], Is.EqualTo(2058));
    }

    [Test]
    public void SequentialEntropyEncoder_AllowsExtendedMagnitudeCategories()
    {
        int[] coefficients = new int[global::jpeg.JpegConstants.BlockSampleCount];
        coefficients[0] = 2048;
        global::jpeg.JpegSequentialHuffmanEntropyEncoder encoder = new global::jpeg.JpegSequentialHuffmanEntropyEncoder();

        encoder.EncodeBlock(
            coefficients,
            ExtendedSequentialSof1DecoderTests.CreateSingleSymbolDcTable(12),
            ExtendedSequentialSof1DecoderTests.CreateSingleSymbolAcTable(0),
            0);
        encoder.FlushToByteBoundary();
        global::jpeg.JpegSequentialHuffmanEntropyDecoder decoder = new global::jpeg.JpegSequentialHuffmanEntropyDecoder(encoder.ToArray());
        int[] decoded = decoder.DecodeBlock(
            ExtendedSequentialSof1DecoderTests.CreateSingleSymbolDcTable(12),
            ExtendedSequentialSof1DecoderTests.CreateSingleSymbolAcTable(0),
            0);

        Assert.That(decoded[0], Is.EqualTo(2048));
    }

    [Test]
    public void Encoder_RejectsInvalidTwelveBitInputs()
    {
        global::jpeg.JpegExtendedSequentialSof1Encoder encoder = new global::jpeg.JpegExtendedSequentialSof1Encoder();
        global::jpeg.JpegFrameHeader frame = ExtendedSequentialSof1DecoderTests.CreateSof1Frame(8, 8);
        frame.Components.Add(BaselineSof0FullImageDecoderTests.CreateFrameComponent(1, 1, 1, 0));
        frame.ComponentCount = 1;
        global::jpeg.JpegScanHeader scan = BaselineSof0FullImageDecoderTests.CreateScan(frame);
        global::jpeg.JpegTableStore tables = BaselineSof0FullImageDecoderTests.CreateTableStore(40);
        global::jpeg.JpegPlanarIntImageBuffer source = CreateSource(frame);

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                encoder.Encode(frame, scan, tables, null!);
            });

        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                global::jpeg.JpegPlanarIntImageBuffer badPrecision = CreateSource(frame);
                badPrecision.Precision = 8;
                encoder.Encode(frame, scan, tables, badPrecision);
            });

        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                global::jpeg.JpegPlanarIntImageBuffer badSample = CreateSource(frame);
                badSample.Components[0].Samples[0] = 4096;
                encoder.Encode(frame, scan, tables, badSample);
            });

        Assert.Throws<System.ArgumentOutOfRangeException>(
            delegate
            {
                encoder.EncodeWithRestartInterval(frame, scan, tables, source, -1);
            });
    }

    public static global::jpeg.JpegPlanarIntImageBuffer CreateSource(global::jpeg.JpegFrameHeader frame)
    {
        global::jpeg.JpegMcuLayout layout = new global::jpeg.JpegMcuLayoutCalculator().Calculate(frame);
        return new global::jpeg.JpegExtendedSequentialSof1Decoder().Allocate(layout, frame.Precision);
    }

    public static void FillBlock(global::jpeg.JpegPlanarIntComponentBuffer component, int x, int y, int value)
    {
        int row = 0;
        while (row < 8)
        {
            int column = 0;
            while (column < 8)
            {
                component.Samples[(y + row) * component.Stride + x + column] = value;
                column = column + 1;
            }

            row = row + 1;
        }
    }
}
