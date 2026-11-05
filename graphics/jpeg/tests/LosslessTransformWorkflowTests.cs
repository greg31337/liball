namespace jpeg.Tests;

internal class LosslessTransformWorkflowTests
{
    [TestCase("rotate90", "bytes")]
    [TestCase("rotate90", "span")]
    [TestCase("rotate90", "stream")]
    [TestCase("rotate90", "file")]
    [TestCase("rotate180", "bytes")]
    [TestCase("rotate180", "span")]
    [TestCase("rotate180", "stream")]
    [TestCase("rotate180", "file")]
    [TestCase("rotate270", "bytes")]
    [TestCase("rotate270", "span")]
    [TestCase("rotate270", "stream")]
    [TestCase("rotate270", "file")]
    [TestCase("flipHorizontal", "bytes")]
    [TestCase("flipHorizontal", "span")]
    [TestCase("flipHorizontal", "stream")]
    [TestCase("flipHorizontal", "file")]
    [TestCase("flipVertical", "bytes")]
    [TestCase("flipVertical", "span")]
    [TestCase("flipVertical", "stream")]
    [TestCase("flipVertical", "file")]
    [TestCase("transpose", "bytes")]
    [TestCase("transpose", "span")]
    [TestCase("transpose", "stream")]
    [TestCase("transpose", "file")]
    [TestCase("transverse", "bytes")]
    [TestCase("transverse", "span")]
    [TestCase("transverse", "stream")]
    [TestCase("transverse", "file")]
    [TestCase("crop", "bytes")]
    [TestCase("crop", "span")]
    [TestCase("crop", "stream")]
    [TestCase("crop", "file")]
    [TestCase("grayscale", "bytes")]
    [TestCase("grayscale", "span")]
    [TestCase("grayscale", "stream")]
    [TestCase("grayscale", "file")]
    public void EveryTransform_WorksThroughEveryInputOutputForm(string operation, string form)
    {
        byte[] source = operation == "grayscale" ? CreateColorJpeg(false) : CreateCoefficientJpeg();
        global::jpeg.JpegCoefficientImage sourceCoefficients = global::jpeg.JpegInspector.ReadCoefficients(source);

        byte[] result = RunForm(operation, form, source);

        AssertTransformResult(operation, result, sourceCoefficients);
    }

    [TestCase("rotate90")]
    [TestCase("rotate180")]
    [TestCase("rotate270")]
    [TestCase("flipHorizontal")]
    [TestCase("flipVertical")]
    [TestCase("transpose")]
    [TestCase("transverse")]
    public void ReversibleTransforms_PreserveEveryCoefficientExactly(string operation)
    {
        byte[] source = CreateCoefficientJpegWithAcValues();
        global::jpeg.JpegCoefficientImage expected = global::jpeg.JpegInspector.ReadCoefficients(source);

        byte[] transformed = RunBytes(operation, source, default(System.Threading.CancellationToken));
        byte[] restored = RunBytes(GetInverseOperation(operation), transformed, default(System.Threading.CancellationToken));
        global::jpeg.JpegCoefficientImage actual = global::jpeg.JpegInspector.ReadCoefficients(restored);

        Assert.That(actual.Width, Is.EqualTo(expected.Width));
        Assert.That(actual.Height, Is.EqualTo(expected.Height));
        Assert.That(actual.Components.Count, Is.EqualTo(expected.Components.Count));
        int componentIndex = 0;
        while (componentIndex < expected.Components.Count)
        {
            Assert.That(
                actual.Components[componentIndex].Coefficients,
                Is.EqualTo(expected.Components[componentIndex].Coefficients));
            componentIndex = componentIndex + 1;
        }
    }

