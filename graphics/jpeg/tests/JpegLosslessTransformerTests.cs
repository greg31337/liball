namespace jpeg.Tests;

internal class JpegLosslessTransformerTests
{
    [Test]
    public void Rotate90_RotatesCoefficientBlocksAndFrequencyAxes()
    {
        global::jpeg.JpegCoefficientImage source = CreateTwoBlockCoefficientImage();
        SetNaturalCoefficient(source.Components[0], 0, 0, 11);
        SetNaturalCoefficient(source.Components[0], 0, 1, 5);
        SetNaturalCoefficient(source.Components[0], 1, 0, 22);
        global::jpeg.JpegLosslessTransformer transformer = new global::jpeg.JpegLosslessTransformer();

        global::jpeg.JpegCoefficientImage rotated = transformer.Rotate90Coefficients(source);

        Assert.That(rotated.Width, Is.EqualTo(8));
        Assert.That(rotated.Height, Is.EqualTo(16));
        Assert.That(rotated.Frame.Width, Is.EqualTo(8));
        Assert.That(rotated.Frame.Height, Is.EqualTo(16));
        Assert.That(rotated.Components[0].BlocksPerRow, Is.EqualTo(1));
        Assert.That(rotated.Components[0].BlockRows, Is.EqualTo(2));
        Assert.That(GetNaturalCoefficient(rotated.Components[0], 0, 0), Is.EqualTo(11));
        Assert.That(GetNaturalCoefficient(rotated.Components[0], 0, 8), Is.EqualTo(5));
        Assert.That(GetNaturalCoefficient(rotated.Components[0], 1, 0), Is.EqualTo(22));
    }

    [Test]
    public void Rotate90_ResolvesStructuredModelSummariesAndPreservesCodingData()
    {
        global::jpeg.JpegCoefficientImage source = JpegCoefficientWriterTests.CreateStructuredCoefficientImage();
        source.EntropyCoding = global::jpeg.JpegEntropyCoding.Huffman;
        source.RestartMarkerCount = 3;
        source.Markers.Add(new global::jpeg.JpegMarkerSegment());
        global::jpeg.JpegLosslessTransformer transformer = new global::jpeg.JpegLosslessTransformer();

        global::jpeg.JpegCoefficientImage rotated = transformer.Rotate90Coefficients(source);

        Assert.That(rotated.Width, Is.EqualTo(8));
        Assert.That(rotated.Height, Is.EqualTo(8));
        Assert.That(rotated.Precision, Is.EqualTo(8));
        Assert.That(rotated.Process, Is.EqualTo(global::jpeg.JpegProcess.BaselineDct));
        Assert.That(rotated.EntropyCoding, Is.EqualTo(global::jpeg.JpegEntropyCoding.Huffman));
        Assert.That(rotated.RestartMarkerCount, Is.EqualTo(3));
        Assert.That(rotated.Markers, Is.SameAs(source.Markers));
    }

    [Test]
    public void Rotate180_RotatesBlocksAndNegatesOddFrequencySums()
    {
        global::jpeg.JpegCoefficientImage source = CreateTwoBlockCoefficientImage();
        SetNaturalCoefficient(source.Components[0], 0, 0, 11);
        SetNaturalCoefficient(source.Components[0], 0, 1, 5);
        SetNaturalCoefficient(source.Components[0], 0, 8, 6);
        SetNaturalCoefficient(source.Components[0], 0, 9, 7);
        SetNaturalCoefficient(source.Components[0], 1, 0, 22);
        global::jpeg.JpegLosslessTransformer transformer = new global::jpeg.JpegLosslessTransformer();

        global::jpeg.JpegCoefficientImage rotated = transformer.Rotate180Coefficients(source);

        Assert.That(rotated.Width, Is.EqualTo(16));
        Assert.That(rotated.Height, Is.EqualTo(8));
        Assert.That(rotated.Components[0].BlocksPerRow, Is.EqualTo(2));
        Assert.That(rotated.Components[0].BlockRows, Is.EqualTo(1));
        Assert.That(GetNaturalCoefficient(rotated.Components[0], 0, 0), Is.EqualTo(22));
        Assert.That(GetNaturalCoefficient(rotated.Components[0], 1, 0), Is.EqualTo(11));
        Assert.That(GetNaturalCoefficient(rotated.Components[0], 1, 1), Is.EqualTo(-5));
        Assert.That(GetNaturalCoefficient(rotated.Components[0], 1, 8), Is.EqualTo(-6));
        Assert.That(GetNaturalCoefficient(rotated.Components[0], 1, 9), Is.EqualTo(7));
    }

