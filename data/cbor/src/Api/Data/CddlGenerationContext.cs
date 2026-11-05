namespace cbor;

/// <summary>Holds the current schema, source, and remaining budgets for generation hooks.</summary>
public class CddlGenerationContext
{
    /// <summary>The compiled schema being generated.</summary>
    public CddlSchema? Schema;
    /// <summary>The selected rule.</summary>
    public CddlRule? Rule;
    /// <summary>The current normalized or source node.</summary>
    public CddlNode? Node;
    /// <summary>The generation options.</summary>
    public CddlGenerationOptions Options = new CddlGenerationOptions();
    /// <summary>The current generated-value nesting depth.</summary>
    public int Depth;
    /// <summary>The remaining generation steps.</summary>
    public int RemainingSteps;
    /// <summary>The remaining candidate attempts.</summary>
    public int RemainingAttempts;
    /// <summary>The source range associated with the current node.</summary>
    public CddlSourceSpan Span = new CddlSourceSpan();
}