    [TestCase(global::jpeg.JpegMetadataPolicy.PreserveAll, 5, true, true)]
    [TestCase(global::jpeg.JpegMetadataPolicy.PreserveKnown, 4, false, true)]
    [TestCase(global::jpeg.JpegMetadataPolicy.StripAll, 0, false, false)]
    [TestCase(global::jpeg.JpegMetadataPolicy.StripUnsafe, 2, false, false)]
    [TestCase(global::jpeg.JpegMetadataPolicy.ReplaceSelected, 4, false, true)]
    public void Transform_AppliesEveryMetadataPolicy(
        global::jpeg.JpegMetadataPolicy policy,
        int markerCount,
        bool hasUnknown,
        bool hasPersonalMetadata)
    {
        byte[] source = CreateMetadataJpeg();
        global::jpeg.JpegTransformOptions options = new global::jpeg.JpegTransformOptions();
        options.MetadataPolicy = policy;
        global::jpeg.IJpegTransformer transformer = new global::jpeg.JpegTransformer();

        byte[] result = transformer.Rotate180WithOptionsAndCancellation(
            source,
            options,
            default(System.Threading.CancellationToken));

        global::jpeg.JpegInfo info = global::jpeg.Jpeg.Identify(result);
        Assert.That(info.Metadata.RawMarkers.Count, Is.EqualTo(markerCount));
        Assert.That(MetadataPolicyTests.ContainsRawMarker(info.Metadata, 0xFFF0), Is.EqualTo(hasUnknown));
        Assert.That(info.Metadata.HasExif, Is.EqualTo(hasPersonalMetadata));
        Assert.That(info.Metadata.Comments.Count > 0, Is.EqualTo(hasPersonalMetadata));
        Assert.That(info.Metadata.HasJfif, Is.EqualTo(policy != global::jpeg.JpegMetadataPolicy.StripAll));
        Assert.That(info.Metadata.HasAdobe, Is.EqualTo(policy != global::jpeg.JpegMetadataPolicy.StripAll));
    }

    [TestCase("rotate90")]
    [TestCase("rotate180")]
    [TestCase("rotate270")]
    [TestCase("flipHorizontal")]
    [TestCase("flipVertical")]
    [TestCase("transpose")]
    [TestCase("transverse")]
    [TestCase("crop")]
    [TestCase("grayscale")]
    public void EveryTransform_HonorsPreCanceledOperations(string operation)
    {
        byte[] source = operation == "grayscale" ? CreateColorJpeg(false) : CreateCoefficientJpeg();
        System.Threading.CancellationTokenSource cancellation = new System.Threading.CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<System.OperationCanceledException>(
            delegate
            {
                RunBytes(operation, source, cancellation.Token);
            });
    }

    [Test]
    public void ProgressiveGrayscaleConversion_PreservesAllLuminanceCoefficients()
    {
        byte[] source = CreateColorJpeg(true);
        global::jpeg.JpegCoefficientImage expected = global::jpeg.JpegInspector.ReadCoefficients(source);
        global::jpeg.IJpegTransformer transformer = new global::jpeg.JpegTransformer();

        byte[] result = transformer.ConvertToGrayscale(source);

        global::jpeg.JpegCoefficientImage actual = global::jpeg.JpegInspector.ReadCoefficients(result);
        Assert.That(actual.Process, Is.EqualTo(global::jpeg.JpegProcess.ProgressiveDct));
        Assert.That(actual.Components.Count, Is.EqualTo(1));
        Assert.That(actual.ScanHeaders.Count, Is.GreaterThan(1));
        Assert.That(actual.Components[0].Coefficients, Is.EqualTo(expected.Components[0].Coefficients));
    }

    public static byte[] RunForm(string operation, string form, byte[] source)
    {
        if (form == "bytes")
        {
            return RunBytes(operation, source, default(System.Threading.CancellationToken));
        }
        if (form == "span")
        {
            return RunSpan(operation, source);
        }
        if (form == "stream")
        {
            return RunStream(operation, source);
        }
        if (form == "file")
        {
            return RunFile(operation, source);
        }

        throw new System.ArgumentOutOfRangeException(nameof(form));
    }

    public static byte[] RunBytes(string operation, byte[] source, System.Threading.CancellationToken cancellationToken)
    {
        global::jpeg.IJpegTransformer transformer = new global::jpeg.JpegTransformer();
        global::jpeg.JpegTransformOptions options = new global::jpeg.JpegTransformOptions();
        if (operation == "rotate90")
        {
            return transformer.Rotate90WithOptionsAndCancellation(source, options, cancellationToken);
        }
        if (operation == "rotate180")
        {
            return transformer.Rotate180WithOptionsAndCancellation(source, options, cancellationToken);
        }
        if (operation == "rotate270")
        {
            return transformer.Rotate270WithOptionsAndCancellation(source, options, cancellationToken);
        }
        if (operation == "flipHorizontal")
        {
            return transformer.FlipHorizontalWithOptionsAndCancellation(source, options, cancellationToken);
        }
        if (operation == "flipVertical")
        {
            return transformer.FlipVerticalWithOptionsAndCancellation(source, options, cancellationToken);
        }
        if (operation == "transpose")
        {
            return transformer.TransposeWithOptionsAndCancellation(source, options, cancellationToken);
        }
        if (operation == "transverse")
        {
            return transformer.TransverseWithOptionsAndCancellation(source, options, cancellationToken);
        }
        if (operation == "crop")
        {
            return transformer.CropWithOptionsAndCancellation(source, 8, 0, 8, 16, options, cancellationToken);
        }
        if (operation == "grayscale")
        {
            return transformer.ConvertToGrayscaleWithOptionsAndCancellation(source, options, cancellationToken);
        }

        throw new System.ArgumentOutOfRangeException(nameof(operation));
    }

