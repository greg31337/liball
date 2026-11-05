namespace uuid;

/// <summary>Provides the common format-independent identity of a parsed identifier value.</summary>
/// <remarks>Implementations return canonical text from <see cref="ToCanonicalString"/>. The interface intentionally contains only operations shared by every supported format.</remarks>
public interface IIdentifierValue
{
    /// <summary>Returns the identifier format represented by this value.</summary>
    /// <returns>The supported identifier kind.</returns>
    IdentifierKind Kind();

    /// <summary>Formats the value using its canonical text representation.</summary>
    /// <returns>Canonical identifier text.</returns>
    string ToCanonicalString();
}
