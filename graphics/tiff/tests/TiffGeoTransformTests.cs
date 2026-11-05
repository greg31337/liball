using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffGeoTransformTests
{
    [Test]
    public void ImageToGeoModel_UsesTransformationMatrixWhenPresent()
    {
        TiffGeoModelMetadata metadata = TiffGeoModelMetadata.Create(
            true,
            new double[] { 1, 1, 0 },
            true,
            new TiffGeoTiepoint[] { TiffGeoTiepoint.Create(0, 0, 0, 0, 0, 0) },
            true,
            new double[] { 2, 0, 0, 10, 0, -3, 0, 20, 0, 0, 1, 5, 0, 0, 0, 1 });

        TiffGeoTransformResult result = TiffCodec.ImageToGeoModel(metadata, 4, 5, 6);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Coordinate.X, Is.EqualTo(18).Within(0.0000001));
        Assert.That(result.Coordinate.Y, Is.EqualTo(5).Within(0.0000001));
        Assert.That(result.Coordinate.Z, Is.EqualTo(11).Within(0.0000001));
    }

    [Test]
    public void GeoModelToImage_InvertsAffineTransformationMatrix()
    {
        TiffGeoModelMetadata metadata = TiffGeoModelMetadata.Create(
            false,
            new double[0],
            false,
            new TiffGeoTiepoint[0],
            true,
            new double[] { 2, 0, 0, 10, 0, -3, 0, 20, 0, 0, 1, 5, 0, 0, 0, 1 });

        TiffGeoTransformResult result = TiffCodec.GeoModelToImage(metadata, 18, 5, 11);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Coordinate.X, Is.EqualTo(4).Within(0.0000001));
        Assert.That(result.Coordinate.Y, Is.EqualTo(5).Within(0.0000001));
        Assert.That(result.Coordinate.Z, Is.EqualTo(6).Within(0.0000001));
    }

    [Test]
    public void ImageToGeoModel_UsesPixelScaleAndFirstTiepoint()
    {
        TiffGeoModelMetadata metadata = TiffGeoModelMetadata.Create(
            true,
            new double[] { 30, 30, 10 },
            true,
            new TiffGeoTiepoint[] { TiffGeoTiepoint.Create(0, 0, 0, 100, 200, 5) },
            false,
            new double[0]);

        TiffGeoTransformResult result = TiffCodec.ImageToGeoModel(metadata, 2, 3, 4);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Coordinate.X, Is.EqualTo(160).Within(0.0000001));
        Assert.That(result.Coordinate.Y, Is.EqualTo(110).Within(0.0000001));
        Assert.That(result.Coordinate.Z, Is.EqualTo(45).Within(0.0000001));
    }

    [Test]
    public void GeoModelToImage_InvertsPixelScaleAndFirstTiepoint()
    {
        TiffGeoModelMetadata metadata = TiffGeoModelMetadata.Create(
            true,
            new double[] { 30, 30, 10 },
            true,
            new TiffGeoTiepoint[] { TiffGeoTiepoint.Create(0, 0, 0, 100, 200, 5) },
            false,
            new double[0]);

        TiffGeoTransformResult result = TiffCodec.GeoModelToImage(metadata, 160, 110, 45);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Coordinate.X, Is.EqualTo(2).Within(0.0000001));
        Assert.That(result.Coordinate.Y, Is.EqualTo(3).Within(0.0000001));
        Assert.That(result.Coordinate.Z, Is.EqualTo(4).Within(0.0000001));
    }

    [Test]
    public void ImageToGeoModel_ReportsMissingTransform()
    {
        TiffGeoTransformResult result = TiffCodec.ImageToGeoModel(new TiffGeoModelMetadata(), 0, 0, 0);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidTagValue), Is.True);
    }

    [Test]
    public void GeoModelToImage_ReportsSingularTransformation()
    {
        TiffGeoModelMetadata metadata = TiffGeoModelMetadata.Create(
            false,
            new double[0],
            false,
            new TiffGeoTiepoint[0],
            true,
            new double[] { 1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 1 });

        TiffGeoTransformResult result = TiffCodec.GeoModelToImage(metadata, 1, 2, 3);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidTagValue), Is.True);
        Assert.That(result.Diagnostics.Get(0).TagId, Is.EqualTo(TiffTagId.ModelTransformation));
    }

    [Test]
    public void GeoModelToImage_ReportsZeroPixelScale()
    {
        TiffGeoModelMetadata metadata = TiffGeoModelMetadata.Create(
            true,
            new double[] { 0, 30, 0 },
            true,
            new TiffGeoTiepoint[] { TiffGeoTiepoint.Create(0, 0, 0, 100, 200, 0) },
            false,
            new double[0]);

        TiffGeoTransformResult result = TiffCodec.GeoModelToImage(metadata, 100, 200, 0);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidTagValue), Is.True);
        Assert.That(result.Diagnostics.Get(0).TagId, Is.EqualTo(TiffTagId.ModelPixelScale));
    }
}