    [Test]
    public void Rotate270_RotatesCoefficientBlocksAndFrequencyAxes()
    {
        global::jpeg.JpegCoefficientImage source = CreateTwoBlockCoefficientImage();
        SetNaturalCoefficient(source.Components[0], 0, 0, 11);
        SetNaturalCoefficient(source.Components[0], 0, 1, 5);
        SetNaturalCoefficient(source.Components[0], 1, 0, 22);
        global::jpeg.JpegLosslessTransformer transformer = new global::jpeg.JpegLosslessTransformer();

        global::jpeg.JpegCoefficientImage rotated = transformer.Rotate270Coefficients(source);

        Assert.That(rotated.Width, Is.EqualTo(8));
        Assert.That(rotated.Height, Is.EqualTo(16));
        Assert.That(rotated.Components[0].BlocksPerRow, Is.EqualTo(1));
        Assert.That(rotated.Components[0].BlockRows, Is.EqualTo(2));
        Assert.That(GetNaturalCoefficient(rotated.Components[0], 0, 0), Is.EqualTo(22));
        Assert.That(GetNaturalCoefficient(rotated.Components[0], 1, 0), Is.EqualTo(11));
        Assert.That(GetNaturalCoefficient(rotated.Components[0], 1, 8), Is.EqualTo(-5));
    }

    [Test]
    public void Rotate90_TransposesQuantizationTables()
    {
        global::jpeg.JpegCoefficientImage source = CreateTwoBlockCoefficientImage();
        source.QuantizationTables[0].Values[1] = 2;
        source.QuantizationTables[0].Values[8] = 3;
        source.QuantizationTables[0].ZigZagValues[1] = 2;
        source.QuantizationTables[0].ZigZagValues[2] = 3;
        global::jpeg.JpegLosslessTransformer transformer = new global::jpeg.JpegLosslessTransformer();

        global::jpeg.JpegCoefficientImage rotated = transformer.Rotate90Coefficients(source);

        Assert.That(rotated.QuantizationTables[0].Values[1], Is.EqualTo(3));
        Assert.That(rotated.QuantizationTables[0].Values[8], Is.EqualTo(2));
        Assert.That(rotated.QuantizationTables[0].ZigZagValues[1], Is.EqualTo(3));
        Assert.That(rotated.QuantizationTables[0].ZigZagValues[2], Is.EqualTo(2));
    }

    [Test]
    public void JpegRotateApis_WriteReadableRotatedBytes()
    {
        byte[] source = CreateTwoBlockCoefficientJpeg(2, 3);

        byte[] rotate90 = global::jpeg.Jpeg.Rotate90(source);
        byte[] rotate180 = global::jpeg.Jpeg.Rotate180(source);
        byte[] rotate270 = global::jpeg.Jpeg.Rotate270(source);

        global::jpeg.JpegInfo info90 = global::jpeg.Jpeg.Identify(rotate90);
        global::jpeg.JpegInfo info180 = global::jpeg.Jpeg.Identify(rotate180);
        global::jpeg.JpegInfo info270 = global::jpeg.Jpeg.Identify(rotate270);
        global::jpeg.JpegCoefficientImage coefficients90 = global::jpeg.JpegInspector.ReadCoefficients(rotate90);
        global::jpeg.JpegCoefficientImage coefficients180 = global::jpeg.JpegInspector.ReadCoefficients(rotate180);
        global::jpeg.JpegCoefficientImage coefficients270 = global::jpeg.JpegInspector.ReadCoefficients(rotate270);

        Assert.That(info90.Width, Is.EqualTo(8));
        Assert.That(info90.Height, Is.EqualTo(16));
        Assert.That(info180.Width, Is.EqualTo(16));
        Assert.That(info180.Height, Is.EqualTo(8));
        Assert.That(info270.Width, Is.EqualTo(8));
        Assert.That(info270.Height, Is.EqualTo(16));
        Assert.That(GetNaturalCoefficient(coefficients90.Components[0], 0, 0), Is.EqualTo(2));
        Assert.That(GetNaturalCoefficient(coefficients90.Components[0], 1, 0), Is.EqualTo(3));
        Assert.That(GetNaturalCoefficient(coefficients180.Components[0], 0, 0), Is.EqualTo(3));
        Assert.That(GetNaturalCoefficient(coefficients180.Components[0], 1, 0), Is.EqualTo(2));
        Assert.That(GetNaturalCoefficient(coefficients270.Components[0], 0, 0), Is.EqualTo(3));
        Assert.That(GetNaturalCoefficient(coefficients270.Components[0], 1, 0), Is.EqualTo(2));
    }

