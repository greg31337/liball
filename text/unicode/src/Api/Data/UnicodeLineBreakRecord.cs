namespace unicode;

/// <summary>
/// Describes a permitted Unicode line-break position in UTF-16 text.
/// </summary>
public class UnicodeLineBreakRecord
{
    /// <summary>
    /// The zero-based UTF-16 code-unit index of the line-break position.
    /// </summary>
    public int Utf16Index;
    /// <summary>
    /// Whether the line break is optional or mandatory.
    /// </summary>
    public UnicodeLineBreakStatus Status;

    /// <summary>
    /// Initializes a line-break result.
    /// </summary>
    /// <param name="utf16Index">The zero-based UTF-16 code-unit index of the break.</param>
    /// <param name="status">Whether the break is optional or mandatory.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>utf16Index</c> is negative.</exception>
    public UnicodeLineBreakRecord(int utf16Index, UnicodeLineBreakStatus status)
    {
        if (utf16Index < 0)
        {
            throw new System.ArgumentOutOfRangeException("utf16Index");
        }

        Utf16Index = utf16Index;
        Status = status;
    }
}
