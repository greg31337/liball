namespace jpeg.Tests;

internal class IdentifierCoverageTests
{
    [TestCase(global::jpeg.JpegMarker.StartOfFrame0, global::jpeg.JpegProcess.BaselineDct, false, false, global::jpeg.JpegSupportStatus.Supported)]
    [TestCase(global::jpeg.JpegMarker.StartOfFrame1, global::jpeg.JpegProcess.ExtendedSequentialDct, false, false, global::jpeg.JpegSupportStatus.Supported)]
    [TestCase(global::jpeg.JpegMarker.StartOfFrame2, global::jpeg.JpegProcess.ProgressiveDct, true, false, global::jpeg.JpegSupportStatus.Supported)]
    [TestCase(global::jpeg.JpegMarker.StartOfFrame3, global::jpeg.JpegProcess.LosslessSequential, false, false, global::jpeg.JpegSupportStatus.Supported)]
    [TestCase(global::jpeg.JpegMarker.StartOfFrame5, global::jpeg.JpegProcess.DifferentialSequentialDct, false, false, global::jpeg.JpegSupportStatus.RecognizedButUnsupported)]
    [TestCase(global::jpeg.JpegMarker.StartOfFrame6, global::jpeg.JpegProcess.DifferentialProgressiveDct, true, false, global::jpeg.JpegSupportStatus.RecognizedButUnsupported)]
    [TestCase(global::jpeg.JpegMarker.StartOfFrame7, global::jpeg.JpegProcess.DifferentialLosslessSequential, false, false, global::jpeg.JpegSupportStatus.RecognizedButUnsupported)]
    [TestCase(global::jpeg.JpegMarker.StartOfFrame9, global::jpeg.JpegProcess.ExtendedSequentialDctArithmetic, false, true, global::jpeg.JpegSupportStatus.Supported)]
    [TestCase(global::jpeg.JpegMarker.StartOfFrame10, global::jpeg.JpegProcess.ProgressiveDctArithmetic, true, true, global::jpeg.JpegSupportStatus.Supported)]
    [TestCase(global::jpeg.JpegMarker.StartOfFrame11, global::jpeg.JpegProcess.LosslessSequentialArithmetic, false, true, global::jpeg.JpegSupportStatus.Supported)]
    [TestCase(global::jpeg.JpegMarker.StartOfFrame13, global::jpeg.JpegProcess.DifferentialSequentialDctArithmetic, false, true, global::jpeg.JpegSupportStatus.RecognizedButUnsupported)]
    [TestCase(global::jpeg.JpegMarker.StartOfFrame14, global::jpeg.JpegProcess.DifferentialProgressiveDctArithmetic, true, true, global::jpeg.JpegSupportStatus.RecognizedButUnsupported)]
    [TestCase(global::jpeg.JpegMarker.StartOfFrame15, global::jpeg.JpegProcess.DifferentialLosslessSequentialArithmetic, false, true, global::jpeg.JpegSupportStatus.RecognizedButUnsupported)]
    public void Identifier_ClassifiesEveryJpegOneStartOfFrameProcess(
        global::jpeg.JpegMarker marker,
        global::jpeg.JpegProcess process,
        bool progressive,
        bool arithmetic,
        global::jpeg.JpegSupportStatus support)
    {
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(marker, IdentifierTests.CreateSofPayload((int)marker, 1));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfScan, IdentifierTests.CreateSosPayload(1));
        writer.WriteEndOfImage();

        global::jpeg.JpegInfo info = new global::jpeg.JpegIdentifierService().Identify(writer.ToArray());

        Assert.That(info.FormatFamily, Is.EqualTo(global::jpeg.JpegFormatFamily.ClassicJpeg));
        Assert.That(info.FormatSupport, Is.EqualTo(global::jpeg.JpegSupportStatus.Supported));
        Assert.That(info.CodingProcessSupport, Is.EqualTo(support));
        Assert.That(info.Process, Is.EqualTo(process));
        Assert.That(info.Frame.Marker, Is.EqualTo(marker));
        Assert.That(info.Progressive, Is.EqualTo(progressive));
        Assert.That(info.ArithmeticCoding, Is.EqualTo(arithmetic));
        Assert.That(info.Diagnostics.Count, Is.EqualTo(support == global::jpeg.JpegSupportStatus.Supported ? 0 : 1));
    }

    [Test]
    public void Identifier_EnforcesEntropyLimitAcrossRestartBoundaries()
    {
        byte[] data = CreateJpegWithSplitEntropyData();
        global::jpeg.JpegReadOptions rejectedOptions = new global::jpeg.JpegReadOptions();
        rejectedOptions.MaxEntropyBytes = 5;
        global::jpeg.JpegReadOptions acceptedOptions = new global::jpeg.JpegReadOptions();
        acceptedOptions.MaxEntropyBytes = 6;
        global::jpeg.JpegIdentifierService identifier = new global::jpeg.JpegIdentifierService();

        global::jpeg.JpegResourceLimitException exception = Assert.Throws<global::jpeg.JpegResourceLimitException>(
            delegate { identifier.IdentifyWithOptions(data, rejectedOptions); })!;
        global::jpeg.JpegInfo info = identifier.IdentifyWithOptions(data, acceptedOptions);

        Assert.That(exception.Message, Does.Contain("entropy bytes"));
        Assert.That(info.SawEndOfImage, Is.True);
        Assert.That(info.RestartMarkerCount, Is.EqualTo(1));
    }

    internal static byte[] CreateJpegWithSplitEntropyData()
    {
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(
            global::jpeg.JpegMarker.StartOfFrame0,
            IdentifierTests.CreateSofPayload(global::jpeg.JpegConstants.MarkerStartOfFrame0, 1));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfScan, IdentifierTests.CreateSosPayload(1));
        writer.Writer.WriteBytes(new byte[] { 1, 2, 3 });
        writer.WriteStandaloneMarker(global::jpeg.JpegMarker.Restart0);
        writer.Writer.WriteBytes(new byte[] { 4, 5, 6 });
        writer.WriteEndOfImage();
        return writer.ToArray();
    }
}
