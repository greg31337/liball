namespace jpeg.Tests;

internal class MetadataPolicyTests
{
    [Test]
    public void Identifier_PreserveKnownDropsUnrecognizedRawMarkers()
    {
        byte[] data = CreateMetadataHeavyJpeg();
        global::jpeg.JpegReadOptions options = new global::jpeg.JpegReadOptions();
        options.MetadataPolicy = global::jpeg.JpegMetadataPolicy.PreserveKnown;

        global::jpeg.JpegInfo info = global::jpeg.JpegIdentifier.Identify(data, options);

        Assert.That(info.Metadata.HasJfif, Is.True);
        Assert.That(info.Metadata.HasExif, Is.True);
        Assert.That(info.Metadata.HasXmp, Is.True);
        Assert.That(info.Metadata.HasMpf, Is.True);
        Assert.That(info.Metadata.HasPhotoshop, Is.True);
        Assert.That(info.Metadata.Comments, Is.EqualTo(new string[] { "note" }));
        Assert.That(ContainsRawMarker(info.Metadata, 0xFFF0), Is.False);
        Assert.That(ContainsRawMarker(info.Metadata, global::jpeg.JpegConstants.MarkerApp13), Is.True);
        Assert.That(ContainsRawMarker(info.Metadata, global::jpeg.JpegConstants.MarkerApp2), Is.True);
    }

    [Test]
    public void Identifier_StripUnsafeKeepsTechnicalMetadataOnly()
    {
        byte[] data = CreateMetadataHeavyJpeg();
        global::jpeg.JpegReadOptions options = new global::jpeg.JpegReadOptions();
        options.MetadataPolicy = global::jpeg.JpegMetadataPolicy.StripUnsafe;

        global::jpeg.JpegInfo info = global::jpeg.JpegIdentifier.Identify(data, options);

        Assert.That(info.Metadata.HasJfif, Is.True);
        Assert.That(info.Metadata.HasIccProfile, Is.True);
        Assert.That(info.Metadata.HasAdobe, Is.True);
        Assert.That(info.Metadata.HasExif, Is.False);
        Assert.That(info.Metadata.HasXmp, Is.False);
        Assert.That(info.Metadata.HasMpf, Is.False);
        Assert.That(info.Metadata.HasPhotoshop, Is.False);
        Assert.That(info.Metadata.Comments.Count, Is.EqualTo(0));
        Assert.That(info.Metadata.RawMarkers.Count, Is.EqualTo(3));
        Assert.That(info.MetadataBytes, Is.EqualTo(GetSafeMetadataPayloadBytes()));
    }

    [Test]
    public void Identifier_StripAllClearsMetadataButKeepsStructuralCounts()
    {
        byte[] data = CreateMetadataHeavyJpeg();
        global::jpeg.JpegReadOptions options = new global::jpeg.JpegReadOptions();
        options.MetadataPolicy = global::jpeg.JpegMetadataPolicy.StripAll;

        global::jpeg.JpegInfo info = global::jpeg.JpegIdentifier.Identify(data, options);

        Assert.That(info.Metadata.HasJfif, Is.False);
        Assert.That(info.Metadata.HasExif, Is.False);
        Assert.That(info.Metadata.RawMarkers.Count, Is.EqualTo(0));
        Assert.That(info.Metadata.Comments.Count, Is.EqualTo(0));
        Assert.That(info.MetadataBytes, Is.EqualTo(0));
        Assert.That(info.AppSegmentCount, Is.EqualTo(7));
        Assert.That(info.CommentCount, Is.EqualTo(1));
    }

    [Test]
    public void Identifier_SkipMetadataOverridesPreservePolicy()
    {
        byte[] data = CreateMetadataHeavyJpeg();
        global::jpeg.JpegReadOptions options = new global::jpeg.JpegReadOptions();
        options.MetadataPolicy = global::jpeg.JpegMetadataPolicy.PreserveAll;
        options.SkipMetadata = true;

        global::jpeg.JpegInfo info = global::jpeg.JpegIdentifier.Identify(data, options);

        Assert.That(info.Metadata.HasJfif, Is.False);
        Assert.That(info.Metadata.HasIccProfile, Is.False);
        Assert.That(info.Metadata.RawMarkers.Count, Is.EqualTo(0));
        Assert.That(info.MetadataBytes, Is.EqualTo(0));
    }

