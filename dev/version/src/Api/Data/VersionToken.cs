namespace version;

/// <summary>
/// Represents a version token.
/// </summary>
/// <remarks>
/// State is exposed through public fields in accordance with the library's data-oriented API model.
/// </remarks>
public class VersionToken
{
    /// <summary>
    /// Stores the <c>kind</c> value.
    /// </summary>
    public VersionTokenKind kind;
    /// <summary>
    /// Stores the <c>text</c> value.
    /// </summary>
    public string text;
    /// <summary>
    /// Stores the <c>startIndex</c> value.
    /// </summary>
    public int startIndex;
    /// <summary>
    /// Stores the <c>endIndex</c> value.
    /// </summary>
    public int endIndex;
    /// <summary>
    /// Stores the <c>lineNumber</c> value.
    /// </summary>
    public int lineNumber;
    /// <summary>
    /// Stores the <c>columnNumber</c> value.
    /// </summary>
    public int columnNumber;

    /// <summary>
    /// Initializes a new Version Token instance from the supplied values.
    /// </summary>
    /// <remarks>
    /// The constructor validates the supplied state before the instance is made available to callers.
    /// </remarks>
    /// <param name="kindValue">The kind value used by the operation.</param>
    /// <param name="textValue">The text value to process.</param>
    /// <param name="startIndexValue">The start index value used by the operation.</param>
    /// <param name="endIndexValue">The end index value used by the operation.</param>
    /// <param name="lineNumberValue">The line number value used by the operation.</param>
    /// <param name="columnNumberValue">The column number value used by the operation.</param>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">An index, limit, or enum value is outside the supported range.</exception>
    public VersionToken(VersionTokenKind kindValue, string textValue, int startIndexValue, int endIndexValue, int lineNumberValue, int columnNumberValue)
    {
        System.ArgumentNullException.ThrowIfNull(textValue);

        if (startIndexValue < 0)
        {
            throw new System.ArgumentOutOfRangeException("startIndexValue");
        }

        if (endIndexValue < startIndexValue)
        {
            throw new System.ArgumentOutOfRangeException("endIndexValue");
        }

        if (lineNumberValue < 1)
        {
            throw new System.ArgumentOutOfRangeException("lineNumberValue");
        }

        if (columnNumberValue < 1)
        {
            throw new System.ArgumentOutOfRangeException("columnNumberValue");
        }

        kind = kindValue;
        text = textValue;
        startIndex = startIndexValue;
        endIndex = endIndexValue;
        lineNumber = lineNumberValue;
        columnNumber = columnNumberValue;
    }

    /// <summary>
    /// Gets a kind.
    /// </summary>
    /// <returns>The resulting version token kind.</returns>
    internal VersionTokenKind GetKind()
    {
        return kind;
    }

    /// <summary>
    /// Gets a text.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetText()
    {
        return text;
    }

    /// <summary>
    /// Gets a start index.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    internal int GetStartIndex()
    {
        return startIndex;
    }

    /// <summary>
    /// Gets an end index.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    internal int GetEndIndex()
    {
        return endIndex;
    }

    /// <summary>
    /// Gets a length.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    internal int GetLength()
    {
        return endIndex - startIndex;
    }

    /// <summary>
    /// Gets a line number.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    internal int GetLineNumber()
    {
        return lineNumber;
    }

    /// <summary>
    /// Gets a column number.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    internal int GetColumnNumber()
    {
        return columnNumber;
    }
}
