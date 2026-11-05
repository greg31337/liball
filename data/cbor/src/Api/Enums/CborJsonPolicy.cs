namespace cbor;

/// <summary>
/// Selects how CBOR values outside the JSON data model are converted.
/// </summary>
public enum CborJsonPolicy
{
    /// <summary>
    /// Rejects bytes, tags, unsafe integers, non-text or duplicate map keys,
    /// undefined and unassigned simple values, and non-finite floats.
    /// </summary>
    Strict = 0,
    /// <summary>
    /// Uses objects identified by a <c>$cbor</c> field to retain CBOR type information.
    /// Maps use ordered key-value entry arrays so arbitrary and duplicate keys survive.
    /// </summary>
    PreserveInformation = 1,
    /// <summary>
    /// Converts bytes and unsafe integers to strings, removes tags, stringifies scalar map
    /// keys, keeps the last colliding key, and converts unsupported simple or float values to null.
    /// </summary>
    AllowLossy = 2
}
