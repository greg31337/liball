namespace jpeg.Tests;

internal class ExifMetadataWriterTests
{
    [Test]
    public void Writer_PreservesParsedExifPayloadWhenFieldsAreUnchanged()
    {
        byte[] payload = ExifMetadataParserTests.CreateExifPayload(ExifTagReaderTests.CreateCommonExifTiff());
        global::jpeg.JpegExifMetadata metadata = new global::jpeg.JpegExifMetadataParser().Parse(payload);
        global::jpeg.JpegExifMetadataWriter writer = new global::jpeg.JpegExifMetadataWriter();

        byte[] written = writer.WritePayload(metadata);

        Assert.That(written, Is.EqualTo(payload));
    }

    [Test]
    public void Writer_ReplacesSelectedExifFieldsInExistingRawPayload()
    {
        byte[] payload = ExifMetadataParserTests.CreateExifPayload(ExifTagReaderTests.CreateCommonExifTiff());
        global::jpeg.JpegExifMetadata metadata = new global::jpeg.JpegExifMetadataParser().Parse(payload);
        metadata.Orientation = 3;
        metadata.Make = "NewMake";
        metadata.DateTimeOriginal = "2026:08:05 10:00:00";
        metadata.PixelWidth = 2048;
        global::jpeg.JpegExifMetadataWriter writer = new global::jpeg.JpegExifMetadataWriter();

        byte[] written = writer.WritePayload(metadata);
        global::jpeg.JpegExifMetadata reparsed = new global::jpeg.JpegExifMetadataParser().Parse(written);

        Assert.That(reparsed.Orientation, Is.EqualTo(3));
        Assert.That(reparsed.Make, Is.EqualTo("NewMake"));
        Assert.That(reparsed.Model, Is.EqualTo("Model X"));
        Assert.That(reparsed.DateTimeOriginal, Is.EqualTo("2026:08:05 10:00:00"));
        Assert.That(reparsed.PixelWidth, Is.EqualTo(2048));
        Assert.That(metadata.RawPayload, Is.EqualTo(payload));
    }

    [Test]
    public void Writer_WritesApp1SegmentWithSelectedExifReplacements()
    {
        byte[] payload = ExifMetadataParserTests.CreateExifPayload(ExifTagReaderTests.CreateCommonExifTiff());
        global::jpeg.JpegExifMetadata metadata = new global::jpeg.JpegExifMetadataParser().Parse(payload);
        metadata.Orientation = 8;
        global::jpeg.JpegExifMetadataWriter writer = new global::jpeg.JpegExifMetadataWriter();
        global::jpeg.JpegMarkerSegmentWriter segmentWriter = new global::jpeg.JpegMarkerSegmentWriter();

        writer.WriteMarkerSegment(segmentWriter, metadata);

        global::jpeg.JpegMarkerSegmentReader reader = new global::jpeg.JpegMarkerSegmentReader(segmentWriter.ToArray());
        global::jpeg.JpegMarkerSegment segment = reader.ReadNextSegment();
        global::jpeg.JpegExifMetadata reparsed = new global::jpeg.JpegExifMetadataParser().Parse(segment.Payload);
        Assert.That(segment.Marker, Is.EqualTo(global::jpeg.JpegMarker.App1));
        Assert.That(reparsed.Orientation, Is.EqualTo(8));
    }

    [Test]
    public void Writer_RejectsSelectedAsciiReplacementThatDoesNotFit()
    {
        byte[] payload = ExifMetadataParserTests.CreateExifPayload(ExifTagReaderTests.CreateCommonExifTiff());
        global::jpeg.JpegExifMetadata metadata = new global::jpeg.JpegExifMetadataParser().Parse(payload);
        metadata.Make = "This maker name is too long";
        global::jpeg.JpegExifMetadataWriter writer = new global::jpeg.JpegExifMetadataWriter();

        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                writer.WritePayload(metadata);
            });
    }

    [Test]
    public void Writer_RejectsSelectedScalarReplacementOutsideExistingTypeRange()
    {
        byte[] payload = ExifMetadataParserTests.CreateExifPayload(ExifTagReaderTests.CreateCommonExifTiff());
        global::jpeg.JpegExifMetadata metadata = new global::jpeg.JpegExifMetadataParser().Parse(payload);
        metadata.Orientation = 70000;
        global::jpeg.JpegExifMetadataWriter writer = new global::jpeg.JpegExifMetadataWriter();

        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                writer.WritePayload(metadata);
            });
    }

    [Test]
    public void Writer_UsesNullForMissingOptionalIfdEntry()
    {
        byte[] tiffData = ExifTagReaderTests.CreateCommonExifTiff();
        global::jpeg.JpegExifTiffHeader header = new global::jpeg.JpegExifTiffReader().ParseHeader(tiffData);
        global::jpeg.JpegExifMetadataWriter writer = new global::jpeg.JpegExifMetadataWriter();

        global::jpeg.JpegExifIfdEntry? missing = writer.FindEntry(tiffData, header.FirstIfdOffset, 0xFFFF, header.ByteOrder);

        Assert.That(missing, Is.Null);
    }
}
