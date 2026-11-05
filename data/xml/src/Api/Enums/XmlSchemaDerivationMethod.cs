namespace xml;

/// <summary>
/// Identifies an XML Schema derivation method.
/// </summary>
public enum XmlSchemaDerivationMethod
{
    /// <summary>
    /// Specifies no derivation method.
    /// </summary>
    None = 0,
    /// <summary>
    /// Specifies derivation by extension.
    /// </summary>
    Extension = 1,
    /// <summary>
    /// Specifies derivation by restriction.
    /// </summary>
    Restriction = 2,
    /// <summary>
    /// Specifies substitution.
    /// </summary>
    Substitution = 4,
    /// <summary>
    /// Specifies derivation by list.
    /// </summary>
    List = 8,
    /// <summary>
    /// Specifies derivation by union.
    /// </summary>
    Union = 16,
    /// <summary>
    /// Specifies every derivation method.
    /// </summary>
    All = 31
}
