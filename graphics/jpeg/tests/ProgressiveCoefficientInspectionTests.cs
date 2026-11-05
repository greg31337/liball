namespace jpeg.Tests;

internal class ProgressiveCoefficientInspectionTests
{
    [Test]
    public void Inspector_ReadsAndRewritesTwelveBitExtendedSequentialCoefficients()
    {
        int[] samples = JpegEncoderProcessOptionsTests.CreateFilledSamples(8, 8, 2058);
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
        byte[] data = new global::jpeg.JpegWriter().WriteWithOptions(source, options);
        global::jpeg.IJpegInspector inspector = new global::jpeg.JpegInspectionService();

        global::jpeg.JpegCoefficientImage coefficients = inspector.ReadCoefficients(data);
        int original = coefficients.Components[0].Coefficients[0];
        coefficients.Components[0].Coefficients[0] = original + 1;
        byte[] rewritten = inspector.WriteCoefficients(coefficients);
        global::jpeg.JpegCoefficientImage reread = inspector.ReadCoefficients(rewritten);

        Assert.That(coefficients.Process, Is.EqualTo(global::jpeg.JpegProcess.ExtendedSequentialDct));
        Assert.That(coefficients.Precision, Is.EqualTo(12));
        Assert.That(global::jpeg.Jpeg.Identify(rewritten).Process, Is.EqualTo(global::jpeg.JpegProcess.ExtendedSequentialDct));
        Assert.That(reread.Components[0].Coefficients, Is.EqualTo(coefficients.Components[0].Coefficients));
        Assert.That(reread.Components[0].Coefficients[0], Is.EqualTo(original + 1));
    }

    [Test]
    public void Inspector_ReadsAndRewritesProgressiveCoefficientsWithoutRequantizing()
    {
        global::jpeg.JpegImage source = JpegWriteApiTests.CreateTwoBlockGrayscaleImage();
        global::jpeg.JpegWriteOptions options = new global::jpeg.JpegWriteOptions();
        options.Process = global::jpeg.JpegProcess.ProgressiveDct;
        byte[] progressive = new global::jpeg.JpegWriter().WriteWithOptions(source, options);
        global::jpeg.IJpegInspector inspector = new global::jpeg.JpegInspectionService();

        global::jpeg.JpegCoefficientImage coefficients = inspector.ReadCoefficients(progressive);
        int original = coefficients.Components[0].Coefficients[0];
        coefficients.Components[0].Coefficients[0] = original + 3;
        byte[] rewritten = inspector.WriteCoefficients(coefficients);
        global::jpeg.JpegInfo info = global::jpeg.Jpeg.Identify(rewritten);
        global::jpeg.JpegCoefficientImage reread = inspector.ReadCoefficients(rewritten);

        Assert.That(coefficients.Process, Is.EqualTo(global::jpeg.JpegProcess.ProgressiveDct));
        Assert.That(coefficients.Precision, Is.EqualTo(8));
        Assert.That(coefficients.Progressive, Is.True);
        Assert.That(coefficients.ScanHeaders.Count, Is.EqualTo(2));
        Assert.That(info.Process, Is.EqualTo(global::jpeg.JpegProcess.ProgressiveDct));
        Assert.That(info.ScanCount, Is.EqualTo(2));
        Assert.That(reread.Components[0].Coefficients, Is.EqualTo(coefficients.Components[0].Coefficients));
        Assert.That(reread.Components[0].Coefficients[0], Is.EqualTo(original + 3));
    }

    [Test]
    public void Inspector_WritesIndependentlyConstructedProgressiveCoefficientImage()
    {
        global::jpeg.JpegCoefficientImage coefficients = JpegCoefficientWriterTests.CreateStructuredCoefficientImage();
        coefficients.Process = global::jpeg.JpegProcess.ProgressiveDct;
        coefficients.Progressive = true;
        coefficients.Frame.Process = global::jpeg.JpegProcess.ProgressiveDct;
        coefficients.Frame.Marker = global::jpeg.JpegMarker.StartOfFrame2;
        coefficients.Frame.Value = global::jpeg.JpegConstants.MarkerStartOfFrame2;
        coefficients.Frame.Progressive = true;
        coefficients.ScanHeaders = new global::jpeg.JpegProgressiveSof2Encoder().CreateDefaultScanScript(coefficients.Frame);
        global::jpeg.IJpegInspector inspector = new global::jpeg.JpegInspectionService();

        byte[] data = inspector.WriteCoefficients(coefficients);
        global::jpeg.JpegCoefficientImage result = inspector.ReadCoefficients(data);

        Assert.That(global::jpeg.Jpeg.Identify(data).Process, Is.EqualTo(global::jpeg.JpegProcess.ProgressiveDct));
        Assert.That(result.ScanHeaders.Count, Is.EqualTo(2));
        Assert.That(result.Components[0].Coefficients[0], Is.EqualTo(2));
        Assert.That(result.Components[0].Coefficients.Skip(1), Is.All.Zero);
    }

    [Test]
    public void Inspector_RoundTripsProgressiveCoefficientsThroughStreamAndFileForms()
    {
        global::jpeg.JpegWriteOptions options = new global::jpeg.JpegWriteOptions();
        options.Progressive = true;
        byte[] data = new global::jpeg.JpegWriter().WriteWithOptions(
            JpegWriteApiTests.CreateGrayscaleImage(8, 8, 138),
            options);
        global::jpeg.IJpegInspector inspector = new global::jpeg.JpegInspectionService();
        System.IO.MemoryStream input = new System.IO.MemoryStream(data);
        global::jpeg.JpegCoefficientImage fromStream = inspector.ReadCoefficientsStream(input);
        System.IO.MemoryStream output = new System.IO.MemoryStream();
        inspector.WriteCoefficientsStream(fromStream, output);
        string path = JpegFilePathApiTests.CreateTempJpegPath();

        try
        {
            inspector.WriteCoefficientsFile(path, fromStream);
            global::jpeg.JpegCoefficientImage fromBytes = inspector.ReadCoefficients(output.ToArray());
            global::jpeg.JpegCoefficientImage fromFile = inspector.ReadCoefficientsFile(path);

            Assert.That(fromBytes.Process, Is.EqualTo(global::jpeg.JpegProcess.ProgressiveDct));
            Assert.That(fromFile.Process, Is.EqualTo(global::jpeg.JpegProcess.ProgressiveDct));
            Assert.That(fromBytes.Components[0].Coefficients, Is.EqualTo(fromStream.Components[0].Coefficients));
            Assert.That(fromFile.Components[0].Coefficients, Is.EqualTo(fromStream.Components[0].Coefficients));
            Assert.That(input.CanRead, Is.True);
            Assert.That(output.CanWrite, Is.True);
        }
        finally
        {
            System.IO.File.Delete(path);
        }
    }
}
