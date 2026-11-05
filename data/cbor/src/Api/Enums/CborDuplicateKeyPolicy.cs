namespace cbor;

/// <summary>
/// Selects how duplicate CBOR map keys are handled.
/// </summary>
public enum CborDuplicateKeyPolicy
{
    /// <summary>Preserves every map entry in its original order.</summary>
    Preserve = 0,
    /// <summary>Rejects a map when an equivalent key occurs more than once.</summary>
    Reject = 1,
    /// <summary>Retains the first entry for each equivalent key.</summary>
    KeepFirst = 2,
    /// <summary>Retains the last entry for each equivalent key.</summary>
    KeepLast = 3
}
