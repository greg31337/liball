namespace jpeg.Tests;

internal class JpegIdentifyApiTests
{
    [Test]
    public void Identify_ReadsByteArray()
    {
        byte[] data = IdentifierTests.CreateMinimalBaselineJpeg();

        global::jpeg.JpegInfo info = global::jpeg.Jpeg.Identify(data);

        Assert.That(info.Width, Is.EqualTo(32));
        Assert.That(info.Height, Is.EqualTo(16));
        Assert.That(info.Process, Is.EqualTo(global::jpeg.JpegProcess.BaselineDct));
        Assert.That(info.SawEndOfImage, Is.True);
    }

    [Test]
    public void Identify_ReadsByteArrayWithOptions()
    {
        byte[] data = CreateJfifBaselineJpeg();
        global::jpeg.JpegReadOptions options = new global::jpeg.JpegReadOptions();
        options.SkipMetadata = true;

        global::jpeg.JpegInfo info = global::jpeg.Jpeg.Identify(data, options);

        Assert.That(info.AppSegmentCount, Is.EqualTo(1));
        Assert.That(info.MetadataBytes, Is.EqualTo(0));
        Assert.That(info.Metadata.HasJfif, Is.False);
        Assert.That(info.Width, Is.EqualTo(32));
    }

    [Test]
    public void Identify_ReadsStream()
    {
        byte[] data = IdentifierTests.CreateMinimalBaselineJpeg();
        System.IO.MemoryStream stream = new System.IO.MemoryStream(data);

        global::jpeg.JpegInfo info = global::jpeg.Jpeg.Identify(stream);

        Assert.That(info.Width, Is.EqualTo(32));
        Assert.That(info.Height, Is.EqualTo(16));
        Assert.That(info.SawStartOfScan, Is.True);
    }

    [Test]
    public void Identify_ReadsStreamWithOptions()
    {
        byte[] data = CreateJfifBaselineJpeg();
        System.IO.MemoryStream stream = new System.IO.MemoryStream(data);
        global::jpeg.JpegReadOptions options = new global::jpeg.JpegReadOptions();
        options.MetadataPolicy = global::jpeg.JpegMetadataPolicy.StripAll;

        global::jpeg.JpegInfo info = global::jpeg.Jpeg.Identify(stream, options);

        Assert.That(info.AppSegmentCount, Is.EqualTo(1));
        Assert.That(info.MetadataBytes, Is.EqualTo(0));
        Assert.That(info.Metadata.RawMarkers.Count, Is.EqualTo(0));
        Assert.That(info.SawEndOfImage, Is.True);
    }

    [Test]
    public void Identify_RejectsNullOptions()
    {
        byte[] data = IdentifierTests.CreateMinimalBaselineJpeg();
        System.IO.MemoryStream stream = new System.IO.MemoryStream(data);

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                global::jpeg.Jpeg.Identify(data, null!);
            });
        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                global::jpeg.Jpeg.Identify(stream, null!);
            });
    }

    public static byte[] CreateJfifBaselineJpeg()
    {
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.App0, JfifMetadataParserTests.CreateJfifPayload(1, 2, 1, 72, 72, 0, 0));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfFrame0, IdentifierTests.CreateSofPayload(global::jpeg.JpegConstants.MarkerStartOfFrame0, 3));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfScan, IdentifierTests.CreateSosPayload(3));
        writer.WriteEndOfImage();
        return writer.ToArray();
    }
}
