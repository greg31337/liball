namespace cbor;

/// <summary>Resolves application-provided CDDL rules outside the source document.</summary>
public interface ICddlRuleResolver
{
    /// <summary>Finds an external rule by name and expected rule kind.</summary>
    /// <param name="name">The requested rule name.</param>
    /// <param name="kind">The expected rule kind.</param>
    /// <returns>The resolved rule, or null when the name is unavailable.</returns>
    CddlRule? ResolveRule(string name, CddlRuleKind kind);
}
