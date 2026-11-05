namespace odf;

/// <summary>
/// Identifies a named text-field declaration family.
/// </summary>
public enum OdfTextFieldDeclarationKind
{
    /// <summary>
    /// The declaration kind is absent or unrecognized.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// A variable declaration.
    /// </summary>
    Variable = 1,
    /// <summary>
    /// A user-field declaration.
    /// </summary>
    UserField = 2,
    /// <summary>
    /// A sequence declaration.
    /// </summary>
    Sequence = 3,
    /// <summary>
    /// A dynamic-data-exchange connection declaration.
    /// </summary>
    DdeConnection = 4
}
