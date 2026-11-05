using System;

namespace sql;

/// <summary>Provides source-span and location operations over an immutable snapshot of SQL source text.</summary>
public class SqlSourceLocator : ISqlSourceLocator
{
    internal string Text;
    internal string? FileName;
    internal SqlLineMap LineMap;

    /// <summary>Initializes a locator by retaining the current text and file identity from the supplied source.</summary>
    /// <param name="sourceText">The source text to snapshot.</param>
    /// <exception cref="ArgumentNullException">sourceText is <c>null</c>.</exception>
    public SqlSourceLocator(SqlSourceText sourceText)
    {
        if (sourceText == null)
        {
            throw new ArgumentNullException("sourceText");
        }

        Text = sourceText.Text;
        FileName = sourceText.FileName;
        LineMap = new SqlLineMap(Text);
    }

    /// <summary>Returns the zero-based exclusive end position of a span.</summary>
    /// <param name="span">The source span to inspect.</param>
    /// <returns>The zero-based exclusive end position.</returns>
    public int GetExclusiveEnd(SqlTextSpan span)
    {
        ValidateSpanValue(span, "span");
        return span.GetEnd();
    }

    /// <summary>Returns whether a position lies within a half-open span.</summary>
    /// <param name="span">The source span to inspect.</param>
    /// <param name="position">The zero-based source position.</param>
    /// <returns><c>true</c> when the position is contained; otherwise, <c>false</c>.</returns>
    public bool ContainsPosition(SqlTextSpan span, int position)
    {
        ValidateSpanValue(span, "span");
        return span.Contains(position);
    }

    /// <summary>Returns whether one span completely contains another span.</summary>
    /// <param name="container">The containing span.</param>
    /// <param name="candidate">The span that may be contained.</param>
    /// <returns><c>true</c> when the candidate is contained; otherwise, <c>false</c>.</returns>
    public bool ContainsSpan(SqlTextSpan container, SqlTextSpan candidate)
    {
        ValidateSpanValue(container, "container");
        ValidateSpanValue(candidate, "candidate");
        return container.ContainsSpan(candidate);
    }

    /// <summary>Returns whether two half-open spans share at least one source position.</summary>
    /// <param name="left">The first span.</param>
    /// <param name="right">The second span.</param>
    /// <returns><c>true</c> when the spans overlap; otherwise, <c>false</c>.</returns>
    public bool Overlaps(SqlTextSpan left, SqlTextSpan right)
    {
        ValidateSpanValue(left, "left");
        ValidateSpanValue(right, "right");
        return left.OverlapsWith(right);
    }

    /// <summary>Returns an allocated source string for a span.</summary>
    /// <param name="span">The source span to read.</param>
    /// <returns>The source text covered by the span.</returns>
    public string GetSourceSlice(SqlTextSpan span)
    {
        ValidateSourceSpan(span);
        return Text.Substring(span.Start, span.Length);
    }

    /// <summary>Returns a non-owning source slice backed by this locator's retained text snapshot.</summary>
    /// <param name="span">The source span to read.</param>
    /// <returns>The source characters covered by the span.</returns>
    public ReadOnlySpan<char> GetSourceSliceSpan(SqlTextSpan span)
    {
        ValidateSourceSpan(span);
        return Text.AsSpan(span.Start, span.Length);
    }

    /// <summary>Returns the one-based line number for a zero-based source position.</summary>
    /// <param name="position">The zero-based source position, including the end-of-file position.</param>
    /// <returns>The one-based line number.</returns>
    public int GetLineNumber(int position)
    {
        return LineMap.GetLineNumber(position);
    }

    /// <summary>Returns the one-based column number for a zero-based source position.</summary>
    /// <param name="position">The zero-based source position, including the end-of-file position.</param>
    /// <returns>The one-based column number.</returns>
    public int GetColumnNumber(int position)
    {
        return LineMap.GetColumnNumber(position);
    }

    /// <summary>Returns file identity and one-based coordinates for a source span.</summary>
    /// <param name="span">The source span to locate.</param>
    /// <returns>The resulting file location.</returns>
    public SqlFileLocation GetFileLocation(SqlTextSpan span)
    {
        ValidateSourceSpan(span);
        int end = span.GetEnd();
        return new SqlFileLocation(FileName, span, LineMap.GetLineNumber(span.Start), LineMap.GetColumnNumber(span.Start), LineMap.GetLineNumber(end), LineMap.GetColumnNumber(end));
    }

    internal static void ValidateSpanValue(SqlTextSpan span, string parameterName)
    {
        if (span == null)
        {
            throw new ArgumentNullException(parameterName);
        }
    }

    internal void ValidateSourceSpan(SqlTextSpan span)
    {
        ValidateSpanValue(span, "span");
        if (span.Start > Text.Length || span.GetEnd() > Text.Length)
        {
            throw new ArgumentOutOfRangeException("span");
        }
    }
}
