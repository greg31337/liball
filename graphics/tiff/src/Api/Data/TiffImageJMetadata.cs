namespace tiff;

/// <summary>Represents image J metadata extracted from or prepared for a TIFF file.</summary>
public class TiffImageJMetadata
{
    /// <summary>Indicates whether the image J is present.</summary>
    public bool HasImageJ;
    /// <summary>Indicates whether hyperstack applies.</summary>
    public bool IsHyperstack;
    /// <summary>Contains the version.</summary>
    public string Version;
    /// <summary>Contains the image count.</summary>
    public int ImageCount;
    /// <summary>Contains the channels.</summary>
    public int Channels;
    /// <summary>Contains the slices.</summary>
    public int Slices;
    /// <summary>Contains the frames.</summary>
    public int Frames;
    /// <summary>Contains the mode.</summary>
    public string Mode;
    /// <summary>Contains the unit.</summary>
    public string Unit;
    /// <summary>Contains the spacing.</summary>
    public string Spacing;
    /// <summary>Contains the raw description.</summary>
    public string RawDescription;
    /// <summary>Contains the original encoded bytes so the value can be preserved without semantic conversion.</summary>
    public byte[] RawBytes;
    /// <summary>Contains the entries.</summary>
    public TiffImageJMetadataEntry[] Entries;
    /// <summary>Contains the channel, depth, and time axes in ImageJ storage order.</summary>
    public TiffImageJAxis[] Axes;
    /// <summary>Contains parsed spatial and temporal calibration.</summary>
    public TiffImageJCalibration Calibration;
    /// <summary>Indicates whether ImageJ private metadata tags were decoded.</summary>
    public bool HasPrivateMetadata;
    /// <summary>Contains the ImageJ Info property.</summary>
    public string Info;
    /// <summary>Contains stack slice labels.</summary>
    public string[] SliceLabels;
    /// <summary>Contains per-channel display ranges.</summary>
    public TiffImageJDisplayRange[] DisplayRanges;
    /// <summary>Contains channel lookup tables.</summary>
    public byte[][] ChannelLuts;
    /// <summary>Contains a serialized ImageJ plot.</summary>
    public byte[] Plot;
    /// <summary>Contains a serialized ImageJ region of interest.</summary>
    public byte[] Roi;
    /// <summary>Contains serialized overlay regions of interest.</summary>
    public byte[][] Overlays;
    /// <summary>Contains ImageJ property strings.</summary>
    public string[] Properties;
    /// <summary>Contains private metadata blocks whose type is not recognized.</summary>
    public TiffImageJPrivateMetadataBlock[] UnrecognizedBlocks;
    /// <summary>Contains the decoded private metadata byte counts, including the header size.</summary>
    public int[] PrivateMetadataByteCounts;
    /// <summary>Contains the original ImageJ private metadata bytes.</summary>
    public byte[] RawPrivateMetadataBytes;

    /// <summary>Creates a new instance of the TIFF Image J Metadata class.</summary>
    public TiffImageJMetadata()
    {
        HasImageJ = false;
        IsHyperstack = false;
        Version = "";
        ImageCount = 0;
        Channels = 0;
        Slices = 0;
        Frames = 0;
        Mode = "";
        Unit = "";
        Spacing = "";
        RawDescription = "";
        RawBytes = new byte[0];
        Entries = new TiffImageJMetadataEntry[0];
        Axes = new TiffImageJAxis[0];
        Calibration = new TiffImageJCalibration();
        HasPrivateMetadata = false;
        Info = "";
        SliceLabels = new string[0];
        DisplayRanges = new TiffImageJDisplayRange[0];
        ChannelLuts = new byte[0][];
        Plot = new byte[0];
        Roi = new byte[0];
        Overlays = new byte[0][];
        Properties = new string[0];
        UnrecognizedBlocks = new TiffImageJPrivateMetadataBlock[0];
        PrivateMetadataByteCounts = new int[0];
        RawPrivateMetadataBytes = new byte[0];
    }

