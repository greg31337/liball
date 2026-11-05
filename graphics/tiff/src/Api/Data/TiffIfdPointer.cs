namespace tiff;

/// <summary>Represents IFD pointer used by the public TIFF API.</summary>
public class TiffIfdPointer
{
    /// <summary>Contains the parent image index.</summary>
    public int ParentImageIndex;
    /// <summary>Contains the parent offset.</summary>
    public ulong ParentOffset;
    /// <summary>Identifies the TIFF tag.</summary>
    public int TagId;
    /// <summary>Contains the depth.</summary>
    public int Depth;
    /// <summary>Contains the byte offset measured from the start of the TIFF file.</summary>
    public ulong Offset;
    /// <summary>Contains the directories.</summary>
    public TiffDirectory[] Directories;

    /// <summary>Creates a new instance of the TIFF IFD Pointer class.</summary>
    public TiffIfdPointer()
    {
        ParentImageIndex = 0;
        ParentOffset = 0;
        TagId = 0;
        Depth = 0;
        Offset = 0;
        Directories = new TiffDirectory[0];
    }

    /// <summary>Creates a new instance of the TIFF IFD Pointer class.</summary>
    /// <param name="parentImageIndex">The parent image index.</param>
    /// <param name="parentOffset">The parent offset.</param>
    /// <param name="tagId">The numeric TIFF tag identifier.</param>
    /// <param name="depth">The depth.</param>
    /// <param name="offset">The offset.</param>
    /// <param name="directories">The directories.</param>
    internal static TiffIfdPointer Create(int parentImageIndex, ulong parentOffset, int tagId, int depth, ulong offset, TiffDirectory[] directories)
    {
        TiffIfdPointer value = new TiffIfdPointer();
        value.Initialize(parentImageIndex, parentOffset, tagId, depth, offset, directories);
        return value;
    }

    internal void Initialize(int parentImageIndex, ulong parentOffset, int tagId, int depth, ulong offset, TiffDirectory[] directories)
    {
        ParentImageIndex = parentImageIndex;
        ParentOffset = parentOffset;
        TagId = tagId;
        Depth = depth;
        Offset = offset;
        Directories = TiffDirectoryReadResult.CopyDirectories(directories);
    }

    /// <summary>Creates a detached copy whose mutable arrays and child models do not alias this instance.</summary>
    /// <returns>The resulting TIFF IFD pointer.</returns>
    internal TiffIfdPointer Copy()
    {
        return TiffIfdPointer.Create(ParentImageIndex, ParentOffset, TagId, Depth, Offset, Directories);
    }
}
