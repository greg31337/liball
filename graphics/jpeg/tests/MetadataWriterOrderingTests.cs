using System.Collections.Generic;

namespace jpeg.Tests;

internal class MetadataWriterOrderingTests
{
    [Test]
    public void Writer_PreservesCallerRawMetadataMarkerOrder()
    {
        byte[] commentPayload = System.Text.Encoding.Latin1.GetBytes("note");
        byte[] unknownPayload = new byte[] { 0x55 };
        byte[] photoshopPayload = PhotoshopMetadataParserTests.CreatePhotoshopPayload(new byte[] { 1, 2, 3 });
        byte[] mpfPayload = MpfMetadataParserTests.CreateMpfPayload(new byte[] { 4, 5 });
        byte[] adobePayload = AdobeMetadataParserTests.CreateAdobePayload(100, 1, 2, global::jpeg.JpegConstants.AdobeTransformYCbCr);
        byte[] xmpPayload = XmpMetadataParserTests.CreateXmpPayload(System.Text.Encoding.UTF8.GetBytes("<x />"));
        byte[] exifPayload = ExifMetadataParserTests.CreateExifPayload(new byte[] { 0x4D, 0x4D, 0, 0x2A, 0, 0, 0, 8 });
        byte[] iccPayload = IccProfileChunkParserTests.CreateIccPayload(1, 1, new byte[] { 9, 8, 7 });
        byte[] jfxxPayload = JfxxMetadataParserTests.CreateJfxxPayload(global::jpeg.JpegConstants.JfxxExtensionRgbThumbnail, new byte[] { 1, 1, 10, 20, 30 });
        byte[] jfifPayload = JfifMetadataParserTests.CreateJfifPayload(1, 2, 1, 72, 96, 0, 0);

        global::jpeg.JpegMetadata metadata = new global::jpeg.JpegMetadata();
        metadata.RawMarkers.Add(CreateRawMarker(global::jpeg.JpegMarker.Comment, global::jpeg.JpegConstants.MarkerComment, commentPayload));
        metadata.RawMarkers.Add(CreateRawMarker(global::jpeg.JpegMarker.App13, global::jpeg.JpegConstants.MarkerApp13, photoshopPayload));
        metadata.RawMarkers.Add(CreateRawMarker(global::jpeg.JpegMarker.App14, global::jpeg.JpegConstants.MarkerApp14, adobePayload));
        metadata.RawMarkers.Add(CreateRawMarker(global::jpeg.JpegMarker.App2, global::jpeg.JpegConstants.MarkerApp2, mpfPayload));
        metadata.RawMarkers.Add(CreateRawMarker(global::jpeg.JpegMarker.App1, global::jpeg.JpegConstants.MarkerApp1, xmpPayload));
        metadata.RawMarkers.Add(CreateRawMarker(global::jpeg.JpegMarker.App1, global::jpeg.JpegConstants.MarkerApp1, exifPayload));
        metadata.RawMarkers.Add(CreateRawMarker(global::jpeg.JpegMarker.App2, global::jpeg.JpegConstants.MarkerApp2, iccPayload));
        metadata.RawMarkers.Add(CreateRawMarker(global::jpeg.JpegMarker.Unknown, 0xFFF0, unknownPayload));
        metadata.RawMarkers.Add(CreateRawMarker(global::jpeg.JpegMarker.App0, global::jpeg.JpegConstants.MarkerApp0, jfxxPayload));
        metadata.RawMarkers.Add(CreateRawMarker(global::jpeg.JpegMarker.App0, global::jpeg.JpegConstants.MarkerApp0, jfifPayload));
        global::jpeg.JpegMarkerSegmentWriter segmentWriter = new global::jpeg.JpegMarkerSegmentWriter();
        global::jpeg.JpegMetadataWriter writer = new global::jpeg.JpegMetadataWriter();

        writer.WriteMarkerSegments(segmentWriter, metadata);

        List<global::jpeg.JpegMarkerSegment> segments = ReadSegments(segmentWriter.ToArray(), 10);
        AssertSegment(segments[0], global::jpeg.JpegConstants.MarkerComment, commentPayload);
        AssertSegment(segments[1], global::jpeg.JpegConstants.MarkerApp13, photoshopPayload);
        AssertSegment(segments[2], global::jpeg.JpegConstants.MarkerApp14, adobePayload);
        AssertSegment(segments[3], global::jpeg.JpegConstants.MarkerApp2, mpfPayload);
        AssertSegment(segments[4], global::jpeg.JpegConstants.MarkerApp1, xmpPayload);
        AssertSegment(segments[5], global::jpeg.JpegConstants.MarkerApp1, exifPayload);
        AssertSegment(segments[6], global::jpeg.JpegConstants.MarkerApp2, iccPayload);
        AssertSegment(segments[7], 0xFFF0, unknownPayload);
        AssertSegment(segments[8], global::jpeg.JpegConstants.MarkerApp0, jfxxPayload);
        AssertSegment(segments[9], global::jpeg.JpegConstants.MarkerApp0, jfifPayload);
    }

