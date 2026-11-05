namespace tiff;

/// <summary>Represents sub IFD used by the public TIFF API.</summary>
public class TiffSubIfd
{
    /// <summary>Contains the parent image index.</summary>
    public int ParentImageIndex;
    /// <summary>Contains the parent offset.</summary>
    public ulong ParentOffset;
    /// <summary>Contains the sub IFD index.</summary>
    public int SubIfdIndex;
    /// <summary>Contains the depth.</summary>
    public int Depth;
    /// <summary>Contains the byte offset measured from the start of the TIFF file.</summary>
    public ulong Offset;
    /// <summary>Contains the directories.</summary>
    public TiffDirectory[] Directories;

    /// <summary>Creates a new instance of the TIFF Sub IFD class.</summary>
    public TiffSubIfd()
    {
        ParentImageIndex = 0;
        ParentOffset = 0;
        SubIfdIndex = 0;
        Depth = 0;
        Offset = 0;
        Directories = new TiffDirectory[0];
    }

    /// <summary>Creates a new instance of the TIFF Sub IFD class.</summary>
    /// <param name="parentImageIndex">The parent image index.</param>
    /// <param name="parentOffset">The parent offset.</param>
    /// <param name="subIfdIndex">The sub IFD index.</param>
    /// <param name="depth">The depth.</param>
    /// <param name="offset">The offset.</param>
    /// <param name="directories">The directories.</param>
    internal static TiffSubIfd Create(int parentImageIndex, ulong parentOffset, int subIfdIndex, int depth, ulong offset, TiffDirectory[] directories)
    {
        TiffSubIfd value = new TiffSubIfd();
        value.Initialize(parentImageIndex, parentOffset, subIfdIndex, depth, offset, directories);
        return value;
    }

    internal void Initialize(int parentImageIndex, ulong parentOffset, int subIfdIndex, int depth, ulong offset, TiffDirectory[] directories)
    {
        ParentImageIndex = parentImageIndex;
        ParentOffset = parentOffset;
        SubIfdIndex = subIfdIndex;
        Depth = depth;
        Offset = offset;
        Directories = TiffDirectoryReadResult.CopyDirectories(directories);
    }

    /// <summary>Creates a detached copy whose mutable arrays and child models do not alias this instance.</summary>
    /// <returns>The resulting TIFF sub IFD.</returns>
    internal TiffSubIfd Copy()
    {
        return TiffSubIfd.Create(ParentImageIndex, ParentOffset, SubIfdIndex, Depth, Offset, Directories);
    }
}
