namespace jpeg.Tests;

internal class JpegEncoderProcessOptionsTests
{
    [Test]
    public void WriteOptions_EncodeProgressiveHuffmanDctStream()
    {
        global::jpeg.JpegImage source = JpegWriteApiTests.CreateTwoBlockGrayscaleImage();
        global::jpeg.JpegWriteOptions options = new global::jpeg.JpegWriteOptions();
        options.Process = global::jpeg.JpegProcess.ProgressiveDct;
        options.SamplePrecision = 8;
        options.EntropyCoding = global::jpeg.JpegEntropyCoding.Huffman;

        byte[] data = new global::jpeg.JpegWriter().WriteWithOptions(source, options);
        global::jpeg.JpegInfo info = global::jpeg.JpegIdentifier.Identify(data);
        global::jpeg.JpegImageReader reader = new global::jpeg.JpegImageReader();
        System.Collections.Generic.List<global::jpeg.JpegProgressiveScanData> scans = reader.ExtractProgressiveScanData(
            data,
            new global::jpeg.JpegReadOptions(),
            default(System.Threading.CancellationToken));
        global::jpeg.JpegPlanarImageBuffer decoded = new global::jpeg.JpegProgressiveSof2FullImageDecoder().Decode(
            info.Frame,
            scans,
            reader.CreateTableStore(info));

        Assert.That(info.Process, Is.EqualTo(global::jpeg.JpegProcess.ProgressiveDct));
        Assert.That(info.ArithmeticCoding, Is.False);
        Assert.That(info.ScanCount, Is.EqualTo(2));
        Assert.That(decoded.Components[0].Samples[0], Is.EqualTo(138));
        Assert.That(decoded.Components[0].Samples[8], Is.EqualTo(143));
    }

    [Test]
    public void WriteOptions_EncodeTwelveBitExtendedSequentialStream()
    {
        int[] samples = CreateFilledSamples(8, 8, 2058);
        global::jpeg.JpegImage source = new global::jpeg.JpegImageFactory().CreatePackedImageFromIntSamples(
            samples,
            8,
            8,
            1,
            8,
            12,
            global::jpeg.JpegColorSpace.Grayscale);
        global::jpeg.JpegWriteOptions options = new global::jpeg.JpegWriteOptions();
        options.Process = global::jpeg.JpegProcess.ExtendedSequentialDct;
        options.SamplePrecision = 12;
        options.EntropyCoding = global::jpeg.JpegEntropyCoding.Huffman;

        byte[] data = new global::jpeg.JpegWriter().WriteWithOptions(source, options);
        global::jpeg.JpegInfo info = global::jpeg.JpegIdentifier.Identify(data);
        global::jpeg.JpegImageReader reader = new global::jpeg.JpegImageReader();
        byte[] entropyData = reader.ExtractSingleScanEntropyData(data, new global::jpeg.JpegReadOptions());
        global::jpeg.JpegPlanarIntImageBuffer decoded = new global::jpeg.JpegExtendedSequentialSof1Decoder().Decode(
            info.Frame,
            info.LastScanHeader,
            reader.CreateTableStore(info),
            entropyData);

        Assert.That(info.Process, Is.EqualTo(global::jpeg.JpegProcess.ExtendedSequentialDct));
        Assert.That(info.Precision, Is.EqualTo(12));
        Assert.That(decoded.Components[0].Samples[0], Is.EqualTo(2058));
        Assert.That(decoded.Components[0].Samples[63], Is.EqualTo(2058));
    }

