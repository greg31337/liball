namespace chart;

/// <summary>Specifies which rows a key join retains.</summary>
public enum ChartJoinKind
{
    /// <summary>Retains only rows whose keys occur in both tables.</summary>
    Inner = 0,
    /// <summary>Retains every left row and its matching right rows.</summary>
    Left = 1,
    /// <summary>Retains every right row and its matching left rows.</summary>
    Right = 2,
    /// <summary>Retains every row from both tables.</summary>
    Full = 3
}
