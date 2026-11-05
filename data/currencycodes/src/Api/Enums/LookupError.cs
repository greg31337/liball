namespace currencycodes;

/// <summary>
/// Identifies the outcome category of a catalog lookup.
/// </summary>
public enum LookupError
{
    /// <summary>
    /// The lookup completed without an error.
    /// </summary>
    None = 0,

    /// <summary>
    /// The supplied lookup text is malformed.
    /// </summary>
    MalformedInput = 1,

    /// <summary>
    /// The supplied value is well formed but is not assigned.
    /// </summary>
    Unassigned = 2,

    /// <summary>
    /// A matching designation exists outside the requested scope.
    /// </summary>
    OutOfScope = 3,

    /// <summary>
    /// More than one designation satisfies a lookup that requires one result.
    /// </summary>
    Ambiguous = 4
}
