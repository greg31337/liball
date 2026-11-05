namespace chart;
/// <summary>Stores deterministic textual metadata for PNG output.</summary>
public class ChartPngMetadata
{
    /// <summary>The image title.</summary>
    public string Title;
    /// <summary>The image author.</summary>
    public string Author;
    /// <summary>The image description.</summary>
    public string Description;
    /// <summary>The copyright notice.</summary>
    public string Copyright;
    /// <summary>The producing software name.</summary>
    public string Software;
    /// <summary>Initializes empty PNG metadata.</summary>
    public ChartPngMetadata()
    {
        Title = "";
        Author = "";
        Description = "";
        Copyright = "";
        Software = "";
    }
}
