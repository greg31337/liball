namespace iso20022;

/// <summary>Records one observable namespace repair.</summary>
public class NamespaceRepair
{
    /// <summary>The kind of repair.</summary>
    public NamespaceRepairKind Kind;
    /// <summary>The value before this repair.</summary>
    public string OriginalValue = string.Empty;
    /// <summary>The value after this repair.</summary>
    public string NormalizedValue = string.Empty;
}
