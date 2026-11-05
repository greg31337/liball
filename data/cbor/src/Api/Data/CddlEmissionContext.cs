namespace cbor;

/// <summary>Holds the current schema traversal position and emitted naming state.</summary>
public class CddlEmissionContext
{
    /// <summary>The compiled schema being emitted.</summary>
    public CddlSchema? Schema;
    /// <summary>The current rule.</summary>
    public CddlRule? Rule;
    /// <summary>The current node.</summary>
    public CddlNode? Node;
    /// <summary>The emission options.</summary>
    public CddlEmissionOptions Options = new CddlEmissionOptions();
    /// <summary>The stable zero-based rule index.</summary>
    public int RuleIndex = -1;
    /// <summary>The stable zero-based node index.</summary>
    public int NodeIndex = -1;
    /// <summary>The current traversal depth.</summary>
    public int Depth;
    /// <summary>The selected output name for the current rule or node.</summary>
    public string? OutputName;
    /// <summary>The source range associated with the current event.</summary>
    public CddlSourceSpan Span = new CddlSourceSpan();
}
