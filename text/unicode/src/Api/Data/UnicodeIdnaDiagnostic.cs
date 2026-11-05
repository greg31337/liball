namespace unicode;

/// <summary>
/// Describes one failure found while preparing or validating a Unicode label.
/// </summary>
public class UnicodeIdnaDiagnostic
{
    /// <summary>The zero-based UTF-16 code-unit index in the prepared label.</summary>
    public int Utf16Index;
    /// <summary>The length of the affected range in UTF-16 code units.</summary>
    public int Utf16Length;
    /// <summary>The Unicode scalar value associated with the failure.</summary>
    public int CodePoint;
    /// <summary>The validation status represented by this diagnostic.</summary>
    public UnicodeIdnaStatus Status;
    /// <summary>The stable machine-readable rule identifier.</summary>
    public string Rule;
    /// <summary>A plain-English explanation of the failure.</summary>
    public string Message;

    /// <summary>
    /// Initializes a new diagnostic.
    /// </summary>
    /// <param name="utf16Index">The zero-based UTF-16 code-unit index.</param>
    /// <param name="utf16Length">The affected length in UTF-16 code units.</param>
    /// <param name="codePoint">The associated Unicode code point.</param>
    /// <param name="status">The validation status.</param>
    /// <param name="rule">The stable diagnostic rule identifier.</param>
    /// <param name="message">The diagnostic message.</param>
    /// <exception cref="System.ArgumentException">Thrown when <c>rule</c> or <c>message</c> is empty.</exception>
    public UnicodeIdnaDiagnostic(
        int utf16Index,
        int utf16Length,
        int codePoint,
        UnicodeIdnaStatus status,
        string rule,
        string message)
    {
        System.ArgumentException.ThrowIfNullOrWhiteSpace(rule);
        System.ArgumentException.ThrowIfNullOrWhiteSpace(message);

        Utf16Index = utf16Index;
        Utf16Length = utf16Length;
        CodePoint = codePoint;
        Status = status;
        Rule = rule;
        Message = message;
    }
}