    [Test]
    public void Rotate_RejectsUnalignedAndInvalidInputs()
    {
        byte[] sourceBytes = JpegReadApiTests.CreateReadableGrayscaleJpeg(2);
        global::jpeg.JpegCoefficientImage source = global::jpeg.JpegInspector.ReadCoefficients(sourceBytes);
        global::jpeg.JpegLosslessTransformer transformer = new global::jpeg.JpegLosslessTransformer();
        source.Width = 9;
        source.Frame.Width = 9;

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                global::jpeg.Jpeg.Rotate90(null!);
            });
        Assert.Throws<System.ArgumentOutOfRangeException>(
            delegate
            {
                transformer.Rotate(source, 45);
            });
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                transformer.Rotate90Coefficients(source);
            });
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                transformer.NegateCoefficient(int.MinValue);
            });
    }

    [Test]
    public void FlipHorizontal_FlipsBlocksAndHorizontalFrequencies()
    {
        global::jpeg.JpegCoefficientImage source = CreateFourBlockCoefficientImage();
        SetNaturalCoefficient(source.Components[0], 0, 1, 5);
        SetNaturalCoefficient(source.Components[0], 0, 8, 6);
        global::jpeg.JpegLosslessTransformer transformer = new global::jpeg.JpegLosslessTransformer();

        global::jpeg.JpegCoefficientImage flipped = transformer.FlipHorizontalCoefficients(source);

        Assert.That(flipped.Width, Is.EqualTo(16));
        Assert.That(flipped.Height, Is.EqualTo(16));
        Assert.That(flipped.Components[0].BlocksPerRow, Is.EqualTo(2));
        Assert.That(flipped.Components[0].BlockRows, Is.EqualTo(2));
        Assert.That(GetNaturalCoefficient(flipped.Components[0], 0, 0), Is.EqualTo(2));
        Assert.That(GetNaturalCoefficient(flipped.Components[0], 1, 0), Is.EqualTo(1));
        Assert.That(GetNaturalCoefficient(flipped.Components[0], 1, 1), Is.EqualTo(-5));
        Assert.That(GetNaturalCoefficient(flipped.Components[0], 1, 8), Is.EqualTo(6));
        Assert.That(GetNaturalCoefficient(flipped.Components[0], 2, 0), Is.EqualTo(4));
        Assert.That(GetNaturalCoefficient(flipped.Components[0], 3, 0), Is.EqualTo(3));
    }

    [Test]
    public void FlipVertical_FlipsBlocksAndVerticalFrequencies()
    {
        global::jpeg.JpegCoefficientImage source = CreateFourBlockCoefficientImage();
        SetNaturalCoefficient(source.Components[0], 0, 1, 5);
        SetNaturalCoefficient(source.Components[0], 0, 8, 6);
        global::jpeg.JpegLosslessTransformer transformer = new global::jpeg.JpegLosslessTransformer();

        global::jpeg.JpegCoefficientImage flipped = transformer.FlipVerticalCoefficients(source);

        Assert.That(flipped.Width, Is.EqualTo(16));
        Assert.That(flipped.Height, Is.EqualTo(16));
        Assert.That(flipped.Components[0].BlocksPerRow, Is.EqualTo(2));
        Assert.That(flipped.Components[0].BlockRows, Is.EqualTo(2));
        Assert.That(GetNaturalCoefficient(flipped.Components[0], 0, 0), Is.EqualTo(3));
        Assert.That(GetNaturalCoefficient(flipped.Components[0], 1, 0), Is.EqualTo(4));
        Assert.That(GetNaturalCoefficient(flipped.Components[0], 2, 0), Is.EqualTo(1));
        Assert.That(GetNaturalCoefficient(flipped.Components[0], 2, 1), Is.EqualTo(5));
        Assert.That(GetNaturalCoefficient(flipped.Components[0], 2, 8), Is.EqualTo(-6));
        Assert.That(GetNaturalCoefficient(flipped.Components[0], 3, 0), Is.EqualTo(2));
    }

    [Test]
    public void JpegFlipApis_WriteReadableFlippedBytes()
    {
        byte[] source = global::jpeg.JpegInspector.WriteCoefficients(CreateFourBlockCoefficientImage());

        byte[] horizontal = global::jpeg.Jpeg.FlipHorizontal(source);
        byte[] vertical = global::jpeg.Jpeg.FlipVertical(source);

        global::jpeg.JpegInfo horizontalInfo = global::jpeg.Jpeg.Identify(horizontal);
        global::jpeg.JpegInfo verticalInfo = global::jpeg.Jpeg.Identify(vertical);
        global::jpeg.JpegCoefficientImage horizontalCoefficients = global::jpeg.JpegInspector.ReadCoefficients(horizontal);
        global::jpeg.JpegCoefficientImage verticalCoefficients = global::jpeg.JpegInspector.ReadCoefficients(vertical);

        Assert.That(horizontalInfo.Width, Is.EqualTo(16));
        Assert.That(horizontalInfo.Height, Is.EqualTo(16));
        Assert.That(verticalInfo.Width, Is.EqualTo(16));
        Assert.That(verticalInfo.Height, Is.EqualTo(16));
        Assert.That(GetNaturalCoefficient(horizontalCoefficients.Components[0], 0, 0), Is.EqualTo(2));
        Assert.That(GetNaturalCoefficient(horizontalCoefficients.Components[0], 1, 0), Is.EqualTo(1));
        Assert.That(GetNaturalCoefficient(horizontalCoefficients.Components[0], 2, 0), Is.EqualTo(4));
        Assert.That(GetNaturalCoefficient(horizontalCoefficients.Components[0], 3, 0), Is.EqualTo(3));
        Assert.That(GetNaturalCoefficient(verticalCoefficients.Components[0], 0, 0), Is.EqualTo(3));
        Assert.That(GetNaturalCoefficient(verticalCoefficients.Components[0], 1, 0), Is.EqualTo(4));
        Assert.That(GetNaturalCoefficient(verticalCoefficients.Components[0], 2, 0), Is.EqualTo(1));
        Assert.That(GetNaturalCoefficient(verticalCoefficients.Components[0], 3, 0), Is.EqualTo(2));
    }

    [Test]
    public void Flip_RejectsUnalignedAndNullInputs()
    {
        byte[] sourceBytes = JpegReadApiTests.CreateReadableGrayscaleJpeg(2);
        global::jpeg.JpegCoefficientImage source = global::jpeg.JpegInspector.ReadCoefficients(sourceBytes);
        global::jpeg.JpegLosslessTransformer transformer = new global::jpeg.JpegLosslessTransformer();
        source.Height = 9;
        source.Frame.Height = 9;

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                global::jpeg.Jpeg.FlipHorizontal(null!);
            });
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                transformer.FlipVerticalCoefficients(source);
            });
    }

    [Test]
    public void Transpose_RotatesBlocksAndFrequencyAxes()
    {
        global::jpeg.JpegCoefficientImage source = CreateTwoBlockCoefficientImage();
        SetNaturalCoefficient(source.Components[0], 0, 0, 11);
        SetNaturalCoefficient(source.Components[0], 0, 1, 5);
        SetNaturalCoefficient(source.Components[0], 1, 0, 22);
        global::jpeg.JpegLosslessTransformer transformer = new global::jpeg.JpegLosslessTransformer();

        global::jpeg.JpegCoefficientImage transposed = transformer.TransposeCoefficients(source);

        Assert.That(transposed.Width, Is.EqualTo(8));
        Assert.That(transposed.Height, Is.EqualTo(16));
        Assert.That(transposed.Frame.Width, Is.EqualTo(8));
        Assert.That(transposed.Frame.Height, Is.EqualTo(16));
        Assert.That(transposed.Components[0].BlocksPerRow, Is.EqualTo(1));
        Assert.That(transposed.Components[0].BlockRows, Is.EqualTo(2));
        Assert.That(GetNaturalCoefficient(transposed.Components[0], 0, 0), Is.EqualTo(11));
        Assert.That(GetNaturalCoefficient(transposed.Components[0], 0, 8), Is.EqualTo(5));
        Assert.That(GetNaturalCoefficient(transposed.Components[0], 1, 0), Is.EqualTo(22));
    }

    [Test]
    public void Transverse_RotatesBlocksAndNegatesOddFrequencySums()
    {
        global::jpeg.JpegCoefficientImage source = CreateTwoBlockCoefficientImage();
        SetNaturalCoefficient(source.Components[0], 0, 0, 11);
        SetNaturalCoefficient(source.Components[0], 0, 1, 5);
        SetNaturalCoefficient(source.Components[0], 1, 0, 22);
        global::jpeg.JpegLosslessTransformer transformer = new global::jpeg.JpegLosslessTransformer();

        global::jpeg.JpegCoefficientImage transversed = transformer.TransverseCoefficients(source);

        Assert.That(transversed.Width, Is.EqualTo(8));
        Assert.That(transversed.Height, Is.EqualTo(16));
        Assert.That(transversed.Components[0].BlocksPerRow, Is.EqualTo(1));
        Assert.That(transversed.Components[0].BlockRows, Is.EqualTo(2));
        Assert.That(GetNaturalCoefficient(transversed.Components[0], 0, 0), Is.EqualTo(22));
        Assert.That(GetNaturalCoefficient(transversed.Components[0], 1, 0), Is.EqualTo(11));
        Assert.That(GetNaturalCoefficient(transversed.Components[0], 1, 8), Is.EqualTo(-5));
    }

    [Test]
    public void JpegTransposeApis_WriteReadableTransformedBytes()
    {
        byte[] source = CreateTwoBlockCoefficientJpeg(2, 3);

        byte[] transpose = global::jpeg.Jpeg.Transpose(source);
        byte[] transverse = global::jpeg.Jpeg.Transverse(source);

        global::jpeg.JpegInfo transposeInfo = global::jpeg.Jpeg.Identify(transpose);
        global::jpeg.JpegInfo transverseInfo = global::jpeg.Jpeg.Identify(transverse);
        global::jpeg.JpegCoefficientImage transposeCoefficients = global::jpeg.JpegInspector.ReadCoefficients(transpose);
        global::jpeg.JpegCoefficientImage transverseCoefficients = global::jpeg.JpegInspector.ReadCoefficients(transverse);

        Assert.That(transposeInfo.Width, Is.EqualTo(8));
        Assert.That(transposeInfo.Height, Is.EqualTo(16));
        Assert.That(transverseInfo.Width, Is.EqualTo(8));
        Assert.That(transverseInfo.Height, Is.EqualTo(16));
        Assert.That(GetNaturalCoefficient(transposeCoefficients.Components[0], 0, 0), Is.EqualTo(2));
        Assert.That(GetNaturalCoefficient(transposeCoefficients.Components[0], 1, 0), Is.EqualTo(3));
        Assert.That(GetNaturalCoefficient(transverseCoefficients.Components[0], 0, 0), Is.EqualTo(3));
        Assert.That(GetNaturalCoefficient(transverseCoefficients.Components[0], 1, 0), Is.EqualTo(2));
    }

    [Test]
    public void Transpose_RejectsUnalignedAndNullInputs()
    {
        byte[] sourceBytes = JpegReadApiTests.CreateReadableGrayscaleJpeg(2);
        global::jpeg.JpegCoefficientImage source = global::jpeg.JpegInspector.ReadCoefficients(sourceBytes);
        global::jpeg.JpegLosslessTransformer transformer = new global::jpeg.JpegLosslessTransformer();
        source.Width = 9;
        source.Frame.Width = 9;

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                global::jpeg.Jpeg.Transpose(null!);
            });
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                transformer.TransverseCoefficients(source);
            });
    }

    [Test]
    public void Crop_CopiesMcuAlignedCoefficientBlocks()
    {
        global::jpeg.JpegCoefficientImage source = CreateFourBlockCoefficientImage();
        SetNaturalCoefficient(source.Components[0], 1, 1, 5);
        global::jpeg.JpegLosslessTransformer transformer = new global::jpeg.JpegLosslessTransformer();

        global::jpeg.JpegCoefficientImage cropped = transformer.CropCoefficients(source, 8, 0, 8, 16);

        Assert.That(cropped.Width, Is.EqualTo(8));
        Assert.That(cropped.Height, Is.EqualTo(16));
        Assert.That(cropped.Frame.Width, Is.EqualTo(8));
        Assert.That(cropped.Frame.Height, Is.EqualTo(16));
        Assert.That(cropped.Components[0].BlocksPerRow, Is.EqualTo(1));
        Assert.That(cropped.Components[0].BlockRows, Is.EqualTo(2));
        Assert.That(GetNaturalCoefficient(cropped.Components[0], 0, 0), Is.EqualTo(2));
        Assert.That(GetNaturalCoefficient(cropped.Components[0], 0, 1), Is.EqualTo(5));
        Assert.That(GetNaturalCoefficient(cropped.Components[0], 1, 0), Is.EqualTo(4));
    }

    [Test]
    public void JpegCropApi_WritesReadableCroppedBytes()
    {
        byte[] source = global::jpeg.JpegInspector.WriteCoefficients(CreateFourBlockCoefficientImage());

        byte[] cropped = global::jpeg.Jpeg.Crop(source, 0, 8, 16, 8);

        global::jpeg.JpegInfo info = global::jpeg.Jpeg.Identify(cropped);
        global::jpeg.JpegCoefficientImage coefficients = global::jpeg.JpegInspector.ReadCoefficients(cropped);
        Assert.That(info.Width, Is.EqualTo(16));
        Assert.That(info.Height, Is.EqualTo(8));
        Assert.That(coefficients.Components[0].BlocksPerRow, Is.EqualTo(2));
        Assert.That(coefficients.Components[0].BlockRows, Is.EqualTo(1));
        Assert.That(GetNaturalCoefficient(coefficients.Components[0], 0, 0), Is.EqualTo(3));
        Assert.That(GetNaturalCoefficient(coefficients.Components[0], 1, 0), Is.EqualTo(4));
    }

    [Test]
    public void Crop_RejectsNonPerfectTransformRectangles()
    {
        global::jpeg.JpegCoefficientImage source = CreateFourBlockCoefficientImage();
        global::jpeg.JpegLosslessTransformer transformer = new global::jpeg.JpegLosslessTransformer();

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                global::jpeg.Jpeg.Crop(null!, 0, 0, 8, 8);
            });
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                transformer.CropCoefficients(source, 4, 0, 8, 8);
            });
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                transformer.CropCoefficients(source, 0, 0, 12, 8);
            });
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                transformer.CropCoefficients(source, 8, 8, 16, 8);
            });
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                transformer.CropCoefficients(source, 0, 0, 0, 8);
            });
    }

    [Test]
    public void JpegTransformApis_PreserveMetadata()
    {
        byte[] source = CreateMetadataCoefficientJpeg();

        byte[] rotate90 = global::jpeg.Jpeg.Rotate90(source);
        byte[] rotate180 = global::jpeg.Jpeg.Rotate180(source);
        byte[] rotate270 = global::jpeg.Jpeg.Rotate270(source);
        byte[] flipHorizontal = global::jpeg.Jpeg.FlipHorizontal(source);
        byte[] flipVertical = global::jpeg.Jpeg.FlipVertical(source);
        byte[] transpose = global::jpeg.Jpeg.Transpose(source);
        byte[] transverse = global::jpeg.Jpeg.Transverse(source);
        byte[] crop = global::jpeg.Jpeg.Crop(source, 0, 0, 8, 8);

        AssertPreservedMetadata(rotate90, 16, 16);
        AssertPreservedMetadata(rotate180, 16, 16);
        AssertPreservedMetadata(rotate270, 16, 16);
        AssertPreservedMetadata(flipHorizontal, 16, 16);
        AssertPreservedMetadata(flipVertical, 16, 16);
        AssertPreservedMetadata(transpose, 16, 16);
        AssertPreservedMetadata(transverse, 16, 16);
        AssertPreservedMetadata(crop, 8, 8);
    }

    [Test]
    public void JpegTransformApis_RejectNonPerfectTransforms()
    {
        byte[] unaligned = global::jpeg.Jpeg.Write(JpegWriteApiTests.CreateGrayscaleImage(9, 8, 128));
        byte[] source = global::jpeg.JpegInspector.WriteCoefficients(CreateFourBlockCoefficientImage());

        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                global::jpeg.Jpeg.Rotate90(unaligned);
            });
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                global::jpeg.Jpeg.FlipVertical(unaligned);
            });
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                global::jpeg.Jpeg.Crop(source, 4, 0, 8, 8);
            });
    }

    [Test]
    public void TransformOptions_ControlPerfectTransformsAndMcuCropResolution()
    {
        global::jpeg.IJpegTransformer transformer = new global::jpeg.JpegTransformer();
        byte[] unaligned = global::jpeg.Jpeg.Write(JpegWriteApiTests.CreateGrayscaleImage(12, 8, 128));
        global::jpeg.JpegTransformOptions imperfect = new global::jpeg.JpegTransformOptions();
        imperfect.RequirePerfectTransform = false;

        byte[] rotated = transformer.Rotate90WithOptionsAndCancellation(
            unaligned,
            imperfect,
            default(System.Threading.CancellationToken));

        Assert.That(global::jpeg.Jpeg.Identify(rotated).Width, Is.EqualTo(8));
        Assert.That(global::jpeg.Jpeg.Identify(rotated).Height, Is.EqualTo(12));

        byte[] cropSource = global::jpeg.Jpeg.Write(JpegWriteApiTests.CreateGrayscaleImage(24, 16, 128));
        global::jpeg.JpegTransformOptions trim = new global::jpeg.JpegTransformOptions();
        trim.McuCropMode = global::jpeg.JpegMcuCropMode.Trim;
        global::jpeg.JpegTransformOptions expand = new global::jpeg.JpegTransformOptions();
        expand.McuCropMode = global::jpeg.JpegMcuCropMode.Expand;
        byte[] trimmed = transformer.CropWithOptionsAndCancellation(cropSource, 4, 0, 12, 16, trim, default(System.Threading.CancellationToken));
        byte[] expanded = transformer.CropWithOptionsAndCancellation(cropSource, 4, 0, 12, 16, expand, default(System.Threading.CancellationToken));

        Assert.That(global::jpeg.Jpeg.Identify(trimmed).Width, Is.EqualTo(8));
        Assert.That(global::jpeg.Jpeg.Identify(trimmed).Height, Is.EqualTo(16));
        Assert.That(global::jpeg.Jpeg.Identify(expanded).Width, Is.EqualTo(16));
        Assert.That(global::jpeg.Jpeg.Identify(expanded).Height, Is.EqualTo(16));
    }

    [Test]
    public void ConvertToGrayscale_RemovesChromaCoefficientsAndAppliesMetadataPolicy()
    {
        global::jpeg.JpegImage color = GoldenEncodeTests.CreateRgbImage(16, 16, 140, 100, 60);
        color.Metadata.Comments.Add("remove me");
        global::jpeg.JpegWriteOptions writeOptions = new global::jpeg.JpegWriteOptions();
        writeOptions.Subsampling = global::jpeg.JpegSubsampling.Ratio420;
        byte[] source = global::jpeg.Jpeg.Write(color, writeOptions);
        global::jpeg.JpegCoefficientImage sourceCoefficients = global::jpeg.JpegInspector.ReadCoefficients(source);
        global::jpeg.JpegTransformOptions options = new global::jpeg.JpegTransformOptions();
        options.MetadataPolicy = global::jpeg.JpegMetadataPolicy.StripAll;
        global::jpeg.IJpegTransformer transformer = new global::jpeg.JpegTransformer();

        byte[] grayscale = transformer.ConvertToGrayscaleWithOptionsAndCancellation(
            source,
            options,
            default(System.Threading.CancellationToken));

        global::jpeg.JpegInfo info = global::jpeg.Jpeg.Identify(grayscale);
        global::jpeg.JpegCoefficientImage result = global::jpeg.JpegInspector.ReadCoefficients(grayscale);
        Assert.That(info.ComponentCount, Is.EqualTo(1));
        Assert.That(info.ColorSpace, Is.EqualTo(global::jpeg.JpegColorSpace.Grayscale));
        Assert.That(info.CommentCount, Is.Zero);
        Assert.That(result.Components[0].HorizontalSamplingFactor, Is.EqualTo(1));
        Assert.That(result.Components[0].VerticalSamplingFactor, Is.EqualTo(1));
        Assert.That(result.Components[0].Coefficients, Is.EqualTo(sourceCoefficients.Components[0].Coefficients));
    }

    [Test]
    public void TransformVariants_HandleSpanStreamFileAndCancellation()
    {
        byte[] source = global::jpeg.JpegInspector.WriteCoefficients(CreateFourBlockCoefficientImage());
        global::jpeg.IJpegTransformer transformer = new global::jpeg.JpegTransformer();
        global::jpeg.JpegTransformOptions options = new global::jpeg.JpegTransformOptions();
        byte[] spanResult = transformer.Rotate90SpanWithOptionsAndCancellation(
            new System.ReadOnlySpan<byte>(source),
            options,
            default(System.Threading.CancellationToken));
        System.IO.MemoryStream input = new System.IO.MemoryStream(source);
        System.IO.MemoryStream output = new System.IO.MemoryStream();
        transformer.FlipHorizontalStreamWithOptionsAndCancellation(input, output, options, default(System.Threading.CancellationToken));
        string inputPath = JpegFilePathApiTests.CreateTempJpegPath();
        string outputPath = JpegFilePathApiTests.CreateTempJpegPath();
        System.IO.File.WriteAllBytes(inputPath, source);
        transformer.TransposeFileWithOptionsAndCancellation(inputPath, outputPath, options, default(System.Threading.CancellationToken));

        Assert.That(global::jpeg.Jpeg.Identify(spanResult).Width, Is.EqualTo(16));
        Assert.That(global::jpeg.Jpeg.Identify(output.ToArray()).Width, Is.EqualTo(16));
        Assert.That(global::jpeg.Jpeg.Identify(System.IO.File.ReadAllBytes(outputPath)).Height, Is.EqualTo(16));

        System.Threading.CancellationTokenSource cancellation = new System.Threading.CancellationTokenSource();
        cancellation.Cancel();
        Assert.Throws<System.OperationCanceledException>(
            delegate
            {
                transformer.TransverseWithOptionsAndCancellation(source, options, cancellation.Token);
            });

        System.IO.File.Delete(inputPath);
        System.IO.File.Delete(outputPath);
    }

    public static global::jpeg.JpegCoefficientImage CreateTwoBlockCoefficientImage()
    {
        return global::jpeg.JpegInspector.ReadCoefficients(JpegCoefficientReaderTests.CreateTwoBlockRestartCoefficientJpeg());
    }

    public static byte[] CreateMetadataCoefficientJpeg()
    {
        global::jpeg.JpegImage image = JpegWriteApiTests.CreateGrayscaleImage(16, 16, 138);
        image.Metadata.Comments.Add("transform note");
        image.Metadata.HasIccProfile = true;
        image.Metadata.IccProfile = new byte[] { 9, 8, 7 };
        return global::jpeg.Jpeg.Write(image);
    }

    public static void AssertPreservedMetadata(byte[] data, int width, int height)
    {
        global::jpeg.JpegInfo info = global::jpeg.Jpeg.Identify(data);

        Assert.That(info.Width, Is.EqualTo(width));
        Assert.That(info.Height, Is.EqualTo(height));
        Assert.That(info.CommentCount, Is.EqualTo(1));
        Assert.That(info.Metadata.Comments, Is.EqualTo(new string[] { "transform note" }));
        Assert.That(info.Metadata.HasIccProfile, Is.True);
        Assert.That(info.Metadata.IccProfile, Is.EqualTo(new byte[] { 9, 8, 7 }));
    }

    public static byte[] CreateTwoBlockCoefficientJpeg(int firstDc, int secondDc)
    {
        global::jpeg.JpegCoefficientImage image = CreateTwoBlockCoefficientImage();
        SetNaturalCoefficient(image.Components[0], 0, 0, firstDc);
        SetNaturalCoefficient(image.Components[0], 1, 0, secondDc);
        return global::jpeg.JpegInspector.WriteCoefficients(image);
    }

    public static global::jpeg.JpegCoefficientImage CreateFourBlockCoefficientImage()
    {
        global::jpeg.JpegCoefficientImage image = new global::jpeg.JpegCoefficientImage();
        image.Width = 16;
        image.Height = 16;
        image.Precision = 8;
        image.ComponentCount = 1;
        image.Process = global::jpeg.JpegProcess.BaselineDct;
        image.ColorSpace = global::jpeg.JpegColorSpace.Grayscale;
        image.Subsampling = global::jpeg.JpegSubsampling.Grayscale;
        image.Frame = BaselineSof0FullImageDecoderTests.CreateFrame(16, 16);
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
        component.BlocksPerRow = 2;
        component.BlockRows = 2;
        component.Coefficients = new int[4 * global::jpeg.JpegConstants.BlockSampleCount];
        image.Components.Add(component);
        SetNaturalCoefficient(component, 0, 0, 1);
        SetNaturalCoefficient(component, 1, 0, 2);
        SetNaturalCoefficient(component, 2, 0, 3);
        SetNaturalCoefficient(component, 3, 0, 4);
        return image;
    }

    public static void SetNaturalCoefficient(global::jpeg.JpegCoefficientComponent component, int blockIndex, int naturalIndex, int value)
    {
        global::jpeg.JpegCoefficientOrderConverter converter = new global::jpeg.JpegCoefficientOrderConverter();
        int zigZagIndex = converter.ToZigZagIndex(naturalIndex);
        component.Coefficients[blockIndex * global::jpeg.JpegConstants.BlockSampleCount + zigZagIndex] = value;
    }

    public static int GetNaturalCoefficient(global::jpeg.JpegCoefficientComponent component, int blockIndex, int naturalIndex)
    {
        global::jpeg.JpegCoefficientOrderConverter converter = new global::jpeg.JpegCoefficientOrderConverter();
        int zigZagIndex = converter.ToZigZagIndex(naturalIndex);
        return component.Coefficients[blockIndex * global::jpeg.JpegConstants.BlockSampleCount + zigZagIndex];
    }
}
