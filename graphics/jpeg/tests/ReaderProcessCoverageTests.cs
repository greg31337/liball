namespace jpeg.Tests;

internal class ReaderProcessCoverageTests
{
    [Test]
    public void WholeImageReader_DecodesProgressiveHuffmanOutput()
    {
        byte[] data = CallbackStreamingApiTests.CreateProgressiveGrayscaleJpeg(143);

        global::jpeg.JpegImage image = new global::jpeg.JpegReader().Read(data);

        Assert.That(image.Precision, Is.EqualTo(8));
        Assert.That(image.PixelFormat, Is.EqualTo(global::jpeg.JpegPixelFormat.L8));
        Assert.That(image.BufferLayout, Is.EqualTo(global::jpeg.JpegBufferLayout.Interleaved));
        Assert.That(image.Pixels, Is.All.EqualTo(143));
    }

    [Test]
    public void WholeImageReader_ReturnsTwelveBitDctAndLosslessSamplesInPlanarBuffers()
    {
        global::jpeg.JpegImage source = CreateWideGrayscaleImage(12, 2058);
        global::jpeg.JpegWriteOptions dctOptions = new global::jpeg.JpegWriteOptions();
        dctOptions.Process = global::jpeg.JpegProcess.ExtendedSequentialDct;
        dctOptions.SamplePrecision = 12;
        global::jpeg.JpegWriteOptions losslessOptions = new global::jpeg.JpegWriteOptions();
        losslessOptions.Process = global::jpeg.JpegProcess.LosslessSequential;
        losslessOptions.SamplePrecision = 12;

        global::jpeg.JpegImage dct = new global::jpeg.JpegReader().Read(
            new global::jpeg.JpegWriter().WriteWithOptions(source, dctOptions));
        global::jpeg.JpegImage lossless = new global::jpeg.JpegReader().Read(
            new global::jpeg.JpegWriter().WriteWithOptions(source, losslessOptions));

        AssertWidePlanarImage(dct, 12, 2058);
        AssertWidePlanarImage(lossless, 12, 2058);
    }

    [Test]
    public void WholeImageReader_DecodesEightBitArithmeticLosslessToPackedPixels()
    {
        global::jpeg.JpegImage source = JpegWriteApiTests.CreateGrayscaleImage(3, 2, 130);
        global::jpeg.JpegWriteOptions options = new global::jpeg.JpegWriteOptions();
        options.Process = global::jpeg.JpegProcess.LosslessSequentialArithmetic;
        options.SamplePrecision = 8;
        options.EntropyCoding = global::jpeg.JpegEntropyCoding.Arithmetic;

        byte[] data = new global::jpeg.JpegWriter().WriteWithOptions(source, options);
        global::jpeg.JpegImage image = new global::jpeg.JpegReader().Read(data);

        Assert.That(image.Precision, Is.EqualTo(8));
        Assert.That(image.PixelFormat, Is.EqualTo(global::jpeg.JpegPixelFormat.L8));
        Assert.That(image.BufferLayout, Is.EqualTo(global::jpeg.JpegBufferLayout.Interleaved));
        Assert.That(image.Pixels, Is.All.EqualTo(130));
    }

    [Test]
    public void WholeImageReader_PreservesSixteenBitLosslessSamples()
    {
        global::jpeg.JpegImage source = CreateWideGrayscaleImage(16, 33000);
        global::jpeg.JpegWriteOptions options = new global::jpeg.JpegWriteOptions();
        options.Process = global::jpeg.JpegProcess.LosslessSequential;
        options.SamplePrecision = 16;

        byte[] data = new global::jpeg.JpegWriter().WriteWithOptions(source, options);
        global::jpeg.JpegImage image = new global::jpeg.JpegReader().Read(data);

        AssertWidePlanarImage(image, 16, 33000);
    }

    [Test]
    public void Reader_AutoOrientAppliesExifAndPreservesBufferDescription()
    {
        global::jpeg.JpegImage source = ReadGeometryTests.CreateBlockImage(16, 8);
        source.Metadata.HasExif = true;
        source.Metadata.ExifMetadata = new global::jpeg.JpegExifMetadataParser().Parse(
            ExifMetadataParserTests.CreateExifPayload(ExifTagReaderTests.CreateCommonExifTiff()));
        byte[] data = new global::jpeg.JpegWriter().Write(source);
        global::jpeg.JpegReadOptions options = new global::jpeg.JpegReadOptions();
        options.AutoOrient = true;

        global::jpeg.JpegImage unrotated = new global::jpeg.JpegReader().Read(data);
        global::jpeg.JpegImage rotated = new global::jpeg.JpegReader().ReadWithOptions(data, options);

        Assert.That(unrotated.Width, Is.EqualTo(16));
        Assert.That(unrotated.Height, Is.EqualTo(8));
        Assert.That(rotated.Width, Is.EqualTo(8));
        Assert.That(rotated.Height, Is.EqualTo(16));
        Assert.That(rotated.RowStride, Is.EqualTo(8));
        Assert.That(rotated.InterleavedBuffer.Pixels, Is.SameAs(rotated.Pixels));
        Assert.That(rotated.Pixels[0], Is.EqualTo(138));
        Assert.That(rotated.Pixels[8 * 8], Is.EqualTo(143));
        Assert.That(rotated.SourceWidth, Is.EqualTo(16));
        Assert.That(rotated.SourceHeight, Is.EqualTo(8));
        Assert.That(rotated.OutputRectangle.Width, Is.EqualTo(8));
        Assert.That(rotated.OutputRectangle.Height, Is.EqualTo(16));
        Assert.That(rotated.Metadata.ExifMetadata.Orientation, Is.EqualTo(6));
    }

