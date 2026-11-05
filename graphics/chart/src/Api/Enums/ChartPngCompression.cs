namespace chart;
/// <summary>Defines built-in PNG DEFLATE compression settings.</summary>
public enum ChartPngCompression
{
    /// <summary>Prioritizes encoding speed.</summary>
    Fastest = 0,
    /// <summary>Balances encoded size and encoding time.</summary>
    Optimal = 1,
    /// <summary>Prioritizes the smallest encoded size.</summary>
    SmallestSize = 2,
    /// <summary>Stores DEFLATE blocks without compression.</summary>
    NoCompression = 3
}
