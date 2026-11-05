using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Provides diagnostic location data and operations for molecular graphs.
/// </summary>
public class DiagnosticLocation
{
    /// <summary>
    /// The source name.
    /// </summary>
    public string SourceName;
    /// <summary>
    /// The line number.
    /// </summary>
    public int LineNumber;
    /// <summary>
    /// The column number.
    /// </summary>
    public int ColumnNumber;
    /// <summary>
    /// The character index.
    /// </summary>
    public int CharacterIndex;
    /// <summary>
    /// The context.
    /// </summary>
    public string Context;

    /// <summary>
    /// Creates an empty diagnostic location.
    /// </summary>
    /// <returns>A diagnostic location with no source position.</returns>
    internal static DiagnosticLocation CreateEmpty()
    {
        return new DiagnosticLocation("", 0, 0, -1, "");
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DiagnosticLocation"/> class.
    /// </summary>
    /// <param name="sourceName">The source name.</param>
    /// <param name="lineNumber">The line number.</param>
    /// <param name="columnNumber">The column number.</param>
    /// <param name="characterIndex">The character index.</param>
    /// <param name="context">The context.</param>
    public DiagnosticLocation(string? sourceName, int lineNumber, int columnNumber, int characterIndex, string? context)
    {
        if (sourceName == null)
        {
            SourceName = "";
        }
        else
        {
            SourceName = sourceName;
        }

        LineNumber = lineNumber;
        ColumnNumber = columnNumber;
        CharacterIndex = characterIndex;

        if (context == null)
        {
            Context = "";
        }
        else
        {
            Context = context;
        }
    }

    /// <summary>
    /// Determines whether the value has line column.
    /// </summary>
    /// <returns><c>true</c> when the documented condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool HasLineColumn()
    {
        return LineNumber > 0 && ColumnNumber > 0;
    }

    /// <summary>
    /// Determines whether the value has character index.
    /// </summary>
    /// <returns><c>true</c> when the documented condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool HasCharacterIndex()
    {
        return CharacterIndex >= 0;
    }
}

