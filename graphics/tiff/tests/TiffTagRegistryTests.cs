using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffTagRegistryTests
{
    [Test]
    public void TagIdConstants_MatchKnownTiffValues()
    {
        Assert.That(TiffTagId.ImageWidth, Is.EqualTo(256));
        Assert.That(TiffTagId.ImageLength, Is.EqualTo(257));
        Assert.That(TiffTagId.BitsPerSample, Is.EqualTo(258));
        Assert.That(TiffTagId.Compression, Is.EqualTo(259));
        Assert.That(TiffTagId.PhotometricInterpretation, Is.EqualTo(262));
        Assert.That(TiffTagId.StripOffsets, Is.EqualTo(273));
        Assert.That(TiffTagId.RowsPerStrip, Is.EqualTo(278));
        Assert.That(TiffTagId.StripByteCounts, Is.EqualTo(279));
        Assert.That(TiffTagId.XResolution, Is.EqualTo(282));
        Assert.That(TiffTagId.TileOffsets, Is.EqualTo(324));
        Assert.That(TiffTagId.SubIFDs, Is.EqualTo(330));
        Assert.That(TiffTagId.ExtraSamples, Is.EqualTo(338));
        Assert.That(TiffTagId.Xmp, Is.EqualTo(700));
        Assert.That(TiffTagId.CzLsmInfo, Is.EqualTo(34412));
        Assert.That(TiffTagId.ExifIfd, Is.EqualTo(34665));
        Assert.That(TiffTagId.IccProfile, Is.EqualTo(34675));
        Assert.That(TiffTagId.GeoKeyDirectory, Is.EqualTo(34735));
        Assert.That(TiffTagId.GdalMetadata, Is.EqualTo(42112));
        Assert.That(TiffTagId.DngVersion, Is.EqualTo(50706));
    }

    [Test]
    public void DefaultRegistry_ContainsBaselineExtensionMetadataAndGeoTiffTags()
    {
        TiffTagRegistry registry = TiffTagRegistry.CreateDefault();

        Assert.That(registry.Count(), Is.GreaterThan(70));
        Assert.That(registry.Contains(TiffTagId.ImageWidth), Is.True);
        Assert.That(registry.Contains(TiffTagId.TileOffsets), Is.True);
        Assert.That(registry.Contains(TiffTagId.ExifIfd), Is.True);
        Assert.That(registry.Contains(TiffTagId.CzLsmInfo), Is.True);
        Assert.That(registry.Contains(TiffTagId.DngVersion), Is.True);
        Assert.That(registry.Contains(TiffTagId.GeoKeyDirectory), Is.True);
        Assert.That(registry.Contains(TiffTagId.GdalNoData), Is.True);
        Assert.That(registry.Contains(65000), Is.False);
    }

    [Test]
    public void ImageWidthDefinition_AllowsShortOrLongSingleValue()
    {
        TiffTagRegistry registry = TiffTagRegistry.CreateDefault();

        TiffTagDefinition definition = RequireDefinition(registry, TiffTagId.ImageWidth);

        Assert.That(definition.TagId, Is.EqualTo(TiffTagId.ImageWidth));
        Assert.That(definition.Name, Is.EqualTo("ImageWidth"));
        Assert.That(definition.PreferredDataType, Is.EqualTo(TiffDataType.Long));
        Assert.That(definition.AllowsDataType(TiffDataType.Short), Is.True);
        Assert.That(definition.AllowsDataType(TiffDataType.Long), Is.True);
        Assert.That(definition.AllowsDataType(TiffDataType.Ascii), Is.False);
        Assert.That(definition.HasFixedCount(), Is.True);
        Assert.That(definition.AllowsCount(1), Is.True);
        Assert.That(definition.AllowsCount(2), Is.False);
    }

    [Test]
    public void VariableCountDefinitions_AllowAnyNonNegativeCount()
    {
        TiffTagRegistry registry = TiffTagRegistry.CreateDefault();

        TiffTagDefinition bitsPerSample = RequireDefinition(registry, TiffTagId.BitsPerSample);
        TiffTagDefinition imageDescription = RequireDefinition(registry, TiffTagId.ImageDescription);

        Assert.That(bitsPerSample.ExpectedCount, Is.EqualTo(TiffTagDefinition.AnyCount));
        Assert.That(bitsPerSample.AllowsCount(0), Is.True);
        Assert.That(bitsPerSample.AllowsCount(3), Is.True);
        Assert.That(bitsPerSample.AllowsCount(-1), Is.False);
        Assert.That(bitsPerSample.AllowsDataType(TiffDataType.Short), Is.True);
        Assert.That(imageDescription.AllowsDataType(TiffDataType.Ascii), Is.True);
        Assert.That(imageDescription.AllowsCount(100), Is.True);
    }

    [Test]
    public void OffsetDefinitions_AllowClassicAndBigTiffOffsetTypes()
    {
        TiffTagRegistry registry = TiffTagRegistry.CreateDefault();

        TiffTagDefinition subIfds = RequireDefinition(registry, TiffTagId.SubIFDs);
        TiffTagDefinition tileOffsets = RequireDefinition(registry, TiffTagId.TileOffsets);

        Assert.That(subIfds.AllowsDataType(TiffDataType.Ifd), Is.True);
        Assert.That(subIfds.AllowsDataType(TiffDataType.Ifd8), Is.True);
        Assert.That(subIfds.AllowsDataType(TiffDataType.Long8), Is.True);
        Assert.That(tileOffsets.AllowsDataType(TiffDataType.Long), Is.True);
        Assert.That(tileOffsets.AllowsDataType(TiffDataType.Long8), Is.True);
        Assert.That(tileOffsets.AllowsCount(4), Is.True);
    }

    [Test]
    public void GeoTiffDefinitions_ExposeExpectedTypesAndCounts()
    {
        TiffTagRegistry registry = TiffTagRegistry.CreateDefault();

        TiffTagDefinition pixelScale = RequireDefinition(registry, TiffTagId.ModelPixelScale);
        TiffTagDefinition transform = RequireDefinition(registry, TiffTagId.ModelTransformation);
        TiffTagDefinition keyDirectory = RequireDefinition(registry, TiffTagId.GeoKeyDirectory);

        Assert.That(pixelScale.AllowsDataType(TiffDataType.Double), Is.True);
        Assert.That(pixelScale.AllowsCount(3), Is.True);
        Assert.That(transform.AllowsDataType(TiffDataType.Double), Is.True);
        Assert.That(transform.AllowsCount(16), Is.True);
        Assert.That(transform.AllowsCount(15), Is.False);
        Assert.That(keyDirectory.AllowsDataType(TiffDataType.Short), Is.True);
        Assert.That(keyDirectory.AllowsCount(4), Is.True);
    }

    [Test]
    public void Find_ReturnsNullForUnknownTag()
    {
        TiffTagRegistry registry = TiffTagRegistry.CreateDefault();

        TiffTagDefinition? definition = registry.Find(65000);

        Assert.That(definition, Is.Null);
    }

    [Test]
    public void Find_ReturnsCopySoRegistryDefinitionsCannotBeMutatedExternally()
    {
        TiffTagRegistry registry = TiffTagRegistry.CreateDefault();

        TiffTagDefinition definition = RequireDefinition(registry, TiffTagId.ImageWidth);
        definition.Name = "Changed";
        definition.AllowedDataTypes[0] = TiffDataType.Ascii;
        TiffTagDefinition definitionAgain = RequireDefinition(registry, TiffTagId.ImageWidth);

        Assert.That(definitionAgain.Name, Is.EqualTo("ImageWidth"));
        Assert.That(definitionAgain.AllowsDataType(TiffDataType.Short), Is.True);
        Assert.That(definitionAgain.AllowsDataType(TiffDataType.Ascii), Is.False);
    }

    [Test]
    public void Register_ReplacesDefinitionWithCopiedValue()
    {
        TiffTagRegistry registry = new TiffTagRegistry();
        TiffTagDefinition definition = TiffTagDefinition.Create(65000, "Private", TiffDataType.Byte, new TiffDataType[] { TiffDataType.Byte }, TiffTagDefinition.AnyCount);

        registry.Register(definition);
        definition.Name = "Changed";

        TiffTagDefinition registered = RequireDefinition(registry, 65000);
        Assert.That(registry.Count(), Is.EqualTo(1));
        Assert.That(registered.Name, Is.EqualTo("Private"));
        Assert.That(registered.AllowsDataType(TiffDataType.Byte), Is.True);
    }

    static TiffTagDefinition RequireDefinition(TiffTagRegistry registry, int tagId)
    {
        TiffTagDefinition? definition = registry.Find(tagId);
        Assert.That(definition, Is.Not.Null);
        return definition!;
    }
}
