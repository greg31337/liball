namespace jpeg.Tests;

internal class PublicIoExceptionContractTests
{
    [Test]
    public void PublicInputFileServices_UseDeterministicIoFailures()
    {
        string missingPath = System.IO.Path.Combine(
            TestContext.CurrentContext.WorkDirectory,
            "missing-jpeg-" + System.Guid.NewGuid().ToString("N") + ".jpg");
        string outputPath = JpegFilePathApiTests.CreateTempJpegPath();

        AssertReadFailure(delegate { new global::jpeg.JpegIdentifierService().IdentifyFile(missingPath); });
        AssertReadFailure(delegate { new global::jpeg.JpegReader().ReadFile(missingPath); });
        AssertReadFailure(delegate { new global::jpeg.JpegInspectionService().ReadMarkersFile(missingPath); });
        AssertReadFailure(delegate { new global::jpeg.JpegInspectionService().InspectMarkersFile(missingPath); });
        AssertReadFailure(delegate { new global::jpeg.JpegInspectionService().ReadCoefficientsFile(missingPath); });
        AssertReadFailure(delegate
        {
            new global::jpeg.JpegMetadataEditor().StripMetadataFile(missingPath, outputPath);
        });
        AssertReadFailure(delegate
        {
            new global::jpeg.JpegTransformer().Rotate90FileWithOptionsAndCancellation(
                missingPath,
                outputPath,
                new global::jpeg.JpegTransformOptions(),
                default(System.Threading.CancellationToken));
        });

        Assert.That(System.IO.File.Exists(outputPath), Is.False);
    }

    [Test]
    public void PublicInputStreamServices_UseDeterministicIoFailures()
    {
        AssertReadFailure(delegate
        {
            new global::jpeg.JpegIdentifierService().IdentifyStream(new JpegIoThrowingReadStream());
        });
        AssertReadFailure(delegate
        {
            new global::jpeg.JpegReader().ReadStream(new JpegIoThrowingReadStream());
        });
        AssertReadFailure(delegate
        {
            new global::jpeg.JpegInspectionService().ReadMarkersStream(new JpegIoThrowingReadStream());
        });
        AssertReadFailure(delegate
        {
            new global::jpeg.JpegInspectionService().InspectMarkersStream(new JpegIoThrowingReadStream());
        });
        AssertReadFailure(delegate
        {
            new global::jpeg.JpegInspectionService().ReadCoefficientsStream(new JpegIoThrowingReadStream());
        });
        AssertReadFailure(delegate
        {
            new global::jpeg.JpegMetadataEditor().StripMetadataStream(
                new JpegIoThrowingReadStream(),
                new System.IO.MemoryStream());
        });
        AssertReadFailure(delegate
        {
            new global::jpeg.JpegTransformer().Rotate90StreamWithOptionsAndCancellation(
                new JpegIoThrowingReadStream(),
                new System.IO.MemoryStream(),
                new global::jpeg.JpegTransformOptions(),
                default(System.Threading.CancellationToken));
        });
    }

    [Test]
    public void PublicOutputStreamServices_UseDeterministicIoFailures()
    {
        global::jpeg.JpegImage image = JpegWriteApiTests.CreateGrayscaleImage(8, 8, 138);
        byte[] source = global::jpeg.Jpeg.Write(image);
        global::jpeg.JpegCoefficientImage coefficients = new global::jpeg.JpegInspectionService().ReadCoefficients(source);

        AssertWriteFailure(delegate
        {
            new global::jpeg.JpegWriter().WriteToStream(image, new JpegIoThrowingWriteStream());
        });
        AssertWriteFailure(delegate
        {
            new global::jpeg.JpegInspectionService().WriteCoefficientsStream(
                coefficients,
                new JpegIoThrowingWriteStream());
        });
        AssertWriteFailure(delegate
        {
            new global::jpeg.JpegMetadataEditor().StripMetadataStream(
                new System.IO.MemoryStream(source),
                new JpegIoThrowingWriteStream());
        });
        AssertWriteFailure(delegate
        {
            new global::jpeg.JpegTransformer().Rotate90StreamWithOptionsAndCancellation(
                new System.IO.MemoryStream(source),
                new JpegIoThrowingWriteStream(),
                new global::jpeg.JpegTransformOptions(),
                default(System.Threading.CancellationToken));
        });
    }

    [Test]
    public void PublicOutputFileServices_UseDeterministicIoFailures()
    {
        global::jpeg.JpegImage image = JpegWriteApiTests.CreateGrayscaleImage(8, 8, 138);
        byte[] source = global::jpeg.Jpeg.Write(image);
        global::jpeg.JpegCoefficientImage coefficients = new global::jpeg.JpegInspectionService().ReadCoefficients(source);
        string inputPath = JpegFilePathApiTests.CreateTempJpegPath();
        string missingDirectory = System.IO.Path.Combine(
            TestContext.CurrentContext.WorkDirectory,
            "missing-directory-" + System.Guid.NewGuid().ToString("N"));
        string outputPath = System.IO.Path.Combine(missingDirectory, "output.jpg");
        System.IO.File.WriteAllBytes(inputPath, source);

        try
        {
            AssertWriteFailure(delegate { new global::jpeg.JpegWriter().WriteFile(image, outputPath); });
            AssertWriteFailure(delegate
            {
                new global::jpeg.JpegInspectionService().WriteCoefficientsFile(outputPath, coefficients);
            });
            AssertWriteFailure(delegate
            {
                new global::jpeg.JpegMetadataEditor().StripMetadataFile(inputPath, outputPath);
            });
            AssertWriteFailure(delegate
            {
                new global::jpeg.JpegTransformer().Rotate90FileWithOptionsAndCancellation(
                    inputPath,
                    outputPath,
                    new global::jpeg.JpegTransformOptions(),
                    default(System.Threading.CancellationToken));
            });
        }
        finally
        {
            System.IO.File.Delete(inputPath);
        }
    }

