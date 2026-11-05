namespace tiff;

/// <summary>Defines the tags and optional raw image segments used to write one TIFF image.</summary>
public class TiffImageDefinition
{
    /// <summary>Contains the tags.</summary>
    public TiffTagValue[] Tags;
    /// <summary>Contains the raw strips.</summary>
    public byte[][] RawStrips;
    /// <summary>Contains the raw tiles.</summary>
    public byte[][] RawTiles;

    /// <summary>Creates a new instance of the TIFF Image Definition class.</summary>
    public TiffImageDefinition()
    {
        Tags = new TiffTagValue[0];
        RawStrips = new byte[0][];
        RawTiles = new byte[0][];
    }

    /// <summary>Creates a new instance of the TIFF Image Definition class.</summary>
    /// <param name="tags">The tags.</param>
    internal static TiffImageDefinition Create(TiffTagValue[] tags)
    {
        TiffImageDefinition value = new TiffImageDefinition();
        value.Initialize(tags);
        return value;
    }

    internal void Initialize(TiffTagValue[] tags)
    {
        Tags = CopyTags(tags);
        RawStrips = new byte[0][];
        RawTiles = new byte[0][];
    }

    /// <summary>Creates a detached copy whose mutable arrays and child models do not alias this instance.</summary>
    /// <returns>The resulting TIFF image definition.</returns>
    internal TiffImageDefinition Copy()
    {
        TiffImageDefinition copy = new TiffImageDefinition();
        copy.Tags = CopyTags(Tags);
        copy.RawStrips = CopyBytesArray(RawStrips);
        copy.RawTiles = CopyBytesArray(RawTiles);
        return copy;
    }

    /// <summary>Sets tag, replacing the previous value at the same logical position.</summary>
    /// <param name="value">The value.</param>
    internal void SetTag(TiffTagValue value)
    {
        Tags = SetTagValue(Tags, value);
    }

    /// <summary>Removes every tag with the supplied identifier.</summary>
    /// <param name="tagId">The numeric TIFF tag identifier.</param>
    internal void RemoveTag(int tagId)
    {
        int count = 0;
        int index = 0;
        while (Tags != null && index < Tags.Length)
        {
            if (Tags[index] != null && Tags[index].TagId != tagId)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        TiffTagValue[] remaining = new TiffTagValue[count];
        index = 0;
        int destinationIndex = 0;
        while (Tags != null && index < Tags.Length)
        {
            if (Tags[index] != null && Tags[index].TagId != tagId)
            {
                remaining[destinationIndex] = Tags[index].Copy();
                destinationIndex = destinationIndex + 1;
            }

            index = index + 1;
        }

        Tags = remaining;
    }

    /// <summary>Sets raw strip, replacing the previous value at the same logical position.</summary>
    /// <param name="stripIndex">The zero-based strip index.</param>
    /// <param name="bytes">The bytes.</param>
    internal void SetRawStrip(int stripIndex, byte[] bytes)
    {
        RawStrips = SetBytes(RawStrips, stripIndex, bytes);
    }

    /// <summary>Sets raw tile, replacing the previous value at the same logical position.</summary>
    /// <param name="tileIndex">The zero-based tile index.</param>
    /// <param name="bytes">The bytes.</param>
    internal void SetRawTile(int tileIndex, byte[] bytes)
    {
        RawTiles = SetBytes(RawTiles, tileIndex, bytes);
    }

    internal static TiffTagValue[] SetTagValue(TiffTagValue[] values, TiffTagValue value)
    {
        TiffTagValue actualValue = value;
        if (actualValue == null)
        {
            actualValue = new TiffTagValue();
        }

        if (values == null)
        {
            TiffTagValue[] single = new TiffTagValue[1];
            single[0] = actualValue.Copy();
            return single;
        }

        int index = 0;
        while (index < values.Length)
        {
            if (values[index] != null && values[index].TagId == actualValue.TagId)
            {
                TiffTagValue[] replaced = CopyTags(values);
                replaced[index] = actualValue.Copy();
                return replaced;
            }

            index = index + 1;
        }

        TiffTagValue[] appended = new TiffTagValue[values.Length + 1];
        index = 0;
        while (index < values.Length)
        {
            if (values[index] == null)
            {
                appended[index] = new TiffTagValue();
            }
            else
            {
                appended[index] = values[index].Copy();
            }

            index = index + 1;
        }

        appended[appended.Length - 1] = actualValue.Copy();
        return appended;
    }

    internal static byte[][] SetBytes(byte[][] values, int valueIndex, byte[] bytes)
    {
        if (valueIndex < 0)
        {
            return CopyBytesArray(values);
        }

        int length = valueIndex + 1;
        if (values != null && values.Length > length)
        {
            length = values.Length;
        }

        byte[][] copy = new byte[length][];
        int index = 0;
        while (values != null && index < values.Length)
        {
            copy[index] = TiffTagValue.CopyByteArray(values[index]);
            index = index + 1;
        }

        while (index < copy.Length)
        {
            copy[index] = new byte[0];
            index = index + 1;
        }

        copy[valueIndex] = TiffTagValue.CopyByteArray(bytes);
        return copy;
    }

    internal static TiffTagValue[] CopyTags(TiffTagValue[] values)
    {
        if (values == null)
        {
            return new TiffTagValue[0];
        }

        TiffTagValue[] copy = new TiffTagValue[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == null)
            {
                copy[index] = new TiffTagValue();
            }
            else
            {
                copy[index] = values[index].Copy();
            }

            index = index + 1;
        }

        return copy;
    }

    internal static byte[][] CopyBytesArray(byte[][] values)
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
}
