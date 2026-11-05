namespace odf;

/// <summary>
/// Identifies the semantic scope of an application setting.
/// </summary>
public enum OdfSettingScope
{
    /// <summary>The setting scope is unknown.</summary>
    Unknown = 0,
    /// <summary>A portable or document-behavior setting.</summary>
    Portable = 1,
    /// <summary>An application view or user-interface setting.</summary>
    View = 2
}
