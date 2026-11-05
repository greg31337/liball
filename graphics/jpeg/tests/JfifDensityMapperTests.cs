namespace jpeg.Tests;

internal class JfifDensityMapperTests
{
    [Test]
    public void Mapper_MapsDotsPerInchDirectlyToDpi()
    {
        global::jpeg.JpegJfifDensityMapper mapper = new global::jpeg.JpegJfifDensityMapper();

        double dpi = mapper.ToDpi(300, 1);

        Assert.That(dpi, Is.EqualTo(300.0));
    }

    [Test]
    public void Mapper_MapsDotsPerCentimeterToDpi()
    {
        global::jpeg.JpegJfifDensityMapper mapper = new global::jpeg.JpegJfifDensityMapper();

        double dpi = mapper.ToDpi(118, 2);

        Assert.That(dpi, Is.EqualTo(299.72).Within(0.001));
    }

    [Test]
    public void Mapper_TreatsUnitZeroAsAspectRatioWithoutAbsoluteDpi()
    {
        global::jpeg.JpegJfifMetadata metadata = new global::jpeg.JpegJfifMetadata();
        metadata.DensityUnit = 0;
        metadata.XDensity = 4;
        metadata.YDensity = 3;
        global::jpeg.JpegJfifDensityMapper mapper = new global::jpeg.JpegJfifDensityMapper();

        mapper.ApplyDpi(metadata);

        Assert.That(metadata.HasDensityDpi, Is.False);
        Assert.That(metadata.XDpi, Is.EqualTo(0.0));
        Assert.That(metadata.YDpi, Is.EqualTo(0.0));
    }

    [Test]
    public void Mapper_ConvertsDpiBackToJfifDensity()
    {
        global::jpeg.JpegJfifDensityMapper mapper = new global::jpeg.JpegJfifDensityMapper();

        int inchDensity = mapper.FromDpi(300.0, 1);
        int centimeterDensity = mapper.FromDpi(300.0, 2);

        Assert.That(inchDensity, Is.EqualTo(300));
        Assert.That(centimeterDensity, Is.EqualTo(118));
    }

    [Test]
    public void Parser_PopulatesDpiFields()
    {
        byte[] payload = JfifMetadataParserTests.CreateJfifPayload(1, 2, 2, 118, 118, 0, 0);

        global::jpeg.JpegJfifMetadata metadata = new global::jpeg.JpegJfifMetadataParser().Parse(payload);

        Assert.That(metadata.HasDensityDpi, Is.True);
        Assert.That(metadata.XDpi, Is.EqualTo(299.72).Within(0.001));
        Assert.That(metadata.YDpi, Is.EqualTo(299.72).Within(0.001));
    }

    [Test]
    public void Identifier_CopiesJfifDpiFieldsToMetadata()
    {
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.App0, JfifMetadataParserTests.CreateJfifPayload(1, 2, 1, 72, 96, 0, 0));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfFrame0, IdentifierTests.CreateSofPayload(global::jpeg.JpegConstants.MarkerStartOfFrame0, 3));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfScan, IdentifierTests.CreateSosPayload(3));
        writer.WriteEndOfImage();

        global::jpeg.JpegInfo info = global::jpeg.JpegIdentifier.Identify(writer.ToArray());

        Assert.That(info.Metadata.HasDensityDpi, Is.True);
        Assert.That(info.Metadata.XDpi, Is.EqualTo(72.0));
        Assert.That(info.Metadata.YDpi, Is.EqualTo(96.0));
    }

    [Test]
    public void Mapper_RejectsInvalidDpiMappingInputs()
    {
        global::jpeg.JpegJfifDensityMapper mapper = new global::jpeg.JpegJfifDensityMapper();

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                mapper.ApplyDpi(null!);
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                mapper.ToDpi(72, 3);
            });

        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                mapper.FromDpi(1.0, 3);
            });

        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                mapper.FromDpi(65536.0, 1);
            });
    }
}
