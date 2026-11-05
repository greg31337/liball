namespace tiff.Tests;

public sealed class TiffGeneratedFixtureDefinition
{
    public string FileName { get; }
    public string Description { get; }
    public TiffFileKind FileKind { get; }
    public bool IsTiled { get; }
    public byte[] Bytes { get; }
    public byte[]? ExpectedRawStrip { get; }
    public int ExpectedTileCount { get; }
    public int ExpectedRawTileIndex { get; }
    public byte[]? ExpectedRawTile { get; }

    public TiffGeneratedFixtureDefinition(
        string fileName,
        string description,
        TiffFileKind fileKind,
        bool isTiled,
        byte[] bytes,
        byte[]? expectedRawStrip,
        int expectedTileCount,
        int expectedRawTileIndex,
        byte[]? expectedRawTile)
    {
        FileName = fileName;
        Description = description;
        FileKind = fileKind;
        IsTiled = isTiled;
        Bytes = bytes;
        ExpectedRawStrip = expectedRawStrip;
        ExpectedTileCount = expectedTileCount;
        ExpectedRawTileIndex = expectedRawTileIndex;
        ExpectedRawTile = expectedRawTile;
    }
}

public static class TiffGeneratedFixtureFactory
{
    public static TiffGeneratedFixtureDefinition[] CreateAll()
    {
        return new TiffGeneratedFixtureDefinition[]
        {
            CreateBigTiffStrippedGray2x2(),
            CreateClassicStrippedGray2x2(),
            CreateClassicTiledGray2x2()
        };
    }

    public static TiffGeneratedFixtureDefinition CreateClassicStrippedGray2x2()
    {
        byte[] stripBytes = new byte[] { 1, 2, 3, 4 };
        string description = "classic stripped gray";

        return new TiffGeneratedFixtureDefinition(
            "classic-stripped-gray-2x2.tif",
            description,
            TiffFileKind.Classic,
            false,
            CreateStrippedGray2x2(TiffFileKind.Classic, description, stripBytes),
            stripBytes,
            0,
            -1,
            null);
    }

    public static TiffGeneratedFixtureDefinition CreateClassicTiledGray2x2()
    {
        byte[][] tileBytes = new byte[][]
        {
            new byte[] { 10 },
            new byte[] { 11 },
            new byte[] { 12 },
            new byte[] { 13 }
        };
        string description = "classic tiled gray";

        return new TiffGeneratedFixtureDefinition(
            "classic-tiled-gray-2x2.tif",
            description,
            TiffFileKind.Classic,
            true,
            CreateTiledGray2x2(TiffFileKind.Classic, description, tileBytes),
            null,
            tileBytes.Length,
            2,
            tileBytes[2]);
    }

    public static TiffGeneratedFixtureDefinition CreateBigTiffStrippedGray2x2()
    {
        byte[] stripBytes = new byte[] { 21, 22, 23, 24 };
        string description = "bigtiff stripped gray";

        return new TiffGeneratedFixtureDefinition(
            "bigtiff-stripped-gray-2x2.tif",
            description,
            TiffFileKind.BigTiff,
            false,
            CreateStrippedGray2x2(TiffFileKind.BigTiff, description, stripBytes),
            stripBytes,
            0,
            -1,
            null);
    }

    static byte[] CreateStrippedGray2x2(TiffFileKind fileKind, string description, byte[] stripBytes)
    {
        TiffWriter writer = CreateWriter(fileKind);
        int imageIndex = writer.AddImage(TiffImageDefinition.Create(CreateStripTags(description)));
        writer.WriteRawStrip(imageIndex, 0, stripBytes);
        writer.Flush();
        return writer.GetFileBytes();
    }

    static byte[] CreateTiledGray2x2(TiffFileKind fileKind, string description, byte[][] tileBytes)
    {
        TiffWriter writer = CreateWriter(fileKind);
        int imageIndex = writer.AddImage(TiffImageDefinition.Create(CreateTileTags(description)));

        int tileIndex = 0;
        while (tileIndex < tileBytes.Length)
        {
            writer.WriteRawTile(imageIndex, tileIndex, tileBytes[tileIndex]);
            tileIndex = tileIndex + 1;
        }

        writer.Flush();
        return writer.GetFileBytes();
    }

    static TiffWriter CreateWriter(TiffFileKind fileKind)
    {
        TiffWriteOptions options = new TiffWriteOptions();
        options.FileKind = fileKind;
        return TiffWriter.Create(options);
    }

    static TiffTagValue[] CreateStripTags(string description)
    {
        return new TiffTagValue[]
        {
            TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageWidth, TiffDataType.Long, 2),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageLength, TiffDataType.Long, 2),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.BitsPerSample, TiffDataType.Short, 8),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.PhotometricInterpretation, TiffDataType.Short, (ulong)TiffPhotometric.BlackIsZero),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.RowsPerStrip, TiffDataType.Long, 2),
            TiffTagValue.CreateAscii(TiffTagId.ImageDescription, description)
        };
    }

    static TiffTagValue[] CreateTileTags(string description)
    {
        return new TiffTagValue[]
        {
            TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageWidth, TiffDataType.Long, 2),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageLength, TiffDataType.Long, 2),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.BitsPerSample, TiffDataType.Short, 8),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.PhotometricInterpretation, TiffDataType.Short, (ulong)TiffPhotometric.BlackIsZero),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.TileWidth, TiffDataType.Long, 1),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.TileLength, TiffDataType.Long, 1),
            TiffTagValue.CreateAscii(TiffTagId.ImageDescription, description)
        };
    }
}
