namespace jpeg.Tests;

internal class MarkerScannerTests
{
    [Test]
    public void ReadStartOfImage_ValidatesSoiAndReturnsOffset()
    {
        byte[] data = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };
        global::jpeg.JpegMarkerScanner scanner = new global::jpeg.JpegMarkerScanner(data);

        global::jpeg.JpegScannedMarker soi = scanner.ReadStartOfImage();
        global::jpeg.JpegScannedMarker app0 = scanner.ReadNextMarker();

        Assert.That(soi.Marker, Is.EqualTo(global::jpeg.JpegMarker.StartOfImage));
        Assert.That(soi.Value, Is.EqualTo(global::jpeg.JpegConstants.MarkerStartOfImage));
        Assert.That(soi.Code, Is.EqualTo(0xD8));
        Assert.That(soi.Offset, Is.EqualTo(0));
        Assert.That(app0.Marker, Is.EqualTo(global::jpeg.JpegMarker.App0));
        Assert.That(app0.Offset, Is.EqualTo(2));
    }

    [Test]
    public void ReadStartOfImage_RejectsInvalidSoi()
    {
        global::jpeg.JpegMarkerScanner scanner = new global::jpeg.JpegMarkerScanner(new byte[] { 0x00, 0x00 });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                scanner.ReadStartOfImage();
            });
    }

    [Test]
    public void ReadNextMarker_SkipsFillBytesAtMarkerBoundary()
    {
        byte[] data = new byte[] { 0xFF, 0xD8, 0xFF, 0xFF, 0xFF, 0xE1 };
        global::jpeg.JpegMarkerScanner scanner = new global::jpeg.JpegMarkerScanner(data);

        scanner.ReadStartOfImage();
        global::jpeg.JpegScannedMarker marker = scanner.ReadNextMarker();

        Assert.That(marker.Marker, Is.EqualTo(global::jpeg.JpegMarker.App1));
        Assert.That(marker.Value, Is.EqualTo(global::jpeg.JpegConstants.MarkerApp1));
        Assert.That(marker.Offset, Is.EqualTo(2));
    }

    [Test]
    public void StreamScanner_ReadsMarkersFromStream()
    {
        byte[] data = new byte[] { 0xFF, 0xD8, 0xFF, 0xFE };
        System.IO.MemoryStream stream = new System.IO.MemoryStream(data);
        global::jpeg.JpegMarkerScanner scanner = global::jpeg.JpegMarkerScanner.FromStream(stream);

        scanner.ReadStartOfImage();
        global::jpeg.JpegScannedMarker marker = scanner.ReadNextMarker();

        Assert.That(marker.Marker, Is.EqualTo(global::jpeg.JpegMarker.Comment));
        Assert.That(marker.Offset, Is.EqualTo(2));
    }

    [Test]
    public void ReadEntropyMarker_IgnoresStuffedBytesAndFindsRestartMarker()
    {
        byte[] data = new byte[] { 0xFF, 0xD8, 0x11, 0xFF, 0x00, 0x22, 0xFF, 0xD0 };
        global::jpeg.JpegMarkerScanner scanner = new global::jpeg.JpegMarkerScanner(data);

        scanner.ReadStartOfImage();
        global::jpeg.JpegScannedMarker marker = scanner.ReadEntropyMarker();

        Assert.That(marker.Marker, Is.EqualTo(global::jpeg.JpegMarker.Restart0));
        Assert.That(marker.FoundAfterEntropyData, Is.True);
        Assert.That(marker.EntropyBytesSkipped, Is.EqualTo(4));
        Assert.That(marker.Offset, Is.EqualTo(6));
        Assert.That(scanner.IsRestartMarker(marker), Is.True);
    }

    [Test]
    public void ReadEntropyMarker_SkipsFillBytesBeforeMarkerCode()
    {
        byte[] data = new byte[] { 0xFF, 0xD8, 0x44, 0xFF, 0xFF, 0xFF, 0xD9 };
        global::jpeg.JpegMarkerScanner scanner = new global::jpeg.JpegMarkerScanner(data);

        scanner.ReadStartOfImage();
        global::jpeg.JpegScannedMarker marker = scanner.ReadEntropyMarker();

        Assert.That(marker.Marker, Is.EqualTo(global::jpeg.JpegMarker.EndOfImage));
        Assert.That(marker.Offset, Is.EqualTo(3));
        Assert.That(marker.EntropyBytesSkipped, Is.EqualTo(1));
    }

    [Test]
    public void ReadNextMarker_RejectsStuffedEntropyByteAtMarkerBoundary()
    {
        byte[] data = new byte[] { 0xFF, 0xD8, 0xFF, 0x00 };
        global::jpeg.JpegMarkerScanner scanner = new global::jpeg.JpegMarkerScanner(data);

        scanner.ReadStartOfImage();

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                scanner.ReadNextMarker();
            });
    }
}