    [Test]
    public void ProgressivePreviewReader_AppliesAutoOrientationToEveryPublishedResult()
    {
        global::jpeg.JpegImage source = ReadGeometryTests.CreateBlockImage(16, 8);
        source.Metadata.HasExif = true;
        source.Metadata.ExifMetadata = new global::jpeg.JpegExifMetadataParser().Parse(
            ExifMetadataParserTests.CreateExifPayload(ExifTagReaderTests.CreateCommonExifTiff()));
        global::jpeg.JpegWriteOptions writeOptions = new global::jpeg.JpegWriteOptions();
        writeOptions.Process = global::jpeg.JpegProcess.ProgressiveDct;
        byte[] data = new global::jpeg.JpegWriter().WriteWithOptions(source, writeOptions);
        global::jpeg.JpegReadOptions readOptions = new global::jpeg.JpegReadOptions();
        readOptions.AutoOrient = true;
        TrackingProgressivePreviews callback = new TrackingProgressivePreviews();

        global::jpeg.JpegImage image = new global::jpeg.JpegReader().ReadProgressivePreviewsWithOptionsAndCancellation(
            data,
            readOptions,
            callback,
            default(System.Threading.CancellationToken));

        Assert.That(callback.ScanIndexes, Is.Not.Empty);
        Assert.That(image.Width, Is.EqualTo(8));
        Assert.That(image.Height, Is.EqualTo(16));
        Assert.That(image.RowStride, Is.EqualTo(8));
        Assert.That(image.Metadata.ExifMetadata.Orientation, Is.EqualTo(6));
    }

    [Test]
    public void Reader_AppliesEveryIccBehaviorPolicy()
    {
        byte[] baseline = JpegReadApiTests.CreateReadableGrayscaleJpeg(2);
        byte[] unsupported = global::jpeg.Jpeg.SetIccProfile(baseline, IccProfileParserTests.CreateIccProfileWithOneTag());
        byte[] supported = global::jpeg.Jpeg.SetIccProfile(baseline, IccMatrixTrcConverterTests.CreateGrayMatrixTrcProfile());
        global::jpeg.JpegReadOptions preserveOptions = new global::jpeg.JpegReadOptions();
        preserveOptions.IccBehaviorPolicy = global::jpeg.JpegIccBehaviorPolicy.Preserve;
        global::jpeg.JpegReadOptions ignoreOptions = new global::jpeg.JpegReadOptions();
        ignoreOptions.IccBehaviorPolicy = global::jpeg.JpegIccBehaviorPolicy.Ignore;
        global::jpeg.JpegReadOptions convertOptions = new global::jpeg.JpegReadOptions();
        convertOptions.IccBehaviorPolicy = global::jpeg.JpegIccBehaviorPolicy.ConvertWhenSupported;
        global::jpeg.JpegReadOptions failOptions = new global::jpeg.JpegReadOptions();
        failOptions.IccBehaviorPolicy = global::jpeg.JpegIccBehaviorPolicy.FailIfUnsupported;
        global::jpeg.JpegReader reader = new global::jpeg.JpegReader();

        global::jpeg.JpegImage preserved = reader.ReadWithOptions(unsupported, preserveOptions);
        global::jpeg.JpegImage ignored = reader.ReadWithOptions(unsupported, ignoreOptions);
        global::jpeg.JpegImage convertedFallback = reader.ReadWithOptions(unsupported, convertOptions);
        global::jpeg.JpegImage converted = reader.ReadWithOptions(supported, failOptions);

        Assert.That(preserved.Metadata.HasIccProfile, Is.True);
        Assert.That(ignored.Metadata.HasIccProfile, Is.False);
        Assert.That(ignored.Metadata.IccProfile, Is.Empty);
        Assert.That(convertedFallback.Metadata.HasIccProfile, Is.True);
        Assert.That(converted.Metadata.Icc.Conversion.Status, Is.EqualTo(global::jpeg.JpegIccConversionStatus.Supported));
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate { reader.ReadWithOptions(unsupported, failOptions); });
    }

    internal static global::jpeg.JpegImage CreateWideGrayscaleImage(int precision, int value)
    {
        int[] samples = JpegEncoderProcessOptionsTests.CreateFilledSamples(8, 8, value);
        return new global::jpeg.JpegImageFactory().CreatePackedImageFromIntSamples(
            samples,
            8,
            8,
            1,
            8,
            precision,
            global::jpeg.JpegColorSpace.Grayscale);
    }

    internal static void AssertWidePlanarImage(global::jpeg.JpegImage image, int precision, int value)
    {
        Assert.That(image.Precision, Is.EqualTo(precision));
        Assert.That(image.PixelFormat, Is.EqualTo(global::jpeg.JpegPixelFormat.RawComponents));
        Assert.That(image.BufferLayout, Is.EqualTo(global::jpeg.JpegBufferLayout.Planar));
        Assert.That(image.BufferOwnership, Is.EqualTo(global::jpeg.JpegBufferOwnership.LibraryOwned));
        Assert.That(image.PlanarBuffer.Components[0].SampleStorage, Is.EqualTo(global::jpeg.JpegSampleStorage.Int32));
        Assert.That(image.PlanarBuffer.Components[0].IntSamples[0], Is.EqualTo(value));
        Assert.That(image.PlanarBuffer.Components[0].IntSamples[63], Is.EqualTo(value));
    }
}
