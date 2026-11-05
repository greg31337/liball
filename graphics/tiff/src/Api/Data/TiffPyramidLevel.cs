namespace tiff;

/// <summary>Represents pyramid level used by the public TIFF API.</summary>
public class TiffPyramidLevel
{
    /// <summary>Contains the level index.</summary>
    public int LevelIndex;
    /// <summary>Contains the parent image index.</summary>
    public int ParentImageIndex;
    /// <summary>Contains the sub IFD index.</summary>
    public int SubIfdIndex;
    /// <summary>Contains the source image index.</summary>
    public int SourceImageIndex;
    /// <summary>Contains the image or region width in pixels.</summary>
    public ulong Width;
    /// <summary>Contains the image or region height in rows.</summary>
    public ulong Length;
    /// <summary>Indicates whether reduced resolution applies.</summary>
    public bool IsReducedResolution;
    /// <summary>Contains the directory.</summary>
    public TiffDirectory Directory;

    /// <summary>Creates a new instance of the TIFF Pyramid Level class.</summary>
    public TiffPyramidLevel()
    {
        LevelIndex = 0;
        ParentImageIndex = 0;
        SubIfdIndex = 0;
        SourceImageIndex = 0;
        Width = 0;
        Length = 0;
        IsReducedResolution = false;
        Directory = new TiffDirectory();
    }

    /// <summary>Creates a new instance of the TIFF Pyramid Level class.</summary>
    /// <param name="levelIndex">The level index.</param>
    /// <param name="parentImageIndex">The parent image index.</param>
    /// <param name="subIfdIndex">The sub IFD index.</param>
    /// <param name="sourceImageIndex">The source image index.</param>
    /// <param name="width">The width.</param>
    /// <param name="length">The length.</param>
    /// <param name="isReducedResolution">Whether reduced resolution applies.</param>
    /// <param name="directory">The TIFF directory whose tags describe the operation.</param>
    internal static TiffPyramidLevel Create(int levelIndex, int parentImageIndex, int subIfdIndex, int sourceImageIndex, ulong width, ulong length, bool isReducedResolution, TiffDirectory directory)
    {
        TiffPyramidLevel value = new TiffPyramidLevel();
        value.Initialize(levelIndex, parentImageIndex, subIfdIndex, sourceImageIndex, width, length, isReducedResolution, directory);
        return value;
    }

    internal void Initialize(int levelIndex, int parentImageIndex, int subIfdIndex, int sourceImageIndex, ulong width, ulong length, bool isReducedResolution, TiffDirectory directory)
    {
        LevelIndex = levelIndex;
        ParentImageIndex = parentImageIndex;
        SubIfdIndex = subIfdIndex;
        SourceImageIndex = sourceImageIndex;
        Width = width;
        Length = length;
        IsReducedResolution = isReducedResolution;
        if (directory == null)
        {
            Directory = new TiffDirectory();
        }
        else
        {
            Directory = directory.Copy();
        }
    }

    /// <summary>Creates a detached copy whose mutable arrays and child models do not alias this instance.</summary>
    /// <returns>The resulting TIFF pyramid level.</returns>
    internal TiffPyramidLevel Copy()
    {
        return TiffPyramidLevel.Create(LevelIndex, ParentImageIndex, SubIfdIndex, SourceImageIndex, Width, Length, IsReducedResolution, Directory);
    }
}
