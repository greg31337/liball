namespace pptx;


/// <summary>
/// Provides connector type identifiers and maps them to PresentationML preset geometry names.
/// </summary>
public static class PptxConnector
{
    /// <summary>
    /// Identifies the straight value accepted by the API.
    /// </summary>
    public const string Straight = "straight";
    /// <summary>
    /// Identifies the elbow value accepted by the API.
    /// </summary>
    public const string Elbow = "elbow";
    /// <summary>
    /// Identifies the curved value accepted by the API.
    /// </summary>
    public const string Curved = "curved";
    /// <summary>
    /// Identifies the straight preset value accepted by the API.
    /// </summary>
    public const string StraightPreset = "straightConnector1";
    /// <summary>
    /// Identifies the elbow preset value accepted by the API.
    /// </summary>
    public const string ElbowPreset = "bentConnector3";
    /// <summary>
    /// Identifies the curved preset value accepted by the API.
    /// </summary>
    public const string CurvedPreset = "curvedConnector3";
}

