using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffTagDefaultLookupTests
{
    [Test]
    public void ContainsDefault_ReturnsTrueForKnownDefaultedTags()
    {
        Assert.That(TiffTagDefaultLookup.ContainsDefault(TiffTagId.BitsPerSample), Is.True);
        Assert.That(TiffTagDefaultLookup.ContainsDefault(TiffTagId.Compression), Is.True);
        Assert.That(TiffTagDefaultLookup.ContainsDefault(TiffTagId.Thresholding), Is.True);
        Assert.That(TiffTagDefaultLookup.ContainsDefault(TiffTagId.FillOrder), Is.True);
        Assert.That(TiffTagDefaultLookup.ContainsDefault(TiffTagId.Orientation), Is.True);
        Assert.That(TiffTagDefaultLookup.ContainsDefault(TiffTagId.SamplesPerPixel), Is.True);
        Assert.That(TiffTagDefaultLookup.ContainsDefault(TiffTagId.RowsPerStrip), Is.True);
        Assert.That(TiffTagDefaultLookup.ContainsDefault(TiffTagId.GrayResponseUnit), Is.True);
        Assert.That(TiffTagDefaultLookup.ContainsDefault(TiffTagId.ResolutionUnit), Is.True);
        Assert.That(TiffTagDefaultLookup.ContainsDefault(TiffTagId.PlanarConfiguration), Is.True);
        Assert.That(TiffTagDefaultLookup.ContainsDefault(TiffTagId.Predictor), Is.True);
        Assert.That(TiffTagDefaultLookup.ContainsDefault(TiffTagId.YCbCrCoefficients), Is.True);
        Assert.That(TiffTagDefaultLookup.ContainsDefault(TiffTagId.YCbCrSubSampling), Is.True);
        Assert.That(TiffTagDefaultLookup.ContainsDefault(TiffTagId.YCbCrPositioning), Is.True);
        Assert.That(TiffTagDefaultLookup.ContainsDefault(TiffTagId.ImageWidth), Is.False);
        Assert.That(TiffTagDefaultLookup.ContainsDefault(65000), Is.False);
    }

    [Test]
    public void Find_ReturnsScalarShortDefaults()
    {
        AssertShortDefault(TiffTagId.BitsPerSample, 1);
        AssertShortDefault(TiffTagId.Compression, (ulong)TiffCompression.None);
        AssertShortDefault(TiffTagId.Thresholding, 1);
        AssertShortDefault(TiffTagId.FillOrder, 1);
        AssertShortDefault(TiffTagId.Orientation, (ulong)TiffOrientation.TopLeft);
        AssertShortDefault(TiffTagId.SamplesPerPixel, 1);
        AssertShortDefault(TiffTagId.GrayResponseUnit, 2);
        AssertShortDefault(TiffTagId.ResolutionUnit, 2);
        AssertShortDefault(TiffTagId.PlanarConfiguration, (ulong)TiffPlanarConfiguration.Contiguous);
        AssertShortDefault(TiffTagId.Predictor, 1);
        AssertShortDefault(TiffTagId.YCbCrPositioning, 1);
    }

    [Test]
    public void Find_ReturnsRowsPerStripLongDefault()
    {
        TiffTagDefaultResult result = TiffTagDefaultLookup.Find(TiffTagId.RowsPerStrip);

        Assert.That(result.Found, Is.True);
        Assert.That(result.UsedDefault, Is.True);
        Assert.That(result.Value.TagId, Is.EqualTo(TiffTagId.RowsPerStrip));
        Assert.That(result.Value.DataType, Is.EqualTo(TiffDataType.Long));
        Assert.That(result.Value.Count, Is.EqualTo(1));
        Assert.That(result.Value.UnsignedValues, Is.EqualTo(new ulong[] { 4294967295 }));
    }

    [Test]
    public void Find_ReturnsYCbCrArrayDefaults()
    {
        TiffTagDefaultResult coefficients = TiffTagDefaultLookup.Find(TiffTagId.YCbCrCoefficients);
        TiffTagDefaultResult subSampling = TiffTagDefaultLookup.Find(TiffTagId.YCbCrSubSampling);

        Assert.That(coefficients.Found, Is.True);
        Assert.That(coefficients.Value.DataType, Is.EqualTo(TiffDataType.Rational));
        Assert.That(coefficients.Value.Count, Is.EqualTo(3));
        Assert.That(coefficients.Value.RationalValues[0].Numerator, Is.EqualTo(299));
        Assert.That(coefficients.Value.RationalValues[0].Denominator, Is.EqualTo(1000));
        Assert.That(coefficients.Value.RationalValues[1].Numerator, Is.EqualTo(587));
        Assert.That(coefficients.Value.RationalValues[1].Denominator, Is.EqualTo(1000));
        Assert.That(coefficients.Value.RationalValues[2].Numerator, Is.EqualTo(114));
        Assert.That(coefficients.Value.RationalValues[2].Denominator, Is.EqualTo(1000));
        Assert.That(subSampling.Value.DataType, Is.EqualTo(TiffDataType.Short));
        Assert.That(subSampling.Value.UnsignedValues, Is.EqualTo(new ulong[] { 2, 2 }));
    }

    [Test]
    public void Find_ReturnsNotFoundForTagsWithoutKnownDefaults()
    {
        TiffTagDefaultResult result = TiffTagDefaultLookup.Find(TiffTagId.ImageWidth);

        Assert.That(result.Found, Is.False);
        Assert.That(result.UsedDefault, Is.False);
        Assert.That(result.Value.TagId, Is.EqualTo(0));
        Assert.That(result.Value.DataType, Is.EqualTo(TiffDataType.Unknown));
    }

    [Test]
    public void FindValueOrDefault_ReturnsExistingValueCopyBeforeDefault()
    {
        TiffTagValue compression = TiffTagValue.CreateUnsignedScalar(TiffTagId.Compression, TiffDataType.Short, 32773);
        TiffTagValue[] values = new TiffTagValue[]
        {
            compression
        };

        TiffTagDefaultResult result = TiffTagDefaultLookup.FindValueOrDefault(values, TiffTagId.Compression);
        compression.UnsignedValues[0] = 1;

        Assert.That(result.Found, Is.True);
        Assert.That(result.UsedDefault, Is.False);
        Assert.That(result.Value.UnsignedValues, Is.EqualTo(new ulong[] { 32773 }));
    }

    [Test]
    public void FindValueOrDefault_ReturnsDefaultWhenValueIsMissing()
    {
        TiffTagDefaultResult result = TiffTagDefaultLookup.FindValueOrDefault(new TiffTagValue[0], TiffTagId.Orientation);

        Assert.That(result.Found, Is.True);
        Assert.That(result.UsedDefault, Is.True);
        Assert.That(result.Value.UnsignedValues, Is.EqualTo(new ulong[] { (ulong)TiffOrientation.TopLeft }));
    }

    [Test]
    public void FindValueOrDefault_ReturnsNotFoundForUnknownMissingTag()
    {
        TiffTagDefaultResult result = TiffTagDefaultLookup.FindValueOrDefault(null!, 65000);

        Assert.That(result.Found, Is.False);
        Assert.That(result.UsedDefault, Is.False);
    }

    [Test]
    public void ResultCopiesMutableTagValue()
    {
        TiffTagValue value = TiffTagDefaultLookup.CreateYCbCrCoefficientsDefault();

        TiffTagDefaultResult result = TiffTagDefaultResult.Create(true, true, value);
        value.RationalValues[0].Numerator = 1;

        Assert.That(result.Value.RationalValues[0].Numerator, Is.EqualTo(299));
    }

    [Test]
    public void CreateDefaultValue_ReturnsIndependentInstances()
    {
        TiffTagValue first = TiffTagDefaultLookup.CreateDefaultValue(TiffTagId.YCbCrSubSampling);
        TiffTagValue second = TiffTagDefaultLookup.CreateDefaultValue(TiffTagId.YCbCrSubSampling);

        first.UnsignedValues[0] = 9;

        Assert.That(second.UnsignedValues, Is.EqualTo(new ulong[] { 2, 2 }));
    }

    static void AssertShortDefault(int tagId, ulong expected)
    {
        TiffTagDefaultResult result = TiffTagDefaultLookup.Find(tagId);

        Assert.That(result.Found, Is.True);
        Assert.That(result.UsedDefault, Is.True);
        Assert.That(result.Value.TagId, Is.EqualTo(tagId));
        Assert.That(result.Value.DataType, Is.EqualTo(TiffDataType.Short));
        Assert.That(result.Value.Count, Is.EqualTo(1));
        Assert.That(result.Value.UnsignedValues, Is.EqualTo(new ulong[] { expected }));
    }
}
