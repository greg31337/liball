namespace iso20022;

/// <summary>Describes an explicit policy for selecting a message version.</summary>
public class VersionSelector
{
    /// <summary>The selection policy.</summary>
    public VersionSelectionKind Kind;
    /// <summary>The exact or compatibility-baseline identifier when applicable.</summary>
    public MessageIdentifier? BaselineIdentifier;
    /// <summary>The inclusive minimum version for range selection.</summary>
    public string MinimumVersion = string.Empty;
    /// <summary>The inclusive maximum version for range selection.</summary>
    public string MaximumVersion = string.Empty;
}
