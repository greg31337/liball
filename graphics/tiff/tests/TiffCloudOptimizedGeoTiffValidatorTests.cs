using System.IO;
using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffCloudOptimizedGeoTiffValidatorTests
{
    [Test]
    public void Validate_ReturnsSuccessForTiledGeoTiffWithOverview()
    {
        byte[] fileBytes = CreateTiledGeoTiffWithOverview(true, 16, 16, true);

        TiffCloudOptimizedGeoTiffValidationResult result = TiffCloudOptimizedGeoTiffValidator.Validate(fileBytes);

        Assert.That(result.Success, Is.True);
        Assert.That(result.ImageCount, Is.EqualTo(2));
        Assert.That(result.FullResolutionImageCount, Is.EqualTo(1));
        Assert.That(result.OverviewImageCount, Is.EqualTo(1));
        Assert.That(result.Diagnostics.Count(), Is.EqualTo(0));
    }

    [Test]
    public void Validate_ReportsStrippedImageDirectory()
    {
        TiffWriter writer = TiffWriter.Create();
        int imageIndex = writer.AddImage(TiffImageDefinition.Create(CreateStrippedTags()));
        writer.WriteRawStrip(imageIndex, 0, CreateBytes(16));
        writer.Flush();

        TiffCloudOptimizedGeoTiffValidationResult result = TiffCloudOptimizedGeoTiffValidator.Validate(writer.GetFileBytes());

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidTagValue), Is.True);
    }

    [Test]
    public void Validate_ReportsMissingGeoTiffTagsOnFullResolutionImage()
    {
        byte[] fileBytes = CreateTiledGeoTiffWithOverview(false, 16, 16, false);

        TiffCloudOptimizedGeoTiffValidationResult result = TiffCloudOptimizedGeoTiffValidator.Validate(fileBytes);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.MissingRequiredTag), Is.True);
    }

    [Test]
    public void Validate_ReportsOverviewDimensionsThatDoNotDecrease()
    {
        byte[] fileBytes = CreateTiledGeoTiffWithOverview(true, 32, 32, false);

        TiffCloudOptimizedGeoTiffValidationResult result = TiffCloudOptimizedGeoTiffValidator.Validate(fileBytes);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidTagValue), Is.True);
    }

    [Test]
    public void ValidateTileOffsetByteCounts_ReportsNonMonotonicRanges()
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        TiffDirectory directory = new TiffDirectory();
        directory.ImageIndex = 4;

        TiffCloudOptimizedGeoTiffValidator.ValidateTileOffsetByteCounts(directory, new ulong[] { 200, 190 }, new ulong[] { 16, 16 }, diagnostics);

        Assert.That(diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
    }

    [Test]
    public void ValidateDirectoryOffsetOrder_ReportsNonIncreasingOffsets()
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        TiffDirectory[] directories = new TiffDirectory[]
        {
            TiffDirectory.Create(0, 128, 64, new TiffIfdEntry[0]),
            TiffDirectory.Create(1, 64, 0, new TiffIfdEntry[0])
        };

        TiffCloudOptimizedGeoTiffValidator.ValidateDirectoryOffsetOrder(directories, diagnostics);

        Assert.That(diagnostics.ContainsCode(TiffErrorCode.InvalidOffset), Is.True);
    }

    [Test]
    public void Validate_StreamClosesByDefaultAfterValidation()
    {
        MemoryStream stream = new MemoryStream(CreateTiledGeoTiffWithOverview(true, 16, 16, true));

        TiffCloudOptimizedGeoTiffValidationResult result = TiffCloudOptimizedGeoTiffValidator.Validate(stream);

        Assert.That(result.Success, Is.True);
        Assert.That(stream.CanRead, Is.False);
    }

    static byte[] CreateTiledGeoTiffWithOverview(bool includeGeoTags, ulong overviewWidth, ulong overviewLength, bool cloudOptimizedLayout)
    {
        TiffWriteOptions options = new TiffWriteOptions();
        options.CloudOptimizedGeoTiffLayout = cloudOptimizedLayout;
        TiffWriter writer = TiffWriter.Create(options);
        int baseImageIndex = writer.AddImage(TiffImageDefinition.Create(CreateTiledTags(32, 32, false, includeGeoTags)));
        int overviewImageIndex = writer.AddImage(TiffImageDefinition.Create(CreateTiledTags(overviewWidth, overviewLength, true, false)));

        writer.WriteRawTile(baseImageIndex, 0, CreateBytes(1024));
        writer.WriteRawTile(overviewImageIndex, 0, CreateBytes((int)(overviewWidth * overviewLength)));
        writer.Flush();
        Assert.That(writer.Success, Is.True);
        return writer.GetFileBytes();
    }

    static TiffTagValue[] CreateTiledTags(ulong width, ulong length, bool reduced, bool includeGeoTags)
    {
        int count = 6;
        if (reduced)
        {
            count = count + 1;
        }

        if (includeGeoTags)
        {
            count = count + 3;
        }

        TiffTagValue[] values = new TiffTagValue[count];
        values[0] = TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageWidth, TiffDataType.Long, width);
        values[1] = TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageLength, TiffDataType.Long, length);
        values[2] = TiffTagValue.CreateUnsignedScalar(TiffTagId.BitsPerSample, TiffDataType.Short, 8);
        values[3] = TiffTagValue.CreateUnsignedScalar(TiffTagId.PhotometricInterpretation, TiffDataType.Short, (ulong)TiffPhotometric.BlackIsZero);
        values[4] = TiffTagValue.CreateUnsignedScalar(TiffTagId.TileWidth, TiffDataType.Long, width);
        values[5] = TiffTagValue.CreateUnsignedScalar(TiffTagId.TileLength, TiffDataType.Long, length);
        int index = 6;
        if (reduced)
        {
            values[index] = TiffTagValue.CreateUnsignedScalar(TiffTagId.NewSubfileType, TiffDataType.Long, 1);
            index = index + 1;
        }

        if (includeGeoTags)
        {
            values[index] = TiffTagValue.CreateUnsignedArray(TiffTagId.GeoKeyDirectory, TiffDataType.Short, new ulong[] { 1, 1, 0, 0 });
            values[index + 1] = TiffTagValue.CreateFloatingArray(TiffTagId.ModelPixelScale, TiffDataType.Double, new double[] { 1, 1, 0 });
            values[index + 2] = TiffTagValue.CreateFloatingArray(TiffTagId.ModelTiepoint, TiffDataType.Double, new double[] { 0, 0, 0, 100, 200, 0 });
        }

        return values;
    }

    static TiffTagValue[] CreateStrippedTags()
    {
        return new TiffTagValue[]
        {
            TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageWidth, TiffDataType.Long, 4),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageLength, TiffDataType.Long, 4),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.BitsPerSample, TiffDataType.Short, 8),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.PhotometricInterpretation, TiffDataType.Short, (ulong)TiffPhotometric.BlackIsZero),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.RowsPerStrip, TiffDataType.Long, 4)
        };
    }

    static byte[] CreateBytes(int count)
    {
        byte[] bytes = new byte[count];
        int index = 0;
        while (index < bytes.Length)
        {
            bytes[index] = (byte)(index % 251);
            index = index + 1;
        }

        return bytes;
    }
}
