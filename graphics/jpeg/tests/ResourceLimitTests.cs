namespace jpeg.Tests;

internal class ResourceLimitTests
{
    [Test]
    public void Identify_EnforcesDimensionAndComponentLimits()
    {
        byte[] data = IdentifierTests.CreateMinimalBaselineJpeg();

        global::jpeg.JpegReadOptions widthOptions = new global::jpeg.JpegReadOptions();
        widthOptions.MaxWidth = 31;
        Assert.Throws<global::jpeg.JpegResourceLimitException>(
            delegate
            {
                global::jpeg.JpegIdentifier.Identify(data, widthOptions);
            });

        global::jpeg.JpegReadOptions heightOptions = new global::jpeg.JpegReadOptions();
        heightOptions.MaxHeight = 15;
        Assert.Throws<global::jpeg.JpegResourceLimitException>(
            delegate
            {
                global::jpeg.JpegIdentifier.Identify(data, heightOptions);
            });

        global::jpeg.JpegReadOptions componentOptions = new global::jpeg.JpegReadOptions();
        componentOptions.MaxComponents = 2;
        Assert.Throws<global::jpeg.JpegResourceLimitException>(
            delegate
            {
                global::jpeg.JpegIdentifier.Identify(data, componentOptions);
            });
    }

    [Test]
    public void Identify_EnforcesScanLimit()
    {
        byte[] data = IdentifierTests.CreateMinimalBaselineJpeg();
        global::jpeg.JpegReadOptions options = new global::jpeg.JpegReadOptions();
        options.MaxScans = 0;

        Assert.Throws<global::jpeg.JpegResourceLimitException>(
            delegate
            {
                global::jpeg.JpegIdentifier.Identify(data, options);
            });
    }

