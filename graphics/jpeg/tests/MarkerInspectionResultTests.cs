namespace jpeg.Tests;

internal class MarkerInspectionResultTests
{
    [Test]
    public void Inspection_ExposesParsedStructureAndSpecialMarkerInformation()
    {
        byte[] data = CreateStructuredInspectionJpeg();
        global::jpeg.JpegInspectionService inspector = new global::jpeg.JpegInspectionService();

        global::jpeg.JpegInspectionResult result = inspector.InspectMarkers(data);

        Assert.That(result.Markers[0].Marker, Is.EqualTo(global::jpeg.JpegMarker.StartOfImage));
        Assert.That(result.Markers[result.Markers.Count - 1].Marker, Is.EqualTo(global::jpeg.JpegMarker.EndOfImage));
        Assert.That(result.Frame.Width, Is.EqualTo(32));
        Assert.That(result.Frame.Height, Is.EqualTo(0));
        Assert.That(result.ScanHeaders.Count, Is.EqualTo(1));
        Assert.That(result.ScanHeaders[0].ComponentCount, Is.EqualTo(1));
        Assert.That(result.Tables.QuantizationTables.Count, Is.EqualTo(1));
        Assert.That(result.Tables.HuffmanTables.Count, Is.EqualTo(1));
        Assert.That(result.Tables.ArithmeticConditioningTables.Count, Is.EqualTo(2));
        Assert.That(result.HasRestartInterval, Is.True);
        Assert.That(result.RestartInterval, Is.EqualTo(3));
        Assert.That(result.RestartMarkerCount, Is.EqualTo(1));
        Assert.That(result.HasDefinedNumberOfLines, Is.True);
        Assert.That(result.DefinedNumberOfLines, Is.EqualTo(16));
        Assert.That(result.Info.Height, Is.EqualTo(16));
        Assert.That(result.HasHierarchicalProgression, Is.True);
        Assert.That(result.HierarchicalProgressionMarkerCount, Is.EqualTo(1));
        Assert.That(result.ExpandReferenceComponentMarkerCount, Is.EqualTo(1));
        Assert.That(result.HasSmartScale, Is.True);
        Assert.That(result.SmartScaleMarkerCount, Is.EqualTo(1));
        Assert.That(result.Diagnostics.Count, Is.EqualTo(3));
        Assert.That(result.Markers.Any(item => item.Marker == global::jpeg.JpegMarker.DefineNumberOfLines && item.FoundAfterEntropyData), Is.True);
        Assert.That(result.Markers.Any(item => item.Marker == global::jpeg.JpegMarker.Restart0 && item.FoundAfterEntropyData), Is.True);
    }

