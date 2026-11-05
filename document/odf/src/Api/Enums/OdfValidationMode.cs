namespace odf;

/// <summary>
/// Identifies a validation operating mode.
/// </summary>
public enum OdfValidationMode
{
    /// <summary>
    /// Stop at the first conformance failure.
    /// </summary>
    Strict = 0,
    /// <summary>
    /// Collect all applicable diagnostics.
    /// </summary>
    Complete = 1,
    /// <summary>
    /// Validate while allowing documented compatibility recovery.
    /// </summary>
    Recovery = 2,
    /// <summary>
    /// Validate only the package layer.
    /// </summary>
    PackageOnly = 3,
    /// <summary>
    /// Validate XML structure and schema rules.
    /// </summary>
    XmlOnly = 4,
    /// <summary>
    /// Validate semantic and cross-reference rules.
    /// </summary>
    SemanticOnly = 5,
    /// <summary>
    /// Validate against a selected compatibility profile.
    /// </summary>
    Profile = 6
}
