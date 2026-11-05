namespace tiff;

internal class TiffWriterImageState
{
    internal TiffImageDefinition Definition;
    internal TiffWriterImageState[] Children;
    internal ulong DirectoryOffset;
    internal ulong NextIfdOffset;
    internal TiffIfdEntry[] Entries;
    internal byte[][] ValueBytes;
    internal ulong[] RawStripOffsets;
    internal ulong[] RawTileOffsets;
    internal bool IsConfigured;
    internal bool IsPyramidLevel;
    internal bool UsesConfiguredTiles;
    internal bool AllowsSparseSegments;
    internal int ConfiguredSegmentCount;
    internal TiffImageInfo ConfiguredInfo;
    internal byte[][] DecodedStrips;
    internal byte[][] DecodedTiles;
    internal bool HasExifMetadata;
    internal TiffTagValue[] ExifValues;
    internal ulong ExifDirectoryOffset;
    internal TiffIfdEntry[] ExifEntries;
    internal byte[][] ExifValueBytes;
    internal bool HasGpsMetadata;
    internal TiffTagValue[] GpsValues;
    internal ulong GpsDirectoryOffset;
    internal TiffIfdEntry[] GpsEntries;
    internal byte[][] GpsValueBytes;

    internal TiffWriterImageState()
    {
        Definition = new TiffImageDefinition();
        Children = new TiffWriterImageState[0];
        DirectoryOffset = 0;
        NextIfdOffset = 0;
        Entries = new TiffIfdEntry[0];
        ValueBytes = new byte[0][];
        RawStripOffsets = new ulong[0];
        RawTileOffsets = new ulong[0];
        IsConfigured = false;
        IsPyramidLevel = false;
        UsesConfiguredTiles = false;
        AllowsSparseSegments = false;
        ConfiguredSegmentCount = 0;
        ConfiguredInfo = new TiffImageInfo();
        DecodedStrips = new byte[0][];
        DecodedTiles = new byte[0][];
        HasExifMetadata = false;
        ExifValues = new TiffTagValue[0];
        ExifDirectoryOffset = 0;
        ExifEntries = new TiffIfdEntry[0];
        ExifValueBytes = new byte[0][];
        HasGpsMetadata = false;
        GpsValues = new TiffTagValue[0];
        GpsDirectoryOffset = 0;
        GpsEntries = new TiffIfdEntry[0];
        GpsValueBytes = new byte[0][];
    }

    internal static TiffWriterImageState Create(TiffImageDefinition definition)
    {
        TiffWriterImageState value = new TiffWriterImageState();
        value.Initialize(definition);
        return value;
    }

    internal void Initialize(TiffImageDefinition definition)
    {
        if (definition == null)
        {
            Definition = new TiffImageDefinition();
        }
        else
        {
            Definition = definition.Copy();
        }

        Children = new TiffWriterImageState[0];
        DirectoryOffset = 0;
        NextIfdOffset = 0;
        Entries = new TiffIfdEntry[0];
        ValueBytes = new byte[0][];
        RawStripOffsets = new ulong[0];
        RawTileOffsets = new ulong[0];
        IsConfigured = false;
        IsPyramidLevel = false;
        UsesConfiguredTiles = false;
        AllowsSparseSegments = false;
        ConfiguredSegmentCount = 0;
        ConfiguredInfo = new TiffImageInfo();
        DecodedStrips = new byte[0][];
        DecodedTiles = new byte[0][];
        HasExifMetadata = false;
        ExifValues = new TiffTagValue[0];
        ExifDirectoryOffset = 0;
        ExifEntries = new TiffIfdEntry[0];
        ExifValueBytes = new byte[0][];
        HasGpsMetadata = false;
        GpsValues = new TiffTagValue[0];
        GpsDirectoryOffset = 0;
        GpsEntries = new TiffIfdEntry[0];
        GpsValueBytes = new byte[0][];
    }

    internal TiffWriterImageState Copy()
    {
        TiffWriterImageState copy = TiffWriterImageState.Create(Definition);
        copy.Children = CopyStates(Children);
        copy.DirectoryOffset = DirectoryOffset;
        copy.NextIfdOffset = NextIfdOffset;
        copy.Entries = TiffDirectory.CopyEntries(Entries);
        copy.ValueBytes = TiffImageDefinition.CopyBytesArray(ValueBytes);
        copy.RawStripOffsets = TiffTagValue.CopyUnsignedArray(RawStripOffsets);
        copy.RawTileOffsets = TiffTagValue.CopyUnsignedArray(RawTileOffsets);
        copy.IsConfigured = IsConfigured;
        copy.IsPyramidLevel = IsPyramidLevel;
        copy.UsesConfiguredTiles = UsesConfiguredTiles;
        copy.AllowsSparseSegments = AllowsSparseSegments;
        copy.ConfiguredSegmentCount = ConfiguredSegmentCount;
        copy.ConfiguredInfo = ConfiguredInfo.Copy();
        copy.DecodedStrips = TiffImageDefinition.CopyBytesArray(DecodedStrips);
        copy.DecodedTiles = TiffImageDefinition.CopyBytesArray(DecodedTiles);
        copy.HasExifMetadata = HasExifMetadata;
        copy.ExifValues = TiffExifMetadata.CopyValues(ExifValues);
        copy.ExifDirectoryOffset = ExifDirectoryOffset;
        copy.ExifEntries = TiffDirectory.CopyEntries(ExifEntries);
        copy.ExifValueBytes = TiffImageDefinition.CopyBytesArray(ExifValueBytes);
        copy.HasGpsMetadata = HasGpsMetadata;
        copy.GpsValues = TiffExifMetadata.CopyValues(GpsValues);
        copy.GpsDirectoryOffset = GpsDirectoryOffset;
        copy.GpsEntries = TiffDirectory.CopyEntries(GpsEntries);
        copy.GpsValueBytes = TiffImageDefinition.CopyBytesArray(GpsValueBytes);
        return copy;
    }

    internal static TiffWriterImageState[] CopyStates(TiffWriterImageState[] states)
    {
        if (states == null)
        {
            return new TiffWriterImageState[0];
        }

        TiffWriterImageState[] copy = new TiffWriterImageState[states.Length];
        int index = 0;
        while (index < states.Length)
        {
            if (states[index] == null)
            {
                copy[index] = new TiffWriterImageState();
            }
            else
            {
                copy[index] = states[index].Copy();
            }

            index = index + 1;
        }

        return copy;
    }
}
