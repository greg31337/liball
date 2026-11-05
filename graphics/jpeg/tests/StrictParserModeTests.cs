namespace jpeg.Tests;

internal class StrictParserModeTests
{
    [Test]
    public void StrictIdentify_RejectsScanBeforeFrame()
    {
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfScan, IdentifierTests.CreateSosPayload(1));
        writer.WriteEndOfImage();

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                global::jpeg.JpegIdentifier.Identify(writer.ToArray());
            });
    }

    [Test]
    public void StrictIdentify_RejectsEndOfImageBeforeScan()
    {
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfFrame0, IdentifierTests.CreateSofPayload(global::jpeg.JpegConstants.MarkerStartOfFrame0, 1));
        writer.WriteEndOfImage();

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                global::jpeg.JpegIdentifier.Identify(writer.ToArray());
            });
    }

    [Test]
    public void StrictIdentify_RejectsDuplicateFrameHeaders()
    {
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfFrame0, IdentifierTests.CreateSofPayload(global::jpeg.JpegConstants.MarkerStartOfFrame0, 1));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfFrame0, IdentifierTests.CreateSofPayload(global::jpeg.JpegConstants.MarkerStartOfFrame0, 1));

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                global::jpeg.JpegIdentifier.Identify(writer.ToArray());
            });
    }

    [Test]
    public void StrictIdentify_RejectsRestartMarkerOutsideEntropyData()
    {
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfFrame0, IdentifierTests.CreateSofPayload(global::jpeg.JpegConstants.MarkerStartOfFrame0, 1));
        writer.WriteStandaloneMarker(global::jpeg.JpegMarker.Restart0);

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                global::jpeg.JpegIdentifier.Identify(writer.ToArray());
            });
    }

    [Test]
    public void StrictIdentify_RejectsRepeatedStartOfImage()
    {
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteStartOfImage();

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                global::jpeg.JpegIdentifier.Identify(writer.ToArray());
            });
    }
}