    public static byte[] RunSpan(string operation, byte[] source)
    {
        global::jpeg.IJpegTransformer transformer = new global::jpeg.JpegTransformer();
        global::jpeg.JpegTransformOptions options = new global::jpeg.JpegTransformOptions();
        System.ReadOnlySpan<byte> span = new System.ReadOnlySpan<byte>(source);
        if (operation == "rotate90")
        {
            return transformer.Rotate90SpanWithOptionsAndCancellation(span, options, default(System.Threading.CancellationToken));
        }
        if (operation == "rotate180")
        {
            return transformer.Rotate180SpanWithOptionsAndCancellation(span, options, default(System.Threading.CancellationToken));
        }
        if (operation == "rotate270")
        {
            return transformer.Rotate270SpanWithOptionsAndCancellation(span, options, default(System.Threading.CancellationToken));
        }
        if (operation == "flipHorizontal")
        {
            return transformer.FlipHorizontalSpanWithOptionsAndCancellation(span, options, default(System.Threading.CancellationToken));
        }
        if (operation == "flipVertical")
        {
            return transformer.FlipVerticalSpanWithOptionsAndCancellation(span, options, default(System.Threading.CancellationToken));
        }
        if (operation == "transpose")
        {
            return transformer.TransposeSpanWithOptionsAndCancellation(span, options, default(System.Threading.CancellationToken));
        }
        if (operation == "transverse")
        {
            return transformer.TransverseSpanWithOptionsAndCancellation(span, options, default(System.Threading.CancellationToken));
        }
        if (operation == "crop")
        {
            return transformer.CropSpanWithOptionsAndCancellation(span, 8, 0, 8, 16, options, default(System.Threading.CancellationToken));
        }
        if (operation == "grayscale")
        {
            return transformer.ConvertToGrayscaleSpanWithOptionsAndCancellation(span, options, default(System.Threading.CancellationToken));
        }

        throw new System.ArgumentOutOfRangeException(nameof(operation));
    }

    public static byte[] RunStream(string operation, byte[] source)
    {
        global::jpeg.IJpegTransformer transformer = new global::jpeg.JpegTransformer();
        global::jpeg.JpegTransformOptions options = new global::jpeg.JpegTransformOptions();
        System.IO.MemoryStream input = new System.IO.MemoryStream(source);
        System.IO.MemoryStream output = new System.IO.MemoryStream();
        if (operation == "rotate90")
        {
            transformer.Rotate90StreamWithOptionsAndCancellation(input, output, options, default(System.Threading.CancellationToken));
        }
        else if (operation == "rotate180")
        {
            transformer.Rotate180StreamWithOptionsAndCancellation(input, output, options, default(System.Threading.CancellationToken));
        }
        else if (operation == "rotate270")
        {
            transformer.Rotate270StreamWithOptionsAndCancellation(input, output, options, default(System.Threading.CancellationToken));
        }
        else if (operation == "flipHorizontal")
        {
            transformer.FlipHorizontalStreamWithOptionsAndCancellation(input, output, options, default(System.Threading.CancellationToken));
        }
        else if (operation == "flipVertical")
        {
            transformer.FlipVerticalStreamWithOptionsAndCancellation(input, output, options, default(System.Threading.CancellationToken));
        }
        else if (operation == "transpose")
        {
            transformer.TransposeStreamWithOptionsAndCancellation(input, output, options, default(System.Threading.CancellationToken));
        }
        else if (operation == "transverse")
        {
            transformer.TransverseStreamWithOptionsAndCancellation(input, output, options, default(System.Threading.CancellationToken));
        }
        else if (operation == "crop")
        {
            transformer.CropStreamWithOptionsAndCancellation(input, output, 8, 0, 8, 16, options, default(System.Threading.CancellationToken));
        }
        else if (operation == "grayscale")
        {
            transformer.ConvertToGrayscaleStreamWithOptionsAndCancellation(input, output, options, default(System.Threading.CancellationToken));
        }
        else
        {
            throw new System.ArgumentOutOfRangeException(nameof(operation));
        }

        Assert.That(input.CanRead, Is.True);
        Assert.That(output.CanWrite, Is.True);
        return output.ToArray();
    }

