namespace jpeg.Tests;

internal class ScanHeaderWriterTests
{
    [Test]
    public void Writer_WritesScanPayloadThatParsesBack()
    {
        global::jpeg.JpegScanHeader scan = CreateBaselineScan();
        global::jpeg.JpegScanHeaderWriter writer = new global::jpeg.JpegScanHeaderWriter();

        byte[] payload = writer.WritePayload(scan);
        global::jpeg.JpegScanHeader parsed = new global::jpeg.JpegScanHeaderParser().Parse(payload);

        Assert.That(payload, Is.EqualTo(IdentifierTests.CreateSosPayload(3)));
        Assert.That(parsed.Components.Count, Is.EqualTo(3));
        Assert.That(parsed.Components[1].Selector, Is.EqualTo(2));
        Assert.That(parsed.Components[1].DcEntropyTableIdentifier, Is.EqualTo(1));
        Assert.That(parsed.SpectralStart, Is.EqualTo(0));
        Assert.That(parsed.SpectralEnd, Is.EqualTo(63));
    }

    [Test]
    public void Writer_WritesScanMarkerSegment()
    {
        global::jpeg.JpegScanHeader scan = CreateBaselineScan();
        global::jpeg.JpegMarkerSegmentWriter segmentWriter = new global::jpeg.JpegMarkerSegmentWriter();
        global::jpeg.JpegScanHeaderWriter writer = new global::jpeg.JpegScanHeaderWriter();

        writer.WriteMarkerSegment(segmentWriter, scan);

        global::jpeg.JpegMarkerSegmentReader reader = new global::jpeg.JpegMarkerSegmentReader(segmentWriter.ToArray());
        global::jpeg.JpegMarkerSegment segment = reader.ReadNextSegment();
        Assert.That(segment.Marker, Is.EqualTo(global::jpeg.JpegMarker.StartOfScan));
        Assert.That(segment.Payload, Is.EqualTo(IdentifierTests.CreateSosPayload(3)));
    }

    [Test]
    public void Writer_WritesSingleComponentProgressiveScan()
    {
        global::jpeg.JpegScanHeader scan = new global::jpeg.JpegScanHeader();
        scan.Components.Add(CreateComponent(1, 0, 0));
        scan.SpectralStart = 1;
        scan.SpectralEnd = 5;
        scan.SuccessiveHigh = 2;
        scan.SuccessiveLow = 1;
        global::jpeg.JpegScanHeaderWriter writer = new global::jpeg.JpegScanHeaderWriter();

        byte[] payload = writer.WritePayload(scan);

        Assert.That(payload, Is.EqualTo(new byte[] { 1, 1, 0, 1, 5, 0x21 }));
    }

    [Test]
    public void Writer_RejectsInvalidScanHeaders()
    {
        global::jpeg.JpegScanHeaderWriter writer = new global::jpeg.JpegScanHeaderWriter();

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                writer.WritePayload(null!);
            });

        global::jpeg.JpegScanHeader empty = new global::jpeg.JpegScanHeader();
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                writer.WritePayload(empty);
            });

        global::jpeg.JpegScanHeader mismatch = CreateBaselineScan();
        mismatch.ComponentCount = 2;
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                writer.WritePayload(mismatch);
            });

        global::jpeg.JpegScanHeader badSelector = CreateBaselineScan();
        badSelector.Components[0].Selector = 256;
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                writer.WritePayload(badSelector);
            });

        global::jpeg.JpegScanHeader duplicate = CreateBaselineScan();
        duplicate.Components[1].Selector = duplicate.Components[0].Selector;
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                writer.WritePayload(duplicate);
            });

        global::jpeg.JpegScanHeader badDc = CreateBaselineScan();
        badDc.Components[0].DcEntropyTableIdentifier = 4;
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                writer.WritePayload(badDc);
            });

        global::jpeg.JpegScanHeader badAc = CreateBaselineScan();
        badAc.Components[0].AcEntropyTableIdentifier = 4;
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                writer.WritePayload(badAc);
            });

        global::jpeg.JpegScanHeader badSpectral = CreateBaselineScan();
        badSpectral.SpectralStart = 64;
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                writer.WritePayload(badSpectral);
            });

        global::jpeg.JpegScanHeader badOrder = CreateBaselineScan();
        badOrder.SpectralStart = 10;
        badOrder.SpectralEnd = 9;
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                writer.WritePayload(badOrder);
            });

        global::jpeg.JpegScanHeader badSuccessive = CreateBaselineScan();
        badSuccessive.SuccessiveHigh = 16;
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                writer.WritePayload(badSuccessive);
            });
    }

    public static global::jpeg.JpegScanHeader CreateBaselineScan()
    {
        global::jpeg.JpegScanHeader scan = new global::jpeg.JpegScanHeader();
        scan.ComponentCount = 3;
        scan.Components.Add(CreateComponent(1, 0, 0));
        scan.Components.Add(CreateComponent(2, 1, 1));
        scan.Components.Add(CreateComponent(3, 1, 1));
        scan.SpectralStart = 0;
        scan.SpectralEnd = 63;
        scan.SuccessiveHigh = 0;
        scan.SuccessiveLow = 0;
        return scan;
    }

    public static global::jpeg.JpegScanComponent CreateComponent(int selector, int dcTable, int acTable)
    {
        global::jpeg.JpegScanComponent component = new global::jpeg.JpegScanComponent();
        component.Selector = selector;
        component.DcEntropyTableIdentifier = dcTable;
        component.AcEntropyTableIdentifier = acTable;
        return component;
    }
}
