namespace fake;

/// <summary>
/// Describes one explicit edge-case fixture without coercing its runtime value type.
/// </summary>
public class EdgeCaseRecord
{
    /// <summary>The broad category containing this case.</summary>
    public EdgeCaseCategory Category;

    /// <summary>A stable case identifier suitable for diagnostics.</summary>
    public string Name = string.Empty;

    /// <summary>The primary fixture value, including reference null.</summary>
    public object? Value;

    /// <summary>An optional contrasting or contradictory value.</summary>
    public object? RelatedValue;

    /// <summary>A concise explanation of the represented boundary or contradiction.</summary>
    public string Description = string.Empty;

    internal EdgeCaseRecord()
    {
        Category = EdgeCaseCategory.EmptyNullWhitespace;
        Name = string.Empty;
        Value = null;
        RelatedValue = null;
        Description = string.Empty;
    }
}
