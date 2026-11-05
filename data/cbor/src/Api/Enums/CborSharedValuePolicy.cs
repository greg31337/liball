namespace cbor;

/// <summary>
/// Selects how string references and shared values are handled.
/// </summary>
public enum CborSharedValuePolicy
{
    /// <summary>Disallows reference-related tags.</summary>
    Disabled = 0,
    /// <summary>Preserves reference-related tags without resolving them.</summary>
    Preserve = 1,
    /// <summary>Resolves references into the resulting value graph.</summary>
    Resolve = 2
}
