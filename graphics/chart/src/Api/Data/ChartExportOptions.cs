namespace chart;
/// <summary>Stores format, background, scale, metadata, encoder, and stream ownership options for unified exports.</summary>
public class ChartExportOptions
{
    /// <summary>The output format to produce.</summary>
    public ChartOutputFormat Format;
    /// <summary>The way scene background commands are preserved or replaced.</summary>
    public ChartExportBackgroundMode BackgroundMode;
    /// <summary>The replacement background used by solid-color exports.</summary>
    public ChartColor BackgroundColor;
    /// <summary>The additional device and export scale applied to the retained scene.</summary>
    public ChartExportScaleSettings Scale;
    /// <summary>The accessibility metadata used by SVG and PNG output.</summary>
    public ChartAccessibilityMetadata Accessibility;
    /// <summary>The BMP encoder options.</summary>
    public ChartBmpOptions Bmp;
    /// <summary>The PNG encoder and textual metadata options.</summary>
    public ChartPngOptions Png;
    /// <summary>The SVG serialization options.</summary>
    public ChartSvgOptions Svg;
    /// <summary>When <c>true</c>, a caller-provided destination stream remains open after export.</summary>
    public bool LeaveStreamOpen;

    /// <summary>Initializes PNG export options that preserve the scene background at its original scale and leave streams open.</summary>
    public ChartExportOptions()
    {
        Format = ChartOutputFormat.Png;
        BackgroundMode = ChartExportBackgroundMode.PreserveScene;
        BackgroundColor = ChartColor.White();
        Scale = new ChartExportScaleSettings(1.0, 1.0);
        Accessibility = new ChartAccessibilityMetadata();
        Bmp = new ChartBmpOptions();
        Png = new ChartPngOptions();
        Svg = new ChartSvgOptions();
        LeaveStreamOpen = true;
    }
}