    [Test]
    public void Writer_PreserveKnownSkipsUnrecognizedRawMarkers()
    {
        byte[] jfifPayload = JfifMetadataParserTests.CreateJfifPayload(1, 2, 1, 72, 96, 0, 0);
        byte[] unknownPayload = new byte[] { 8, 9 };
        global::jpeg.JpegMetadata metadata = new global::jpeg.JpegMetadata();
        metadata.RawMarkers.Add(MetadataWriterOrderingTests.CreateRawMarker(global::jpeg.JpegMarker.Unknown, 0xFFF0, unknownPayload));
        metadata.RawMarkers.Add(MetadataWriterOrderingTests.CreateRawMarker(global::jpeg.JpegMarker.App0, global::jpeg.JpegConstants.MarkerApp0, jfifPayload));
        global::jpeg.JpegMarkerSegmentWriter segmentWriter = new global::jpeg.JpegMarkerSegmentWriter();
        global::jpeg.JpegMetadataWriter writer = new global::jpeg.JpegMetadataWriter();

        writer.WriteMarkerSegmentsWithPolicy(segmentWriter, metadata, global::jpeg.JpegMetadataPolicy.PreserveKnown);

        global::jpeg.JpegMarkerSegmentReader reader = new global::jpeg.JpegMarkerSegmentReader(segmentWriter.ToArray());
        global::jpeg.JpegMarkerSegment segment = reader.ReadNextSegment();
        Assert.That(segment.Value, Is.EqualTo(global::jpeg.JpegConstants.MarkerApp0));
        Assert.That(segment.Payload, Is.EqualTo(jfifPayload));
    }

    [Test]
    public void Writer_ReplaceSelectedUsesStructuredMetadataInsteadOfRawMarkers()
    {
        global::jpeg.JpegMetadata metadata = new global::jpeg.JpegMetadata();
        metadata.RawMarkers.Add(MetadataWriterOrderingTests.CreateRawMarker(global::jpeg.JpegMarker.App0, global::jpeg.JpegConstants.MarkerApp0, JfifMetadataParserTests.CreateJfifPayload(1, 2, 1, 72, 96, 0, 0)));
        metadata.Comments.Add("replacement");
        global::jpeg.JpegMarkerSegmentWriter segmentWriter = new global::jpeg.JpegMarkerSegmentWriter();
        global::jpeg.JpegMetadataWriter writer = new global::jpeg.JpegMetadataWriter();

        writer.WriteMarkerSegmentsWithPolicy(segmentWriter, metadata, global::jpeg.JpegMetadataPolicy.ReplaceSelected);

        global::jpeg.JpegMarkerSegmentReader reader = new global::jpeg.JpegMarkerSegmentReader(segmentWriter.ToArray());
        global::jpeg.JpegMarkerSegment segment = reader.ReadNextSegment();
        Assert.That(segment.Value, Is.EqualTo(global::jpeg.JpegConstants.MarkerComment));
        Assert.That(segment.Payload, Is.EqualTo(System.Text.Encoding.Latin1.GetBytes("replacement")));
    }

    public static byte[] CreateMetadataHeavyJpeg()
    {
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.App0, JfifMetadataParserTests.CreateJfifPayload(1, 2, 1, 72, 96, 0, 0));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.App1, ExifMetadataParserTests.CreateExifPayload(new byte[] { 0x4D, 0x4D, 0, 0x2A, 0, 0, 0, 8 }));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.App1, XmpMetadataParserTests.CreateXmpPayload(System.Text.Encoding.UTF8.GetBytes("<x />")));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.Comment, System.Text.Encoding.Latin1.GetBytes("note"));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.App2, MpfMetadataParserTests.CreateMpfPayload(new byte[] { 1, 2, 3, 4 }));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.App13, PhotoshopMetadataParserTests.CreatePhotoshopPayload(new byte[] { 1, 2, 3 }));
        writer.WriteMarkerSegmentValue(0xFFF0, new byte[] { 7, 7 });
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.App2, IccProfileChunkParserTests.CreateIccPayload(1, 1, new byte[] { 9, 8, 7 }));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.App14, AdobeMetadataParserTests.CreateAdobePayload(100, 1, 2, global::jpeg.JpegConstants.AdobeTransformYCbCr));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfFrame0, IdentifierTests.CreateSofPayload(global::jpeg.JpegConstants.MarkerStartOfFrame0, 3));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfScan, IdentifierTests.CreateSosPayload(3));
        writer.WriteEndOfImage();
        return writer.ToArray();
    }

    public static long GetSafeMetadataPayloadBytes()
    {
        long total = 0;
        total = total + JfifMetadataParserTests.CreateJfifPayload(1, 2, 1, 72, 96, 0, 0).Length;
        total = total + IccProfileChunkParserTests.CreateIccPayload(1, 1, new byte[] { 9, 8, 7 }).Length;
        total = total + AdobeMetadataParserTests.CreateAdobePayload(100, 1, 2, global::jpeg.JpegConstants.AdobeTransformYCbCr).Length;
        return total;
    }

    public static bool ContainsRawMarker(global::jpeg.JpegMetadata metadata, int value)
    {
        foreach (global::jpeg.JpegRawMarker marker in metadata.RawMarkers)
        {
            if (marker.Value == value)
            {
                return true;
            }
        }

        return false;
    }
}