    public static byte[] RunFile(string operation, byte[] source)
    {
        global::jpeg.IJpegTransformer transformer = new global::jpeg.JpegTransformer();
        global::jpeg.JpegTransformOptions options = new global::jpeg.JpegTransformOptions();
        string inputPath = JpegFilePathApiTests.CreateTempJpegPath();
        string outputPath = JpegFilePathApiTests.CreateTempJpegPath();
        try
        {
            System.IO.File.WriteAllBytes(inputPath, source);
            if (operation == "rotate90")
            {
                transformer.Rotate90FileWithOptionsAndCancellation(inputPath, outputPath, options, default(System.Threading.CancellationToken));
            }
            else if (operation == "rotate180")
            {
                transformer.Rotate180FileWithOptionsAndCancellation(inputPath, outputPath, options, default(System.Threading.CancellationToken));
            }
            else if (operation == "rotate270")
            {
                transformer.Rotate270FileWithOptionsAndCancellation(inputPath, outputPath, options, default(System.Threading.CancellationToken));
            }
            else if (operation == "flipHorizontal")
            {
                transformer.FlipHorizontalFileWithOptionsAndCancellation(inputPath, outputPath, options, default(System.Threading.CancellationToken));
            }
            else if (operation == "flipVertical")
            {
                transformer.FlipVerticalFileWithOptionsAndCancellation(inputPath, outputPath, options, default(System.Threading.CancellationToken));
            }
            else if (operation == "transpose")
            {
                transformer.TransposeFileWithOptionsAndCancellation(inputPath, outputPath, options, default(System.Threading.CancellationToken));
            }
            else if (operation == "transverse")
            {
                transformer.TransverseFileWithOptionsAndCancellation(inputPath, outputPath, options, default(System.Threading.CancellationToken));
            }
            else if (operation == "crop")
            {
                transformer.CropFileWithOptionsAndCancellation(inputPath, outputPath, 8, 0, 8, 16, options, default(System.Threading.CancellationToken));
            }
            else if (operation == "grayscale")
            {
                transformer.ConvertToGrayscaleFileWithOptionsAndCancellation(inputPath, outputPath, options, default(System.Threading.CancellationToken));
            }
            else
            {
                throw new System.ArgumentOutOfRangeException(nameof(operation));
            }

            return System.IO.File.ReadAllBytes(outputPath);
        }
        finally
        {
            if (System.IO.File.Exists(inputPath))
            {
                System.IO.File.Delete(inputPath);
            }
            if (System.IO.File.Exists(outputPath))
            {
                System.IO.File.Delete(outputPath);
            }
        }
    }

    public static void AssertTransformResult(
        string operation,
        byte[] data,
        global::jpeg.JpegCoefficientImage source)
    {
        global::jpeg.JpegCoefficientImage result = global::jpeg.JpegInspector.ReadCoefficients(data);
        if (operation == "grayscale")
        {
            Assert.That(result.ColorSpace, Is.EqualTo(global::jpeg.JpegColorSpace.Grayscale));
            Assert.That(result.Components.Count, Is.EqualTo(1));
            Assert.That(result.Components[0].Coefficients, Is.EqualTo(source.Components[0].Coefficients));
            return;
        }

        int[] expectedDc;
        if (operation == "rotate90")
        {
            expectedDc = new int[] { 3, 1, 4, 2 };
        }
        else if (operation == "rotate180")
        {
            expectedDc = new int[] { 4, 3, 2, 1 };
        }
        else if (operation == "rotate270")
        {
            expectedDc = new int[] { 2, 4, 1, 3 };
        }
        else if (operation == "flipHorizontal")
        {
            expectedDc = new int[] { 2, 1, 4, 3 };
        }
        else if (operation == "flipVertical")
        {
            expectedDc = new int[] { 3, 4, 1, 2 };
        }
        else if (operation == "transpose")
        {
            expectedDc = new int[] { 1, 3, 2, 4 };
        }
        else if (operation == "transverse")
        {
            expectedDc = new int[] { 4, 2, 3, 1 };
        }
        else if (operation == "crop")
        {
            expectedDc = new int[] { 2, 4 };
            Assert.That(result.Width, Is.EqualTo(8));
            Assert.That(result.Height, Is.EqualTo(16));
        }
        else
        {
            throw new System.ArgumentOutOfRangeException(nameof(operation));
        }

        int blockIndex = 0;
        while (blockIndex < expectedDc.Length)
        {
            Assert.That(
                JpegLosslessTransformerTests.GetNaturalCoefficient(result.Components[0], blockIndex, 0),
                Is.EqualTo(expectedDc[blockIndex]));
            blockIndex = blockIndex + 1;
        }
    }

