namespace version;

/// <summary>
/// Represents a version diagnostic.
/// </summary>
/// <remarks>
/// State is exposed through public fields in accordance with the library's data-oriented API model.
/// </remarks>
public class VersionDiagnostic
{
    /// <summary>
    /// Stores the <c>code</c> value.
    /// </summary>
    public string code;
    /// <summary>
    /// Stores the <c>message</c> value.
    /// </summary>
    public string message;
    /// <summary>
    /// Stores the <c>sourceText</c> value.
    /// </summary>
    public string sourceText;
    /// <summary>
    /// Stores the <c>index</c> value.
    /// </summary>
    public int index;
    /// <summary>
    /// Stores the <c>lineNumber</c> value.
    /// </summary>
    public int lineNumber;
    /// <summary>
    /// Stores the <c>columnNumber</c> value.
    /// </summary>
    public int columnNumber;

    /// <summary>
    /// Initializes a new Version Diagnostic instance from the supplied values.
    /// </summary>
    /// <remarks>
    /// The constructor validates the supplied state before the instance is made available to callers.
    /// </remarks>
    /// <param name="codeValue">The code value used by the operation.</param>
    /// <param name="messageValue">The message value used by the operation.</param>
    /// <param name="sourceTextValue">The source text value to process.</param>
    /// <param name="indexValue">The index value used by the operation.</param>
    /// <param name="lineNumberValue">The line number value used by the operation.</param>
    /// <param name="columnNumberValue">The column number value used by the operation.</param>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">An index, limit, or enum value is outside the supported range.</exception>
    public VersionDiagnostic(string codeValue, string messageValue, string sourceTextValue, int indexValue, int lineNumberValue, int columnNumberValue)
    {
        System.ArgumentNullException.ThrowIfNull(codeValue);
        System.ArgumentNullException.ThrowIfNull(messageValue);
        System.ArgumentNullException.ThrowIfNull(sourceTextValue);

        if (indexValue < 0)
        {
            throw new System.ArgumentOutOfRangeException("indexValue");
        }

        if (lineNumberValue < 1)
        {
            throw new System.ArgumentOutOfRangeException("lineNumberValue");
        }

        if (columnNumberValue < 1)
        {
            throw new System.ArgumentOutOfRangeException("columnNumberValue");
        }

        code = codeValue;
        message = messageValue;
        sourceText = sourceTextValue;
        index = indexValue;
        lineNumber = lineNumberValue;
        columnNumber = columnNumberValue;
    }

    /// <summary>
    /// Creates a diagnostic whose line and column are calculated from a zero-based source index.
    /// </summary>
    /// <param name="code">The code used by the operation.</param>
    /// <param name="message">The message used by the operation.</param>
    /// <param name="sourceText">The source text to process.</param>
    /// <param name="index">The index used by the operation.</param>
    /// <returns>The resulting version diagnostic.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">An index, limit, or enum value is outside the supported range.</exception>
    internal static VersionDiagnostic FromIndex(string code, string message, string sourceText, int index)
    {
        System.ArgumentNullException.ThrowIfNull(sourceText);

        if (index < 0 || index > sourceText.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        int lineNumber = 1;
        int columnNumber = 1;
        int currentIndex = 0;

        while (currentIndex < index)
        {
            char current = sourceText[currentIndex];

            if (current == '\r')
            {
                lineNumber = lineNumber + 1;
                columnNumber = 1;
                currentIndex = currentIndex + 1;

                if (currentIndex < index && currentIndex < sourceText.Length && sourceText[currentIndex] == '\n')
                {
                    currentIndex = currentIndex + 1;
                }
            }
            else if (current == '\n')
            {
                lineNumber = lineNumber + 1;
                columnNumber = 1;
                currentIndex = currentIndex + 1;
            }
            else
            {
                columnNumber = columnNumber + 1;
                currentIndex = currentIndex + 1;
            }
        }

        return new VersionDiagnostic(code, message, sourceText, index, lineNumber, columnNumber);
    }

    /// <summary>
    /// Gets a code.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetCode()
    {
        return code;
    }

    /// <summary>
    /// Gets a message.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetMessage()
    {
        return message;
    }

    /// <summary>
    /// Gets a source text.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetSourceText()
    {
        return sourceText;
    }

    /// <summary>
    /// Gets an index.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    internal int GetIndex()
    {
        return index;
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
