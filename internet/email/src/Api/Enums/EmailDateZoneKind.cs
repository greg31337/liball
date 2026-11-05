namespace email;
/// <summary>
/// Specifies the supported date zone values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailDateZoneKind
{
    /// <summary>
    /// Represents the numeric choice or state.
    /// </summary>
    Numeric = 0,
    /// <summary>
    /// Represents the obsolete named choice or state.
    /// </summary>
    ObsoleteNamed = 1,
    /// <summary>
    /// Represents the obsolete military choice or state.
    /// </summary>
    ObsoleteMilitary = 2,
    /// <summary>
    /// Represents the unknown named choice or state.
    /// </summary>
    UnknownNamed = 3
}