    [Test]
    public void Identify_EnforcesMetadataByteLimit()
    {
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.App0, new byte[] { 1, 2 });
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfFrame0, IdentifierTests.CreateSofPayload(global::jpeg.JpegConstants.MarkerStartOfFrame0, 3));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfScan, IdentifierTests.CreateSosPayload(3));
        writer.WriteEndOfImage();

        global::jpeg.JpegReadOptions options = new global::jpeg.JpegReadOptions();
        options.MaxMetadataBytes = 1;

        Assert.Throws<global::jpeg.JpegResourceLimitException>(
            delegate
            {
                global::jpeg.JpegIdentifier.Identify(writer.ToArray(), options);
            });
    }

    [Test]
    public void Identify_EnforcesSingleMarkerPayloadLimitBeforeRetention()
    {
        byte[] data = CreateJpegWithApplicationMarkers(1, 2);
        global::jpeg.JpegReadOptions options = new global::jpeg.JpegReadOptions();
        options.MaxMarkerPayloadBytes = 1;

        global::jpeg.JpegResourceLimitException exception = Assert.Throws<global::jpeg.JpegResourceLimitException>(
            delegate
            {
                global::jpeg.JpegIdentifier.Identify(data, options);
            })!;

        Assert.That(exception.Message, Does.Contain("marker payload"));
    }

    [Test]
    public void Identify_EnforcesRetainedMarkerCountLimit()
    {
        byte[] data = CreateJpegWithApplicationMarkers(2, 1);
        global::jpeg.JpegReadOptions options = new global::jpeg.JpegReadOptions();
        options.MaxRetainedMarkers = 1;

        global::jpeg.JpegResourceLimitException exception = Assert.Throws<global::jpeg.JpegResourceLimitException>(
            delegate
            {
                global::jpeg.JpegIdentifier.Identify(data, options);
            })!;

        Assert.That(exception.Message, Does.Contain("retained marker"));
    }

    [Test]
    public void Identify_EnforcesCombinedTableCountLimit()
    {
        byte[] data = JpegReadApiTests.CreateReadableGrayscaleJpeg(2);
        global::jpeg.JpegReadOptions options = new global::jpeg.JpegReadOptions();
        options.MaxTables = 0;

        global::jpeg.JpegResourceLimitException exception = Assert.Throws<global::jpeg.JpegResourceLimitException>(
            delegate
            {
                global::jpeg.JpegIdentifier.Identify(data, options);
            })!;

        Assert.That(exception.Message, Does.Contain("table count"));
    }

    [Test]
    public void Read_EnforcesCombinedComponentBufferByteLimit()
    {
        byte[] data = JpegReadApiTests.CreateReadableGrayscaleJpeg(2);
        global::jpeg.JpegReadOptions options = new global::jpeg.JpegReadOptions();
        options.MaxComponentBufferBytes = 63;

        global::jpeg.JpegResourceLimitException exception = Assert.Throws<global::jpeg.JpegResourceLimitException>(
            delegate
            {
                global::jpeg.Jpeg.Read(data, options);
            })!;

        Assert.That(exception.Message, Does.Contain("component buffer"));
    }

    [Test]
    public void ReadProgressive_EnforcesCoefficientStateByteLimit()
    {
        byte[] data = CallbackStreamingApiTests.CreateProgressiveGrayscaleJpeg(143);
        global::jpeg.JpegReadOptions options = new global::jpeg.JpegReadOptions();
        options.MaxProgressiveStateBytes = 255;
        TrackingProgressivePreviews callback = new TrackingProgressivePreviews();
        global::jpeg.IJpegReader reader = new global::jpeg.JpegReader();

        global::jpeg.JpegResourceLimitException exception = Assert.Throws<global::jpeg.JpegResourceLimitException>(
            delegate
            {
                reader.ReadProgressivePreviewsWithOptionsAndCancellation(data, options, callback, default(System.Threading.CancellationToken));
            })!;

        Assert.That(exception.Message, Does.Contain("progressive coefficient state"));
        Assert.That(callback.ScanIndexes, Is.Empty);
    }

    [Test]
    public void ReadRows_EnforcesCallbackOutputByteLimit()
    {
        byte[] data = JpegReadApiTests.CreateReadableGrayscaleJpeg(2);
        global::jpeg.JpegReadOptions options = new global::jpeg.JpegReadOptions();
        options.MaxCallbackOutputBytes = 7;
        TrackingDecodedRows callback = new TrackingDecodedRows();
        global::jpeg.IJpegReader reader = new global::jpeg.JpegReader();

        global::jpeg.JpegResourceLimitException exception = Assert.Throws<global::jpeg.JpegResourceLimitException>(
            delegate
            {
                reader.ReadRowsWithOptionsAndCancellation(data, options, callback, default(System.Threading.CancellationToken));
            })!;

        Assert.That(exception.Message, Does.Contain("callback row"));
        Assert.That(callback.RowIndexes, Is.Empty);
    }

    [Test]
    public void ReadProgressive_EnforcesCallbackOutputByteLimit()
    {
        byte[] data = CallbackStreamingApiTests.CreateProgressiveGrayscaleJpeg(143);
        global::jpeg.JpegReadOptions options = new global::jpeg.JpegReadOptions();
        options.MaxCallbackOutputBytes = 63;
        TrackingProgressivePreviews callback = new TrackingProgressivePreviews();
        global::jpeg.IJpegReader reader = new global::jpeg.JpegReader();

        global::jpeg.JpegResourceLimitException exception = Assert.Throws<global::jpeg.JpegResourceLimitException>(
            delegate
            {
                reader.ReadProgressivePreviewsWithOptionsAndCancellation(data, options, callback, default(System.Threading.CancellationToken));
            })!;

        Assert.That(exception.Message, Does.Contain("Progressive preview"));
        Assert.That(callback.ScanIndexes, Is.Empty);
    }

    [Test]
    public void ProgressiveDecoder_EnforcesPlanarCallbackOutputByteLimit()
    {
        global::jpeg.JpegFrameHeader frame = ProgressiveSof2FullImageDecoderTests.CreateProgressiveFrame(8, 8);
        frame.Components.Add(BaselineSof0FullImageDecoderTests.CreateFrameComponent(1, 1, 1, 0));
        frame.ComponentCount = 1;
        global::jpeg.JpegScanHeader scan = ProgressiveSof2FullImageDecoderTests.CreateDcScan(frame, 0, 0);
        global::jpeg.JpegTableStore tables = BaselineSof0FullImageDecoderTests.CreateTableStore(40);
        System.Collections.Generic.List<global::jpeg.JpegProgressiveScanData> scans = new System.Collections.Generic.List<global::jpeg.JpegProgressiveScanData>();
        scans.Add(ProgressiveSof2FullImageDecoderTests.CreateScanData(scan, ProgressiveSof2FullImageDecoderTests.CreateDcEntropy(scan, new int[] { 2 }, new int[] { 0 })));
        global::jpeg.JpegReadOptions options = new global::jpeg.JpegReadOptions();
        options.MaxCallbackOutputBytes = 63;
        global::jpeg.JpegProgressiveSof2FullImageDecoder decoder = new global::jpeg.JpegProgressiveSof2FullImageDecoder();
        int callbackCount = 0;

        global::jpeg.JpegResourceLimitException exception = Assert.Throws<global::jpeg.JpegResourceLimitException>(
            delegate
            {
                decoder.DecodeWithScanCallbackOptions(
                    frame,
                    scans,
                    tables,
                    0,
                    options,
                    delegate (global::jpeg.JpegProgressiveScanCallbackUpdate update)
                    {
                        callbackCount = callbackCount + 1;
                    });
            })!;

        Assert.That(exception.Message, Does.Contain("Progressive scan callback"));
        Assert.That(callbackCount, Is.EqualTo(0));
    }

    [Test]
    public void Identify_EnforcesOutputPixelAndByteLimits()
    {
        byte[] data = IdentifierTests.CreateMinimalBaselineJpeg();

        global::jpeg.JpegReadOptions pixelOptions = new global::jpeg.JpegReadOptions();
        pixelOptions.MaxOutputPixels = 511;
        Assert.Throws<global::jpeg.JpegResourceLimitException>(
            delegate
            {
                global::jpeg.JpegIdentifier.Identify(data, pixelOptions);
            });

        global::jpeg.JpegReadOptions byteOptions = new global::jpeg.JpegReadOptions();
        byteOptions.MaxOutputBytes = 1535;
        Assert.Throws<global::jpeg.JpegResourceLimitException>(
            delegate
            {
                global::jpeg.JpegIdentifier.Identify(data, byteOptions);
            });
    }

    [Test]
    public void Identify_ReportsOutputEstimatesWhenWithinLimits()
    {
        byte[] data = IdentifierTests.CreateMinimalBaselineJpeg();
        global::jpeg.JpegInfo info = global::jpeg.JpegIdentifier.Identify(data, new global::jpeg.JpegReadOptions());

        Assert.That(info.EstimatedOutputPixels, Is.EqualTo(512));
        Assert.That(info.EstimatedOutputBytes, Is.EqualTo(1536));
    }

    [Test]
    public void Identify_AllowsNegativeLimitsAsDisabled()
    {
        byte[] data = IdentifierTests.CreateMinimalBaselineJpeg();
        global::jpeg.JpegReadOptions options = new global::jpeg.JpegReadOptions();
        options.MaxWidth = -1;
        options.MaxHeight = -1;
        options.MaxComponents = -1;
        options.MaxScans = -1;
        options.MaxMarkerPayloadBytes = -1;
        options.MaxRetainedMarkers = -1;
        options.MaxTables = -1;
        options.MaxMetadataBytes = -1;
        options.MaxEntropyBytes = -1;
        options.MaxOutputPixels = -1;
        options.MaxOutputBytes = -1;
        options.MaxComponentBufferBytes = -1;
        options.MaxProgressiveStateBytes = -1;
        options.MaxCallbackOutputBytes = -1;

        global::jpeg.JpegInfo info = global::jpeg.JpegIdentifier.Identify(data, options);

        Assert.That(info.SawEndOfImage, Is.True);
    }

    [Test]
    public void Identify_StreamOverloadUsesConfiguredLimits()
    {
        byte[] data = IdentifierTests.CreateMinimalBaselineJpeg();
        System.IO.MemoryStream stream = new System.IO.MemoryStream(data);
        global::jpeg.JpegReadOptions options = new global::jpeg.JpegReadOptions();
        options.MaxWidth = 31;

        Assert.Throws<global::jpeg.JpegResourceLimitException>(
            delegate
            {
                global::jpeg.JpegIdentifier.Identify(stream, options);
            });
    }

    [Test]
    public void Identify_RejectsNullOptions()
    {
        byte[] data = IdentifierTests.CreateMinimalBaselineJpeg();

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                global::jpeg.JpegIdentifier.Identify(data, null!);
            });
    }

    public static byte[] CreateJpegWithApplicationMarkers(int markerCount, int payloadLength)
    {
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        byte[] payload = new byte[payloadLength];
        int index = 0;
        while (index < markerCount)
        {
            writer.WriteMarkerSegment(global::jpeg.JpegMarker.App3, payload);
            index = index + 1;
        }

        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfFrame0, IdentifierTests.CreateSofPayload(global::jpeg.JpegConstants.MarkerStartOfFrame0, 1));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfScan, IdentifierTests.CreateSosPayload(1));
        writer.WriteEndOfImage();
        return writer.ToArray();
    }
}
