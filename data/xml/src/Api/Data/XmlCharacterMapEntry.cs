namespace xml;

/// <summary>
/// Carries one output character map entry.
/// </summary>
public class XmlCharacterMapEntry
{
    /// <summary>
    /// The Unicode scalar value to replace.
    /// </summary>
    public uint CodePoint;
    /// <summary>
    /// The replacement text, which is emitted without escaping.
    /// </summary>
    public string? Replacement;
    /// <summary>
    /// The name of the character map that owns the entry.
    /// </summary>
    public XmlName? MapName;
}

