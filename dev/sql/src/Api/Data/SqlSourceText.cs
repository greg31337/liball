using System;

namespace sql;

/// <summary>Stores SQL text together with optional file identity and line-position lookup data.</summary>
public class SqlSourceText
{
    /// <summary>The text associated with this value.</summary>
    public string Text;
    /// <summary>The file name associated with this value.</summary>
    public string? FileName;
    /// <summary>The line map associated with this value.</summary>
    public SqlLineMap LineMap;
    /// <summary>The full span associated with this value.</summary>
    public SqlTextSpan FullSpan;

    /// <summary>Initializes source text without file identity.</summary>
    /// <param name="text">The source text to process.</param>
    /// <exception cref="ArgumentNullException">text is <c>null</c>.</exception>
    public SqlSourceText(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        Text = text;
        FileName = null;
        LineMap = new SqlLineMap(text);
        FullSpan = new SqlTextSpan(0, text.Length);
    }

    /// <summary>Creates source text without file identity.</summary>
    /// <param name="text">The source text to process.</param>
    /// <returns>The resulting source text.</returns>
    internal static SqlSourceText Create(string text)
    {
        return new SqlSourceText(text);
    }

    /// <summary>Creates source text with optional file identity.</summary>
    /// <param name="text">The source text to process.</param>
    /// <param name="fileName">The optional file name associated with the source text.</param>
    /// <exception cref="ArgumentNullException">text is <c>null</c>.</exception>
    /// <returns>The resulting source text.</returns>
    internal static SqlSourceText Create(string text, string? fileName)
    {
        SqlSourceText sourceText = new SqlSourceText(text);
        sourceText.FileName = fileName;
        return sourceText;
    }

    /// <summary>Returns the length associated with this value.</summary>
    /// <returns>The requested zero-based position.</returns>
    internal int GetLength()
    {
        return Text.Length;
    }

    /// <summary>Returns whether this value contains no elements or source characters.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsEmpty()
    {
        return Text.Length == 0;
    }

    /// <summary>Returns whether file name is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasFileName()
    {
        return FileName != null && FileName.Length > 0;
    }

    /// <summary>Returns the source text covered by the supplied span.</summary>
    /// <param name="span">The source span associated with the value.</param>
    /// <returns>The resulting string.</returns>
    internal string GetText(SqlTextSpan span)
    {
        ValidateSpan(span);
        return Text.Substring(span.Start, span.Length);
    }

    /// <summary>Returns the number of line items.</summary>
    /// <returns>The requested count.</returns>
    internal int GetLineCount()
    {
        return LineMap.GetLineCount();
    }

    /// <summary>Returns the line number associated with this value.</summary>
    /// <param name="position">The zero-based source or stream position.</param>
    /// <returns>The requested zero-based position.</returns>
    internal int GetLineNumber(int position)
    {
        return LineMap.GetLineNumber(position);
    }

    /// <summary>Returns the column number associated with this value.</summary>
    /// <param name="position">The zero-based source or stream position.</param>
    /// <returns>The requested zero-based position.</returns>
    internal int GetColumnNumber(int position)
    {
        return LineMap.GetColumnNumber(position);
    }

    /// <summary>Returns the line start position associated with this value.</summary>
    /// <param name="lineNumber">The line number to use.</param>
    /// <returns>The requested zero-based position.</returns>
    internal int GetLineStartPosition(int lineNumber)
    {
        return LineMap.GetLineStartPosition(lineNumber);
    }

    /// <summary>Returns the line number for span associated with this value.</summary>
    /// <param name="span">The source span associated with the value.</param>
    /// <returns>The requested zero-based position.</returns>
    internal int GetLineNumberForSpan(SqlTextSpan span)
    {
        return LineMap.GetLineNumberForSpan(span);
    }

    /// <summary>Returns the column number for span associated with this value.</summary>
    /// <param name="span">The source span associated with the value.</param>
    /// <returns>The requested zero-based position.</returns>
    internal int GetColumnNumberForSpan(SqlTextSpan span)
    {
        return LineMap.GetColumnNumberForSpan(span);
    }

    internal void ValidateSpan(SqlTextSpan span)
    {
        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        if (span.Start > Text.Length || span.GetEnd() > Text.Length)
        {
            throw new ArgumentOutOfRangeException("span");
        }
    }
}