    /// <summary>Creates a new instance of the TIFF Image J Metadata class.</summary>
    /// <param name="hasImageJ">The has image J.</param>
    /// <param name="isHyperstack">Whether hyperstack applies.</param>
    /// <param name="version">The version.</param>
    /// <param name="imageCount">The image count.</param>
    /// <param name="channels">The channels.</param>
    /// <param name="slices">The slices.</param>
    /// <param name="frames">The frames.</param>
    /// <param name="mode">The mode.</param>
    /// <param name="unit">The unit.</param>
    /// <param name="spacing">The spacing.</param>
    /// <param name="rawDescription">The raw description.</param>
    /// <param name="rawBytes">The raw bytes.</param>
    /// <param name="entries">The entries.</param>
    internal static TiffImageJMetadata Create(bool hasImageJ, bool isHyperstack, string version, int imageCount, int channels, int slices, int frames, string mode, string unit, string spacing, string rawDescription, byte[] rawBytes, TiffImageJMetadataEntry[] entries)
    {
        TiffImageJMetadata value = new TiffImageJMetadata();
        value.Initialize(hasImageJ, isHyperstack, version, imageCount, channels, slices, frames, mode, unit, spacing, rawDescription, rawBytes, entries);
        return value;
    }

    internal void Initialize(bool hasImageJ, bool isHyperstack, string version, int imageCount, int channels, int slices, int frames, string mode, string unit, string spacing, string rawDescription, byte[] rawBytes, TiffImageJMetadataEntry[] entries)
    {
        HasImageJ = hasImageJ;
        IsHyperstack = isHyperstack;
        Version = TiffImageJMetadataEntry.NormalizeString(version);
        ImageCount = imageCount;
        Channels = channels;
        Slices = slices;
        Frames = frames;
        Mode = TiffImageJMetadataEntry.NormalizeString(mode);
        Unit = TiffImageJMetadataEntry.NormalizeString(unit);
        Spacing = TiffImageJMetadataEntry.NormalizeString(spacing);
        RawDescription = TiffImageJMetadataEntry.NormalizeString(rawDescription);
        RawBytes = TiffTagValue.CopyByteArray(rawBytes);
        Entries = CopyEntries(entries);
        if (hasImageJ)
        {
            Axes = CreateAxes(channels, slices, frames);
        }
        else
        {
            Axes = new TiffImageJAxis[0];
        }
        Calibration = new TiffImageJCalibration();
        Calibration.Unit = Unit;
    }

    /// <summary>Returns the number of available entry entries.</summary>
    /// <returns>The resulting int.</returns>
    internal int EntryCount()
    {
        return Entries.Length;
    }

    /// <summary>Returns the value.</summary>
    /// <param name="key">The key.</param>
    /// <returns>The requested value or model.</returns>
    internal string GetValue(string key)
    {
        int index = 0;
        while (index < Entries.Length)
        {
            if (Entries[index] != null && EqualsIgnoreCase(Entries[index].Key, key))
            {
                return Entries[index].Value;
            }

            index = index + 1;
        }

        return "";
    }

    /// <summary>Creates a detached copy whose mutable arrays and child models do not alias this instance.</summary>
    /// <returns>The resulting TIFF image J metadata.</returns>
    internal TiffImageJMetadata Copy()
    {
        TiffImageJMetadata copy = TiffImageJMetadata.Create(HasImageJ, IsHyperstack, Version, ImageCount, Channels, Slices, Frames, Mode, Unit, Spacing, RawDescription, RawBytes, Entries);
        copy.Axes = CopyAxes(Axes);
        copy.Calibration = Calibration.Copy();
        copy.HasPrivateMetadata = HasPrivateMetadata;
        copy.Info = TiffImageJMetadataEntry.NormalizeString(Info);
        copy.SliceLabels = CopyStrings(SliceLabels);
        copy.DisplayRanges = CopyDisplayRanges(DisplayRanges);
        copy.ChannelLuts = CopyByteArrays(ChannelLuts);
        copy.Plot = TiffTagValue.CopyByteArray(Plot);
        copy.Roi = TiffTagValue.CopyByteArray(Roi);
        copy.Overlays = CopyByteArrays(Overlays);
        copy.Properties = CopyStrings(Properties);
        copy.UnrecognizedBlocks = CopyPrivateBlocks(UnrecognizedBlocks);
        copy.PrivateMetadataByteCounts = CopyInts(PrivateMetadataByteCounts);
        copy.RawPrivateMetadataBytes = TiffTagValue.CopyByteArray(RawPrivateMetadataBytes);
        return copy;
    }

