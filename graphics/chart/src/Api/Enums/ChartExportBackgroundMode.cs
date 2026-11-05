namespace chart;
/// <summary>Specifies how a unified export resolves the scene background.</summary>
public enum ChartExportBackgroundMode
{
    /// <summary>Retains background clear commands already present in the scene.</summary>
    PreserveScene = 0,
    /// <summary>Removes scene background clear commands so alpha remains transparent.</summary>
    Transparent = 1,
    /// <summary>Replaces scene background clear commands with the configured export color.</summary>
    SolidColor = 2
}