    [Test]
    public void Writer_OrdersStructuredMetadataByPolicy()
    {
        global::jpeg.JpegMetadata metadata = new global::jpeg.JpegMetadata();
        metadata.HasAdobe = true;
        metadata.Adobe = AdobeMetadataWriterTests.CreateAdobeMetadata();
        metadata.Comments.Add("first");
        metadata.HasXmp = true;
        metadata.XmpMetadata = new global::jpeg.JpegXmpMetadataParser().Parse(XmpMetadataParserTests.CreateXmpPayload(new byte[] { 1, 2 }));
        metadata.HasExif = true;
        metadata.ExifMetadata = new global::jpeg.JpegExifMetadataParser().Parse(ExifMetadataParserTests.CreateExifPayload(new byte[] { 0x4D, 0x4D, 0, 0x2A, 0, 0, 0, 8 }));
        metadata.HasIccProfileChunks = true;
        metadata.IccProfileChunks.Add(CreateIccChunk(1, 1, new byte[] { 9, 8 }));
        metadata.HasJfxx = true;
        metadata.Jfxx.Add(JfxxMetadataWriterTests.CreateRgbJfxxMetadata());
        metadata.HasJfif = true;
        metadata.Jfif = JfifMetadataWriterTests.CreateJfifMetadata();
        metadata.Segments.Add(CreateMetadataSegment(global::jpeg.JpegMarker.App12, new byte[] { 7, 6 }));
        global::jpeg.JpegMarkerSegmentWriter segmentWriter = new global::jpeg.JpegMarkerSegmentWriter();
        global::jpeg.JpegMetadataWriter writer = new global::jpeg.JpegMetadataWriter();

        writer.WriteMarkerSegments(segmentWriter, metadata);

        List<global::jpeg.JpegMarkerSegment> segments = ReadSegments(segmentWriter.ToArray(), 8);
        Assert.That(segments[0].Value, Is.EqualTo(global::jpeg.JpegConstants.MarkerApp0));
        Assert.That(new global::jpeg.JpegJfifMetadataParser().IsJfif(segments[0].Payload), Is.True);
        Assert.That(segments[1].Value, Is.EqualTo(global::jpeg.JpegConstants.MarkerApp0));
        Assert.That(new global::jpeg.JpegJfxxMetadataParser().IsJfxx(segments[1].Payload), Is.True);
        Assert.That(segments[2].Value, Is.EqualTo(global::jpeg.JpegConstants.MarkerApp2));
        Assert.That(new global::jpeg.JpegIccProfileChunkParser().IsIccProfile(segments[2].Payload), Is.True);
        Assert.That(segments[3].Value, Is.EqualTo(global::jpeg.JpegConstants.MarkerApp1));
        Assert.That(new global::jpeg.JpegExifMetadataParser().IsExif(segments[3].Payload), Is.True);
        Assert.That(segments[4].Value, Is.EqualTo(global::jpeg.JpegConstants.MarkerApp1));
        Assert.That(new global::jpeg.JpegXmpMetadataParser().IsXmp(segments[4].Payload), Is.True);
        Assert.That(segments[5].Value, Is.EqualTo(global::jpeg.JpegConstants.MarkerApp14));
        Assert.That(segments[6].Value, Is.EqualTo(global::jpeg.JpegConstants.MarkerComment));
        Assert.That(segments[7].Value, Is.EqualTo(global::jpeg.JpegConstants.MarkerApp12));
    }

