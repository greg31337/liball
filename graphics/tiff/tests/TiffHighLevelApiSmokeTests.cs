using System.IO;
using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffHighLevelApiSmokeTests
{
    [Test]
    public void HighLevelApi_WritesOpensInspectsValidatesAndReadsStrippedImage()
    {
        string path = CreateTempPath();
        try
        {
            TiffWriter writer = TiffWriter.Create(path);
            int imageIndex = writer.AddImage(TiffImageDefinition.Create(CreateStripTags(3, 2)));
            TiffRawStripWriteResult writeResult = writer.WriteRawStrip(imageIndex, 0, new byte[] { 1, 2, 3, 4, 5, 6 });
            TiffDiagnostics flushDiagnostics = writer.Flush();
            byte[] fileBytes = File.ReadAllBytes(path);
            TiffReadOptions streamOptions = new TiffReadOptions();
            streamOptions.LeaveOpen = true;
            MemoryStream stream = new MemoryStream(fileBytes);

            TiffFile pathFile = TiffFile.OpenRead(path);
            TiffFile streamFile = TiffFile.OpenRead(stream, streamOptions);
            TiffInspectionResult inspection = TiffInspector.Inspect(path);
            TiffValidationResult validation = TiffValidator.Validate(fileBytes);
            TiffImage image = pathFile.GetImage(0);
            TiffImageInfo info = image.GetInfo();
            TiffTagDefaultResult compression = image.GetTagDefaulted(TiffTagId.Compression);
            TiffRawStripReadResult strip = image.ReadRawStrip(0);
            byte[] destination = new byte[6];
            TiffDiagnostics copyDiagnostics = image.ReadEncodedStripToBuffer(0, destination);
            TiffImage metadataOnlyImage = TiffFile.OpenMetadata(fileBytes).GetImage(0);
            TiffRawStripReadResult metadataOnlyRead = metadataOnlyImage.ReadRawStrip(0);

            Assert.That(writeResult.Success, Is.True);
            Assert.That(flushDiagnostics.Count(), Is.EqualTo(0));
            Assert.That(pathFile.Success, Is.True);
            Assert.That(streamFile.Success, Is.True);
            Assert.That(stream.CanRead, Is.True);
            Assert.That(inspection.Success, Is.True);
            Assert.That(inspection.ImageCount(), Is.EqualTo(1));
            Assert.That(validation.Success, Is.True);
            Assert.That(info.Width, Is.EqualTo(3));
            Assert.That(info.Length, Is.EqualTo(2));
            Assert.That(image.GetTag(TiffTagId.ImageWidth), Is.Not.Null);
            Assert.That(compression.Found, Is.True);
            Assert.That(compression.UsedDefault, Is.True);
            Assert.That(image.GetStripCount(), Is.EqualTo(1));
            Assert.That(image.GetScanlineSize(), Is.EqualTo(3));
            Assert.That(strip.Success, Is.True);
            Assert.That(strip.Bytes, Is.EqualTo(new byte[] { 1, 2, 3, 4, 5, 6 }));
            Assert.That(copyDiagnostics.Count(), Is.EqualTo(0));
            Assert.That(destination, Is.EqualTo(new byte[] { 1, 2, 3, 4, 5, 6 }));
            Assert.That(metadataOnlyRead.Success, Is.False);
            Assert.That(metadataOnlyRead.Diagnostics.ContainsCode(TiffErrorCode.UnknownError), Is.True);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Test]
    public void HighLevelApi_WritesStreamTiledImageAndReadsRawTile()
    {
        MemoryStream stream = new MemoryStream();
        TiffWriteOptions options = new TiffWriteOptions();
        options.LeaveOpen = true;
        TiffWriter writer = TiffWriter.Create(stream, options);
        int imageIndex = writer.AddImage(TiffImageDefinition.Create(CreateTileTags(4, 4)));
        byte[] tileBytes = CreateBytes(16);

        TiffRawTileWriteResult writeResult = writer.WriteRawTile(imageIndex, 0, tileBytes);
        TiffDiagnostics flushDiagnostics = writer.Flush();
        byte[] fileBytes = stream.ToArray();
        TiffFile file = TiffFile.OpenRead(fileBytes);
        TiffImage image = file.GetImage(0);
        TiffRawTileReadResult tile = image.ReadEncodedTile(0);

        Assert.That(writeResult.Success, Is.True);
        Assert.That(flushDiagnostics.Count(), Is.EqualTo(0));
        Assert.That(stream.CanRead, Is.True);
        Assert.That(file.Success, Is.True);
        Assert.That(image.GetTileCount(), Is.EqualTo(1));
        Assert.That(image.GetStripCount(), Is.EqualTo(0));
        Assert.That(image.GetTileSize(0), Is.EqualTo(16));
        Assert.That(tile.Success, Is.True);
        Assert.That(tile.Bytes, Is.EqualTo(tileBytes));
    }

    static TiffTagValue[] CreateStripTags(ulong width, ulong length)
    {
        return new TiffTagValue[]
        {
            TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageWidth, TiffDataType.Long, width),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageLength, TiffDataType.Long, length),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.BitsPerSample, TiffDataType.Short, 8),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.PhotometricInterpretation, TiffDataType.Short, (ulong)TiffPhotometric.BlackIsZero),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.RowsPerStrip, TiffDataType.Long, length)
        };
    }

    static TiffTagValue[] CreateTileTags(ulong width, ulong length)
    {
        return new TiffTagValue[]
        {
            TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageWidth, TiffDataType.Long, width),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageLength, TiffDataType.Long, length),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.BitsPerSample, TiffDataType.Short, 8),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.PhotometricInterpretation, TiffDataType.Short, (ulong)TiffPhotometric.BlackIsZero),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.TileWidth, TiffDataType.Long, width),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.TileLength, TiffDataType.Long, length)
        };
    }

    static byte[] CreateBytes(int count)
    {
        byte[] bytes = new byte[count];
        int index = 0;
        while (index < bytes.Length)
        {
            bytes[index] = (byte)(index + 1);
            index = index + 1;
        }

        return bytes;
    }

    static string CreateTempPath()
    {
        string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".tif");
        DeleteIfExists(path);
        return path;
    }

    static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
