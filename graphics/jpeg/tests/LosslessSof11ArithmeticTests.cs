namespace jpeg.Tests;

internal class LosslessSof11ArithmeticTests
{
    [Test]
    public void Encoder_EncodesGrayscaleSamplesThatDecodeBackWithDefaultConditioning()
    {
        global::jpeg.JpegFrameHeader frame = CreateFrame(3, 2, 12, 1);
        global::jpeg.JpegScanHeader scan = LosslessSof3HuffmanDecoderTests.CreateScan(1, 1, 0);
        global::jpeg.JpegTableStore tables = new global::jpeg.JpegTableStore();
        global::jpeg.JpegPlanarIntImageBuffer source = ExtendedSequentialSof1EncoderTests.CreateSource(frame);
        SetSample(source.Components[0], 0, 0, 2050);
        SetSample(source.Components[0], 1, 0, 2051);
        SetSample(source.Components[0], 2, 0, 2052);
        SetSample(source.Components[0], 0, 1, 2049);
        SetSample(source.Components[0], 1, 1, 2050);
        SetSample(source.Components[0], 2, 1, 2051);
        global::jpeg.JpegLosslessSof11ArithmeticEncoder encoder = new global::jpeg.JpegLosslessSof11ArithmeticEncoder();
        global::jpeg.JpegLosslessSof11ArithmeticDecoder decoder = new global::jpeg.JpegLosslessSof11ArithmeticDecoder();

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
    public void Encoder_EncodesInterleavedComponentsThatDecodeBackWithCustomConditioning()
    {
        global::jpeg.JpegFrameHeader frame = CreateFrame(2, 1, 8, 3);
        global::jpeg.JpegScanHeader scan = LosslessSof3HuffmanDecoderTests.CreateScan(3, 1, 0);
        global::jpeg.JpegTableStore tables = new global::jpeg.JpegTableStore();
        tables.ArithmeticConditioningTables.Add(ArithmeticConditioningTableWriterTests.CreateDcTable(0, 0, 1));
        global::jpeg.JpegPlanarIntImageBuffer source = ExtendedSequentialSof1EncoderTests.CreateSource(frame);
        SetSample(source.Components[0], 0, 0, 130);
        SetSample(source.Components[0], 1, 0, 131);
        SetSample(source.Components[1], 0, 0, 129);
        SetSample(source.Components[1], 1, 0, 130);
        SetSample(source.Components[2], 0, 0, 128);
        SetSample(source.Components[2], 1, 0, 129);
        global::jpeg.JpegLosslessSof11ArithmeticEncoder encoder = new global::jpeg.JpegLosslessSof11ArithmeticEncoder();
        global::jpeg.JpegLosslessSof11ArithmeticDecoder decoder = new global::jpeg.JpegLosslessSof11ArithmeticDecoder();

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
    public void EntropyCoder_RoundTripsSixteenBitLosslessDifferenceCategory()
    {
        global::jpeg.JpegLosslessArithmeticEntropyEncoder encoder = new global::jpeg.JpegLosslessArithmeticEntropyEncoder();
        global::jpeg.JpegArithmeticConditioningTable table = encoder.CreateDefaultDcConditioningTable(0);

        encoder.EncodeDifference(table, 0, 32768);
        encoder.Finish();
        global::jpeg.JpegLosslessArithmeticEntropyDecoder decoder = new global::jpeg.JpegLosslessArithmeticEntropyDecoder(encoder.ToArray());

        Assert.That(decoder.DecodeDifference(table, 0), Is.EqualTo(32768));
    }

    [Test]
    public void Encoder_WritesRestartMarkersAndRestartsPrediction()
    {
        global::jpeg.JpegFrameHeader frame = CreateFrame(2, 1, 12, 1);
        global::jpeg.JpegScanHeader scan = LosslessSof3HuffmanDecoderTests.CreateScan(1, 1, 0);
        global::jpeg.JpegTableStore tables = new global::jpeg.JpegTableStore();
        global::jpeg.JpegPlanarIntImageBuffer source = ExtendedSequentialSof1EncoderTests.CreateSource(frame);
        SetSample(source.Components[0], 0, 0, 2050);
        SetSample(source.Components[0], 1, 0, 2050);
        global::jpeg.JpegLosslessSof11ArithmeticEncoder encoder = new global::jpeg.JpegLosslessSof11ArithmeticEncoder();
        global::jpeg.JpegLosslessSof11ArithmeticDecoder decoder = new global::jpeg.JpegLosslessSof11ArithmeticDecoder();

        byte[] entropyData = encoder.EncodeWithRestartInterval(frame, scan, tables, source, 1);
        global::jpeg.JpegPlanarIntImageBuffer decoded = decoder.DecodeWithRestartInterval(frame, scan, tables, entropyData, 1);

        Assert.That(BaselineSof0EncoderTests.ContainsRestart0(entropyData), Is.True);
        Assert.That(decoded.Components[0].Samples[0], Is.EqualTo(2050));
        Assert.That(decoded.Components[0].Samples[1], Is.EqualTo(2050));
    }

    [Test]
    public void Encoder_AppliesPointTransformWhenSamplesAreAligned()
    {
        global::jpeg.JpegFrameHeader frame = CreateFrame(2, 1, 8, 1);
        global::jpeg.JpegScanHeader scan = LosslessSof3HuffmanDecoderTests.CreateScan(1, 1, 1);
        global::jpeg.JpegTableStore tables = new global::jpeg.JpegTableStore();
        global::jpeg.JpegPlanarIntImageBuffer source = ExtendedSequentialSof1EncoderTests.CreateSource(frame);
        SetSample(source.Components[0], 0, 0, 128);
        SetSample(source.Components[0], 1, 0, 130);
        global::jpeg.JpegLosslessSof11ArithmeticEncoder encoder = new global::jpeg.JpegLosslessSof11ArithmeticEncoder();
        global::jpeg.JpegLosslessSof11ArithmeticDecoder decoder = new global::jpeg.JpegLosslessSof11ArithmeticDecoder();

        byte[] entropyData = encoder.Encode(frame, scan, tables, source);
        global::jpeg.JpegPlanarIntImageBuffer decoded = decoder.Decode(frame, scan, tables, entropyData);

        Assert.That(decoded.Components[0].Samples[0], Is.EqualTo(128));
        Assert.That(decoded.Components[0].Samples[1], Is.EqualTo(130));
    }

    [Test]
    public void Decoder_RejectsInvalidSof11Inputs()
    {
        global::jpeg.JpegLosslessSof11ArithmeticDecoder decoder = new global::jpeg.JpegLosslessSof11ArithmeticDecoder();
        global::jpeg.JpegTableStore tables = new global::jpeg.JpegTableStore();

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                decoder.Decode(LosslessSof3HuffmanDecoderTests.CreateFrame(1, 1, 8, 1), LosslessSof3HuffmanDecoderTests.CreateScan(1, 1, 0), tables, new byte[] { 0 });
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                decoder.Decode(CreateFrame(1, 1, 8, 1), LosslessSof3HuffmanDecoderTests.CreateScan(1, 8, 0), tables, new byte[] { 0 });
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                decoder.Decode(CreateSubsampledFrame(), LosslessSof3HuffmanDecoderTests.CreateScan(1, 1, 0), tables, new byte[] { 0 });
            });
    }

