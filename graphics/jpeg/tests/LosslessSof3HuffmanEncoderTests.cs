namespace jpeg.Tests;

internal class LosslessSof3HuffmanEncoderTests
{
    [Test]
    public void Encoder_EncodesGrayscaleSamplesThatDecodeBack()
    {
        global::jpeg.JpegFrameHeader frame = LosslessSof3HuffmanDecoderTests.CreateFrame(3, 2, 12, 1);
        global::jpeg.JpegScanHeader scan = LosslessSof3HuffmanDecoderTests.CreateScan(1, 1, 0);
        global::jpeg.JpegTableStore tables = LosslessSof3HuffmanDecoderTests.CreateTables(BaselineHuffmanEntropyDecoderTests.CreateDcTable());
        global::jpeg.JpegPlanarIntImageBuffer source = ExtendedSequentialSof1EncoderTests.CreateSource(frame);
        SetSample(source.Components[0], 0, 0, 2050);
        SetSample(source.Components[0], 1, 0, 2051);
        SetSample(source.Components[0], 2, 0, 2052);
        SetSample(source.Components[0], 0, 1, 2049);
        SetSample(source.Components[0], 1, 1, 2050);
        SetSample(source.Components[0], 2, 1, 2051);
        global::jpeg.JpegLosslessSof3HuffmanEncoder encoder = new global::jpeg.JpegLosslessSof3HuffmanEncoder();
        global::jpeg.JpegLosslessSof3HuffmanDecoder decoder = new global::jpeg.JpegLosslessSof3HuffmanDecoder();

        byte[] entropyData = encoder.Encode(frame, scan, tables, source);
        global::jpeg.JpegPlanarIntImageBuffer decoded = decoder.Decode(frame, scan, tables, entropyData);

        Assert.That(entropyData.Length, Is.GreaterThan(0));
        Assert.That(decoded.Components[0].Samples[0], Is.EqualTo(2050));
        Assert.That(decoded.Components[0].Samples[1], Is.EqualTo(2051));
        Assert.That(decoded.Components[0].Samples[2], Is.EqualTo(2052));
        Assert.That(decoded.Components[0].Samples[decoded.Components[0].Stride], Is.EqualTo(2049));
        Assert.That(decoded.Components[0].Samples[decoded.Components[0].Stride + 1], Is.EqualTo(2050));
        Assert.That(decoded.Components[0].Samples[decoded.Components[0].Stride + 2], Is.EqualTo(2051));
    }

    [Test]
    public void Encoder_EncodesInterleavedComponentsThatDecodeBack()
    {
        global::jpeg.JpegFrameHeader frame = LosslessSof3HuffmanDecoderTests.CreateFrame(2, 1, 8, 3);
        global::jpeg.JpegScanHeader scan = LosslessSof3HuffmanDecoderTests.CreateScan(3, 1, 0);
        global::jpeg.JpegTableStore tables = LosslessSof3HuffmanDecoderTests.CreateTables(BaselineHuffmanEntropyDecoderTests.CreateDcTable());
        global::jpeg.JpegPlanarIntImageBuffer source = ExtendedSequentialSof1EncoderTests.CreateSource(frame);
        SetSample(source.Components[0], 0, 0, 130);
        SetSample(source.Components[0], 1, 0, 131);
        SetSample(source.Components[1], 0, 0, 129);
        SetSample(source.Components[1], 1, 0, 130);
        SetSample(source.Components[2], 0, 0, 128);
        SetSample(source.Components[2], 1, 0, 129);
        global::jpeg.JpegLosslessSof3HuffmanEncoder encoder = new global::jpeg.JpegLosslessSof3HuffmanEncoder();
        global::jpeg.JpegLosslessSof3HuffmanDecoder decoder = new global::jpeg.JpegLosslessSof3HuffmanDecoder();

        byte[] entropyData = encoder.Encode(frame, scan, tables, source);
        global::jpeg.JpegPlanarIntImageBuffer decoded = decoder.Decode(frame, scan, tables, entropyData);

        Assert.That(decoded.Components[0].Samples[0], Is.EqualTo(130));
        Assert.That(decoded.Components[0].Samples[1], Is.EqualTo(131));
        Assert.That(decoded.Components[1].Samples[0], Is.EqualTo(129));
        Assert.That(decoded.Components[1].Samples[1], Is.EqualTo(130));
        Assert.That(decoded.Components[2].Samples[0], Is.EqualTo(128));
        Assert.That(decoded.Components[2].Samples[1], Is.EqualTo(129));
    }

    [Test]
    public void EntropyEncoder_AllowsSixteenBitLosslessDifferenceCategory()
    {
        global::jpeg.JpegHuffmanTable table = LosslessSof3HuffmanDecoderTests.CreateSingleSymbolDcTable(16);
        global::jpeg.JpegLosslessHuffmanEntropyEncoder encoder = new global::jpeg.JpegLosslessHuffmanEntropyEncoder();

        encoder.EncodeDifference(table, 32768);
        encoder.FlushToByteBoundary();
        global::jpeg.JpegLosslessHuffmanEntropyDecoder decoder = new global::jpeg.JpegLosslessHuffmanEntropyDecoder(encoder.ToArray());

        Assert.That(decoder.DecodeDifference(table), Is.EqualTo(32768));
    }

