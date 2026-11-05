namespace xml;

/// <summary>
/// Compares strings and creates stable keys for one XPath collation.
/// </summary>
public interface IXPathCollation
{
    /// <summary>
    /// Compares two strings using the collation.
    /// </summary>
    /// <param name="left">The left string.</param>
    /// <param name="right">The right string.</param>
    /// <returns>A negative, zero, or positive comparison value.</returns>
    int CompareXPathCollation(string left, string right);

    /// <summary>
    /// Creates a stable binary collation key.
    /// </summary>
    /// <param name="value">The string to key.</param>
    /// <returns>The binary collation key.</returns>
    byte[] CreateXPathCollationKey(string value);
}