    public static string GetInverseOperation(string operation)
    {
        if (operation == "rotate90")
        {
            return "rotate270";
        }
        if (operation == "rotate270")
        {
            return "rotate90";
        }

        return operation;
    }

    public static byte[] CreateCoefficientJpeg()
    {
        return global::jpeg.JpegInspector.WriteCoefficients(JpegLosslessTransformerTests.CreateFourBlockCoefficientImage());
    }

    public static byte[] CreateCoefficientJpegWithAcValues()
    {
        global::jpeg.JpegCoefficientImage image = JpegLosslessTransformerTests.CreateFourBlockCoefficientImage();
        image.HuffmanTables = new global::jpeg.JpegImageWriter().CreateComprehensiveDctHuffmanTables(1);
        int blockIndex = 0;
        while (blockIndex < 4)
        {
            JpegLosslessTransformerTests.SetNaturalCoefficient(image.Components[0], blockIndex, 1, blockIndex + 5);
            JpegLosslessTransformerTests.SetNaturalCoefficient(image.Components[0], blockIndex, 8, blockIndex + 10);
            JpegLosslessTransformerTests.SetNaturalCoefficient(image.Components[0], blockIndex, 9, blockIndex + 15);
            blockIndex = blockIndex + 1;
        }

        return global::jpeg.JpegInspector.WriteCoefficients(image);
    }

    public static byte[] CreateColorJpeg(bool progressive)
    {
        global::jpeg.JpegImage image = GoldenEncodeTests.CreateRgbImage(16, 16, 147, 92, 31);
        global::jpeg.JpegWriteOptions options = new global::jpeg.JpegWriteOptions();
        options.Subsampling = global::jpeg.JpegSubsampling.Ratio420;
        if (progressive)
        {
            options.Process = global::jpeg.JpegProcess.ProgressiveDct;
        }

        return global::jpeg.Jpeg.Write(image, options);
    }

    public static byte[] CreateMetadataJpeg()
    {
        global::jpeg.JpegCoefficientImage image = JpegLosslessTransformerTests.CreateFourBlockCoefficientImage();
        image.Metadata.RawMarkers.Add(RawMarkerEditingTests.CreateRawMarker(
            global::jpeg.JpegMarker.Comment,
            global::jpeg.JpegConstants.MarkerComment,
            System.Text.Encoding.Latin1.GetBytes("note")));
        image.Metadata.RawMarkers.Add(RawMarkerEditingTests.CreateRawMarker(
            global::jpeg.JpegMarker.Unknown,
            0xFFF0,
            new byte[] { 7, 7 }));
        image.Metadata.RawMarkers.Add(RawMarkerEditingTests.CreateRawMarker(
            global::jpeg.JpegMarker.App14,
            global::jpeg.JpegConstants.MarkerApp14,
            AdobeMetadataParserTests.CreateAdobePayload(100, 1, 2, global::jpeg.JpegConstants.AdobeTransformUnknown)));
        image.Metadata.RawMarkers.Add(RawMarkerEditingTests.CreateRawMarker(
            global::jpeg.JpegMarker.App0,
            global::jpeg.JpegConstants.MarkerApp0,
            JfifMetadataParserTests.CreateJfifPayload(1, 2, 1, 72, 96, 0, 0)));
        image.Metadata.RawMarkers.Add(RawMarkerEditingTests.CreateRawMarker(
            global::jpeg.JpegMarker.App1,
            global::jpeg.JpegConstants.MarkerApp1,
            ExifMetadataParserTests.CreateExifPayload(new byte[] { 0x4D, 0x4D, 0, 0x2A, 0, 0, 0, 8 })));
        image.Metadata.RawMarkersAreExplicit = true;
        return global::jpeg.JpegInspector.WriteCoefficients(image);
    }
}
