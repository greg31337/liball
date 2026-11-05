namespace pdf;

/// <summary>
/// Specifies how a table column width is resolved.
/// </summary>
public enum PdfTableColumnSizing
{
    /// <summary>Uses the configured value as an exact width.</summary>
    Fixed = 0,
    /// <summary>Shares remaining available width using the configured positive weight.</summary>
    Proportional = 1,
    /// <summary>Uses the widest measured single-column cell content.</summary>
    Auto = 2,
    /// <summary>Uses measured content width but never less than the configured value.</summary>
    Minimum = 3,
    /// <summary>Uses measured content width but never more than the configured value.</summary>
    Maximum = 4
}
