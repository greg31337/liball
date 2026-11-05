namespace tiff;

/// <summary>Represents one TIFF image file directory and its parsed entries.</summary>
public class TiffDirectory
{
    /// <summary>Identifies the zero-based image directory in the file.</summary>
    public int ImageIndex;
    /// <summary>Contains the byte offset measured from the start of the TIFF file.</summary>
    public ulong Offset;
    /// <summary>Contains the next IFD offset.</summary>
    public ulong NextIfdOffset;
    /// <summary>Contains the entries.</summary>
    public TiffIfdEntry[] Entries;

    /// <summary>Creates a new instance of the TIFF Directory class.</summary>
    public TiffDirectory()
    {
        ImageIndex = 0;
        Offset = 0;
        NextIfdOffset = 0;
        Entries = new TiffIfdEntry[0];
    }

    /// <summary>Creates a new instance of the TIFF Directory class.</summary>
    /// <param name="imageIndex">The zero-based image index.</param>
    /// <param name="offset">The offset.</param>
    /// <param name="nextIfdOffset">The next IFD offset.</param>
    /// <param name="entries">The entries.</param>
    internal static TiffDirectory Create(int imageIndex, ulong offset, ulong nextIfdOffset, TiffIfdEntry[] entries)
    {
        TiffDirectory value = new TiffDirectory();
        value.Initialize(imageIndex, offset, nextIfdOffset, entries);
        return value;
    }

    internal void Initialize(int imageIndex, ulong offset, ulong nextIfdOffset, TiffIfdEntry[] entries)
    {
        ImageIndex = imageIndex;
        Offset = offset;
        NextIfdOffset = nextIfdOffset;
        Entries = CopyEntries(entries);
    }

    /// <summary>Returns the number of available entry entries.</summary>
    /// <returns>The resulting int.</returns>
    internal int EntryCount()
    {
        return Entries.Length;
    }

    /// <summary>Creates a detached copy whose mutable arrays and child models do not alias this instance.</summary>
    /// <returns>The resulting TIFF directory.</returns>
    internal TiffDirectory Copy()
    {
        return TiffDirectory.Create(ImageIndex, Offset, NextIfdOffset, Entries);
    }

    /// <summary>Finds the entry, or returns the type's documented missing-value result.</summary>
    /// <param name="tagId">The numeric TIFF tag identifier.</param>
    /// <returns>A detached copy of the requested entry, or <c>null</c> when the tag is absent.</returns>
    internal TiffIfdEntry? FindEntry(int tagId)
    {
        int index = 0;
        while (index < Entries.Length)
        {
            if (Entries[index] != null && Entries[index].TagId == tagId)
            {
                return Entries[index].Copy();
            }

            index = index + 1;
        }

        return null;
    }

    internal static TiffIfdEntry[] CopyEntries(TiffIfdEntry[] entries)
    {
        if (entries == null)
        {
            return new TiffIfdEntry[0];
        }

        TiffIfdEntry[] copy = new TiffIfdEntry[entries.Length];
        int index = 0;
        while (index < entries.Length)
        {
            if (entries[index] == null)
            {
                copy[index] = new TiffIfdEntry();
            }
            else
            {
                copy[index] = entries[index].Copy();
            }

            index = index + 1;
        }

        return copy;
    }
}
