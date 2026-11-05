using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffExpandedTagRegistryTests
{
    [Test]
    public void TagIds_ExposeFaxJpegDepthCfaMetadataAndDngValues()
    {
        Assert.That(TiffTagId.Group3Options, Is.EqualTo(292));
        Assert.That(TiffTagId.Group4Options, Is.EqualTo(293));
        Assert.That(TiffTagId.ColorResponseUnit, Is.EqualTo(300));
        Assert.That(TiffTagId.ColorResponseCurves, Is.EqualTo(301));
        Assert.That(TiffTagId.BadFaxLines, Is.EqualTo(326));
        Assert.That(TiffTagId.CleanFaxData, Is.EqualTo(327));
        Assert.That(TiffTagId.ConsecutiveBadFaxLines, Is.EqualTo(328));
        Assert.That(TiffTagId.JPEGProc, Is.EqualTo(512));
        Assert.That(TiffTagId.JPEGInterchangeFormat, Is.EqualTo(513));
        Assert.That(TiffTagId.JPEGInterchangeFormatLength, Is.EqualTo(514));
        Assert.That(TiffTagId.JPEGRestartInterval, Is.EqualTo(515));
        Assert.That(TiffTagId.JPEGLosslessPredictors, Is.EqualTo(517));
        Assert.That(TiffTagId.JPEGPointTransforms, Is.EqualTo(518));
        Assert.That(TiffTagId.JPEGQTables, Is.EqualTo(519));
        Assert.That(TiffTagId.JPEGDCTables, Is.EqualTo(520));
        Assert.That(TiffTagId.JPEGACTables, Is.EqualTo(521));
        Assert.That(TiffTagId.ImageDepth, Is.EqualTo(32997));
        Assert.That(TiffTagId.TileDepth, Is.EqualTo(32998));
        Assert.That(TiffTagId.CFARepeatPatternDim, Is.EqualTo(33421));
        Assert.That(TiffTagId.CFAPattern, Is.EqualTo(33422));
        Assert.That(TiffTagId.RichTiffIptc, Is.EqualTo(33723));
        Assert.That(TiffTagId.FaxReceiveParameters, Is.EqualTo(34908));
        Assert.That(TiffTagId.FaxSubAddress, Is.EqualTo(34909));
        Assert.That(TiffTagId.FaxReceiveTime, Is.EqualTo(34910));
        Assert.That(TiffTagId.FaxDcs, Is.EqualTo(34911));
        Assert.That(TiffTagId.BlackLevel, Is.EqualTo(50714));
        Assert.That(TiffTagId.WhiteLevel, Is.EqualTo(50717));
        Assert.That(TiffTagId.DefaultCropSize, Is.EqualTo(50720));
        Assert.That(TiffTagId.ActiveArea, Is.EqualTo(50829));
        Assert.That(TiffTagId.DepthFormat, Is.EqualTo(51177));
        Assert.That(TiffTagId.EnhanceParams, Is.EqualTo(51182));
    }

    [Test]
    public void DefaultRegistry_ContainsEveryExpandedInventoryFamily()
    {
        TiffTagRegistry registry = TiffTagRegistry.CreateDefault();
        int[] tagIds = new int[]
        {
            TiffTagId.ColorResponseUnit,
            TiffTagId.BadFaxLines,
            TiffTagId.FaxReceiveParameters,
            TiffTagId.JPEGProc,
            TiffTagId.JPEGACTables,
            TiffTagId.ImageDepth,
            TiffTagId.TileDepth,
            TiffTagId.CFARepeatPatternDim,
            TiffTagId.CFAPattern,
            TiffTagId.ExifIfd,
            TiffTagId.GpsIfd,
            TiffTagId.InteroperabilityIfd,
            TiffTagId.Xmp,
            TiffTagId.RichTiffIptc,
            TiffTagId.IccProfile,
            TiffTagId.ModelPixelScale,
            TiffTagId.GeoKeyDirectory,
            TiffTagId.GdalMetadata,
            TiffTagId.DngVersion,
            TiffTagId.CFALayout,
            TiffTagId.BlackLevel,
            TiffTagId.DefaultCropSize,
            TiffTagId.ActiveArea,
            TiffTagId.DepthFormat
        };

        Assert.That(registry.Count(), Is.GreaterThan(130));
        int index = 0;
        while (index < tagIds.Length)
        {
            Assert.That(registry.Contains(tagIds[index]), Is.True, "Missing tag " + tagIds[index]);
            index = index + 1;
        }
    }

    [Test]
    public void Registry_UsesStandardFaxJpegAndDepthTypeCounts()
    {
        TiffTagRegistry registry = TiffTagRegistry.CreateDefault();

        AssertDefinition(registry, TiffTagId.BadFaxLines, TiffDataType.Long, 1);
        AssertDefinition(registry, TiffTagId.CleanFaxData, TiffDataType.Short, 1);
        AssertDefinition(registry, TiffTagId.FaxSubAddress, TiffDataType.Ascii, TiffTagDefinition.AnyCount);
        AssertDefinition(registry, TiffTagId.JPEGProc, TiffDataType.Short, 1);
        AssertDefinition(registry, TiffTagId.JPEGRestartInterval, TiffDataType.Short, 1);
        AssertDefinition(registry, TiffTagId.JPEGQTables, TiffDataType.Long, TiffTagDefinition.AnyCount);
        AssertDefinition(registry, TiffTagId.ImageDepth, TiffDataType.Long, 1);
        AssertDefinition(registry, TiffTagId.TileDepth, TiffDataType.Long, 1);
        AssertDefinition(registry, TiffTagId.CFARepeatPatternDim, TiffDataType.Short, 2);
    }

    [Test]
    public void Registry_AllowsBigTiffStorageAndSpecificationAlternativeTypes()
    {
        TiffTagRegistry registry = TiffTagRegistry.CreateDefault();
        TiffTagDefinition stripOffsets = RequireDefinition(registry, TiffTagId.StripOffsets);
        TiffTagDefinition stripByteCounts = RequireDefinition(registry, TiffTagId.StripByteCounts);
        TiffTagDefinition imageWidth = RequireDefinition(registry, TiffTagId.ImageWidth);
        TiffTagDefinition sampleMinimum = RequireDefinition(registry, TiffTagId.SMinSampleValue);
        TiffTagDefinition dotRange = RequireDefinition(registry, TiffTagId.DotRange);
        TiffTagDefinition iptc = RequireDefinition(registry, TiffTagId.Iptc);
        TiffTagDefinition blackLevel = RequireDefinition(registry, TiffTagId.BlackLevel);
        TiffTagDefinition whiteLevel = RequireDefinition(registry, TiffTagId.WhiteLevel);
        TiffTagDefinition cropSize = RequireDefinition(registry, TiffTagId.DefaultCropSize);

        Assert.That(stripOffsets.AllowsDataType(TiffDataType.Long8), Is.True);
        Assert.That(stripByteCounts.AllowsDataType(TiffDataType.Long8), Is.True);
        Assert.That(imageWidth.AllowsDataType(TiffDataType.Long8), Is.False);
        Assert.That(sampleMinimum.AllowsDataType(TiffDataType.SLong), Is.True);
        Assert.That(sampleMinimum.AllowsDataType(TiffDataType.Double), Is.True);
        Assert.That(dotRange.AllowsDataType(TiffDataType.Byte), Is.True);
        Assert.That(dotRange.AllowsDataType(TiffDataType.Short), Is.True);
        Assert.That(iptc.AllowsDataType(TiffDataType.Long), Is.True);
        Assert.That(iptc.AllowsDataType(TiffDataType.Undefined), Is.True);
        Assert.That(blackLevel.AllowsDataType(TiffDataType.Short), Is.True);
        Assert.That(blackLevel.AllowsDataType(TiffDataType.Long), Is.True);
        Assert.That(blackLevel.AllowsDataType(TiffDataType.Rational), Is.True);
        Assert.That(whiteLevel.AllowsDataType(TiffDataType.Short), Is.True);
        Assert.That(whiteLevel.AllowsDataType(TiffDataType.Long), Is.True);
        Assert.That(cropSize.AllowsDataType(TiffDataType.Short), Is.True);
        Assert.That(cropSize.AllowsDataType(TiffDataType.Long), Is.True);
        Assert.That(cropSize.AllowsDataType(TiffDataType.Rational), Is.True);
    }

    [Test]
    public void DngDefinitions_ExposeFixedAndVariableCounts()
    {
        TiffTagRegistry registry = TiffTagRegistry.CreateDefault();

        AssertDefinition(registry, TiffTagId.DngVersion, TiffDataType.Byte, 4);
        AssertDefinition(registry, TiffTagId.BlackLevelRepeatDim, TiffDataType.Short, 2);
        AssertDefinition(registry, TiffTagId.DefaultScale, TiffDataType.Rational, 2);
        AssertDefinition(registry, TiffTagId.LensInfo, TiffDataType.Rational, 4);
        AssertDefinition(registry, TiffTagId.RawDataUniqueId, TiffDataType.Byte, 16);
        AssertDefinition(registry, TiffTagId.ActiveArea, TiffDataType.Long, 4);
        AssertDefinition(registry, TiffTagId.BlackLevelDeltaH, TiffDataType.SRational, TiffTagDefinition.AnyCount);
        AssertDefinition(registry, TiffTagId.MaskedAreas, TiffDataType.Long, TiffTagDefinition.AnyCount);
        AssertDefinition(registry, TiffTagId.EnhanceParams, TiffDataType.Ascii, TiffTagDefinition.AnyCount);
    }

    [Test]
    public void ExpandedDefaults_ReturnSpecificationValuesAndStorageTypes()
    {
        AssertUnsignedDefault(TiffTagId.NewSubfileType, TiffDataType.Long, new ulong[] { 0 });
        AssertUnsignedDefault(TiffTagId.MinSampleValue, TiffDataType.Short, new ulong[] { 0 });
        AssertUnsignedDefault(TiffTagId.T4Options, TiffDataType.Long, new ulong[] { 0 });
        AssertUnsignedDefault(TiffTagId.T6Options, TiffDataType.Long, new ulong[] { 0 });
        AssertUnsignedDefault(TiffTagId.InkSet, TiffDataType.Short, new ulong[] { 1 });
        AssertUnsignedDefault(TiffTagId.NumberOfInks, TiffDataType.Short, new ulong[] { 4 });
        AssertUnsignedDefault(TiffTagId.SampleFormat, TiffDataType.Short, new ulong[] { 1 });
        AssertUnsignedDefault(TiffTagId.ImageDepth, TiffDataType.Long, new ulong[] { 1 });
        AssertUnsignedDefault(TiffTagId.TileDepth, TiffDataType.Long, new ulong[] { 1 });
        AssertUnsignedDefault(TiffTagId.JPEGProc, TiffDataType.Short, new ulong[] { 1 });
        AssertUnsignedDefault(TiffTagId.CFALayout, TiffDataType.Short, new ulong[] { 1 });
        AssertUnsignedDefault(TiffTagId.BlackLevelRepeatDim, TiffDataType.Short, new ulong[] { 1, 1 });

        AssertRationalDefault(TiffTagId.BlackLevel, TiffDataType.Rational, new long[] { 0 }, new long[] { 1 });
        AssertRationalDefault(TiffTagId.DefaultScale, TiffDataType.Rational, new long[] { 1, 1 }, new long[] { 1, 1 });
        AssertRationalDefault(TiffTagId.DefaultCropOrigin, TiffDataType.Rational, new long[] { 0, 0 }, new long[] { 1, 1 });
        AssertRationalDefault(TiffTagId.BaselineExposure, TiffDataType.SRational, new long[] { 0 }, new long[] { 1 });
        AssertRationalDefault(TiffTagId.BaselineNoise, TiffDataType.Rational, new long[] { 1 }, new long[] { 1 });
        AssertRationalDefault(TiffTagId.BestQualityScale, TiffDataType.Rational, new long[] { 1 }, new long[] { 1 });
    }

    static void AssertDefinition(TiffTagRegistry registry, int tagId, TiffDataType dataType, long count)
    {
        TiffTagDefinition definition = RequireDefinition(registry, tagId);
        Assert.That(definition.PreferredDataType, Is.EqualTo(dataType));
        Assert.That(definition.AllowsDataType(dataType), Is.True);
        Assert.That(definition.ExpectedCount, Is.EqualTo(count));
    }

    static void AssertUnsignedDefault(int tagId, TiffDataType dataType, ulong[] expected)
    {
        TiffTagDefaultResult result = TiffTagDefaultLookup.Find(tagId);
        Assert.That(result.Found, Is.True);
        Assert.That(result.UsedDefault, Is.True);
        Assert.That(result.Value.DataType, Is.EqualTo(dataType));
        Assert.That(result.Value.UnsignedValues, Is.EqualTo(expected));
    }

    static void AssertRationalDefault(int tagId, TiffDataType dataType, long[] numerators, long[] denominators)
    {
        TiffTagDefaultResult result = TiffTagDefaultLookup.Find(tagId);
        Assert.That(result.Found, Is.True);
        Assert.That(result.Value.DataType, Is.EqualTo(dataType));
        Assert.That(result.Value.RationalValues.Length, Is.EqualTo(numerators.Length));
        int index = 0;
        while (index < numerators.Length)
        {
            Assert.That(result.Value.RationalValues[index].Numerator, Is.EqualTo(numerators[index]));
            Assert.That(result.Value.RationalValues[index].Denominator, Is.EqualTo(denominators[index]));
            index = index + 1;
        }
    }

    static TiffTagDefinition RequireDefinition(TiffTagRegistry registry, int tagId)
    {
        TiffTagDefinition? definition = registry.Find(tagId);
        Assert.That(definition, Is.Not.Null);
        return definition!;
    }
}