    [Test]
    public void Inspection_StrictRejectsAndTolerantReportsMarkerOrderProblems()
    {
        byte[] data = CreateDuplicateFrameJpeg();
        global::jpeg.JpegInspectionService inspector = new global::jpeg.JpegInspectionService();
        global::jpeg.JpegReadOptions tolerantOptions = new global::jpeg.JpegReadOptions();
        tolerantOptions.ErrorPolicy = global::jpeg.JpegErrorPolicy.Tolerant;

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                inspector.InspectMarkers(data);
            });

        global::jpeg.JpegInspectionResult tolerant = inspector.InspectMarkersWithOptions(data, tolerantOptions);

        Assert.That(tolerant.Markers.Count(item => item.Marker == global::jpeg.JpegMarker.StartOfFrame0), Is.EqualTo(2));
        Assert.That(tolerant.Diagnostics.Any(item => item.Code == global::jpeg.JpegDiagnosticCode.InvalidMarkerOrder), Is.True);
        Assert.That(tolerant.Info.SawEndOfImage, Is.True);
    }

    [Test]
    public void Inspection_SupportsSpanStreamAndFileWorkflows()
    {
        byte[] data = IdentifierTests.CreateMinimalBaselineJpeg();
        global::jpeg.JpegInspectionService inspector = new global::jpeg.JpegInspectionService();
        global::jpeg.JpegReadOptions options = new global::jpeg.JpegReadOptions();
        options.MetadataPolicy = global::jpeg.JpegMetadataPolicy.StripAll;
        string path = JpegFilePathApiTests.CreateTempJpegPath();
        System.IO.File.WriteAllBytes(path, data);
        try
        {
            global::jpeg.JpegInspectionResult span = inspector.InspectMarkersSpanWithOptions(new System.ReadOnlySpan<byte>(data), options);
            System.IO.MemoryStream stream = new System.IO.MemoryStream(data);
            global::jpeg.JpegInspectionResult streamed = inspector.InspectMarkersStreamWithOptions(stream, options);
            global::jpeg.JpegInspectionResult file = inspector.InspectMarkersFileWithOptions(path, options);

            Assert.That(span.Markers.Select(item => item.Value), Is.EqualTo(streamed.Markers.Select(item => item.Value)));
            Assert.That(streamed.Markers.Select(item => item.Value), Is.EqualTo(file.Markers.Select(item => item.Value)));
            Assert.That(span.Frame.Width, Is.EqualTo(32));
            Assert.That(streamed.ScanHeaders.Count, Is.EqualTo(1));
            Assert.That(file.Info.Metadata.RawMarkers, Is.Empty);
            Assert.That(stream.CanRead, Is.True);
        }
        finally
        {
            System.IO.File.Delete(path);
        }
    }

    [Test]
    public void Inspection_ReportsJpegLsAndHonorsCancellation()
    {
        byte[] jpegLs = CreateJpegLsInspectionJpeg();
        global::jpeg.JpegInspectionService inspector = new global::jpeg.JpegInspectionService();

        global::jpeg.JpegInspectionResult result = inspector.InspectMarkers(jpegLs);
        System.Threading.CancellationTokenSource cancellation = new System.Threading.CancellationTokenSource();
        cancellation.Cancel();

        Assert.That(result.IsJpegLs, Is.True);
        Assert.That(result.JpegLsMarkerCount, Is.EqualTo(1));
        Assert.That(result.Frame.Process, Is.EqualTo(global::jpeg.JpegProcess.JpegLs));
        Assert.That(result.Info.FormatFamily, Is.EqualTo(global::jpeg.JpegFormatFamily.JpegLs));
        Assert.That(result.Diagnostics.Any(item => item.Marker == global::jpeg.JpegMarker.JpegLs), Is.True);
        Assert.Throws<System.OperationCanceledException>(
            delegate
            {
                inspector.InspectMarkersWithCancellation(jpegLs, cancellation.Token);
            });
    }

    public static byte[] CreateStructuredInspectionJpeg()
    {
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.DefineQuantizationTable, QuantizationTableParserTests.CreateEightBitTablePayload(0));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.DefineHuffmanTable, HuffmanTableParserTests.CreateCanonicalDcPayload());
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.DefineArithmeticConditioning, new byte[] { 0x00, 0xA3, 0x11, 0x2A });
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.DefineRestartInterval, new byte[] { 0, 3 });
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.DefineHierarchicalProgression, new byte[] { 1 });
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.ExpandReferenceComponents, new byte[] { 2 });
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.JpegExtension, new byte[] { 0, 8 });
        byte[] frame = IdentifierTests.CreateSofPayload(global::jpeg.JpegConstants.MarkerStartOfFrame0, 1);
        frame[1] = 0;
        frame[2] = 0;
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfFrame0, frame);
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfScan, IdentifierTests.CreateSosPayload(1));
        writer.Writer.WriteByte(0x11);
        writer.Writer.WriteUInt16(global::jpeg.JpegConstants.MarkerRestart0);
        writer.Writer.WriteByte(0x22);
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.DefineNumberOfLines, new byte[] { 0, 16 });
        writer.Writer.WriteByte(0x33);
        writer.WriteEndOfImage();
        return writer.ToArray();
    }

    public static byte[] CreateDuplicateFrameJpeg()
    {
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfFrame0, IdentifierTests.CreateSofPayload(global::jpeg.JpegConstants.MarkerStartOfFrame0, 1));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfFrame0, IdentifierTests.CreateSofPayload(global::jpeg.JpegConstants.MarkerStartOfFrame0, 1));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfScan, IdentifierTests.CreateSosPayload(1));
        writer.WriteEndOfImage();
        return writer.ToArray();
    }

    public static byte[] CreateJpegLsInspectionJpeg()
    {
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.JpegLs, IdentifierTests.CreateSofPayload(global::jpeg.JpegConstants.MarkerJpegLs, 1));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfScan, JpegLsModeTests.CreateJpegLsLikeSosPayload());
        writer.WriteEndOfImage();
        return writer.ToArray();
    }
}
