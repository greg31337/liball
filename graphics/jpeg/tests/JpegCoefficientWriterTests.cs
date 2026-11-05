namespace jpeg.Tests;

internal class JpegCoefficientWriterTests
{
    [Test]
    public void WriteCoefficients_RoundTripsBaselineGrayscaleCoefficients()
    {
        byte[] source = JpegReadApiTests.CreateReadableGrayscaleJpeg(2);
        global::jpeg.JpegCoefficientImage coefficients = global::jpeg.JpegInspector.ReadCoefficients(source);
        coefficients.Metadata.Comments.Add("coefficient note");

        byte[] output = global::jpeg.JpegInspector.WriteCoefficients(coefficients);

        global::jpeg.JpegInfo info = global::jpeg.Jpeg.Identify(output);
        global::jpeg.JpegCoefficientImage reread = global::jpeg.JpegInspector.ReadCoefficients(output);
        Assert.That(info.Width, Is.EqualTo(8));
        Assert.That(info.Height, Is.EqualTo(8));
        Assert.That(info.CommentCount, Is.EqualTo(1));
        Assert.That(info.Metadata.Comments[0], Is.EqualTo("coefficient note"));
        Assert.That(reread.Width, Is.EqualTo(coefficients.Width));
        Assert.That(reread.Height, Is.EqualTo(coefficients.Height));
        Assert.That(reread.QuantizationTables.Count, Is.EqualTo(coefficients.QuantizationTables.Count));
        Assert.That(reread.HuffmanTables.Count, Is.EqualTo(coefficients.HuffmanTables.Count));
        Assert.That(reread.Components[0].Coefficients[0], Is.EqualTo(2));
    }

    [Test]
    public void WriteCoefficients_WritesRestartMarkersToStream()
    {
        byte[] source = JpegCoefficientReaderTests.CreateTwoBlockRestartCoefficientJpeg();
        global::jpeg.JpegCoefficientImage coefficients = global::jpeg.JpegInspector.ReadCoefficients(source);
        System.IO.MemoryStream stream = new System.IO.MemoryStream();

        global::jpeg.JpegInspector.WriteCoefficients(coefficients, stream);

        byte[] output = stream.ToArray();
        global::jpeg.JpegInfo info = global::jpeg.Jpeg.Identify(output);
        global::jpeg.JpegCoefficientImage reread = global::jpeg.JpegInspector.ReadCoefficients(output);
        Assert.That(info.HasRestartInterval, Is.True);
        Assert.That(info.RestartInterval, Is.EqualTo(1));
        Assert.That(info.RestartMarkerCount, Is.EqualTo(1));
        Assert.That(reread.Components[0].Coefficients[0], Is.EqualTo(2));
        Assert.That(reread.Components[0].Coefficients[64], Is.EqualTo(2));
    }

    [Test]
    public void WriteCoefficientsFile_WritesPath()
    {
        string path = JpegFilePathApiTests.CreateTempJpegPath();
        byte[] source = JpegReadApiTests.CreateReadableGrayscaleJpeg(3);
        global::jpeg.JpegCoefficientImage coefficients = global::jpeg.JpegInspector.ReadCoefficients(source);

        global::jpeg.JpegInspector.WriteCoefficientsFile(path, coefficients);

        global::jpeg.JpegCoefficientImage reread = global::jpeg.JpegInspector.ReadCoefficientsFile(path);
        Assert.That(System.IO.File.Exists(path), Is.True);
        Assert.That(reread.Components[0].Coefficients[0], Is.EqualTo(3));

        System.IO.File.Delete(path);
    }

    [Test]
    public void WriteCoefficients_RejectsLosslessProcessWithoutDctCoefficients()
    {
        byte[] source = JpegReadApiTests.CreateReadableGrayscaleJpeg(2);
        global::jpeg.JpegCoefficientImage coefficients = global::jpeg.JpegInspector.ReadCoefficients(source);
        coefficients.Process = global::jpeg.JpegProcess.LosslessSequential;

        Assert.Throws<global::jpeg.JpegUnsupportedException>(
            delegate
            {
                global::jpeg.JpegInspector.WriteCoefficients(coefficients);
            });
    }