    [Test]
    public void PublicServices_KeepNonIoExceptionCategoriesDistinct()
    {
        global::jpeg.JpegReader reader = new global::jpeg.JpegReader();
        global::jpeg.JpegWriter writer = new global::jpeg.JpegWriter();
        byte[] data = JpegReadApiTests.CreateReadableGrayscaleJpeg(2);
        global::jpeg.JpegReadOptions limited = new global::jpeg.JpegReadOptions();
        limited.MaxEntropyBytes = 0;
        global::jpeg.JpegImage invalidImage = JpegWriteApiTests.CreateGrayscaleImage(8, 8, 128);
        invalidImage.Width = 0;
        global::jpeg.JpegMarkerSegmentWriter unsupported = new global::jpeg.JpegMarkerSegmentWriter();
        unsupported.WriteStartOfImage();
        unsupported.WriteMarkerSegment(
            global::jpeg.JpegMarker.StartOfFrame5,
            IdentifierTests.CreateSofPayload(global::jpeg.JpegConstants.MarkerStartOfFrame5, 1));
        unsupported.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfScan, IdentifierTests.CreateSosPayload(1));
        unsupported.WriteEndOfImage();

        Assert.Throws<System.ArgumentNullException>(delegate { reader.ReadStream(null!); });
        Assert.Throws<System.ArgumentException>(delegate
        {
            writer.WriteToStream(JpegWriteApiTests.CreateGrayscaleImage(8, 8, 128), new System.IO.MemoryStream(new byte[1], false));
        });
        Assert.Throws<global::jpeg.JpegInvalidDataException>(delegate { reader.Read(new byte[] { 0xff, 0xd8 }); });
        Assert.Throws<global::jpeg.JpegUnsupportedException>(delegate { reader.Read(unsupported.ToArray()); });
        Assert.Throws<global::jpeg.JpegResourceLimitException>(delegate { reader.ReadWithOptions(data, limited); });
        Assert.Throws<global::jpeg.JpegWriteValidationException>(delegate { writer.Write(invalidImage); });
        Assert.Throws<System.OperationCanceledException>(delegate
        {
            new global::jpeg.JpegIdentifierService().IdentifyStreamWithCancellation(
                new JpegIoThrowingReadStream(),
                new System.Threading.CancellationToken(true));
        });
    }

    internal static void AssertReadFailure(TestDelegate operation)
    {
        global::jpeg.JpegIOException exception = Assert.Throws<global::jpeg.JpegIOException>(operation)!;
        Assert.That(exception.Message, Is.EqualTo("JPEG input stream could not be read.").Or.EqualTo("JPEG input file could not be read."));
        Assert.That(exception.InnerException, Is.Not.Null);
        Assert.That(exception.InnerException, Is.InstanceOf<System.IO.IOException>());
    }

    internal static void AssertWriteFailure(TestDelegate operation)
    {
        global::jpeg.JpegIOException exception = Assert.Throws<global::jpeg.JpegIOException>(operation)!;
        Assert.That(exception.Message, Is.EqualTo("JPEG output stream could not be written.").Or.EqualTo("JPEG output file could not be written."));
        Assert.That(exception.InnerException, Is.Not.Null);
        Assert.That(exception.InnerException, Is.InstanceOf<System.IO.IOException>());
    }
}

internal class JpegIoThrowingReadStream : System.IO.Stream
{
    public override bool CanRead
    {
        get { return true; }
    }

    public override bool CanSeek
    {
        get { return false; }
    }

    public override bool CanWrite
    {
        get { return false; }
    }

    public override long Length
    {
        get { throw new System.NotSupportedException(); }
    }

    public override long Position
    {
        get { throw new System.NotSupportedException(); }
        set { throw new System.NotSupportedException(); }
    }

    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new System.IO.IOException("native read failure");
    }

    public override long Seek(long offset, System.IO.SeekOrigin origin)
    {
        throw new System.NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new System.NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new System.NotSupportedException();
    }
}

internal class JpegIoThrowingWriteStream : System.IO.Stream
{
    public override bool CanRead
    {
        get { return false; }
    }

    public override bool CanSeek
    {
        get { return false; }
    }

    public override bool CanWrite
    {
        get { return true; }
    }

    public override long Length
    {
        get { throw new System.NotSupportedException(); }
    }

    public override long Position
    {
        get { throw new System.NotSupportedException(); }
        set { throw new System.NotSupportedException(); }
    }

    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new System.NotSupportedException();
    }

    public override long Seek(long offset, System.IO.SeekOrigin origin)
    {
        throw new System.NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new System.NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new System.IO.IOException("native write failure");
    }
}
