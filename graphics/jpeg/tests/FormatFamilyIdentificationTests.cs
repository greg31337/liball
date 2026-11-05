namespace jpeg.Tests;

internal class FormatFamilyIdentificationTests
{
    [Test]
    public void Identify_ReportsClassicJpegAndSupportedProcess()
    {
        global::jpeg.JpegIdentifierService service = new global::jpeg.JpegIdentifierService();

        global::jpeg.JpegInfo info = service.Identify(IdentifierTests.CreateMinimalBaselineJpeg());

        Assert.That(info.FormatFamily, Is.EqualTo(global::jpeg.JpegFormatFamily.ClassicJpeg));
        Assert.That(info.FormatSupport, Is.EqualTo(global::jpeg.JpegSupportStatus.Supported));
        Assert.That(info.CodingProcessSupport, Is.EqualTo(global::jpeg.JpegSupportStatus.Supported));
        Assert.That(info.Process, Is.EqualTo(global::jpeg.JpegProcess.BaselineDct));
    }

    [Test]
    public void Identify_ReportsJpegLsAsRecognizedButUnsupported()
    {
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.JpegLs, IdentifierTests.CreateSofPayload(global::jpeg.JpegConstants.MarkerJpegLs, 1));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfScan, JpegLsModeTests.CreateJpegLsLikeSosPayload());
        writer.WriteEndOfImage();

        global::jpeg.JpegInfo info = new global::jpeg.JpegIdentifierService().Identify(writer.ToArray());

        Assert.That(info.FormatFamily, Is.EqualTo(global::jpeg.JpegFormatFamily.JpegLs));
        Assert.That(info.FormatSupport, Is.EqualTo(global::jpeg.JpegSupportStatus.RecognizedButUnsupported));
        Assert.That(info.CodingProcessSupport, Is.EqualTo(global::jpeg.JpegSupportStatus.RecognizedButUnsupported));
        Assert.That(info.Process, Is.EqualTo(global::jpeg.JpegProcess.JpegLs));
    }

    [Test]
    public void Identify_ReportsUnsupportedClassicCodingProcessSeparatelyFromFormatSupport()
    {
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfFrame5, IdentifierTests.CreateSofPayload(global::jpeg.JpegConstants.MarkerStartOfFrame5, 1));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfScan, IdentifierTests.CreateSosPayload(1));
        writer.WriteEndOfImage();

        global::jpeg.JpegInfo info = new global::jpeg.JpegIdentifierService().Identify(writer.ToArray());

        Assert.That(info.FormatFamily, Is.EqualTo(global::jpeg.JpegFormatFamily.ClassicJpeg));
        Assert.That(info.FormatSupport, Is.EqualTo(global::jpeg.JpegSupportStatus.Supported));
        Assert.That(info.CodingProcessSupport, Is.EqualTo(global::jpeg.JpegSupportStatus.RecognizedButUnsupported));
        Assert.That(info.Process, Is.EqualTo(global::jpeg.JpegProcess.DifferentialSequentialDct));
    }

    [Test]
    public void Identify_DistinguishesUnsupportedRelatedFormatsAndUnknownInput()
    {
        AssertUnsupportedFamily(
            new byte[] { 0x00, 0x00, 0x00, 0x0C, 0x6A, 0x50, 0x20, 0x20, 0x0D, 0x0A, 0x87, 0x0A },
            global::jpeg.JpegFormatFamily.Jpeg2000);
        AssertUnsupportedFamily(
            new byte[] { 0x49, 0x49, 0xBC, 0x01 },
            global::jpeg.JpegFormatFamily.JpegXr);
        AssertUnsupportedFamily(
            new byte[] { 0xFF, 0x0A },
            global::jpeg.JpegFormatFamily.JpegXl);
        AssertUnsupportedFamily(
            new byte[] { 0x00, 0x00, 0x00, 0x18, 0x66, 0x74, 0x79, 0x70, 0x68, 0x65, 0x69, 0x63 },
            global::jpeg.JpegFormatFamily.Heif);

        global::jpeg.JpegInfo unknown = new global::jpeg.JpegIdentifierService().Identify(new byte[] { 0x89, 0x50, 0x4E, 0x47 });
        Assert.That(unknown.FormatFamily, Is.EqualTo(global::jpeg.JpegFormatFamily.Unknown));
        Assert.That(unknown.FormatSupport, Is.EqualTo(global::jpeg.JpegSupportStatus.Unknown));
        Assert.That(unknown.CodingProcessSupport, Is.EqualTo(global::jpeg.JpegSupportStatus.Unknown));
    }

    [Test]
    public void IdentifyStream_PreservesPeekedBytesForNonSeekableClassicInput()
    {
        NonSeekableReadStream stream = new NonSeekableReadStream(IdentifierTests.CreateMinimalBaselineJpeg());

        global::jpeg.JpegInfo info = new global::jpeg.JpegIdentifierService().IdentifyStream(stream);

        Assert.That(info.FormatFamily, Is.EqualTo(global::jpeg.JpegFormatFamily.ClassicJpeg));
        Assert.That(info.Width, Is.EqualTo(32));
        Assert.That(info.Height, Is.EqualTo(16));
        Assert.That(info.SawEndOfImage, Is.True);
        Assert.That(stream.CanSeek, Is.False);
    }

    internal void AssertUnsupportedFamily(byte[] data, global::jpeg.JpegFormatFamily expected)
    {
        global::jpeg.JpegInfo info = new global::jpeg.JpegIdentifierService().Identify(data);

        Assert.That(info.FormatFamily, Is.EqualTo(expected));
        Assert.That(info.FormatSupport, Is.EqualTo(global::jpeg.JpegSupportStatus.RecognizedButUnsupported));
        Assert.That(info.CodingProcessSupport, Is.EqualTo(global::jpeg.JpegSupportStatus.Unknown));
        Assert.That(info.SawStartOfImage, Is.False);
    }
}

internal class NonSeekableReadStream : System.IO.Stream
{
    internal System.IO.MemoryStream Source;

    internal NonSeekableReadStream(byte[] data)
    {
        Source = new System.IO.MemoryStream(data);
    }

    public override bool CanRead
    {
        get { return true; }
    }

    public override bool CanSeek
    {
        get { return false; }
    }

    public override bool CanWrite
    {
        get { return false; }
    }

    public override long Length
    {
        get { throw new System.NotSupportedException(); }
    }

    public override long Position
    {
        get { throw new System.NotSupportedException(); }
        set { throw new System.NotSupportedException(); }
    }

    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        return Source.Read(buffer, offset, count);
    }

    public override long Seek(long offset, System.IO.SeekOrigin origin)
    {
        throw new System.NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new System.NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new System.NotSupportedException();
    }
}
