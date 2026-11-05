namespace uri;

/// <summary>
/// Describes the exception-less outcome of comparing two URNs.
/// </summary>
public class UrnEquivalenceResult
{
    /// <summary>Whether both inputs were valid and comparison completed.</summary>
    public bool Success;

    /// <summary>Whether the URNs are equivalent at the selected level.</summary>
    public bool Equal;

    /// <summary>The equivalence level used for the comparison.</summary>
    public UrnEquivalenceLevel Level;

    /// <summary>The first differing component, or <see cref="UriComponent.None"/> when equal.</summary>
    public UriComponent DifferentComponent;

    /// <summary>The failure diagnostic, or <c>null</c> after successful comparison.</summary>
    public UriDiagnostic? Diagnostic;

    /// <summary>Initializes a URN equivalence result.</summary>
    /// <param name="success">Whether comparison completed.</param>
    /// <param name="equal">Whether the URNs are equivalent.</param>
    /// <param name="level">The equivalence level.</param>
    /// <param name="differentComponent">The first differing component.</param>
    /// <param name="diagnostic">The failure diagnostic.</param>
    public UrnEquivalenceResult(
        bool success,
        bool equal,
        UrnEquivalenceLevel level,
        UriComponent differentComponent,
        UriDiagnostic? diagnostic)
    {
        Success = success;
        Equal = equal;
        Level = level;
        DifferentComponent = differentComponent;
        Diagnostic = diagnostic;
    }
}
