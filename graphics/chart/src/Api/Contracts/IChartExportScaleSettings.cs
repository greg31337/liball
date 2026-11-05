namespace chart;
/// <summary>Defines device-pixel and export scaling used to convert logical chart sizes to output sizes.</summary>
public interface IChartExportScaleSettings
{
    /// <summary>Returns the combined device and export scale.</summary>
    /// <returns>The positive effective scale.</returns>
    double EffectiveScale();
    /// <summary>Scales a logical width to a rounded pixel width.</summary>
    /// <param name = "width">The positive logical width.</param>
    /// <returns>The positive scaled pixel width.</returns>
    int ScaleWidth(int width);
    /// <summary>Scales a logical height to a rounded pixel height.</summary>
    /// <param name = "height">The positive logical height.</param>
    /// <returns>The positive scaled pixel height.</returns>
    int ScaleHeight(int height);
    /// <summary>Scales a logical size without rounding.</summary>
    /// <param name = "size">The positive logical size.</param>
    /// <returns>The scaled size.</returns>
    ChartSize ScaleSize(ChartSize size);
    /// <summary>Replaces the device-pixel ratio and export scale together.</summary>
    /// <param name = "devicePixelRatio">The positive number of device pixels per logical pixel.</param>
    /// <param name = "exportScale">The positive output magnification.</param>
    void SetScales(double devicePixelRatio, double exportScale);
    /// <summary>Creates an independent copy.</summary>
    /// <returns>A new settings object with the same scale values.</returns>
    ChartExportScaleSettings Copy();
    /// <summary>Returns the device-pixel ratio.</summary>
    /// <returns>The current positive device-pixel ratio.</returns>
    double GetDevicePixelRatio();
    /// <summary>Returns the export scale.</summary>
    /// <returns>The current positive export scale.</returns>
    double GetExportScale();
}