    internal static TiffImageJAxis[] CreateAxes(int channels, int slices, int frames)
    {
        TiffImageJAxis[] axes = new TiffImageJAxis[3];
        axes[0] = TiffImageJAxis.Create(TiffImageJAxisKind.Channel, NormalizeAxisSize(channels), 0);
        axes[1] = TiffImageJAxis.Create(TiffImageJAxisKind.Depth, NormalizeAxisSize(slices), 1);
        axes[2] = TiffImageJAxis.Create(TiffImageJAxisKind.Time, NormalizeAxisSize(frames), 2);
        return axes;
    }

    internal static int NormalizeAxisSize(int size)
    {
        if (size <= 0)
        {
            return 1;
        }

        return size;
    }

    internal static TiffImageJAxis[] CopyAxes(TiffImageJAxis[] axes)
    {
        if (axes == null)
        {
            return new TiffImageJAxis[0];
        }

        TiffImageJAxis[] copy = new TiffImageJAxis[axes.Length];
        int index = 0;
        while (index < axes.Length)
        {
            copy[index] = axes[index].Copy();
            index = index + 1;
        }

        return copy;
    }

    internal static string[] CopyStrings(string[] values)
    {
        if (values == null)
        {
            return new string[0];
        }

        string[] copy = new string[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            copy[index] = TiffImageJMetadataEntry.NormalizeString(values[index]);
            index = index + 1;
        }

        return copy;
    }

    internal static TiffImageJDisplayRange[] CopyDisplayRanges(TiffImageJDisplayRange[] ranges)
    {
        if (ranges == null)
        {
            return new TiffImageJDisplayRange[0];
        }

        TiffImageJDisplayRange[] copy = new TiffImageJDisplayRange[ranges.Length];
        int index = 0;
        while (index < ranges.Length)
        {
            copy[index] = ranges[index].Copy();
            index = index + 1;
        }

        return copy;
    }

    internal static byte[][] CopyByteArrays(byte[][] values)
    {
        if (values == null)
        {
            return new byte[0][];
        }

        byte[][] copy = new byte[values.Length][];
        int index = 0;
        while (index < values.Length)
        {
            copy[index] = TiffTagValue.CopyByteArray(values[index]);
            index = index + 1;
        }

        return copy;
    }

    internal static TiffImageJPrivateMetadataBlock[] CopyPrivateBlocks(TiffImageJPrivateMetadataBlock[] blocks)
    {
        if (blocks == null)
        {
            return new TiffImageJPrivateMetadataBlock[0];
        }

        TiffImageJPrivateMetadataBlock[] copy = new TiffImageJPrivateMetadataBlock[blocks.Length];
        int index = 0;
        while (index < blocks.Length)
        {
            copy[index] = blocks[index].Copy();
            index = index + 1;
        }

        return copy;
    }

    internal static int[] CopyInts(int[] values)
    {
        if (values == null)
        {
            return new int[0];
        }

        int[] copy = new int[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            copy[index] = values[index];
            index = index + 1;
        }

        return copy;
    }

    internal static TiffImageJMetadataEntry[] CopyEntries(TiffImageJMetadataEntry[] entries)
    {
        if (entries == null)
        {
            return new TiffImageJMetadataEntry[0];
        }

        TiffImageJMetadataEntry[] copy = new TiffImageJMetadataEntry[entries.Length];
        int index = 0;
        while (index < entries.Length)
        {
            if (entries[index] == null)
            {
                copy[index] = new TiffImageJMetadataEntry();
            }
            else
            {
                copy[index] = entries[index].Copy();
            }

            index = index + 1;
        }

        return copy;
    }

    internal static bool EqualsIgnoreCase(string left, string right)
    {
        string actualLeft = TiffImageJMetadataEntry.NormalizeString(left);
        string actualRight = TiffImageJMetadataEntry.NormalizeString(right);
        if (actualLeft.Length != actualRight.Length)
        {
            return false;
        }

        int index = 0;
        while (index < actualLeft.Length)
        {
            if (ToLowerAscii(actualLeft[index]) != ToLowerAscii(actualRight[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static char ToLowerAscii(char value)
    {
        if (value >= 'A' && value <= 'Z')
        {
            return (char)(value + 32);
        }

        return value;
    }
}
