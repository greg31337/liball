using System.Collections.Generic;
using System.IO;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class JpegMarkerScannerTests
{
    [Test]
    public void Scan_RecordsMarkerOffsetsPayloadOffsetsAndLengths()
    {
        byte[] bytes = new byte[]
        {
            255, 216,
            255, 224, 0, 4, 1, 2,
            255, 219, 0, 3, 153,
            255, 218, 0, 8, 3, 1, 0, 2, 17, 3,
            18, 52, 255, 0, 86, 255, 208, 120, 255, 217
        };
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        BoundedStreamReader reader = CreateReader(bytes, diagnostics);

        List<JpegMarkerSegment> segments = JpegMarkerScanner.Scan(reader, new ImageMetadataReaderOptions());

        Assert.That(diagnostics, Is.Empty);
        Assert.That(segments, Has.Count.EqualTo(6));
        AssertSegment(segments[0], 216, "SOI", 0, 2, false, -1, 0);
        AssertSegment(segments[1], 224, "APP0", 2, 6, true, 6, 2);
        AssertSegment(segments[2], 219, "DQT", 8, 5, true, 12, 1);
        AssertSegment(segments[3], 218, "SOS", 13, 10, true, 17, 6);
        AssertSegment(segments[4], 208, "RST0", 28, 2, false, -1, 0);
        AssertSegment(segments[5], 217, "EOI", 31, 2, false, -1, 0);
        Assert.That(segments[3].IsStartOfScan, Is.True);
        Assert.That(segments[4].IsRestartMarker, Is.True);
        Assert.That(segments[5].IsEndOfImage, Is.True);
    }

    [Test]
    public void Scan_AllowsAdditionalMarkersAfterScanDataBeforeEoi()
    {
        byte[] bytes = new byte[]
        {
            255, 216,
            255, 218, 0, 4, 1, 2,
            17, 34, 255, 196, 0, 3, 51,
            255, 217
        };
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        BoundedStreamReader reader = CreateReader(bytes, diagnostics);

        List<JpegMarkerSegment> segments = JpegMarkerScanner.Scan(reader, new ImageMetadataReaderOptions());

        Assert.That(diagnostics, Is.Empty);
        Assert.That(segments, Has.Count.EqualTo(4));
        Assert.That(segments[1].Name, Is.EqualTo("SOS"));
        Assert.That(segments[2].Name, Is.EqualTo("DHT"));
        Assert.That(segments[2].MarkerOffset, Is.EqualTo(10));
        Assert.That(segments[3].Name, Is.EqualTo("EOI"));
    }

    [Test]
    public void Scan_InvalidStartOfImageReturnsDiagnostic()
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        BoundedStreamReader reader = CreateReader(new byte[] { 1, 2, 3 }, diagnostics);

        List<JpegMarkerSegment> segments = JpegMarkerScanner.Scan(reader, new ImageMetadataReaderOptions());

        Assert.That(segments, Is.Empty);
        Assert.That(diagnostics, Has.Count.EqualTo(1));
        Assert.That(diagnostics[0].Code, Is.EqualTo("jpeg.soi.invalid"));
        Assert.That(diagnostics[0].Severity, Is.EqualTo(MetadataSeverity.RecoverableError));
    }

    [Test]
    public void Scan_TruncatedSegmentRecordsDeclaredLengthAndDiagnostic()
    {
        byte[] bytes = new byte[] { 255, 216, 255, 225, 0, 16, 1, 2 };
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        BoundedStreamReader reader = CreateReader(bytes, diagnostics);

        List<JpegMarkerSegment> segments = JpegMarkerScanner.Scan(reader, new ImageMetadataReaderOptions());

        Assert.That(segments, Has.Count.EqualTo(2));
        Assert.That(segments[1].Name, Is.EqualTo("APP1"));
        Assert.That(segments[1].DeclaredLength, Is.EqualTo(16));
        Assert.That(segments[1].PayloadOffset, Is.EqualTo(6));
        Assert.That(segments[1].PayloadLength, Is.EqualTo(14));
        Assert.That(segments[1].IsTruncated, Is.True);
        Assert.That(diagnostics[0].Code, Is.EqualTo("jpeg.segment.truncated"));
    }

    [Test]
    public void Scan_ReportsSegmentPayloadLimitAndContinues()
    {
        byte[] bytes = new byte[] { 255, 216, 255, 224, 0, 4, 1, 2, 255, 217 };
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        BoundedStreamReader reader = CreateReader(bytes, diagnostics);
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.MaxSegmentBytes = 1;

        List<JpegMarkerSegment> segments = JpegMarkerScanner.Scan(reader, options);

        Assert.That(segments, Has.Count.EqualTo(3));
        Assert.That(segments[1].Name, Is.EqualTo("APP0"));
        Assert.That(segments[2].Name, Is.EqualTo("EOI"));
        Assert.That(diagnostics, Has.Count.EqualTo(1));
        Assert.That(diagnostics[0].Code, Is.EqualTo("jpeg.segment.limit"));
        Assert.That(diagnostics[0].Severity, Is.EqualTo(MetadataSeverity.LimitExceeded));
    }

    internal static BoundedStreamReader CreateReader(byte[] bytes, List<MetadataDiagnostic> diagnostics)
    {
        MemoryStream stream = new MemoryStream(bytes);
        return new BoundedStreamReader(stream, stream.Length, stream.Length, diagnostics);
    }

    internal static void AssertSegment(JpegMarkerSegment segment, int marker, string name, long markerOffset, long segmentLength, bool hasLength, long payloadOffset, long payloadLength)
    {
        Assert.That(segment.Marker, Is.EqualTo(marker));
        Assert.That(segment.Name, Is.EqualTo(name));
        Assert.That(segment.MarkerOffset, Is.EqualTo(markerOffset));
        Assert.That(segment.SegmentLength, Is.EqualTo(segmentLength));
        Assert.That(segment.HasLength, Is.EqualTo(hasLength));
        Assert.That(segment.PayloadOffset, Is.EqualTo(payloadOffset));
        Assert.That(segment.PayloadLength, Is.EqualTo(payloadLength));
    }
}