    [Test]
    public void Encoder_WritesRestartMarkersAndRestartsPrediction()
    {
        global::jpeg.JpegFrameHeader frame = LosslessSof3HuffmanDecoderTests.CreateFrame(2, 1, 12, 1);
        global::jpeg.JpegScanHeader scan = LosslessSof3HuffmanDecoderTests.CreateScan(1, 1, 0);
        global::jpeg.JpegTableStore tables = LosslessSof3HuffmanDecoderTests.CreateTables(BaselineHuffmanEntropyDecoderTests.CreateDcTable());
        global::jpeg.JpegPlanarIntImageBuffer source = ExtendedSequentialSof1EncoderTests.CreateSource(frame);
        SetSample(source.Components[0], 0, 0, 2050);
        SetSample(source.Components[0], 1, 0, 2050);
        global::jpeg.JpegLosslessSof3HuffmanEncoder encoder = new global::jpeg.JpegLosslessSof3HuffmanEncoder();
        global::jpeg.JpegLosslessSof3HuffmanDecoder decoder = new global::jpeg.JpegLosslessSof3HuffmanDecoder();

        byte[] entropyData = encoder.EncodeWithRestartInterval(frame, scan, tables, source, 1);
        global::jpeg.JpegPlanarIntImageBuffer decoded = decoder.DecodeWithRestartInterval(frame, scan, tables, entropyData, 1);

        Assert.That(BaselineSof0EncoderTests.ContainsRestart0(entropyData), Is.True);
        Assert.That(decoded.Components[0].Samples[0], Is.EqualTo(2050));
        Assert.That(decoded.Components[0].Samples[1], Is.EqualTo(2050));
    }

    [Test]
    public void Encoder_AppliesPointTransformWhenSamplesAreAligned()
    {
        global::jpeg.JpegFrameHeader frame = LosslessSof3HuffmanDecoderTests.CreateFrame(2, 1, 8, 1);
        global::jpeg.JpegScanHeader scan = LosslessSof3HuffmanDecoderTests.CreateScan(1, 1, 1);
        global::jpeg.JpegTableStore tables = LosslessSof3HuffmanDecoderTests.CreateTables(BaselineHuffmanEntropyDecoderTests.CreateDcTable());
        global::jpeg.JpegPlanarIntImageBuffer source = ExtendedSequentialSof1EncoderTests.CreateSource(frame);
        SetSample(source.Components[0], 0, 0, 128);
        SetSample(source.Components[0], 1, 0, 130);
        global::jpeg.JpegLosslessSof3HuffmanEncoder encoder = new global::jpeg.JpegLosslessSof3HuffmanEncoder();
        global::jpeg.JpegLosslessSof3HuffmanDecoder decoder = new global::jpeg.JpegLosslessSof3HuffmanDecoder();

        byte[] entropyData = encoder.Encode(frame, scan, tables, source);
        global::jpeg.JpegPlanarIntImageBuffer decoded = decoder.Decode(frame, scan, tables, entropyData);

        Assert.That(decoded.Components[0].Samples[0], Is.EqualTo(128));
        Assert.That(decoded.Components[0].Samples[1], Is.EqualTo(130));
    }

    [Test]
    public void Encoder_RejectsInvalidLosslessInputs()
    {
        global::jpeg.JpegLosslessSof3HuffmanEncoder encoder = new global::jpeg.JpegLosslessSof3HuffmanEncoder();
        global::jpeg.JpegFrameHeader frame = LosslessSof3HuffmanDecoderTests.CreateFrame(1, 1, 8, 1);
        global::jpeg.JpegScanHeader scan = LosslessSof3HuffmanDecoderTests.CreateScan(1, 1, 0);
        global::jpeg.JpegTableStore tables = LosslessSof3HuffmanDecoderTests.CreateTables(BaselineHuffmanEntropyDecoderTests.CreateDcTable());
        global::jpeg.JpegPlanarIntImageBuffer source = ExtendedSequentialSof1EncoderTests.CreateSource(frame);

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                encoder.Encode(frame, scan, tables, null!);
            });

        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                global::jpeg.JpegPlanarIntImageBuffer badPrecision = ExtendedSequentialSof1EncoderTests.CreateSource(frame);
                badPrecision.Precision = 12;
                encoder.Encode(frame, scan, tables, badPrecision);
            });

        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                global::jpeg.JpegPlanarIntImageBuffer badSample = ExtendedSequentialSof1EncoderTests.CreateSource(frame);
                badSample.Components[0].Samples[0] = 256;
                encoder.Encode(frame, scan, tables, badSample);
            });

        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                global::jpeg.JpegPlanarIntImageBuffer unaligned = ExtendedSequentialSof1EncoderTests.CreateSource(frame);
                unaligned.Components[0].Samples[0] = 129;
                encoder.Encode(frame, LosslessSof3HuffmanDecoderTests.CreateScan(1, 1, 1), tables, unaligned);
            });

        Assert.Throws<System.ArgumentOutOfRangeException>(
            delegate
            {
                encoder.EncodeWithRestartInterval(frame, scan, tables, source, -1);
            });
    }

    public static void SetSample(global::jpeg.JpegPlanarIntComponentBuffer component, int x, int y, int value)
    {
        component.Samples[y * component.Stride + x] = value;
    }
}
