namespace jpeg.Tests;

internal class ScanHeaderParserTests
{
    [Test]
    public void Parser_ReadsScanComponentSelectorsAndSpectralRange()
    {
        global::jpeg.JpegScanHeaderParser parser = new global::jpeg.JpegScanHeaderParser();

        global::jpeg.JpegScanHeader scan = parser.Parse(IdentifierTests.CreateSosPayload(3));

        Assert.That(scan.ComponentCount, Is.EqualTo(3));
        Assert.That(scan.Components.Count, Is.EqualTo(3));
        Assert.That(scan.Components[0].Selector, Is.EqualTo(1));
        Assert.That(scan.Components[0].DcEntropyTableIdentifier, Is.EqualTo(0));
        Assert.That(scan.Components[0].AcEntropyTableIdentifier, Is.EqualTo(0));
        Assert.That(scan.Components[1].Selector, Is.EqualTo(2));
        Assert.That(scan.Components[1].DcEntropyTableIdentifier, Is.EqualTo(1));
        Assert.That(scan.Components[1].AcEntropyTableIdentifier, Is.EqualTo(1));
        Assert.That(scan.SpectralStart, Is.EqualTo(0));
        Assert.That(scan.SpectralEnd, Is.EqualTo(63));
        Assert.That(scan.SuccessiveHigh, Is.EqualTo(0));
        Assert.That(scan.SuccessiveLow, Is.EqualTo(0));
    }

    [Test]
    public void Identifier_StoresParsedScanHeaders()
    {
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfFrame0, IdentifierTests.CreateSofPayload(global::jpeg.JpegConstants.MarkerStartOfFrame0, 3));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfScan, IdentifierTests.CreateSosPayload(3));
        writer.WriteEndOfImage();

        global::jpeg.JpegInfo info = global::jpeg.JpegIdentifier.Identify(writer.ToArray());

        Assert.That(info.ScanCount, Is.EqualTo(1));
        Assert.That(info.ScanHeaders.Count, Is.EqualTo(1));
        Assert.That(info.LastScanHeader.ComponentCount, Is.EqualTo(3));
        Assert.That(info.LastScanHeader.Components[2].Selector, Is.EqualTo(3));
        Assert.That(info.LastScanHeader.Components[2].DcEntropyTableIdentifier, Is.EqualTo(1));
        Assert.That(info.LastScanSpectralEnd, Is.EqualTo(63));
    }

    [Test]
    public void Identifier_RejectsScanSelectorsMissingFromFrame()
    {
        byte[] scanPayload = IdentifierTests.CreateSosPayload(3);
        scanPayload[1] = 4;
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfFrame0, IdentifierTests.CreateSofPayload(global::jpeg.JpegConstants.MarkerStartOfFrame0, 3));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfScan, scanPayload);

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                global::jpeg.JpegIdentifier.Identify(writer.ToArray());
            });
    }

    [Test]
    public void Parser_RejectsMalformedScanHeaders()
    {
        global::jpeg.JpegScanHeaderParser parser = new global::jpeg.JpegScanHeaderParser();

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                parser.Parse(null!);
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                parser.Parse(new byte[] { 1, 1, 0 });
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                parser.Parse(new byte[] { 0, 0, 63, 0 });
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                parser.Parse(new byte[] { 5, 1, 0, 2, 0, 3, 0, 4, 0, 5, 0, 0, 63, 0 });
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                parser.Parse(new byte[] { 1, 1, 0 });
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                parser.Parse(new byte[] { 2, 1, 0, 1, 0, 0, 63, 0 });
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                parser.Parse(new byte[] { 1, 1, 0x40, 0, 63, 0 });
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                parser.Parse(new byte[] { 1, 1, 0x04, 0, 63, 0 });
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                parser.Parse(new byte[] { 1, 1, 0, 64, 63, 0 });
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                parser.Parse(new byte[] { 1, 1, 0, 10, 9, 0 });
            });
    }
}
