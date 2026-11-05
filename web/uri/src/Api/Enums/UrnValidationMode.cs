namespace uri;

/// <summary>
/// Identifies the generic or namespace-registration validation applied to a URN.
/// </summary>
public enum UrnValidationMode
{
    /// <summary>Validate the generic RFC 8141 syntax.</summary>
    GenericSyntax = 0,

    /// <summary>Also enforce restrictions that apply to registered namespace identifiers.</summary>
    RegisteredNamespace = 1
}
