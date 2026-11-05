namespace tiff;

/// <summary>Links an OME image and base IFD to one reduced-resolution SubIFD level.</summary>
public class TiffOmePyramidLevel
{
    /// <summary>Contains the zero-based OME image index.</summary>
    public int OmeImageIndex;
    /// <summary>Contains the zero-based base TIFF IFD index.</summary>
    public int BaseIfdIndex;
    /// <summary>Contains the pyramid level index, where zero identifies the base image.</summary>
    public int LevelIndex;
    /// <summary>Contains the level width.</summary>
    public ulong Width;
    /// <summary>Contains the level height.</summary>
    public ulong Length;

    /// <summary>Creates an empty OME pyramid relationship.</summary>
    public TiffOmePyramidLevel()
    {
        OmeImageIndex = 0;
        BaseIfdIndex = 0;
        LevelIndex = 0;
        Width = 0;
        Length = 0;
    }

    internal static TiffOmePyramidLevel Create(int omeImageIndex, int baseIfdIndex, int levelIndex, ulong width, ulong length)
    {
        TiffOmePyramidLevel value = new TiffOmePyramidLevel();
        value.OmeImageIndex = omeImageIndex;
        value.BaseIfdIndex = baseIfdIndex;
        value.LevelIndex = levelIndex;
        value.Width = width;
        value.Length = length;
        return value;
    }

    internal TiffOmePyramidLevel Copy()
    {
        return TiffOmePyramidLevel.Create(OmeImageIndex, BaseIfdIndex, LevelIndex, Width, Length);
    }
}
