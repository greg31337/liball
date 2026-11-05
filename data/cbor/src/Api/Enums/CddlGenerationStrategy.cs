namespace cbor;

/// <summary>Selects how a schema-driven CDDL sample is chosen.</summary>
public enum CddlGenerationStrategy
{
    /// <summary>Produces the smallest practical matching value.</summary>
    Minimal = 0,
    /// <summary>Prefers boundary values and occurrence limits.</summary>
    Boundary = 1,
    /// <summary>Produces a representative value using the configured seed.</summary>
    Representative = 2
}
