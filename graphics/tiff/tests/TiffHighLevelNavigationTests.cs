using System.Collections.Generic;
using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffHighLevelNavigationTests
{
    [Test]
    public void File_NavigatesTopLevelReducedImagesAndThumbnails()
    {
        TiffWriter writer = TiffWriter.Create();
        int baseIndex = writer.AddImage(TiffImageDefinition.Create(CreateStripTags(4, 4, false, "")));
        int reducedIndex = writer.AddImage(TiffImageDefinition.Create(CreateStripTags(2, 2, true, "")));
        writer.WriteRawStrip(baseIndex, 0, CreateBytes(16, 1));
        writer.WriteRawStrip(reducedIndex, 0, CreateBytes(4, 9));
        writer.Flush();
        ITiffFile file = TiffFile.OpenRead(writer.GetFileBytes());

        int reducedCount = file.GetReducedResolutionImageCount();
        int thumbnailCount = file.GetThumbnailCount();
        TiffImage reduced = file.GetReducedResolutionImage(0);
        TiffImage thumbnail = file.GetThumbnail(0);

        Assert.That(reducedCount, Is.EqualTo(1));
        Assert.That(thumbnailCount, Is.EqualTo(1));
        Assert.That(reduced.GetIndex(), Is.EqualTo(1));
        Assert.That(reduced.GetInfo().IsReducedResolution, Is.True);
        Assert.That(reduced.ReadEncodedStrip(0).Bytes, Is.EqualTo(CreateBytes(4, 9)));
        Assert.That(thumbnail.GetIndex(), Is.EqualTo(1));
    }

    [Test]
    public void File_NavigatesImageJSeriesByExplicitCztCoordinates()
    {
        string description = "ImageJ=1.54f\nimages=8\nchannels=2\nslices=2\nframes=2\nhyperstack=true\n";
        TiffWriter writer = TiffWriter.Create();
        int index = 0;
        while (index < 8)
        {
            string imageDescription = "";
            if (index == 0)
            {
                imageDescription = description;
            }

            int imageIndex = writer.AddImage(TiffImageDefinition.Create(CreateStripTags(1, 1, false, imageDescription)));
            writer.WriteRawStrip(imageIndex, 0, new byte[] { (byte)(index + 1) });
            index = index + 1;
        }

        writer.Flush();
        ITiffFile file = TiffFile.OpenRead(writer.GetFileBytes());

        TiffSeriesGroupResult group = file.GetSeriesGroup();
        TiffSeries series = file.GetSeriesByIndex(0);
        TiffImage plane = file.GetSeriesPlaneByCoordinates(0, 1, 1, 1);

        Assert.That(group.Success, Is.True);
        Assert.That(file.GetSeriesCount(), Is.EqualTo(1));
        Assert.That(series.SizeC, Is.EqualTo(2));
        Assert.That(series.SizeZ, Is.EqualTo(2));
        Assert.That(series.SizeT, Is.EqualTo(2));
        Assert.That(series.Planes[7].ChannelIndex, Is.EqualTo(1));
        Assert.That(series.Planes[7].ZIndex, Is.EqualTo(1));
        Assert.That(series.Planes[7].TimeIndex, Is.EqualTo(1));
        Assert.That(plane.GetIndex(), Is.EqualTo(7));
        Assert.That(plane.ReadEncodedStrip(0).Bytes, Is.EqualTo(new byte[] { 8 }));
    }

    [Test]
    public void Image_NavigatesPyramidLevelsAndReadsTileByCoordinate()
    {
        TiffWriter writer = TiffWriter.Create();
        int baseIndex = writer.AddImage(TiffImageDefinition.Create(CreateTileTags(4, 4, 2, 2, false)));
        TiffImageDefinition overviewDefinition = TiffImageDefinition.Create(CreateTileTags(2, 2, 2, 2, true));
        overviewDefinition.RawTiles = new byte[][] { new byte[0] };
        writer.AddSubImage(baseIndex, overviewDefinition);
        writer.WriteRawTile(baseIndex, 0, CreateBytes(4, 1));
        writer.WriteRawTile(baseIndex, 1, CreateBytes(4, 2));
        writer.WriteRawTile(baseIndex, 2, CreateBytes(4, 3));
        writer.WriteRawTile(baseIndex, 3, CreateBytes(4, 4));
        TiffDiagnostics flushDiagnostics = writer.Flush();
        ITiffImage image = TiffFile.OpenRead(writer.GetFileBytes()).GetImage(0);

        TiffPyramidDetectionResult pyramid = image.GetPyramid();
        TiffImage overview = image.GetPyramidLevel(1);
        TiffTileLookupResult baseLookup = image.LookupPyramidTile(0, 3, 3, 0, 0);
        TiffTileLookupResult overviewLookup = image.LookupPyramidTile(1, 1, 1, 0, 0);
        TiffRawTileReadResult baseTile = image.ReadPyramidTile(0, 3, 3, 0, 0);
        TiffRawTileReadResult overviewTile = image.ReadPyramidTile(1, 1, 1, 0, 0);

        Assert.That(flushDiagnostics.Count(), Is.EqualTo(0));
        Assert.That(pyramid.Success, Is.True);
        Assert.That(pyramid.Pyramid.HasPyramid, Is.True);
        Assert.That(image.GetPyramidLevelCount(), Is.EqualTo(2));
        Assert.That(overview.GetInfo().Width, Is.EqualTo(2));
        Assert.That(overview.GetInfo().IsReducedResolution, Is.True);
        Assert.That(baseLookup.Success, Is.True);
        Assert.That(baseLookup.Lookup.PyramidLevelIndex, Is.EqualTo(0));
        Assert.That(baseLookup.Lookup.TileIndex, Is.EqualTo(3));
        Assert.That(overviewLookup.Success, Is.True);
        Assert.That(overviewLookup.Lookup.PyramidLevelIndex, Is.EqualTo(1));
        Assert.That(overviewLookup.Lookup.TileIndex, Is.EqualTo(0));
        Assert.That(baseTile.Success, Is.True);
        Assert.That(baseTile.Bytes, Is.EqualTo(CreateBytes(4, 4)));
        Assert.That(overviewTile.Success, Is.True);
        Assert.That(overviewTile.IsSparse, Is.True);
        Assert.That(overviewTile.Bytes, Is.Empty);
    }

    static TiffTagValue[] CreateStripTags(ulong width, ulong length, bool reducedResolution, string description)
    {
        List<TiffTagValue> tags = CreateCommonTags(width, length, reducedResolution);
        tags.Add(TiffTagValue.CreateUnsignedScalar(TiffTagId.RowsPerStrip, TiffDataType.Long, length));
        if (description.Length > 0)
        {
            tags.Add(TiffTagValue.CreateAscii(TiffTagId.ImageDescription, description));
        }

        return tags.ToArray();
    }

    static TiffTagValue[] CreateTileTags(ulong width, ulong length, ulong tileWidth, ulong tileLength, bool reducedResolution)
    {
        List<TiffTagValue> tags = CreateCommonTags(width, length, reducedResolution);
        tags.Add(TiffTagValue.CreateUnsignedScalar(TiffTagId.TileWidth, TiffDataType.Long, tileWidth));
        tags.Add(TiffTagValue.CreateUnsignedScalar(TiffTagId.TileLength, TiffDataType.Long, tileLength));
        return tags.ToArray();
    }

    static List<TiffTagValue> CreateCommonTags(ulong width, ulong length, bool reducedResolution)
    {
        List<TiffTagValue> tags = new List<TiffTagValue>();
        tags.Add(TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageWidth, TiffDataType.Long, width));
        tags.Add(TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageLength, TiffDataType.Long, length));
        tags.Add(TiffTagValue.CreateUnsignedScalar(TiffTagId.BitsPerSample, TiffDataType.Short, 8));
        tags.Add(TiffTagValue.CreateUnsignedScalar(TiffTagId.Compression, TiffDataType.Short, (ulong)TiffCompression.None));
        tags.Add(TiffTagValue.CreateUnsignedScalar(TiffTagId.PhotometricInterpretation, TiffDataType.Short, (ulong)TiffPhotometric.BlackIsZero));
        tags.Add(TiffTagValue.CreateUnsignedScalar(TiffTagId.SamplesPerPixel, TiffDataType.Short, 1));
        tags.Add(TiffTagValue.CreateUnsignedScalar(TiffTagId.PlanarConfiguration, TiffDataType.Short, (ulong)TiffPlanarConfiguration.Contiguous));
        if (reducedResolution)
        {
            tags.Add(TiffTagValue.CreateUnsignedScalar(TiffTagId.NewSubfileType, TiffDataType.Long, 1));
        }

        return tags;
    }

    static byte[] CreateBytes(int count, byte value)
    {
        byte[] bytes = new byte[count];
        int index = 0;
        while (index < bytes.Length)
        {
            bytes[index] = value;
            index = index + 1;
        }

        return bytes;
    }
}
