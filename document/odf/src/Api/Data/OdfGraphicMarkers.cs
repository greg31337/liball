namespace odf;

/// <summary>
/// Describes the markers attached to the ends of a graphic line.
/// </summary>
public class OdfGraphicMarkers
{
    /// <summary>The starting marker name.</summary>
    public string StartName = "";
    /// <summary>The ending marker name.</summary>
    public string EndName = "";
    /// <summary>The starting marker width.</summary>
    public string StartWidth = "";
    /// <summary>The ending marker width.</summary>
    public string EndWidth = "";
    /// <summary>The preserved starting-marker centering boolean.</summary>
    public string StartCenterValue = "";
    /// <summary>The preserved ending-marker centering boolean.</summary>
    public string EndCenterValue = "";
}
