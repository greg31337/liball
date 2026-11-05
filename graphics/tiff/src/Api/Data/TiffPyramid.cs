namespace tiff;

/// <summary>Represents pyramid used by the public TIFF API.</summary>
public class TiffPyramid
{
    /// <summary>Indicates whether the pyramid is present.</summary>
    public bool HasPyramid;
    /// <summary>Contains the parent image index.</summary>
    public int ParentImageIndex;
    /// <summary>Contains the base width.</summary>
    public ulong BaseWidth;
    /// <summary>Contains the base length.</summary>
    public ulong BaseLength;
    /// <summary>Contains the levels.</summary>
    public TiffPyramidLevel[] Levels;

    /// <summary>Creates a new instance of the TIFF Pyramid class.</summary>
    public TiffPyramid()
    {
        HasPyramid = false;
        ParentImageIndex = 0;
        BaseWidth = 0;
        BaseLength = 0;
        Levels = new TiffPyramidLevel[0];
    }

    /// <summary>Creates a new instance of the TIFF Pyramid class.</summary>
    /// <param name="hasPyramid">The has pyramid.</param>
    /// <param name="parentImageIndex">The parent image index.</param>
    /// <param name="baseWidth">The base width.</param>
    /// <param name="baseLength">The base length.</param>
    /// <param name="levels">The levels.</param>
    internal static TiffPyramid Create(bool hasPyramid, int parentImageIndex, ulong baseWidth, ulong baseLength, TiffPyramidLevel[] levels)
    {
        TiffPyramid value = new TiffPyramid();
        value.Initialize(hasPyramid, parentImageIndex, baseWidth, baseLength, levels);
        return value;
    }

    internal void Initialize(bool hasPyramid, int parentImageIndex, ulong baseWidth, ulong baseLength, TiffPyramidLevel[] levels)
    {
        HasPyramid = hasPyramid;
        ParentImageIndex = parentImageIndex;
        BaseWidth = baseWidth;
        BaseLength = baseLength;
        Levels = CopyLevels(levels);
    }

    /// <summary>Returns the number of available level entries.</summary>
    /// <returns>The resulting int.</returns>
    internal int LevelCount()
    {
        return Levels.Length;
    }

    /// <summary>Creates a detached copy whose mutable arrays and child models do not alias this instance.</summary>
    /// <returns>The resulting TIFF pyramid.</returns>
    internal TiffPyramid Copy()
    {
        return TiffPyramid.Create(HasPyramid, ParentImageIndex, BaseWidth, BaseLength, Levels);
    }

    internal static TiffPyramidLevel[] CopyLevels(TiffPyramidLevel[] levels)
    {
        if (levels == null)
        {
            return new TiffPyramidLevel[0];
        }

        TiffPyramidLevel[] copy = new TiffPyramidLevel[levels.Length];
        int index = 0;
        while (index < levels.Length)
        {
            if (levels[index] == null)
            {
                copy[index] = new TiffPyramidLevel();
            }
            else
            {
                copy[index] = levels[index].Copy();
            }

            index = index + 1;
        }

        return copy;
    }
}
