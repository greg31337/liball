namespace odf;

/// <summary>
/// Identifies an inert database front-end component.
/// </summary>
public enum OdfDatabaseComponentKind
{
    /// <summary>The component kind is absent or unrecognized.</summary>
    Unknown = 0,
    /// <summary>A form component.</summary>
    Form = 1,
    /// <summary>A report component.</summary>
    Report = 2,
    /// <summary>A component collection.</summary>
    Collection = 3
}
