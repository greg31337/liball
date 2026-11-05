namespace chart;
/// <summary>Specifies how scale domains are shared between subplots.</summary>
public enum ChartScaleSharing
{
    /// <summary>Uses a separate domain for every subplot.</summary>
    Independent = 0,
    /// <summary>Uses one domain shared by every subplot.</summary>
    Shared = 1,
    /// <summary>Shares domains between subplots with the same nonempty group identifier.</summary>
    Grouped = 2
}
