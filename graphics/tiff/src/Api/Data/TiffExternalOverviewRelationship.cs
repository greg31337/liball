namespace tiff;

/// <summary>Describes the relationship between one base image and one caller-supplied external overview image.</summary>
public class TiffExternalOverviewRelationship
{
    /// <summary>Contains the zero-based overview level, ordered from largest to smallest external image.</summary>
    public int LevelIndex;
    /// <summary>Contains the base image index in its TIFF file.</summary>
    public int BaseImageIndex;
    /// <summary>Contains the zero-based index in the caller-supplied path or stream array.</summary>
    public int SourceIndex;
    /// <summary>Contains the external image index in its TIFF file.</summary>
    public int SourceImageIndex;
    /// <summary>Indicates whether the external source was supplied as a path.</summary>
    public bool IsPathSource;
    /// <summary>Contains the exact caller-supplied path, or an empty string for a stream source.</summary>
    public string SourcePath;
    /// <summary>Contains the base image width.</summary>
    public ulong BaseWidth;
    /// <summary>Contains the base image length.</summary>
    public ulong BaseLength;
    /// <summary>Contains the external overview width.</summary>
    public ulong OverviewWidth;
    /// <summary>Contains the external overview length.</summary>
    public ulong OverviewLength;
    /// <summary>Contains the base-to-overview horizontal reduction factor.</summary>
    public double HorizontalReduction;
    /// <summary>Contains the base-to-overview vertical reduction factor.</summary>
    public double VerticalReduction;
    /// <summary>Indicates whether the external image carries a reduced-resolution subfile marker.</summary>
    public bool IsReducedResolution;

    /// <summary>Creates an empty external overview relationship.</summary>
    public TiffExternalOverviewRelationship()
    {
        LevelIndex = 0;
        BaseImageIndex = 0;
        SourceIndex = 0;
        SourceImageIndex = 0;
        IsPathSource = false;
        SourcePath = "";
        BaseWidth = 0;
        BaseLength = 0;
        OverviewWidth = 0;
        OverviewLength = 0;
        HorizontalReduction = 0d;
        VerticalReduction = 0d;
        IsReducedResolution = false;
    }

    internal static TiffExternalOverviewRelationship Create(int baseImageIndex, int sourceIndex, int sourceImageIndex, bool isPathSource, string sourcePath, TiffImageInfo baseInfo, TiffImageInfo overviewInfo)
    {
        TiffExternalOverviewRelationship value = new TiffExternalOverviewRelationship();
        value.BaseImageIndex = baseImageIndex;
        value.SourceIndex = sourceIndex;
        value.SourceImageIndex = sourceImageIndex;
        value.IsPathSource = isPathSource;
        if (sourcePath != null)
        {
            value.SourcePath = sourcePath;
        }

        value.BaseWidth = baseInfo.Width;
        value.BaseLength = baseInfo.Length;
        value.OverviewWidth = overviewInfo.Width;
        value.OverviewLength = overviewInfo.Length;
        value.HorizontalReduction = (double)baseInfo.Width / (double)overviewInfo.Width;
        value.VerticalReduction = (double)baseInfo.Length / (double)overviewInfo.Length;
        value.IsReducedResolution = overviewInfo.IsReducedResolution;
        return value;
    }

    internal TiffExternalOverviewRelationship Copy()
    {
        TiffExternalOverviewRelationship value = new TiffExternalOverviewRelationship();
        value.LevelIndex = LevelIndex;
        value.BaseImageIndex = BaseImageIndex;
        value.SourceIndex = SourceIndex;
        value.SourceImageIndex = SourceImageIndex;
        value.IsPathSource = IsPathSource;
        value.SourcePath = SourcePath;
        value.BaseWidth = BaseWidth;
        value.BaseLength = BaseLength;
        value.OverviewWidth = OverviewWidth;
        value.OverviewLength = OverviewLength;
        value.HorizontalReduction = HorizontalReduction;
        value.VerticalReduction = VerticalReduction;
        value.IsReducedResolution = IsReducedResolution;
        return value;
    }
}
