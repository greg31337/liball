namespace cbor;

/// <summary>Holds schema-driven value generation selection, strategy, and budgets.</summary>
public class CddlGenerationOptions
{
    /// <summary>An explicit root rule name, or null to use the compiled root.</summary>
    public string? RootRuleName;
    /// <summary>The sample selection strategy.</summary>
    public CddlGenerationStrategy Strategy = CddlGenerationStrategy.Representative;
    /// <summary>The deterministic seed used for representative choices.</summary>
    public int Seed;
    /// <summary>The enabled feature names.</summary>
    public string[] EnabledFeatures = [];
    /// <summary>An optional application generation hook.</summary>
    public ICddlGenerationHook? Hook;
    /// <summary>The maximum generated value nesting depth.</summary>
    public int MaxNestingDepth = 64;
    /// <summary>The maximum items in one generated collection.</summary>
    public int MaxCollectionItems = 1000;
    /// <summary>The maximum total generation steps.</summary>
    public int MaxSteps = 100000;
    /// <summary>The maximum candidate generation attempts.</summary>
    public int MaxAttempts = 1000;
}