    [Test]
    public void Writer_ClassifiesExtendedXmpWithStandardXmpCategory()
    {
        global::jpeg.JpegMetadataWriter writer = new global::jpeg.JpegMetadataWriter();
        byte[] payload = ExtendedXmpTests.CreateExtendedXmpPayload(ExtendedXmpTests.Guid, 2, 0, new byte[] { 1, 2 });

        int category = writer.GetPayloadCategory(global::jpeg.JpegConstants.MarkerApp1, payload);

        Assert.That(category, Is.EqualTo(global::jpeg.JpegMetadataWriter.CategoryXmp));
    }

    [Test]
    public void Writer_RejectsNullInputs()
    {
        global::jpeg.JpegMetadataWriter writer = new global::jpeg.JpegMetadataWriter();

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                writer.WriteMarkerSegments(null!, new global::jpeg.JpegMetadata());
            });

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                writer.WriteMarkerSegments(new global::jpeg.JpegMarkerSegmentWriter(), null!);
            });

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                writer.WriteRawMarkerSegments(new global::jpeg.JpegMarkerSegmentWriter(), null!);
            });
    }

    public static global::jpeg.JpegRawMarker CreateRawMarker(global::jpeg.JpegMarker marker, int value, byte[] payload)
    {
        global::jpeg.JpegRawMarker raw = new global::jpeg.JpegRawMarker();
        raw.Marker = marker;
        raw.Value = value;
        raw.Code = value & 255;
        raw.Payload = payload;
        raw.PayloadLength = payload.Length;
        raw.IsApplicationMarker = value >= global::jpeg.JpegConstants.MarkerApp0 && value <= global::jpeg.JpegConstants.MarkerApp15;
        raw.IsComment = value == global::jpeg.JpegConstants.MarkerComment;
        raw.IsUnknownMarker = value == 0xFFF0;
        return raw;
    }

    public static global::jpeg.JpegIccProfileChunk CreateIccChunk(int sequenceNumber, int chunkCount, byte[] data)
    {
        global::jpeg.JpegIccProfileChunk chunk = new global::jpeg.JpegIccProfileChunk();
        chunk.IsPresent = true;
        chunk.SequenceNumber = sequenceNumber;
        chunk.ChunkCount = chunkCount;
        chunk.Data = data;
        return chunk;
    }

    public static global::jpeg.JpegMetadataSegment CreateMetadataSegment(global::jpeg.JpegMarker marker, byte[] data)
    {
        global::jpeg.JpegMetadataSegment segment = new global::jpeg.JpegMetadataSegment();
        segment.Marker = marker;
        segment.Data = data;
        return segment;
    }

    public static List<global::jpeg.JpegMarkerSegment> ReadSegments(byte[] data, int count)
    {
        global::jpeg.JpegMarkerSegmentReader reader = new global::jpeg.JpegMarkerSegmentReader(data);
        List<global::jpeg.JpegMarkerSegment> segments = new List<global::jpeg.JpegMarkerSegment>();
        int index = 0;
        while (index < count)
        {
            segments.Add(reader.ReadNextSegment());
            index = index + 1;
        }

        return segments;
    }

    public static void AssertSegment(global::jpeg.JpegMarkerSegment segment, int markerValue, byte[] payload)
    {
        Assert.That(segment.Value, Is.EqualTo(markerValue));
        Assert.That(segment.Payload, Is.EqualTo(payload));
    }
}
