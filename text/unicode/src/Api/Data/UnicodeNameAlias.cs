namespace unicode;

/// <summary>
/// Describes one Unicode character-name alias.
/// </summary>
public class UnicodeNameAlias
{
    /// <summary>
    /// The Unicode code point associated with the alias.
    /// </summary>
    public int CodePoint;
    /// <summary>
    /// The alias text.
    /// </summary>
    public string Name;
    /// <summary>
    /// The kind of alias.
    /// </summary>
    public UnicodeNameAliasKind Kind;

    /// <summary>
    /// Initializes a character-name alias record.
    /// </summary>
    /// <param name="codePoint">The Unicode code point associated with the alias.</param>
    /// <param name="name">The alias text.</param>
    /// <param name="kind">The kind of alias.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>name</c> is <c>null</c>.</exception>
    public UnicodeNameAlias(int codePoint, string name, UnicodeNameAliasKind kind)
    {
        System.ArgumentNullException.ThrowIfNull(name);

        CodePoint = codePoint;
        Name = name;
        Kind = kind;
    }
}
