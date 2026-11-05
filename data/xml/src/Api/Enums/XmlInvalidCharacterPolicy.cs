namespace xml;

/// <summary>
/// Specifies how invalid output characters are handled.
/// </summary>
public enum XmlInvalidCharacterPolicy
{
    /// <summary>
    /// Reports an error.
    /// </summary>
    Error = 0,
    /// <summary>
    /// Writes a character reference when permitted.
    /// </summary>
    CharacterReference = 1,
    /// <summary>
    /// Writes a configured replacement character.
    /// </summary>
    Replace = 2
}
