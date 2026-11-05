namespace cbor;

/// <summary>Holds schema traversal filtering and naming policies.</summary>
public class CddlEmissionOptions
{
    /// <summary>Indicates whether standard-prelude rules are emitted.</summary>
    public bool IncludePreludeRules;
    /// <summary>Indicates whether normalized nodes are emitted.</summary>
    public bool IncludeNormalizedNodes = true;
    /// <summary>Indicates whether source text and ranges are supplied to the sink.</summary>
    public bool IncludeSourceContext = true;
    /// <summary>An optional root rule name used to restrict traversal.</summary>
    public string? RootRuleName;
    /// <summary>An optional prefix applied to emitted symbolic names.</summary>
    public string NamePrefix = "";
}
