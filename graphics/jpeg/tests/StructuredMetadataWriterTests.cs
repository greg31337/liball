namespace jpeg.Tests;

internal class StructuredMetadataWriterTests
{
    [Test]
    public void Writer_EmitsStructuredMpfAndPhotoshopInCanonicalOrder()
    {
        byte[] mpfPayload = MpfMetadataParserTests.CreateMpfPayload(new byte[] { 0x49, 0x49, 42, 0, 8, 0, 0, 0 });
        byte[] iptc = new byte[] { 0x1C, 2, 5, 0, 3, 65, 66, 67 };
        byte[] photoshopPayload = PhotoshopMetadataParserTests.CreatePhotoshopPayload(iptc);
        global::jpeg.JpegMetadata metadata = CreateStructuredMetadata(mpfPayload, photoshopPayload);
        global::jpeg.JpegMarkerSegmentWriter destination = new global::jpeg.JpegMarkerSegmentWriter();

        new global::jpeg.JpegMetadataWriter().WriteMarkerSegments(destination, metadata);

        System.Collections.Generic.List<global::jpeg.JpegMarkerSegment> segments =
            MetadataWriterOrderingTests.ReadSegments(destination.ToArray(), 2);
        MetadataWriterOrderingTests.AssertSegment(segments[0], global::jpeg.JpegConstants.MarkerApp2, mpfPayload);
        MetadataWriterOrderingTests.AssertSegment(segments[1], global::jpeg.JpegConstants.MarkerApp13, photoshopPayload);
    }

    [Test]
    public void PublicWriter_RoundTripsStructuredMpfPhotoshopAndIptcByteForByte()
    {
        byte[] mpfPayload = MpfMetadataParserTests.CreateMpfPayload(new byte[] { 0x4D, 0x4D, 0, 42, 0, 0, 0, 8 });
        byte[] iptc = new byte[] { 0x1C, 2, 120, 0, 4, 9, 8, 7, 6 };
        byte[] photoshopPayload = PhotoshopMetadataParserTests.CreatePhotoshopPayload(iptc);
        global::jpeg.JpegImage source = JpegWriteApiTests.CreateGrayscaleImage(8, 8, 138);
        source.Metadata = CreateStructuredMetadata(mpfPayload, photoshopPayload);
        global::jpeg.JpegWriter writer = new global::jpeg.JpegWriter();

        byte[] first = writer.Write(source);
        global::jpeg.JpegInfo firstInfo = global::jpeg.Jpeg.Identify(first);
        global::jpeg.JpegImage decoded = global::jpeg.Jpeg.Read(first);
        byte[] second = writer.Write(decoded);
        global::jpeg.JpegInfo secondInfo = global::jpeg.Jpeg.Identify(second);

        Assert.That(firstInfo.Metadata.HasMpf, Is.True);
        Assert.That(firstInfo.Metadata.MpfMetadata.RawPayload, Is.EqualTo(mpfPayload));
        Assert.That(firstInfo.Metadata.HasPhotoshop, Is.True);
        Assert.That(firstInfo.Metadata.Photoshop.RawPayload, Is.EqualTo(photoshopPayload));
        Assert.That(firstInfo.Metadata.HasIptc, Is.True);
        Assert.That(firstInfo.Metadata.Iptc, Is.EqualTo(iptc));
        AssertRawMetadataEqual(firstInfo.Metadata, secondInfo.Metadata);
    }

    [TestCase(global::jpeg.JpegMetadataPolicy.PreserveAll, 2)]
    [TestCase(global::jpeg.JpegMetadataPolicy.PreserveKnown, 2)]
    [TestCase(global::jpeg.JpegMetadataPolicy.ReplaceSelected, 2)]
    [TestCase(global::jpeg.JpegMetadataPolicy.StripUnsafe, 0)]
    [TestCase(global::jpeg.JpegMetadataPolicy.StripAll, 0)]
    public void Writer_AppliesMetadataPoliciesToStructuredMpfAndPhotoshop(
        global::jpeg.JpegMetadataPolicy policy,
        int expectedSegmentCount)
    {
        byte[] mpfPayload = MpfMetadataParserTests.CreateMpfPayload(new byte[] { 1, 2, 3, 4 });
        byte[] photoshopPayload = PhotoshopMetadataParserTests.CreatePhotoshopPayload(new byte[] { 5, 6, 7 });
        global::jpeg.JpegMetadata metadata = CreateStructuredMetadata(mpfPayload, photoshopPayload);
        global::jpeg.JpegMarkerSegmentWriter destination = new global::jpeg.JpegMarkerSegmentWriter();

        new global::jpeg.JpegMetadataWriter().WriteMarkerSegmentsWithPolicy(destination, metadata, policy);

        if (expectedSegmentCount == 0)
        {
            Assert.That(destination.ToArray(), Is.Empty);
            return;
        }

        System.Collections.Generic.List<global::jpeg.JpegMarkerSegment> segments =
            MetadataWriterOrderingTests.ReadSegments(destination.ToArray(), expectedSegmentCount);
        MetadataWriterOrderingTests.AssertSegment(segments[0], global::jpeg.JpegConstants.MarkerApp2, mpfPayload);
        MetadataWriterOrderingTests.AssertSegment(segments[1], global::jpeg.JpegConstants.MarkerApp13, photoshopPayload);
    }

    internal static global::jpeg.JpegMetadata CreateStructuredMetadata(byte[] mpfPayload, byte[] photoshopPayload)
    {
        global::jpeg.JpegMetadata metadata = new global::jpeg.JpegMetadata();
        metadata.HasMpf = true;
        metadata.MpfMetadata = new global::jpeg.JpegMpfMetadataParser().Parse(mpfPayload);
        metadata.Mpf = CopyBytes(mpfPayload);
        metadata.HasPhotoshop = true;
        metadata.Photoshop = new global::jpeg.JpegPhotoshopMetadataParser().Parse(photoshopPayload);
        metadata.HasIptc = metadata.Photoshop.HasIptc;
        metadata.Iptc = CopyBytes(metadata.Photoshop.IptcData);
        return metadata;
    }

    internal static void AssertRawMetadataEqual(global::jpeg.JpegMetadata expected, global::jpeg.JpegMetadata actual)
    {
        Assert.That(actual.RawMarkers.Count, Is.EqualTo(expected.RawMarkers.Count));
        int index = 0;
        while (index < expected.RawMarkers.Count)
        {
            Assert.That(actual.RawMarkers[index].Value, Is.EqualTo(expected.RawMarkers[index].Value));
            Assert.That(actual.RawMarkers[index].Payload, Is.EqualTo(expected.RawMarkers[index].Payload));
            index = index + 1;
        }
    }

    internal static byte[] CopyBytes(byte[] source)
    {
        byte[] result = new byte[source.Length];
        System.Array.Copy(source, 0, result, 0, source.Length);
        return result;
    }
}
