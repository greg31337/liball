using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffCloudOptimizedGeoTiffWriterLayoutTests
{
    [Test]
    public void Flush_CloudOptimizedLayoutWritesOverviewTileDataBeforeFullResolutionTiles()
    {
        byte[] fileBytes = CreateTwoLevelTiledGeoTiff(true);
        TiffFile file = TiffFile.OpenRead(fileBytes);
        TiffDirectory[] directories = file.GetDirectories();

        ulong baseTileOffset = ReadFirstTileOffset(directories[0], fileBytes);
        ulong overviewTileOffset = ReadFirstTileOffset(directories[1], fileBytes);
        TiffCloudOptimizedGeoTiffValidationResult validationResult = TiffCloudOptimizedGeoTiffValidator.Validate(fileBytes);

        Assert.That(file.Success, Is.True);
        Assert.That(directories[0].Offset, Is.LessThan(directories[1].Offset));
        Assert.That(overviewTileOffset, Is.LessThan(baseTileOffset));
        Assert.That(validationResult.Success, Is.True);
    }

    [Test]
    public void Flush_DefaultLayoutKeepsRawTileDataInImageOrder()
    {
        byte[] fileBytes = CreateTwoLevelTiledGeoTiff(false);
        TiffFile file = TiffFile.OpenRead(fileBytes);
        TiffDirectory[] directories = file.GetDirectories();

        ulong baseTileOffset = ReadFirstTileOffset(directories[0], fileBytes);
        ulong overviewTileOffset = ReadFirstTileOffset(directories[1], fileBytes);
        TiffCloudOptimizedGeoTiffValidationResult validation = TiffCloudOptimizedGeoTiffValidator.Validate(fileBytes);

        Assert.That(file.Success, Is.True);
        Assert.That(baseTileOffset, Is.LessThan(overviewTileOffset));
        Assert.That(validation.Success, Is.False);
        Assert.That(validation.Diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
    }

    static byte[] CreateTwoLevelTiledGeoTiff(bool cloudOptimizedLayout)
    {
        TiffWriteOptions options = new TiffWriteOptions();
        options.CloudOptimizedGeoTiffLayout = cloudOptimizedLayout;
        TiffWriter writer = TiffWriter.Create(options);
        int baseImageIndex = writer.AddImage(TiffImageDefinition.Create(CreateTiledTags(32, 32, false, true)));
        int overviewImageIndex = writer.AddImage(TiffImageDefinition.Create(CreateTiledTags(16, 16, true, false)));

        writer.WriteRawTile(baseImageIndex, 0, CreateBytes(1024));
        writer.WriteRawTile(overviewImageIndex, 0, CreateBytes(256));
        writer.Flush();

        Assert.That(writer.Success, Is.True);
        return writer.GetFileBytes();
    }

    static ulong ReadFirstTileOffset(TiffDirectory directory, byte[] fileBytes)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        TiffIfdEntry? entry = directory.FindEntry(TiffTagId.TileOffsets);
        Assert.That(entry, Is.Not.Null);

        ulong[] offsets = TiffImageInfoExtractor.ReadUnsignedArray(entry, fileBytes, TiffByteOrder.LittleEndian, new TiffLimits(), directory.ImageIndex, diagnostics);

        Assert.That(diagnostics.Count(), Is.EqualTo(0));
        Assert.That(offsets.Length, Is.EqualTo(1));
        return offsets[0];
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
