namespace chart;
/// <summary>Stores BMP bit depth, row orientation, and resolution settings.</summary>
public class ChartBmpOptions
{
    /// <summary>The output pixel bit depth.</summary>
    public ChartBmpBitDepth BitDepth;
    /// <summary>The order in which image rows are stored.</summary>
    public ChartBmpRowOrientation RowOrientation;
    /// <summary>The nonnegative horizontal resolution in pixels per meter.</summary>
    public int HorizontalPixelsPerMeter;
    /// <summary>The nonnegative vertical resolution in pixels per meter.</summary>
    public int VerticalPixelsPerMeter;
    /// <summary>Initializes 32-bit top-down BMP options with unspecified resolution.</summary>
    public ChartBmpOptions()
    {
        BitDepth = ChartBmpBitDepth.Rgba32;
        RowOrientation = ChartBmpRowOrientation.TopDown;
        HorizontalPixelsPerMeter = 0;
        VerticalPixelsPerMeter = 0;
    }
}
