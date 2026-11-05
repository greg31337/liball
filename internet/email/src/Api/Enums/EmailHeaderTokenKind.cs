namespace email;
/// <summary>
/// Specifies the supported header token values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailHeaderTokenKind
{
    /// <summary>
    /// Represents the cfws choice or state.
    /// </summary>
    Cfws = 0,
    /// <summary>
    /// Represents the atom choice or state.
    /// </summary>
    Atom = 1,
    /// <summary>
    /// Represents the dot atom choice or state.
    /// </summary>
    DotAtom = 2,
    /// <summary>
    /// Represents the quoted string choice or state.
    /// </summary>
    QuotedString = 3,
    /// <summary>
    /// Represents the domain literal choice or state.
    /// </summary>
    DomainLiteral = 4,
    /// <summary>
    /// Represents the special choice or state.
    /// </summary>
    Special = 5,
    /// <summary>
    /// Represents the obsolete choice or state.
    /// </summary>
    Obsolete = 6,
    /// <summary>
    /// Represents the invalid choice or state.
    /// </summary>
    Invalid = 7
}