    [Test]
    public void WriteOptions_EncodeLosslessHuffmanWithPredictorAndPointTransform()
    {
        int[] samples = new int[] { 2050, 2052, 2048, 2050, 2052, 2054 };
        global::jpeg.JpegImage source = new global::jpeg.JpegImageFactory().CreatePackedImageFromIntSamples(
            samples,
            3,
            2,
            1,
            3,
            12,
            global::jpeg.JpegColorSpace.Grayscale);
        global::jpeg.JpegWriteOptions options = new global::jpeg.JpegWriteOptions();
        options.Process = global::jpeg.JpegProcess.LosslessSequential;
        options.SamplePrecision = 12;
        options.EntropyCoding = global::jpeg.JpegEntropyCoding.Huffman;
        options.LosslessPredictor = global::jpeg.JpegLosslessPredictorSelection.LeftPlusAboveMinusUpperLeft;
        options.PointTransform = 1;

        byte[] data = new global::jpeg.JpegWriter().WriteWithOptions(source, options);
        global::jpeg.JpegInfo info = global::jpeg.JpegIdentifier.Identify(data);
        global::jpeg.JpegImageReader reader = new global::jpeg.JpegImageReader();
        byte[] entropyData = reader.ExtractSingleScanEntropyData(data, new global::jpeg.JpegReadOptions());
        global::jpeg.JpegPlanarIntImageBuffer decoded = new global::jpeg.JpegLosslessSof3HuffmanDecoder().Decode(
            info.Frame,
            info.LastScanHeader,
            reader.CreateTableStore(info),
            entropyData);

        Assert.That(info.Process, Is.EqualTo(global::jpeg.JpegProcess.LosslessSequential));
        Assert.That(info.LastScanHeader.SpectralStart, Is.EqualTo(4));
        Assert.That(info.LastScanHeader.SuccessiveLow, Is.EqualTo(1));
        Assert.That(decoded.Components[0].Samples[0], Is.EqualTo(2050));
        Assert.That(decoded.Components[0].Samples[decoded.Components[0].Stride + 2], Is.EqualTo(2054));
    }

    [Test]
    public void WriteOptions_EncodeLosslessArithmeticStream()
    {
        global::jpeg.JpegImage source = JpegWriteApiTests.CreateGrayscaleImage(3, 2, 130);
        global::jpeg.JpegWriteOptions options = new global::jpeg.JpegWriteOptions();
        options.Process = global::jpeg.JpegProcess.LosslessSequentialArithmetic;
        options.SamplePrecision = 8;
        options.EntropyCoding = global::jpeg.JpegEntropyCoding.Arithmetic;
        options.LosslessPredictor = global::jpeg.JpegLosslessPredictorSelection.Above;

        byte[] data = new global::jpeg.JpegWriter().WriteWithOptions(source, options);
        global::jpeg.JpegInfo info = global::jpeg.JpegIdentifier.Identify(data);
        global::jpeg.JpegImageReader reader = new global::jpeg.JpegImageReader();
        byte[] entropyData = reader.ExtractSingleScanEntropyData(data, new global::jpeg.JpegReadOptions());
        global::jpeg.JpegPlanarIntImageBuffer decoded = new global::jpeg.JpegLosslessSof11ArithmeticDecoder().Decode(
            info.Frame,
            info.LastScanHeader,
            reader.CreateTableStore(info),
            entropyData);

        Assert.That(info.Process, Is.EqualTo(global::jpeg.JpegProcess.LosslessSequentialArithmetic));
        Assert.That(info.ArithmeticCoding, Is.True);
        Assert.That(info.HuffmanTableCount, Is.EqualTo(0));
        Assert.That(decoded.Components[0].Samples[0], Is.EqualTo(130));
        Assert.That(decoded.Components[0].Samples[decoded.Components[0].Stride + 2], Is.EqualTo(130));
    }

    [Test]
    public void WriteOptions_RejectConflictsAndRecognizedUnsupportedModes()
    {
        global::jpeg.JpegImage source = JpegWriteApiTests.CreateGrayscaleImage(8, 8, 128);
        global::jpeg.JpegWriteOptions entropyConflict = new global::jpeg.JpegWriteOptions();
        entropyConflict.Process = global::jpeg.JpegProcess.BaselineDct;
        entropyConflict.EntropyCoding = global::jpeg.JpegEntropyCoding.Arithmetic;
        global::jpeg.JpegWriteOptions differential = new global::jpeg.JpegWriteOptions();
        differential.Process = global::jpeg.JpegProcess.DifferentialSequentialDct;
        global::jpeg.JpegWriteOptions hierarchical = new global::jpeg.JpegWriteOptions();
        hierarchical.Hierarchical = true;

        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                new global::jpeg.JpegWriter().WriteWithOptions(source, entropyConflict);
            });
        Assert.Throws<global::jpeg.JpegUnsupportedException>(
            delegate
            {
                new global::jpeg.JpegWriter().WriteWithOptions(source, differential);
            });
        Assert.Throws<global::jpeg.JpegUnsupportedException>(
            delegate
            {
                new global::jpeg.JpegWriter().WriteWithOptions(source, hierarchical);
            });
    }

    internal static int[] CreateFilledSamples(int width, int height, int value)
    {
        int[] samples = new int[width * height];
        int index = 0;
        while (index < samples.Length)
        {
            samples[index] = value;
            index = index + 1;
        }

        return samples;
    }
}
