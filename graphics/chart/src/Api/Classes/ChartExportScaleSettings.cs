namespace chart;
/// <summary>Stores device-pixel and export scaling for logical chart sizes.</summary>
public class ChartExportScaleSettings : IChartExportScaleSettings
{
    internal double DevicePixelRatio;
    internal double ExportScale;

    /// <summary>Initializes export scale settings.</summary>
    /// <param name = "devicePixelRatio">The positive number of device pixels per logical pixel.</param>
    /// <param name = "exportScale">The positive output magnification.</param>
    public ChartExportScaleSettings(double devicePixelRatio, double exportScale)
    {
        ValidateScales(devicePixelRatio, exportScale);
        DevicePixelRatio = devicePixelRatio;
        ExportScale = exportScale;
    }

    /// <summary>Returns the combined device and export scale.</summary>
    /// <returns>The positive effective scale.</returns>
    public double EffectiveScale()
    {
        double scale = DevicePixelRatio * ExportScale;
        ChartRenderStyle.ValidatePositive(scale, "scale");
        return scale;
    }

    /// <summary>Scales a logical width to a rounded pixel width.</summary>
    /// <param name = "width">The positive logical width.</param>
    /// <returns>The positive scaled pixel width.</returns>
    public int ScaleWidth(int width)
    {
        return ScalePixelCount(width, "width");
    }

    /// <summary>Scales a logical height to a rounded pixel height.</summary>
    /// <param name = "height">The positive logical height.</param>
    /// <returns>The positive scaled pixel height.</returns>
    public int ScaleHeight(int height)
    {
        return ScalePixelCount(height, "height");
    }

    /// <summary>Scales a logical size without rounding.</summary>
    /// <param name = "size">The positive logical size.</param>
    /// <returns>The scaled size.</returns>
    public ChartSize ScaleSize(ChartSize size)
    {
        if (size == null)
        {
            throw new System.ArgumentNullException("size");
        }

        if (size.IsEmpty())
        {
            throw new System.ArgumentOutOfRangeException("size");
        }

        return size.Scale(EffectiveScale());
    }

    /// <summary>Replaces the device-pixel ratio and export scale together.</summary>
    /// <param name = "devicePixelRatio">The positive number of device pixels per logical pixel.</param>
    /// <param name = "exportScale">The positive output magnification.</param>
    public void SetScales(double devicePixelRatio, double exportScale)
    {
        ValidateScales(devicePixelRatio, exportScale);
        DevicePixelRatio = devicePixelRatio;
        ExportScale = exportScale;
    }

    /// <summary>Creates an independent copy.</summary>
    /// <returns>A new settings object with the same scale values.</returns>
    public ChartExportScaleSettings Copy()
    {
        return new ChartExportScaleSettings(DevicePixelRatio, ExportScale);
    }

    /// <summary>Returns the device-pixel ratio.</summary>
    /// <returns>The current positive device-pixel ratio.</returns>
    public double GetDevicePixelRatio()
    {
        return DevicePixelRatio;
    }

    /// <summary>Returns the export scale.</summary>
    /// <returns>The current positive export scale.</returns>
    public double GetExportScale()
    {
        return ExportScale;
    }

    internal int ScalePixelCount(int value, string name)
    {
        if (value <= 0)
        {
            throw new System.ArgumentOutOfRangeException(name);
        }

        double scaled = System.Math.Round(value * EffectiveScale());
        if (scaled < 1.0 || scaled > int.MaxValue)
        {
            throw new System.ArgumentOutOfRangeException(name);
        }

        return (int)scaled;
    }

    internal static void ValidateScales(double devicePixelRatio, double exportScale)
    {
        ChartRenderStyle.ValidatePositive(devicePixelRatio, "devicePixelRatio");
        ChartRenderStyle.ValidatePositive(exportScale, "exportScale");
        double effective = devicePixelRatio * exportScale;
        ChartRenderStyle.ValidatePositive(effective, "exportScale");
    }
}
