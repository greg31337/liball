namespace xml;

/// <summary>
/// Specifies an XDM sequence occurrence indicator.
/// </summary>
public enum XdmOccurrence
{
    /// <summary>
    /// Requires exactly one item.
    /// </summary>
    ExactlyOne = 0,
    /// <summary>
    /// Allows zero or one item.
    /// </summary>
    ZeroOrOne = 1,
    /// <summary>
    /// Allows zero or more items.
    /// </summary>
    ZeroOrMore = 2,
    /// <summary>
    /// Requires one or more items.
    /// </summary>
    OneOrMore = 3
}