    [Test]
    public void WriteCoefficients_RejectsInvalidModel()
    {
        byte[] source = JpegReadApiTests.CreateReadableGrayscaleJpeg(2);
        global::jpeg.JpegCoefficientImage coefficients = global::jpeg.JpegInspector.ReadCoefficients(source);
        global::jpeg.JpegCoefficientImage missingTables = global::jpeg.JpegInspector.ReadCoefficients(source);
        global::jpeg.JpegCoefficientImage shortBuffer = global::jpeg.JpegInspector.ReadCoefficients(source);
        missingTables.HuffmanTables.Clear();
        shortBuffer.Components[0].Coefficients = new int[1];

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                global::jpeg.JpegInspector.WriteCoefficients(null!);
            });
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                global::jpeg.JpegInspector.WriteCoefficients(missingTables);
            });
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                global::jpeg.JpegInspector.WriteCoefficients(shortBuffer);
            });
        Assert.That(coefficients.Components[0].Coefficients[0], Is.EqualTo(2));
    }

    [Test]
    public void WriteCoefficients_WritesIndependentlyConstructedStructuredModel()
    {
        global::jpeg.JpegCoefficientImage coefficients = CreateStructuredCoefficientImage();
        coefficients.HasRestartInterval = true;
        coefficients.RestartInterval = 0;
        global::jpeg.JpegArithmeticConditioningTable dcConditioning = new global::jpeg.JpegArithmeticConditioningTable();
        dcConditioning.TableClass = 0;
        dcConditioning.Identifier = 0;
        dcConditioning.ConditioningValue = 0xA3;
        dcConditioning.Lower = 3;
        dcConditioning.Upper = 10;
        global::jpeg.JpegArithmeticConditioningTable acConditioning = new global::jpeg.JpegArithmeticConditioningTable();
        acConditioning.TableClass = 1;
        acConditioning.Identifier = 1;
        acConditioning.ConditioningValue = 42;
        acConditioning.Kx = 42;
        coefficients.ArithmeticConditioningTables.Add(dcConditioning);
        coefficients.ArithmeticConditioningTables.Add(acConditioning);

        byte[] output = global::jpeg.JpegInspector.WriteCoefficients(coefficients);

        global::jpeg.JpegInfo info = global::jpeg.Jpeg.Identify(output);
        global::jpeg.JpegCoefficientImage reread = global::jpeg.JpegInspector.ReadCoefficients(output);
        Assert.That(info.Width, Is.EqualTo(8));
        Assert.That(info.Height, Is.EqualTo(8));
        Assert.That(info.Precision, Is.EqualTo(8));
        Assert.That(info.HasRestartInterval, Is.True);
        Assert.That(info.RestartInterval, Is.Zero);
        Assert.That(info.ArithmeticConditioningTables.Count, Is.EqualTo(2));
        Assert.That(reread.EntropyCoding, Is.EqualTo(global::jpeg.JpegEntropyCoding.Huffman));
        Assert.That(reread.Components[0].Coefficients[0], Is.EqualTo(2));
    }

    [Test]
    public void WriteCoefficients_RejectsInconsistentStructuredReferences()
    {
        global::jpeg.JpegCoefficientImage mismatchedSummary = CreateStructuredCoefficientImage();
        global::jpeg.JpegCoefficientImage mismatchedGeometry = CreateStructuredCoefficientImage();
        global::jpeg.JpegCoefficientImage missingQuantization = CreateStructuredCoefficientImage();
        global::jpeg.JpegCoefficientImage missingHuffman = CreateStructuredCoefficientImage();
        mismatchedSummary.Width = 9;
        mismatchedGeometry.Components[0].BlocksPerRow = 2;
        missingQuantization.QuantizationTables.Clear();
        missingHuffman.HuffmanTables.RemoveAt(1);

        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                global::jpeg.JpegInspector.WriteCoefficients(mismatchedSummary);
            });
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                global::jpeg.JpegInspector.WriteCoefficients(mismatchedGeometry);
            });
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                global::jpeg.JpegInspector.WriteCoefficients(missingQuantization);
            });
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                global::jpeg.JpegInspector.WriteCoefficients(missingHuffman);
            });
    }

    public static global::jpeg.JpegCoefficientImage CreateStructuredCoefficientImage()
    {
        global::jpeg.JpegCoefficientImage image = new global::jpeg.JpegCoefficientImage();
        image.Frame = BaselineSof0FullImageDecoderTests.CreateFrame(8, 8);
        image.Frame.Components.Add(BaselineSof0FullImageDecoderTests.CreateFrameComponent(1, 1, 1, 0));
        image.Frame.ComponentCount = 1;
        image.ScanHeaders.Add(BaselineSof0FullImageDecoderTests.CreateScan(image.Frame));
        global::jpeg.JpegTableStore tables = BaselineSof0FullImageDecoderTests.CreateTableStore(40);
        image.QuantizationTables = tables.QuantizationTables;
        image.HuffmanTables = tables.HuffmanTables;
        global::jpeg.JpegCoefficientComponent component = new global::jpeg.JpegCoefficientComponent();
        component.ComponentIndex = 0;
        component.Identifier = 1;
        component.HorizontalSamplingFactor = 1;
        component.VerticalSamplingFactor = 1;
        component.QuantizationTableIdentifier = 0;
        component.BlocksPerRow = 1;
        component.BlockRows = 1;
        component.Coefficients = new int[global::jpeg.JpegConstants.BlockSampleCount];
        component.Coefficients[0] = 2;
        image.Components.Add(component);
        return image;
    }
}
