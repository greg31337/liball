namespace tiff;

/// <summary>Contains ImageJ spatial and temporal calibration values.</summary>
public class TiffImageJCalibration
{
    /// <summary>Contains the spatial unit.</summary>
    public string Unit;
    /// <summary>Contains the calibrated pixel width when declared.</summary>
    public double PixelWidth;
    /// <summary>Contains the calibrated pixel height when declared.</summary>
    public double PixelHeight;
    /// <summary>Contains the calibrated slice spacing.</summary>
    public double PixelDepth;
    /// <summary>Contains the frame interval.</summary>
    public double FrameInterval;
    /// <summary>Contains the time unit.</summary>
    public string TimeUnit;
    /// <summary>Contains the frames-per-second value.</summary>
    public double FramesPerSecond;
    /// <summary>Contains the X-axis origin.</summary>
    public double XOrigin;
    /// <summary>Contains the Y-axis origin.</summary>
    public double YOrigin;
    /// <summary>Contains the Z-axis origin.</summary>
    public double ZOrigin;

    /// <summary>Creates an empty ImageJ calibration.</summary>
    public TiffImageJCalibration()
    {
        Unit = "";
        PixelWidth = 0;
        PixelHeight = 0;
        PixelDepth = 0;
        FrameInterval = 0;
        TimeUnit = "";
        FramesPerSecond = 0;
        XOrigin = 0;
        YOrigin = 0;
        ZOrigin = 0;
    }

    internal TiffImageJCalibration Copy()
    {
        TiffImageJCalibration copy = new TiffImageJCalibration();
        copy.Unit = TiffImageJMetadataEntry.NormalizeString(Unit);
        copy.PixelWidth = PixelWidth;
        copy.PixelHeight = PixelHeight;
        copy.PixelDepth = PixelDepth;
        copy.FrameInterval = FrameInterval;
        copy.TimeUnit = TiffImageJMetadataEntry.NormalizeString(TimeUnit);
        copy.FramesPerSecond = FramesPerSecond;
        copy.XOrigin = XOrigin;
        copy.YOrigin = YOrigin;
        copy.ZOrigin = ZOrigin;
        return copy;
    }
}