    [Test]
    public void Encoder_RejectsInvalidSof11Inputs()
    {
        global::jpeg.JpegLosslessSof11ArithmeticEncoder encoder = new global::jpeg.JpegLosslessSof11ArithmeticEncoder();
        global::jpeg.JpegFrameHeader frame = CreateFrame(1, 1, 8, 1);
        global::jpeg.JpegScanHeader scan = LosslessSof3HuffmanDecoderTests.CreateScan(1, 1, 0);
        global::jpeg.JpegTableStore tables = new global::jpeg.JpegTableStore();
        global::jpeg.JpegPlanarIntImageBuffer source = ExtendedSequentialSof1EncoderTests.CreateSource(frame);

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                encoder.Encode(frame, scan, tables, null!);
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

    public static global::jpeg.JpegFrameHeader CreateFrame(int width, int height, int precision, int componentCount)
    {
        global::jpeg.JpegFrameHeader frame = LosslessSof3HuffmanDecoderTests.CreateFrame(width, height, precision, componentCount);
        frame.Marker = global::jpeg.JpegMarker.StartOfFrame11;
        frame.Value = global::jpeg.JpegConstants.MarkerStartOfFrame11;
        frame.Process = global::jpeg.JpegProcess.LosslessSequentialArithmetic;
        frame.ArithmeticCoding = true;
        return frame;
    }

    public static global::jpeg.JpegFrameHeader CreateSubsampledFrame()
    {
        global::jpeg.JpegFrameHeader frame = CreateFrame(1, 1, 8, 1);
        frame.Components[0].HorizontalSamplingFactor = 2;
        return frame;
    }

    public static void SetSample(global::jpeg.JpegPlanarIntComponentBuffer component, int x, int y, int value)
    {
        component.Samples[y * component.Stride + x] = value;
    }
}
