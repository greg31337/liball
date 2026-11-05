namespace odf;

/// <summary>
/// Identifies a node in an OpenDocument application-settings tree.
/// </summary>
public enum OdfSettingKind
{
    /// <summary>The setting node kind is absent or unrecognized.</summary>
    Unknown = 0,
    /// <summary>A typed configuration item.</summary>
    Item = 1,
    /// <summary>A configuration item set.</summary>
    Set = 2,
    /// <summary>An indexed configuration map.</summary>
    IndexedMap = 3,
    /// <summary>A named configuration map.</summary>
    NamedMap = 4,
    /// <summary>A configuration map entry.</summary>
    MapEntry = 5
}
