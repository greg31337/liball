using System;

namespace sql;

/// <summary>Provides source-span and location operations for one SQL source snapshot.</summary>
public interface ISqlSourceLocator
{
    /// <summary>Returns the zero-based exclusive end position of a span.</summary>
    /// <param name="span">The source span to inspect.</param>
    /// <returns>The zero-based exclusive end position.</returns>
    int GetExclusiveEnd(SqlTextSpan span);

    /// <summary>Returns whether a position lies within a half-open span.</summary>
    /// <param name="span">The source span to inspect.</param>
    /// <param name="position">The zero-based source position.</param>
    /// <returns><c>true</c> when the position is contained; otherwise, <c>false</c>.</returns>
    bool ContainsPosition(SqlTextSpan span, int position);

    /// <summary>Returns whether one span completely contains another span.</summary>
    /// <param name="container">The containing span.</param>
    /// <param name="candidate">The span that may be contained.</param>
    /// <returns><c>true</c> when the candidate is contained; otherwise, <c>false</c>.</returns>
    bool ContainsSpan(SqlTextSpan container, SqlTextSpan candidate);

    /// <summary>Returns whether two half-open spans share at least one source position.</summary>
    /// <param name="left">The first span.</param>
    /// <param name="right">The second span.</param>
    /// <returns><c>true</c> when the spans overlap; otherwise, <c>false</c>.</returns>
    bool Overlaps(SqlTextSpan left, SqlTextSpan right);

    /// <summary>Returns an allocated source string for a span.</summary>
    /// <param name="span">The source span to read.</param>
    /// <returns>The source text covered by the span.</returns>
    string GetSourceSlice(SqlTextSpan span);

    /// <summary>Returns a non-owning source slice backed by the locator's retained text snapshot.</summary>
    /// <param name="span">The source span to read.</param>
    /// <returns>The source characters covered by the span.</returns>
    ReadOnlySpan<char> GetSourceSliceSpan(SqlTextSpan span);

    /// <summary>Returns the one-based line number for a zero-based source position.</summary>
    /// <param name="position">The zero-based source position, including the end-of-file position.</param>
    /// <returns>The one-based line number.</returns>
    int GetLineNumber(int position);

    /// <summary>Returns the one-based column number for a zero-based source position.</summary>
    /// <param name="position">The zero-based source position, including the end-of-file position.</param>
    /// <returns>The one-based column number.</returns>
    int GetColumnNumber(int position);

    /// <summary>Returns file identity and one-based coordinates for a source span.</summary>
    /// <param name="span">The source span to locate.</param>
    /// <returns>The resulting file location.</returns>
    SqlFileLocation GetFileLocation(SqlTextSpan span);
}
